# Microsoft Store listing: Headset Stats (beta)

Store ID **9P8NLJ432R5R** · package **MedinPiranej.HeadsetStats** · PFN `MedinPiranej.HeadsetStats_n7g8jr7bjpyvp`

Copy-paste source for Partner Center. Keep Sony trademarks out of the **name** and **keywords**; the
description may name compatible hardware (a factual compatibility statement) with the disclaimer.

## Product name

Headset Stats

## Short description (≤ 270 characters)

Shows your wireless headset's battery, charging, mic mute and button status in the Windows system tray.
Works with the PlayStation PULSE 3D wireless headset (USB adapter). Beta. Free, open source, offline.

## Description

Headset Stats puts your wireless headset's status where you can see it: in the system tray, next to the
clock.

Windows treats many headset USB adapters as a plain sound card, so it never shows the headset's battery.
Headset Stats listens to the adapter's status messages and shows:

• Battery level, with low-battery warnings (the tray icon turns amber, then red)
• Charging ⚡ and headset on/off
• Mic mute, the headset's own volume and game/chat balance
• A live history of everything the headset reports

Extras:
• Make the headset's Chat or Game button open an app, a shortcut or a website, or run a command
• Light and dark mode
• Copy device logs with one click for bug reports

Supported: PlayStation® PULSE 3D™ wireless headset with its USB adapter (CFI-ZWD1). Other Sony headsets can
be tried in an experimental, listen-only mode. Tell us if yours works!

This is a BETA. Please report problems or send us your headset's logs:
https://github.com/medinpiranej/headset-stats/issues

Private by design: no account, no ads, no telemetry, no internet connection. Open source (MIT):
https://github.com/medinpiranej/headset-stats

Headset Stats is an independent project and is not affiliated with or endorsed by Sony Interactive
Entertainment. PlayStation, PULSE and PULSE 3D are trademarks of Sony Interactive Entertainment Inc.

## What's new in this version

First beta: battery, charging, on/off, mic mute, volume and game/chat balance for the PULSE 3D; Chat/Game
button actions; device history and logs; light and dark mode.

## Product features (up to 20, ≤ 200 characters each)

1. Headset battery level in the system tray
2. Low-battery notification
3. Charging and power-off status
4. Mic mute indicator on the tray icon
5. Headset volume and game/chat balance
6. Run apps or commands from the headset's Chat and Game buttons
7. Live history of the headset's messages
8. Light and dark mode
9. No account, no ads, no telemetry, works offline
10. Open source (MIT)

## Keywords (up to 7)

headset battery · headset · battery indicator · system tray · wireless headset · mic mute · gaming headset

## Category

Utilities & tools (subcategory: none)

## Screenshots (1920×1080, in this order)

`docs/store/` has dark and light sets; use the dark set:

1. `store-device-dark.png`
2. `store-buttons-dark.png`
3. `store-history-dark.png`
4. `store-supporteddevices-dark.png`
5. `store-about-dark.png`

## Store logos and promotional art

In `docs/store/logos/` (regenerate with `HeadsetStats.exe --render-store-listing-art docs/store/logos`):

| Partner Center field | File |
|---|---|
| 1:1 app tile icon (300×300) | `app-tile-icon-300x300.png` (transparent) |
| 1:1 box art (2160×2160) | `box-art-2160x2160.png` |
| 2:3 poster art (1440×2160) | `poster-art-1440x2160.png` |
| 16:9 hero / super hero art (3840×2160) | `hero-art-3840x2160.png` |

## URLs

| Field | Value |
|---|---|
| Privacy policy | https://medinpiranej.github.io/headset-stats/privacy.html (needs GitHub Pages, see README) |
| Website | https://medinpiranej.github.io/headset-stats/ |
| Support contact | https://github.com/medinpiranej/headset-stats/issues |
| Store page (after approval) | https://apps.microsoft.com/detail/9P8NLJ432R5R |

## Restricted capability justification (runFullTrust)

Headset Stats is a Windows desktop (WinForms) tray application. It needs full trust to read status reports
from the headset's USB adapter through the Win32 HID API (hid.dll / SetupAPI), to show a notification-area
icon, and to start processes the user explicitly configures for the headset's Chat and Game buttons. It
makes no network connections.

## Notes for certification

Headset Stats shows status from a specific wireless headset: the PlayStation PULSE 3D wireless headset
with its USB adapter (USB id 054C:0D5E). Without that hardware the app still starts normally: it adds a
tray icon (grey headphones with a red slash, meaning "no adapter"), and double-clicking it opens the
device window, whose tabs (Device, History, Buttons, Supported devices, About) all work. The About tab
explains how the data is gathered.

The app is read-only towards the device, makes no network connections, and collects no data.
Screenshots with the hardware connected: <link to the repository's docs/store folder or a short video>.
Source code: https://github.com/medinpiranej/headset-stats

## Age rating questionnaire (IARC)

No violence, no sexual content, no gambling, no user-generated content sharing, no chat, no location
sharing, no purchases. Expect the lowest rating (e.g. PEGI 3 / ESRB Everyone).

## Pricing and availability

- Price: Free
- Markets: all
- **Visibility for the beta** (choose one):
  - *Public, labelled beta*: anyone can find and install it. The description says "BETA". Easiest to join.
  - *Private audience*: hidden from search; only Microsoft accounts you add to a customer group can install
    it via the direct link. Testers must send you their Microsoft account email.
