import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../data/location/location_service.dart';
import '../../data/models/mode.dart';
import '../../state/people.dart';
import '../../state/settings.dart';
import '../../state/visibility.dart';
import '../../utils/formatters.dart';
import '../router.dart';
import '../widgets/common.dart';
import '../widgets/map_parts.dart';
import '../widgets/person_tile.dart';
import '../widgets/radius_sheet.dart';

/// Visibility switch and the users visible nearby, for one mode.
class NearbyScreen extends ConsumerWidget {
  const NearbyScreen({super.key, required this.mode});

  final Mode mode;

  String get _title => mode == Mode.private ? 'Private' : 'Business';

  Future<void> _refresh(BuildContext context, WidgetRef ref) async {
    final visibility = ref.read(visibilityProvider.notifier);
    if (!ref.read(visibilityProvider).of(mode).visible) {
      await visibility.reload();
      return;
    }
    try {
      // A new location reloads the list by itself.
      await visibility.refreshLocation(mode);
      await ref.read(nearbyProvider(mode).future);
    } catch (e) {
      if (context.mounted) showLocationError(context, ref, e);
      await ref.refresh(nearbyProvider(mode).future).catchError((Object _) => const <Never>[]);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final visible = ref.watch(visibilityProvider.select((s) => s.of(mode).visible));
    final loaded = ref.watch(visibilityProvider.select((s) => s.loaded));
    final radius = ref.watch(settingsProvider.select((s) => s.radiusMeters));
    final unlimited = radius == unlimitedRadiusMeters;
    final nearby = ref.watch(nearbyProvider(mode));

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            const BrandLogo(size: 30),
            const SizedBox(width: 12),
            Flexible(child: Text(_title, overflow: TextOverflow.ellipsis)),
          ],
        ),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 12),
            child: ActionChip(
              avatar: const Icon(Icons.radar, size: 18),
              // "Unlimited" doesn't fit next to the title on small phones.
              label: Text(unlimited ? '∞' : formatRadius(radius)),
              tooltip: 'Search radius',
              onPressed: () => showRadiusSheet(context),
            ),
          ),
        ],
      ),
      floatingActionButton: visible
          ? Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                FloatingActionButton.small(
                  heroTag: 'refresh-${mode.name}',
                  tooltip: 'Update my location',
                  onPressed: () => _refresh(context, ref),
                  child: const Icon(Icons.my_location),
                ),
                const SizedBox(height: 12),
                FloatingActionButton.extended(
                  heroTag: 'map-${mode.name}',
                  onPressed: () => context.push(Routes.map(mode)),
                  icon: const Icon(Icons.map_outlined),
                  label: const Text('Map'),
                ),
              ],
            )
          : null,
      body: RefreshIndicator(
        onRefresh: () => _refresh(context, ref),
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
              sliver: SliverToBoxAdapter(child: VisibilityCard(mode: mode)),
            ),
            if (!loaded)
              const SliverFillRemaining(
                hasScrollBody: false,
                child: Center(child: CircularProgressIndicator()),
              )
            else if (!visible)
              SliverFillRemaining(
                hasScrollBody: false,
                child: EmptyState(
                  icon: Icons.visibility_off_outlined,
                  title: 'You are not visible',
                  message: mode == Mode.private
                      ? 'Make yourself visible to find people around you. '
                            'If you are not visible, they cannot find you, and you cannot find them.'
                      : 'Make yourself visible to find businesses around you. '
                            'Others will see your business profile.',
                ),
              )
            else
              ...switch (nearby) {
                AsyncValue(:final value?, hasError: false) when value.isEmpty => [
                  SliverFillRemaining(
                    hasScrollBody: false,
                    child: EmptyState(
                      icon: Icons.travel_explore,
                      title: 'Nobody around yet',
                      message: unlimited
                          ? 'No one else is visible right now. Pull down to search again.'
                          : 'No one is visible within ${formatRadius(radius)}. '
                                'Pull down to search again, or try a larger radius.',
                      action: FilledButton.tonalIcon(
                        onPressed: () => showRadiusSheet(context),
                        icon: const Icon(Icons.radar),
                        label: const Text('Change radius'),
                      ),
                    ),
                  ),
                ],
                AsyncValue(:final value?, hasError: false) => [
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(20, 12, 20, 4),
                    sliver: SliverToBoxAdapter(
                      child: Text(
                        '${value.length} ${value.length == 1 ? 'person' : 'people'} '
                        '${unlimited ? 'visible' : 'within ${formatRadius(radius)}'}',
                        style: Theme.of(context).textTheme.titleSmall,
                      ),
                    ),
                  ),
                  SliverList.builder(
                    itemCount: value.length,
                    itemBuilder: (_, i) => PersonTile(person: value[i]),
                  ),
                  // Room for the buttons.
                  const SliverToBoxAdapter(child: SizedBox(height: 150)),
                ],
                AsyncValue(:final error?) => [
                  SliverFillRemaining(
                    hasScrollBody: false,
                    child: ErrorState(
                      error: error,
                      onRetry: () => ref.invalidate(nearbyProvider(mode)),
                    ),
                  ),
                ],
                _ => [
                  const SliverFillRemaining(
                    hasScrollBody: false,
                    child: Center(child: CircularProgressIndicator()),
                  ),
                ],
              },
          ],
        ),
      ),
    );
  }
}

