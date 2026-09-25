using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core.Tests;

public sealed class AppSettingsTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"headset-stats-{Guid.NewGuid():N}", "settings.json");

    [Fact]
    public void Defaults_to_nothing()
    {
        var settings = new AppSettings(_path);

        Assert.Equal(ButtonAction.Nothing, settings.Get(HeadsetButton.Chat));
        Assert.False(settings.Get(HeadsetButton.Game).IsConfigured);
    }

    [Fact]
    public void Persists_actions_across_instances()
    {
        var shortcut = new ButtonAction(ButtonActionKind.Open, @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Discord.lnk");
        var command = new ButtonAction(ButtonActionKind.Command, "start spotify:");
        var first = new AppSettings(_path);
        first.Set(HeadsetButton.Chat, shortcut);
        first.Set(HeadsetButton.Game, command);

        var second = new AppSettings(_path);

        Assert.Equal(shortcut, second.Get(HeadsetButton.Chat));
        Assert.Equal(command, second.Get(HeadsetButton.Game));
    }

    [Fact]
    public void Trying_unsupported_devices_is_on_by_default_and_persists()
    {
        Assert.True(new AppSettings(_path).TryUnsupportedDevices);

        new AppSettings(_path).TryUnsupportedDevices = false;

        Assert.False(new AppSettings(_path).TryUnsupportedDevices);
    }

    [Fact]
    public void Blank_target_is_not_configured()
    {
        Assert.False(new ButtonAction(ButtonActionKind.Command, "  ").IsConfigured);
    }

    [Fact]
    public void Corrupt_file_falls_back_to_nothing()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "{ not json");

        Assert.Equal(ButtonAction.Nothing, new AppSettings(_path).Get(HeadsetButton.Chat));
    }

    [Fact]
    public void Only_chat_and_game_are_assignable()
    {
        Assert.Equal([HeadsetButton.Chat, HeadsetButton.Game], AppSettings.Assignable);
    }

    [Theory]
    [InlineData((byte)0x13, HeadsetButton.Game)]
    [InlineData((byte)0x14, HeadsetButton.Chat)]
    [InlineData((byte)0x11, HeadsetButton.VolumeUp)]
    [InlineData((byte)0x15, HeadsetButton.MicMute)]
    public void Protocol_reports_which_button_was_pressed(byte trigger, HeadsetButton expected)
    {
        var status = new Pulse3DProtocol().TryParse([0xB0, 0x05, 0x28, 0x32, 0xEF, trigger, 0x11, 0x64]);

        Assert.Equal(expected, status!.Button);
    }

    [Fact]
    public void Non_button_reports_have_no_button()
    {
        var status = new Pulse3DProtocol().TryParse([0xB0, 0x05, 0x28, 0x32, 0xEF, 0x58, 0x11, 0x64]);

        Assert.Null(status!.Button);
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_path)!;
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }
}
