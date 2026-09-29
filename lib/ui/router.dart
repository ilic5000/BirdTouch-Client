import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../data/models/mode.dart';
import '../data/models/people.dart';
import '../state/session.dart';
import 'screens/about_screen.dart';
import 'screens/auth/sign_in_screen.dart';
import 'screens/auth/sign_up_screen.dart';
import 'screens/auth/welcome_screen.dart';
import 'screens/contacts_screen.dart';
import 'screens/edit_profile_screen.dart';
import 'screens/fake_location_screen.dart';
import 'screens/home_shell.dart';
import 'screens/map_screen.dart';
import 'screens/me_screen.dart';
import 'screens/nearby_screen.dart';
import 'screens/person_screen.dart';
import 'screens/settings_screen.dart';

/// Paths of the app's screens.
abstract final class Routes {
  static const welcome = '/welcome';
  static const signIn = '/welcome/sign-in';
  static const signUp = '/welcome/sign-up';
  static const private = '/private';
  static const business = '/business';
  static const contacts = '/contacts';
  static const me = '/me';
  static const settings = '/settings';
  static const about = '/about';
  static const fakeLocation = '/settings/fake-location';

  static String nearby(Mode mode) => mode == Mode.private ? private : business;
  static String editProfile(Mode mode) => '/profile/${mode.apiName}';
  static String map(Mode mode) => '/map/${mode.apiName}';
  static String person(Person person) => '/people/${person.mode.apiName}/${person.userId}';
}

final _rootKey = GlobalKey<NavigatorState>();

final routerProvider = Provider<GoRouter>((ref) {
  // Re-runs the redirect when the user signs in or out (also when the token expires).
  final authChanges = ValueNotifier(ref.read(sessionProvider) != null);
  ref.listen(
    sessionProvider.select((s) => s != null),
    (_, signedIn) => authChanges.value = signedIn,
  );
  ref.onDispose(authChanges.dispose);

  final router = GoRouter(
    navigatorKey: _rootKey,
    initialLocation: Routes.private,
    refreshListenable: authChanges,
    redirect: (context, state) {
      final signedIn = ref.read(sessionProvider) != null;
      final onWelcome = state.matchedLocation.startsWith(Routes.welcome);
      if (!signedIn && !onWelcome) return Routes.welcome;
      if (signedIn && onWelcome) return Routes.private;
      return null;
    },
    routes: [
      GoRoute(
        path: Routes.welcome,
        builder: (_, _) => const WelcomeScreen(),
        routes: [
          GoRoute(path: 'sign-in', builder: (_, _) => const SignInScreen()),
          GoRoute(path: 'sign-up', builder: (_, _) => const SignUpScreen()),
        ],
      ),
      StatefulShellRoute(
        builder: (_, _, shell) => HomeShell(shell: shell),
        navigatorContainerBuilder: (_, shell, children) => IndexedStack(
          index: shell.currentIndex,
          children: [
            for (final (index, child) in children.indexed)
              Offstage(
                offstage: index != shell.currentIndex,
                child: TickerMode(
                  enabled: index == shell.currentIndex,
                  // The tabs stay alive, and the same person can be in two of them (nearby and
                  // saved). Only the visible tab's avatars fly to the next screen; otherwise
                  // their Hero tags would clash.
                  child: HeroMode(enabled: index == shell.currentIndex, child: child),
                ),
              ),
          ],
        ),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: Routes.private,
                builder: (_, _) => const NearbyScreen(mode: Mode.private),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: Routes.business,
                builder: (_, _) => const NearbyScreen(mode: Mode.business),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [GoRoute(path: Routes.contacts, builder: (_, _) => const ContactsScreen())],
          ),
          StatefulShellBranch(
            routes: [GoRoute(path: Routes.me, builder: (_, _) => const MeScreen())],
          ),
        ],
      ),
      GoRoute(
        path: '/people/:mode/:userId',
        builder: (_, state) => PersonScreen(
          mode: Mode.fromApi(state.pathParameters['mode']!),
          userId: state.pathParameters['userId']!,
          initial: state.extra is Person ? state.extra! as Person : null,
        ),
      ),
      GoRoute(
        path: '/profile/:mode',
        builder: (_, state) => EditProfileScreen(mode: Mode.fromApi(state.pathParameters['mode']!)),
      ),
      GoRoute(
        path: '/map/:mode',
        builder: (_, state) => NearbyMapScreen(mode: Mode.fromApi(state.pathParameters['mode']!)),
      ),
      GoRoute(path: Routes.settings, builder: (_, _) => const SettingsScreen()),
      GoRoute(path: Routes.fakeLocation, builder: (_, _) => const FakeLocationScreen()),
      GoRoute(path: Routes.about, builder: (_, _) => const AboutScreen()),
    ],
  );
  ref.onDispose(router.dispose);
  return router;
});
