import 'package:flutter/material.dart';

import '../widgets/common.dart';

/// Short introduction shown the first time a user opens the app (replaces the old showcase tour).
Future<void> showOnboarding(BuildContext context) => showModalBottomSheet<void>(
  context: context,
  isScrollControlled: true,
  useSafeArea: true,
  builder: (context) => const SafeArea(top: false, child: _OnboardingSheet()),
);

class _OnboardingSheet extends StatelessWidget {
  const _OnboardingSheet();

  static const _items = [
    (
      Icons.visibility_outlined,
      'Go visible',
      'Make yourself visible to others and discover BirdTouch users nearby. '
          'Remember: if you are not visible, they cannot find you, and you cannot find them.',
    ),
    (Icons.people_outline, 'Private users', 'People who use BirdTouch for social purposes.'),
    (
      Icons.business_center_outlined,
      'Business users',
      'People who use BirdTouch for business. If someone uses both, here you see their business '
          'info.',
    ),
    (
      Icons.bookmarks_outlined,
      'Save contacts',
      'Save people to find them later, anytime, even when they are not around.',
    ),
    (
      Icons.edit_outlined,
      'Update your information',
      'Fill in your private and business profiles under "Me" so others know who you are.',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(24, 0, 24, 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Center(child: BrandLogo(size: 56)),
          const SizedBox(height: 16),
          Text(
            'Welcome to BirdTouch',
            textAlign: TextAlign.center,
            style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 20),
          for (final (icon, title, text) in _items)
            Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  CircleAvatar(
                    backgroundColor: theme.colorScheme.primaryContainer,
                    foregroundColor: theme.colorScheme.onPrimaryContainer,
                    child: Icon(icon),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(title, style: theme.textTheme.titleMedium),
                        const SizedBox(height: 2),
                        Text(
                          text,
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: theme.colorScheme.onSurfaceVariant,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          const SizedBox(height: 8),
          FilledButton(onPressed: () => Navigator.pop(context), child: const Text('Got it')),
        ],
      ),
    );
  }
}
