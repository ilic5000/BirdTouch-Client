import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../data/models/mode.dart';
import '../../data/models/people.dart';
import '../../data/models/profile.dart';
import '../../state/people.dart';
import '../../utils/formatters.dart';
import '../../utils/links.dart';
import '../widgets/common.dart';
import '../widgets/person_tile.dart';
import '../widgets/profile_avatar.dart';

/// Details of another user's profile, with actions to contact them and to save them.
class PersonScreen extends ConsumerWidget {
  const PersonScreen({super.key, required this.mode, required this.userId, this.initial});

  final Mode mode;
  final String userId;

  /// The person as shown in the list that opened this screen.
  final Person? initial;

  /// The freshest version of the person: from the lists if they are there, else [initial].
  Person? _resolve(WidgetRef ref) {
    bool matches(Person p) => p.userId == userId;
    final nearby = ref.watch(nearbyProvider(mode)).value?.where(matches).firstOrNull;
    final contact = ref.watch(contactsProvider(mode)).value?.where(matches).firstOrNull;
    return nearby ?? contact ?? initial;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final person = _resolve(ref);
    if (person == null) {
      return Scaffold(
        appBar: AppBar(),
        body: const EmptyState(
          icon: Icons.person_off_outlined,
          title: 'Not available',
          message: 'This person is no longer nearby or saved.',
        ),
      );
    }

    final saved = ref.watch(savedContactIdsProvider(mode)).contains(userId);
    final profile = person.profile;
    final name = displayNameOf(profile);
    final theme = Theme.of(context);
    final distance = switch (person) {
      NearbyUser(:final distanceKm) => formatDistanceKm(distanceKm),
      Contact() => null,
    };

    return Scaffold(
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => toggleSaved(context, ref, person),
        backgroundColor: saved ? theme.colorScheme.tertiaryContainer : theme.colorScheme.tertiary,
        foregroundColor: saved
            ? theme.colorScheme.onTertiaryContainer
            : theme.colorScheme.onTertiary,
        icon: Icon(saved ? Icons.bookmark : Icons.bookmark_add_outlined),
        label: Text(saved ? 'Saved' : 'Save contact'),
      ),
      body: CustomScrollView(
        slivers: [
          _Header(person: person, name: name),
          SliverPadding(
            // Room for the save button.
            padding: withBottomInset(context, const EdgeInsets.fromLTRB(16, 16, 16, 100)),
            sliver: SliverList.list(
              children: [
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      if (distance != null)
                        Chip(
                          avatar: const Icon(Icons.near_me_outlined, size: 18),
                          label: Text('$distance away'),
                        ),
                      Chip(
                        avatar: Icon(
                          mode == Mode.private
                              ? Icons.person_outline
                              : Icons.business_center_outlined,
                          size: 18,
                        ),
                        label: Text(mode == Mode.private ? 'Private' : 'Business'),
                      ),
                    ],
                  ),
                ),
                _QuickActions(profile: profile),
                if ((profile.description ?? '').isNotEmpty) ...[
                  const SizedBox(height: 16),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'About',
                            style: theme.textTheme.titleSmall?.copyWith(
                              color: theme.colorScheme.primary,
                            ),
                          ),
                          const SizedBox(height: 8),
                          SelectableText(profile.description!, style: theme.textTheme.bodyLarge),
                        ],
                      ),
                    ),
                  ),
                ],
                const SizedBox(height: 16),
                _Details(profile: profile),
                if (profile case final PrivateProfile p when _hasSocial(p)) ...[
                  const SizedBox(height: 16),
                  _Social(profile: p),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  static bool _hasSocial(PrivateProfile p) =>
      [p.facebookUrl, p.twitterUrl, p.linkedinUrl].any((v) => (v ?? '').isNotEmpty);
}

class _Header extends StatelessWidget {
  const _Header({required this.person, required this.name});

