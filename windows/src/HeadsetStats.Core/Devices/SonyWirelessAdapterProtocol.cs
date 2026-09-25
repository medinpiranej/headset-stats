using HeadsetStats.Core.Hid;

namespace HeadsetStats.Core.Devices;

/// <summary>
/// Sony wireless headset USB adapter, USB id 054C:0D5E.
/// Status arrives on the vendor collection (usage page 0xFF01) as 8-byte input report 0xB0.
/// The layout is still being reverse-engineered; see docs/PROTOCOL.md.
/// </summary>
public sealed class SonyWirelessAdapterProtocol : IHeadsetProtocol
{
    public const byte StatusReportId = 0xB0;
    private const ushort StatusUsagePage = 0xFF01;
    private const int BatteryOffset = 2;

    public string DisplayName => "Sony wireless headset (054C:0D5E)";
    public ushort VendorId => 0x054C;
    public ushort ProductId => 0x0D5E;

    public bool IsStatusCollection(HidDeviceInfo collection) =>
        collection.VendorId == VendorId && collection.ProductId == ProductId && collection.UsagePage == StatusUsagePage;

    public HeadsetStatus? TryParse(ReadOnlySpan<byte> report)
    {
        if (report.Length <= BatteryOffset || report[0] != StatusReportId) return null;

        var battery = report[BatteryOffset];
        if (battery > 100) return null;

        // Byte 3 looks like a flag field (0x80 seen while connected); charging bit not identified yet.
        return new HeadsetStatus(battery, IsCharging: null, report.ToArray());
    }
}
