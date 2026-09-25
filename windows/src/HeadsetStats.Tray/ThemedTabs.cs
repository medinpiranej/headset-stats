namespace HeadsetStats.Tray;

/// <summary>
/// Minimal tab strip + page host. Replaces TabControl, whose headers and borders stay light
/// in .NET 9's dark mode. Uses only SystemColors, so it follows the app's color mode.
/// </summary>
internal sealed class ThemedTabs : Panel
{
    private readonly FlowLayoutPanel _strip = new() { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
    private readonly Panel _host = new() { Dock = DockStyle.Fill };
    private readonly List<(Button Header, Control Page)> _tabs = [];
    private int _selected = -1;

    public ThemedTabs()
    {
        Controls.Add(_host);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = SystemColors.ControlDark });
        Controls.Add(_strip);
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selected;
        set => Select(value);
    }

    public void Add(string title, Control page)
    {
        var index = _tabs.Count;
        var header = new Button
        {
            Text = title,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Margin = Padding.Empty,
            Padding = new Padding(LogicalToDeviceUnits(10), LogicalToDeviceUnits(4), LogicalToDeviceUnits(10), LogicalToDeviceUnits(6)),
            ForeColor = SystemColors.ControlText,
            TabStop = true,
        };
        header.FlatAppearance.BorderSize = 0;
        header.Click += (_, _) => Select(index);
        header.Paint += (_, e) =>
        {
            if (_selected != index) return;
            // Accent underline marks the selected tab.
            using var accent = new SolidBrush(SystemColors.Highlight);
            var h = LogicalToDeviceUnits(3);
            e.Graphics.FillRectangle(accent, 0, header.Height - h, header.Width, h);
        };

        page.Dock = DockStyle.Fill;
        page.Visible = false;
        _host.Controls.Add(page);
        _strip.Controls.Add(header);
        _tabs.Add((header, page));
        if (_selected < 0) Select(0);
    }

    private void Select(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;
        _selected = index;
        for (var i = 0; i < _tabs.Count; i++)
        {
            var (header, page) = _tabs[i];
            var selected = i == index;
            header.BackColor = selected ? SystemColors.Window : SystemColors.Control;
            header.Font = new Font(Font, selected ? FontStyle.Bold : FontStyle.Regular);
            page.Visible = selected;
            header.Invalidate();
        }
    }
}
