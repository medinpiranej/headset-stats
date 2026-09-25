# Supported headsets

| Headset | Adapter | USB id | Battery | Charging | On/off | Platforms |
|---|---|---|---|---|---|---|
| PULSE 3D wireless headset (CFI-ZWH1) | CFI-ZWD1 | `054C:0D5E` | ✅ 10 % steps | ✅ | ✅ | Windows |
| Other Sony headsets | ? | `054C:*` | 🧪 experimental | 🧪 | 🧪 | Windows |

The app identifies the headset by its adapter's USB id. All colour variants of the PULSE 3D appear
to use the same adapter. The adapter doesn't expose a serial number, colour, or headset firmware
version, only its own revision (`REV_0100`).

## PULSE 3D limitations

- The level is a voltage estimate in 10 % steps.
- No level while charging (the adapter reports "charging" instead).
- Status arrives only on change: power on/off, cable in/out, adapter plugged in. It can't be polled.
- Volume, mic mute and game/chat act inside the headset. The app shows their state, but Windows' volume and mic are unaffected. The monitor button isn't reported.

## Wanted

| Headset | Notes |
|---|---|
| PULSE Elite | Uses the PlayStation Link USB adapter (different USB id); also Bluetooth |
| PULSE Explore | PlayStation Link earbuds |
| Older PlayStation Gold / Platinum | Already supported on Linux by other projects |

Have one of these? The easiest way to help:

1. Run Headset Stats with only that headset's adapter plugged in. It appears as **"Not supported yet
   (experimental)"**. The app only listens and tries the PULSE 3D format.
2. Use it for a minute (power off/on, buttons, charging cable).
3. Press **Report a device on GitHub…**: the device logs are copied and a *New headset* issue opens. Paste them
   and tell us what worked.

For deeper captures, see the [Reverse-engineering guide](Reverse-Engineering-Guide).
