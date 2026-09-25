# Headset Stats for Android — planned

Suggested stack:

- **Kotlin + Jetpack Compose**.
- **USB Host API** (`UsbManager`) to open the adapter over USB-C/OTG. Declare a
  `USB_DEVICE_ATTACHED` intent filter with vendor `0x054C` / product `0x0D5E`, then read the
  interrupt IN endpoint of interface 3.
- Show the level in a persistent notification or home-screen widget.

Report layout: [`../docs/PROTOCOL.md`](../docs/PROTOCOL.md).
Distribution: Google Play (one-time US$25 developer fee) and/or F-Droid (free).
