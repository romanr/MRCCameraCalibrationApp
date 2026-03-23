# Copilot Instructions for MRCCameraCalibrationApp

## Project Overview

This is a **Unity 2022.3.2f1** project targeting **Meta Quest (Android/XR)** devices. It is a reverse-engineered and improved version of the Oculus MRC Camera Calibration App (`com.oculus.MrcCameraCalibration`). The app enables users to calibrate a mixed-reality capture (MRC) camera for use with tools such as the [Oculus PC MRC app](https://developer.oculus.com/downloads/package/mixed-reality-capture-tools/) and [Reality Mixer](https://github.com/fabio914/RealityMixer).

The repository contains two distinct sub-projects:

1. **Unity Project** (root) — The main app targeting Meta Quest, built with Unity 2022.3.2f1.
2. **`UnityUtilsAar/`** — An Android Studio / Gradle project that produces a native Android AAR (`unityplugin.aar`) bundled into the Unity project.

---

## Repository Structure

```
MRCCameraCalibrationApp/
├── Assets/
│   ├── Editor/
│   │   └── BuildProcessor.cs          # Unity build pre-processor (sets managed debugger port to 50000)
│   ├── Plugins/Android/               # AAR output location (unityplugin.aar goes here)
│   ├── Scripts/
│   │   ├── CalibrationNetworkServer.cs # Core TCP server; communicates calibration data over port 25671
│   │   ├── Global.cs                   # MonoBehaviour showing IP address and connection status
│   │   ├── MRCNetworkDiscovery.cs      # UDP broadcast for network discovery on port 47898
│   │   ├── BufferedAudioStream.cs      # Audio streaming helper
│   │   └── TimerToSwitchScene.cs       # Simple timed scene-switch utility
│   ├── Oculus/                         # Oculus SDK assets (VR and Platform SDKs)
│   ├── Scenes/                         # Unity scenes
│   └── XR/                             # XR configuration (Oculus Loader, settings)
├── Packages/
│   └── manifest.json                   # Unity package manifest
├── ProjectSettings/
│   └── ProjectVersion.txt             # Unity version: 2022.3.2f1
├── UnityUtilsAar/                      # Android Studio project for the native plugin
│   ├── UnityPlugin/                    # The AAR library module
│   │   ├── build.gradle                # Builds AAR; post-build copies it to Assets/Plugins/Android/unityplugin.aar
│   │   └── src/main/java/com/insbyte/unityplugin/
│   │       ├── PluginInstance.java     # File-system access via DocumentFile API (Android SAF)
│   │       └── DeviceActivity.java     # Activity for folder-picker (ACTION_OPEN_DOCUMENT_TREE)
│   ├── app/                            # Companion Android app module (not shipped; for testing)
│   ├── build.gradle                    # Top-level Gradle config (AGP 7.4.2)
│   └── settings.gradle
├── .gitignore                          # Standard Unity .gitignore
└── README.md
```

---

## Key Technologies & Dependencies

| Layer | Technology |
|---|---|
| Game Engine | Unity 2022.3.2f1 |
| Target Platform | Android (Meta Quest / Oculus) |
| XR SDK | `com.unity.xr.oculus` 4.0.0, `com.unity.xr.management` 4.4.0 |
| Networking (Unity) | `com.unity.netcode.gameobjects` 1.4.0, `OVRNetwork` (from Oculus SDK) |
| Native Android Plugin | Java AAR built with AGP 7.4.2, `compileSdk`/`targetSdk` 32, `minSdk` 29 |
| Android Storage | Storage Access Framework (SAF) via `DocumentFile` / `DocumentsContract` |
| UI | TextMeshPro 3.0.6 |

---

## Building

### Unity App (APK / AAB)

Open the project in **Unity 2022.3.2f1**. The target platform is **Android**. To build:

- Use **File → Build Settings → Android → Build**.
- `BuildProcessor.cs` runs automatically before each build and sets `EditorUserBuildSettings.managedDebuggerFixedPort = 50000`.
- The compiled APK/AAB output is excluded from version control by `.gitignore`.

### Native Android AAR (`UnityUtilsAar/`)

The AAR must be built **before** opening or building the Unity project if plugin changes are needed.

```bash
cd UnityUtilsAar
./gradlew :UnityPlugin:assembleDebug    # or assembleRelease
```

After a successful build, Gradle automatically copies and renames the output to:

```
Assets/Plugins/Android/unityplugin.aar
```

This is handled by the `copyAARDebug` / `copyAARRelease` tasks in `UnityUtilsAar/UnityPlugin/build.gradle` (lines 39–58), which are `finalizedBy` the respective `assemble*` tasks.

**No manual copy step is needed.**

---

## Architecture & Important Patterns

### Calibration Protocol

`CalibrationNetworkServer.cs` hosts a TCP server on port **25671**. It communicates with the Oculus PC MRC tool using a custom binary protocol. Message type constants (31–40) are defined as private `const int` fields in the class. The two compile-time feature flags at the top are:

```csharp
//#define ENABLE_MRC_IN_APP   // enables in-app MRC rendering
//#define ENABLE_QUEST_STORE  // enables Oculus Platform entitlement check
```

Both are commented out by default. Toggle as needed.

### Network Discovery

`MRCNetworkDiscovery.cs` broadcasts UDP packets on port **47898** every second. The broadcast message encodes the device name/model and uses big-endian integers. Call `StartBroadcast()` / `StopBroadcast()` to control it.

### Android File Access (SAF)

`PluginInstance.java` wraps Android's **Storage Access Framework** (SAF). Key points:

- `initialize(Activity)` must be called from Unity before any other method.
- `askForAccess(String path)` launches `DeviceActivity` (a `ComponentActivity`) which presents the system folder picker (`ACTION_OPEN_DOCUMENT_TREE`).
- `copyFile(from, to)` and `deleteFile(path)` operate via `DocumentFile` using persisted URI permissions.
- **Known issue**: The folder-selection window may not work correctly on all devices. After granting folder permission, put the headset into standby and back to active (power button) to reload permissions.

### `.meta` Files

Unity requires a `.meta` file alongside every asset. **Always commit `.meta` files** when adding or renaming assets. Missing `.meta` files will cause Unity to regenerate them with new GUIDs, potentially breaking scene/prefab references.

---

## Known Issues & Workarounds

1. **`mrc.xml` saving broken (as of April 2024)**: The app can no longer save `mrc.xml` due to Android permission changes. A workaround is under investigation.
2. **Folder picker may not work**: The `ACTION_OPEN_DOCUMENT_TREE` dialog (`DeviceActivity`) may behave unexpectedly. Workaround: toggle standby mode after granting permissions.
3. **No automated tests**: The Unity side has no unit or integration tests. The Android modules contain only stub `ExampleUnitTest` / `ExampleInstrumentedTest` placeholders; do not rely on them for validation.

---

## Code Conventions

- **C# (Unity scripts)**: PascalCase for public members; camelCase for private fields. `MonoBehaviour` lifecycle methods (`Start`, `Update`) use standard Unity patterns.
- **Java (Android plugin)**: Standard Android/Java conventions. All public API methods in `PluginInstance` are `static` and accessed via `PluginInstance.methodName(...)` from Unity C# using `AndroidJavaClass`.
- **No test framework** is configured for the Unity project. Avoid adding tests unless specifically requested.
- Do not remove or modify `.meta` files unless also removing the corresponding asset.

---

## Linting / Static Analysis

There is no automated linter configured for C# or Java in this repository. Rely on Unity's built-in compiler errors and Android Studio / `./gradlew lint` for the AAR module.

```bash
cd UnityUtilsAar
./gradlew :UnityPlugin:lint
```
