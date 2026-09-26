# Contributing to Headset Stats

Thanks for your interest! The project is open to contributions of all sizes, from reporting how
your headset behaves to adding a whole new platform.

## Ways to help

| You can… | How |
|---|---|
| **Get your headset supported** | [Try the app with your headset](#try-the-app-with-your-headset-no-coding-needed) and send us the device logs. You don't need to write code. |
| **Report a bug** | Open a *Bug report* issue and paste the app's **Copy device logs** output. |
| **Fix or improve the Windows app** | Pick an open issue, or open one to discuss your idea first. |
| **Build the macOS, Linux or Android app** | See the README in `macos/`, `linux/` or `android/` and open an issue to coordinate. |
| **Improve the docs** | README, `docs/PROTOCOL.md`, and the wiki pages in `docs/wiki/`. |

## Try the app with your headset (no coding needed)

The Windows app is open to headsets it doesn't support yet. If you own another Sony headset
(for example a PULSE Elite or PULSE Explore with the PlayStation Link adapter, or an older
PlayStation headset):

1. Run Headset Stats with only your headset's USB adapter plugged in (unplug any PULSE 3D adapter).
2. Make sure **Supported devices → "Try Sony headsets that aren't supported yet"** is ticked (it is by default).
   The app then shows your device as **"Not supported yet (experimental)"**. It only listens to the
   adapter and never sends it anything.
3. Use the headset for a minute: switch it off and on, press its buttons, plug the charging cable in and out.
4. Check the tray icon and the **Device** tab. Does the battery look right? Charging? Mute?
5. Press **Report a device on GitHub…** (bottom of the window). It copies the **device logs** and opens a
   *New headset* issue. Paste the logs and tell us what worked, what was wrong, and what was missing.

The device logs contain the app and Windows version, the Sony devices on your PC with their capabilities,
and the raw messages received. They don't contain file paths or your user name. Nothing is sent
automatically; you choose what to paste.

## Capturing a new headset in detail

```
cd windows
dotnet run --project src/HeadsetStats.Probe -- list
dotnet run --project src/HeadsetStats.Probe -- listen <VID>:<PID> 600
```

While it listens, about a minute apart: switch the headset off and on, plug the charging cable in,
switch it off and on again, unplug the cable. **Write down what you did at which timestamp**, and
include that with the output in the issue. The full guide is in `docs/wiki/Reverse-Engineering-Guide.md`.

## Development

Requires the .NET 9 SDK.

```
cd windows
.\build.ps1       # build (warnings are errors), test and package into ..\artifacts
```

While iterating, `dotnet build`, `dotnet test` and `dotnet run --project src/HeadsetStats.Tray` are quicker.
See [windows/README.md](windows/README.md#build-locally) for all options.

Read [`CLAUDE.md`](CLAUDE.md) first. It describes the layout, conventions and pitfalls, and applies
to human contributors too.

### Pull request checklist

- [ ] `.\build.ps1` succeeds (no warnings, tests pass). CI runs the same script
- [ ] New protocol code has tests built from **real captured reports** (with the capture date in a comment)
- [ ] New findings are documented in `docs/PROTOCOL.md`
- [ ] The tray icon or window changed? Regenerate `docs/tray-icons.png` / `docs/screenshots/`
- [ ] No new third-party runtime dependencies without discussing it first

## Ground rules

- **Only read from devices.** Exploratory code must not send output or feature reports to a
  headset unless the change says so clearly and explains why. Unknown commands can change device settings.
- **No Sony material.** Never add Sony firmware, software, SDK code, logos, or product photos.
  Only use Sony trademarks to say which hardware is compatible.
- **Stay offline.** The app must not make network connections or collect data (see `PRIVACY.md`).
- Be kind and constructive in issues and reviews.

## License

By contributing, you agree that your contributions are licensed under the project's
[MIT License](LICENSE).
