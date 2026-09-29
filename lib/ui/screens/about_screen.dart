import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../../config/app_config.dart';
import '../../utils/links.dart';
import '../widgets/common.dart';
import 'person_screen.dart' show openUri;

final _packageInfoProvider = FutureProvider<PackageInfo>((ref) => PackageInfo.fromPlatform());

class AboutScreen extends ConsumerWidget {
  const AboutScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final info = ref.watch(_packageInfoProvider).value;
    final version = info == null ? '' : '${info.version} (${info.buildNumber})';

    return Scaffold(
      appBar: AppBar(title: const Text('About')),
      body: ListView(
        padding: withBottomInset(context, const EdgeInsets.only(bottom: 32)),
        children: [
          const SizedBox(height: 16),
          const Center(child: BrandLogo(size: 96)),
          const SizedBox(height: 16),
          Text(
            'BirdTouch',
            textAlign: TextAlign.center,
            style: theme.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w800),
          ),
          if (version.isNotEmpty)
            Text(
              'Version $version',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          Padding(
            padding: const EdgeInsets.fromLTRB(32, 20, 32, 8),
            child: Text(
              'Make yourself visible to people around you, and see who else is nearby. '
              'Use the private mode to meet people, and the business mode to find companies and '
              'business contacts.',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyLarge,
            ),
          ),
          const SectionHeader('Author'),
          ListTile(
            leading: const Icon(Icons.person_outline),
            title: const Text(AppConfig.authorName),
            subtitle: const Text(AppConfig.authorEmail),
            trailing: const Icon(Icons.mail_outline),
            onTap: () => openUri(context, emailUri(AppConfig.authorEmail)!),
          ),
          const SectionHeader('Source code'),
          ListTile(
            leading: const Icon(Icons.phone_android),
            title: const Text('BirdTouch app'),
            subtitle: const Text(AppConfig.sourceCodeUrl),
            trailing: const Icon(Icons.open_in_new, size: 18),
            onTap: () => openUri(context, Uri.parse(AppConfig.sourceCodeUrl)),
          ),
          ListTile(
            leading: const Icon(Icons.dns_outlined),
            title: const Text('BirdTouch server'),
            subtitle: const Text(AppConfig.serverSourceCodeUrl),
            trailing: const Icon(Icons.open_in_new, size: 18),
            onTap: () => openUri(context, Uri.parse(AppConfig.serverSourceCodeUrl)),
          ),
          ListTile(
            leading: const Icon(Icons.description_outlined),
            title: const Text('Open source licenses'),
            onTap: () => showLicensePage(
              context: context,
              applicationName: 'BirdTouch',
              applicationVersion: version,
              applicationIcon: const Padding(
                padding: EdgeInsets.all(12),
                child: BrandLogo(size: 64),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
