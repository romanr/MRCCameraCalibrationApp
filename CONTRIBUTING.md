# Contributing / Developer Guide

This document describes everything a developer needs to know to build, test, and work on the Mixed Reality Counterpart project.

---

## Prerequisites

### Unity
- **Unity 2022.3.2f1** (exact version recommended to avoid project upgrade issues)
  - Download via [Unity Hub](https://unity.com/download)
  - Required modules: **Android Build Support** (includes Android SDK & NDK Tools and OpenJDK)

### Android Native App & Plugin (MixedRealityConfigPart)
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
  >
  > **SDK Setup:** Gradle needs to know where your Android SDK is located. Create a `local.properties` file in the `UnityUtilsAar/` directory:
  > ```bash
  > echo "sdk.dir=/Users/roman/Library/Android/sdk" > UnityUtilsAar/local.properties
  > ```
  > *(On macOS, the default SDK path is usually `~/Library/Android/sdk`, $HOME does not work)*

### Target Device
- Meta Quest 2, Quest Pro, or Quest 3 running **Android 15 (API 35)** or newer

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
├── UnityUtilsAar/              # Android project root
│   ├── UnityPlugin/            # Android Library module – produces MixedRealityConfigPart.aar
│   └── app/                   # Android Application module – "Mixed Reality Counterpart"
├── README.md
└── CONTRIBUTING.md             # This file
```

---

## Building the Android App/Plugin

All Gradle commands below are run from the `UnityUtilsAar/` directory.

```bash
cd UnityUtilsAar
```

### Build the Android App (APK)
The application "Mixed Reality Counterpart" (package `com.insbyte.MixedRealityConfigPart`) can be built for the device.

**Debug Build:**
```bash
./gradlew app:assembleDebug
```
Output: `app/build/outputs/apk/debug/MixedRealityConfigPart-debug.apk`

**Release Build (Unsigned):**
```bash
./gradlew app:assembleRelease
```
Output: `app/build/outputs/apk/release/MixedRealityConfigPart-release-unsigned.apk`

### Build the Unity Plugin (AAR)
Builds a debug AAR and automatically copies it to the Unity project.

**Debug Build:**
```bash
./gradlew UnityPlugin:assembleDebug
```
Output: `Assets/Plugins/Android/MixedRealityConfigPart.aar`

**Release Build:**
```bash
./gradlew UnityPlugin:assembleRelease
```
Output: `Assets/Plugins/Android/MixedRealityConfigPart.aar`

---

## Running Tests

### Unit Tests (JVM – no device required)

Runs the JUnit tests for both the `app` and `UnityPlugin` modules on the local JVM.

```bash
# All modules
./gradlew test
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
```

Connect a device via USB (or start an Android emulator with API 29+) before running these commands.

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

> The Unity build bundles the AAR from `Assets/Plugins/Android/MixedRealityConfigPart.aar`. Make sure to rebuild the AAR (see above) whenever you change the native plugin code.

---

## Quick-Reference: Common Gradle Tasks

| Task | Command |
|---|---|
| Build & Install App | `./gradlew app:installDebug` |
| Assemble App APK | `./gradlew app:assembleDebug` |
| Assemble Plugin AAR | `./gradlew UnityPlugin:assembleDebug` |
| Run unit tests | `./gradlew test` |
| Run instrumented tests | `./gradlew connectedAndroidTest` |
| Clean build outputs | `./gradlew clean` |
