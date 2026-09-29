import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

/// One problem of a `422 validation_error`.
@immutable
final class ValidationIssue {
  const ValidationIssue({required this.location, required this.message, this.type});

  factory ValidationIssue.fromJson(Map<String, dynamic> json) => ValidationIssue(
    location: [for (final part in json['location'] as List? ?? const []) part.toString()],
    message: json['message'] as String? ?? '',
    type: json['type'] as String?,
  );

  /// e.g. `["body", "password"]` or `["query", "radiusKm"]`.
  final List<String> location;
  final String message;
  final String? type;

  String? get field => location.length > 1 ? location.last : null;
}

/// An error returned by the API (`{"code", "message", "details"?}`) or a network failure.
///
/// Branch on [code], which is stable. [message] comes from the server and is meant for humans;
/// [userMessage] is what the app shows.
final class ApiException implements Exception {
  const ApiException({
    required this.code,
    required this.message,
    this.statusCode,
    this.details = const [],
  });

  /// Converts any error thrown by Dio into an [ApiException].
  factory ApiException.fromDio(DioException error) {
    final response = error.response;
    if (response == null) {
      return switch (error.type) {
        DioExceptionType.connectionTimeout ||
        DioExceptionType.sendTimeout ||
        DioExceptionType.receiveTimeout => const ApiException(
          code: timeout,
          message: 'The server did not respond in time',
        ),
        DioExceptionType.cancel => const ApiException(code: cancelled, message: 'Cancelled'),
        _ => ApiException(code: network, message: error.message ?? 'Network error'),
      };
    }
    final data = response.data;
    if (data is Map && data['code'] is String) {
      return ApiException(
        code: data['code'] as String,
        message: data['message'] as String? ?? '',
        statusCode: response.statusCode,
        details: [
          for (final item in data['details'] as List? ?? const [])
            if (item is Map<String, dynamic>) ValidationIssue.fromJson(item),
        ],
      );
    }
    return ApiException(
      code: unknown,
      message: 'Unexpected response (HTTP ${response.statusCode})',
      statusCode: response.statusCode,
    );
  }

  // Codes produced by the client itself.
  static const network = 'network_error';
  static const timeout = 'timeout';
  static const cancelled = 'cancelled';
  static const unknown = 'unknown_error';

  // Codes returned by the server.
  static const notAuthenticated = 'not_authenticated';
  static const invalidCredentials = 'invalid_credentials';
  static const userNotFound = 'user_not_found';
  static const pictureNotFound = 'picture_not_found';
  static const usernameTaken = 'username_taken';
  static const notVisible = 'not_visible';
  static const pictureTooLarge = 'picture_too_large';
  static const unsupportedPictureType = 'unsupported_picture_type';
  static const validationError = 'validation_error';
  static const cannotSaveSelf = 'cannot_save_self';

  final String code;
  final String message;
  final int? statusCode;
  final List<ValidationIssue> details;

  bool get isConnectivityProblem => code == network || code == timeout;

  /// Validation message of a request body field, e.g. `fieldError('email')`.
  String? fieldError(String field) {
    for (final issue in details) {
      if (issue.field == field) return issue.message;
    }
    return null;
  }

  /// A message suitable for showing to the user.
  String get userMessage => switch (code) {
    network => 'Cannot reach the server. Check your connection and the server address.',
    timeout => 'The server is taking too long to respond. Please try again.',
    notAuthenticated => 'Your session has expired. Please sign in again.',
    invalidCredentials => 'Wrong username or password.',
    usernameTaken => 'This username is already taken.',
    userNotFound => 'This user no longer exists.',
    notVisible => 'You need to be visible to see who is around.',
    pictureTooLarge => 'The picture is too large.',
    unsupportedPictureType => 'Pictures must be JPEG, PNG or WebP.',
    cannotSaveSelf => 'You cannot save yourself as a contact.',
    validationError when details.isNotEmpty => _validationSummary(),
    _ => message.isNotEmpty ? message : 'Something went wrong. Please try again.',
  };

  String _validationSummary() {
    final issue = details.first;
    return issue.field == null ? issue.message : '${issue.field}: ${issue.message}';
  }

  @override
  String toString() => 'ApiException($statusCode $code: $message)';
}
