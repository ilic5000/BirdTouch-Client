import 'dart:typed_data';

import 'package:birdtouch/data/api/api_exception.dart';
import 'package:birdtouch/data/api/birdtouch_api.dart';
import 'package:birdtouch/data/models/mode.dart';
import 'package:birdtouch/data/models/people.dart';
import 'package:birdtouch/data/models/profile.dart';
import 'package:birdtouch/data/models/session.dart';
import 'package:birdtouch/state/people.dart';
import 'package:birdtouch/state/session.dart';
import 'package:birdtouch/state/settings.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'helpers.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late FakeAdapter adapter;
  late MemorySessionStorage storage;
  late ProviderContainer container;
  late BirdTouchApi api;

  Future<void> setUpContainer({Session? session}) async {
    SharedPreferences.setMockInitialValues({'server_url': 'http://test.local:4050'});
    final prefs = await SharedPreferences.getInstance();
    storage = MemorySessionStorage()..stored = session;
    container = ProviderContainer(
      retry: (_, _) => null,
      overrides: [
        sharedPreferencesProvider.overrideWithValue(prefs),
        sessionStorageProvider.overrideWithValue(storage),
        initialSessionProvider.overrideWithValue(session),
      ],
    );
    adapter = FakeAdapter((_) => noContent());
    container.read(dioProvider).httpClientAdapter = adapter;
    api = container.read(apiProvider);
  }

  final signedIn = Session.fromJson(loginJson(token: 'abc'));

  tearDown(() => container.dispose());

  group('requests', () {
    setUp(() => setUpContainer(session: signedIn));

    test('login sends camelCase credentials without a token', () async {
      adapter.handler = (_) => jsonResponse(200, loginJson(token: 'new'));
      final session = await api.login(username: 'ana.anic', password: 'secret123');

      final request = adapter.requests.single;
      expect(request.method, 'POST');
      expect(request.options.uri.toString(), 'http://test.local:4050/api/v1/auth/login');
      expect(decodeBody(request), {'username': 'ana.anic', 'password': 'secret123'});
      expect(request.options.headers['Authorization'], isNull);
      expect(session.accessToken, 'new');
    });

    test('register leaves out blank names', () async {
      adapter.handler = (_) => jsonResponse(201, loginJson());
      await api.register(username: 'ana', password: 'secret123', firstName: ' Ana ', lastName: '');
      expect(decodeBody(adapter.requests.single), {
        'username': 'ana',
        'password': 'secret123',
        'firstName': 'Ana',
      });
    });

    test('authenticated requests carry the bearer token', () async {
      adapter.handler = (_) => jsonResponse(200, privateProfileJson());
      final profile = await api.profile(Mode.private);
      expect(adapter.requests.single.path, '/api/v1/me/private-profile');
      expect(adapter.requests.single.options.headers['Authorization'], 'Bearer abc');
      expect(profile, isA<PrivateProfile>());
    });

    test('profile updates are PATCHes with only the changes', () async {
      adapter.handler = (_) => jsonResponse(200, businessProfileJson());
      await api.updateProfile(Mode.business, {'website': null, 'email': 'x@y.rs'});
      final request = adapter.requests.single;
      expect(request.method, 'PATCH');
      expect(request.path, '/api/v1/me/business-profile');
      expect(decodeBody(request), {'website': null, 'email': 'x@y.rs'});
    });

    test('pictures are uploaded as raw bytes', () async {
      final bytes = Uint8List.fromList([0xFF, 0xD8, 0xFF, 1, 2, 3]);
      await api.uploadPicture(Mode.private, bytes, contentType: 'image/jpeg');
      final request = adapter.requests.single;
      expect(request.method, 'PUT');
      expect(request.path, '/api/v1/me/pictures/private');
      expect(request.options.contentType, 'image/jpeg');
      expect(request.body, bytes);
    });

    test('visibility and nearby', () async {
      adapter.handler = (r) => switch (r.path) {
        '/api/v1/me/visibility/business' => jsonResponse(200, {
          'mode': 'business',
          'latitude': 44.81,
          'longitude': 20.46,
          'updatedAt': '2026-09-28T18:22:34Z',
        }),
        _ => jsonResponse(200, [
          {'userId': 'u2', 'distanceKm': 0.3, 'profile': businessProfileJson()},
        ]),
      };
      await api.setVisibility(Mode.business, latitude: 44.81, longitude: 20.46);
      final nearby = await api.nearby(Mode.business, radiusKm: 0.5, limit: 100);

      expect(decodeBody(adapter.requests.first), {'latitude': 44.81, 'longitude': 20.46});
      final search = adapter.requests.last;
      expect(search.path, '/api/v1/nearby/business');
      expect(search.options.uri.query, 'radiusKm=0.5&limit=100');
      expect(nearby.single.userId, 'u2');

      await api.nearby(Mode.business);
      expect(adapter.requests.last.options.uri.query, isEmpty, reason: 'no radius: no limit');
    });

    test('contacts are saved and removed one by one', () async {
      await api.saveContact(Mode.private, 'u2');
      await api.removeContact(Mode.business, 'u3');
      expect(adapter.requests.map((r) => '${r.method} ${r.path}'), [
        'PUT /api/v1/me/contacts/private/u2',
        'DELETE /api/v1/me/contacts/business/u3',
      ]);
    });

    test('picture URLs are resolved against the server', () {
      expect(
        api.resolveUrl('/api/v1/users/u/pictures/private?v=1'),
        'http://test.local:4050/api/v1/users/u/pictures/private?v=1',
      );
    });
  });

  group('errors', () {
    setUp(() => setUpContainer(session: signedIn));

    test('API errors become ApiException with the stable code', () async {
      adapter.handler = (_) => errorResponse(409, 'username_taken');
      await expectLater(
        api.register(username: 'ana', password: 'secret123'),
        throwsA(
          isA<ApiException>()
              .having((e) => e.code, 'code', ApiException.usernameTaken)
              .having((e) => e.statusCode, 'status', 409),
        ),
      );
    });

    test('validation errors expose the field messages', () async {
      adapter.handler = (_) => errorResponse(422, 'validation_error', [
        {
          'location': ['body', 'email'],
          'message': 'value is not a valid email address',
          'type': 'value_error',
        },
      ]);
      try {
        await api.updateProfile(Mode.private, {'email': 'nope'});
        fail('should throw');
      } on ApiException catch (e) {
        expect(e.fieldError('email'), 'value is not a valid email address');
        expect(e.userMessage, contains('email'));
      }
    });

    test('network failures are reported as connectivity problems', () async {
      adapter.handler = (r) =>
          throw DioException.connectionError(requestOptions: r.options, reason: 'refused');
      await expectLater(
        api.me(),
        throwsA(isA<ApiException>().having((e) => e.isConnectivityProblem, 'connectivity', true)),
      );
    });

    test('401 not_authenticated signs the user out', () async {
      adapter.handler = (_) => errorResponse(401, 'not_authenticated');
      expect(container.read(sessionProvider), isNotNull);
      await expectLater(api.me(), throwsA(isA<ApiException>()));
      expect(container.read(sessionProvider), isNull);
      expect(container.read(sessionProvider.notifier).expiredByServer, isTrue);
      await Future<void>.delayed(Duration.zero);
      expect(storage.stored, isNull);
    });

    test('a wrong password does not sign anybody out', () async {
      adapter.handler = (_) => errorResponse(401, 'invalid_credentials');
      await expectLater(api.login(username: 'a', password: 'b'), throwsA(isA<ApiException>()));
      expect(container.read(sessionProvider), isNotNull);
    });
  });

  group('state', () {
    test('signing in stores the session; signing out hides the user and clears it', () async {
      await setUpContainer();
      adapter.handler = (r) =>
          r.path.endsWith('/login') ? jsonResponse(200, loginJson()) : noContent();

      await container.read(sessionProvider.notifier).signIn(username: ' ana.anic ', password: 'pw');
      expect(decodeBody(adapter.requests.single), containsPair('username', 'ana.anic'));
      expect(container.read(currentUserIdProvider), 'user-1');
      expect(storage.stored?.accessToken, 'token-1');

      await container.read(sessionProvider.notifier).signOut();
      expect(
        adapter.requests.where((r) => r.method == 'DELETE').map((r) => r.path),
        unorderedEquals(['/api/v1/me/visibility/private', '/api/v1/me/visibility/business']),
      );
      expect(container.read(sessionProvider), isNull);
      expect(storage.stored, isNull);
    });

    test('saving a contact shows it at once and rolls back when the server refuses', () async {
      await setUpContainer(session: signedIn);
      adapter.handler = (r) =>
          r.method == 'GET' ? jsonResponse(200, <Object>[]) : errorResponse(404, 'user_not_found');
      final contacts = contactsProvider(Mode.private);
      container.listen(contacts, (_, _) {});
      await container.read(contacts.future);

      final person = NearbyUser(
        userId: 'u2',
        distanceKm: 0.1,
        profile: PrivateProfile.fromJson(privateProfileJson()),
      );
      final saving = container.read(contacts.notifier).save(person);
      expect(container.read(savedContactIdsProvider(Mode.private)), {'u2'});
      await expectLater(saving, throwsA(isA<ApiException>()));
      expect(container.read(savedContactIdsProvider(Mode.private)), isEmpty);
    });

    test('settings keep the radius within the allowed steps', () async {
      await setUpContainer();
      final settings = container.read(settingsProvider.notifier);
      await settings.setDeveloperMode(true);
      await settings.setRadius(500000);
      expect(container.read(settingsProvider).radiusKm, 500);
      await settings.setRadius(unlimitedRadiusMeters);
      expect(container.read(settingsProvider).radiusKm, isNull);
      await settings.setFakeLocation((latitude: 45.26, longitude: 19.83));
      expect(container.read(settingsProvider).effectiveFakeLocation, isNotNull);
      await settings.setDeveloperMode(false);
      expect(container.read(settingsProvider).radiusMeters, standardRadiusSteps.last);
      // Kept, but only used with the developer options on.
      expect(container.read(settingsProvider).fakeLocation, isNotNull);
      expect(container.read(settingsProvider).effectiveFakeLocation, isNull);
    });
  });
}
