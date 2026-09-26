using System.Globalization;
using System.Text;
using HeadsetStats.Core.Devices;
using HeadsetStats.Core.Hid;

namespace HeadsetStats.Core;

/// <summary>
/// Plain-text report for bug reports and new-device requests: app/OS version, the active device,
/// every Sony HID collection on the PC, and recent raw reports. Contains no file paths or user names;
/// it's only copied to the clipboard, never sent anywhere by the app.
/// </summary>
public static class DiagnosticReport
{
    public static string Create(HeadsetMonitor monitor, string appVersion, IReadOnlyList<HidDeviceInfo>? sonyCollections = null)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("### Headset Stats diagnostic report");
        sb.AppendLine(inv, $"- App: {appVersion}");
        sb.AppendLine(inv, $"- Windows: {Environment.OSVersion.Version}");
        sb.AppendLine(inv, $"- Created: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine();

        sb.AppendLine("#### Active device");
        var protocol = monitor.ActiveProtocol;
        var device = monitor.ActiveDevice;
        if (protocol is null || device is null)
        {
            sb.AppendLine("None (no supported or experimental adapter connected)");
        }
        else
        {
            sb.AppendLine(inv, $"- {protocol.DisplayName} ({(protocol.IsSupported ? "supported" : "NOT supported yet, experimental")})");
            sb.AppendLine(inv, $"- USB id {device.VendorId:X4}:{device.ProductId:X4}, firmware {device.VersionNumber >> 8:X}.{device.VersionNumber & 0xFF:X2}");
            sb.AppendLine(inv, $"- Reading usage page 0x{device.UsagePage:X4}, {device.InputReportLength}-byte input reports");
            sb.AppendLine(inv, $"- State: {monitor.State}{(monitor.IsSettling ? " (settling after charging)" : "")}");
            if (monitor.LastStatus is { } s) sb.AppendLine(inv, $"- Last status: {Describe(s)} at {s.ReceivedAt:yyyy-MM-dd HH:mm:ss}");
        }
        sb.AppendLine();

        sb.AppendLine("#### Sony HID collections (vendor 054C)");
        var collections = sonyCollections ?? HidDeviceInfo.Enumerate(ExperimentalProtocol.SonyVendorId);
        if (collections.Count == 0) sb.AppendLine("None found");
        foreach (var c in collections.OrderBy(c => c.ProductId).ThenBy(c => c.UsagePage))
        {
            sb.AppendLine(inv,
                $"- {c.VendorId:X4}:{c.ProductId:X4} rev {c.VersionNumber >> 8:X}.{c.VersionNumber & 0xFF:X2} · usage page 0x{c.UsagePage:X4} usage 0x{c.Usage:X4} · in {c.InputReportLength} out {c.OutputReportLength} feature {c.FeatureReportLength} · \"{c.ProductName}\"");
        }
        sb.AppendLine();

        var history = monitor.History;
        sb.AppendLine(inv, $"#### Reports this session ({history.Count}, oldest first)");
        sb.AppendLine("```");
        foreach (var entry in history)
            sb.AppendLine(inv, $"{entry.At:HH:mm:ss.fff}  {Hex(entry.Raw)}  {(entry.Decoded is { } d ? Describe(d) : "(not decoded)")}");
        sb.AppendLine("```");
        return sb.ToString();
    }

    private static string Describe(HeadsetStatus s)
    {
        var parts = new List<string>
        {
            s.IsHeadsetOn ? "on" : "off",
            s.IsCharging == true ? "charging" : s.BatteryPercent is { } p ? $"battery {p}%" : "battery ?",
        };
        if (s.IsMicMuted == true) parts.Add("mic muted");
        if (s.VolumePercent is { } v) parts.Add($"volume {v}%");
        if (s.GameChatBalance is { } b and not 0) parts.Add($"balance {b:+0;-0}");
        if (s.Trigger is { } t) parts.Add($"event: {t}");
        return string.Join(", ", parts);
    }

    private static string Hex(byte[] bytes) => string.Join(' ', bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
}
