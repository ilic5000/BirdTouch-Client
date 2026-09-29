import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../config/app_config.dart';
import '../data/location/location_service.dart' show Coordinates;
import '../utils/server_url.dart';

/// Loaded in `main()` before the app starts, and overridden in the [ProviderScope].
final sharedPreferencesProvider = Provider<SharedPreferences>(
  (ref) => throw UnimplementedError('sharedPreferencesProvider must be overridden'),
);

/// Search radius choices, in meters.
const standardRadiusSteps = [10, 25, 50, 100, 250, 500, 1000, 2000, 5000, 10000];

/// No distance limit (the radius is left out of the search). Larger than every other step, so it
/// sorts and rounds like the biggest radius.
const unlimitedRadiusMeters = 0x7fffffff;

/// Extra choices unlocked by the developer options, for testing across cities. The server allows
/// at most 500 km, or no limit.
const developerRadiusSteps = [
  ...standardRadiusSteps,
  25000,
  50000,
  100000,
  250000,
  500000,
  unlimitedRadiusMeters,
];

const defaultRadiusMeters = 100;

@immutable
final class AppSettings {
  const AppSettings({
    required this.serverUrl,
    required this.radiusMeters,
    required this.developerMode,
    this.fakeLocation,
  });

  /// Base URL of the BirdTouch server, e.g. `http://160.99.21.233:4050`.
  final String serverUrl;

  /// Search radius for nearby users, or [unlimitedRadiusMeters].
  final int radiusMeters;

  /// Developer options are unlocked.
  final bool developerMode;

  /// Location to use instead of the device's, set in the developer options. Only used while they
  /// are on; see [effectiveFakeLocation].
  final Coordinates? fakeLocation;

  Coordinates? get effectiveFakeLocation => developerMode ? fakeLocation : null;

  /// Null when the radius is unlimited.
  double? get radiusKm => radiusMeters == unlimitedRadiusMeters ? null : radiusMeters / 1000;

  List<int> get radiusSteps => developerMode ? developerRadiusSteps : standardRadiusSteps;

  AppSettings copyWith({
    String? serverUrl,
    int? radiusMeters,
    bool? developerMode,
    Coordinates? Function()? fakeLocation,
  }) => AppSettings(
    serverUrl: serverUrl ?? this.serverUrl,
    radiusMeters: radiusMeters ?? this.radiusMeters,
    developerMode: developerMode ?? this.developerMode,
    fakeLocation: fakeLocation == null ? this.fakeLocation : fakeLocation(),
  );
}

final settingsProvider = NotifierProvider<SettingsController, AppSettings>(SettingsController.new);

/// App settings, stored in SharedPreferences (they aren't secret).
class SettingsController extends Notifier<AppSettings> {
  static const _serverUrlKey = 'server_url';
  static const _radiusKey = 'search_radius_meters';
  static const _developerModeKey = 'developer_mode';
  static const _onboardingKeyPrefix = 'onboarding_seen_';
  static const _fakeLatitudeKey = 'fake_latitude';
  static const _fakeLongitudeKey = 'fake_longitude';

  SharedPreferences get _prefs => ref.read(sharedPreferencesProvider);

  @override
  AppSettings build() {
    final prefs = ref.watch(sharedPreferencesProvider);
    final developerMode = prefs.getBool(_developerModeKey) ?? AppConfig.developerOptionsByDefault;
    final steps = developerMode ? developerRadiusSteps : standardRadiusSteps;
    final radius = prefs.getInt(_radiusKey) ?? defaultRadiusMeters;
    final fakeLatitude = prefs.getDouble(_fakeLatitudeKey);
    final fakeLongitude = prefs.getDouble(_fakeLongitudeKey);
    return AppSettings(
      serverUrl: prefs.getString(_serverUrlKey) ?? normalizeServerUrl(AppConfig.defaultServerUrl)!,
      radiusMeters: steps.contains(radius) ? radius : _closest(steps, radius),
      developerMode: developerMode,
      fakeLocation: fakeLatitude == null || fakeLongitude == null
          ? null
          : (latitude: fakeLatitude, longitude: fakeLongitude),
    );
  }

  /// Sets the location used instead of the device's (developer option), or null for the device's.
  Future<void> setFakeLocation(Coordinates? location) async {
    if (location == null) {
      await _prefs.remove(_fakeLatitudeKey);
      await _prefs.remove(_fakeLongitudeKey);
    } else {
      await _prefs.setDouble(_fakeLatitudeKey, location.latitude);
      await _prefs.setDouble(_fakeLongitudeKey, location.longitude);
    }
    state = state.copyWith(fakeLocation: () => location);
  }

  Future<void> setServerUrl(String url) async {
    final normalized = normalizeServerUrl(url);
    if (normalized == null) throw ArgumentError.value(url, 'url', 'Not a valid server address');
    await _prefs.setString(_serverUrlKey, normalized);
    state = state.copyWith(serverUrl: normalized);
  }

  Future<void> resetServerUrl() async {
    await _prefs.remove(_serverUrlKey);
    state = state.copyWith(serverUrl: normalizeServerUrl(AppConfig.defaultServerUrl));
  }

  Future<void> setRadius(int meters) async {
    await _prefs.setInt(_radiusKey, meters);
    state = state.copyWith(radiusMeters: meters);
  }

  Future<void> setDeveloperMode(bool enabled) async {
    await _prefs.setBool(_developerModeKey, enabled);
    var radius = state.radiusMeters;
    if (!enabled && !standardRadiusSteps.contains(radius)) {
      radius = standardRadiusSteps.last;
      await _prefs.setInt(_radiusKey, radius);
    }
    state = state.copyWith(developerMode: enabled, radiusMeters: radius);
  }

  bool onboardingSeen(String userId) => _prefs.getBool('$_onboardingKeyPrefix$userId') ?? false;

  Future<void> setOnboardingSeen(String userId, {bool seen = true}) =>
      _prefs.setBool('$_onboardingKeyPrefix$userId', seen);

  static int _closest(List<int> steps, int value) =>
      steps.reduce((a, b) => (a - value).abs() <= (b - value).abs() ? a : b);
}
