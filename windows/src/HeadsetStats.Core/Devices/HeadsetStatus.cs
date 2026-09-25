namespace HeadsetStats.Core.Devices;

/// <summary>A decoded status update from a headset adapter.</summary>
/// <param name="IsHeadsetOn">Whether the headset is switched on and linked to the adapter.</param>
/// <param name="BatteryPercent">Battery level 0–100, or null when the headset doesn't report it (e.g. while charging).</param>
/// <param name="IsCharging">True/false when the protocol reports it, null when unknown.</param>
/// <param name="RawReport">The undecoded report, kept for diagnostics.</param>
public sealed record HeadsetStatus(bool IsHeadsetOn, int? BatteryPercent, bool? IsCharging, byte[] RawReport)
{
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// Sent while the headset is (re)establishing its link, e.g. just switched on or the adapter just plugged in.
    /// The battery estimate in these reports is freshly measured.
    /// </summary>
    public bool IsLinkUp { get; init; }

    /// <summary>Headset's own volume 0–100 (set with its buttons; independent of the Windows volume), if reported.</summary>
    public int? VolumePercent { get; init; }

    /// <summary>Whether the headset's mic mute is on, if reported.</summary>
    public bool? IsMicMuted { get; init; }

    /// <summary>Game/chat balance: 0 = centred, negative = toward chat, positive = toward game (in button presses), if reported.</summary>
    public int? GameChatBalance { get; init; }

    /// <summary>What triggered this report, in plain words (e.g. "Volume up"), if known.</summary>
    public string? Trigger { get; init; }
}
