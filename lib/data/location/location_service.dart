import 'package:geocoding/geocoding.dart';
import 'package:geolocator/geolocator.dart';

/// Why the location could not be determined.
enum LocationProblem {
  serviceDisabled('Location services are turned off. Turn them on to become visible.'),
  permissionDenied('BirdTouch needs your location to show you to people nearby.'),
  permissionDeniedForever(
    'Location permission is blocked. Allow it in the system settings to become visible.',
  ),
  unavailable('Could not determine your location. Please try again.');

  const LocationProblem(this.message);

  final String message;

  /// Whether the problem can only be fixed in the system settings.
  bool get needsSettings => this == serviceDisabled || this == permissionDeniedForever;
}

class LocationException implements Exception {
  const LocationException(this.problem);

  final LocationProblem problem;

  String get message => problem.message;

  @override
  String toString() => 'LocationException(${problem.name})';
}

typedef Coordinates = ({double latitude, double longitude});

/// Gets the device location, asking for permission when needed. Only "while in use" location is
/// used: the app never tracks the location in the background.
class LocationService {
  const LocationService();

  /// Current location. Throws [LocationException].
  Future<Coordinates> currentLocation() async {
    if (!await Geolocator.isLocationServiceEnabled()) {
      throw const LocationException(LocationProblem.serviceDisabled);
    }
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    switch (permission) {
      case LocationPermission.denied:
        throw const LocationException(LocationProblem.permissionDenied);
      case LocationPermission.deniedForever:
        throw const LocationException(LocationProblem.permissionDeniedForever);
      case LocationPermission.whileInUse ||
          LocationPermission.always ||
          LocationPermission.unableToDetermine:
        break;
    }

    try {
      final position = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 20),
        ),
      );
      return (latitude: position.latitude, longitude: position.longitude);
    } catch (_) {
      // Indoors a fresh GPS fix can time out; a recent known position is good enough.
      final last = await Geolocator.getLastKnownPosition();
      if (last != null) return (latitude: last.latitude, longitude: last.longitude);
      throw const LocationException(LocationProblem.unavailable);
    }
  }

  /// Street address of a location, e.g. "Knez Mihailova 1, Beograd", or null when unknown. Uses
  /// the platform's geocoder, which needs a network connection.
  Future<String?> addressOf(Coordinates location) async {
    final places = await Geocoding().placemarkFromCoordinates(
      location.latitude,
      location.longitude,
    );
    if (places.isEmpty) return null;
    final place = places.first;
    final street = [
      place.thoroughfare,
      place.subThoroughfare,
    ].where((part) => part != null && part.isNotEmpty).join(' ');
    final parts = {
      if (street.isNotEmpty) street else ?place.name,
      ?place.locality,
    }.where((part) => part.isNotEmpty);
    return parts.isEmpty ? null : parts.join(', ');
  }

  /// Opens the settings screen that fixes [problem].
  Future<void> openSettingsFor(LocationProblem problem) async {
    if (problem == LocationProblem.serviceDisabled) {
      await Geolocator.openLocationSettings();
    } else {
      await Geolocator.openAppSettings();
    }
  }
}

/// Always returns the same location instead of the device's (developer option).
class FixedLocationService extends LocationService {
  const FixedLocationService(this.location);

  final Coordinates location;

  @override
  Future<Coordinates> currentLocation() async => location;
}
