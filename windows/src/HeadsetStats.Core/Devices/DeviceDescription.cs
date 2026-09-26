namespace HeadsetStats.Core.Devices;

/// <summary>User-facing description of a supported headset, shown in the app's device list.</summary>
/// <param name="HeadsetName">Product name, e.g. "PULSE 3D wireless headset".</param>
/// <param name="HeadsetModel">Manufacturer model number of the headset.</param>
/// <param name="AdapterModel">Manufacturer model number of the USB adapter.</param>
/// <param name="Reports">What the app can show for this headset.</param>
/// <param name="Limitations">Things users should expect, in plain language.</param>
public sealed record DeviceDescription(
    string HeadsetName,
    string HeadsetModel,
    string AdapterModel,
    string Reports,
    IReadOnlyList<string> Limitations);
