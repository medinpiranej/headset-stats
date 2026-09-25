# Headset Stats for Linux — planned

Suggested stack (build on a Linux PC):

- Read reports from **hidraw** (via `hidapi`, or directly from `/dev/hidraw*`).
- Tray icon via **StatusNotifierItem** (for example Rust + `ksni`, or Python + AppIndicator).
- A **udev rule** so non-root users can open the adapter:
  ```
  KERNEL=="hidraw*", ATTRS{idVendor}=="054c", ATTRS{idProduct}=="0d5e", TAG+="uaccess"
  ```

Report layout: [`../docs/PROTOCOL.md`](../docs/PROTOCOL.md).
Distribution: Flathub, AUR, or `.deb`/`.rpm` on GitHub Releases (all free).
Consider adding the device to [HeadsetControl](https://github.com/Sapd/HeadsetControl)
as well, since many Linux users already use it.
