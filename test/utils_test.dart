import 'dart:typed_data';

import 'package:birdtouch/state/settings.dart';
import 'package:birdtouch/utils/formatters.dart';
import 'package:birdtouch/utils/image_type.dart';
import 'package:birdtouch/utils/links.dart';
import 'package:birdtouch/utils/server_url.dart';
import 'package:birdtouch/utils/validators.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Validators mirror the server rules', () {
    test('username', () {
      expect(Validators.username('ab'), isNotNull);
      expect(Validators.username('ana.anic'), isNull);
      expect(Validators.username('a_b@c+d-e'), isNull);
      expect(Validators.username('ana anic'), isNotNull);
      expect(Validators.username('ana#1'), isNotNull);
      expect(Validators.username('a' * 64), isNull);
      expect(Validators.username('a' * 65), isNotNull);
    });

    test('password', () {
      expect(Validators.password('1234567'), isNotNull);
      expect(Validators.password('12345678'), isNull);
      expect(Validators.password('x' * 129), isNotNull);
    });

    test('email is optional but must be valid', () {
      expect(Validators.optionalEmail(''), isNull);
      expect(Validators.optionalEmail('ana@example.com'), isNull);
      expect(Validators.optionalEmail('ana@example'), isNotNull);
      expect(Validators.optionalEmail('ana example.com'), isNotNull);
    });

    test('text length', () {
      expect(Validators.shortText('x' * 200), isNull);
      expect(Validators.shortText('x' * 201), isNotNull);
      expect(Validators.longText('x' * 2000), isNull);
      expect(Validators.longText('x' * 2001), isNotNull);
    });
  });

  group('formatters', () {
    test('distance', () {
      expect(formatDistanceKm(0.004), '< 10 m');
      expect(formatDistanceKm(0.11), '110 m');
      expect(formatDistanceKm(0.996), '1.0 km');
      expect(formatDistanceKm(1.26), '1.3 km');
      expect(formatDistanceKm(15.4), '15 km');
    });

    test('radius', () {
      expect(formatRadius(50), '50 m');
      expect(formatRadius(1000), '1 km');
      expect(formatRadius(2500), '2.5 km');
      expect(formatRadius(unlimitedRadiusMeters), 'Unlimited');
    });

    test('time ago', () {
      final now = DateTime(2026, 9, 28, 12);
      expect(formatTimeAgo(now.subtract(const Duration(seconds: 20)), now: now), 'just now');
      expect(formatTimeAgo(now.subtract(const Duration(minutes: 5)), now: now), '5 min ago');
      expect(formatTimeAgo(now.subtract(const Duration(hours: 3)), now: now), '3 h ago');
    });

    test('coordinates', () {
      expect(formatCoordinates(44.8125, 20.4612), '44.81250, 20.46120');
      expect(parseCoordinates('44.8125, 20.4612'), (latitude: 44.8125, longitude: 20.4612));
      expect(parseCoordinates(' 44.8125 20.4612 '), (latitude: 44.8125, longitude: 20.4612));
      expect(parseCoordinates('-33.9;151.2'), (latitude: -33.9, longitude: 151.2));
      expect(parseCoordinates('91, 20'), isNull);
      expect(parseCoordinates('44.8'), isNull);
      expect(parseCoordinates('here'), isNull);
    });

    test('initials', () {
      expect(initialsOf('Ana Anić'), 'AA');
      expect(initialsOf('  ana  '), 'A');
      expect(initialsOf('Đorđe Petrović Njegoš'), 'ĐN');
      expect(initialsOf(''), '?');
    });
  });

  group('links', () {
    test('web links get a scheme', () {
      expect(webUri('ana.rs').toString(), 'https://ana.rs');
      expect(webUri('http://ana.rs/x').toString(), 'http://ana.rs/x');
      expect(webUri('  '), isNull);
    });

    test('social handles become profile URLs', () {
      expect(webUri('@ana', network: SocialNetwork.twitter).toString(), 'https://x.com/ana');
      expect(webUri('ana', network: SocialNetwork.facebook).toString(), 'https://facebook.com/ana');
      expect(
        webUri('linkedin.com/in/ana', network: SocialNetwork.linkedin).toString(),
        'https://linkedin.com/in/ana',
      );
    });

    test('phone, email, maps', () {
      expect(phoneUri('+381 60 123-4567').toString(), 'tel:+381601234567');
      expect(phoneUri(''), isNull);
      expect(emailUri('ana@example.com').toString(), 'mailto:ana@example.com');
      expect(mapsUri('Knez Mihailova 1')!.queryParameters['query'], 'Knez Mihailova 1');
    });
  });

  group('server URL', () {
    test('adds http and removes trailing slashes', () {
      expect(normalizeServerUrl('188.245.72.17:4050'), 'http://188.245.72.17:4050');
      expect(normalizeServerUrl('https://api.birdtouch.rs/'), 'https://api.birdtouch.rs');
      expect(normalizeServerUrl(' http://10.0.2.2:4050// '), 'http://10.0.2.2:4050');
    });

    test('rejects invalid addresses', () {
      expect(normalizeServerUrl(''), isNull);
      expect(normalizeServerUrl('ftp://host'), isNull);
      expect(normalizeServerUrl('http://'), isNull);
    });
  });

  test('image type detection', () {
    expect(detectImageType(Uint8List.fromList([0xFF, 0xD8, 0xFF, 0xE0])), 'image/jpeg');
    expect(
      detectImageType(Uint8List.fromList([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0])),
      'image/png',
    );
    expect(
      detectImageType(Uint8List.fromList('RIFF\x00\x00\x00\x00WEBPVP8 '.codeUnits)),
      'image/webp',
    );
    expect(detectImageType(Uint8List.fromList('GIF89a'.codeUnits)), isNull);
    expect(detectImageType(Uint8List(0)), isNull);
  });
}
