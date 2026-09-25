# Headset Stats for Windows

Tray app for Windows 10/11 that shows the headset battery next to the clock.

![Tray icon states at 16, 24 and 32 px on dark and light taskbars](../docs/tray-icons.png)

Regenerate this preview after changing `BatteryIcon.cs`:

```
dotnet run --project src/HeadsetStats.Tray -- --render-icons ../docs/tray-icons.png
```

## Layout

| Project | What it is |
|---|---|
| `src/HeadsetStats.Core` | HID access (Win32 `hid.dll` / SetupAPI), device protocols, `HeadsetMonitor` |
| `src/HeadsetStats.Tray` | WinForms tray app (`HeadsetStats.exe`) |
| `src/HeadsetStats.Probe` | Read-only CLI for reverse-engineering (`headset-probe.exe`) |
| `tests/HeadsetStats.Core.Tests` | xUnit tests for protocol parsing |

No third-party runtime dependencies.

## Build and run

Requires the .NET 9 SDK (`winget install Microsoft.DotNet.SDK.9`).

```
dotnet build
dotnet test
dotnet run --project src/HeadsetStats.Tray
```

Probe tool:

```
dotnet run --project src/HeadsetStats.Probe -- list
dotnet run --project src/HeadsetStats.Probe -- features 054C:0D5E
dotnet run --project src/HeadsetStats.Probe -- listen 054C:0D5E 180
```

## Adding a headset

Implement `IHeadsetProtocol` in `src/HeadsetStats.Core/Devices`, register it in
`SupportedHeadsets`, add a test with a captured report, and document it in `docs/PROTOCOL.md`.

## Microsoft Store (planned)

Package as MSIX with a full-trust desktop entry point and replace the registry-based
"Start with Windows" with an MSIX `StartupTask`.
