import 'package:flutter/foundation.dart';
import 'package:intl/intl.dart';

import 'mode.dart';

/// A private or business profile, as returned by the API.
///
/// Profiles have no id of their own: the owner is the logged in user, or the `userId` of the
/// nearby/contact item that wraps the profile.
@immutable
sealed class Profile {
  const Profile({
    this.email,
    this.phoneNumber,
    this.address,
    this.description,
    this.pictureUrl,
    this.updatedAt,
  });

  factory Profile.fromJson(Mode mode, Map<String, dynamic> json) => switch (mode) {
    Mode.private => PrivateProfile.fromJson(json),
    Mode.business => BusinessProfile.fromJson(json),
  };

  final String? email;
  final String? phoneNumber;
  final String? address;
  final String? description;

  /// Relative URL of the picture (needs the Authorization header), or null. It changes whenever
  /// the picture changes, so it can be used as a cache key.
  final String? pictureUrl;
  final DateTime? updatedAt;

  Mode get mode;

  /// Name to show for the profile: the full name, or the company name.
  String get displayName;

  /// Whether the server shows this profile to other users in nearby searches.
  bool get isDiscoverable => missingForDiscovery.isEmpty;

  /// What has to be filled in before other users can find this profile. Empty when discoverable.
  List<String> get missingForDiscovery;

  /// The editable fields, keyed by their JSON name.
  Map<String, Object?> toJson();

  /// Fields of [edited] that differ from this profile, for a partial `PATCH`.
  /// A field cleared by the user is included as `null`.
  Map<String, Object?> changesTo(Profile edited) {
    final before = toJson();
    final after = edited.toJson();
    return {
      for (final entry in after.entries)
        if (before[entry.key] != entry.value) entry.key: entry.value,
    };
  }
}

@immutable
final class PrivateProfile extends Profile {
  const PrivateProfile({
    this.firstName,
    this.lastName,
    super.email,
    super.phoneNumber,
    this.dateOfBirth,
    super.address,
    super.description,
    this.facebookUrl,
    this.twitterUrl,
    this.linkedinUrl,
    super.pictureUrl,
    super.updatedAt,
  });

  factory PrivateProfile.fromJson(Map<String, dynamic> json) => PrivateProfile(
    firstName: json['firstName'] as String?,
    lastName: json['lastName'] as String?,
    email: json['email'] as String?,
    phoneNumber: json['phoneNumber'] as String?,
    dateOfBirth: _parseDate(json['dateOfBirth'] as String?),
    address: json['address'] as String?,
    description: json['description'] as String?,
    facebookUrl: json['facebookUrl'] as String?,
    twitterUrl: json['twitterUrl'] as String?,
    linkedinUrl: json['linkedinUrl'] as String?,
    pictureUrl: json['pictureUrl'] as String?,
    updatedAt: _parseDateTime(json['updatedAt'] as String?),
  );

  final String? firstName;
  final String? lastName;

  /// Date of birth, as a date without time (UTC midnight).
  final DateTime? dateOfBirth;
  final String? facebookUrl;
  final String? twitterUrl;
  final String? linkedinUrl;

  @override
  Mode get mode => Mode.private;

  @override
  String get displayName => [firstName, lastName].whereType<String>().join(' ').trim();

  bool get hasName => (firstName ?? '').isNotEmpty || (lastName ?? '').isNotEmpty;

  // Mirrors the server: a name, plus at least one way to reach the person.
  @override
  List<String> get missingForDiscovery => [
    if (!hasName) 'your first or last name',
    if ([email, phoneNumber, facebookUrl, twitterUrl, linkedinUrl].every((v) => (v ?? '').isEmpty))
      'an email, phone number or social link',
  ];

  @override
  Map<String, Object?> toJson() => {
    'firstName': firstName,
    'lastName': lastName,
    'email': email,
    'phoneNumber': phoneNumber,
    'dateOfBirth': dateOfBirth == null ? null : formatApiDate(dateOfBirth!),
    'address': address,
    'description': description,
    'facebookUrl': facebookUrl,
    'twitterUrl': twitterUrl,
    'linkedinUrl': linkedinUrl,
  };

  @override
  bool operator ==(Object other) =>
      other is PrivateProfile &&
      mapEquals(other.toJson(), toJson()) &&
      other.pictureUrl == pictureUrl &&
      other.updatedAt == updatedAt;

  @override
  int get hashCode => Object.hash(Object.hashAll(toJson().values), pictureUrl, updatedAt);
}

@immutable
final class BusinessProfile extends Profile {
  const BusinessProfile({
    this.companyName,
    super.email,
    super.phoneNumber,
    this.website,
    super.address,
    super.description,
    super.pictureUrl,
    super.updatedAt,
  });

  factory BusinessProfile.fromJson(Map<String, dynamic> json) => BusinessProfile(
    companyName: json['companyName'] as String?,
    email: json['email'] as String?,
    phoneNumber: json['phoneNumber'] as String?,
    website: json['website'] as String?,
    address: json['address'] as String?,
    description: json['description'] as String?,
    pictureUrl: json['pictureUrl'] as String?,
    updatedAt: _parseDateTime(json['updatedAt'] as String?),
  );

  final String? companyName;
  final String? website;

  @override
  Mode get mode => Mode.business;

  @override
  String get displayName => companyName ?? '';

  // Mirrors the server: a company name and an email.
  @override
  List<String> get missingForDiscovery => [
    if ((companyName ?? '').isEmpty) 'the company name',
    if ((email ?? '').isEmpty) 'the company email',
  ];

  @override
  Map<String, Object?> toJson() => {
    'companyName': companyName,
    'email': email,
    'phoneNumber': phoneNumber,
    'website': website,
    'address': address,
    'description': description,
  };

  @override
  bool operator ==(Object other) =>
      other is BusinessProfile &&
      mapEquals(other.toJson(), toJson()) &&
      other.pictureUrl == pictureUrl &&
      other.updatedAt == updatedAt;

  @override
  int get hashCode => Object.hash(Object.hashAll(toJson().values), pictureUrl, updatedAt);
}

/// Formats a date the way the API expects it: `YYYY-MM-DD`.
String formatApiDate(DateTime date) => DateFormat('yyyy-MM-dd').format(date);

DateTime? _parseDate(String? value) {
  if (value == null) return null;
  final date = DateTime.parse(value);
  return DateTime.utc(date.year, date.month, date.day);
}

DateTime? _parseDateTime(String? value) => value == null ? null : DateTime.parse(value);
