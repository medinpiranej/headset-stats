# CLAUDE.md

Guidance for Claude (and other contributors) working in this repository.

## What this is

**Headset Stats**: an open-source (MIT, © Medin Piranej) app that shows the battery level of
wireless headsets that don't report it to the OS. The first target is the **PlayStation PULSE 3D**
on Windows. It's planned for the Microsoft Store (free); macOS, Linux and Android apps come later.

## Repository layout

```
docs/            Shared across platforms: PROTOCOL.md (source of truth for device protocols),
                 tray-icons.png, wiki/ (sources for the GitHub wiki)
windows/         .NET 9 solution (HeadsetStats.sln): the only implemented platform
  src/HeadsetStats.Core     Win32 HID (hid.dll + SetupAPI P/Invoke), protocols, HeadsetMonitor, StatusStore
  src/HeadsetStats.Tray     WinForms tray app → HeadsetStats.exe (tray icon + DeviceWindow with
                            Device / History / Supported devices / About tabs)
  src/HeadsetStats.Probe    Read-only reverse-engineering CLI → headset-probe.exe
  tests/HeadsetStats.Core.Tests   xUnit; protocol tests use real captured reports
macos/ linux/ android/      Placeholders (README with suggested stack). Built on the
                            owner's Mac and Linux PC, not on this Windows machine.
```

Keep each platform in its own folder. Protocol knowledge goes in `docs/PROTOCOL.md`, never
only in platform code, because every platform reimplements the same decoding.

## Commands (run from `windows/`)

```
dotnet build
dotnet test
dotnet run --project src/HeadsetStats.Tray
dotnet run --project src/HeadsetStats.Tray -- --render-icons ../docs/tray-icons.png
dotnet run --project src/HeadsetStats.Tray -- --screenshots ../docs/screenshots   # PNG of each window tab, live data
dotnet run --project src/HeadsetStats.Probe -- list | features 054C:0D5E | listen 054C:0D5E 180 | log 054C:0D5E FILE
```

- `TreatWarningsAsErrors` is on (`windows/Directory.Build.props`), so builds must be warning-free.
- A running `HeadsetStats.exe` or `headset-probe.exe` locks its `bin/` output. Stop it
  (`Stop-Process -Name HeadsetStats`) before rebuilding that project.
- If `dotnet` isn't found in a fresh shell right after install, reload PATH from the machine/user environment.

## PULSE 3D protocol: key facts

Full details and raw captures are in `docs/PROTOCOL.md`.

- Adapter CFI-ZWD1 = USB `054C:0D5E`. Status comes on HID collection **COL04** (usage page `0xFF01`).
- Input report **`0xB0`**, 8 bytes, e.g. `B0 02 28 28 EF 58 11 28`:
  - **byte 3**: battery 0–100 in steps of 10; **`0x80` = charging** (no level while charging)
  - **byte 4**: bit `0x04` set = headset on (`0xEF`); `0xEB`/`0xE3` = switching off / off
- Reports arrive **only on change** (power on/off, cable in/out, adapter plugged in). There's no
  periodic update and **no way to poll**: GET_REPORT and output reports `B0`/`B1` all fail.
  That's why `StatusStore` persists the last report and the tray shows "as of HH:mm".
- The battery value is a voltage estimate. It reads high right after charging (100 → 70, true 50). Reports sent
  during link-up (byte 5 `0x59`/`0x5A`) are accurate, so `ChargeSettling` marks readings "settling" until one arrives (max 10 min).

## Conventions

- **No third-party runtime dependencies** in Core/Tray (keeps the Store package small and auditable).
- New headset = implement `IHeadsetProtocol`, register it in `SupportedHeadsets.All`, add tests built
  from **real captured reports** (with a comment giving the capture date), and document it in `docs/PROTOCOL.md`.
- `HeadsetMonitor` raises events on a thread-pool thread; the tray marshals them to the UI thread.
  It also exposes `ActiveDevice` (adapter USB id, firmware, HID path) and `History` (live reports this session).
- User-facing text about a headset (model numbers, what it reports, limitations) lives in its protocol's
  `DeviceDescription`. The Supported devices tab is generated from `SupportedHeadsets.All`, so
  adding a protocol lists it automatically.
- The About tab explains how the data is gathered and what to expect. Keep it in sync with
  `docs/PROTOCOL.md` when findings change.
- The window is built in code, not the designer. Give pixel sizes at 96 DPI through `LogicalToDeviceUnits`
  (the app is PerMonitorV2), and check `--screenshots` output at the owner's display scaling.
- Save images through a `FileStream`, not `Image.Save(path)`: GDI+ fails on paths over 260 characters.
- Tray icon (`BatteryIcon.cs`) is vector-drawn per `TrayIconKind`. After changing it, regenerate
  `docs/tray-icons.png` and check 16 px readability on both dark and light backgrounds.
- Commits end with the `Co-Authored-By` trailer when Claude authored them.

## Rules for working with hardware

- **The probe tool and exploratory scripts must stay read-only.** Never send output or feature
  reports (SET_REPORT, `WriteFile`) to a device without the owner's explicit OK for that specific
  report. Unknown commands could change headset settings or firmware state.
- Synchronous HID handles serialize I/O. A pending `ReadFile` blocks `HidD_SetOutputReport` on the
  **same** handle, so use a separate handle, or overlapped I/O as `HidConnection` does.
- Opening a HID handle with access `0` is fine for attributes/caps, but GET/SET_REPORT needs read/write access.

## Legal and trademark rules

- Reverse engineering is for interoperability only, from hardware we own. Never commit Sony
  firmware, software, SDK code, logos or product photos.
- "PlayStation", "PULSE", "PULSE 3D" and "PULSE Elite" are Sony trademarks. Only use them to say what
  hardware is supported (e.g. "PULSE 3D wireless headset"). Never put them in the app name, icon, or Store title.
- Keep the "Not affiliated with or endorsed by Sony Interactive Entertainment" disclaimer in the
  README, the About dialog and the Store listing.
- The app must stay offline with no telemetry. `PRIVACY.md` promises this and the Store listing links to it.

## Status and next steps

- Done (Windows): battery/charging/on-off decoding, tray app with state icons, low-battery
  balloon, persisted last status, device details window (Device / History / Supported devices /
  About), probe tool, tests.
- Wiki page sources are in `docs/wiki/`. The GitHub wiki itself isn't created yet (the owner must
  create the first page on github.com); after that, push the pages to `headset-stats.wiki.git`.
- Next: MSIX packaging for the Microsoft Store (needs Windows SDK + Partner Center identity values;
  switch "Start with Windows" from the HKCU Run key to an MSIX `StartupTask`), then more headsets
  (PULSE Elite / PlayStation Link), then the macOS, Linux and Android apps.
