# Letting people know about Headset Stats

The pitch: **the PULSE 3D has no way to show its battery on a PC.** Sony's PlayStation Link PC driver
(Sept 2025) only covers the PULSE Elite and PULSE Explore. Headset Stats fills that gap for free.

Rules of thumb: post as yourself, say you're the developer, answer the question first and link second,
read each community's self-promotion rules, and don't paste the same text everywhere.

## Where

| Place | Why | How |
|---|---|---|
| r/PS5, r/PlayStation | Most PULSE 3D owners | Check self-promo rules; some allow it only in weekly threads. Answer existing "pulse 3d battery on PC" questions (search `site:reddit.com pulse 3d battery pc`) |
| r/pcgaming, r/Windows11 | PC players using PS5 gear | "I made a free tray app…" post with a screenshot |
| r/opensource, r/csharp, r/dotnet | Developers | The reverse-engineering story (see draft 2) |
| Hacker News ("Show HN") | Developers, tinkerers | Draft 2, link to the repo |
| [ResetEra: PS5 Pulse 3D Wireless Headset thread](https://www.resetera.com/threads/ps5-pulse-3d-wireless-headset.290675/) | Long-running owners' thread | A short reply (draft 1) |
| [Linus Tech Tips: PULSE 3D review thread](https://linustechtips.com/topic/1439806-sonyplaystation-pulse-3d-headset-short-review-and-initial-impressions-spoilers-not-even-worth-the-effort-of-measuring/) | PC-focused audience | A reply about PC use |
| [GameFAQs PS5 board](https://gamefaqs.gamespot.com/boards/264562-playstation-5) | PULSE 3D battery threads exist there | Only where PC use comes up |
| Steam Community discussions | PC players | Answer headset battery questions |
| YouTube "how to check PULSE 3D battery" videos | Big audience asking exactly this | A helpful comment ("on PC there's a free app…") |
| AlternativeTo, Softpedia | Software directories that rank well in search | Submit a listing |

## Draft 1: answering "how do I see the PULSE 3D battery on PC?"

> Windows doesn't show it: the PULSE 3D's USB adapter only shows up as a sound card. I got annoyed by that and
> made a small free app that reads the adapter and puts the battery level in the system tray (also charging,
> mic mute, volume). It's on the Microsoft Store as a beta: https://apps.microsoft.com/detail/9P8NLJ432R5R.
> It's open source: https://github.com/medinpiranej/headset-stats. (I'm the developer. Feedback welcome, especially
> from anyone with other Sony headsets.)

## Draft 2: developer / Show HN post

> **Show HN: I reverse-engineered the PS5 PULSE 3D headset's USB adapter to show its battery on Windows**
>
> The PULSE 3D's USB adapter shows up on Windows as a plain sound card, so there's no battery indicator. Next to
> the audio interface it has a vendor HID channel that sends an 8-byte report whenever something changes. I
> decoded battery, charging, power, mic mute, volume, game/chat balance and button events by recording reports
> while pressing each button and plugging the charger in and out. Two quirks: it can't be polled, only listened to,
> and it over-reports the battery right after charging (100 % that's really 50 %) until it re-links.
>
> The result is a small .NET tray app (free, MIT, no telemetry): https://github.com/medinpiranej/headset-stats.
> The protocol notes and raw captures are in docs/PROTOCOL.md. On the Microsoft Store: https://apps.microsoft.com/detail/9P8NLJ432R5R
