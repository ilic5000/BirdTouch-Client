import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../state/session.dart';
import '../../state/settings.dart';
import '../../utils/formatters.dart';
import '../router.dart';
import '../widgets/common.dart';
import '../widgets/radius_sheet.dart';
import 'auth/server_settings_sheet.dart';
import 'onboarding_sheet.dart';
import 'person_screen.dart' show openUri;

class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  /// Long-pressing the title toggles the developer options, as in the old app.
  Future<void> _toggleDeveloperMode(BuildContext context, WidgetRef ref) async {
    final enabled = !ref.read(settingsProvider).developerMode;
    await ref.read(settingsProvider.notifier).setDeveloperMode(enabled);
    if (context.mounted) {
      showMessage(context, enabled ? 'Developer options enabled' : 'Developer options disabled');
    }
  }

  Future<void> _deleteAccount(BuildContext context, WidgetRef ref) async {
    final sure = await confirm(
      context,
      title: 'Delete your account?',
      message: 'Are you sure? There is no going back: we do not keep backups of your data.',
      confirmLabel: 'Yes',
      destructive: true,
    );
    if (!sure || !context.mounted) return;
    final reallySure = await confirm(
      context,
      title: 'Confirm one more time',
      message:
          'This permanently removes your profiles, pictures and saved contacts, and removes you '
          'from the contacts of others.',
      confirmLabel: 'Delete everything',
      destructive: true,
    );
    if (!reallySure || !context.mounted) return;

    try {
      await ref.read(apiProvider).deleteAccount();
    } catch (e) {
      if (context.mounted) showError(context, e);
      return;
    }
    if (context.mounted) {
      await showDialog<void>(
        context: context,
        barrierDismissible: false,
        builder: (context) => AlertDialog(
          title: const Text('Farewell'),
          content: const Text('Your account has been deleted. We are sad to see you go, godspeed.'),
          actions: [
            FilledButton(onPressed: () => Navigator.pop(context), child: const Text('Close')),
          ],
        ),
      );
    }
    await ref.read(sessionProvider.notifier).signOut(hide: false);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final settings = ref.watch(settingsProvider);
    final session = ref.watch(sessionProvider);

    return Scaffold(
      appBar: AppBar(
        title: GestureDetector(
          onLongPress: () => _toggleDeveloperMode(context, ref),
          child: const Text('Settings'),
        ),
      ),
      body: ListView(
        padding: withBottomInset(context, const EdgeInsets.only(bottom: 32)),
        children: [
          const SectionHeader('Search'),
          const Padding(padding: EdgeInsets.symmetric(horizontal: 20), child: RadiusPicker()),
          if (settings.developerMode) ...[
            const SectionHeader('Developer options'),
            ListTile(
              leading: const Icon(Icons.dns_outlined),
              title: const Text('Server'),
              subtitle: Text(settings.serverUrl),
              trailing: const Icon(Icons.edit_outlined),
              onTap: () => showServerSettings(context),
            ),
            ListTile(
              leading: const Icon(Icons.api_outlined),
              title: const Text('API documentation'),
              subtitle: Text('${settings.serverUrl}/docs'),
              trailing: const Icon(Icons.open_in_new, size: 18),
              onTap: () => openUri(context, Uri.parse('${settings.serverUrl}/docs')),
            ),
            ListTile(
              leading: const Icon(Icons.edit_location_alt_outlined),
              title: const Text('Fake location'),
              subtitle: Text(switch (settings.fakeLocation) {
                final fake? => formatCoordinates(fake.latitude, fake.longitude),
                null => 'Off: the device location is used',
              }),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push(Routes.fakeLocation),
            ),
            ListTile(
              leading: const Icon(Icons.school_outlined),
              title: const Text('Show the introduction again'),
              onTap: () => showOnboarding(context),
            ),
            const ListTile(
              leading: Icon(Icons.bug_report_outlined),
              title: Text('Build'),
              subtitle: Text(
                '${kReleaseMode
                    ? 'release'
                    : kProfileMode
                    ? 'profile'
                    : 'debug'} · '
                'search radius up to 500 km or unlimited',
              ),
            ),
            SwitchListTile(
              secondary: const Icon(Icons.developer_mode),
              title: const Text('Developer options'),
              subtitle: const Text('Long-press the "Settings" title to toggle'),
              value: true,
              onChanged: (_) => _toggleDeveloperMode(context, ref),
            ),
          ],
          const SectionHeader('Account'),
          if (session != null)
            ListTile(
              leading: const Icon(Icons.person_outline),
              title: Text(session.account.username),
              subtitle: Text('Member since ${formatDate(session.account.createdAt.toLocal())}'),
            ),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: Text(
              'You can remove your account and all its data from BirdTouch at any time. '
              'This cannot be undone.',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 0),
            child: OutlinedButton.icon(
              style: OutlinedButton.styleFrom(
                foregroundColor: theme.colorScheme.error,
                side: BorderSide(color: theme.colorScheme.error),
              ),
              onPressed: () => _deleteAccount(context, ref),
              icon: const Icon(Icons.delete_forever_outlined),
              label: const Text('Delete account'),
            ),
          ),
        ],
      ),
    );
  }
}
