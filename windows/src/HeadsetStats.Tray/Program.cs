namespace HeadsetStats.Tray;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args is ["--render-icons", var output])
        {
            IconPreview.Save(output);
            return;
        }

        ApplicationConfiguration.Initialize();

        if (args is ["--screenshots", var folder, ..])
        {
            var mode = args.Length > 2 ? args[2] : "system";
            SetColorMode(mode switch { "dark" => SystemColorMode.Dark, "light" => SystemColorMode.Classic, _ => SystemColorMode.System });
            WindowScreenshots.Save(folder, mode is "dark" or "light" ? "-" + mode : "");
            return;
        }

        using var singleInstance = new Mutex(initiallyOwned: true, @"Local\HeadsetStats.Tray", out var isFirst);
        if (!isFirst) return;

        // Follow Windows' light/dark app setting.
        SetColorMode(SystemColorMode.System);
        Application.Run(new TrayContext());
    }

    // Dark mode is experimental in .NET 9 WinForms (WFO5001, suppressed in the csproj);
    // it must be set before any window is created.
    private static void SetColorMode(SystemColorMode mode) => Application.SetColorMode(mode);
}
