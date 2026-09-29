# Microsoft Store listing: Headset Stats (beta)

Store ID **9P8NLJ432R5R** · package **MedinPiranej.HeadsetStats** · PFN `MedinPiranej.HeadsetStats_n7g8jr7bjpyvp`

Copy-paste source for Partner Center. Keep Sony trademarks out of the **name** and **keywords**; the
description may name compatible hardware (a factual compatibility statement) with the disclaimer.

> **Paste the texts below exactly as they are.** Each paragraph is a single line on purpose: the Store keeps
> line breaks, so hard-wrapped text shows up broken mid-sentence. Blank lines separate paragraphs.

## Product name

Headset Stats

## Short description

Shows the battery level of your PlayStation® PULSE 3D™ wireless headset on your Windows PC, right in the system tray, plus charging, mic mute and button status. Other Sony headsets can be tried in an experimental mode. Free, open source and offline. Beta.

## Description

Headset Stats shows the battery level of your PlayStation® PULSE 3D™ wireless headset on your Windows PC.

When you plug the PULSE 3D's USB adapter into a PC, Windows only sees a sound card and never shows the headset's battery. Headset Stats reads the adapter's status messages and puts the battery level in the system tray, next to the clock.

WHAT YOU GET
• Battery level in the tray: green, amber, then red as it drains, with a low-battery warning at 15 %
• Charging ⚡ and headset on/off
• Mic mute badge, the headset's own volume and game/chat balance
• Optional: make the headset's Chat or Game button open an app, a shortcut or a website, or run a command
• A device window with a live history of everything the headset reports
• Light and dark mode

REQUIREMENTS
• PlayStation® PULSE 3D™ wireless headset (CFI-ZWH1) with its USB wireless adapter (CFI-ZWD1) plugged into your PC
• Windows 10 (version 1809 or later) or Windows 11

OTHER SONY HEADSETS (EXPERIMENTAL)
The PULSE Elite and PULSE Explore get their battery shown on PC by Sony's own PlayStation Link PC driver. Have a different Sony headset, such as an older PlayStation headset? Headset Stats can try it in an experimental, listen-only mode: if its adapter uses the same kind of messages, the battery may show up too. Whether it works or not, please tell us with the in-app "Report a device on GitHub" button. That's how new headsets get supported.

GOOD TO KNOW
• The headset reports its battery in 10 % steps.
• Right after charging it reads too high for a while; the app marks the value as "settling" until it's reliable.
• The headset only sends status when something changes. Switch it off and on to refresh.

This is a BETA. Report problems or ideas at https://github.com/medinpiranej/headset-stats/issues

Private by design: no account, no ads, no telemetry, no internet connection. Free and open source (MIT): https://github.com/medinpiranej/headset-stats

Headset Stats is an independent project and is not affiliated with or endorsed by Sony Interactive Entertainment. PlayStation, PULSE, PULSE 3D, PULSE Elite and PULSE Explore are trademarks of Sony Interactive Entertainment Inc., used only to identify compatible hardware.

## What's new in this version

Clearer description of what the app does and which headsets it supports. Beta: battery level, charging, on/off, mic mute, volume and game/chat balance for the PULSE 3D wireless headset; Chat/Game button actions; experimental mode for other Sony headsets.

## Product features (up to 20, ≤ 200 characters each)

1. Battery level of the PlayStation PULSE 3D wireless headset in the Windows system tray
2. Low-battery warning at 15 %
3. Charging and headset on/off status
4. Mic mute indicator on the tray icon
5. The headset's own volume and game/chat balance
6. Run apps, shortcuts, websites or commands from the headset's Chat and Game buttons
7. Experimental, listen-only mode for other Sony headsets
8. Live history of the headset's messages
9. Light and dark mode
10. No account, no ads, no telemetry, works offline; open source (MIT)

## Keywords (up to 7)

headset battery · battery level · headset battery tray · wireless headset · mic mute · gaming headset · battery indicator

(Sony product names stay out of the keywords, following the trademark rules in CLAUDE.md; the description names the PULSE 3D.)

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
