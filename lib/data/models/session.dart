import 'package:flutter/foundation.dart';

/// The logged in account (`GET /api/v1/me`).
@immutable
final class Account {
  const Account({required this.id, required this.username, required this.createdAt});

  factory Account.fromJson(Map<String, dynamic> json) => Account(
    id: json['id'] as String,
    username: json['username'] as String,
    createdAt: DateTime.parse(json['createdAt'] as String),
  );

  final String id;
  final String username;
  final DateTime createdAt;

  Map<String, Object?> toJson() => {
    'id': id,
    'username': username,
    'createdAt': createdAt.toIso8601String(),
  };
}

/// A login: the access token and the account it belongs to. There are no refresh tokens; when the
/// token expires the user has to log in again.
@immutable
final class Session {
  const Session({required this.accessToken, required this.expiresAt, required this.account});

  /// Parses the login/registration response.
  factory Session.fromJson(Map<String, dynamic> json) => Session(
    accessToken: json['accessToken'] as String,
    expiresAt: DateTime.parse(json['expiresAt'] as String),
    account: Account.fromJson(json['user'] as Map<String, dynamic>),
  );

  final String accessToken;
  final DateTime expiresAt;
  final Account account;

  bool get isExpired => !DateTime.now().isBefore(expiresAt);

  /// Same shape as the login response, so it can be stored and read back with [Session.fromJson].
  Map<String, Object?> toJson() => {
    'accessToken': accessToken,
    'expiresAt': expiresAt.toIso8601String(),
    'user': account.toJson(),
  };
}
