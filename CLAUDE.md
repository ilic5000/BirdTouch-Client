# BirdTouch client

Flutter app (Android + iOS) for the BirdTouch-WebAPI server (FastAPI, `C:\Projects\BirdTouch-WebAPI`
locally, https://github.com/ilic5000/BirdTouch-WebAPI). README.md has the full docs. Replaced the
old Xamarin app (still in git history).

## Commands

```sh
flutter pub get
flutter analyze                     # must be clean
dart format lib test                # page width 100 (analysis_options.yaml); CI checks it
flutter test                        # unit + widget tests, no server needed
BIRDTOUCH_E2E_URL=http://localhost:4050 flutter test test/e2e   # against a running server
flutter run --dart-define-from-file=dart_defines/local.example.json   # emulator + local server
flutter build apk --debug           # Android build check (iOS builds need macOS)
python tool/seed_demo_users.py      # demo users visible in Belgrade, for manual testing
```

The server for local testing: `docker compose up -d --build` in the server repo (port 4050).

## Builds for testers

When asked to build the app for someone to test (no stores):

- **Android**: run
  `flutter build apk --release --dart-define-from-file=dart_defines/production.example.json`
  (add `--build-number=<n>`, higher than the last one, if asked for a new version), then give the
  absolute path: `C:\Projects\BirdTouch-Client\build\app\outputs\flutter-apk\app-release.apk`
  (about 57 MB; `--split-per-abi` gives smaller per-CPU APKs, most phones need `app-arm64-v8a`).
  It's signed with the debug key unless `android/key.properties` exists; testers install the file
  directly ("install unknown apps").
- **iOS** can't be built here (needs macOS + Xcode). Say so and give the steps: on a Mac,
  `flutter build ipa --release --dart-define-from-file=dart_defines/production.example.json`,
  then TestFlight (upload `build/ios/ipa/*.ipa` with Transporter; paid Apple Developer account) or
  ad hoc (`--export-method ad-hoc`, testers' UDIDs registered). README "Builds for test users".
- The build points at the server in `production.example.json` (`http://160.99.21.233:4050`) with
  the developer options on. Mention that the server must run the matching server version.

## Architecture

- `lib/data`: `BirdTouchApi` (all endpoints, throws `ApiException` with the server's stable `code`),
  models, location, picture picking/cropping, secure storage. No Flutter widgets here.
- `lib/state`: Riverpod 3 providers. `sessionProvider` (login; `dioProvider` adds the token and calls
  `expire()` on 401), `settingsProvider` (SharedPreferences), `visibilityProvider` (per mode,
  periodic location refresh), `nearbyProvider(mode)`, `contactsProvider(mode)`,
  `myProfileProvider(mode)`. Per-user providers watch `currentUserIdProvider` so they reset on
  sign-in/out.
- `lib/ui`: `router.dart` (go_router; redirect to `/welcome` when signed out), `theme.dart`,
  `screens/`, `widgets/`.
- Riverpod 3 specifics: family notifiers get the argument in the constructor
  (`AsyncNotifierProvider.family(ContactsController.new)`), `AsyncValue.value` is nullable, check
  `ref.mounted` after awaits. Automatic retry is disabled in `ProviderScope(retry: ...)`.

## Rules

- Mirror server rules on the client (`utils/validators.dart`, `Profile.missingForDiscovery`). If the
  server rules change, change them here too.
- Profile updates are partial: send only `Profile.changesTo(edited)`; `null` clears a field.
- API changes are made in the server repo first (its tests, README and `changes-from-old-api.md`),
  then here: `BirdTouchApi`, models, `test/api_test.dart`, `test/e2e/api_e2e_test.dart`.
- New screens: a route in `ui/router.dart`, a file in `ui/screens/`. Show errors with
  `showError(context, e)` / `errorMessage(e)`, which know `ApiException` and `LocationException`.
- Every behaviour change gets a test (`test/app_test.dart` for UI flows with the mocked API).
- Build-time settings are `--dart-define`s read in `config/app_config.dart`; document new ones in the
  README table and `dart_defines/*.example.json`.
- Developer options are on by default (`BIRDTOUCH_DEV_OPTIONS`) while the app is in testing. Before
  a public release: `false` in `app_config.dart` and `production.example.json`.
- Keep README.md up to date (features, configuration, testing, builds).
