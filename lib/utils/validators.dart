/// Form validators that mirror the server rules, so users see problems before sending the form.
/// Each returns an error message, or null when the value is valid.
abstract final class Validators {
  static final _usernamePattern = RegExp(r'^[A-Za-z0-9._@+-]+$');

  // Pragmatic check; the server does the full validation.
  static final _emailPattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  static const shortTextMax = 200;
  static const longTextMax = 2000;

  static String? username(String? value) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) return 'Enter a username';
    if (text.length < 3) return 'At least 3 characters';
    if (text.length > 64) return 'At most 64 characters';
    if (!_usernamePattern.hasMatch(text)) return 'Only letters, digits and . _ @ + -';
    return null;
  }

  static String? password(String? value) {
    final text = value ?? '';
    if (text.isEmpty) return 'Enter a password';
    if (text.length < 8) return 'At least 8 characters';
    if (text.length > 128) return 'At most 128 characters';
    return null;
  }

  static String? required(String? value, [String message = 'Required']) =>
      (value?.trim() ?? '').isEmpty ? message : null;

  static String? optionalEmail(String? value) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) return null;
    if (text.length > shortTextMax) return 'At most $shortTextMax characters';
    return _emailPattern.hasMatch(text) ? null : 'Enter a valid email address';
  }

  static String? shortText(String? value) =>
      (value?.trim().length ?? 0) > shortTextMax ? 'At most $shortTextMax characters' : null;

  static String? longText(String? value) =>
      (value?.trim().length ?? 0) > longTextMax ? 'At most $longTextMax characters' : null;
}
