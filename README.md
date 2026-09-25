# Headset Stats

Shows the battery level of wireless gaming headsets that don't report it to the operating
system, starting with Sony's PlayStation wireless headsets on Windows.

> **Status: early development.** The battery byte is identified but still being confirmed.
> See [docs/PROTOCOL.md](docs/PROTOCOL.md).

## Platforms

| Folder | Platform | Status |
|---|---|---|
| [`windows/`](windows/) | Windows 10/11 tray app (.NET 9) | In progress |
| [`macos/`](macos/) | macOS menu bar app | Planned |
| [`linux/`](linux/) | Linux tray app | Planned |
| [`android/`](android/) | Android app | Planned |

All platforms share the protocol documentation in [`docs/`](docs/).

## Supported devices

| Adapter USB id | Headset | Battery | Charging |
|---|---|---|---|
| `054C:0D5E` | Sony wireless headset (model TBC) | Provisional | Not yet |

Own a headset that isn't listed? Capture a few reports with the probe tool (see
[docs/PROTOCOL.md](docs/PROTOCOL.md#capturing-samples)) and open an issue.

## Privacy

Headset Stats collects no data and makes no network connections. See [PRIVACY.md](PRIVACY.md).

## License

[MIT](LICENSE) © 2026 Medin Piranej

---

*Not affiliated with or endorsed by Sony Interactive Entertainment. PlayStation, PULSE, PULSE 3D
and PULSE Elite are trademarks of Sony Interactive Entertainment Inc., used here only to
identify compatible hardware.*
