// End-to-end test of the API client against a running BirdTouch server.
//
// Skipped unless BIRDTOUCH_E2E_URL is set, e.g.:
//   BIRDTOUCH_E2E_URL=http://localhost:4050 flutter test test/e2e
//
// It creates two throwaway accounts and deletes them at the end.
@Tags(['e2e'])
library;

import 'dart:io';
import 'dart:typed_data';

import 'package:birdtouch/data/api/api_exception.dart';
import 'package:birdtouch/data/api/birdtouch_api.dart';
import 'package:birdtouch/data/models/mode.dart';
import 'package:birdtouch/data/models/profile.dart';
import 'package:birdtouch/data/models/session.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

final _serverUrl = Platform.environment['BIRDTOUCH_E2E_URL'];

/// An API client logged in as one user.
class _User {
  _User(String baseUrl) : dio = Dio(BaseOptions(baseUrl: baseUrl)) {
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          if (session != null && options.extra['anonymous'] != true) {
            options.headers['Authorization'] = 'Bearer ${session!.accessToken}';
          }
          handler.next(options);
        },
      ),
    );
    api = BirdTouchApi(dio);
  }

  final Dio dio;
  late final BirdTouchApi api;
  Session? session;
}

Matcher _apiError(String code) => throwsA(isA<ApiException>().having((e) => e.code, 'code', code));

