using System.Text.Json;
using System.Text.Json.Serialization;
using HeadsetStats.Core.Devices;

namespace HeadsetStats.Core;

public enum ButtonActionKind
{
    /// <summary>Nothing extra; the headset still does its own thing (e.g. change game/chat balance).</summary>
    None,
    /// <summary>Open a shortcut, app, file or URL, as if double-clicked.</summary>
    Open,
    /// <summary>Run a command line through cmd.exe, without a console window.</summary>
    Command,
}

/// <summary>What to do when a headset button is pressed.</summary>
public sealed record ButtonAction(ButtonActionKind Kind, string Target)
{
    public static ButtonAction Nothing { get; } = new(ButtonActionKind.None, "");

    public bool IsConfigured => Kind != ButtonActionKind.None && !string.IsNullOrWhiteSpace(Target);
}

/// <summary>User settings (button actions, experimental device support), persisted as JSON in the app's local data folder.</summary>
public sealed class AppSettings
{
    /// <summary>Buttons the user can assign actions to. Volume and mute keep their headset function only.</summary>
    public static IReadOnlyList<HeadsetButton> Assignable { get; } = [HeadsetButton.Chat, HeadsetButton.Game];

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;
    private Dictionary<HeadsetButton, ButtonAction> _actions = [];
    private bool _tryUnsupportedDevices = true;

    public AppSettings(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HeadsetStats", "settings.json");
        Load();
    }

    public string FilePath => _path;

    /// <summary>Listen to Sony devices that aren't supported yet and try to decode them (read-only). On by default.</summary>
    public bool TryUnsupportedDevices
    {
        get => _tryUnsupportedDevices;
        set { _tryUnsupportedDevices = value; Save(); }
    }

    public ButtonAction Get(HeadsetButton button) => _actions.GetValueOrDefault(button, ButtonAction.Nothing);

    public void Set(HeadsetButton button, ButtonAction action)
    {
        _actions[button] = action;
        Save();
    }

    private void Load()
    {
        try
        {
            var file = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(_path), Json);
            _actions = file?.ButtonActions ?? [];
            _tryUnsupportedDevices = file?.TryUnsupportedDevices ?? true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _actions = [];
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(new SettingsFile(_actions, _tryUnsupportedDevices), Json));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Keep the in-memory value; the user can retry by changing the setting again.
        }
    }

    private sealed record SettingsFile(Dictionary<HeadsetButton, ButtonAction>? ButtonActions, bool? TryUnsupportedDevices);
}
