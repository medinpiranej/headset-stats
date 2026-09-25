# Development

## Repository layout

| Path | Contents |
|---|---|
| `docs/` | `PROTOCOL.md` (shared by all platforms), icon preview, wiki sources (`docs/wiki/`) |
| `windows/` | .NET 9 solution: the Windows app |
| `macos/`, `linux/`, `android/` | Planned apps, one folder per platform |
| `CLAUDE.md` | Conventions and project knowledge for Claude and other contributors |

## Windows

Requires the .NET 9 SDK (`winget install Microsoft.DotNet.SDK.9`).

```
cd windows
.\build.ps1         # build (warnings are errors), test, package into ..\artifacts
dotnet run --project src/HeadsetStats.Tray
```

See [windows/README.md](https://github.com/medinpiranej/headset-stats/blob/main/windows/README.md#build-locally) for options (self-contained, ARM64, …).

| Project | Role |
|---|---|
| `HeadsetStats.Core` | Win32 HID via P/Invoke (`hid.dll`, SetupAPI); `IHeadsetProtocol` implementations; `HeadsetMonitor` (find adapter → read reports → reconnect); `StatusStore` (persists the last report) |
| `HeadsetStats.Tray` | WinForms `NotifyIcon` app; vector-drawn icons (`BatteryIcon.cs`) |
| `HeadsetStats.Probe` | Read-only CLI: `list`, `features`, `listen` |
| `HeadsetStats.Core.Tests` | xUnit tests built from real captured reports |

There are no third-party runtime dependencies.

### Adding a headset

1. Capture reports (see [Reverse-engineering guide](Reverse-Engineering-Guide)).
2. Implement `IHeadsetProtocol` in `src/HeadsetStats.Core/Devices` and add it to `SupportedHeadsets.All`.
3. Add tests using the captured bytes, with a comment giving the capture date.
4. Document it in `docs/PROTOCOL.md` and on [Supported headsets](Supported-Headsets).

### Changing the tray icon

Edit `BatteryIcon.cs`, then regenerate and check the preview at 16 px on dark and light backgrounds:

```
dotnet run --project src/HeadsetStats.Tray -- --render-icons ../docs/tray-icons.png
```

Stop a running `HeadsetStats.exe` before rebuilding; it locks its output folder.

## Other platforms

Each platform reimplements the decoding from `docs/PROTOCOL.md`:

| Platform | Suggested approach |
|---|---|
| macOS | Swift + SwiftUI `MenuBarExtra`; IOKit `IOHIDManager` matching `054C:0D5E`, usage page `0xFF01` |
| Linux | hidraw (hidapi); StatusNotifierItem tray; udev rule `ATTRS{idVendor}=="054c", ATTRS{idProduct}=="0d5e", TAG+="uaccess"` |
| Android | Kotlin; USB Host API (`UsbManager`), interrupt IN endpoint of interface 3 |
