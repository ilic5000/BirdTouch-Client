import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/api/birdtouch_api.dart';
import '../data/models/mode.dart';
import '../data/models/session.dart';
import '../data/storage/session_storage.dart';
import 'settings.dart';

final sessionStorageProvider = Provider<SessionStorage>((ref) => SessionStorage());

/// The session read from secure storage in `main()`, before the app starts.
final initialSessionProvider = Provider<Session?>((ref) => null);

/// HTTP client for the configured server. It adds the access token to every request, and signs
/// the user out when the server says the token is no longer valid.
final dioProvider = Provider<Dio>((ref) {
  final baseUrl = ref.watch(settingsProvider.select((s) => s.serverUrl));
  final dio = Dio(
    BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 20),
      sendTimeout: const Duration(seconds: 60),
      contentType: Headers.jsonContentType,
      responseType: ResponseType.json,
    ),
  );
  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (options, handler) {
        final token = ref.read(sessionProvider)?.accessToken;
        if (token != null && options.extra['anonymous'] != true) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      onError: (error, handler) {
        final sentAuth = error.requestOptions.headers['Authorization'];
        if (error.response?.statusCode == 401 && sentAuth != null) {
          // Ignore late failures of requests made with an older token.
          final current = ref.read(sessionProvider)?.accessToken;
          if (sentAuth == 'Bearer $current') {
            ref.read(sessionProvider.notifier).expire();
          }
        }
        handler.next(error);
      },
    ),
  );
  if (kDebugMode) {
    // One line per request in the debug console, e.g. "[api] 200 GET /api/v1/me".
    dio.interceptors.add(
      InterceptorsWrapper(
        onResponse: (response, handler) {
          final request = response.requestOptions;
          debugPrint('[api] ${response.statusCode} ${request.method} ${request.uri.path}');
          handler.next(response);
        },
        onError: (error, handler) {
          final request = error.requestOptions;
          final status = error.response?.statusCode ?? error.type.name;
          debugPrint(
            '[api] $status ${request.method} ${request.uri.path} ${error.response?.data ?? ''}',
          );
          handler.next(error);
        },
      ),
    );
  }
  ref.onDispose(dio.close);
  return dio;
});

final apiProvider = Provider<BirdTouchApi>((ref) => BirdTouchApi(ref.watch(dioProvider)));

/// Headers needed to download profile pictures.
final pictureHeadersProvider = Provider<Map<String, String>>((ref) {
  final token = ref.watch(sessionProvider.select((s) => s?.accessToken));
  return token == null ? const {} : {'Authorization': 'Bearer $token'};
});

/// Id of the logged in user, or null. Watch this to reload per-user data after a (re)login.
final currentUserIdProvider = Provider<String?>(
  (ref) => ref.watch(sessionProvider.select((s) => s?.account.id)),
);

final sessionProvider = NotifierProvider<SessionController, Session?>(SessionController.new);

/// The login. `null` means signed out.
class SessionController extends Notifier<Session?> {
  /// Set when the session ended because the server rejected the token, so the welcome screen can
  /// tell the user why they were signed out.
  bool expiredByServer = false;

  @override
  Session? build() => ref.read(initialSessionProvider);

  BirdTouchApi get _api => ref.read(apiProvider);

  Future<void> signIn({required String username, required String password}) async {
    final session = await _api.login(username: username.trim(), password: password);
    await _start(session);
  }

  Future<void> register({
    required String username,
    required String password,
    String? firstName,
    String? lastName,
  }) async {
    final session = await _api.register(
      username: username.trim(),
      password: password,
      firstName: firstName,
      lastName: lastName,
    );
    await _start(session);
  }

  /// Signs out. The user is hidden first (best effort), otherwise others would keep seeing them
  /// until the server hides inactive users.
  Future<void> signOut({bool hide = true}) async {
    if (hide && state != null) {
      await Future.wait([
        for (final mode in Mode.values)
          _api.hide(mode).timeout(const Duration(seconds: 4)).catchError((Object _) {}),
      ]);
    }
    await _end();
  }

  /// Called when the server answers `401 not_authenticated`.
  void expire() {
    if (state == null) return;
    expiredByServer = true;
    unawaited(_end());
  }

  Future<void> _start(Session session) async {
    await ref.read(sessionStorageProvider).write(session);
    expiredByServer = false;
    state = session;
  }

  Future<void> _end() async {
    state = null;
    await ref.read(sessionStorageProvider).clear();
  }
}
