using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core.Tests;

// Button presses captured 2026-09-26 00:00–00:08 from a PULSE 3D (054C:0D5E). See docs/PROTOCOL.md.
public class Pulse3DControlsTests
{
    private readonly Pulse3DProtocol _protocol = new();

    private HeadsetStatus Parse(params byte[] report) => _protocol.TryParse(report)!;

    [Theory]
    [InlineData(new byte[] { 0xB0, 0x03, 0x28, 0x32, 0xEF, 0x11, 0x11, 0x32 }, 50, "Volume up")]
    [InlineData(new byte[] { 0xB0, 0x05, 0x28, 0x32, 0xEF, 0x11, 0x11, 0x64 }, 100, "Volume up")]
    [InlineData(new byte[] { 0xB0, 0x03, 0x28, 0x32, 0xEF, 0x12, 0x11, 0x46 }, 70, "Volume down")]
    public void Reads_headset_volume(byte[] report, int volume, string trigger)
    {
        var status = Parse(report);

        Assert.Equal(volume, status.VolumePercent);
        Assert.Equal(trigger, status.Trigger);
        Assert.Equal(50, status.BatteryPercent);
    }

    [Fact]
    public void Reads_mic_mute()
    {
        var muted = Parse(0xB0, 0x05, 0x28, 0x32, 0xED, 0x15, 0x11, 0x64);
        var live = Parse(0xB0, 0x05, 0x28, 0x32, 0xEF, 0x15, 0x11, 0x64);

        Assert.True(muted.IsMicMuted);
        Assert.False(live.IsMicMuted);
        Assert.Equal("Mic mute button", muted.Trigger);
        Assert.True(muted.IsHeadsetOn);
    }

    [Theory]
    [InlineData((byte)0x1E, (byte)0x14, -1, "Chat button")]
    [InlineData((byte)0x0A, (byte)0x14, -3, "Chat button")]
    [InlineData((byte)0x14, (byte)0x13, -2, "Game button")]
    [InlineData((byte)0x28, (byte)0x13, 0, "Game button")]
    public void Reads_game_chat_balance(byte balance, byte trigger, int expected, string triggerName)
    {
        var status = Parse(0xB0, 0x05, balance, 0x32, 0xEF, trigger, 0x11, 0x64);

        Assert.Equal(expected, status.GameChatBalance);
        Assert.Equal(triggerName, status.Trigger);
    }

    [Fact]
    public void Reads_power_sequence()
    {
        Assert.Equal("Switching off", Parse(0xB0, 0x05, 0x28, 0x32, 0xEB, 0x59, 0x11, 0x64).Trigger);
        Assert.Equal("Switched off", Parse(0xB0, 0x05, 0x28, 0x32, 0xE3, 0x5B, 0x11, 0x64).Trigger);
        Assert.Equal("Switching on", Parse(0xB0, 0x05, 0x28, 0x32, 0xEF, 0x59, 0x11, 0x64).Trigger);
        Assert.Equal("Switched on", Parse(0xB0, 0x05, 0x28, 0x32, 0xEF, 0x5A, 0x11, 0x64).Trigger);
    }

    [Fact]
    public void Mic_state_is_unknown_while_headset_is_off()
    {
        Assert.Null(Parse(0xB0, 0x05, 0x28, 0x32, 0xE3, 0x5B, 0x11, 0x64).IsMicMuted);
    }
}
