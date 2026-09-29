import 'dart:typed_data';
import 'dart:ui';

import 'package:image_cropper/image_cropper.dart';
import 'package:image_picker/image_picker.dart';

import '../../config/app_config.dart';

enum PictureSource { gallery, camera }

/// Takes or chooses a picture, then lets the user crop it to a square (e.g. their face out of a
/// bigger photo). The cropped picture is scaled down and compressed on the device.
class PicturePicker {
  const PicturePicker();

  /// The cropped picture as JPEG, or null when the user cancels.
  /// Throws `PlatformException`, e.g. when access to the photos or camera is denied.
  Future<Uint8List?> pick(
    PictureSource source, {
    required Color toolbarColor,
    required Color toolbarForegroundColor,
    required Color accentColor,
  }) async {
    // Not scaled yet, so a small part of a big photo stays sharp after cropping.
    final file = await ImagePicker().pickImage(
      source: source == PictureSource.camera ? ImageSource.camera : ImageSource.gallery,
      preferredCameraDevice: CameraDevice.front,
    );
    if (file == null) return null;

    final cropped = await ImageCropper().cropImage(
      sourcePath: file.path,
      aspectRatio: const CropAspectRatio(ratioX: 1, ratioY: 1),
      maxWidth: AppConfig.pictureMaxDimension.round(),
      maxHeight: AppConfig.pictureMaxDimension.round(),
      compressFormat: ImageCompressFormat.jpg,
      compressQuality: AppConfig.pictureQuality,
      uiSettings: [
        AndroidUiSettings(
          toolbarTitle: 'Crop picture',
          toolbarColor: toolbarColor,
          toolbarWidgetColor: toolbarForegroundColor,
          activeControlsWidgetColor: accentColor,
          statusBarLight: toolbarForegroundColor.computeLuminance() < 0.5,
          cropStyle: CropStyle.circle,
          lockAspectRatio: true,
          hideBottomControls: false,
          aspectRatioPresets: [CropAspectRatioPreset.square],
        ),
        IOSUiSettings(
          title: 'Crop picture',
          cropStyle: CropStyle.circle,
          aspectRatioLockEnabled: true,
          resetAspectRatioEnabled: false,
          aspectRatioPickerButtonHidden: true,
          aspectRatioPresets: [CropAspectRatioPreset.square],
        ),
      ],
    );
    return cropped?.readAsBytes();
  }
}
