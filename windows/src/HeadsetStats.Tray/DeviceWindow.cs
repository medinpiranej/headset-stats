using System.Diagnostics;
using System.Globalization;
using HeadsetStats.Core;
using HeadsetStats.Core.Devices;
using HeadsetStats.Core.Hid;

namespace HeadsetStats.Tray;

internal enum DeviceWindowTab { Device, History, SupportedDevices, About }

/// <summary>Everything the app knows about the connected headset, the supported devices, and how it all works.</summary>
internal sealed class DeviceWindow : Form
{
    private const string RepositoryUrl = "https://github.com/medinpiranej/headset-stats";

    private readonly HeadsetMonitor _monitor;
    private readonly StatusStore _store;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Padding = new Point(12, 4) };
    private readonly ListView _deviceList = CreateList(("Property", 170), ("Value", 420));
    private readonly ListView _historyList = CreateList(("Time", 80), ("Battery", 80), ("Charging", 75), ("Headset", 65), ("Raw report", 210));

    public DeviceWindow(HeadsetMonitor monitor, StatusStore store)
    {
        _monitor = monitor;
        _store = store;

        Text = "Headset Stats";
        Icon = BatteryIcon.Create(TrayIconKind.App, null, 32);
        Font = SystemFonts.MessageBoxFont ?? Font;
        StartPosition = FormStartPosition.CenterScreen;
        // Sizes below are at 96 DPI; LogicalToDeviceUnits scales them for the monitor's DPI.
        ClientSize = new Size(LogicalToDeviceUnits(760), LogicalToDeviceUnits(540));
        MinimumSize = new Size(LogicalToDeviceUnits(560), LogicalToDeviceUnits(400));

        _tabs.TabPages.Add(Page("Device", _deviceList,
            "Live information about the connected headset. Values update when the headset reports a change."));
        _tabs.TabPages.Add(Page("History", _historyList,
            "Status reports received since the app started, newest first. The raw bytes are what the adapter sent."));
        _tabs.TabPages.Add(SupportedDevicesPage());
        _tabs.TabPages.Add(AboutPage());
        Controls.Add(_tabs);

        ScaleColumns(_deviceList);
        ScaleColumns(_historyList);
        _deviceList.Resize += (_, _) => FillLastColumn(_deviceList);
        RefreshData();
    }

    private void ScaleColumns(ListView list)
    {
        foreach (ColumnHeader column in list.Columns) column.Width = LogicalToDeviceUnits(column.Width);
    }

    private static void FillLastColumn(ListView list)
    {
        if (list.Columns.Count == 0) return;
        var others = list.Columns.Cast<ColumnHeader>().SkipLast(1).Sum(c => c.Width);
        var width = list.ClientSize.Width - others;
        if (width > 50) list.Columns[^1].Width = width;
    }

    public void ShowTab(DeviceWindowTab tab) => _tabs.SelectedIndex = (int)tab;

    /// <summary>Re-reads the monitor. Call on the UI thread.</summary>
    public void RefreshData()
    {
        if (IsDisposed) return;
        FillDevice();
        FillHistory();
    }

    private void FillDevice()
    {
        var protocol = _monitor.ActiveProtocol;
        var device = _monitor.ActiveDevice;
        var status = _monitor.LastStatus;
        var lastKnown = _monitor.LastKnownBatteryPercent;

        var rows = new List<(string, string)>
        {
            ("Headset", protocol?.Description.HeadsetName ?? "No supported headset adapter connected"),
            ("Headset model", protocol?.Description.HeadsetModel ?? "–"),
            ("Connection", _monitor.State switch
            {
                MonitorState.NoAdapter => "No adapter found (checking every 3 seconds)",
                MonitorState.WaitingForHeadset when status is not null => "Adapter connected · showing the status saved from earlier",
                MonitorState.WaitingForHeadset => "Adapter connected · waiting for the headset to report (switch it off and on)",
                _ => "Adapter connected · live",
            }),
            ("Headset power", status is null ? "Unknown" : status.IsHeadsetOn ? "On" : "Off"),
            ("Battery", status switch
            {
                null => "Unknown",
                { BatteryPercent: { } p } => $"{p} %",
                { IsCharging: true } => lastKnown is null ? "Not reported while charging" : $"Not reported while charging · last known {lastKnown} %",
                _ => "Unknown",
            }),
            ("Charging", status?.IsCharging switch { true => "Yes", false => "No", null => "Unknown" }),
            ("Last update", status is null ? "–" : status.ReceivedAt.ToString("f", CultureInfo.CurrentCulture)),
            ("Last raw report", status is null ? "–" : FormatHex(status.RawReport)),
            ("Adapter model", protocol?.Description.AdapterModel ?? "–"),
            ("Adapter USB id", device is null ? "–" : $"{device.VendorId:X4}:{device.ProductId:X4}"),
            ("Adapter firmware", device is null ? "–" : FormatBcd(device.VersionNumber)),
            ("Adapter reports", protocol?.Description.Reports ?? "–"),
            ("HID collection", device is null ? "–" : $"usage page 0x{device.UsagePage:X4}, {device.InputReportLength}-byte input reports"),
            ("Device path", device?.Path ?? "–"),
            ("Saved status file", _store.FilePath),
        };

        _deviceList.BeginUpdate();
        _deviceList.Items.Clear();
        foreach (var (name, value) in rows) _deviceList.Items.Add(new ListViewItem([name, value]));
        _deviceList.EndUpdate();
    }

    private void FillHistory()
    {
        _historyList.BeginUpdate();
        _historyList.Items.Clear();
        foreach (var s in _monitor.History.Reverse())
        {
            _historyList.Items.Add(new ListViewItem(
            [
                s.ReceivedAt.ToString("HH:mm:ss", CultureInfo.CurrentCulture),
                s.BatteryPercent is { } p ? $"{p} %" : "–",
                s.IsCharging switch { true => "Yes", false => "No", null => "?" },
                s.IsHeadsetOn ? "On" : "Off",
                FormatHex(s.RawReport),
            ]));
        }
        _historyList.EndUpdate();
    }

    private TabPage SupportedDevicesPage()
    {
        var list = CreateList(("Headset", 190), ("Model", 85), ("Adapter", 85), ("USB id", 90), ("Shows", 290));
        ScaleColumns(list);
        list.Dock = DockStyle.Top;
        list.Height = LogicalToDeviceUnits(30 + 24 * SupportedHeadsets.All.Count);

        var text = new RichText();
        foreach (var protocol in SupportedHeadsets.All)
        {
            var d = protocol.Description;
            list.Items.Add(new ListViewItem([d.HeadsetName, d.HeadsetModel, d.AdapterModel, $"{protocol.VendorId:X4}:{protocol.ProductId:X4}", d.Reports]));

            text.Heading($"What to expect: {d.HeadsetName}");
            foreach (var limitation in d.Limitations) text.Bullet(limitation);
        }
        text.Heading("Your headset isn't listed?");
        text.Paragraph("Support is added one headset at a time, by recording what its adapter sends. If you own " +
                       "another wireless headset, you can help: the project's page explains how to capture its " +
                       $"reports with the included probe tool.\n{RepositoryUrl}");

        var page = new TabPage("Supported devices") { Padding = new Padding(8) };
        page.Controls.Add(text.Control);
        page.Controls.Add(new Panel { Dock = DockStyle.Top, Height = LogicalToDeviceUnits(8) });
        text.ScrollToTop();
        page.Controls.Add(list);
        return page;
    }

    private TabPage AboutPage()
    {
        var text = new RichText();
        text.Title("Headset Stats");
        text.Paragraph($"Version {Application.ProductVersion.Split('+')[0]} · Open source (MIT) by Medin Piranej\n{RepositoryUrl}");

        text.Heading("What it does");
        text.Paragraph("Windows doesn't show the battery of headsets that connect through their own USB adapter, " +
                       "because it treats the adapter as a plain sound card. Headset Stats reads the adapter's status " +
                       "messages and shows the battery level, charging state and power state in the system tray.");

        text.Heading("How the information is gathered");
        text.Paragraph("Besides its audio connection, the adapter has a small data channel (a USB HID interface) that " +
                       "the console uses. Whenever something changes, the adapter sends an 8-byte status message on it. " +
                       "Headset Stats listens to that channel and decodes the message. It only listens and never sends " +
                       "anything to the headset or changes its settings.");
        text.Paragraph("The meaning of each byte was worked out by recording these messages while switching the headset " +
                       "on and off and plugging the charging cable in and out, then matching each change to the byte that " +
                       "changed. No Sony software was used or copied. The full notes and recordings are public in the " +
                       "project's docs/PROTOCOL.md.");

        text.Heading("What to expect");
        text.Bullet("The level comes from the headset itself, in 10 % steps. It's an estimate, not a precise measurement.");
        text.Bullet("The headset only reports when something changes, and the PC can't ask for an update. If nothing " +
                    "has changed since the app started, you'll see the last saved value with its time, or \"?\". " +
                    "Switching the headset off and on always refreshes it.");
        text.Bullet("While charging, the headset reports only that it's charging, not how full it is.");
        text.Bullet("Right after charging the level can read high for a minute and then settle.");
        text.Bullet("The PS5 shows a few bars rather than a number, so the two won't always look the same.");

        text.Heading("Contributing");
        text.Paragraph("Headset Stats is open source and open to contributions: support for more headsets, bug " +
                       "reports, code, documentation, and apps for macOS, Linux and Android. You don't need to " +
                       "write code to help get your headset supported. See CONTRIBUTING.md on the project page.\n" +
                       $"{RepositoryUrl}");

        text.Heading("Privacy");
        text.Paragraph("Headset Stats works entirely offline. It collects no data, has no telemetry and makes no network " +
                       "connections. The only thing it stores is the last status message, in your local app data folder.");

        text.Heading("Trademarks");
        text.Paragraph("Not affiliated with or endorsed by Sony Interactive Entertainment. PlayStation, PULSE, PULSE 3D " +
                       "and PULSE Elite are trademarks of Sony Interactive Entertainment Inc., used here only to identify " +
                       "compatible hardware.");

        var page = new TabPage("About") { Padding = new Padding(8) };
        page.Controls.Add(text.Control);
        text.ScrollToTop();
        return page;
    }

    private TabPage Page(string title, Control content, string hint)
    {
        var page = new TabPage(title) { Padding = new Padding(8) };
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        // Room for two wrapped lines at the current font size.
        page.Controls.Add(new Label { Text = hint, Dock = DockStyle.Top, Height = Font.Height * 2 + LogicalToDeviceUnits(8), ForeColor = SystemColors.GrayText });
        return page;
    }

    private static ListView CreateList(params (string Name, int Width)[] columns)
    {
        var list = new ListView { View = View.Details, FullRowSelect = true, GridLines = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        foreach (var (name, width) in columns) list.Columns.Add(name, width);
        return list;
    }

    private static string FormatHex(byte[] bytes) => string.Join(' ', bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));

    // USB bcdDevice: 0x0100 → "1.00"
    private static string FormatBcd(ushort value) => $"{value >> 8:X}.{value & 0xFF:X2}";

    /// <summary>Small helper for a read-only rich text page with headings, paragraphs and bullets.</summary>
    private sealed class RichText
    {
        public RichTextBox Control { get; } = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Window,
            DetectUrls = true,
        };

        public RichText()
        {
            Control.LinkClicked += (_, e) =>
            {
                if (e.LinkText is { } url && url.StartsWith("https://", StringComparison.Ordinal))
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            };
        }

        public void ScrollToTop()
        {
            Control.SelectionStart = 0;
            Control.SelectionLength = 0;
            Control.ScrollToCaret();
        }

        public void Title(string text) => Append(text + "\n", 14f, FontStyle.Bold, indent: 0);
        public void Heading(string text) => Append("\n" + text + "\n", 10.5f, FontStyle.Bold, indent: 0);
        public void Paragraph(string text) => Append(text + "\n", 9.5f, FontStyle.Regular, indent: 0);

        public void Bullet(string text)
        {
            Control.SelectionStart = Control.TextLength;
            Control.SelectionBullet = true;
            Append(text + "\n", 9.5f, FontStyle.Regular, indent: 8);
            Control.SelectionBullet = false;
        }

        private void Append(string text, float size, FontStyle style, int indent)
        {
            Control.SelectionStart = Control.TextLength;
            Control.SelectionLength = 0;
            Control.SelectionFont = new Font("Segoe UI", size, style);
            Control.SelectionIndent = indent;
            Control.AppendText(text);
        }
    }
}
