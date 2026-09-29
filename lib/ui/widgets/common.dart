import 'package:flutter/material.dart';

import '../../data/api/api_exception.dart';
import '../../data/location/location_service.dart';

/// Message to show for an error thrown by the API, the location service, or anything else.
String errorMessage(Object error) => switch (error) {
  ApiException() => error.userMessage,
  LocationException() => error.message,
  _ => 'Something went wrong. Please try again.',
};

void showMessage(BuildContext context, String message, {SnackBarAction? action}) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    // Snack bars with an action stay until dismissed by default; these hide after a while.
    ..showSnackBar(SnackBar(content: Text(message), action: action, persist: false));
}

/// [padding] plus the space of the system navigation (buttons or gesture bar) at the bottom of
/// the screen. Scroll views given an explicit padding don't add it themselves, so without it their
/// last items end up behind the navigation.
EdgeInsets withBottomInset(BuildContext context, EdgeInsets padding) =>
    padding + EdgeInsets.only(bottom: MediaQuery.paddingOf(context).bottom);

void showError(BuildContext context, Object error) => showMessage(context, errorMessage(error));

class BrandLogo extends StatelessWidget {
  const BrandLogo({super.key, this.size = 40});

  final double size;

  @override
  Widget build(BuildContext context) => Image.asset(
    'assets/branding/logo.png',
    width: size,
    height: size,
    filterQuality: FilterQuality.medium,
    semanticLabel: 'BirdTouch',
  );
}

/// Centered icon, title, message and optional action, for empty lists and similar states.
class EmptyState extends StatelessWidget {
  const EmptyState({super.key, required this.icon, required this.title, this.message, this.action});

  final IconData icon;
  final String title;
  final String? message;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: theme.colorScheme.primaryContainer.withValues(alpha: 0.6),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 40, color: theme.colorScheme.onPrimaryContainer),
            ),
            const SizedBox(height: 20),
            Text(title, style: theme.textTheme.titleLarge, textAlign: TextAlign.center),
            if (message != null) ...[
              const SizedBox(height: 8),
              Text(
                message!,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                textAlign: TextAlign.center,
              ),
            ],
            if (action != null) ...[const SizedBox(height: 20), action!],
          ],
        ),
      ),
    );
  }
}

class ErrorState extends StatelessWidget {
  const ErrorState({super.key, required this.error, this.onRetry});

  final Object error;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) => EmptyState(
    icon: error is ApiException && (error as ApiException).isConnectivityProblem
        ? Icons.cloud_off_outlined
        : Icons.error_outline,
    title: 'Could not load',
    message: errorMessage(error),
    action: onRetry == null
        ? null
        : FilledButton.tonalIcon(
            onPressed: onRetry,
            icon: const Icon(Icons.refresh),
            label: const Text('Try again'),
          ),
  );
}

/// A section title in a list, e.g. in settings.
class SectionHeader extends StatelessWidget {
  const SectionHeader(this.title, {super.key});

  final String title;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 24, 20, 8),
      child: Text(
        title,
        style: theme.textTheme.titleSmall?.copyWith(
          color: theme.colorScheme.primary,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

/// Small progress indicator for buttons.
class ButtonProgress extends StatelessWidget {
  const ButtonProgress({super.key, this.color});

  final Color? color;

  @override
  Widget build(BuildContext context) => SizedBox.square(
    dimension: 22,
    child: CircularProgressIndicator(strokeWidth: 2.5, color: color),
  );
}

/// Asks the user to confirm, returns true when they did.
Future<bool> confirm(
  BuildContext context, {
  required String title,
  required String message,
  required String confirmLabel,
  bool destructive = false,
}) async {
  final result = await showDialog<bool>(
    context: context,
    builder: (context) {
      final colors = Theme.of(context).colorScheme;
      return AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          FilledButton(
            style: destructive
                ? FilledButton.styleFrom(
                    backgroundColor: colors.error,
                    foregroundColor: colors.onError,
                    minimumSize: const Size(64, 44),
                  )
                : FilledButton.styleFrom(minimumSize: const Size(64, 44)),
            onPressed: () => Navigator.pop(context, true),
            child: Text(confirmLabel),
          ),
        ],
      );
    },
  );
  return result ?? false;
}
