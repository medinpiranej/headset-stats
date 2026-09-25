---
name: New headset
about: Help add support for a headset by sharing what its adapter sends
title: "Support for <headset name>"
labels: new headset
---

**Headset**: <!-- product name and model number, e.g. PULSE Elite CFI-ZWH2 -->
**Adapter**: <!-- model number printed on the USB adapter, if any -->
**Connection**: <!-- USB adapter / Bluetooth -->

### Probe `list` output

```
<!-- dotnet run --project src/HeadsetStats.Probe -- list <VID> -->
```

### Probe `listen` output

```
<!-- dotnet run --project src/HeadsetStats.Probe -- listen <VID>:<PID> 600 -->
```

### What I did, and when

| Time in the log | Action | Battery on the console, if known |
|---|---|---|
| 00:20 | switched off | |
| 00:35 | switched on | |
| 01:40 | plugged in the charging cable | |
