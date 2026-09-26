using HeadsetStats.Core.Hid;

namespace HeadsetStats.Core.Devices;

/// <summary>
/// Sony PULSE 3D wireless headset adapter (CFI-ZWD1), USB id 054C:0D5E.
/// Status arrives on the vendor collection (usage page 0xFF01) as 8-byte input report 0xB0:
/// <code>
/// B0 05 28 32 EF 11 11 64
///    │  │  │  │  │  │  └ volume 0–100
///    │  │  │  │  │  └ constant 0x11
///    │  │  │  │  └ event that triggered the report
///    │  │  │  └ flags: 0x04 headset on, 0x02 mic live
///    │  │  └ battery 0–100, or 0x80 while charging
///    │  └ game/chat balance, 40 = centred, ±10 per press
///    └ coarse volume level
/// </code>
/// See docs/PROTOCOL.md for the captures behind this layout.
/// </summary>
public sealed class Pulse3DProtocol : IHeadsetProtocol
{
    public const byte StatusReportId = 0xB0;
    private const ushort StatusUsagePage = 0xFF01;
    private const int ReportLength = 8;

    private const int BalanceOffset = 2;
    private const int BalanceCentre = 40;
    private const int BalanceStep = 10;

    private const int BatteryOffset = 3;
    private const byte BatteryCharging = 0x80;

    private const int FlagsOffset = 4;
    private const byte FlagHeadsetOn = 0x04;
    private const byte FlagMicLive = 0x02;

    private const int EventOffset = 5;
    private const byte EventVolumeUp = 0x11;
    private const byte EventVolumeDown = 0x12;
    private const byte EventGame = 0x13;
    private const byte EventChat = 0x14;
    private const byte EventMute = 0x15;
    private const byte EventBattery = 0x58;
    private const byte EventPowerChanging = 0x59;
    private const byte EventPoweredOn = 0x5A;
    private const byte EventPoweredOff = 0x5B;

    private const int VolumeOffset = 7;

    public string DisplayName => "PULSE 3D wireless headset";
    public ushort VendorId => 0x054C;
    public ushort ProductId => 0x0D5E;

    public DeviceDescription Description { get; } = new(
        HeadsetName: "PULSE 3D wireless headset",
        HeadsetModel: "CFI-ZWH1",
        AdapterModel: "CFI-ZWD1",
        Reports: "Battery (10 % steps), charging, on/off, mic mute, headset volume, game/chat balance",
        Limitations:
        [
            "Battery level comes in 10 % steps and is estimated by the headset from its battery voltage.",
            "Right after unplugging the charging cable the level reads too high (e.g. 100 % that drops to 50 %). Switch the headset off and on to get an accurate reading; the app marks the value as \"settling\" until then.",
            "While charging, the headset reports only \"charging\", with no level.",
            "The adapter sends status only when something changes (a button is pressed, the headset is switched on or off, the cable is plugged or unplugged, the adapter is plugged in). The PC can't ask for it, so the shown value may be from earlier.",
            "The headset's volume, mute and game/chat buttons act inside the headset; the app shows their state but they don't change Windows' volume or microphone. The monitor button isn't reported at all.",
        ]);

    public bool IsStatusCollection(HidDeviceInfo collection) =>
        collection.VendorId == VendorId && collection.ProductId == ProductId && collection.UsagePage == StatusUsagePage;

    public HeadsetStatus? TryParse(ReadOnlySpan<byte> report)
    {
        if (report.Length < ReportLength || report[0] != StatusReportId) return null;

        var flags = report[FlagsOffset];
        var isOn = (flags & FlagHeadsetOn) != 0;
        var trigger = report[EventOffset];
        var battery = report[BatteryOffset];
        var volume = report[VolumeOffset];
        if (battery > 100 && battery != BatteryCharging) return null;

        var charging = battery == BatteryCharging;
        return new HeadsetStatus(isOn, charging ? null : battery, charging, report.ToArray())
        {
            IsLinkUp = isOn && trigger is EventPowerChanging or EventPoweredOn,
            IsMicMuted = isOn ? (flags & FlagMicLive) == 0 : null,
            VolumePercent = volume <= 100 ? volume : null,
            GameChatBalance = (report[BalanceOffset] - BalanceCentre) / BalanceStep,
            Button = trigger switch
            {
                EventVolumeUp => HeadsetButton.VolumeUp,
                EventVolumeDown => HeadsetButton.VolumeDown,
                EventGame => HeadsetButton.Game,
                EventChat => HeadsetButton.Chat,
                EventMute => HeadsetButton.MicMute,
                _ => null,
            },
            Trigger = trigger switch
            {
                EventVolumeUp => "Volume up",
                EventVolumeDown => "Volume down",
                EventGame => "Game button",
                EventChat => "Chat button",
                EventMute => "Mic mute button",
                EventBattery => "Battery or charging update",
                EventPowerChanging => isOn ? "Switching on" : "Switching off",
                EventPoweredOn => "Switched on",
                EventPoweredOff => "Switched off",
                _ => $"Unknown (0x{trigger:X2})",
            },
        };
    }
}
