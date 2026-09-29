import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/api/api_exception.dart';
import '../../data/models/mode.dart';
import '../../data/models/profile.dart';
import '../../data/pictures/picture_picker.dart';
import '../../state/people.dart';
import '../../utils/formatters.dart';
import '../../utils/image_type.dart';
import '../../utils/validators.dart';
import '../widgets/common.dart';
import '../widgets/profile_avatar.dart';

/// Editor of the user's own private or business profile. Only changed fields are sent.
class EditProfileScreen extends ConsumerWidget {
  const EditProfileScreen({super.key, required this.mode});

  final Mode mode;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profile = ref.watch(myProfileProvider(mode));
    final title = mode == Mode.private ? 'Private profile' : 'Business profile';
    return switch (profile) {
      AsyncValue(:final value?) => _ProfileForm(key: ValueKey(mode), initial: value, title: title),
      AsyncValue(:final error?) => Scaffold(
        appBar: AppBar(title: Text(title)),
        body: ErrorState(error: error, onRetry: () => ref.invalidate(myProfileProvider(mode))),
      ),
      _ => Scaffold(
        appBar: AppBar(title: Text(title)),
        body: const Center(child: CircularProgressIndicator()),
      ),
    };
  }
}

/// A text field of the form: JSON key, label, icon and input options.
typedef _Field = ({
  String key,
  String label,
  IconData icon,
  TextInputType keyboard,
  String? Function(String?) validator,
  List<String> autofill,
  int maxLines,
  String? hint,
});

_Field _field(
  String key,
  String label,
  IconData icon, {
  TextInputType keyboard = TextInputType.text,
  String? Function(String?) validator = Validators.shortText,
  List<String> autofill = const [],
  int maxLines = 1,
  String? hint,
}) => (
  key: key,
  label: label,
  icon: icon,
  keyboard: keyboard,
  validator: validator,
  autofill: autofill,
  maxLines: maxLines,
  hint: hint,
);

final _privateSections = <(String, List<_Field>)>[
  (
    'About you',
    [
      _field('firstName', 'First name', Icons.badge_outlined, autofill: [AutofillHints.givenName]),
      _field('lastName', 'Last name', Icons.badge_outlined, autofill: [AutofillHints.familyName]),
      _field(
        'description',
        'Description',
        Icons.notes,
        maxLines: 4,
        validator: Validators.longText,
        hint: 'Short information about yourself',
        keyboard: TextInputType.multiline,
      ),
    ],
  ),
  (
    'Contact',
    [
      _field(
        'email',
        'Email',
        Icons.mail_outline,
        keyboard: TextInputType.emailAddress,
        validator: Validators.optionalEmail,
        autofill: [AutofillHints.email],
      ),
      _field(
        'phoneNumber',
        'Phone number',
        Icons.phone_outlined,
        keyboard: TextInputType.phone,
        autofill: [AutofillHints.telephoneNumber],
      ),
      _field(
        'address',
        'Address',
        Icons.place_outlined,
        keyboard: TextInputType.streetAddress,
        autofill: [AutofillHints.fullStreetAddress],
      ),
    ],
  ),
  (
    'Social networks',
    [
      _field(
        'facebookUrl',
        'Facebook',
        Icons.facebook,
        keyboard: TextInputType.url,
        hint: 'facebook.com/you or your username',
      ),
      _field(
        'twitterUrl',
        'X (Twitter)',
        Icons.alternate_email,
        keyboard: TextInputType.url,
        hint: 'x.com/you or @you',
      ),
      _field(
        'linkedinUrl',
        'LinkedIn',
        Icons.work_outline,
        keyboard: TextInputType.url,
        hint: 'linkedin.com/in/you',
      ),
    ],
  ),
];

final _businessSections = <(String, List<_Field>)>[
  (
    'Company',
    [
      _field(
        'companyName',
        'Company name',
        Icons.business_outlined,
        autofill: [AutofillHints.organizationName],
      ),
      _field(
        'description',
        'Description',
        Icons.notes,
        maxLines: 4,
        validator: Validators.longText,
        hint: 'What does your company do?',
        keyboard: TextInputType.multiline,
      ),
    ],
  ),
  (
    'Contact',
    [
      _field(
        'email',
        'Company email',
        Icons.mail_outline,
        keyboard: TextInputType.emailAddress,
        validator: Validators.optionalEmail,
        autofill: [AutofillHints.email],
      ),
      _field('phoneNumber', 'Phone number', Icons.phone_outlined, keyboard: TextInputType.phone),
      _field(
        'website',
        'Website',
        Icons.language,
        keyboard: TextInputType.url,
        hint: 'www.example.com',
      ),
      _field('address', 'Address', Icons.place_outlined, keyboard: TextInputType.streetAddress),
    ],
  ),
];

class _ProfileForm extends ConsumerStatefulWidget {
  const _ProfileForm({super.key, required this.initial, required this.title});

  final Profile initial;
  final String title;

  @override
  ConsumerState<_ProfileForm> createState() => _ProfileFormState();
}

