import 'dart:convert';
import 'dart:typed_data';

import 'package:birdtouch/app.dart';
import 'package:birdtouch/data/api/api_exception.dart';
import 'package:birdtouch/data/api/birdtouch_api.dart';
import 'package:birdtouch/data/location/location_service.dart';
import 'package:birdtouch/data/models/mode.dart';
import 'package:birdtouch/data/models/people.dart';
import 'package:birdtouch/data/models/profile.dart';
import 'package:birdtouch/data/models/session.dart';
import 'package:birdtouch/data/pictures/picture_picker.dart';
import 'package:birdtouch/state/people.dart';
import 'package:birdtouch/state/session.dart';
import 'package:birdtouch/state/settings.dart';
import 'package:birdtouch/state/visibility.dart';
import 'package:birdtouch/ui/widgets/map_parts.dart';
import 'package:birdtouch/ui/widgets/profile_avatar.dart';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'helpers.dart';

class MockApi extends Mock implements BirdTouchApi {}

class FakeLocation extends LocationService {
  LocationProblem? problem;

  @override
  Future<Coordinates> currentLocation() async {
    if (problem != null) throw LocationException(problem!);
    return (latitude: 44.8125, longitude: 20.4612);
  }

  @override
  Future<String?> addressOf(Coordinates location) async => 'Knez Mihailova 1, Beograd';
}

/// Map tiles without the network: every tile is a transparent pixel.
class FakeTileProvider extends TileProvider {
  static final _pixel = Uint8List.fromList(
    base64Decode(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
    ),
  );

  @override
  ImageProvider getImage(TileCoordinates coordinates, TileLayer options) => MemoryImage(_pixel);
}

class FakePicturePicker extends PicturePicker {
  final picked = <PictureSource>[];

  /// The start of a JPEG file.
  static final jpeg = Uint8List.fromList([0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3]);

  @override
  Future<Uint8List?> pick(
    PictureSource source, {
    required Color toolbarColor,
    required Color toolbarForegroundColor,
    required Color accentColor,
  }) async {
    picked.add(source);
    return jpeg;
  }
}

