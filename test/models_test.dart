import 'package:birdtouch/data/models/mode.dart';
import 'package:birdtouch/data/models/people.dart';
import 'package:birdtouch/data/models/profile.dart';
import 'package:birdtouch/data/models/session.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers.dart';

void main() {
  group('PrivateProfile', () {
    test('parses the API JSON', () {
      final p = PrivateProfile.fromJson(
        privateProfileJson(pictureUrl: '/api/v1/users/u/pictures/private?v=1'),
      );
      expect(p.displayName, 'Ana Anić');
      expect(p.dateOfBirth, DateTime.utc(1990, 5, 17));
      expect(p.pictureUrl, '/api/v1/users/u/pictures/private?v=1');
      expect(p.mode, Mode.private);
      expect(p.toJson()['dateOfBirth'], '1990-05-17');
    });

    test('discoverable needs a name and a way to reach the person', () {
      expect(const PrivateProfile(firstName: 'Ana', email: 'a@b.rs').isDiscoverable, isTrue);
      expect(
        const PrivateProfile(lastName: 'Anić', twitterUrl: 'x.com/ana').isDiscoverable,
        isTrue,
      );
      expect(const PrivateProfile(firstName: 'Ana', description: 'Hi').isDiscoverable, isFalse);
      expect(const PrivateProfile(email: 'a@b.rs').missingForDiscovery, hasLength(1));
      expect(const PrivateProfile().missingForDiscovery, hasLength(2));
    });

    test('changesTo returns only changed fields, with null for cleared ones', () {
      final before = PrivateProfile.fromJson(privateProfileJson());
      final after = PrivateProfile(
        firstName: 'Ana',
        lastName: 'Anić',
        email: 'new@example.com',
        phoneNumber: '+381 60 123 4567',
        dateOfBirth: DateTime.utc(1990, 5, 17),
        address: 'Knez Mihailova 1, Beograd',
        description: null,
        facebookUrl: 'https://facebook.com/ana',
      );
      expect(before.changesTo(after), {'email': 'new@example.com', 'description': null});
      expect(before.changesTo(before), isEmpty);
    });
  });

  group('BusinessProfile', () {
    test('parses and checks discoverability', () {
      final p = BusinessProfile.fromJson(businessProfileJson());
      expect(p.displayName, 'Ana d.o.o.');
      expect(p.isDiscoverable, isTrue);
      expect(const BusinessProfile(companyName: 'X').isDiscoverable, isFalse);
    });
  });

  test('NearbyUser and Contact wrap the profile of their mode', () {
    final nearby = NearbyUser.fromJson(Mode.business, {
      'userId': 'u2',
      'distanceKm': 0.11,
      'profile': businessProfileJson(),
    });
    expect(nearby.profile, isA<BusinessProfile>());
    expect(nearby.distanceKm, 0.11);
    expect((nearby.latitude, nearby.longitude), (null, null)); // older servers
    final located = NearbyUser.fromJson(Mode.private, {
      'userId': 'u2',
      'distanceKm': 0.1,
      'latitude': 44.8,
      'longitude': 20.4,
      'profile': privateProfileJson(),
    });
    expect((located.latitude, located.longitude), (44.8, 20.4));

    final contact = Contact.fromJson(Mode.private, {
      'userId': 'u3',
      'savedAt': '2026-09-28T18:22:34.4Z',
      'profile': privateProfileJson(),
    });
    expect(contact.profile, isA<PrivateProfile>());
    expect(contact.mode, Mode.private);
  });

  test('VisibilityStatus parses', () {
    final status = VisibilityStatus.fromJson({
      'mode': 'business',
      'latitude': 44.8125,
      'longitude': 20,
      'updatedAt': '2026-09-28T18:22:34.4Z',
    });
    expect(status.mode, Mode.business);
    expect(status.longitude, 20.0);
  });

  test('Session survives a round trip through JSON, and knows when it expires', () {
    final session = Session.fromJson(loginJson());
    final copy = Session.fromJson(session.toJson());
    expect(copy.accessToken, session.accessToken);
    expect(copy.account.username, 'ana.anic');
    expect(copy.isExpired, isFalse);

    final expired = Session(
      accessToken: 't',
      expiresAt: DateTime.now().subtract(const Duration(seconds: 1)),
      account: session.account,
    );
    expect(expired.isExpired, isTrue);
  });
}
