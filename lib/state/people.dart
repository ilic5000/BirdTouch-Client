import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config.dart';
import '../data/api/api_exception.dart';
import '../data/models/mode.dart';
import '../data/models/people.dart';
import '../data/models/profile.dart';
import '../data/pictures/picture_picker.dart';
import 'session.dart';
import 'settings.dart';
import 'visibility.dart';

/// Users visible nearby in a mode, nearest first. Empty while the user is hidden in that mode.
///
/// Reloads by itself when the user becomes visible, their location is updated, or the search
/// radius changes.
final nearbyProvider = FutureProvider.family<List<NearbyUser>, Mode>((ref, mode) async {
  final visibility = ref.watch(
    visibilityProvider.select((s) => (visible: s.of(mode).visible, at: s.of(mode).updatedAt)),
  );
  final radiusKm = ref.watch(settingsProvider.select((s) => s.radiusKm));
  if (!visibility.visible) return const [];
  try {
    return await ref
        .watch(apiProvider)
        .nearby(mode, radiusKm: radiusKm, limit: AppConfig.nearbyLimit);
  } on ApiException catch (e) {
    if (e.code == ApiException.notVisible) {
      // Hidden by the server (e.g. no location update for a long time).
      ref.read(visibilityProvider.notifier).markHidden(mode);
      return const [];
    }
    rethrow;
  }
});

final contactsProvider = AsyncNotifierProvider.family<ContactsController, List<Contact>, Mode>(
  ContactsController.new,
);

/// Ids of the saved contacts in a mode, e.g. to show which nearby users are saved.
final savedContactIdsProvider = Provider.family<Set<String>, Mode>((ref, mode) {
  final contacts = ref.watch(contactsProvider(mode)).value ?? const [];
  return {for (final contact in contacts) contact.userId};
});

/// Saved contacts in one mode. The server is the source of truth; changes are shown immediately
/// and rolled back if the server refuses them.
class ContactsController extends AsyncNotifier<List<Contact>> {
  ContactsController(this.mode);

  final Mode mode;

  @override
  Future<List<Contact>> build() async {
    final userId = ref.watch(currentUserIdProvider);
    if (userId == null) return const [];
    return ref.watch(apiProvider).contacts(mode);
  }

  Future<void> save(Person person) async {
    final current = state.value ?? const [];
    if (current.any((c) => c.userId == person.userId)) return;
    state = AsyncData([
      ...current,
      Contact(userId: person.userId, profile: person.profile, savedAt: DateTime.now()),
    ]);
    try {
      await ref.read(apiProvider).saveContact(mode, person.userId);
    } catch (_) {
      _without(person.userId);
      rethrow;
    }
  }

  Future<void> remove(String userId) async {
    final current = state.value ?? const [];
    final index = current.indexWhere((c) => c.userId == userId);
    if (index < 0) return;
    final removed = current[index];
    _without(userId);
    try {
      await ref.read(apiProvider).removeContact(mode, userId);
    } catch (_) {
      if (ref.mounted) {
        final now = [...?state.value];
        now.insert(index.clamp(0, now.length), removed);
        state = AsyncData(now);
      }
      rethrow;
    }
  }

  Future<void> toggle(Person person) async {
    final saved = (state.value ?? const []).any((c) => c.userId == person.userId);
    return saved ? remove(person.userId) : save(person);
  }

  void _without(String userId) {
    if (!ref.mounted) return;
    state = AsyncData([...?state.value?.where((c) => c.userId != userId)]);
  }
}

final picturePickerProvider = Provider<PicturePicker>((ref) => const PicturePicker());

final myProfileProvider = AsyncNotifierProvider.family<MyProfileController, Profile, Mode>(
  MyProfileController.new,
);

/// The logged in user's own profile in one mode.
class MyProfileController extends AsyncNotifier<Profile> {
  MyProfileController(this.mode);

  final Mode mode;

  @override
  Future<Profile> build() async {
    final userId = ref.watch(currentUserIdProvider);
    if (userId == null) return _empty();
    return ref.watch(apiProvider).profile(mode);
  }

  /// Saves the fields of [edited] that changed. Throws `ApiException`.
  Future<void> save(Profile edited) async {
    final current = state.value ?? _empty();
    final changes = current.changesTo(edited);
    if (changes.isEmpty) return;
    final updated = await ref.read(apiProvider).updateProfile(mode, changes);
    if (ref.mounted) state = AsyncData(updated);
  }

  Future<void> uploadPicture(Uint8List bytes, {required String contentType}) async {
    final api = ref.read(apiProvider);
    await api.uploadPicture(mode, bytes, contentType: contentType);
    final updated = await api.profile(mode);
    if (ref.mounted) state = AsyncData(updated);
  }

  Future<void> deletePicture() async {
    final api = ref.read(apiProvider);
    await api.deletePicture(mode);
    final updated = await api.profile(mode);
    if (ref.mounted) state = AsyncData(updated);
  }

  Profile _empty() => switch (mode) {
    Mode.private => const PrivateProfile(),
    Mode.business => const BusinessProfile(),
  };
}
