using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace HeadsetStats.Tray;

/// <summary>
/// "Start with Windows".
/// Unpackaged (zip) build: the per-user Run key.
/// Microsoft Store (MSIX) build: the package declares a startup task (see windows/packaging/AppxManifest.xml),
/// which the user turns on or off in Settings → Apps → Startup. Registry writes from a package are private
/// to it, so the Run key would have no effect there.
/// </summary>
internal static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "HeadsetStats";
    private const int AppModelErrorNoPackage = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int length, char[]? name);

    /// <summary>True when running from an MSIX package (e.g. installed from the Microsoft Store).</summary>
    public static bool IsPackaged { get; } = DetectPackaged();

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value) key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            else key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>Opens Windows' startup apps page, where packaged apps' startup tasks are switched on or off.</summary>
    public static void OpenStartupSettings() =>
        Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true });

    private static bool DetectPackaged()
    {
        var length = 0;
        return GetCurrentPackageFullName(ref length, null) != AppModelErrorNoPackage;
    }
}
