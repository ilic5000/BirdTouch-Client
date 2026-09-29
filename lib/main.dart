import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app.dart';
import 'data/storage/session_storage.dart';
import 'state/session.dart';
import 'state/settings.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Read everything needed for the first frame, so the app opens on the right screen.
  final sessionStorage = SessionStorage();
  final (prefs, session) = await (SharedPreferences.getInstance(), sessionStorage.read()).wait;

  runApp(
    ProviderScope(
      // Failed requests are retried by the user (pull to refresh, "Try again"), not automatically.
      retry: (_, _) => null,
      overrides: [
        sharedPreferencesProvider.overrideWithValue(prefs),
        sessionStorageProvider.overrideWithValue(sessionStorage),
        initialSessionProvider.overrideWithValue(session),
      ],
      child: const BirdTouchApp(),
    ),
  );
}
