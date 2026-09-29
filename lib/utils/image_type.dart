import 'dart:typed_data';

/// MIME type of JPEG, PNG or WebP image data, detected from its first bytes, or null for other
/// formats (the server accepts only these three).
String? detectImageType(Uint8List bytes) {
  bool startsWith(List<int> signature, [int offset = 0]) {
    if (bytes.length < offset + signature.length) return false;
    for (var i = 0; i < signature.length; i++) {
      if (bytes[offset + i] != signature[i]) return false;
    }
    return true;
  }

  if (startsWith([0xFF, 0xD8, 0xFF])) return 'image/jpeg';
  if (startsWith([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return 'image/png';
  if (startsWith('RIFF'.codeUnits) && startsWith('WEBP'.codeUnits, 8)) return 'image/webp';
  return null;
}
