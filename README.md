# Headset Stats

See your wireless headset's battery level on your PC, starting with the **PlayStation PULSE 3D**
wireless headset on Windows.

Windows treats the PULSE 3D's USB adapter as a plain audio device, so it never shows the headset's
battery. Headset Stats reads the adapter's status messages and puts the level in your system tray.

![Tray icon states](docs/tray-icons.png)

> **Status: early development.** On Windows, the PULSE 3D's battery, charging, on/off, mic mute, volume,
> game/chat balance and button presses all work. Other Sony headsets can be tried experimentally.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/screenshots/window-device-dark.png">
  <img src="docs/screenshots/window-device-light.png" alt="Device details window" width="560">
</picture>

## Features

- **Battery level** in the tray: green above 30 %, amber at 30 % and below, red at 15 % and below
- **Low-battery notification** at 15 %
- **Charging indicator** (⚡), **headset off** state, and a **mic muted** badge
- Shows the headset's own **volume** and **game/chat balance** in the device window
- **Remembers the last status** across restarts. The adapter only reports changes, so the app shows
  the last known state with its time until a new report arrives.
- **Device details window** (double-click the tray icon): live headset and adapter information, a history
  of every status report with its raw bytes, the list of supported devices, and an About page explaining
  how the data is gathered and what to expect
- **Chat / Game button actions**: make the headset's Chat or Game button open any shortcut, app, file or
  website, or run your own command (in addition to changing the headset's balance)
- **Light and dark mode**, following your Windows setting
- **Every value is selectable and copyable** (Ctrl+C, right-click → Copy), plus **Copy device logs** for bug reports
- **Open to new headsets**: other Sony headsets can be tried in an experimental, listen-only mode (see below)
- Start with Windows (optional)
- Offline, no telemetry, no third-party dependencies

## Supported devices

| Headset | Adapter (USB id) | Battery | Charging | On/off |
|---|---|---|---|---|
| PULSE 3D wireless headset (CFI-ZWH1) | CFI-ZWD1 (`054C:0D5E`) | ✅ 10 % steps | ✅ | ✅ |
| Other Sony headsets | `054C:*` | 🧪 experimental | 🧪 | 🧪 |

🧪 **Not supported yet, but you can try it.** With no supported adapter plugged in, the app listens
(read-only) to other Sony devices, marks them *"Not supported yet (experimental)"* and tries the PULSE 3D
format, which similar Sony adapters may share. Controllers like the DualSense are skipped. **Tell us whether it
works**: press *Report a device on GitHub…* in the app. It copies the device logs and opens a ready-made issue.
See [CONTRIBUTING.md](CONTRIBUTING.md#try-the-app-with-your-headset-no-coding-needed).

### Known limitations (PULSE 3D)

- The level is reported in **10 % steps** and is a voltage estimate.
- **Right after charging it reads too high** (e.g. 100 % that is really 50 %). The app greys the icon and
  marks the value "settling" until the headset reconnects. Switch it off and on for an accurate reading.
- **No level is reported while charging**; the app shows ⚡ and the last known level.
- The adapter sends status **only when something changes** (a button, switched on or off, cable plugged
  or unplugged, adapter plugged in), and the PC can't ask for it. If the icon shows "?", switch the headset off and on.
- Volume, mute and game/chat act **inside the headset**: the app shows them but Windows' volume is unaffected.
  The **monitor** button isn't reported to the PC.

## Platforms

| Folder | Platform | Status |
|---|---|---|
| [`windows/`](windows/) | Windows 10/11 tray app (.NET 9) | ✅ Working, Microsoft Store release planned |
| [`macos/`](macos/) | macOS menu bar app (Swift) | Planned |
| [`linux/`](linux/) | Linux tray app | Planned |
| [`android/`](android/) | Android app (Kotlin, USB host) | Planned |

All platforms share one protocol description: [`docs/PROTOCOL.md`](docs/PROTOCOL.md).

## Install

The Microsoft Store release is coming. Until then, build it yourself (below), or download the zip
from the latest successful [GitHub Actions run](https://github.com/medinpiranej/headset-stats/actions)
(*Artifacts* section). Unzip it and run `HeadsetStats.exe`. It needs the .NET 9 Desktop Runtime:
`winget install Microsoft.DotNet.DesktopRuntime.9`.

## Build locally (Windows)

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.SDK.9`).

```
git clone https://github.com/medinpiranej/headset-stats
cd headset-stats/windows
.\build.ps1
```

`build.ps1` builds, runs the tests and packages the app into `artifacts\` (a folder and a ~220 KB zip).
Options, quick dev commands and the probe tool are in [`windows/README.md`](windows/README.md#build-locally).

## How it works

The PULSE 3D's USB adapter exposes a vendor-specific HID interface next to its audio interface.
Whenever the headset's state changes, the adapter sends an 8-byte report `0xB0`. Byte 3 is the
battery level (or `0x80` while charging) and byte 4 says whether the headset is on. The byte
layout was worked out by capturing these reports while switching the headset on and off and
plugging the cable in and out. See [`docs/PROTOCOL.md`](docs/PROTOCOL.md) for the full layout and raw captures.

## Adding a headset

1. Run the read-only probe while switching your headset on and off and plugging the charging cable in and out:
   ```
   cd windows
   dotnet run --project src/HeadsetStats.Probe -- list
   dotnet run --project src/HeadsetStats.Probe -- listen <VID>:<PID> 300
   ```
2. Open an issue with the output, your headset model, and what you did at each timestamp.
3. Or implement it yourself: add an `IHeadsetProtocol`, tests with your captured reports, and a
   section in `docs/PROTOCOL.md`.

## Contributing

**The project is open to contributions!** Headset captures, bug reports, code, docs, and the
macOS, Linux and Android apps are all welcome, and you don't need to write code to get your
headset supported. See [CONTRIBUTING.md](CONTRIBUTING.md) to get started.

## Support the project

Headset Stats is free and always will be. If it's useful to you, you can support its development
through [GitHub Sponsors](https://github.com/sponsors/medinpiranej). Starring the repo and
reporting your headset help too.

## Privacy

Headset Stats collects no data and makes no network connections. See [PRIVACY.md](PRIVACY.md).

## License

[MIT](LICENSE) © 2026 Medin Piranej

---

*Not affiliated with or endorsed by Sony Interactive Entertainment. PlayStation, PULSE, PULSE 3D
and PULSE Elite are trademarks of Sony Interactive Entertainment Inc., used here only to
identify compatible hardware.*
