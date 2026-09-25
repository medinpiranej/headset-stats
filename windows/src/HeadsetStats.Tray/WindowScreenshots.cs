using HeadsetStats.Core;

namespace HeadsetStats.Tray;

/// <summary>
/// Opens the device window with live data and saves a PNG of each tab (for docs and store listings).
/// Run: HeadsetStats.exe --screenshots &lt;folder&gt;
/// </summary>
internal static class WindowScreenshots
{
    public static void Save(string folder)
    {
        Directory.CreateDirectory(folder);
        ApplicationConfiguration.Initialize();

        var store = new StatusStore();
        using var monitor = new HeadsetMonitor(store: store);
        monitor.Start();
        using var window = new DeviceWindow(monitor, store);

        // Give the monitor a moment to find the adapter and restore the saved status.
        var timer = new System.Windows.Forms.Timer { Interval = 2500 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            foreach (var tab in Enum.GetValues<DeviceWindowTab>())
            {
                window.ShowTab(tab);
                window.RefreshData();
                window.Refresh();
                Application.DoEvents();
                Thread.Sleep(300);
                Application.DoEvents();

                var bounds = window.Bounds;
                using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                // Save via a stream: GDI+ file paths are limited to MAX_PATH.
                using var file = File.Create(Path.Combine(folder, $"window-{tab.ToString().ToLowerInvariant()}.png"));
                bitmap.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            }
            window.Close();
        };
        window.Shown += (_, _) => timer.Start();
        window.TopMost = true;
        Application.Run(window);
    }
}
