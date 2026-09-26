# Reverse-engineering guide

How the PULSE 3D protocol was decoded, and how to do the same for another headset. Everything
here observes hardware you own, for interoperability. Nothing is copied from Sony software.

## 1. Find the adapter

```
cd windows
dotnet run --project src/HeadsetStats.Probe -- list          # Sony devices (vendor 054C)
dotnet run --project src/HeadsetStats.Probe -- list 1234     # another vendor id
```

Each line is one HID *collection*, with its usage page and report sizes. Vendor-defined usage pages
(`0xFF00`–`0xFFFF`) with a small input report are the likely status channels.

## 2. Dump feature reports (read-only)

```
dotnet run --project src/HeadsetStats.Probe -- features 054C:0D5E
```

This sends GET_FEATURE for every report id and prints the answers.

## 3. Listen while changing one thing at a time

```
dotnet run --project src/HeadsetStats.Probe -- listen 054C:0D5E 1200
```

Then, about a minute apart, and **write down what you did and when**:

1. Switch the headset off, wait 10 s, switch it on.
2. Plug the charging cable in.
3. Switch it off and on while charging.
4. Unplug the cable.
5. Switch it off and on again.
6. Unplug and replug the adapter.

Match each change to the bytes that changed at that moment. For the PULSE 3D:

- one byte jumped to `0x80` exactly when the cable went in, and back to a number when it came out (the **charging/battery byte**)
- one byte changed `EF → EB → E3` at every switch-off, and back at every switch-on (the **power state**)
- bytes that never changed are constants

Useful sanity checks: compare the level with the console's reading, and watch whether the
values move in sensible steps (10 % steps here).

## 4. Read the report descriptor

The descriptor tells you which report ids exist and which ones the PC may **send**. On the PULSE 3D,
COL04 declares input `0xB0` (7 flag bits) and a single output `0xB1` (1 bit). The battery byte isn't
declared at all, which is common for vendor data.

## 5. Only then, and carefully, try writing

Sending output or feature reports can change device settings. Only try it on your own device,
one report at a time, knowing how to undo it. On the PULSE 3D, `B0 00` was rejected as undeclared
and `B1 01` was stalled by the device; nothing changed on the headset.

## Pitfalls we hit

- **Synchronous handles serialize I/O.** A blocking `ReadFile` on a handle makes
  `HidD_SetOutputReport` on the *same* handle wait forever. Use a second handle or overlapped I/O.
- **GET/SET_REPORT needs read/write access.** A handle opened with access `0` can read attributes
  and capabilities, but report requests fail.
- **Status is event-driven.** An empty log may just mean nothing changed. Switch the headset off and on.
- **Voltage bounce.** Levels read high right after charging; don't mistake the settling for a decoding error.

## 6. Contribute

Open an issue with the probe output, your headset and adapter model numbers, the USB id, and your
timeline of actions. Or add an `IHeadsetProtocol`, tests built from your captures, and a section in
`docs/PROTOCOL.md` (see [Development](Development)).
