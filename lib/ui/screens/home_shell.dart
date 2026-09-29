import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../data/models/mode.dart';
import '../../state/session.dart';
import '../../state/settings.dart';
import '../../state/visibility.dart';
import 'onboarding_sheet.dart';

/// Home screen with the bottom navigation: private, business, saved contacts and the user's own
/// profile. A green dot marks the modes in which the user is visible.
class HomeShell extends ConsumerStatefulWidget {
  const HomeShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  ConsumerState<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends ConsumerState<HomeShell> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _showOnboardingOnce());
  }

  Future<void> _showOnboardingOnce() async {
    final userId = ref.read(currentUserIdProvider);
    final settings = ref.read(settingsProvider.notifier);
    if (!mounted || userId == null || settings.onboardingSeen(userId)) return;
    await settings.setOnboardingSeen(userId);
    if (mounted) await showOnboarding(context);
  }

  @override
  Widget build(BuildContext context) {
    final visibility = ref.watch(visibilityProvider);

    Widget modeIcon(Mode mode, IconData icon) => Badge(
      isLabelVisible: visibility.of(mode).visible,
      smallSize: 10,
      backgroundColor: Colors.green.shade500,
      child: Icon(icon),
    );

    return Scaffold(
      body: widget.shell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: widget.shell.currentIndex,
        onDestinationSelected: (index) =>
            widget.shell.goBranch(index, initialLocation: index == widget.shell.currentIndex),
        destinations: [
          NavigationDestination(
            icon: modeIcon(Mode.private, Icons.people_outline),
            selectedIcon: modeIcon(Mode.private, Icons.people),
            label: 'Private',
          ),
          NavigationDestination(
            icon: modeIcon(Mode.business, Icons.business_center_outlined),
            selectedIcon: modeIcon(Mode.business, Icons.business_center),
            label: 'Business',
          ),
          const NavigationDestination(
            icon: Icon(Icons.bookmarks_outlined),
            selectedIcon: Icon(Icons.bookmarks),
            label: 'Saved',
          ),
          const NavigationDestination(
            icon: Icon(Icons.account_circle_outlined),
            selectedIcon: Icon(Icons.account_circle),
            label: 'Me',
          ),
        ],
      ),
    );
  }
}
