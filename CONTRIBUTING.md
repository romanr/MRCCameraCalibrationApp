# Contributing / Developer Guide

This document describes everything a developer needs to know to build, test, and work on the MRCCameraCalibrationApp project.

---

## Prerequisites

### Unity
- **Unity 2022.3.2f1** (exact version recommended to avoid project upgrade issues)
  - Download via [Unity Hub](https://unity.com/download)
  - Required modules: **Android Build Support** (includes Android SDK & NDK Tools and OpenJDK)

### Android Native Plugin (UnityUtilsAar)
- **Android Studio** (latest stable) _or_ a standalone JDK + Android SDK setup
- **JDK 17** – required by Android Gradle Plugin 8.x
- **Android SDK** with the following components installed:
  - SDK Platform **API level 35** (Android 15)
  - Android Emulator (only needed for instrumented tests)
- **Gradle** – a wrapper (`gradlew` / `gradlew.bat`) is included in `UnityUtilsAar/`; no separate installation is required
  > **Linux / macOS first-time setup:** If you get `Permission denied` when running `./gradlew`, make it executable once:
  > ```bash
  > chmod +x UnityUtilsAar/gradlew
  > ```

### Target Device
- Meta Quest 2, Quest Pro, or Quest 3 running **Android 10 (API 29)** or newer

---

## Repository Layout

```
MRCCameraCalibrationApp/
├── Assets/                     # Unity project assets
│   ├── Scripts/                # C# game / app scripts
│   ├── Plugins/Android/        # Pre-built Android AAR placed here by the Gradle build
│   └── ...
├── Packages/                   # Unity Package Manager dependencies
├── ProjectSettings/            # Unity project settings
├── UnityUtilsAar/              # Android native plugin (Gradle project)
│   ├── UnityPlugin/            # Android Library module – produces unityplugin.aar
│   └── app/                   # Android Application module (used for development / testing)
├── README.md
└── CONTRIBUTING.md             # This file
```

---

## Building the Android Plugin

All Gradle commands below are run from the `UnityUtilsAar/` directory.

```bash
cd UnityUtilsAar
```

### Debug Build

Builds a debug AAR and automatically copies it to `Assets/Plugins/Android/unityplugin.aar`.

```bash
./gradlew UnityPlugin:assembleDebug
# Windows:
gradlew.bat UnityPlugin:assembleDebug
```

### Release Build (unsigned)

Builds a release AAR and automatically copies it to `Assets/Plugins/Android/unityplugin.aar`.

> **Note:** The release build type has `minifyEnabled false` and no signing config, so the output is an **unsigned** AAR. No keystore is required.

```bash
./gradlew UnityPlugin:assembleRelease
# Windows:
gradlew.bat UnityPlugin:assembleRelease
```

The output file is placed at:
```
Assets/Plugins/Android/unityplugin.aar
```

After the AAR is updated, open/refresh the Unity project so it picks up the new plugin.

---

## Running Tests

### Unit Tests (JVM – no device required)

Runs the JUnit tests for both the `app` and `UnityPlugin` modules on the local JVM.

```bash
# All modules
./gradlew test

# app module only
./gradlew app:test

# UnityPlugin module only
./gradlew UnityPlugin:test
```

Test reports are written to (paths relative to `UnityUtilsAar/`):
```
<module>/build/reports/tests/testDebugUnitTest/index.html
```

### Instrumented Tests (requires a connected device or running emulator)

Runs the Espresso / AndroidJUnit4 tests on a physical device or emulator.

```bash
# All modules
./gradlew connectedAndroidTest

# app module only
./gradlew app:connectedAndroidTest

# UnityPlugin module only
./gradlew UnityPlugin:connectedAndroidTest
```

Connect a device via USB (or start an Android emulator with API 29+) before running these commands.

Test reports are written to (paths relative to `UnityUtilsAar/`):
```
<module>/build/reports/androidTests/connected/index.html
```

---

## Building the Unity Project

1. Open **Unity Hub** and add the project from the repository root.
2. Ensure the project opens with **Unity 2022.3.2f1**.
3. Go to **File → Build Settings** and select **Android**.
4. Click **Switch Platform** if Android is not already selected.
5. Configure the build:
   - **Texture Compression:** ASTC (recommended for Meta Quest)
   - **Run Device:** select your connected headset or leave as default
6. Click **Build** (or **Build And Run** to deploy directly to the headset).

> The Unity build bundles the AAR from `Assets/Plugins/Android/unityplugin.aar`. Make sure to rebuild the AAR (see above) whenever you change the native plugin code.

---

## Quick-Reference: Common Gradle Tasks

| Task | Command |
|---|---|
| Assemble debug AAR | `./gradlew UnityPlugin:assembleDebug` |
| Assemble release AAR (unsigned) | `./gradlew UnityPlugin:assembleRelease` |
| Run unit tests | `./gradlew test` |
| Run instrumented tests | `./gradlew connectedAndroidTest` |
| Clean build outputs | `./gradlew clean` |
