# MRCCameraCalibrationApp
This is a reverse-engineered and improved version of Oculus MRC Camera Calibration App (com.oculus.MrcCameraCalibration)

It is aimed to be compatible both with [Oculus PC MRC app](https://developer.oculus.com/downloads/package/mixed-reality-capture-tools/) and [Reality Mixer](https://github.com/fabio914/RealityMixer) and maybe other Mixed Reality applications.

# WORK IN PROGRESS

What is done:
- Ported on newer Unity Engine (2022.3.2f1)
- Added controllers display
- Added passthrough mode for convinience (works best on Quest Pro and upcoming Quest 3)
- Fixed issues with failing `mrc.xml` not saving in other game folders
- Added fallback save to `/sdcard/MRC/mrc.xml` for Android 12+ devices where `Android/data` access is restricted

Current Issues:
- Folder selection window for granting permissions may not work correctly, see [this video](https://www.youtube.com/watch?v=wIoor8jwg9w)
- You will need to put headset into Stand-by and back to Active again (using Power button) after granting folder permission

## Android 12+ File Saving

On Android 12 and later, scoped storage restrictions prevent the app from writing directly to other apps' `Android/data` directories. The following workarounds are applied:

- The app requests `MANAGE_EXTERNAL_STORAGE` permission on startup for the broadest possible file access.
- After saving calibration data, the app also writes `mrc.xml` to `/sdcard/MRC/mrc.xml` (i.e., `/storage/emulated/0/MRC/mrc.xml`) as a fallback.
- If automatic distribution to app directories fails, you can manually copy `/sdcard/MRC/mrc.xml` to the target app's `Android/data/{package}/files/mrc.xml`.

Future:
- Some UI for managing the games permissions we want to calibrate for
