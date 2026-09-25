using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace HeadsetStats.Tray;

/// <summary>Draws the tray icon: the battery percentage as text, colored by level.</summary>
internal static class BatteryIcon
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create(int? percent, bool lowBattery)
    {
        var size = SystemInformation.SmallIconSize.Width;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            var text = percent switch
            {
                null => "–",
                100 => "F",
                _ => percent.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            var color = percent is null ? Color.Gray : lowBattery ? Color.OrangeRed : Color.White;

            using var font = new Font("Segoe UI", size * (text.Length > 1 ? 0.56f : 0.72f), FontStyle.Bold, GraphicsUnit.Pixel);
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
