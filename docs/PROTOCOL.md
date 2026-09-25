# Headset protocol notes

Shared by every platform app. Findings come from observing the USB HID traffic of
hardware we own, for interoperability. No Sony software or firmware was copied.

## PULSE 3D wireless headset adapter (CFI-ZWD1) — USB `054C:0D5E`

Windows shows the adapter as "Wireless Headset". Interface 3 (`MI_03`) is HID with four
top-level collections:

| Collection | Usage page / usage | Input | Output | Feature | Notes |
|---|---|---|---|---|---|
| COL01 | `0x000C` / `0x0001` (Consumer Control) | 5 | – | – | Media keys. Nothing seen when pressing volume/mute. |
| COL02 | `0xFF00` / `0x0001` (vendor) | 64 | – | 64 | Feature `0xFF` returns `FF 01 00…`. |
| COL03 | `0xFF03` / `0x0020` (vendor) | – | – | 35 | Feature `0xA0` returns a UTF-16 string ("Hid Interface"); `0xA1` returns zeros. |
| COL04 | `0xFF01` / `0x0020` (vendor) | 8 | 2 | – | **Status reports** (below). |

### Status report `0xB0` (COL04, 8 bytes, input)

Sent by the adapter without being asked whenever something changes: the headset powers on
or off, the charging cable is plugged in or pulled out, or the battery estimate changes.
It isn't sent on a timer, and the PC can't request it (see output report `0xB1` below).

```
B0 02 28 28 EF 58 11 28
│  │  │  │  │  │  │  └── unknown (0x1E or 0x28 seen)
│  │  │  │  │  │  └── constant 0x11
│  │  │  │  │  └── link sub-state: 0x58 steady; 0x59/0x5A/0x5B during power on/off
│  │  │  │  └── headset state: bit 0x04 set = on (0xEF); 0xEB switching off, 0xE3 off
│  │  │  └── battery: 0–100 in steps of 10, or 0x80 = charging (no level reported)
│  │  └── constant 0x28 in every capture
│  └── constant 0x02
└── report id
```

The battery value comes from a voltage-based estimate: after the cable was pulled out it
read 60 %, fell to 50 % within 15 s, then 40 % after a power cycle.

#### Capture 2026-09-25 (headset at about 40 %, PS5 showing 1 bar)

```
00:20.8  B0 02 28 1E EB 59 11 28   switched off
00:21.0  B0 02 28 1E E3 5B 11 28   off
00:34.8  B0 02 28 28 EF 59 11 28   switched on, 40 %
00:35.0  B0 02 28 28 EF 5A 11 28
01:00.9  B0 02 28 28 EF 58 11 28   steady
02:10.0  B0 02 28 80 EF 58 11 28   cable plugged in → charging
03:46.6  B0 02 28 80 EB 59 11 28   switched off while charging
03:46.9  B0 02 28 80 E3 5B 11 28
04:06.7  B0 02 28 80 EF 59 11 28   switched on while charging
04:06.9  B0 02 28 80 EF 5A 11 28
04:19.5  B0 02 28 3C EF 58 11 28   cable pulled out → 60 %
04:32.9  B0 02 28 32 EF 58 11 28   50 %
04:48.2  B0 02 28 32 EB 59 11 28   switched off
04:48.5  B0 02 28 32 E3 5B 11 28
05:05.5  B0 02 28 28 EF 59 11 28   switched on, 40 %
05:05.7  B0 02 28 28 EF 5A 11 28
05:31.1  B0 02 28 28 EF 58 11 28
07:20.4  B0 02 28 80 EF 58 11 28   cable plugged in → charging
```
### Report descriptor (COL04)

- Input `0xB0` declares 7 one-bit buttons, usages `0xFF01:0x25`–`0x2B`. The battery byte is
  **not** declared; it sits in undeclared/constant bits.
- Output `0xB1` declares a single one-bit button, usage `0xFF01:0x2C`. It's the only output
  report on this collection. `B0 00` is rejected (`ERROR_INVALID_PARAMETER`).

### Open questions

- [x] Battery is byte 3 (steps of 10). Byte 2 is constant 0x28.
- [x] Charging: byte 3 = 0x80. No level is reported while charging.
- [x] Headset off: byte 4 bit 0x04 clear (0xEB, then 0xE3).
- [ ] What does output `B1` bit 0 (usage `0x2C`) do? `B1 01` is refused by the device (`ERROR_GEN_FAILURE`, via both `HidD_SetOutputReport` and `WriteFile`); maybe it only works in some state, e.g. during pairing.
- [ ] Which of the 7 declared flag bits in `B0` is mic mute? Is the battery reported while charging anywhere else?
- [ ] Does the adapter send a new report on its own as the battery drains (e.g. 40 → 30)?
- [x] Retail model: PULSE 3D wireless headset (CFI-ZWH1) with USB adapter CFI-ZWD1.

### Capturing samples

On Windows, run the read-only probe from `windows/`:

```
dotnet run --project src/HeadsetStats.Probe -- listen 054C:0D5E 180
```

Then switch the headset off and on, plug and unplug the charging cable, and paste the
output into an issue along with your headset model.
