using HeadsetStats.Core.Hid;

namespace HeadsetStats.Core.Devices;

/// <summary>Describes one supported adapter and how to decode its reports.</summary>
public interface IHeadsetProtocol
{
    string DisplayName { get; }
    ushort VendorId { get; }
    ushort ProductId { get; }
    DeviceDescription Description { get; }

    /// <summary>Whether this HID collection is the one that carries status reports.</summary>
    bool IsStatusCollection(HidDeviceInfo collection);

    /// <summary>Decodes an input report, or returns null if it is not a status report.</summary>
    HeadsetStatus? TryParse(ReadOnlySpan<byte> report);
}
