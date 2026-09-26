using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core;

/// <summary>One report as received from the adapter, with its decoding when the protocol understood it.</summary>
public sealed record ReportEntry(DateTimeOffset At, byte[] Raw, HeadsetStatus? Decoded);