void main() {
  late MockApi api;
  late FakeLocation location;
  late FakePicturePicker picturePicker;
  late MemorySessionStorage storage;

  final session = Session.fromJson(loginJson());
  final bob = NearbyUser(
    userId: 'bob',
    distanceKm: 0.12,
    latitude: 44.8135,
    longitude: 20.4615,
    profile: PrivateProfile.fromJson(
      privateProfileJson(firstName: 'Bob', lastName: 'Bobić', email: 'bob@example.com'),
    ),
  );

  setUp(() {
    api = MockApi();
    location = FakeLocation();
    picturePicker = FakePicturePicker();
    storage = MemorySessionStorage();
    when(() => api.visibility()).thenAnswer((_) async => []);
    when(() => api.contacts(any())).thenAnswer((_) async => []);
    when(
      () => api.profile(Mode.private),
    ).thenAnswer((_) async => PrivateProfile.fromJson(privateProfileJson()));
    when(() => api.profile(Mode.business)).thenAnswer((_) async => const BusinessProfile());
    when(() => api.hide(any())).thenAnswer((_) async {});
  });

  setUpAll(() {
    registerFallbackValue(Mode.private);
    registerFallbackValue(Uint8List(0));
  });

  Future<void> startApp(
    WidgetTester tester, {
    Session? signedIn,
    bool onboardingSeen = true,
    Map<String, Object> prefs = const {},
  }) async {
    SharedPreferences.setMockInitialValues({
      if (onboardingSeen) 'onboarding_seen_user-1': true,
      ...prefs,
    });
    final sharedPreferences = await SharedPreferences.getInstance();
    storage.stored = signedIn;
    // A phone-sized screen (the default test surface is 800x600).
    tester.view.physicalSize = const Size(1080, 2340);
    tester.view.devicePixelRatio = 2.75;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(
      ProviderScope(
        retry: (_, _) => null,
        overrides: [
          sharedPreferencesProvider.overrideWithValue(sharedPreferences),
          sessionStorageProvider.overrideWithValue(storage),
          initialSessionProvider.overrideWithValue(signedIn),
          apiProvider.overrideWithValue(api),
          deviceLocationServiceProvider.overrideWithValue(location),
          picturePickerProvider.overrideWithValue(picturePicker),
          mapTileProviderProvider.overrideWithValue(FakeTileProvider.new),
        ],
        child: const BirdTouchApp(),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
  }

  /// Disposes the providers, which stops the location refresh timer.
  Future<void> stopApp(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox());
    await tester.pump();
  }

  testWidgets('signed out users see the welcome screen', (tester) async {
    await startApp(tester);
    expect(find.text('BirdTouch'), findsOneWidget);
    expect(find.text('Sign in'), findsOneWidget);
    expect(find.text('Sign up'), findsOneWidget);
    expect(find.text('188.245.72.17:4050'), findsOneWidget);
    await stopApp(tester);
  });

  testWidgets('signing in opens the home screen and the introduction', (tester) async {
    when(
      () => api.login(username: 'ana.anic', password: 'secret123'),
    ).thenAnswer((_) async => session);

    await startApp(tester, onboardingSeen: false);
    await tester.tap(find.text('Sign in'));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'Username'), 'ana.anic');
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'secret123');
    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('Welcome to BirdTouch'), findsOneWidget);
    await tester.ensureVisible(find.text('Got it'));
    await tester.tap(find.text('Got it'));
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('Make me visible'), findsOneWidget);
    expect(find.text('You are not visible'), findsOneWidget);
    expect(storage.stored?.accessToken, 'token-1');
    await stopApp(tester);
  });

  testWidgets('a wrong password shows an error', (tester) async {
    when(
      () => api.login(
        username: any(named: 'username'),
        password: any(named: 'password'),
      ),
    ).thenThrow(const ApiException(code: 'invalid_credentials', message: '', statusCode: 401));

    await startApp(tester);
    await tester.tap(find.text('Sign in'));
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'Username'), 'ana');
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'wrong-password');
    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pumpAndSettle();

    expect(find.text('Wrong username or password.'), findsOneWidget);
    await stopApp(tester);
  });

  testWidgets('going visible shows people nearby, who can be saved', (tester) async {
    when(
      () => api.setVisibility(
        Mode.private,
        latitude: any(named: 'latitude'),
        longitude: any(named: 'longitude'),
      ),
    ).thenAnswer(
      (_) async => VisibilityStatus(
        mode: Mode.private,
        latitude: 44.8125,
        longitude: 20.4612,
        updatedAt: DateTime.now(),
      ),
    );
    when(
      () => api.nearby(
        Mode.private,
        radiusKm: any(named: 'radiusKm'),
        limit: any(named: 'limit'),
      ),
    ).thenAnswer((_) async => [bob]);
    when(() => api.saveContact(Mode.private, 'bob')).thenAnswer((_) async {});

    await startApp(tester, signedIn: session);
    expect(find.text('You are not visible'), findsOneWidget);

    await tester.tap(find.byType(Switch));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('You are visible'), findsOneWidget);
    expect(find.text('Bob Bobić'), findsOneWidget);
    expect(find.textContaining('120 m'), findsOneWidget);
    verify(() => api.setVisibility(Mode.private, latitude: 44.8125, longitude: 20.4612)).called(1);

    await tester.tap(find.byTooltip('Save contact'));
    await tester.pump();
    verify(() => api.saveContact(Mode.private, 'bob')).called(1);
    expect(find.byTooltip('Remove from saved'), findsOneWidget);
    await stopApp(tester);
  });

  testWidgets('without location permission the user stays hidden and is told why', (tester) async {
    location.problem = LocationProblem.permissionDeniedForever;
    await startApp(tester, signedIn: session);

    await tester.tap(find.byType(Switch));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text(LocationProblem.permissionDeniedForever.message), findsOneWidget);
    expect(find.text('Make me visible'), findsOneWidget);
    verifyNever(
      () => api.setVisibility(
        any(),
        latitude: any(named: 'latitude'),
        longitude: any(named: 'longitude'),
      ),
    );
    await stopApp(tester);
  });

  testWidgets('an expired session goes back to the welcome screen', (tester) async {
    await startApp(tester, signedIn: session);
    expect(find.text('Make me visible'), findsOneWidget);

    final element = tester.element(find.byType(Scaffold).first);
    ProviderScope.containerOf(element).read(sessionProvider.notifier).expire();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('Sign up'), findsOneWidget);
    expect(find.text('Your session has expired. Please sign in again.'), findsOneWidget);
    await stopApp(tester);
  });

  testWidgets('the undo message after removing a saved contact goes away by itself', (
    tester,
  ) async {
    when(() => api.contacts(Mode.private)).thenAnswer(
      (_) async => [Contact(userId: 'bob', profile: bob.profile, savedAt: DateTime(2026, 9, 1))],
    );
    when(() => api.removeContact(Mode.private, 'bob')).thenAnswer((_) async {});

    await startApp(tester, signedIn: session);
    await tester.tap(find.byIcon(Icons.bookmarks_outlined));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
    await tester.drag(find.text('Bob Bobić'), const Offset(-500, 0));
    await tester.pumpAndSettle();

    verify(() => api.removeContact(Mode.private, 'bob')).called(1);
    expect(find.text('Bob Bobić removed from saved'), findsOneWidget);
    expect(find.text('Undo'), findsOneWidget);
    await tester.pump(const Duration(seconds: 6));
    await tester.pump(const Duration(seconds: 1));
    expect(find.text('Bob Bobić removed from saved'), findsNothing);
    await stopApp(tester);
  });

  testWidgets('tapping the profile picture offers to change it, then crops and uploads it', (
    tester,
  ) async {
    when(
      () => api.uploadPicture(Mode.private, any(), contentType: any(named: 'contentType')),
    ).thenAnswer((_) async {});

    await startApp(tester, signedIn: session);
    await tester.tap(find.text('Me'));
    await tester.pump(const Duration(milliseconds: 500));
    await tester.tap(find.text('Private profile'));
    await tester.pumpAndSettle();

    await tester.tap(find.byType(ProfileAvatar));
    await tester.pumpAndSettle();
    expect(find.text('Take a photo'), findsOneWidget);
    await tester.tap(find.text('Choose from gallery'));
    await tester.pumpAndSettle();

    expect(picturePicker.picked, [PictureSource.gallery]);
    verify(
      () => api.uploadPicture(Mode.private, FakePicturePicker.jpeg, contentType: 'image/jpeg'),
    ).called(1);
    await stopApp(tester);
  });

  group('signing out', () {
    Future<void> signOut(WidgetTester tester, {required bool developerMode}) async {
      await startApp(tester, signedIn: session, prefs: {'developer_mode': developerMode});
      await tester.tap(find.text('Me'));
      await tester.pump(const Duration(milliseconds: 500));
      await tester.scrollUntilVisible(find.text('Sign out'), 200);
      await tester.tap(find.text('Sign out'));
      await tester.pumpAndSettle();
    }

    testWidgets('hides the user', (tester) async {
      await signOut(tester, developerMode: false);
      expect(find.text('Stay visible to others'), findsNothing);
      await tester.tap(find.widgetWithText(FilledButton, 'Sign out'));
      await tester.pumpAndSettle();

      verify(() => api.hide(Mode.private)).called(1);
      verify(() => api.hide(Mode.business)).called(1);
      expect(find.text('Sign up'), findsOneWidget);
      await stopApp(tester);
    });

    testWidgets('can keep the user visible with the developer options', (tester) async {
      await signOut(tester, developerMode: true);
      await tester.tap(find.text('Stay visible to others'));
      await tester.pump();
      expect(find.textContaining('keep seeing you'), findsOneWidget);
      await tester.tap(find.widgetWithText(FilledButton, 'Sign out'));
      await tester.pumpAndSettle();

      verifyNever(() => api.hide(any()));
      expect(find.text('Sign up'), findsOneWidget);
      await stopApp(tester);
    });
  });

  testWidgets('the unlimited search radius searches without a distance limit', (tester) async {
    final visible = VisibilityStatus(
      mode: Mode.private,
      latitude: 44.8125,
      longitude: 20.4612,
      updatedAt: DateTime.now(),
    );
    when(() => api.visibility()).thenAnswer((_) async => [visible]);
    when(
      () => api.setVisibility(
        Mode.private,
        latitude: any(named: 'latitude'),
        longitude: any(named: 'longitude'),
      ),
    ).thenAnswer((_) async => visible);
    when(
      () => api.nearby(
        Mode.private,
        radiusKm: any(named: 'radiusKm'),
        limit: any(named: 'limit'),
      ),
    ).thenAnswer((_) async => [bob]);

    await startApp(
      tester,
      signedIn: session,
      prefs: {'developer_mode': true, 'search_radius_meters': unlimitedRadiusMeters},
    );
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('∞'), findsOneWidget);
    expect(find.text('1 person visible'), findsOneWidget);
    verify(
      () => api.nearby(Mode.private, radiusKm: null, limit: any(named: 'limit')),
    ).called(greaterThan(0));
    await stopApp(tester);
  });

  testWidgets('screens leave room for the system navigation at the bottom', (tester) async {
    await startApp(tester, signedIn: session);
    tester.view.padding = const FakeViewPadding(bottom: 132); // 48 logical pixels
    await tester.tap(find.text('Me'));
    await tester.pump(const Duration(milliseconds: 500));
    await tester.tap(find.text('Settings'));
    await tester.pumpAndSettle();

    final list = tester.widget<ListView>(find.byType(ListView));
    expect((list.padding! as EdgeInsets).bottom, 32 + 48);
    await stopApp(tester);
  });

  group('map', () {
    /// Visible in private mode at [latitude], [longitude]; location updates echo the location.
    void visibleAt({double latitude = 44.8125, double longitude = 20.4612}) {
      VisibilityStatus status(double lat, double lon) => VisibilityStatus(
        mode: Mode.private,
        latitude: lat,
        longitude: lon,
        updatedAt: DateTime.now(),
      );
      when(() => api.visibility()).thenAnswer((_) async => [status(latitude, longitude)]);
      when(
        () => api.setVisibility(
          Mode.private,
          latitude: any(named: 'latitude'),
          longitude: any(named: 'longitude'),
        ),
      ).thenAnswer(
        (i) async =>
            status(i.namedArguments[#latitude] as double, i.namedArguments[#longitude] as double),
      );
      when(
        () => api.nearby(
          Mode.private,
          radiusKm: any(named: 'radiusKm'),
          limit: any(named: 'limit'),
        ),
      ).thenAnswer((_) async => [bob]);
    }

    testWidgets('shows where the user is and the people nearby, who can be opened', (tester) async {
      visibleAt();
      await startApp(tester, signedIn: session);
      await tester.pump(const Duration(milliseconds: 500));
      expect(find.text('Knez Mihailova 1, Beograd'), findsOneWidget); // on the visibility card

      await tester.tap(find.text('Map'));
      await tester.pumpAndSettle();

      expect(find.text('People nearby'), findsOneWidget);
      expect(find.text('You are here'), findsOneWidget);
      expect(find.text('Knez Mihailova 1, Beograd'), findsOneWidget);
      expect(find.text('1 person within 100 m'), findsOneWidget);
      expect(find.bySemanticsLabel('Your location'), findsWidgets);

      await tester.tap(find.text('Bob Bobić'));
      await tester.pumpAndSettle();
      expect(find.text('Save contact'), findsOneWidget); // Bob's profile
      await stopApp(tester);
    });

    testWidgets('the developer options can set a fake location, which is sent right away', (
      tester,
    ) async {
      visibleAt();
      await startApp(tester, signedIn: session, prefs: {'developer_mode': true});
      await tester.tap(find.text('Me'));
      await tester.pump(const Duration(milliseconds: 500));
      await tester.tap(find.text('Settings'));
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(find.text('Fake location'), 200);
      expect(find.text('Off: the device location is used'), findsOneWidget);
      await tester.tap(find.text('Fake location'));
      await tester.pumpAndSettle();

      await tester.enterText(find.widgetWithText(TextField, 'Latitude, longitude'), 'nonsense');
      await tester.tap(find.byTooltip('Show on the map'));
      await tester.pump();
      expect(find.textContaining('Enter latitude and longitude'), findsOneWidget);

      await tester.enterText(
        find.widgetWithText(TextField, 'Latitude, longitude'),
        '45.2671, 19.8335',
      );
      await tester.tap(find.byTooltip('Show on the map'));
      await tester.pump();
      await tester.tap(find.text('Use this location'));
      await tester.pumpAndSettle();

      verify(
        () => api.setVisibility(Mode.private, latitude: 45.2671, longitude: 19.8335),
      ).called(1);
      expect(find.text('45.26710, 19.83350'), findsOneWidget);
      await stopApp(tester);
    });
  });

  testWidgets('a saved contact who is also nearby can be opened (hero tags do not clash)', (
    tester,
  ) async {
    final visible = VisibilityStatus(
      mode: Mode.private,
      latitude: 44.8125,
      longitude: 20.4612,
      updatedAt: DateTime.now(),
    );
    when(() => api.visibility()).thenAnswer((_) async => [visible]);
    when(
      () => api.setVisibility(
        Mode.private,
        latitude: any(named: 'latitude'),
        longitude: any(named: 'longitude'),
      ),
    ).thenAnswer((_) async => visible);
    when(
      () => api.nearby(
        Mode.private,
        radiusKm: any(named: 'radiusKm'),
        limit: any(named: 'limit'),
      ),
    ).thenAnswer((_) async => [bob]);
    when(() => api.contacts(Mode.private)).thenAnswer(
      (_) async => [Contact(userId: 'bob', profile: bob.profile, savedAt: DateTime(2026, 9, 1))],
    );

    await startApp(tester, signedIn: session);
    // Build the Saved tab too, so Bob's avatar is in two tabs.
    await tester.tap(find.byIcon(Icons.bookmarks_outlined));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
    await tester.tap(find.byIcon(Icons.people_outline).last);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));

    await tester.tap(find.text('Bob Bobić'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Saved'), findsWidgets);
    expect(find.text('Private'), findsWidgets); // the chip on Bob's profile
    await stopApp(tester);
  });
}
