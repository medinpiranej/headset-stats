# Installation and usage

## Install

The Microsoft Store release is coming. Until then, build from source (see [Development](Development)):

```
git clone https://github.com/medinpiranej/headset-stats
cd headset-stats/windows
dotnet run --project src/HeadsetStats.Tray
```

Requirements: Windows 10 or 11, and the .NET 9 SDK to build. Plug the headset's USB adapter into the PC.

## The tray icon

![Tray icon states](https://raw.githubusercontent.com/medinpiranej/headset-stats/main/docs/tray-icons.png)

| Icon | Meaning |
|---|---|
| Green headphones, green number | Battery above 30 % |
| Amber headphones, amber number | 30 % or less |
| Red headphones, red number | 15 % or less (a notification also appears) |
| Green headphones, yellow ⚡ | Charging. The headset doesn't report a level while charging. |
| Grey headphones | Headset switched off |
| Grey headphones, "?" | Adapter found, but no status received yet |
| Grey headphones, red slash | No supported adapter plugged in |

Hover the icon, or right-click it, to see the headset model and details such as
"Battery 40% · updated 14:32", "Charging · was 40%" or "Headset is off · last seen 40%".

Right-click menu: **Device details…**, **Supported devices**, **Start with Windows**, **About**, **Exit**.

## The device window

Double-click the tray icon to open it.

![Device tab](https://raw.githubusercontent.com/medinpiranej/headset-stats/main/docs/screenshots/window-device-light.png)

- **Device**: everything known about the connected headset and adapter, including the raw last report
  and whether the values are live or saved from earlier.
- **History**: every status report received since the app started, decoded (event, battery, charging,
  power, mic, volume), with its raw bytes.
- **Buttons**: live headset volume, mic and game/chat balance, and what the **Chat** and **Game**
  buttons do on the PC: nothing extra, open a shortcut/app/file/website (pick from the Start menu or
  Desktop), or run a command. Use **Test now** to try it. The headset still changes its balance too.
- **Supported devices**: supported headsets, and what to expect from each.
- **About**: how the information is gathered, what to expect, privacy and trademarks.

## Troubleshooting

**The icon shows "?"**
The adapter only sends status when something changes, and the PC can't ask for it. Switch the
headset off and on, or unplug and replug the adapter. Once one status has arrived, the app
remembers it across restarts and shows "as of HH:mm".

**The level looks too high right after charging**
The headset estimates its level from battery voltage, which reads high for a minute after the
cable is pulled out. It settles (for example 60 → 50 → 40 %).

**The PS5 shows bars and the app shows a different number**
The PS5 shows only a few coarse bars. The app shows the value the headset reports, in 10 % steps.
For example, 40 % showed as 1 bar on a PS5.

**The icon shows the red slash but the adapter is plugged in**
Check Device Manager for "Wireless Headset" under Sound devices. Your adapter may be a different
model that isn't supported yet; see [Supported headsets](Supported-Headsets).
