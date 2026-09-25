using System.Text.Json;
using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core;

/// <summary>
/// Persists the last status report per adapter. Adapters only report on change, so without this
/// the app would show nothing after a restart until the headset is power-cycled or plugged in.
/// </summary>
public sealed class StatusStore
{
    private sealed record Entry(ushort VendorId, ushort ProductId, string RawReport, DateTimeOffset ReceivedAt);

    private readonly string _path;

    public StatusStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HeadsetStats", "last-status.json");
    }

    public void Save(IHeadsetProtocol protocol, HeadsetStatus status)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var entry = new Entry(protocol.VendorId, protocol.ProductId, Convert.ToHexString(status.RawReport), status.ReceivedAt);
            File.WriteAllText(_path, JsonSerializer.Serialize(entry));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: losing the cache only costs the startup display.
        }
    }

    public HeadsetStatus? Load(IHeadsetProtocol protocol)
    {
        try
        {
            var entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(_path));
            if (entry is null || entry.VendorId != protocol.VendorId || entry.ProductId != protocol.ProductId) return null;
            return protocol.TryParse(Convert.FromHexString(entry.RawReport)) is { } status
                ? status with { ReceivedAt = entry.ReceivedAt }
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            return null;
        }
    }
}
