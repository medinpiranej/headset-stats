using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpDoNotRound = 1;

    private const int DwmwaExtendedFrameBounds = 9;

    // Captions for store screenshots, per tab.
    private static readonly Dictionary<DeviceWindowTab, string> StoreCaptions = new()
    {
        [DeviceWindowTab.Device] = "Battery, charging, mic and volume at a glance",
        [DeviceWindowTab.History] = "Every message from the headset, decoded",
        [DeviceWindowTab.Buttons] = "Make the Chat and Game buttons launch anything",
        [DeviceWindowTab.SupportedDevices] = "Know exactly what to expect from your headset",
        [DeviceWindowTab.About] = "Open source, offline, no telemetry",
    };

    /// <param name="storeSize">If set, each capture is centred on a canvas of this size with a caption (store listings).</param>
    public static void Save(string folder, string suffix, Size? storeSize = null)
    {
        Directory.CreateDirectory(folder);

        var store = new StatusStore();
        var settings = new AppSettings();
        using var monitor = new HeadsetMonitor(store: store, tryUnsupported: () => settings.TryUnsupportedDevices);
        monitor.Start();
        using var window = new DeviceWindow(monitor, store, settings);

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
                // Trim the 1 px Windows 11 border: it is semi-transparent and would include background pixels.
                var bounds = Rectangle.FromLTRB(r.Left + 1, r.Top + 1, r.Right - 1, r.Bottom - 1);
                using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                using var output = storeSize is { } canvas ? Compose(bitmap, canvas, StoreCaptions[tab], dark: suffix.Contains("dark")) : null;
                // Save via a stream: GDI+ file paths are limited to MAX_PATH.
                var prefix = storeSize is null ? "window" : "store";
                using var file = File.Create(Path.Combine(folder, $"{prefix}-{tab.ToString().ToLowerInvariant()}{suffix}.png"));
                (output ?? bitmap).Save(file, ImageFormat.Png);
            }
            window.Close();
        };
        window.TopMost = true;
        window.Shown += (_, _) =>
        {
            // Square corners, so no background shows through the rounded corners.
            var square = DwmwcpDoNotRound;
            DwmSetWindowAttribute(window.Handle, DwmwaWindowCornerPreference, ref square, sizeof(int));
            timer.Start();
        };
        Application.Run(window);
    }

    /// <summary>Window capture centred on a gradient canvas, with a caption and the app name above it.</summary>
    private static Bitmap Compose(Bitmap window, Size canvas, string caption, bool dark)
    {
        var result = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(result);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        var top = dark ? Color.FromArgb(0x1E, 0x2A, 0x24) : Color.FromArgb(0xE8, 0xF3, 0xEC);
        var bottom = dark ? Color.FromArgb(0x10, 0x14, 0x12) : Color.FromArgb(0xFA, 0xFB, 0xFA);
        using (var gradient = new LinearGradientBrush(new Rectangle(Point.Empty, canvas), top, bottom, 90f))
            g.FillRectangle(gradient, 0, 0, canvas.Width, canvas.Height);

        var text = dark ? Color.White : Color.FromArgb(0x16, 0x1A, 0x18);
        var captionHeight = canvas.Height * 0.16f;
        using (var captionFont = new Font("Segoe UI Semibold", canvas.Height * 0.042f, FontStyle.Regular, GraphicsUnit.Pixel))
        using (var brush = new SolidBrush(text))
        using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            g.DrawString(caption, captionFont, brush, new RectangleF(0, 0, canvas.Width, captionHeight), format);

        // Scale the window to fit below the caption, never enlarging it.
        var area = new RectangleF(canvas.Width * 0.05f, captionHeight, canvas.Width * 0.9f, canvas.Height - captionHeight - canvas.Height * 0.05f);
        var scale = Math.Min(1f, Math.Min(area.Width / window.Width, area.Height / window.Height));
        var w = window.Width * scale;
        var h = window.Height * scale;
        var x = area.X + (area.Width - w) / 2;
        var y = area.Y + (area.Height - h) / 2;
        using (var shadow = new SolidBrush(Color.FromArgb(dark ? 110 : 45, 0, 0, 0)))
            g.FillRectangle(shadow, x + 10, y + 14, w, h);
        g.DrawImage(window, x, y, w, h);
        return result;
    }
}
