namespace HeadsetStats.Tray;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args is ["--render-icons", var output, ..])
        {
            IconPreview.Save(output, allSizes: args.Length > 2 && args[2] == "all");
            return;
        }

        if (args is ["--report", var reportPath])
        {
            // Same report as "Copy device logs", written to a file (for support and scripts).
            var settings = new HeadsetStats.Core.AppSettings();
            using var monitor = new HeadsetStats.Core.HeadsetMonitor(
                store: new HeadsetStats.Core.StatusStore(), tryUnsupported: () => settings.TryUnsupportedDevices);
            monitor.Start();
            Thread.Sleep(3000);
            File.WriteAllText(reportPath, HeadsetStats.Core.DiagnosticReport.Create(monitor, Application.ProductVersion.Split('+')[0]));
            return;
        }

        ApplicationConfiguration.Initialize();

        if (args is ["--render-store-assets", var assetsFolder])
        {
            StoreAssets.Save(assetsFolder);
            return;
        }

        if (args is ["--render-store-listing-art", var artFolder])
        {
            StoreAssets.SaveListingArt(artFolder);
            return;
        }

        if (args is [("--screenshots" or "--store-screenshots") and var kind, var folder, ..])
        {
            var mode = args.Length > 2 ? args[2] : "system";
            SetColorMode(mode switch { "dark" => SystemColorMode.Dark, "light" => SystemColorMode.Classic, _ => SystemColorMode.System });
            WindowScreenshots.Save(folder, mode is "dark" or "light" ? "-" + mode : "",
                kind == "--store-screenshots" ? new Size(1920, 1080) : null);
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
