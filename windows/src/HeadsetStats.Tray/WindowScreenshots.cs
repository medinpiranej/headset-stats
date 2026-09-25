using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using HeadsetStats.Core;

namespace HeadsetStats.Tray;

/// <summary>
/// Opens the device window with live data and saves a PNG of each tab (for docs and store listings).
/// Run: HeadsetStats.exe --screenshots &lt;folder&gt; [light|dark]
/// Captures only the window's visible frame (kept topmost), never the screen around it.
/// </summary>
internal static class WindowScreenshots
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out Rect value, int size);

    private const int DwmwaExtendedFrameBounds = 9;

    public static void Save(string folder, string suffix)
    {
        Directory.CreateDirectory(folder);

        var store = new StatusStore();
        using var monitor = new HeadsetMonitor(store: store);
        monitor.Start();
        using var window = new DeviceWindow(monitor, store, new ButtonActionSettings());

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
                Thread.Sleep(tab == DeviceWindowTab.Device ? 1000 : 300); // first tab: let the window finish its initial paint
                Application.DoEvents();

                // Visible frame only (excludes the invisible resize border), with the window topmost,
                // so the capture contains nothing but this window. PrintWindow misrenders native ListViews.
                DwmGetWindowAttribute(window.Handle, DwmwaExtendedFrameBounds, out var r, Marshal.SizeOf<Rect>());
                var bounds = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
                using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                // Save via a stream: GDI+ file paths are limited to MAX_PATH.
                using var file = File.Create(Path.Combine(folder, $"window-{tab.ToString().ToLowerInvariant()}{suffix}.png"));
                bitmap.Save(file, ImageFormat.Png);
            }
            window.Close();
        };
        window.TopMost = true;
        window.Shown += (_, _) => timer.Start();
        Application.Run(window);
    }
}
