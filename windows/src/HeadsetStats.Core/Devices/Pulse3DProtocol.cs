using HeadsetStats.Core.Hid;

namespace HeadsetStats.Core.Devices;

/// <summary>
/// Sony PULSE 3D wireless headset adapter (CFI-ZWD1), USB id 054C:0D5E.
/// Status arrives on the vendor collection (usage page 0xFF01) as 8-byte input report 0xB0.
/// See docs/PROTOCOL.md for the captures behind this layout.
/// </summary>
public sealed class Pulse3DProtocol : IHeadsetProtocol
{
    public const byte StatusReportId = 0xB0;
    private const ushort StatusUsagePage = 0xFF01;
    private const int ReportLength = 8;

    private const int BatteryOffset = 3;
    private const byte BatteryCharging = 0x80;

    private const int StateOffset = 4;
    private const byte StateHeadsetOnBit = 0x04;

    public string DisplayName => "PULSE 3D wireless headset";
    public ushort VendorId => 0x054C;
    public ushort ProductId => 0x0D5E;

    public DeviceDescription Description { get; } = new(
        HeadsetName: "PULSE 3D wireless headset",
        HeadsetModel: "CFI-ZWH1",
        AdapterModel: "CFI-ZWD1",
        Reports: "Battery (10 % steps), charging, headset on/off",
        Limitations:
        [
            "Battery level comes in 10 % steps and is estimated by the headset from its battery voltage.",
            "Right after unplugging the charging cable the level reads high for about a minute, then settles.",
            "While charging, the headset reports only \"charging\", with no level.",
            "The adapter sends status only when something changes (headset switched on or off, cable plugged or unplugged, adapter plugged in). The PC can't ask for it, so the shown value may be from earlier.",
            "Volume and mic-mute buttons are handled inside the headset and aren't visible to the PC.",
        ]);

    public bool IsStatusCollection(HidDeviceInfo collection) =>
        collection.VendorId == VendorId && collection.ProductId == ProductId && collection.UsagePage == StatusUsagePage;

    public HeadsetStatus? TryParse(ReadOnlySpan<byte> report)
    {
        if (report.Length < ReportLength || report[0] != StatusReportId) return null;

        var isOn = (report[StateOffset] & StateHeadsetOnBit) != 0;
        var battery = report[BatteryOffset];

        if (battery == BatteryCharging)
            return new HeadsetStatus(isOn, BatteryPercent: null, IsCharging: true, report.ToArray());
        if (battery > 100)
            return null;

        return new HeadsetStatus(isOn, battery, IsCharging: false, report.ToArray());
    }
}
