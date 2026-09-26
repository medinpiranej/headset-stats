using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core.Tests;

public sealed class StatusStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"headset-stats-{Guid.NewGuid():N}", "last-status.json");
    private readonly Pulse3DProtocol _protocol = new();

    [Fact]
    public void Round_trips_last_status_with_its_timestamp()
    {
        var store = new StatusStore(_path);
        var saved = _protocol.TryParse([0xB0, 0x02, 0x28, 0x28, 0xEF, 0x58, 0x11, 0x28])! with
        {
            ReceivedAt = new DateTimeOffset(2026, 9, 25, 14, 30, 0, TimeSpan.Zero),
        };

        store.Save(_protocol, saved);
        var loaded = store.Load(_protocol);

        Assert.NotNull(loaded);
        Assert.Equal(40, loaded.BatteryPercent);
        Assert.True(loaded.IsHeadsetOn);
        Assert.Equal(saved.ReceivedAt, loaded.ReceivedAt);
    }

    [Fact]
    public void Returns_null_when_nothing_saved()
    {
        Assert.Null(new StatusStore(_path).Load(_protocol));
    }

    [Fact]
    public void Returns_null_for_corrupt_file()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "not json");

        Assert.Null(new StatusStore(_path).Load(_protocol));
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_path)!;
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }
}
