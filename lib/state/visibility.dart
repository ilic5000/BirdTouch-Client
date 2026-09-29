import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config.dart';
import '../data/api/birdtouch_api.dart';
import '../data/location/location_service.dart';
import '../data/models/mode.dart';
import '../data/models/people.dart';
import 'session.dart';
import 'settings.dart';

/// The device's location. Overridden in tests.
final deviceLocationServiceProvider = Provider<LocationService>((ref) => const LocationService());

/// The location used by the app: the device's, or the fake one set in the developer options.
final locationServiceProvider = Provider<LocationService>((ref) {
  final fake = ref.watch(settingsProvider.select((s) => s.effectiveFakeLocation));
  final device = ref.watch(deviceLocationServiceProvider);
  return fake == null ? device : FixedLocationService(fake);
});

/// Street address of a location, or null when the platform can't tell. Use [roundedForAddress]
/// for the key, so small moves don't look the address up again.
final addressProvider = FutureProvider.family<String?, Coordinates>((ref, location) async {
  try {
    return await ref.read(deviceLocationServiceProvider).addressOf(location);
  } catch (e) {
    debugPrint('Could not look up the address: $e');
    return null;
  }
});

/// [latitude] and [longitude] rounded to about 10 m.
Coordinates roundedForAddress(double latitude, double longitude) => (
  latitude: (latitude * 10000).roundToDouble() / 10000,
  longitude: (longitude * 10000).roundToDouble() / 10000,
);

@immutable
final class ModeVisibility {
  const ModeVisibility({
    this.visible = false,
    this.busy = false,
    this.updatedAt,
    this.latitude,
    this.longitude,
  });

  factory ModeVisibility.of(VisibilityStatus status) => ModeVisibility(
    visible: true,
    updatedAt: status.updatedAt,
    latitude: status.latitude,
    longitude: status.longitude,
  );

  final bool visible;

  /// A request to change the visibility or location is in progress.
  final bool busy;

  /// When the location was last sent to the server.
  final DateTime? updatedAt;

  /// The location last sent to the server: where others see the user.
  final double? latitude;
  final double? longitude;

  ModeVisibility copyWith({bool? visible, bool? busy}) => ModeVisibility(
    visible: visible ?? this.visible,
    busy: busy ?? this.busy,
    updatedAt: updatedAt,
    latitude: latitude,
    longitude: longitude,
  );

  @override
  bool operator ==(Object other) =>
      other is ModeVisibility &&
      other.visible == visible &&
      other.busy == busy &&
      other.updatedAt == updatedAt &&
      other.latitude == latitude &&
      other.longitude == longitude;

  @override
  int get hashCode => Object.hash(visible, busy, updatedAt, latitude, longitude);
}

@immutable
final class VisibilityState {
  const VisibilityState({
    this.private = const ModeVisibility(),
    this.business = const ModeVisibility(),
    this.loaded = false,
  });

  final ModeVisibility private;
  final ModeVisibility business;

  /// The visibility has been read from the server.
  final bool loaded;

  ModeVisibility of(Mode mode) => switch (mode) {
    Mode.private => private,
    Mode.business => business,
  };

  bool get anyVisible => private.visible || business.visible;

  VisibilityState asLoaded() => VisibilityState(private: private, business: business, loaded: true);

  VisibilityState withMode(Mode mode, ModeVisibility value) => VisibilityState(
    private: mode == Mode.private ? value : private,
    business: mode == Mode.business ? value : business,
    loaded: loaded,
  );
}

final visibilityProvider = NotifierProvider<VisibilityController, VisibilityState>(
  VisibilityController.new,
);

/// Whether the user is visible in each mode, and keeping their location up to date on the server.
///
/// While the user is visible and the app is in the foreground, the location is sent every
/// [AppConfig.locationRefreshInterval]. The server hides users whose location is too old, so a
/// user who stops using the app eventually disappears for others.
class VisibilityController extends Notifier<VisibilityState> {
  Timer? _timer;

