using HeadsetStats.Core.Hid;

namespace HeadsetStats.Core.Devices;

/// <summary>
/// A Sony device the app doesn't support yet, tried read-only so owners can check whether it works
/// and send us logs. Reports are decoded with the PULSE 3D format when they match its shape
/// (report 0xB0, 8 bytes); anything else is only recorded raw.
/// </summary>
public sealed class ExperimentalProtocol : IHeadsetProtocol
{
    public const ushort SonyVendorId = 0x054C;

    // Sony devices that are known not to be headsets: never tried.
    private static readonly HashSet<ushort> ExcludedProductIds =
    [
        0x05C4, // DualShock 4 (v1)
        0x09CC, // DualShock 4 (v2)
        0x0BA0, // DualShock 4 USB wireless adaptor
        0x0CE6, // DualSense
        0x0DF2, // DualSense Edge
    ];

    private static readonly Pulse3DProtocol Pulse3D = new();

    public ExperimentalProtocol(ushort productId)
    {
        ProductId = productId;
        Description = new DeviceDescription(
            HeadsetName: $"Unsupported Sony device ({SonyVendorId:X4}:{productId:X4})",
            HeadsetModel: "Unknown",
            AdapterModel: "Unknown",
            Reports: "Not supported yet: trying the PULSE 3D message format (experimental)",
            Limitations:
            [
                "This device hasn't been tested. The app only listens to it and never sends it anything.",
                "Values are decoded with the PULSE 3D format and may be wrong or missing.",
                "Please tell us whether it works: use \"Copy device logs\" and open an issue on the project page.",
            ]);
    }

    public string DisplayName => Description.HeadsetName;
    public ushort VendorId => SonyVendorId;
    public ushort ProductId { get; }
    public DeviceDescription Description { get; }
    public bool IsSupported => false;

    public bool IsStatusCollection(HidDeviceInfo collection) =>
        collection.VendorId == VendorId && collection.ProductId == ProductId
        && collection.UsagePage >= 0xFF00 && collection.InputReportLength > 0;

    public HeadsetStatus? TryParse(ReadOnlySpan<byte> report) => Pulse3D.TryParse(report);

    /// <summary>Sony devices worth trying: not supported by another protocol and not a known non-headset.</summary>
    public static IEnumerable<(ExperimentalProtocol Protocol, HidDeviceInfo Collection)> FindCandidates(
        IEnumerable<IHeadsetProtocol> supported, IReadOnlyList<HidDeviceInfo>? sonyCollections = null)
    {
        var known = supported.Select(p => (p.VendorId, p.ProductId)).ToHashSet();
        var collections = sonyCollections ?? HidDeviceInfo.Enumerate(SonyVendorId);
        foreach (var device in collections.GroupBy(c => c.ProductId))
        {
            if (ExcludedProductIds.Contains(device.Key) || known.Contains((SonyVendorId, device.Key))) continue;
            if (Pick(device) is { } collection) yield return (new ExperimentalProtocol(device.Key), collection);
        }
    }

    /// <summary>The collection most likely to carry status: vendor page 0xFF01 (as on the PULSE 3D), else any vendor page with input.</summary>
    private static HidDeviceInfo? Pick(IEnumerable<HidDeviceInfo> collections)
    {
        var withInput = collections.Where(c => c.UsagePage >= 0xFF00 && c.InputReportLength > 0).ToList();
        return withInput.FirstOrDefault(c => c.UsagePage == 0xFF01) ?? withInput.FirstOrDefault();
    }
}
