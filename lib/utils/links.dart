/// Social networks a private profile can link to.
enum SocialNetwork {
  facebook('Facebook', 'https://facebook.com/'),
  twitter('X (Twitter)', 'https://x.com/'),
  linkedin('LinkedIn', 'https://www.linkedin.com/in/');

  const SocialNetwork(this.label, this.profilePrefix);

  final String label;
  final String profilePrefix;
}

/// Turns what the user entered into a URL that can be opened.
///
/// `ana.rs` → `https://ana.rs`; for social networks a bare handle such as `@ana` becomes the
/// profile URL. Returns null for blank input.
Uri? webUri(String? value, {SocialNetwork? network}) {
  var text = value?.trim() ?? '';
  if (text.isEmpty) return null;
  if (network != null && !text.contains('.') && !text.contains('/')) {
    text = '${network.profilePrefix}${text.replaceFirst(RegExp('^@'), '')}';
  }
  if (!RegExp(r'^[a-zA-Z][a-zA-Z0-9+.-]*://').hasMatch(text)) text = 'https://$text';
  final uri = Uri.tryParse(text);
  return uri == null || uri.host.isEmpty ? null : uri;
}

Uri? emailUri(String? email) =>
    (email ?? '').trim().isEmpty ? null : Uri(scheme: 'mailto', path: email!.trim());

Uri? phoneUri(String? phone) {
  final digits = (phone ?? '').replaceAll(RegExp(r'[^0-9+*#]'), '');
  return digits.isEmpty ? null : Uri(scheme: 'tel', path: digits);
}

/// Opens the address in the default maps app (or the browser).
Uri? mapsUri(String? address) => (address ?? '').trim().isEmpty
    ? null
    : Uri.https('www.google.com', '/maps/search/', {'api': '1', 'query': address!.trim()});
