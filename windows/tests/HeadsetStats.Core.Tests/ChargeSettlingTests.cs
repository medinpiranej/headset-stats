using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core.Tests;

// Sequence captured 2026-09-25 from a PULSE 3D: charged from 40 % for ~35 min, cable pulled,
// then the adapter replugged. True level afterwards ≈ 50 %.
public class ChargeSettlingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 25, 23, 18, 0, TimeSpan.FromHours(2));
    private readonly Pulse3DProtocol _protocol = new();

    private HeadsetStatus At(TimeSpan offset, params byte[] report) =>
        _protocol.TryParse(report)! with { ReceivedAt = T0 + offset };

    private HeadsetStatus Charging => At(TimeSpan.Zero, 0xB0, 0x02, 0x28, 0x80, 0xEF, 0x58, 0x11, 0x28);
    private HeadsetStatus Unplugged100 => At(TimeSpan.FromMinutes(27.5), 0xB0, 0x02, 0x28, 0x64, 0xEF, 0x58, 0x11, 0x28);
    private HeadsetStatus Drift70 => At(TimeSpan.FromMinutes(28), 0xB0, 0x02, 0x28, 0x46, 0xEF, 0x58, 0x11, 0x28);
    private HeadsetStatus LinkUp50 => At(TimeSpan.FromMinutes(31.5), 0xB0, 0x02, 0x28, 0x32, 0xEF, 0x5A, 0x11, 0x28);

    [Fact]
    public void Reading_right_after_charging_is_settling()
    {
        var settling = new ChargeSettling();
        settling.Observe(null, Charging);
        settling.Observe(Charging, Unplugged100);

        Assert.True(settling.IsSettling(Unplugged100.ReceivedAt));

        settling.Observe(Unplugged100, Drift70);
        Assert.True(settling.IsSettling(Drift70.ReceivedAt));
    }

    [Fact]
    public void Link_up_report_ends_settling()
    {
        var settling = new ChargeSettling();
        settling.Observe(Charging, Unplugged100);
        settling.Observe(Unplugged100, LinkUp50);

        Assert.True(LinkUp50.IsLinkUp);
        Assert.False(settling.IsSettling(LinkUp50.ReceivedAt));
    }

    [Fact]
    public void Settling_expires_after_the_window()
    {
        var settling = new ChargeSettling();
        settling.Observe(Charging, Unplugged100);

        Assert.False(settling.IsSettling(Unplugged100.ReceivedAt + ChargeSettling.Window + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Normal_readings_are_not_settling()
    {
        var settling = new ChargeSettling();
        settling.Observe(LinkUp50, Drift70 with { ReceivedAt = LinkUp50.ReceivedAt.AddMinutes(30) });

        Assert.False(settling.IsSettling(LinkUp50.ReceivedAt.AddMinutes(30)));
    }

    [Fact]
    public void Status_restored_from_disk_counts_as_previous()
    {
        // App restarted while charging; the first live report after unplugging must still be flagged.
        var settling = new ChargeSettling();
        settling.Observe(previous: Charging, current: Unplugged100);

        Assert.True(settling.IsSettling(Unplugged100.ReceivedAt.AddSeconds(30)));
    }
}
