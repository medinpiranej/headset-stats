using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core;

/// <summary>
/// Tracks whether the battery reading is unreliable because charging just ended.
/// The headset estimates its level from battery voltage, which reads high right after charging
/// (captured: 100 % → 70 % within 33 s, true level 50 %). A report sent while the headset links up
/// is measured fresh and accurate, so it ends the settling period, as does <see cref="Window"/> elapsing.
/// </summary>
public sealed class ChargeSettling
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private DateTimeOffset? _since;

    /// <summary>Feeds the next status; <paramref name="previous"/> may be a status restored from disk.</summary>
    public void Observe(HeadsetStatus? previous, HeadsetStatus current)
    {
        if (current.IsCharging == true || current.IsLinkUp)
            _since = null;
        else if (previous?.IsCharging == true && current.IsCharging == false)
            _since = current.ReceivedAt;
    }

    public bool IsSettling(DateTimeOffset now) => _since is { } since && now - since < Window;
}
