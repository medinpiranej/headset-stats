# Headset Stats for Windows

Tray app for Windows 10/11 that shows the headset battery next to the clock, plus a device window with
everything the headset reports.

![Tray icon states at 16, 24 and 32 px on dark and light taskbars](../docs/tray-icons.png)

## Build locally

### 1. Install the tools (once)

| Tool | Install |
|---|---|
| .NET 9 SDK | `winget install Microsoft.DotNet.SDK.9` |
| Git | `winget install Git.Git` |

Open a **new** terminal afterwards so both are on `PATH`. No Visual Studio needed; any editor works
(VS Code with the C# Dev Kit, Visual Studio 2022 or Rider can open `HeadsetStats.sln`).

### 2. Get the code

```
git clone https://github.com/medinpiranej/headset-stats
cd headset-stats/windows
```

### 3. Build, test and package

```
.\build.ps1
```

This restores, builds (warnings are errors), runs the tests, and publishes into `..\artifacts\`:

```
artifacts\HeadsetStats-<version>-win-x64\      HeadsetStats.exe, headset-probe.exe, LICENSE, PRIVACY.md
artifacts\HeadsetStats-<version>-win-x64.zip   the same, zipped (~220 KB)
```

| Option | Effect |
|---|---|
| `-SelfContained` | Bundle the .NET runtime so it runs on PCs without .NET (much larger) |
| `-Runtime win-arm64` | Build for ARM64 PCs (default `win-x64`) |
| `-Configuration Debug` | Debug build |
| `-SkipTests` | Skip the unit tests |
| `-Output <folder>` | Put the package somewhere else |

Without `-SelfContained`, the PC running the app needs the **.NET 9 Desktop Runtime**
(`winget install Microsoft.DotNet.DesktopRuntime.9`). If Windows blocks the script, run it once with
`powershell -ExecutionPolicy Bypass -File .\build.ps1`.

GitHub Actions runs the same script on every push and pull request and uploads the zip as a build artifact.

### Quick commands while developing

```
dotnet build                                   # build everything
dotnet test                                    # run the tests
dotnet run --project src/HeadsetStats.Tray     # start the tray app from source
```

Close a running Headset Stats (tray icon → Exit) before rebuilding; it locks its output files.

## Using the app

Double-click the tray icon, or right-click → **Device details…**, to open the device window:

| Tab | Shows |
|---|---|
| Device | Support status, headset and adapter model, connection, power, battery, charging, mic, volume, game/chat balance, last event and raw report, USB id, adapter firmware, HID path, saved-status file |
| History | Every message received this session, decoded when possible, with its raw bytes |
| Buttons | Live volume / mic / balance and the last button press, plus actions for the Chat and Game buttons (open a shortcut, app, file or URL, or run a command), stored in `%LOCALAPPDATA%\HeadsetStats\settings.json` |
| Supported devices | Supported headsets and what to expect from each, and the switch for trying unsupported Sony headsets |
| About | How the information is gathered, what to expect, contributing, privacy, trademarks |

- Every list is copyable (Ctrl+C / Ctrl+A / right-click → Copy, Copy value, Copy all).
- The bottom bar has **Copy device logs** and **Report a device on GitHub…**. The tray menu has **Copy device logs** too.
- With no supported adapter plugged in, other Sony devices are tried read-only as "Not supported yet (experimental)".
- The app follows Windows' light/dark app setting.

### Command-line options

| Command | What it does |
|---|---|
| `HeadsetStats.exe` | Starts the tray app (one instance at a time) |
| `HeadsetStats.exe --report report.md` | Writes the same report as "Copy device logs" to a file |
| `HeadsetStats.exe --render-icons icons.png` | Renders every tray icon state (docs) |
| `HeadsetStats.exe --screenshots <folder> light\|dark` | Screenshots every window tab with live data (docs, store listing) |

To regenerate the images in `docs/`:

```
dotnet run --project src/HeadsetStats.Tray -- --render-icons ../docs/tray-icons.png
dotnet run --project src/HeadsetStats.Tray -- --screenshots ../docs/screenshots light
dotnet run --project src/HeadsetStats.Tray -- --screenshots ../docs/screenshots dark
```

## Probe tool (for new headsets)

Read-only; it never sends anything to a device.

```
dotnet run --project src/HeadsetStats.Probe -- list                      # Sony HID collections
dotnet run --project src/HeadsetStats.Probe -- features 054C:0D5E        # dump feature reports
dotnet run --project src/HeadsetStats.Probe -- listen 054C:0D5E 180      # print reports for 3 minutes
dotnet run --project src/HeadsetStats.Probe -- log 054C:0D5E capture.log # log until stopped, survives unplugging
```

The published `headset-probe.exe` takes the same arguments.

## Project layout

| Project | What it is |
|---|---|
| `src/HeadsetStats.Core` | Win32 HID access (`hid.dll` / SetupAPI), device protocols (`Pulse3DProtocol`, `ExperimentalProtocol`), `HeadsetMonitor`, `ChargeSettling`, `StatusStore`, `AppSettings`, `DiagnosticReport` |
| `src/HeadsetStats.Tray` | WinForms tray app (`HeadsetStats.exe`): icon drawing, device window, button actions |
| `src/HeadsetStats.Probe` | Read-only reverse-engineering CLI (`headset-probe.exe`) |
| `tests/HeadsetStats.Core.Tests` | xUnit tests: protocol decoding from real captures, settling, settings, experimental devices, device logs |
| `build.ps1` | Build, test and package script (used by CI too) |

No third-party runtime dependencies. Conventions and pitfalls are in [`../CLAUDE.md`](../CLAUDE.md).

## Adding a headset

Implement `IHeadsetProtocol` (including its `DeviceDescription`) in `src/HeadsetStats.Core/Devices`, register it in
`SupportedHeadsets.All`, add tests built from captured reports, and document it in `docs/PROTOCOL.md`.
Until then, owners can already try their headset in experimental mode and send device logs.

## Microsoft Store (planned)

Package as MSIX with a full-trust desktop entry point, replace the registry-based "Start with Windows"
with an MSIX `StartupTask`, and add an Xbox Game Bar widget in the same package.
