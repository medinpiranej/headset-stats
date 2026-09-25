# Supported headsets

| Headset | Adapter | USB id | Battery | Charging | On/off | Platforms |
|---|---|---|---|---|---|---|
| PULSE 3D wireless headset (CFI-ZWH1) | CFI-ZWD1 | `054C:0D5E` | ✅ 10 % steps | ✅ | ✅ | Windows |

The app identifies the headset by its adapter's USB id. All colour variants of the PULSE 3D appear
to use the same adapter. The adapter doesn't expose a serial number, colour, or headset firmware
version, only its own revision (`REV_0100`).

## PULSE 3D limitations

- The level is a voltage estimate in 10 % steps.
- No level while charging (the adapter reports "charging" instead).
- Status arrives only on change: power on/off, cable in/out, adapter plugged in. It can't be polled.
- Volume and mic-mute buttons are handled inside the headset; the PC sees nothing.

## Wanted

| Headset | Notes |
|---|---|
| PULSE Elite | Uses the PlayStation Link USB adapter (different USB id); also Bluetooth |
| PULSE Explore | PlayStation Link earbuds |
| Older PlayStation Gold / Platinum | Already supported on Linux by other projects |

Have one of these? Follow the [Reverse-engineering guide](Reverse-Engineering-Guide) and open an issue with your capture.
