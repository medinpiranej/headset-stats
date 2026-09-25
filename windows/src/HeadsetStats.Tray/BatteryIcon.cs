using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace HeadsetStats.Tray;

internal enum BatteryIconKind { Unknown, Charging, Level }

/// <summary>Draws the tray icon: the battery percentage as text colored by level, a bolt while charging, or a dash.</summary>
internal static class BatteryIcon
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create(BatteryIconKind kind, int? percent, bool low)
    {
        if (kind == BatteryIconKind.Charging) return Draw("⚡", Color.Gold, "Segoe UI Symbol", 0.8f);
        if (kind == BatteryIconKind.Unknown || percent is null) return Draw("–", Color.Gray, "Segoe UI", 0.72f);

        var text = percent == 100 ? "F" : percent.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Draw(text, low ? Color.OrangeRed : Color.White, "Segoe UI", text.Length > 1 ? 0.56f : 0.72f);
    }

    private static Icon Draw(string text, Color color, string fontFamily, float scale)
    {
        var size = SystemInformation.SmallIconSize.Width;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            using var font = new Font(fontFamily, size * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(color);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, brush, new RectangleF(0, 0, size, size), format);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
