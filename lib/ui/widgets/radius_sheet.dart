import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../state/settings.dart';
import '../../utils/formatters.dart';

/// Lets the user pick the search radius for nearby users.
Future<void> showRadiusSheet(BuildContext context) => showModalBottomSheet<void>(
  context: context,
  useSafeArea: true,
  builder: (_) => const SafeArea(
    top: false,
    child: Padding(
      padding: EdgeInsets.fromLTRB(24, 0, 24, 32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [RadiusPicker()],
      ),
    ),
  ),
);

/// Slider over the allowed search radius steps. Saves the value when the user lets go.
class RadiusPicker extends ConsumerStatefulWidget {
  const RadiusPicker({super.key});

  @override
  ConsumerState<RadiusPicker> createState() => _RadiusPickerState();
}

class _RadiusPickerState extends ConsumerState<RadiusPicker> {
  int? _dragIndex;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final settings = ref.watch(settingsProvider);
    final steps = settings.radiusSteps;
    final savedIndex = steps.indexOf(settings.radiusMeters).clamp(0, steps.length - 1);
    final index = _dragIndex ?? savedIndex;

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(child: Text('Search radius', style: theme.textTheme.titleLarge)),
            Text(
              formatRadius(steps[index]),
              style: theme.textTheme.titleLarge?.copyWith(
                color: theme.colorScheme.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
        const SizedBox(height: 4),
        Text(
          'How far away BirdTouch looks for people who are visible.',
          style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
        ),
        const SizedBox(height: 8),
        Slider(
          value: index.toDouble(),
          max: (steps.length - 1).toDouble(),
          divisions: steps.length - 1,
          label: formatRadius(steps[index]),
          onChanged: (value) => setState(() => _dragIndex = value.round()),
          onChangeEnd: (value) async {
            await ref.read(settingsProvider.notifier).setRadius(steps[value.round()]);
            if (mounted) setState(() => _dragIndex = null);
          },
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(formatRadius(steps.first), style: theme.textTheme.bodySmall),
              Text(formatRadius(steps.last), style: theme.textTheme.bodySmall),
            ],
          ),
        ),
      ],
    );
  }
}
