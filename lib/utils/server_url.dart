/// Turns what the user typed into a server base URL, or returns null when it isn't one.
///
/// `188.245.72.17:4050` → `http://188.245.72.17:4050`; a trailing slash is removed. Without a
/// scheme, `http` is assumed because test servers usually don't have TLS.
String? normalizeServerUrl(String input) {
  var text = input.trim();
  if (text.isEmpty) return null;
  if (!text.contains('://')) text = 'http://$text';
  final uri = Uri.tryParse(text);
  if (uri == null || !(uri.isScheme('http') || uri.isScheme('https')) || uri.host.isEmpty) {
    return null;
  }
  var result = uri.replace(query: null, fragment: null).toString();
  while (result.endsWith('/')) {
    result = result.substring(0, result.length - 1);
  }
  // Uri.replace(query: null) keeps an empty '?'.
  if (result.endsWith('?')) result = result.substring(0, result.length - 1);
  return result;
}
