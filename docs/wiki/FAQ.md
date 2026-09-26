# FAQ

**Why doesn't Windows show my PULSE 3D's battery by itself?**
The adapter presents itself as a USB audio device. The battery value only appears in a
vendor-specific message, which Windows doesn't understand.

**Why does the app sometimes show an old value, or "?"?**
The adapter only sends status when something changes, and the PC can't ask for it. The app shows
the last status it saw with a timestamp. Switch the headset off and on to refresh it.

**Why can't I see the percentage while charging?**
While charging, the headset reports "charging" instead of a level. The app shows ⚡ and the last known level.

**Can the PS5 show a percentage instead of bars?**
No. That's the PS5's own interface, which this project doesn't change.

**Does it work with the PULSE Elite / PULSE Explore?**
Not yet. They use a different adapter. See [Supported headsets](Supported-Headsets) for how to help.

**Does it send any data anywhere?**
No. It has no network access and no telemetry.

**Is this made by Sony?**
No. It's an independent open-source project by Medin Piranej, not affiliated with or endorsed by Sony.
