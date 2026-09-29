import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/session.dart';

/// Keeps the login in the platform's secure storage (Keychain / Android Keystore).
class SessionStorage {
  SessionStorage([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  static const _key = 'session';

  final FlutterSecureStorage _storage;

  /// The stored session, or null when there is none or it has expired.
  Future<Session?> read() async {
    try {
      final json = await _storage.read(key: _key);
      if (json == null) return null;
      final session = Session.fromJson(jsonDecode(json) as Map<String, dynamic>);
      if (session.isExpired) {
        await clear();
        return null;
      }
      return session;
    } catch (e) {
      // Unreadable data (e.g. restored from a backup onto another device): start signed out.
      debugPrint('Could not read the stored session: $e');
      await clear();
      return null;
    }
  }

  Future<void> write(Session session) =>
      _storage.write(key: _key, value: jsonEncode(session.toJson()));

  Future<void> clear() async {
    try {
      await _storage.delete(key: _key);
    } catch (e) {
      debugPrint('Could not clear the stored session: $e');
    }
  }
}