void main() {
  // Note: no TestWidgetsFlutterBinding here, it would block real HTTP requests.
  final skip = _serverUrl == null ? 'Set BIRDTOUCH_E2E_URL to run the end-to-end tests' : null;
  final suffix = DateTime.now().millisecondsSinceEpoch;
  late _User alice;
  late _User bob;

  setUpAll(() {
    if (skip != null) return;
    alice = _User(_serverUrl!);
    bob = _User(_serverUrl!);
  });

  tearDownAll(() async {
    if (skip != null) return;
    for (final user in [alice, bob]) {
      if (user.session != null) await user.api.deleteAccount().catchError((Object _) {});
    }
  });

  test('the server is reachable', () async {
    await BirdTouchApi.checkServer(_serverUrl!);
  }, skip: skip);

  test('full flow: accounts, profiles, pictures, visibility, nearby, contacts', () async {
    // Accounts
    alice.session = await alice.api.register(
      username: 'e2e.alice.$suffix',
      password: 'secret123',
      firstName: 'Alice',
      lastName: 'E2E',
    );
    bob.session = await bob.api.register(
      username: 'e2e.bob.$suffix',
      password: 'secret123',
      firstName: 'Bob',
    );
    expect(await alice.api.isUsernameAvailable('E2E.ALICE.$suffix'), isFalse);
    expect(await alice.api.isUsernameAvailable('e2e.nobody.$suffix'), isTrue);
    await expectLater(
      bob.api.register(username: 'e2e.alice.$suffix', password: 'secret123'),
      _apiError(ApiException.usernameTaken),
    );
    await expectLater(
      alice.api.login(username: 'e2e.alice.$suffix', password: 'wrong-password'),
      _apiError(ApiException.invalidCredentials),
    );
    alice.session = await alice.api.login(username: 'e2e.alice.$suffix', password: 'secret123');
    expect((await alice.api.me()).username, 'e2e.alice.$suffix');

    // Profiles: partial updates, clearing with null, validation errors
    final initial = await alice.api.profile(Mode.private) as PrivateProfile;
    expect(initial.displayName, 'Alice E2E');
    expect(initial.isDiscoverable, isFalse);
    final edited = PrivateProfile(
      firstName: 'Alice',
      lastName: 'E2E',
      email: 'alice@example.com',
      dateOfBirth: DateTime.utc(1990, 5, 17),
      description: 'Hello',
    );
    var updated = await alice.api.updateProfile(Mode.private, initial.changesTo(edited));
    expect(updated.isDiscoverable, isTrue);
    expect((updated as PrivateProfile).dateOfBirth, DateTime.utc(1990, 5, 17));
    updated = await alice.api.updateProfile(Mode.private, {'description': null});
    expect(updated.description, isNull);
    expect(updated.email, 'alice@example.com');
    try {
      await alice.api.updateProfile(Mode.private, {'email': 'not-an-email'});
      fail('expected a validation error');
    } on ApiException catch (e) {
      expect(e.code, ApiException.validationError);
      expect(e.fieldError('email'), isNotNull);
    }
    await bob.api.updateProfile(Mode.private, {'phoneNumber': '+381 60 000 0000'});
    await bob.api.updateProfile(Mode.business, {
      'companyName': 'Bob d.o.o.',
      'email': 'office@bob.example.com',
    });

    // Pictures: raw upload, authenticated download
    final png = Uint8List.fromList([
      0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, // signature is what counts
    ]);
    await alice.api.uploadPicture(Mode.private, png, contentType: 'image/png');
    final withPicture = await alice.api.profile(Mode.private);
    expect(withPicture.pictureUrl, isNotNull);
    final download = await bob.dio.get<List<int>>(
      withPicture.pictureUrl!,
      options: Options(responseType: ResponseType.bytes),
    );
    expect(download.data, png);
    expect(download.headers.value('content-type'), 'image/png');
    await expectLater(
      alice.api.uploadPicture(
        Mode.private,
        Uint8List.fromList('GIF89a'.codeUnits),
        contentType: 'image/gif',
      ),
      _apiError(ApiException.unsupportedPictureType),
    );
    await alice.api.deletePicture(Mode.private);
    expect((await alice.api.profile(Mode.private)).pictureUrl, isNull);

    // Visibility and nearby
    await expectLater(
      alice.api.nearby(Mode.private, radiusKm: 1),
      _apiError(ApiException.notVisible),
    );
    await alice.api.setVisibility(Mode.private, latitude: 44.8125, longitude: 20.4612);
    await bob.api.setVisibility(Mode.private, latitude: 44.8130, longitude: 20.4615);
    await bob.api.setVisibility(Mode.business, latitude: 44.8130, longitude: 20.4615);
    final visibility = await bob.api.visibility();
    expect(visibility.map((v) => v.mode), unorderedEquals(Mode.values));

    final nearby = await alice.api.nearby(Mode.private, radiusKm: 0.5, limit: 200);
    final foundBob = nearby.where((n) => n.userId == bob.session!.account.id).single;
    expect(foundBob.profile.displayName, 'Bob');
    expect(foundBob.distanceKm, inInclusiveRange(0.01, 0.2));
    expect((foundBob.latitude, foundBob.longitude), (44.8130, 20.4615));
    expect(nearby.any((n) => n.userId == alice.session!.account.id), isFalse);
    final everywhere = await alice.api.nearby(Mode.private, limit: 200); // no distance limit
    expect(everywhere.any((n) => n.userId == bob.session!.account.id), isTrue);

    // Contacts
    await alice.api.saveContact(Mode.private, foundBob.userId);
    await alice.api.saveContact(Mode.private, foundBob.userId); // saving twice is fine
    await alice.api.saveContact(Mode.business, foundBob.userId);
    final contacts = await alice.api.contacts(Mode.private);
    expect(contacts.single.userId, foundBob.userId);
    expect((await alice.api.contacts(Mode.business)).single.profile.displayName, 'Bob d.o.o.');
    await expectLater(
      alice.api.saveContact(Mode.private, alice.session!.account.id),
      _apiError(ApiException.cannotSaveSelf),
    );
    await alice.api.removeContact(Mode.private, foundBob.userId);
    expect(await alice.api.contacts(Mode.private), isEmpty);

    // Hiding
    await bob.api.hide(Mode.private);
    final afterHide = await alice.api.nearby(Mode.private, radiusKm: 0.5, limit: 200);
    expect(afterHide.any((n) => n.userId == bob.session!.account.id), isFalse);

    // Deleting an account removes it from other users' contacts and invalidates its token
    await bob.api.deleteAccount();
    expect(await alice.api.contacts(Mode.business), isEmpty);
    await expectLater(bob.api.me(), _apiError(ApiException.notAuthenticated));
    bob.session = null;
  }, skip: skip);
}
