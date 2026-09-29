import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../data/models/people.dart';
import '../../data/models/profile.dart';
import '../../state/people.dart';
import '../../utils/formatters.dart';
import '../router.dart';
import 'common.dart';
import 'profile_avatar.dart';

/// A nearby user or contact in a list, with a button to save or remove them.
class PersonTile extends ConsumerWidget {
  const PersonTile({super.key, required this.person});

  final Person person;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final profile = person.profile;
    final saved = ref.watch(savedContactIdsProvider(person.mode)).contains(person.userId);
    final name = displayNameOf(profile);
    final subtitle = _subtitle(profile);
    final distance = switch (person) {
      NearbyUser(:final distanceKm) => formatDistanceKm(distanceKm),
      Contact() => null,
    };

    return ListTile(
      contentPadding: const EdgeInsets.fromLTRB(20, 6, 8, 6),
      leading: Hero(
        tag: 'avatar-${person.mode.name}-${person.userId}',
        child: ProfileAvatar(
          pictureUrl: profile.pictureUrl,
          name: profile.displayName,
          mode: person.mode,
          radius: 26,
        ),
      ),
      title: Text(name, maxLines: 1, overflow: TextOverflow.ellipsis),
      subtitle: subtitle == null && distance == null
          ? null
          : Text([?distance, ?subtitle].join(' · '), maxLines: 2, overflow: TextOverflow.ellipsis),
      titleTextStyle: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
      trailing: IconButton(
        tooltip: saved ? 'Remove from saved' : 'Save contact',
        isSelected: saved,
        selectedIcon: Icon(Icons.bookmark, color: theme.colorScheme.tertiary),
        icon: const Icon(Icons.bookmark_add_outlined),
        onPressed: () => toggleSaved(context, ref, person),
      ),
      onTap: () => context.push(Routes.person(person), extra: person),
    );
  }

  static String? _subtitle(Profile profile) {
    final text = switch (profile) {
      PrivateProfile(:final description) => description,
      BusinessProfile(:final description, :final website, :final address) =>
        description ?? website ?? address,
    };
    return text?.replaceAll(RegExp(r'\s+'), ' ');
  }
}

/// Name to show for a profile, with a fallback for profiles without one.
String displayNameOf(Profile profile) {
  final name = profile.displayName;
  if (name.isNotEmpty) return name;
  return switch (profile) {
    PrivateProfile() => 'Unnamed user',
    BusinessProfile() => 'Unnamed company',
  };
}

/// Saves or removes [person] from the contacts, telling the user what happened.
Future<void> toggleSaved(BuildContext context, WidgetRef ref, Person person) async {
  final controller = ref.read(contactsProvider(person.mode).notifier);
  final wasSaved = ref.read(savedContactIdsProvider(person.mode)).contains(person.userId);
  final name = displayNameOf(person.profile);
  final messenger = ScaffoldMessenger.of(context);
  try {
    await controller.toggle(person);
    messenger
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(wasSaved ? '$name removed from saved' : '$name saved'),
          action: wasSaved
              ? SnackBarAction(label: 'Undo', onPressed: () => controller.save(person))
              : null,
          // With an action, a snack bar would otherwise stay until dismissed.
          persist: false,
        ),
      );
  } catch (e) {
    messenger
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(errorMessage(e))));
  }
}
