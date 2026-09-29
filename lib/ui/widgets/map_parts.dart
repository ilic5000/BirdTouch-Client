import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../config/app_config.dart';
import '../../data/models/people.dart';
import '../../state/visibility.dart';
import '../../utils/formatters.dart';
import '../../utils/links.dart';
import '../screens/person_screen.dart' show openUri;
import 'person_tile.dart' show displayNameOf;
import 'profile_avatar.dart';

/// Where map tiles come from; null for flutter_map's network provider. Overridden in tests, which
/// have no network. Each map needs its own instance: a map disposes its provider when it closes.
final mapTileProviderProvider = Provider<TileProvider Function()?>((ref) => null);

/// The background map, from [AppConfig.mapTilesUrl].
class MapTiles extends ConsumerWidget {
  const MapTiles({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => TileLayer(
    urlTemplate: AppConfig.mapTilesUrl,
    userAgentPackageName: AppConfig.appPackageName,
    tileProvider: ref.watch(mapTileProviderProvider)?.call(),
    maxNativeZoom: 19,
  );
}

/// Credit to the map data, required by the tile providers.
class MapAttribution extends StatelessWidget {
  const MapAttribution({super.key});

  @override
  Widget build(BuildContext context) => RichAttributionWidget(
    showFlutterMapAttribution: false,
    attributions: [
      TextSourceAttribution(
        AppConfig.mapAttribution,
        onTap: () => openUri(context, Uri.parse('https://www.openstreetmap.org/copyright')),
      ),
    ],
  );
}

/// The user's own position: a blue dot with a soft halo.
class MyLocationDot extends StatelessWidget {
  const MyLocationDot({super.key, this.color});

  final Color? color;

  static const size = 64.0;

  @override
  Widget build(BuildContext context) {
    final dot = color ?? const Color(0xFF1A73E8);
    return Semantics(
      label: 'Your location',
      child: Stack(
        alignment: Alignment.center,
        children: [
          Container(
            width: size,
            height: size,
            decoration: BoxDecoration(color: dot.withValues(alpha: 0.18), shape: BoxShape.circle),
          ),
          Container(
            width: 22,
            height: 22,
            decoration: BoxDecoration(
              color: dot,
              shape: BoxShape.circle,
              border: Border.all(color: Colors.white, width: 3),
              boxShadow: const [BoxShadow(color: Colors.black26, blurRadius: 4)],
            ),
          ),
        ],
      ),
    );
  }
}

/// A nearby user on the map: their picture (or initials) with their name below.
class PersonMarker extends StatelessWidget {
  const PersonMarker({super.key, required this.person, required this.onTap});

  final Person person;
  final VoidCallback onTap;

  /// Size of the marker box; the picture is in its center, so it sits on the person's location.
  static const size = 132.0;
  static const _radius = 24.0;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final name = displayNameOf(person.profile);
    return Stack(
      alignment: Alignment.center,
      children: [
        GestureDetector(
          onTap: onTap,
          child: Container(
            padding: const EdgeInsets.all(3),
            decoration: const BoxDecoration(
              color: Colors.white,
              shape: BoxShape.circle,
              boxShadow: [BoxShadow(color: Colors.black38, blurRadius: 6)],
            ),
            child: ProfileAvatar(
              pictureUrl: person.profile.pictureUrl,
              name: person.profile.displayName,
              mode: person.mode,
              radius: _radius,
            ),
          ),
        ),
        Positioned(
          top: size / 2 + _radius + 6,
          left: 0,
          right: 0,
          child: Center(
            child: GestureDetector(
              onTap: onTap,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surface.withValues(alpha: 0.92),
                  borderRadius: BorderRadius.circular(10),
                  boxShadow: const [BoxShadow(color: Colors.black26, blurRadius: 3)],
                ),
                child: Text(
                  name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.labelMedium?.copyWith(fontWeight: FontWeight.w600),
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

/// Street address of a location (looked up on the device), falling back to the coordinates.
class AddressText extends ConsumerWidget {
  const AddressText({
    super.key,
    required this.latitude,
    required this.longitude,
    this.style,
    this.maxLines = 2,
  });

  final double latitude;
  final double longitude;
  final TextStyle? style;
  final int maxLines;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final address = ref.watch(addressProvider(roundedForAddress(latitude, longitude)));
    final text = switch (address) {
      AsyncValue(value: final String a?) => a,
      AsyncValue(isLoading: true) => 'Finding the address…',
      _ => formatCoordinates(latitude, longitude),
    };
    return Text(text, maxLines: maxLines, overflow: TextOverflow.ellipsis, style: style);
  }
}

/// Opens [latitude], [longitude] in the phone's maps app.
Future<void> openInMapsApp(BuildContext context, double latitude, double longitude) =>
    openUri(context, mapsUri('$latitude,$longitude')!);

/// Marks a location that comes from the developer options, not from the device.
class FakeLocationBadge extends StatelessWidget {
  const FakeLocationBadge({super.key});

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
      decoration: BoxDecoration(
        color: colors.tertiaryContainer,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        'Fake',
        style: Theme.of(context).textTheme.labelSmall?.copyWith(color: colors.onTertiaryContainer),
      ),
    );
  }
}
