import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../state/session.dart';
import '../../../state/settings.dart';
import '../../router.dart';
import '../../widgets/common.dart';
import 'server_settings_sheet.dart';

class WelcomeScreen extends ConsumerStatefulWidget {
  const WelcomeScreen({super.key});

  @override
  ConsumerState<WelcomeScreen> createState() => _WelcomeScreenState();
}

class _WelcomeScreenState extends ConsumerState<WelcomeScreen> {
  @override
  void initState() {
    super.initState();
    final session = ref.read(sessionProvider.notifier);
    if (session.expiredByServer) {
      session.expiredByServer = false;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) showMessage(context, 'Your session has expired. Please sign in again.');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final serverUrl = ref.watch(settingsProvider.select((s) => s.serverUrl));

    return Scaffold(
      body: DecoratedBox(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [colors.primaryContainer.withValues(alpha: 0.55), colors.surface],
            stops: const [0, 0.6],
          ),
        ),
        child: SafeArea(
          child: Column(
            children: [
              Align(
                alignment: Alignment.topLeft,
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: IconButton.filledTonal(
                    tooltip: 'Server settings',
                    icon: const Icon(Icons.settings_outlined),
                    onPressed: () => showServerSettings(context),
                  ),
                ),
              ),
              Expanded(
                child: Center(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.symmetric(horizontal: 32),
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 420),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          const Center(child: BrandLogo(size: 128)),
                          const SizedBox(height: 24),
                          Text(
                            'BirdTouch',
                            textAlign: TextAlign.center,
                            style: theme.textTheme.displaySmall?.copyWith(
                              fontWeight: FontWeight.w800,
                              color: colors.primary,
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            'Meet people and businesses around you.',
                            textAlign: TextAlign.center,
                            style: theme.textTheme.titleMedium?.copyWith(
                              color: colors.onSurfaceVariant,
                            ),
                          ),
                          const SizedBox(height: 56),
                          FilledButton(
                            onPressed: () => context.push(Routes.signIn),
                            child: const Text('Sign in'),
                          ),
                          Padding(
                            padding: const EdgeInsets.symmetric(vertical: 12),
                            child: Text(
                              'or',
                              textAlign: TextAlign.center,
                              style: theme.textTheme.titleMedium?.copyWith(color: colors.primary),
                            ),
                          ),
                          FilledButton(
                            style: FilledButton.styleFrom(
                              backgroundColor: colors.tertiary,
                              foregroundColor: colors.onTertiary,
                            ),
                            onPressed: () => context.push(Routes.signUp),
                            child: const Text('Sign up'),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
              TextButton.icon(
                onPressed: () => showServerSettings(context),
                icon: const Icon(Icons.dns_outlined, size: 18),
                label: Text(
                  Uri.parse(serverUrl).authority,
                  style: theme.textTheme.bodySmall?.copyWith(color: colors.onSurfaceVariant),
                ),
              ),
              const SizedBox(height: 8),
            ],
          ),
        ),
      ),
    );
  }
}
