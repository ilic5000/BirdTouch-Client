import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../config/app_config.dart';
import '../../../data/api/birdtouch_api.dart';
import '../../../state/session.dart';
import '../../../state/settings.dart';
import '../../../utils/server_url.dart';
import '../../widgets/common.dart';

/// Lets the user point the app to another BirdTouch server. Signs the user out if they are
/// signed in, because a login belongs to one server.
Future<void> showServerSettings(BuildContext context) => showModalBottomSheet<void>(
  context: context,
  isScrollControlled: true,
  useSafeArea: true,
  builder: (_) => const SafeArea(top: false, child: _ServerSettingsSheet()),
);

class _ServerSettingsSheet extends ConsumerStatefulWidget {
  const _ServerSettingsSheet();

  @override
  ConsumerState<_ServerSettingsSheet> createState() => _ServerSettingsSheetState();
}

enum _Check { none, checking, ok, failed }

class _ServerSettingsSheetState extends ConsumerState<_ServerSettingsSheet> {
  late final _controller = TextEditingController(text: ref.read(settingsProvider).serverUrl);
  _Check _check = _Check.none;
  String? _error;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<bool> _test() async {
    final url = normalizeServerUrl(_controller.text);
    if (url == null) {
      setState(() {
        _check = _Check.failed;
        _error = 'Enter an address such as http://192.168.1.10:4050';
      });
      return false;
    }
    setState(() {
      _check = _Check.checking;
      _error = null;
    });
    try {
      await BirdTouchApi.checkServer(url);
      if (mounted) setState(() => _check = _Check.ok);
      return true;
    } catch (e) {
      if (mounted) {
        setState(() {
          _check = _Check.failed;
          _error = errorMessage(e);
        });
      }
      return false;
    }
  }

  Future<void> _save() async {
    final url = normalizeServerUrl(_controller.text);
    if (url == null) {
      await _test();
      return;
    }
    if (!await _test() && mounted) {
      final saveAnyway = await confirm(
        context,
        title: 'Server not reachable',
        message: 'BirdTouch could not connect to $url. Save it anyway?',
        confirmLabel: 'Save anyway',
      );
      if (!saveAnyway) return;
    }
    if (url == ref.read(settingsProvider).serverUrl) {
      if (mounted) Navigator.pop(context);
      return;
    }
    if (ref.read(sessionProvider) != null) {
      await ref.read(sessionProvider.notifier).signOut();
    }
    await ref.read(settingsProvider.notifier).setServerUrl(url);
    if (mounted) Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final signedIn = ref.watch(sessionProvider) != null;
    final bottomInset = MediaQuery.viewInsetsOf(context).bottom;

    return Padding(
      padding: EdgeInsets.fromLTRB(24, 0, 24, 24 + bottomInset),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text('Server', style: theme.textTheme.headlineSmall),
          const SizedBox(height: 8),
          Text(
            'Address of the BirdTouch server. '
            'On the Android emulator, your computer is http://10.0.2.2:4050.',
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
          const SizedBox(height: 20),
          TextField(
            controller: _controller,
            keyboardType: TextInputType.url,
            autocorrect: false,
            decoration: InputDecoration(
              labelText: 'Server address',
              hintText: 'http://192.168.1.10:4050',
              prefixIcon: const Icon(Icons.dns_outlined),
              errorText: _error,
              suffixIcon: switch (_check) {
                _Check.checking => const Padding(
                  padding: EdgeInsets.all(14),
                  child: ButtonProgress(),
                ),
                _Check.ok => Icon(Icons.check_circle, color: Colors.green.shade600),
                _Check.failed => Icon(Icons.error, color: theme.colorScheme.error),
                _Check.none => null,
              },
            ),
            onChanged: (_) {
              if (_check != _Check.none) setState(() => _check = _Check.none);
            },
            onSubmitted: (_) => _save(),
          ),
          if (_check == _Check.ok)
            Padding(
              padding: const EdgeInsets.only(top: 8, left: 4),
              child: Text(
                'Connected to a BirdTouch server.',
                style: theme.textTheme.bodySmall?.copyWith(color: Colors.green.shade700),
              ),
            ),
          if (signedIn)
            Padding(
              padding: const EdgeInsets.only(top: 12),
              child: Text(
                'Changing the server signs you out.',
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.error),
              ),
            ),
          const SizedBox(height: 20),
          Row(
            children: [
              TextButton(
                onPressed: () {
                  _controller.text = normalizeServerUrl(AppConfig.defaultServerUrl)!;
                  setState(() => _check = _Check.none);
                },
                child: const Text('Default'),
              ),
              const Spacer(),
              OutlinedButton(
                onPressed: _check == _Check.checking ? null : _test,
                child: const Text('Test'),
              ),
              const SizedBox(width: 12),
              FilledButton(
                onPressed: _check == _Check.checking ? null : _save,
                child: const Text('Save'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
