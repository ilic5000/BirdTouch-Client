import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/models/mode.dart';
import '../../state/people.dart';
import '../widgets/common.dart';
import '../widgets/person_tile.dart';

/// Saved private and business contacts. They are stored on the server, so they are the same on
/// every device.
class ContactsScreen extends ConsumerStatefulWidget {
  const ContactsScreen({super.key});

  @override
  ConsumerState<ContactsScreen> createState() => _ContactsScreenState();
}

class _ContactsScreenState extends ConsumerState<ContactsScreen> {
  Mode _mode = Mode.private;

  @override
  Widget build(BuildContext context) {
    final contacts = ref.watch(contactsProvider(_mode));
    int? count(Mode mode) => ref.watch(contactsProvider(mode)).value?.length;

    return Scaffold(
      appBar: AppBar(title: const Text('Saved')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
            child: SizedBox(
              width: double.infinity,
              child: SegmentedButton<Mode>(
                segments: [
                  for (final (mode, label, icon) in [
                    (Mode.private, 'Private', Icons.people_outline),
                    (Mode.business, 'Business', Icons.business_center_outlined),
                  ])
                    ButtonSegment(
                      value: mode,
                      icon: Icon(icon),
                      label: Text(count(mode) == null ? label : '$label (${count(mode)})'),
                    ),
                ],
                selected: {_mode},
                showSelectedIcon: false,
                onSelectionChanged: (selection) => setState(() => _mode = selection.single),
              ),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => ref.refresh(contactsProvider(_mode).future),
              child: switch (contacts) {
                AsyncValue(:final value?, hasError: false) when value.isEmpty => _scrollable(
                  EmptyState(
                    icon: Icons.bookmark_border,
                    title: _mode == Mode.private
                        ? 'There are no saved private users at the moment.'
                        : 'There are no saved business users at the moment.',
                    message:
                        'Tap the bookmark next to anyone nearby to save them, so you can '
                        'find them anytime.',
                  ),
                ),
                AsyncValue(:final value?, hasError: false) => ListView.builder(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.only(bottom: 24),
                  itemCount: value.length,
                  // Newest first.
                  itemBuilder: (_, i) {
                    final contact = value[value.length - 1 - i];
                    return Dismissible(
                      key: ValueKey('${contact.mode.name}-${contact.userId}'),
                      direction: DismissDirection.endToStart,
                      background: Container(
                        alignment: Alignment.centerRight,
                        padding: const EdgeInsets.only(right: 24),
                        color: Theme.of(context).colorScheme.errorContainer,
                        child: Icon(
                          Icons.bookmark_remove_outlined,
                          color: Theme.of(context).colorScheme.onErrorContainer,
                        ),
                      ),
                      onDismissed: (_) => toggleSaved(context, ref, contact),
                      child: PersonTile(person: contact),
                    );
                  },
                ),
                AsyncValue(:final error?) => _scrollable(
                  ErrorState(error: error, onRetry: () => ref.invalidate(contactsProvider(_mode))),
                ),
                _ => const Center(child: CircularProgressIndicator()),
              },
            ),
          ),
        ],
      ),
    );
  }

  /// Keeps pull-to-refresh working when the content doesn't scroll.
  Widget _scrollable(Widget child) => LayoutBuilder(
    builder: (context, constraints) => SingleChildScrollView(
      physics: const AlwaysScrollableScrollPhysics(),
      child: ConstrainedBox(
        constraints: BoxConstraints(minHeight: constraints.maxHeight),
        child: child,
      ),
    ),
  );
}