class _ProfileFormState extends ConsumerState<_ProfileForm> {
  final _formKey = GlobalKey<FormState>();
  late final Mode _mode = widget.initial.mode;
  late final _sections = _mode == Mode.private ? _privateSections : _businessSections;
  late final Map<String, TextEditingController> _controllers;
  DateTime? _dateOfBirth;

  bool _saving = false;
  bool _uploading = false;
  Map<String, String> _serverErrors = const {};

  @override
  void initState() {
    super.initState();
    final json = widget.initial.toJson();
    _controllers = {
      for (final (_, fields) in _sections)
        for (final f in fields) f.key: TextEditingController(text: json[f.key] as String? ?? ''),
    };
    if (widget.initial case PrivateProfile(:final dateOfBirth)) _dateOfBirth = dateOfBirth;
  }

  @override
  void dispose() {
    for (final c in _controllers.values) {
      c.dispose();
    }
    super.dispose();
  }

  String? _text(String key) {
    final value = _controllers[key]!.text.trim();
    return value.isEmpty ? null : value;
  }

  /// The profile as currently entered in the form.
  Profile get _edited => switch (_mode) {
    Mode.private => PrivateProfile(
      firstName: _text('firstName'),
      lastName: _text('lastName'),
      email: _text('email'),
      phoneNumber: _text('phoneNumber'),
      dateOfBirth: _dateOfBirth,
      address: _text('address'),
      description: _text('description'),
      facebookUrl: _text('facebookUrl'),
      twitterUrl: _text('twitterUrl'),
      linkedinUrl: _text('linkedinUrl'),
    ),
    Mode.business => BusinessProfile(
      companyName: _text('companyName'),
      email: _text('email'),
      phoneNumber: _text('phoneNumber'),
      website: _text('website'),
      address: _text('address'),
      description: _text('description'),
    ),
  };

  Profile get _saved => ref.read(myProfileProvider(_mode)).value ?? widget.initial;

  bool get _dirty => _saved.changesTo(_edited).isNotEmpty;

