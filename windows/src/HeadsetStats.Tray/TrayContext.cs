using HeadsetStats.Core;
using HeadsetStats.Core.Devices;

namespace HeadsetStats.Tray;

internal sealed class TrayContext : ApplicationContext
{
    private readonly StatusStore _store = new();
    private readonly AppSettings _settings = new();
    private readonly Dictionary<HeadsetButton, DateTime> _lastActionRun = [];
    private readonly HeadsetMonitor _monitor;
    private readonly NotifyIcon _tray = new();
    private DeviceWindow? _window;
    private readonly ToolStripMenuItem _modelItem = new() { Enabled = false, Visible = false };
    private readonly ToolStripMenuItem _statusItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _hintItem = new("Tip: switch the headset off and on for an accurate reading") { Enabled = false, Visible = false };
    // Re-renders periodically so time-based states (settling after charging) expire without a new report.
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 30_000 };
    private readonly ToolStripMenuItem _startupItem = new("Start with Windows") { CheckOnClick = true };
    private readonly SynchronizationContext _ui;
    private bool _lowBatteryNotified;

    public TrayContext()
    {
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _monitor = new HeadsetMonitor(store: _store, tryUnsupported: () => _settings.TryUnsupportedDevices);

        _startupItem.Checked = StartupRegistration.IsEnabled;
        _startupItem.CheckedChanged += (_, _) => StartupRegistration.IsEnabled = _startupItem.Checked;

        var details = new ToolStripMenuItem("Device details…", null, (_, _) => ShowWindow(DeviceWindowTab.Device));
        details.Font = new Font(details.Font, FontStyle.Bold);

        var menu = new ContextMenuStrip();
        menu.Items.Add(_modelItem);
        menu.Items.Add(_statusItem);
        menu.Items.Add(_hintItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(details);
        menu.Items.Add("Supported devices", null, (_, _) => ShowWindow(DeviceWindowTab.SupportedDevices));
        menu.Items.Add(_startupItem);
        menu.Items.Add("Copy device logs", null, (_, _) => CopyDeviceLogs());
        menu.Items.Add("About", null, (_, _) => ShowWindow(DeviceWindowTab.About));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowWindow(DeviceWindowTab.Device);
        _tray.Visible = true;

        _monitor.Changed += (_, _) => _ui.Post(_ => { Render(); _window?.RefreshData(); }, null);
        _monitor.ButtonPressed += (_, button) => _ui.Post(_ => OnButtonPressed(button), null);
        _refreshTimer.Tick += (_, _) => { Render(); _window?.RefreshData(); };
        _refreshTimer.Start();
        Render();
        _monitor.Start();
    }

    private void Render()
    {
        var status = _monitor.LastStatus;
        var charging = status?.IsCharging == true;
        var lastKnown = _monitor.LastKnownBatteryPercent;
        var percent = status is { IsHeadsetOn: true } ? status.BatteryPercent : null;
        var settling = percent is not null && !charging && _monitor.IsSettling;
        var low = percent is not null && percent <= BatteryIcon.LowPercent && !charging && !settling;

        var text = _monitor.State switch
        {
            MonitorState.NoAdapter => "No headset adapter found",
            _ when status is null => "Waiting for headset… (turn it off and on)",
            _ when !status.IsHeadsetOn => "Headset is off" + (lastKnown is null ? "" : $" · last seen {lastKnown}%"),
            _ when charging => "Charging" + (lastKnown is null ? "" : $" · was {lastKnown}%"),
            _ when settling => $"Battery ~{percent}% · settling after charging",
            _ => $"Battery {percent}%",
        };
        var micMuted = status is { IsHeadsetOn: true, IsMicMuted: true } && _monitor.State != MonitorState.NoAdapter;
        if (micMuted) text += " · mic muted";
        _hintItem.Visible = settling;
        if (status is not null && _monitor.State != MonitorState.NoAdapter)
            text += _monitor.State == MonitorState.WaitingForHeadset
                ? $" · as of {FormatTime(status.ReceivedAt)}"
                : $" · updated {status.ReceivedAt:HH:mm}";
        _statusItem.Text = text;
        _modelItem.Text = _monitor.ActiveProtocol?.DisplayName ?? "";
        _modelItem.Visible = _monitor.ActiveProtocol is not null;
        var name = _monitor.ActiveProtocol switch
        {
            null => "Headset Stats",
            { IsSupported: false } => "Not supported yet (experimental)",
            var p => p.DisplayName,
        };
        _tray.Text = Truncate(name + "\n" + text, 127);

        var kind = _monitor.State switch
        {
            MonitorState.NoAdapter => TrayIconKind.NoAdapter,
            _ when status is null => TrayIconKind.Waiting,
            _ when !status.IsHeadsetOn => TrayIconKind.HeadsetOff,
            _ when charging => TrayIconKind.Charging,
            _ when percent is null => TrayIconKind.Waiting,
            _ when settling => TrayIconKind.Settling,
            _ => TrayIconKind.Level,
        };
        var old = _tray.Icon;
        _tray.Icon = BatteryIcon.Create(kind, percent, micMuted: micMuted);
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

    private void OnButtonPressed(HeadsetButton button)
    {
        _window?.RefreshData();
        var action = _settings.Get(button);
        if (!action.IsConfigured) return;

        // One press = one report, but guard against bursts (e.g. a held button repeating).
        var now = DateTime.UtcNow;
        if (_lastActionRun.TryGetValue(button, out var last) && now - last < TimeSpan.FromMilliseconds(500)) return;
        _lastActionRun[button] = now;

        var error = ButtonActionRunner.Run(action);
        _window?.ShowButtonResult(button, error);
        if (error is not null)
            _tray.ShowBalloonTip(5000, $"{button} button action failed", error, ToolTipIcon.Error);
    }

    private void CopyDeviceLogs()
    {
        Clipboard.SetText(DiagnosticReport.Create(_monitor, Application.ProductVersion.Split('+')[0]));
        _tray.ShowBalloonTip(4000, "Device logs copied",
            "Paste them into a GitHub issue to help support your headset. Nothing is sent automatically.", ToolTipIcon.Info);
    }

    private void ShowWindow(DeviceWindowTab tab)
    {
        if (_window is null || _window.IsDisposed)
        {
            _window = new DeviceWindow(_monitor, _store, _settings);
            _window.FormClosed += (_, _) => _window = null;
        }
        _window.ShowTab(tab);
        _window.RefreshData();
        _window.Show();
        if (_window.WindowState == FormWindowState.Minimized) _window.WindowState = FormWindowState.Normal;
        _window.Activate();
    }

    private static string FormatTime(DateTimeOffset time) =>
        time.Date == DateTime.Today ? time.ToString("HH:mm") : time.ToString("d MMM HH:mm");

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    protected override void ExitThreadCore()
    {
        _refreshTimer.Dispose();
        _tray.Visible = false;
        _window?.Close();
        _monitor.Dispose();
        _tray.Icon?.Dispose();
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
