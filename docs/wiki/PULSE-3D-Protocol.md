# PULSE 3D protocol

Summary of how the PULSE 3D USB adapter (CFI-ZWD1, `054C:0D5E`) reports headset status. The
complete notes, raw captures and open questions live in the repository:
**[docs/PROTOCOL.md](https://github.com/medinpiranej/headset-stats/blob/main/docs/PROTOCOL.md)**.
That file is the source of truth.

## The adapter

A USB composite device with an audio function ("Wireless Stereo Headset") and HID interface 3,
which has four collections:

| Collection | Usage page | Purpose |
|---|---|---|
| COL01 | `0x000C` Consumer Control | Media keys (nothing observed) |
| COL02 | `0xFF00` vendor | Unknown |
| COL03 | `0xFF03` vendor | Feature reports only (strings, zeros) |
| **COL04** | **`0xFF01` vendor** | **Status reports** |

## Status report `0xB0`

```
B0 02 28 28 EF 58 11 28
         ││ ││
         ││ └┴── byte 4: headset state. Bit 0x04 set = on (0xEF); 0xEB/0xE3 = switching off/off
         └┴───── byte 3: battery 0–100 in 10 % steps, or 0x80 = charging
```

Other bytes are constant or not yet understood.

## When reports are sent

| Event | Report |
|---|---|
| Headset switched on | ✅ (several reports during the link-up) |
| Headset switched off | ✅ |
| Charging cable plugged in / pulled out | ✅ |
| Adapter plugged into the PC with the headset on | ✅ |
| Time passing, battery charging | ❌ nothing seen in ~10 minutes of charging |

## Requesting status: not possible

| Attempt | Result |
|---|---|
| GET_REPORT `0xB0` | device stalls (`ERROR_GEN_FAILURE`) |
| Output report `B0 00` | not a declared output report |
| Output report `B1 01` (the only declared output) | device stalls |

That's why apps must remember the last report they saw.

## Example capture

```
00:34.8  B0 02 28 28 EF 59 11 28   switched on, 40 %
02:10.0  B0 02 28 80 EF 58 11 28   cable plugged in → charging
04:19.5  B0 02 28 3C EF 58 11 28   cable pulled out → 60 % (voltage bounce)
04:32.9  B0 02 28 32 EF 58 11 28   50 %
04:48.5  B0 02 28 32 E3 5B 11 28   switched off
05:05.5  B0 02 28 28 EF 59 11 28   switched on, 40 %
```
