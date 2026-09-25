using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core.Tests;

// Reports captured 2026-09-25 from a PULSE 3D adapter (054C:0D5E). See docs/PROTOCOL.md.
public class Pulse3DProtocolTests
{
    private readonly Pulse3DProtocol _protocol = new();

    [Theory]
    [InlineData(new byte[] { 0xB0, 0x02, 0x28, 0x28, 0xEF, 0x58, 0x11, 0x28 }, 40)]
    [InlineData(new byte[] { 0xB0, 0x02, 0x28, 0x1E, 0xEB, 0x59, 0x11, 0x28 }, 30)]
    [InlineData(new byte[] { 0xB0, 0x02, 0x28, 0x3C, 0xEF, 0x58, 0x11, 0x28 }, 60)]
    public void Reads_battery_percent_when_not_charging(byte[] report, int expected)
    {
        var status = _protocol.TryParse(report);

        Assert.NotNull(status);
        Assert.Equal(expected, status.BatteryPercent);
        Assert.False(status.IsCharging);
        Assert.Equal(report, status.RawReport);
    }

    [Fact]
    public void Reports_charging_without_a_level()
    {
        var status = _protocol.TryParse([0xB0, 0x02, 0x28, 0x80, 0xEF, 0x58, 0x11, 0x28]);

        Assert.NotNull(status);
        Assert.True(status.IsCharging);
        Assert.Null(status.BatteryPercent);
    }

    [Theory]
    [InlineData((byte)0xEF, true)]   // on
    [InlineData((byte)0xEB, false)]  // switching off
    [InlineData((byte)0xE3, false)]  // off
    public void Reads_headset_power_state(byte state, bool expectedOn)
    {
        var status = _protocol.TryParse([0xB0, 0x02, 0x28, 0x28, state, 0x58, 0x11, 0x28]);

        Assert.NotNull(status);
        Assert.Equal(expectedOn, status.IsHeadsetOn);
    }

    [Theory]
    [InlineData((byte)0xEF, (byte)0x58, false)] // on, steady
    [InlineData((byte)0xEF, (byte)0x59, true)]  // on, linking up
    [InlineData((byte)0xEF, (byte)0x5A, true)]  // on, link up done
    [InlineData((byte)0xEB, (byte)0x59, false)] // switching off
    [InlineData((byte)0xE3, (byte)0x5B, false)] // off
    public void Detects_link_up_reports(byte state, byte link, bool expected)
    {
        var status = _protocol.TryParse([0xB0, 0x02, 0x28, 0x28, state, link, 0x11, 0x28]);

        Assert.NotNull(status);
        Assert.Equal(expected, status.IsLinkUp);
    }

    [Fact]
    public void Ignores_other_report_ids()
    {
        Assert.Null(_protocol.TryParse([0xA0, 0x02, 0x28, 0x28, 0xEF, 0x58, 0x11, 0x28]));
    }

    [Fact]
    public void Rejects_out_of_range_battery()
    {
        Assert.Null(_protocol.TryParse([0xB0, 0x02, 0x28, 0xC8, 0xEF, 0x58, 0x11, 0x28]));
    }

    [Fact]
    public void Rejects_truncated_report()
    {
        Assert.Null(_protocol.TryParse([0xB0, 0x02, 0x28, 0x28]));
    }
}
