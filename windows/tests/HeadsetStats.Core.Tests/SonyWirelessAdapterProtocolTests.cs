using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core.Tests;

public class SonyWirelessAdapterProtocolTests
{
    private readonly SonyWirelessAdapterProtocol _protocol = new();

    [Fact]
    public void Parses_battery_from_captured_status_report()
    {
        // Captured 2026-09-25 from 054C:0D5E after the headset was switched on.
        byte[] report = [0xB0, 0x02, 0x28, 0x80, 0xEF, 0x58, 0x11, 0x1E];

        var status = _protocol.TryParse(report);

        Assert.NotNull(status);
        Assert.Equal(40, status.BatteryPercent);
        Assert.Null(status.IsCharging);
        Assert.Equal(report, status.RawReport);
    }

    [Fact]
    public void Ignores_other_report_ids()
    {
        Assert.Null(_protocol.TryParse([0xA0, 0x02, 0x28, 0x80, 0, 0, 0, 0]));
    }

    [Fact]
    public void Rejects_out_of_range_battery()
    {
        Assert.Null(_protocol.TryParse([0xB0, 0x02, 0xC8, 0x80, 0, 0, 0, 0]));
    }

    [Fact]
    public void Rejects_truncated_report()
    {
        Assert.Null(_protocol.TryParse([0xB0, 0x02]));
    }
}
