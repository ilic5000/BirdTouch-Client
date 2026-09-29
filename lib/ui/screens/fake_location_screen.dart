import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:latlong2/latlong.dart';

import '../../state/settings.dart';
import '../../state/visibility.dart';
import '../../utils/formatters.dart';
import '../widgets/common.dart';
import '../widgets/map_parts.dart';

/// Developer option: choose a location to use instead of the device's, by tapping the map or
/// typing coordinates. Useful to test with users in another city.
class FakeLocationScreen extends ConsumerStatefulWidget {
  const FakeLocationScreen({super.key});

  /// Where the map starts when there is no better guess: Knez Mihailova, Belgrade.
  static const defaultCenter = LatLng(44.8176, 20.4569);

  @override
  ConsumerState<FakeLocationScreen> createState() => _FakeLocationScreenState();
}

class _FakeLocationScreenState extends ConsumerState<FakeLocationScreen> {
  final _map = MapController();
  final _coordinates = TextEditingController();
  late LatLng? _point = _initialPoint();
  String? _error;

  /// The current fake location, or where the user is visible, if anywhere.
  LatLng? _initialPoint() {
    if (ref.read(settingsProvider).fakeLocation case final fake?) {
      return LatLng(fake.latitude, fake.longitude);
    }
    final visibility = ref.read(visibilityProvider);
    for (final v in [visibility.private, visibility.business]) {
      if (v.latitude != null && v.longitude != null) return LatLng(v.latitude!, v.longitude!);
    }
    return null;
  }

  @override
  void initState() {
    super.initState();
    if (_point case final point?) _coordinates.text = _format(point);
  }

  @override
  void dispose() {
    _map.dispose();
    _coordinates.dispose();
    super.dispose();
  }

  static String _format(LatLng point) => formatCoordinates(point.latitude, point.longitude);

  void _select(LatLng point, {bool move = false}) {
    setState(() {
      _point = point;
      _error = null;
      _coordinates.text = _format(point);
    });
    if (move) _map.move(point, _map.camera.zoom < 14 ? 15 : _map.camera.zoom);
  }

  void _goToTyped() {
    final typed = parseCoordinates(_coordinates.text);
    if (typed == null) {
      setState(() => _error = 'Enter latitude and longitude, e.g. 44.8176, 20.4569');
      return;
    }
    FocusScope.of(context).unfocus();
    _select(LatLng(typed.latitude, typed.longitude), move: true);
  }

  Future<void> _save(LatLng? point) async {
    await ref
        .read(settingsProvider.notifier)
        .setFakeLocation(
          point == null ? null : (latitude: point.latitude, longitude: point.longitude),
        );
    if (!mounted) return;
    showMessage(
      context,
      point == null ? 'Using the device location' : 'Using the fake location ${_format(point)}',
    );
    Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final active = ref.watch(settingsProvider.select((s) => s.fakeLocation)) != null;
    final point = _point;

    return Scaffold(
      appBar: AppBar(title: const Text('Fake location')),
      body: Column(
        children: [
          Expanded(
            child: FlutterMap(
              mapController: _map,
              options: MapOptions(
                initialCenter: point ?? FakeLocationScreen.defaultCenter,
                initialZoom: point == null ? 12 : 15,
                onTap: (_, tapped) => _select(tapped),
                interactionOptions: const InteractionOptions(
                  flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
                ),
              ),
              children: [
                const MapTiles(),
                if (point != null)
                  MarkerLayer(
                    markers: [
                      Marker(
                        point: point,
                        width: 48,
                        height: 48,
                        // The tip of the pin is on the location.
                        alignment: Alignment.topCenter,
                        child: Icon(Icons.location_on, size: 48, color: theme.colorScheme.error),
                      ),
                    ],
                  ),
                const MapAttribution(),
              ],
            ),
          ),
          Material(
            elevation: 4,
            color: theme.colorScheme.surfaceContainerLow,
            child: SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 16),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      'Tap the map, or type coordinates. BirdTouch then uses this location '
                      'instead of the device\'s while the developer options are on.',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _coordinates,
                      keyboardType: const TextInputType.numberWithOptions(
                        signed: true,
                        decimal: true,
                      ),
                      textInputAction: TextInputAction.go,
                      onSubmitted: (_) => _goToTyped(),
                      decoration: InputDecoration(
                        labelText: 'Latitude, longitude',
                        prefixIcon: const Icon(Icons.pin_drop_outlined),
                        errorText: _error,
                        suffixIcon: IconButton(
                          tooltip: 'Show on the map',
                          onPressed: _goToTyped,
                          icon: const Icon(Icons.arrow_forward),
                        ),
                      ),
                    ),
                    if (point != null) ...[
                      const SizedBox(height: 8),
                      AddressText(
                        latitude: point.latitude,
                        longitude: point.longitude,
                        maxLines: 1,
                        style: theme.textTheme.bodyMedium,
                      ),
                    ],
                    const SizedBox(height: 16),
                    Row(
                      children: [
                        if (active) ...[
                          Expanded(
                            child: OutlinedButton(
                              onPressed: () => _save(null),
                              child: const Text('Use device'),
                            ),
                          ),
                          const SizedBox(width: 12),
                        ],
                        Expanded(
                          child: FilledButton(
                            onPressed: point == null ? null : () => _save(point),
                            child: const Text('Use this location'),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
