using HeadsetStats.Core.Devices;
using HeadsetStats.Core.Hid;

namespace HeadsetStats.Core.Tests;

public class ExperimentalProtocolTests
{
    private static HidDeviceInfo Collection(ushort productId, ushort usagePage, int input) =>
        new($"path-{productId:X4}-{usagePage:X4}", 0x054C, productId, 0x0100, usagePage, 0x0001, input, 0, 0, "Hid Interface");

    [Fact]
    public void Skips_supported_devices_and_controllers()
    {
        var collections = new[]
        {
            Collection(0x0D5E, 0xFF01, 8),  // PULSE 3D: supported already
            Collection(0x0CE6, 0xFF00, 64), // DualSense: not a headset
            Collection(0x0E5F, 0xFF01, 8),  // unknown Sony device
        };

        var candidates = ExperimentalProtocol.FindCandidates(SupportedHeadsets.All, collections).ToList();

        var (protocol, collection) = Assert.Single(candidates);
        Assert.Equal(0x0E5F, protocol.ProductId);
        Assert.False(protocol.IsSupported);
        Assert.Equal(0xFF01, collection.UsagePage);
    }

    [Fact]
    public void Prefers_usage_page_FF01_then_any_vendor_page_with_input()
    {
        var collections = new[]
        {
            Collection(0x0E60, 0x000C, 5),  // consumer control: not a vendor page
            Collection(0x0E60, 0xFF03, 0),  // vendor, but no input reports
            Collection(0x0E60, 0xFF00, 64),
        };

        var (_, collection) = Assert.Single(ExperimentalProtocol.FindCandidates([], collections));
        Assert.Equal(0xFF00, collection.UsagePage);
    }

    [Fact]
    public void Devices_without_a_vendor_input_collection_are_not_candidates()
    {
        Assert.Empty(ExperimentalProtocol.FindCandidates([], [Collection(0x0E61, 0x000C, 5)]));
    }

    [Fact]
    public void Decodes_reports_in_the_PULSE_3D_format()
    {
        var status = new ExperimentalProtocol(0x0E5F).TryParse([0xB0, 0x05, 0x28, 0x32, 0xEF, 0x58, 0x11, 0x64]);

        Assert.Equal(50, status!.BatteryPercent);
    }

    [Fact]
    public void Other_formats_are_left_undecoded()
    {
        Assert.Null(new ExperimentalProtocol(0x0E5F).TryParse([0x01, 0x02, 0x03]));
    }

    [Fact]
    public void Diagnostic_report_contains_devices_and_no_paths()
    {
        using var monitor = new HeadsetMonitor();
        var collections = new[] { Collection(0x0D5E, 0xFF01, 8) };

        var report = DiagnosticReport.Create(monitor, "0.1.0", collections);

        Assert.Contains("054C:0D5E", report);
        Assert.Contains("usage page 0xFF01", report);
        Assert.DoesNotContain("path-", report);
        Assert.DoesNotContain(Environment.UserName, report, StringComparison.OrdinalIgnoreCase);
    }
}