/// Card with the visibility switch of a mode, and a warning when the profile is too incomplete
/// for others to see it.
class VisibilityCard extends ConsumerWidget {
  const VisibilityCard({super.key, required this.mode});

  final Mode mode;

  Future<void> _toggle(BuildContext context, WidgetRef ref, bool visible) async {
    try {
      await ref.read(visibilityProvider.notifier).setVisible(mode, visible);
    } catch (e) {
      if (context.mounted) showLocationError(context, ref, e);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final state = ref.watch(visibilityProvider.select((s) => s.of(mode)));
    final loaded = ref.watch(visibilityProvider.select((s) => s.loaded));
    final profile = ref.watch(myProfileProvider(mode)).value;
    final showIncomplete = state.visible && profile != null && !profile.isDiscoverable;

    final background = state.visible ? colors.primaryContainer : colors.surfaceContainerHigh;
    final foreground = state.visible ? colors.onPrimaryContainer : colors.onSurface;
    final updated = state.updatedAt;
    final (latitude, longitude) = (state.latitude, state.longitude);
    final fake = ref.watch(settingsProvider.select((s) => s.effectiveFakeLocation)) != null;

    return AnimatedContainer(
      duration: const Duration(milliseconds: 250),
      decoration: BoxDecoration(color: background, borderRadius: BorderRadius.circular(24)),
      padding: const EdgeInsets.fromLTRB(20, 16, 16, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                state.visible ? Icons.visibility : Icons.visibility_off_outlined,
                color: foreground,
                size: 28,
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      state.visible ? 'You are visible' : 'Make me visible',
                      style: theme.textTheme.titleMedium?.copyWith(
                        color: foreground,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      state.visible
                          ? 'Location updated ${updated == null ? 'just now' : formatTimeAgo(updated)}'
                          : 'Share your location to see and be seen',
                      style: theme.textTheme.bodySmall?.copyWith(color: foreground),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              if (state.busy || !loaded)
                const Padding(padding: EdgeInsets.all(12), child: ButtonProgress())
              else
                Switch(value: state.visible, onChanged: (value) => _toggle(context, ref, value)),
            ],
          ),
          if (state.visible && latitude != null && longitude != null) ...[
            const SizedBox(height: 8),
            // Where others see the user; opens the map.
            InkWell(
              borderRadius: BorderRadius.circular(12),
              onTap: () => context.push(Routes.map(mode)),
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  children: [
                    const SizedBox(width: 2),
                    Icon(Icons.place_outlined, color: foreground, size: 22),
                    const SizedBox(width: 18),
                    Flexible(
                      child: AddressText(
                        latitude: latitude,
                        longitude: longitude,
                        maxLines: 1,
                        style: theme.textTheme.bodyMedium?.copyWith(color: foreground),
                      ),
                    ),
                    if (fake) ...[const SizedBox(width: 8), const FakeLocationBadge()],
                  ],
                ),
              ),
            ),
          ],
          if (showIncomplete) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.fromLTRB(12, 8, 4, 8),
              decoration: BoxDecoration(
                color: colors.surface.withValues(alpha: 0.7),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Row(
                children: [
                  Icon(Icons.info_outline, color: colors.tertiary, size: 20),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Others cannot see you until you add '
                      '${profile.missingForDiscovery.join(' and ')}.',
                      style: theme.textTheme.bodySmall,
                    ),
                  ),
                  TextButton(
                    onPressed: () => context.push(Routes.editProfile(mode)),
                    child: const Text('Edit'),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// Shows a location or API error, offering to open the settings when that is the fix.
void showLocationError(BuildContext context, WidgetRef ref, Object error) {
  if (error is LocationException && error.problem.needsSettings) {
    showMessage(
      context,
      error.message,
      action: SnackBarAction(
        label: 'Settings',
        onPressed: () => ref.read(locationServiceProvider).openSettingsFor(error.problem),
      ),
    );
  } else {
    showError(context, error);
  }
}