  Future<void> _save() async {
    setState(() => _serverErrors = const {});
    if (_saving || !_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      await ref.read(myProfileProvider(_mode).notifier).save(_edited);
      if (!mounted) return;
      showMessage(context, 'Changes saved successfully.');
      Navigator.of(context).pop();
    } on ApiException catch (e) {
      if (!mounted) return;
      if (e.code == ApiException.validationError && e.details.any((d) => d.field != null)) {
        setState(() {
          _serverErrors = {
            for (final d in e.details)
              if (d.field != null) d.field!: d.message,
          };
        });
        _formKey.currentState!.validate();
      } else {
        showError(context, e);
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _pickDateOfBirth() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _dateOfBirth ?? DateTime(now.year - 25),
      firstDate: DateTime(1900),
      lastDate: now,
      initialEntryMode: DatePickerEntryMode.calendarOnly,
      helpText: 'Date of birth',
    );
    if (picked != null) {
      setState(() => _dateOfBirth = DateTime.utc(picked.year, picked.month, picked.day));
    }
  }

  Future<void> _changePicture() async {
    if (_uploading) return;
    final hasPicture = _saved.pictureUrl != null;
    final action = await showModalBottomSheet<String>(
      context: context,
      useSafeArea: true,
      builder: (context) => SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Choose from gallery'),
              onTap: () => Navigator.pop(context, 'gallery'),
            ),
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Take a photo'),
              onTap: () => Navigator.pop(context, 'camera'),
            ),
            if (hasPicture)
              ListTile(
                leading: Icon(Icons.delete_outline, color: Theme.of(context).colorScheme.error),
                title: Text(
                  'Remove picture',
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
                onTap: () => Navigator.pop(context, 'remove'),
              ),
            const SizedBox(height: 16),
          ],
        ),
      ),
    );
    if (action == null || !mounted) return;

    final controller = ref.read(myProfileProvider(_mode).notifier);
    try {
      if (action == 'remove') {
        setState(() => _uploading = true);
        await controller.deletePicture();
        return;
      }
      final colors = Theme.of(context).colorScheme;
      final bytes = await ref
          .read(picturePickerProvider)
          .pick(
            action == 'camera' ? PictureSource.camera : PictureSource.gallery,
            toolbarColor: colors.surface,
            toolbarForegroundColor: colors.onSurface,
            accentColor: colors.primary,
          );
      if (bytes == null || !mounted) return;
      setState(() => _uploading = true);
      final type = detectImageType(bytes);
      if (type == null) {
        if (mounted) showMessage(context, 'Please choose a JPEG, PNG or WebP picture.');
        return;
      }
      await controller.uploadPicture(bytes, contentType: type);
    } on PlatformException catch (e) {
      if (mounted) {
        showMessage(
          context,
          e.code.contains('access_denied')
              ? 'BirdTouch needs access to your photos or camera to change the picture.'
              : 'Could not get the picture.',
        );
      }
    } catch (e) {
      if (mounted) showError(context, e);
    } finally {
      if (mounted) setState(() => _uploading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final saved = ref.watch(myProfileProvider(_mode)).value ?? widget.initial;
    final edited = _edited;

    return PopScope(
      canPop: !_dirty || _saving,
      onPopInvokedWithResult: (didPop, _) async {
        if (didPop) return;
        final discard = await confirm(
          context,
          title: 'Discard changes?',
          message: 'Your changes have not been saved.',
          confirmLabel: 'Discard',
          destructive: true,
        );
        if (discard && context.mounted) Navigator.of(context).pop();
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(widget.title),
          actions: [
            Padding(
              padding: const EdgeInsets.only(right: 12),
              child: FilledButton(
                style: FilledButton.styleFrom(minimumSize: const Size(88, 40)),
                onPressed: _saving || !_dirty ? null : _save,
                child: _saving ? const ButtonProgress() : const Text('Save'),
              ),
            ),
          ],
        ),
        body: Form(
          key: _formKey,
          onChanged: () => setState(() {}),
          child: ListView(
            padding: withBottomInset(context, const EdgeInsets.fromLTRB(16, 8, 16, 32)),
            children: [
              Center(
                child: Stack(
                  children: [
                    // The whole picture opens the picture options, not only the camera button.
                    GestureDetector(
                      onTap: _changePicture,
                      child: ProfileAvatar(
                        pictureUrl: saved.pictureUrl,
                        name: edited.displayName,
                        mode: _mode,
                        radius: 60,
                      ),
                    ),
                    if (_uploading)
                      const Positioned.fill(
                        child: DecoratedBox(
                          decoration: BoxDecoration(color: Colors.black38, shape: BoxShape.circle),
                          child: Center(child: CircularProgressIndicator(color: Colors.white)),
                        ),
                      ),
                    Positioned(
                      right: 0,
                      bottom: 0,
                      child: IconButton.filled(
                        tooltip: 'Change picture',
                        onPressed: _uploading ? null : _changePicture,
                        icon: const Icon(Icons.photo_camera_outlined),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              _DiscoverabilityBanner(profile: edited),
              for (final (title, fields) in _sections) ...[
                SectionHeader(title),
                for (final f in fields) ...[
                  TextFormField(
                    controller: _controllers[f.key],
                    decoration: InputDecoration(
                      labelText: f.label,
                      hintText: f.hint,
                      prefixIcon: Icon(f.icon),
                      alignLabelWithHint: f.maxLines > 1,
                    ),
                    keyboardType: f.keyboard,
                    autofillHints: f.autofill,
                    maxLines: f.maxLines,
                    minLines: 1,
                    textCapitalization:
                        f.keyboard == TextInputType.text || f.keyboard == TextInputType.multiline
                        ? TextCapitalization.sentences
                        : TextCapitalization.none,
                    autocorrect:
                        f.keyboard == TextInputType.text || f.keyboard == TextInputType.multiline,
                    validator: (v) => f.validator(v) ?? _serverErrors[f.key],
                  ),
                  const SizedBox(height: 12),
                  if (f.key == 'description' && _mode == Mode.private) ...[
                    _DateOfBirthField(
                      value: _dateOfBirth,
                      error: _serverErrors['dateOfBirth'],
                      onTap: _pickDateOfBirth,
                      onClear: () => setState(() => _dateOfBirth = null),
                    ),
                    const SizedBox(height: 12),
                  ],
                ],
              ],
              const SizedBox(height: 8),
              Text(
                _mode == Mode.private
                    ? 'Everything you enter here is shown to people who find you in private mode.'
                    : 'Everything you enter here is shown to people who find you in business mode.',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _DateOfBirthField extends StatelessWidget {
  const _DateOfBirthField({
    required this.value,
    required this.error,
    required this.onTap,
    required this.onClear,
  });

  final DateTime? value;
  final String? error;
  final VoidCallback onTap;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) => InkWell(
    borderRadius: BorderRadius.circular(14),
    onTap: onTap,
    child: InputDecorator(
      isEmpty: value == null,
      decoration: InputDecoration(
        labelText: 'Date of birth',
        prefixIcon: const Icon(Icons.cake_outlined),
        errorText: error,
        suffixIcon: value == null
            ? null
            : IconButton(tooltip: 'Clear', icon: const Icon(Icons.clear), onPressed: onClear),
      ),
      child: Text(value == null ? '' : formatDate(value!)),
    ),
  );
}

/// Tells the user whether others can find this profile, and what is missing if not.
class _DiscoverabilityBanner extends StatelessWidget {
  const _DiscoverabilityBanner({required this.profile});

  final Profile profile;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final ok = profile.isDiscoverable;
    final color = ok ? Colors.green.shade700 : theme.colorScheme.tertiary;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          Icon(ok ? Icons.check_circle_outline : Icons.info_outline, color: color),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              ok
                  ? 'Your profile is complete. People nearby can find you when you are visible.'
                  : 'To be found by others, add ${profile.missingForDiscovery.join(' and ')}.',
              style: theme.textTheme.bodyMedium,
            ),
          ),
        ],
      ),
    );
  }
}
