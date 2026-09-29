import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/models/mode.dart';
import '../../state/session.dart';
import '../../utils/formatters.dart';

/// Round profile picture. Falls back to the initials on a colour derived from the name.
///
/// Pictures need the Authorization header. Their URL changes whenever the picture changes, so
/// they are cached on disk by URL.
class ProfileAvatar extends ConsumerWidget {
  const ProfileAvatar({
    super.key,
    required this.pictureUrl,
    required this.name,
    required this.mode,
    this.radius = 24,
  });

  final String? pictureUrl;
  final String name;
  final Mode mode;
  final double radius;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final placeholder = _Initials(name: name, mode: mode, radius: radius);
    final url = pictureUrl;
    if (url == null) return placeholder;

    final absoluteUrl = ref.watch(apiProvider).resolveUrl(url);
    return ClipOval(
      child: CachedNetworkImage(
        imageUrl: absoluteUrl,
        httpHeaders: ref.watch(pictureHeadersProvider),
        width: radius * 2,
        height: radius * 2,
        fit: BoxFit.cover,
        fadeInDuration: const Duration(milliseconds: 200),
        memCacheWidth: (radius * 2 * MediaQuery.devicePixelRatioOf(context)).round(),
        placeholder: (_, _) => placeholder,
        errorWidget: (_, _, _) => placeholder,
      ),
    );
  }
}

/// Full-width picture for the profile detail header.
class ProfilePicture extends ConsumerWidget {
  const ProfilePicture({super.key, required this.pictureUrl, required this.placeholder});

  final String pictureUrl;
  final Widget placeholder;

  @override
  Widget build(BuildContext context, WidgetRef ref) => CachedNetworkImage(
    imageUrl: ref.watch(apiProvider).resolveUrl(pictureUrl),
    httpHeaders: ref.watch(pictureHeadersProvider),
    fit: BoxFit.cover,
    fadeInDuration: const Duration(milliseconds: 200),
    placeholder: (_, _) => placeholder,
    errorWidget: (_, _, _) => placeholder,
  );
}

class _Initials extends StatelessWidget {
  const _Initials({required this.name, required this.mode, required this.radius});

  final String name;
  final Mode mode;
  final double radius;

  static const _palette = [
    Color(0xFF7E57C2),
    Color(0xFFEC407A),
    Color(0xFF26A69A),
    Color(0xFF5C6BC0),
    Color(0xFFFFA726),
    Color(0xFF42A5F5),
    Color(0xFF8D6E63),
    Color(0xFF66BB6A),
  ];

  @override
  Widget build(BuildContext context) {
    // A stable hash, so a person keeps their colour across app starts.
    final color = _palette[name.codeUnits.fold(0, (a, b) => a + b) % _palette.length];
    return CircleAvatar(
      radius: radius,
      backgroundColor: color,
      foregroundColor: Colors.white,
      child: name.trim().isEmpty
          ? Icon(mode == Mode.private ? Icons.person : Icons.business_center, size: radius)
          : Text(
              initialsOf(name),
              style: TextStyle(fontSize: radius * 0.72, fontWeight: FontWeight.w600),
            ),
    );
  }
}
