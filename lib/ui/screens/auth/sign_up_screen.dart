import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../data/api/api_exception.dart';
import '../../../state/session.dart';
import '../../../utils/validators.dart';
import '../../router.dart';
import '../../widgets/common.dart';
import 'auth_layout.dart';

class SignUpScreen extends ConsumerStatefulWidget {
  const SignUpScreen({super.key});

  @override
  ConsumerState<SignUpScreen> createState() => _SignUpScreenState();
}

enum _Availability { unknown, checking, available, taken }

class _SignUpScreenState extends ConsumerState<SignUpScreen> {
  final _formKey = GlobalKey<FormState>();
  final _username = TextEditingController();
  final _password = TextEditingController();
  final _passwordAgain = TextEditingController();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();

  _Availability _availability = _Availability.unknown;
  Timer? _debounce;
  bool _obscure = true;
  bool _busy = false;
  String? _error;

  /// Field errors returned by the server.
  Map<String, String> _serverErrors = const {};

  @override
  void dispose() {
    _debounce?.cancel();
    for (final c in [_username, _password, _passwordAgain, _firstName, _lastName]) {
      c.dispose();
    }
    super.dispose();
  }

  void _onUsernameChanged(String value) {
    _debounce?.cancel();
    setState(() {
      _availability = _Availability.unknown;
      _serverErrors = {..._serverErrors}..remove('username');
    });
    if (Validators.username(value) != null) return;
    _debounce = Timer(const Duration(milliseconds: 500), () => _checkAvailability(value.trim()));
  }

  Future<void> _checkAvailability(String username) async {
    setState(() => _availability = _Availability.checking);
    try {
      final available = await ref.read(apiProvider).isUsernameAvailable(username);
      if (mounted && _username.text.trim() == username) {
        setState(() => _availability = available ? _Availability.available : _Availability.taken);
      }
    } catch (_) {
      // Not important: registration checks it again.
      if (mounted) setState(() => _availability = _Availability.unknown);
    }
  }

  Future<void> _submit() async {
    setState(() => _serverErrors = const {});
    if (_busy || !_formKey.currentState!.validate()) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref
          .read(sessionProvider.notifier)
          .register(
            username: _username.text,
            password: _password.text,
            firstName: _firstName.text,
            lastName: _lastName.text,
          );
      TextInput.finishAutofillContext();
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        if (e.code == ApiException.usernameTaken) {
          _availability = _Availability.taken;
        } else if (e.code == ApiException.validationError && e.details.isNotEmpty) {
          _serverErrors = {
            for (final issue in e.details)
              if (issue.field != null) issue.field!: issue.message,
          };
        } else {
          _error = e.userMessage;
        }
      });
      _formKey.currentState!.validate();
    } catch (e) {
      if (mounted) setState(() => _error = errorMessage(e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return AuthLayout(
      title: 'Create an account',
      subtitle: 'Your name is shown to people nearby when you are visible.',
      child: Form(
        key: _formKey,
        child: AutofillGroup(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _username,
                decoration: InputDecoration(
                  labelText: 'Username',
                  prefixIcon: const Icon(Icons.alternate_email),
                  helperText: _availability == _Availability.available
                      ? 'Username is available'
                      : 'Letters, digits and . _ @ + -',
                  helperStyle: _availability == _Availability.available
                      ? TextStyle(color: Colors.green.shade700)
                      : null,
                  suffixIcon: switch (_availability) {
                    _Availability.checking => const Padding(
                      padding: EdgeInsets.all(14),
                      child: ButtonProgress(),
                    ),
                    _Availability.available => Icon(
                      Icons.check_circle,
                      color: Colors.green.shade600,
                    ),
                    _Availability.taken => Icon(Icons.cancel, color: colors.error),
                    _Availability.unknown => null,
                  },
                ),
                autofillHints: const [AutofillHints.newUsername],
                autocorrect: false,
                textInputAction: TextInputAction.next,
                onChanged: _onUsernameChanged,
                validator: (v) =>
                    Validators.username(v) ??
                    (_availability == _Availability.taken ? 'Username already exists' : null) ??
                    _serverErrors['username'],
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _password,
                obscureText: _obscure,
                decoration: InputDecoration(
                  labelText: 'Password',
                  prefixIcon: const Icon(Icons.lock_outline),
                  helperText: 'At least 8 characters',
                  suffixIcon: ExcludeFocus(
                    child: IconButton(
                      tooltip: _obscure ? 'Show password' : 'Hide password',
                      icon: Icon(
                        _obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                      ),
                      onPressed: () => setState(() => _obscure = !_obscure),
                    ),
                  ),
                ),
                autofillHints: const [AutofillHints.newPassword],
                textInputAction: TextInputAction.next,
                validator: (v) => Validators.password(v) ?? _serverErrors['password'],
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _passwordAgain,
                obscureText: _obscure,
                decoration: const InputDecoration(
                  labelText: 'Password again',
                  prefixIcon: Icon(Icons.lock_outline),
                ),
                textInputAction: TextInputAction.next,
                validator: (v) => v != _password.text ? 'Passwords do not match' : null,
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _firstName,
                      decoration: const InputDecoration(labelText: 'First name'),
                      autofillHints: const [AutofillHints.givenName],
                      textCapitalization: TextCapitalization.words,
                      textInputAction: TextInputAction.next,
                      validator: (v) => Validators.shortText(v) ?? _serverErrors['firstName'],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextFormField(
                      controller: _lastName,
                      decoration: const InputDecoration(labelText: 'Last name'),
                      autofillHints: const [AutofillHints.familyName],
                      textCapitalization: TextCapitalization.words,
                      textInputAction: TextInputAction.done,
                      validator: (v) => Validators.shortText(v) ?? _serverErrors['lastName'],
                      onFieldSubmitted: (_) => _submit(),
                    ),
                  ),
                ],
              ),
              if (_error != null) FormErrorBanner(message: _error!),
              const SizedBox(height: 28),
              FilledButton(
                style: FilledButton.styleFrom(
                  backgroundColor: colors.tertiary,
                  foregroundColor: colors.onTertiary,
                ),
                onPressed: _busy ? null : _submit,
                child: _busy ? ButtonProgress(color: colors.onTertiary) : const Text('Sign up'),
              ),
              const SizedBox(height: 12),
              TextButton(
                onPressed: () => context.pushReplacement(Routes.signIn),
                child: const Text('Already have an account? Sign in'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
