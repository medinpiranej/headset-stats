namespace HeadsetStats.Core.Devices;

public static class SupportedHeadsets
{
    public static IReadOnlyList<IHeadsetProtocol> All { get; } =
    [
        new SonyWirelessAdapterProtocol(),
    ];
}
