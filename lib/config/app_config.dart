import 'package:flutter/foundation.dart';

/// Build-time configuration.
///
/// Values come from `--dart-define` / `--dart-define-from-file` (see `dart_defines/`), so the same
/// code can be built for testers and for production without changes.
abstract final class AppConfig {
  /// Server used until the user picks another one in the server settings.
  static const defaultServerUrl = String.fromEnvironment(
    'BIRDTOUCH_API_URL',
    defaultValue: 'http://188.245.72.17:4050',
  );

  /// Whether developer options (larger search radius, server address, ...) are unlocked on the
  /// first start. Always on in debug builds. Testers can also unlock them by long-pressing the
  /// "Settings" title.
  static const developerOptionsByDefault = bool.fromEnvironment(
    'BIRDTOUCH_DEV_OPTIONS',
    defaultValue: kDebugMode,
  );

  /// How often the location is sent to the server while the user is visible. The server hides
  /// users whose location hasn't been updated for 24 hours.
  static const locationRefreshInterval = Duration(minutes: 5);

  /// Maximum number of nearby users requested at once (server allows 1-200).
  static const nearbyLimit = 100;

  /// Map tiles, as a URL template. OpenStreetMap's tiles are fine for testing and light use (see
  /// https://operations.osmfoundation.org/policies/tiles/); use another provider for production.
  static const mapTilesUrl = String.fromEnvironment(
    'BIRDTOUCH_MAP_TILES_URL',
    defaultValue: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
  );

  /// Attribution required by the map tile provider, shown on the maps.
  static const mapAttribution = String.fromEnvironment(
    'BIRDTOUCH_MAP_ATTRIBUTION',
    defaultValue: 'OpenStreetMap contributors',
  );

  /// Sent in the User-Agent of tile requests, as tile providers ask.
  static const appPackageName = 'rs.birdtouch.birdtouch';

  /// Pictures are scaled down on the device before uploading (server limit is 5 MB).
  static const pictureMaxDimension = 1024.0;
  static const pictureQuality = 85;

  static const sourceCodeUrl = 'https://github.com/ilic5000/BirdTouch-Client';
  static const serverSourceCodeUrl = 'https://github.com/ilic5000/BirdTouch-WebAPI';
  static const authorName = 'Nenad Ilić';
  static const authorEmail = 'nenad.ilic@pmf.edu.rs';
}
