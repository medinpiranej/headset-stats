# Headset Stats for macOS — planned

Suggested stack (build on a Mac with Xcode):

- **Swift + SwiftUI** `MenuBarExtra` for the menu bar item.
- **IOKit `IOHIDManager`**: match vendor `0x054C`, product `0x0D5E`, usage page `0xFF01`,
  and register an input-report callback. The report layout is in [`../docs/PROTOCOL.md`](../docs/PROTOCOL.md).
- App Sandbox entitlement: `com.apple.security.device.usb`.

Distribution: the Mac App Store, and notarized downloads from GitHub, both require the
Apple Developer Program (US$99/year).
