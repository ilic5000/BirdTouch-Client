import 'dart:convert';
import 'dart:typed_data';

import 'package:birdtouch/data/models/session.dart';
import 'package:birdtouch/data/storage/session_storage.dart';
import 'package:dio/dio.dart';

/// A request seen by [FakeAdapter].
typedef RecordedRequest = ({String method, String path, RequestOptions options, List<int> body});

/// Dio adapter that answers requests with [handler] instead of the network, and records them.
class FakeAdapter implements HttpClientAdapter {
  FakeAdapter(this.handler);

  ResponseBody Function(RecordedRequest request) handler;
  final requests = <RecordedRequest>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    final body = requestStream == null
        ? <int>[]
        : await requestStream.expand((chunk) => chunk).toList();
    final request = (method: options.method, path: options.uri.path, options: options, body: body);
    requests.add(request);
    return handler(request);
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody jsonResponse(int status, Object? body) => ResponseBody.fromString(
  jsonEncode(body),
  status,
  headers: {
    Headers.contentTypeHeader: [Headers.jsonContentType],
  },
);

ResponseBody noContent() => ResponseBody.fromString('', 204);

ResponseBody errorResponse(int status, String code, [List<Map<String, Object?>>? details]) =>
    jsonResponse(status, {
      'code': code,
      'message': 'Server message for $code',
      'details': ?details,
    });

Object? decodeBody(RecordedRequest request) => jsonDecode(utf8.decode(request.body));

/// Session storage kept in memory (the real one uses the platform's secure storage).
class MemorySessionStorage extends SessionStorage {
  Session? stored;

  @override
  Future<Session?> read() async => stored;

  @override
  Future<void> write(Session session) async => stored = session;

  @override
  Future<void> clear() async => stored = null;
}

Map<String, Object?> loginJson({String token = 'token-1', String id = 'user-1'}) => {
  'accessToken': token,
  'tokenType': 'bearer',
  'expiresAt': DateTime.now().add(const Duration(days: 30)).toUtc().toIso8601String(),
  'user': {'id': id, 'username': 'ana.anic', 'createdAt': '2026-09-28T18:22:34.446920Z'},
};

Map<String, Object?> privateProfileJson({
  String? firstName = 'Ana',
  String? lastName = 'Anić',
  String? email = 'ana@example.com',
  String? pictureUrl,
}) => {
  'firstName': firstName,
  'lastName': lastName,
  'email': email,
  'phoneNumber': '+381 60 123 4567',
  'dateOfBirth': '1990-05-17',
  'address': 'Knez Mihailova 1, Beograd',
  'description': 'Hi!',
  'facebookUrl': 'https://facebook.com/ana',
  'twitterUrl': null,
  'linkedinUrl': null,
  'pictureUrl': pictureUrl,
  'updatedAt': '2026-09-28T18:22:34.4Z',
};

Map<String, Object?> businessProfileJson({String? companyName = 'Ana d.o.o.'}) => {
  'companyName': companyName,
  'email': 'office@ana.rs',
  'phoneNumber': '011 123 456',
  'website': 'https://ana.rs',
  'address': 'Bulevar 1, Beograd',
  'description': 'We make things.',
  'pictureUrl': null,
  'updatedAt': '2026-09-28T18:22:34.4Z',
};
