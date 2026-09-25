# Headset protocol notes

Shared by every platform app. Findings come from observing the USB HID traffic of
hardware we own, for interoperability. No Sony software or firmware was copied.

## Sony wireless headset adapter — USB `054C:0D5E`

Windows shows the adapter as "Wireless Headset". Interface 3 (`MI_03`) is HID with four
top-level collections:

| Collection | Usage page / usage | Input | Output | Feature | Notes |
|---|---|---|---|---|---|
| COL01 | `0x000C` / `0x0001` (Consumer Control) | 5 | – | – | Media keys. Nothing seen when pressing volume/mute. |
| COL02 | `0xFF00` / `0x0001` (vendor) | 64 | – | 64 | Feature `0xFF` returns `FF 01 00…`. |
| COL03 | `0xFF03` / `0x0020` (vendor) | – | – | 35 | Feature `0xA0` returns a UTF-16 string ("Hid Interface"); `0xA1` returns zeros. |
| COL04 | `0xFF01` / `0x0020` (vendor) | 8 | 2 | – | **Status reports** (below). |

### Status report `0xB0` (COL04, 8 bytes, input)

Sent by the adapter without being asked, seen when the headset is switched on.
It isn't sent on a timer.

```
B0 02 28 80 EF 58 11 1E
│  │  │  │  └──┴──┴──┴── unknown
│  │  │  └── flags? (0x80 while connected)
│  │  └── battery percent (0x28 = 40)   ← PROVISIONAL, needs more samples
│  └── unknown (0x02)
└── report id
```

### Open questions

- [ ] Confirm byte 2 is battery % (compare with the PS5's reading, and watch it drop over time).
- [ ] Find the charging flag (capture with the charging cable plugged in and unplugged).
- [ ] Find the "headset off" signal (a report when powering off?).
- [ ] Can a 2-byte output report on COL04 (e.g. `B0 00`) request status on demand?
- [ ] Which retail model(s) use this adapter (Pulse 3D, Pulse Elite / PlayStation Link, …)?

### Capturing samples

On Windows, run the read-only probe from `windows/`:

```
dotnet run --project src/HeadsetStats.Probe -- listen 054C:0D5E 180
```

Then switch the headset off and on, plug and unplug the charging cable, and paste the
output into an issue along with your headset model.
