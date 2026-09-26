using System.ComponentModel;
using System.Diagnostics;
using HeadsetStats.Core;

namespace HeadsetStats.Tray;

/// <summary>Runs the action a user assigned to a headset button.</summary>
internal static class ButtonActionRunner
{
    /// <summary>Runs the action; returns an error message, or null on success (or when nothing is configured).</summary>
    public static string? Run(ButtonAction action)
    {
        if (!action.IsConfigured) return null;
        try
        {
            var start = action.Kind switch
            {
                // Shell-execute so .lnk shortcuts, documents and URLs open like a double-click.
                ButtonActionKind.Open => new ProcessStartInfo(action.Target.Trim().Trim('"')) { UseShellExecute = true },
                // /s + outer quotes: cmd strips exactly that pair, so the user's own quoting is kept as typed.
                ButtonActionKind.Command => new ProcessStartInfo("cmd.exe", $"/d /s /c \"{action.Target}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                _ => null,
            };
            if (start is null) return null;
            using var process = Process.Start(start);
            return null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            return ex.Message;
        }
    }
}
