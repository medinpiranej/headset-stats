using HeadsetStats.Core;
using HeadsetStats.Core.Devices;

namespace HeadsetStats.Tray;

/// <summary>"Buttons" tab: assign actions to the Chat and Game buttons, and show live button state.</summary>
internal sealed class ButtonsPage : Panel
{
    private readonly ButtonActionSettings _settings;
    private readonly Label _liveState = new() { AutoSize = true, Margin = new Padding(0, 4, 0, 8) };
    private readonly Dictionary<HeadsetButton, Label> _results = [];

    public ButtonsPage(ButtonActionSettings settings, Func<int, int> scale)
    {
        _settings = settings;
        Padding = new Padding(scale(12));
        AutoScroll = true;

        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(scale(700), 0),
            Margin = new Padding(0, 0, 0, scale(8)),
            ForeColor = SystemColors.GrayText,
            Text = "Choose what happens on the PC when you press Chat or Game on the headset. The button still changes " +
                   "the headset's game/chat balance as usual; the action runs in addition. Actions only run while " +
                   "Headset Stats is running.",
        });
        layout.Controls.Add(_liveState);

        foreach (var button in ButtonActionSettings.Assignable)
            layout.Controls.Add(ButtonGroup(button, scale));

        Controls.Add(layout);
    }

    /// <summary>Shows the headset's live volume, mute and balance state, and the last button press.</summary>
    public void ShowState(HeadsetStatus? status, (HeadsetButton Button, DateTimeOffset At)? lastPress)
    {
        var volume = status?.VolumePercent is { } v ? $"{v} %" : "unknown";
        var mic = status?.IsMicMuted switch { true => "muted", false => "live", null => "unknown" };
        var balance = status?.GameChatBalance switch
        {
            null => "unknown",
            0 => "centred",
            < 0 and var b => $"{-b} toward chat",
            var b => $"{b} toward game",
        };
        var last = lastPress is { } p ? $"{ButtonName(p.Button)} at {p.At:HH:mm:ss}" : "none yet";
        _liveState.Text = $"Headset volume: {volume}    ·    Mic: {mic}    ·    Balance: {balance}\nLast button press: {last}";
    }

    private GroupBox ButtonGroup(HeadsetButton button, Func<int, int> scale)
    {
        var current = _settings.Get(button);
        var group = new GroupBox
        {
            Text = $"{ButtonName(button)} button",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(scale(10)),
            Margin = new Padding(0, 0, 0, scale(10)),
            MinimumSize = new Size(scale(640), 0),
        };

        var nothing = new RadioButton { Text = "Do nothing extra", AutoSize = true };
        var open = new RadioButton { Text = "Open a shortcut, app, file or website:", AutoSize = true };
        var command = new RadioButton { Text = "Run a command:", AutoSize = true };
        var openPath = new TextBox { Width = scale(420), PlaceholderText = @"e.g. C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Discord.lnk" };
        var commandText = new TextBox { Width = scale(420), PlaceholderText = "e.g. start spotify:   or   powershell -File C:\\scripts\\mute-obs.ps1" };
        var result = new Label { AutoSize = true, ForeColor = SystemColors.GrayText };
        _results[button] = result;

        switch (current.Kind)
        {
            case ButtonActionKind.Open: open.Checked = true; openPath.Text = current.Target; break;
            case ButtonActionKind.Command: command.Checked = true; commandText.Text = current.Target; break;
            default: nothing.Checked = true; break;
        }

        void Save()
        {
            var action = open.Checked ? new ButtonAction(ButtonActionKind.Open, openPath.Text.Trim())
                : command.Checked ? new ButtonAction(ButtonActionKind.Command, commandText.Text.Trim())
                : ButtonAction.Nothing;
            _settings.Set(button, action);
        }

        void Pick(string? folder)
        {
            using var dialog = new OpenFileDialog
            {
                Title = $"Choose what the {ButtonName(button)} button opens",
                Filter = "Shortcuts and programs (*.lnk;*.exe;*.url;*.bat;*.cmd;*.ps1)|*.lnk;*.exe;*.url;*.bat;*.cmd;*.ps1|All files (*.*)|*.*",
                DereferenceLinks = false, // keep the .lnk itself, so the shortcut's arguments and working folder apply
                InitialDirectory = folder ?? "",
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
            openPath.Text = dialog.FileName;
            open.Checked = true;
            Save();
        }

        nothing.CheckedChanged += (_, _) => { if (nothing.Checked) Save(); };
        open.CheckedChanged += (_, _) => { if (open.Checked) Save(); };
        command.CheckedChanged += (_, _) => { if (command.Checked) Save(); };
        // Boxes stay enabled (disabled boxes render light in dark mode); typing in one selects its option.
        openPath.TextChanged += (_, _) => { if (openPath.Focused) open.Checked = true; if (open.Checked) Save(); };
        commandText.TextChanged += (_, _) => { if (commandText.Focused) command.Checked = true; if (command.Checked) Save(); };

        var startMenu = new Button { Text = "Start menu…", AutoSize = true };
        startMenu.Click += (_, _) => Pick(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms));
        var desktop = new Button { Text = "Desktop…", AutoSize = true };
        desktop.Click += (_, _) => Pick(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        var browse = new Button { Text = "Browse…", AutoSize = true };
        browse.Click += (_, _) => Pick(null);
        var test = new Button { Text = "Test now", AutoSize = true };
        test.Click += (_, _) => ShowResult(button, ButtonActionRunner.Run(_settings.Get(button)), testRun: true);

        var rows = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
        rows.Controls.Add(nothing);
        rows.Controls.Add(open);
        rows.Controls.Add(Row(scale, openPath, startMenu, desktop, browse));
        rows.Controls.Add(command);
        rows.Controls.Add(Row(scale, commandText));
        rows.Controls.Add(Row(scale, test, result));
        group.Controls.Add(rows);
        return group;
    }

    /// <summary>Shows the outcome of running a button's action (from a real press or the Test button).</summary>
    public void ShowResult(HeadsetButton button, string? error, bool testRun = false)
    {
        if (!_results.TryGetValue(button, out var label)) return;
        var action = _settings.Get(button);
        label.ForeColor = error is null ? SystemColors.GrayText : Color.Firebrick;
        label.Text = error is not null ? $"Failed: {error}"
            : !action.IsConfigured ? "Nothing to run."
            : $"{(testRun ? "Test ran" : "Ran")} at {DateTime.Now:HH:mm:ss}.";
    }

    private static FlowLayoutPanel Row(Func<int, int> scale, params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(scale(20), 0, 0, scale(4)) };
        row.Controls.AddRange(controls);
        return row;
    }

    private static string ButtonName(HeadsetButton button) => button switch
    {
        HeadsetButton.VolumeUp => "Volume up",
        HeadsetButton.VolumeDown => "Volume down",
        HeadsetButton.MicMute => "Mic mute",
        _ => button.ToString(),
    };
}