  BirdTouchApi get _api => ref.read(apiProvider);
  LocationService get _location => ref.read(locationServiceProvider);

  @override
  VisibilityState build() {
    final userId = ref.watch(currentUserIdProvider);
    ref.onDispose(() => _timer?.cancel());
    if (userId == null) return const VisibilityState();

    final lifecycle = AppLifecycleListener(
      onResume: () => unawaited(reload()),
      onPause: () => _timer?.cancel(),
    );
    ref.onDispose(lifecycle.dispose);

    // A new fake location (developer option) is sent right away.
    ref.listen(
      settingsProvider.select((s) => s.effectiveFakeLocation),
      // After the notification, so locationServiceProvider has the new location.
      (_, _) => Future.microtask(_refreshQuietly),
    );

    Future.microtask(reload);
    return const VisibilityState();
  }

  /// Reads the visibility from the server, then sends a fresh location for the visible modes.
  Future<void> reload() async {
    try {
      final statuses = await _api.visibility();
      if (!ref.mounted) return;
      var next = const VisibilityState(loaded: true);
      for (final status in statuses) {
        next = next.withMode(status.mode, ModeVisibility.of(status));
      }
      state = next;
      _syncTimer();
      if (next.anyVisible) await _refreshQuietly();
    } catch (e) {
      debugPrint('Could not load the visibility: $e');
      if (ref.mounted) state = state.asLoaded();
    }
  }

  /// Becomes visible in [mode] at the current location, or hides.
  /// Throws [LocationException] or `ApiException`.
  Future<void> setVisible(Mode mode, bool visible) async {
    if (state.of(mode).busy) return;
    _update(mode, (v) => v.copyWith(busy: true));
    try {
      if (visible) {
        final location = await _location.currentLocation();
        final status = await _api.setVisibility(
          mode,
          latitude: location.latitude,
          longitude: location.longitude,
        );
        _update(mode, (_) => ModeVisibility.of(status));
      } else {
        await _api.hide(mode);
        _update(mode, (_) => const ModeVisibility());
      }
    } catch (_) {
      _update(mode, (v) => v.copyWith(busy: false));
      rethrow;
    } finally {
      _syncTimer();
    }
  }

  /// Sends the current location for [mode], or for every visible mode.
  /// Throws [LocationException] or `ApiException`.
  Future<void> refreshLocation([Mode? mode]) async {
    final modes = [
      for (final m in Mode.values)
        if ((mode == null || m == mode) && state.of(m).visible) m,
    ];
    if (modes.isEmpty) return;
    for (final m in modes) {
      _update(m, (v) => v.copyWith(busy: true));
    }
    try {
      final location = await _location.currentLocation();
      for (final m in modes) {
        final status = await _api.setVisibility(
          m,
          latitude: location.latitude,
          longitude: location.longitude,
        );
        _update(m, (_) => ModeVisibility.of(status));
      }
    } finally {
      for (final m in modes) {
        _update(m, (v) => v.copyWith(busy: false));
      }
    }
  }

  /// The server says the user isn't visible in [mode] (e.g. hidden for inactivity).
  void markHidden(Mode mode) {
    _update(mode, (_) => const ModeVisibility());
    _syncTimer();
  }

  Future<void> _refreshQuietly() async {
    try {
      await refreshLocation();
    } catch (e) {
      debugPrint('Could not refresh the location: $e');
    }
  }

  void _update(Mode mode, ModeVisibility Function(ModeVisibility) change) {
    if (!ref.mounted) return;
    state = state.withMode(mode, change(state.of(mode)));
  }

  void _syncTimer() {
    _timer?.cancel();
    _timer = null;
    if (ref.mounted && state.anyVisible) {
      _timer = Timer.periodic(AppConfig.locationRefreshInterval, (_) => _refreshQuietly());
    }
  }
}