  final Person person;
  final String name;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final pictureUrl = person.profile.pictureUrl;
    final placeholder = DecoratedBox(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [colors.primary, colors.tertiary],
        ),
      ),
      child: Center(
        child: Hero(
          tag: 'avatar-${person.mode.name}-${person.userId}',
          child: ProfileAvatar(
            pictureUrl: null,
            name: person.profile.displayName,
            mode: person.mode,
            radius: 64,
          ),
        ),
      ),
    );

    return SliverAppBar(
      expandedHeight: 340,
      pinned: true,
      stretch: true,
      foregroundColor: Colors.white,
      backgroundColor: colors.primary,
      systemOverlayStyle: SystemUiOverlayStyle.light,
      flexibleSpace: FlexibleSpaceBar(
        title: Text(
          name,
          style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700),
        ),
        stretchModes: const [StretchMode.zoomBackground],
        background: Stack(
          fit: StackFit.expand,
          children: [
            if (pictureUrl == null)
              placeholder
            else
              ProfilePicture(pictureUrl: pictureUrl, placeholder: placeholder),
            // Scrim, so the white title stays readable on light pictures.
            const DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [Colors.black38, Colors.transparent, Colors.transparent, Colors.black54],
                  stops: [0, 0.25, 0.6, 1],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _QuickActions extends StatelessWidget {
  const _QuickActions({required this.profile});

  final Profile profile;

  @override
  Widget build(BuildContext context) {
    final website = switch (profile) {
      BusinessProfile(:final website) => webUri(website),
      PrivateProfile() => null,
    };
    final actions = [
      (Icons.call_outlined, 'Call', phoneUri(profile.phoneNumber)),
      (Icons.mail_outline, 'Email', emailUri(profile.email)),
      if (website != null) (Icons.language, 'Website', website),
      (Icons.map_outlined, 'Map', mapsUri(profile.address)),
    ].where((a) => a.$3 != null).toList();
    if (actions.isEmpty) return const SizedBox.shrink();

    return Row(
      children: [
        for (final (icon, label, uri) in actions)
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 4),
              child: FilledButton.tonal(
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 12),
                  minimumSize: const Size(0, 64),
                ),
                onPressed: () => openUri(context, uri!),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(icon),
                    const SizedBox(height: 4),
                    Text(label, style: const TextStyle(fontSize: 13)),
                  ],
                ),
              ),
            ),
          ),
      ],
    );
  }
}

class _Details extends StatelessWidget {
  const _Details({required this.profile});

  final Profile profile;

  @override
  Widget build(BuildContext context) {
    final rows = <(IconData, String, String?, Uri?)>[
      if (profile case PrivateProfile(:final firstName?))
        (Icons.badge_outlined, 'First name', firstName, null),
      if (profile case PrivateProfile(:final lastName?))
        (Icons.badge_outlined, 'Last name', lastName, null),
      if (profile case BusinessProfile(:final companyName?))
        (Icons.business_outlined, 'Company name', companyName, null),
      (Icons.mail_outline, 'Email', profile.email, emailUri(profile.email)),
      (Icons.phone_outlined, 'Phone number', profile.phoneNumber, phoneUri(profile.phoneNumber)),
      if (profile case BusinessProfile(:final website?))
        (Icons.language, 'Website', website, webUri(website)),
      (Icons.place_outlined, 'Address', profile.address, mapsUri(profile.address)),
      if (profile case PrivateProfile(:final dateOfBirth?))
        (Icons.cake_outlined, 'Date of birth', formatDate(dateOfBirth), null),
    ].where((r) => (r.$3 ?? '').isNotEmpty).toList();

    if (rows.isEmpty) return const SizedBox.shrink();
    return Card(
      child: Column(
        children: [
          for (final (icon, label, value, uri) in rows)
            ListTile(
              leading: Icon(icon),
              title: Text(value!),
              subtitle: Text(label),
              trailing: uri == null ? null : const Icon(Icons.open_in_new, size: 18),
              onTap: uri == null ? null : () => openUri(context, uri),
              onLongPress: () {
                Clipboard.setData(ClipboardData(text: value));
                showMessage(context, '$label copied');
              },
            ),
        ],
      ),
    );
  }
}

class _Social extends StatelessWidget {
  const _Social({required this.profile});

  final PrivateProfile profile;

  @override
  Widget build(BuildContext context) {
    final links = [
      (SocialNetwork.facebook, Icons.facebook, profile.facebookUrl),
      (SocialNetwork.twitter, Icons.alternate_email, profile.twitterUrl),
      (SocialNetwork.linkedin, Icons.work_outline, profile.linkedinUrl),
    ];
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (final (network, icon, value) in links)
          if (webUri(value, network: network) case final uri?)
            ActionChip(
              avatar: Icon(icon, size: 18),
              label: Text(network.label),
              onPressed: () => openUri(context, uri),
            ),
      ],
    );
  }
}

/// Opens a link in the app that handles it (browser, phone, mail, maps).
Future<void> openUri(BuildContext context, Uri uri) async {
  final messenger = ScaffoldMessenger.of(context);
  try {
    if (await launchUrl(uri, mode: LaunchMode.externalApplication)) return;
  } catch (_) {
    // Falls through to the message.
  }
  messenger.showSnackBar(SnackBar(content: Text('No app can open $uri')));
}
