import 'package:flutter/foundation.dart';

import 'mode.dart';
import 'profile.dart';

/// Another user and their profile in one mode. Common shape of nearby results and contacts.
@immutable
sealed class Person {
  const Person({required this.userId, required this.profile});

  final String userId;
  final Profile profile;

  Mode get mode => profile.mode;
}

/// A user found by a nearby search.
@immutable
final class NearbyUser extends Person {
  const NearbyUser({
    required super.userId,
    required super.profile,
    required this.distanceKm,
    this.latitude,
    this.longitude,
  });

  factory NearbyUser.fromJson(Mode mode, Map<String, dynamic> json) => NearbyUser(
    userId: json['userId'] as String,
    distanceKm: (json['distanceKm'] as num).toDouble(),
    latitude: (json['latitude'] as num?)?.toDouble(),
    longitude: (json['longitude'] as num?)?.toDouble(),
    profile: Profile.fromJson(mode, json['profile'] as Map<String, dynamic>),
  );

  final double distanceKm;

  /// Where the user is visible. Null from servers older than the map feature.
  final double? latitude;
  final double? longitude;
}

/// A saved contact. The profile is always the contact's current one.
@immutable
final class Contact extends Person {
  const Contact({required super.userId, required super.profile, required this.savedAt});

  factory Contact.fromJson(Mode mode, Map<String, dynamic> json) => Contact(
    userId: json['userId'] as String,
    savedAt: DateTime.parse(json['savedAt'] as String),
    profile: Profile.fromJson(mode, json['profile'] as Map<String, dynamic>),
  );

  final DateTime savedAt;
}

/// The user is visible in [mode] at the given location.
@immutable
final class VisibilityStatus {
  const VisibilityStatus({
    required this.mode,
    required this.latitude,
    required this.longitude,
    required this.updatedAt,
  });

  factory VisibilityStatus.fromJson(Map<String, dynamic> json) => VisibilityStatus(
    mode: Mode.fromApi(json['mode'] as String),
    latitude: (json['latitude'] as num).toDouble(),
    longitude: (json['longitude'] as num).toDouble(),
    updatedAt: DateTime.parse(json['updatedAt'] as String),
  );

  final Mode mode;
  final double latitude;
  final double longitude;
  final DateTime updatedAt;
}
