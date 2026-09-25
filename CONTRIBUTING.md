# Contributing to Headset Stats

Thanks for your interest! The project is open to contributions of all sizes, from reporting how
your headset behaves to adding a whole new platform.

## Ways to help

| You can… | How |
|---|---|
| **Get your headset supported** | Capture what its adapter sends with the probe tool and open a *New headset* issue. You don't need to write code. |
| **Report a bug** | Open a *Bug report* issue with your headset, Windows version, and the contents of the app's **History** tab. |
| **Fix or improve the Windows app** | Pick an open issue, or open one to discuss your idea first. |
| **Build the macOS, Linux or Android app** | See the README in `macos/`, `linux/` or `android/` and open an issue to coordinate. |
| **Improve the docs** | README, `docs/PROTOCOL.md`, and the wiki pages in `docs/wiki/`. |

## Capturing a new headset

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
dotnet build      # warnings are treated as errors
dotnet test
```

Read [`CLAUDE.md`](CLAUDE.md) first. It describes the layout, conventions and pitfalls, and applies
to human contributors too.

### Pull request checklist

- [ ] `dotnet build` has no warnings and `dotnet test` passes
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
