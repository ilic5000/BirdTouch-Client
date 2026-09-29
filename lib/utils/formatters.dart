import 'package:intl/intl.dart';

import '../state/settings.dart' show unlimitedRadiusMeters;

/// `8 m`, `120 m`, `1.2 km`, `15 km`.
String formatDistanceKm(double km) {
  if (km < 0.01) return '< 10 m';
  final meters = (km * 100).round() * 10;
  if (meters < 1000) return '$meters m';
  if (km < 9.95) return '${km.toStringAsFixed(1)} km';
  return '${km.round()} km';
}

/// Search radius in meters: `50 m`, `1 km`, `2.5 km`, `Unlimited`.
String formatRadius(int meters) {
  if (meters == unlimitedRadiusMeters) return 'Unlimited';
  if (meters < 1000) return '$meters m';
  final km = meters / 1000;
  return km == km.roundToDouble() ? '${km.round()} km' : '${km.toStringAsFixed(1)} km';
}

/// `44.81250, 20.46120` (about 1 m precision).
String formatCoordinates(double latitude, double longitude) =>
    '${latitude.toStringAsFixed(5)}, ${longitude.toStringAsFixed(5)}';

/// Reads coordinates typed as `44.8125, 20.4612` (or separated by spaces or a semicolon), as
/// copied from most map apps. Null when they are not valid.
({double latitude, double longitude})? parseCoordinates(String text) {
  final parts = text.trim().split(RegExp(r'\s*[,;\s]\s*'));
  if (parts.length != 2) return null;
  final latitude = double.tryParse(parts[0]);
  final longitude = double.tryParse(parts[1]);
  if (latitude == null || longitude == null) return null;
  if (latitude.abs() > 90 || longitude.abs() > 180) return null;
  return (latitude: latitude, longitude: longitude);
}

/// `just now`, `5 min ago`, `2 h ago`, or a date.
String formatTimeAgo(DateTime time, {DateTime? now}) {
  final diff = (now ?? DateTime.now()).difference(time);
  if (diff.inMinutes < 1) return 'just now';
  if (diff.inHours < 1) return '${diff.inMinutes} min ago';
  if (diff.inDays < 1) return '${diff.inHours} h ago';
  return DateFormat.yMMMd().format(time.toLocal());
}

String formatDate(DateTime date) => DateFormat.yMMMMd().format(date);

/// Up to two initials for an avatar placeholder.
String initialsOf(String name) {
  final parts = name.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
  if (parts.isEmpty) return '?';
  final first = parts.first.characters.first;
  final second = parts.length > 1 ? parts.last.characters.first : '';
  return (first + second).toUpperCase();
}

extension on String {
  // Keeps letters with diacritics or emoji intact.
  Iterable<String> get characters => runes.map(String.fromCharCode);
}
