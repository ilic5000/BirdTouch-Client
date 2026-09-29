import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:latlong2/latlong.dart';

import '../../data/models/mode.dart';
import '../../data/models/people.dart';
import '../../state/people.dart';
import '../../state/settings.dart';
import '../../state/visibility.dart';
import '../../utils/formatters.dart';
import '../router.dart';
import '../widgets/common.dart';
import '../widgets/map_parts.dart';
import 'nearby_screen.dart' show showLocationError;

/// The users visible nearby in a mode on a map, around the user's own location (the one others
/// see). Tapping someone opens their profile.
class NearbyMapScreen extends ConsumerStatefulWidget {
  const NearbyMapScreen({super.key, required this.mode});

  final Mode mode;

  @override
  ConsumerState<NearbyMapScreen> createState() => _NearbyMapScreenState();
}

class _NearbyMapScreenState extends ConsumerState<NearbyMapScreen> {
  final _map = MapController();
  bool _refreshing = false;

  Mode get _mode => widget.mode;

  @override
  void dispose() {
    _map.dispose();
    super.dispose();
  }

  /// Frames the user and everyone found, or zooms in on the user when nobody is around.
  CameraFit _fitAll(LatLng me, List<NearbyUser> people) => people.isEmpty
      ? CameraFit.coordinates(coordinates: [me], maxZoom: 16)
      : CameraFit.coordinates(
          coordinates: [me, for (final p in people) LatLng(p.latitude!, p.longitude!)],
          // Room for the markers' names and for the card at the bottom.
          padding: const EdgeInsets.fromLTRB(72, 72, 72, 240),
          maxZoom: 17,
        );

  Future<void> _refresh() async {
    setState(() => _refreshing = true);
    try {
      await ref.read(visibilityProvider.notifier).refreshLocation(_mode);
      await ref.read(nearbyProvider(_mode).future);
    } catch (e) {
      if (mounted) showLocationError(context, ref, e);
    } finally {
      if (mounted) setState(() => _refreshing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final visibility = ref.watch(visibilityProvider.select((s) => s.of(_mode)));
    final settings = ref.watch(settingsProvider);
    final nearby = ref.watch(nearbyProvider(_mode));
    final people = [
      for (final p in nearby.value ?? const <NearbyUser>[])
        if (p.latitude != null && p.longitude != null) p,
    ];
    final title = _mode == Mode.private ? 'People nearby' : 'Businesses nearby';

    final (latitude, longitude) = (visibility.latitude, visibility.longitude);
    if (!visibility.visible || latitude == null || longitude == null) {
      return Scaffold(
        appBar: AppBar(title: Text(title)),
        body: const EmptyState(
          icon: Icons.visibility_off_outlined,
          title: 'You are not visible',
          message: 'Make yourself visible to see who is around you on the map.',
        ),
      );
    }
    final me = LatLng(latitude, longitude);
    final radius = settings.radiusMeters;

    return Scaffold(
      appBar: AppBar(
        title: Text(title),
        actions: [
          IconButton(
            tooltip: 'Update my location',
            onPressed: _refreshing ? null : _refresh,
            icon: _refreshing ? const ButtonProgress() : const Icon(Icons.refresh),
          ),
        ],
      ),
      body: Stack(
        children: [
          FlutterMap(
            mapController: _map,
            options: MapOptions(
              initialCenter: me,
              initialZoom: 16,
              initialCameraFit: _fitAll(me, people),
              interactionOptions: const InteractionOptions(
                flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
              ),
            ),
            children: [
              const MapTiles(),
              if (radius != unlimitedRadiusMeters)
                CircleLayer(
                  circles: [
                    CircleMarker(
                      point: me,
                      radius: radius.toDouble(),
                      useRadiusInMeter: true,
                      color: theme.colorScheme.primary.withValues(alpha: 0.07),
                      borderColor: theme.colorScheme.primary.withValues(alpha: 0.5),
                      borderStrokeWidth: 1.5,
                    ),
                  ],
                ),
              MarkerLayer(
                markers: [
                  for (final person in people)
                    Marker(
                      key: ValueKey(person.userId),
                      point: LatLng(person.latitude!, person.longitude!),
                      width: PersonMarker.size,
                      height: PersonMarker.size,
                      child: PersonMarker(
                        person: person,
                        onTap: () => context.push(Routes.person(person), extra: person),
                      ),
                    ),
                  // Last, so it's never hidden under someone standing close by.
                  Marker(
                    point: me,
                    width: MyLocationDot.size,
                    height: MyLocationDot.size,
                    child: const IgnorePointer(child: MyLocationDot()),
                  ),
                ],
              ),
              const MapAttribution(),
            ],
          ),
          Positioned(
            left: 16,
            right: 16,
            bottom: 16 + MediaQuery.paddingOf(context).bottom,
            child: _MyLocationCard(
              latitude: latitude,
              longitude: longitude,
              fake: settings.effectiveFakeLocation != null,
              summary: switch (nearby) {
                AsyncValue(hasError: true) => 'Could not load people nearby',
                AsyncValue(:final value?) =>
                  '${value.length} ${value.length == 1 ? 'person' : 'people'} '
                      '${radius == unlimitedRadiusMeters ? 'visible' : 'within ${formatRadius(radius)}'}',
                _ => 'Looking around…',
              },
              onCenter: () => _map.move(me, 17),
              onFitAll: () => _map.fitCamera(_fitAll(me, people)),
            ),
          ),
        ],
      ),
    );
  }
}

/// "You are here": the address others see the user at, and buttons to move the map.
class _MyLocationCard extends StatelessWidget {
  const _MyLocationCard({
    required this.latitude,
    required this.longitude,
    required this.fake,
    required this.summary,
    required this.onCenter,
    required this.onFitAll,
  });

  final double latitude;
  final double longitude;
  final bool fake;
  final String summary;
  final VoidCallback onCenter;
  final VoidCallback onFitAll;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      elevation: 3,
      color: theme.colorScheme.surfaceContainerLowest,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 8, 12),
        child: Row(
          children: [
            const SizedBox.square(dimension: 28, child: MyLocationDot()),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    children: [
                      Text('You are here', style: theme.textTheme.titleSmall),
                      if (fake) ...[const SizedBox(width: 8), const FakeLocationBadge()],
                    ],
                  ),
                  AddressText(
                    latitude: latitude,
                    longitude: longitude,
                    style: theme.textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    summary,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
            IconButton(
              tooltip: 'Show everyone',
              onPressed: onFitAll,
              icon: const Icon(Icons.zoom_out_map),
            ),
            IconButton.filledTonal(
              tooltip: 'Center on me',
              onPressed: onCenter,
              icon: const Icon(Icons.my_location),
            ),
          ],
        ),
      ),
    );
  }
}
