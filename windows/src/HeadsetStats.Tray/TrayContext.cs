using HeadsetStats.Core;

namespace HeadsetStats.Tray;

internal sealed class TrayContext : ApplicationContext
{
    private const int LowBatteryPercent = 15;

    private readonly HeadsetMonitor _monitor = new();
    private readonly NotifyIcon _tray = new();
    private readonly ToolStripMenuItem _statusItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _startupItem = new("Start with Windows") { CheckOnClick = true };
    private readonly SynchronizationContext _ui;
    private bool _lowBatteryNotified;

    public TrayContext()
    {
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        _startupItem.Checked = StartupRegistration.IsEnabled;
        _startupItem.CheckedChanged += (_, _) => StartupRegistration.IsEnabled = _startupItem.Checked;

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_startupItem);
        menu.Items.Add("About", null, (_, _) => ShowAbout());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        _tray.ContextMenuStrip = menu;
        _tray.Visible = true;

        _monitor.Changed += (_, _) => _ui.Post(_ => Render(), null);
        Render();
        _monitor.Start();
    }

    private void Render()
    {
        var status = _monitor.LastStatus;
        var charging = status?.IsCharging == true;
        var lastKnown = _monitor.LastKnownBatteryPercent;
        var percent = status is { IsHeadsetOn: true } ? status.BatteryPercent : null;
        var low = percent is not null && percent <= LowBatteryPercent && !charging;

        var text = _monitor.State switch
        {
            MonitorState.NoAdapter => "No headset adapter found",
            _ when status is null => "Waiting for headset… (turn it off and on)",
            _ when !status.IsHeadsetOn => "Headset is off" + (lastKnown is null ? "" : $" · last seen {lastKnown}%"),
            _ when charging => "Charging" + (lastKnown is null ? "" : $" · was {lastKnown}%"),
            _ => $"Battery {percent}% · updated {status.ReceivedAt:HH:mm}",
        };
        _statusItem.Text = text;
        _tray.Text = Truncate("Headset Stats\n" + text, 127);

        var icon = _monitor.State == MonitorState.NoAdapter || status is not { IsHeadsetOn: true }
            ? BatteryIcon.Create(BatteryIconKind.Unknown, null, low: false)
            : charging
                ? BatteryIcon.Create(BatteryIconKind.Charging, null, low: false)
                : BatteryIcon.Create(BatteryIconKind.Level, percent, low);
        var old = _tray.Icon;
        _tray.Icon = icon;
        old?.Dispose();

        if (low && !_lowBatteryNotified)
        {
            _tray.ShowBalloonTip(5000, "Headset battery low", $"{percent}% remaining", ToolTipIcon.Warning);
            _lowBatteryNotified = true;
        }
        else if (!low)
        {
            _lowBatteryNotified = false;
        }
    }

    private static void ShowAbout() => MessageBox.Show(
        "Headset Stats — shows the battery level of wireless headsets.\n\n" +
        "Open source (MIT) by Medin Piranej.\nhttps://github.com/medinpiranej/headset-stats\n\n" +
        "Not affiliated with or endorsed by Sony Interactive Entertainment.",
        "About Headset Stats", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _monitor.Dispose();
        _tray.Icon?.Dispose();
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
