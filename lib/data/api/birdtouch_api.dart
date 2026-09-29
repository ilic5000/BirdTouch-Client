import 'dart:typed_data';

import 'package:dio/dio.dart';

import '../models/mode.dart';
import '../models/people.dart';
import '../models/profile.dart';
import '../models/session.dart';
import 'api_exception.dart';

/// Typed client for the BirdTouch REST API (`/api/v1`).
///
/// Every method throws [ApiException] on failure. The Dio instance is expected to add the
/// `Authorization` header (see `dioProvider`).
class BirdTouchApi {
  BirdTouchApi(this._dio);

  final Dio _dio;

  /// Request option that tells the auth interceptor not to send the token.
  static final _anonymous = Options(extra: {'anonymous': true});

  /// Checks that a BirdTouch server answers at [baseUrl] (`GET /health`).
  static Future<void> checkServer(String baseUrl) async {
    final dio = Dio(
      BaseOptions(
        baseUrl: baseUrl,
        connectTimeout: const Duration(seconds: 8),
        receiveTimeout: const Duration(seconds: 8),
      ),
    );
    try {
      final response = await dio.get<dynamic>('/health');
      final data = response.data;
      if (data is! Map || data['status'] != 'ok') {
        throw const ApiException(
          code: ApiException.unknown,
          message: 'This does not look like a BirdTouch server',
        );
      }
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    } finally {
      dio.close();
    }
  }

  /// Absolute URL of a relative API URL, such as a profile's `pictureUrl`.
  String resolveUrl(String relative) =>
      Uri.parse(_dio.options.baseUrl).resolve(relative).toString();

  // --- Auth ---

  Future<Session> register({
    required String username,
    required String password,
    String? firstName,
    String? lastName,
  }) => _send(
    () => _dio.post<dynamic>(
      '/api/v1/auth/register',
      data: {
        'username': username,
        'password': password,
        'firstName': ?_blankToNull(firstName),
        'lastName': ?_blankToNull(lastName),
      },
      options: _anonymous,
    ),
    (data) => Session.fromJson(data as Map<String, dynamic>),
  );

  Future<Session> login({required String username, required String password}) => _send(
    () => _dio.post<dynamic>(
      '/api/v1/auth/login',
      data: {'username': username, 'password': password},
      options: _anonymous,
    ),
    (data) => Session.fromJson(data as Map<String, dynamic>),
  );

  Future<bool> isUsernameAvailable(String username) => _send(
    () => _dio.get<dynamic>(
      '/api/v1/auth/username-availability',
      queryParameters: {'username': username},
      options: _anonymous,
    ),
    (data) => (data as Map<String, dynamic>)['available'] as bool,
  );

  // --- Account ---

  Future<Account> me() => _send(
    () => _dio.get<dynamic>('/api/v1/me'),
    (data) => Account.fromJson(data as Map<String, dynamic>),
  );

  /// Permanently deletes the account and everything about it.
  Future<void> deleteAccount() => _send(() => _dio.delete<dynamic>('/api/v1/me'));

  // --- Profiles ---

  Future<Profile> profile(Mode mode) => _send(
    () => _dio.get<dynamic>(_profilePath(mode)),
    (data) => Profile.fromJson(mode, data as Map<String, dynamic>),
  );

  /// Changes only the fields in [changes]; a `null` value clears the field.
  Future<Profile> updateProfile(Mode mode, Map<String, Object?> changes) => _send(
    () => _dio.patch<dynamic>(_profilePath(mode), data: changes),
    (data) => Profile.fromJson(mode, data as Map<String, dynamic>),
  );

  /// Uploads the raw image bytes (JPEG, PNG or WebP, max 5 MB).
  Future<void> uploadPicture(Mode mode, Uint8List bytes, {required String contentType}) => _send(
    () => _dio.put<dynamic>(
      '/api/v1/me/pictures/${mode.apiName}',
      data: Stream.fromIterable([bytes]),
      options: Options(
        contentType: contentType,
        headers: {Headers.contentLengthHeader: bytes.length},
      ),
    ),
  );

  Future<void> deletePicture(Mode mode) =>
      _send(() => _dio.delete<dynamic>('/api/v1/me/pictures/${mode.apiName}'));

  // --- Visibility and nearby users ---

  /// The modes in which the user is currently visible.
  Future<List<VisibilityStatus>> visibility() => _send(
    () => _dio.get<dynamic>('/api/v1/me/visibility'),
    (data) => [
      for (final item in data as List) VisibilityStatus.fromJson(item as Map<String, dynamic>),
    ],
  );

  /// Makes the user visible in [mode] at the location, or updates the location.
  Future<VisibilityStatus> setVisibility(
    Mode mode, {
    required double latitude,
    required double longitude,
  }) => _send(
    () => _dio.put<dynamic>(
      '/api/v1/me/visibility/${mode.apiName}',
      data: {'latitude': latitude, 'longitude': longitude},
    ),
    (data) => VisibilityStatus.fromJson(data as Map<String, dynamic>),
  );

  Future<void> hide(Mode mode) =>
      _send(() => _dio.delete<dynamic>('/api/v1/me/visibility/${mode.apiName}'));

  /// Users visible in [mode] within [radiusKm] of the user's last location, nearest first.
  /// Without [radiusKm] there is no distance limit.
  /// Throws `not_visible` when the user isn't visible in [mode] themselves.
  Future<List<NearbyUser>> nearby(Mode mode, {double? radiusKm, int? limit}) => _send(
    () => _dio.get<dynamic>(
      '/api/v1/nearby/${mode.apiName}',
      // Dart formats doubles with a '.', whatever the phone's locale is.
      queryParameters: {'radiusKm': ?radiusKm, 'limit': ?limit},
    ),
    (data) => [
      for (final item in data as List) NearbyUser.fromJson(mode, item as Map<String, dynamic>),
    ],
  );

  // --- Contacts ---

  /// Saved contacts, oldest first.
  Future<List<Contact>> contacts(Mode mode) => _send(
    () => _dio.get<dynamic>('/api/v1/me/contacts/${mode.apiName}'),
    (data) => [
      for (final item in data as List) Contact.fromJson(mode, item as Map<String, dynamic>),
    ],
  );

  Future<void> saveContact(Mode mode, String userId) =>
      _send(() => _dio.put<dynamic>('/api/v1/me/contacts/${mode.apiName}/$userId'));

  Future<void> removeContact(Mode mode, String userId) =>
      _send(() => _dio.delete<dynamic>('/api/v1/me/contacts/${mode.apiName}/$userId'));

  // --- Helpers ---

  static String _profilePath(Mode mode) => switch (mode) {
    Mode.private => '/api/v1/me/private-profile',
    Mode.business => '/api/v1/me/business-profile',
  };

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return trimmed == null || trimmed.isEmpty ? null : trimmed;
  }

  Future<T> _send<T>(
    Future<Response<dynamic>> Function() request, [
    T Function(dynamic data)? parse,
  ]) async {
    try {
      final response = await request();
      return parse == null ? null as T : parse(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
