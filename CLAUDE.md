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
- Keep README.md up to date (features, configuration, testing, builds).
