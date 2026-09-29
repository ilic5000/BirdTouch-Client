import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../data/models/mode.dart';
import '../../data/models/profile.dart';
import '../../state/people.dart';
import '../../state/session.dart';
import '../../state/settings.dart';
import '../router.dart';
import '../widgets/common.dart';
import '../widgets/profile_avatar.dart';

/// The user's own account: profiles, settings, about, sign out.
class MeScreen extends ConsumerWidget {
  const MeScreen({super.key});

  Future<void> _signOut(BuildContext context, WidgetRef ref) async {
    final developerMode = ref.read(settingsProvider).developerMode;
    final choice = await showDialog<({bool stayVisible})>(
      context: context,
      builder: (_) => _SignOutDialog(developerMode: developerMode),
    );
    if (choice != null) {
      await ref.read(sessionProvider.notifier).signOut(hide: !choice.stayVisible);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final username = ref.watch(sessionProvider.select((s) => s?.account.username)) ?? '';
    final private = ref.watch(myProfileProvider(Mode.private));
    final name = private.value?.displayName ?? '';

    return Scaffold(
      appBar: AppBar(title: const Text('Me')),
      body: RefreshIndicator(
        onRefresh: () => Future.wait([
          for (final mode in Mode.values) ref.refresh(myProfileProvider(mode).future),
        ]),
        child: ListView(
          padding: const EdgeInsets.only(bottom: 24),
          children: [
            const SizedBox(height: 8),
            Center(
              child: GestureDetector(
                onTap: () => context.push(Routes.editProfile(Mode.private)),
                child: ProfileAvatar(
                  pictureUrl: private.value?.pictureUrl,
                  name: name,
                  mode: Mode.private,
                  radius: 52,
                ),
              ),
            ),
            const SizedBox(height: 16),
            Text(
              name.isEmpty ? username : name,
              textAlign: TextAlign.center,
              style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
            ),
            if (name.isNotEmpty)
              Text(
                '@$username',
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            const SectionHeader('Your profiles'),
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 16),
              child: Column(
                children: [
                  _ProfileCard(mode: Mode.private),
                  SizedBox(height: 12),
                  _ProfileCard(mode: Mode.business),
                ],
              ),
            ),
            const SectionHeader('App'),
            ListTile(
              leading: const Icon(Icons.settings_outlined),
              title: const Text('Settings'),
              subtitle: const Text('Search radius, account'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push(Routes.settings),
            ),
            ListTile(
              leading: const Icon(Icons.info_outline),
              title: const Text('About'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push(Routes.about),
            ),
            ListTile(
              leading: Icon(Icons.logout, color: theme.colorScheme.error),
              title: Text('Sign out', style: TextStyle(color: theme.colorScheme.error)),
              onTap: () => _signOut(context, ref),
            ),
          ],
        ),
      ),
    );
  }
}

class _ProfileCard extends ConsumerWidget {
  const _ProfileCard({required this.mode});

  final Mode mode;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final profile = ref.watch(myProfileProvider(mode));
    final title = mode == Mode.private ? 'Private profile' : 'Business profile';

    final (String subtitle, Widget? status) = switch (profile) {
      AsyncValue(value: final Profile p?) => (
        p.displayName.isEmpty ? 'Not filled in yet' : p.displayName,
        p.isDiscoverable
            ? _Status(label: 'Complete', color: Colors.green.shade700, icon: Icons.check_circle)
            : _Status(
                label: 'Incomplete',
                color: theme.colorScheme.tertiary,
                icon: Icons.error_outline,
              ),
      ),
      AsyncValue(hasError: true) => ('Could not load', null),
      _ => ('Loading…', null),
    };

    return Card(
      child: InkWell(
        onTap: () => context.push(Routes.editProfile(mode)),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              ProfileAvatar(
                pictureUrl: profile.value?.pictureUrl,
                name: profile.value?.displayName ?? '',
                mode: mode,
                radius: 26,
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: theme.textTheme.titleMedium),
                    const SizedBox(height: 2),
                    Text(
                      subtitle,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    if (status != null) ...[const SizedBox(height: 6), status],
                  ],
                ),
              ),
              const Icon(Icons.edit_outlined),
            ],
          ),
        ),
      ),
    );
  }
}

class _Status extends StatelessWidget {
  const _Status({required this.label, required this.color, required this.icon});

  final String label;
  final Color color;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Icon(icon, size: 16, color: color),
      const SizedBox(width: 4),
      Text(label, style: Theme.of(context).textTheme.labelMedium?.copyWith(color: color)),
    ],
  );
}

/// Confirms signing out. With the developer options on, the user can choose to stay visible, e.g.
/// to set up test users and then look for them from another account.
class _SignOutDialog extends StatefulWidget {
  const _SignOutDialog({required this.developerMode});

  final bool developerMode;

  @override
  State<_SignOutDialog> createState() => _SignOutDialogState();
}

class _SignOutDialogState extends State<_SignOutDialog> {
  bool _stayVisible = false;

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Sign out?'),
    content: Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          _stayVisible
              ? 'People nearby will keep seeing you where you are now, until the server hides '
                    'you for inactivity (24 hours by default).'
              : 'You will be hidden from people nearby.',
        ),
        if (widget.developerMode) ...[
          const SizedBox(height: 12),
          CheckboxListTile(
            value: _stayVisible,
            onChanged: (value) => setState(() => _stayVisible = value ?? false),
            contentPadding: EdgeInsets.zero,
            controlAffinity: ListTileControlAffinity.leading,
            title: const Text('Stay visible to others'),
            subtitle: const Text('Developer option'),
          ),
        ],
      ],
    ),
    actions: [
      TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
      FilledButton(
        style: FilledButton.styleFrom(minimumSize: const Size(64, 44)),
        onPressed: () => Navigator.pop(context, (stayVisible: _stayVisible)),
        child: const Text('Sign out'),
      ),
    ],
  );
}
