using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;

namespace HeadsetStats.Tray;

internal enum TrayIconKind { NoAdapter, Waiting, HeadsetOff, Charging, Level }

/// <summary>
/// Draws the tray icon: a headphones silhouette tinted by state, with the event on top
/// (battery %, charging bolt, "?" or a slash). Vector-drawn so it stays crisp at any DPI.
/// </summary>
internal static class BatteryIcon
{
    public const int LowPercent = 15;
    private const int WarnPercent = 30;

    private static readonly Color Green = Color.FromArgb(0x2E, 0x8B, 0x57);
    private static readonly Color Amber = Color.FromArgb(0xC7, 0x8A, 0x00);
    private static readonly Color Red = Color.FromArgb(0xD3, 0x2F, 0x2F);
    private static readonly Color Grey = Color.FromArgb(0x80, 0x80, 0x80);
    private static readonly Color Bolt = Color.FromArgb(0xFF, 0xD5, 0x4F);
    private static readonly Color GreenText = Color.FromArgb(0x7C, 0xE0, 0x9A);
    private static readonly Color AmberText = Color.FromArgb(0xFF, 0xC8, 0x4A);
    private static readonly Color RedText = Color.FromArgb(0xFF, 0x70, 0x70);
    private static readonly Color Outline = Color.FromArgb(220, 0x10, 0x10, 0x10);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create(TrayIconKind kind, int? percent)
    {
        using var bitmap = Render(kind, percent, SystemInformation.SmallIconSize.Width);
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

    public static Bitmap Render(TrayIconKind kind, int? percent, int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        float s = size;
        var tint = kind switch
        {
            TrayIconKind.Level => percent <= LowPercent ? Red : percent <= WarnPercent ? Amber : Green,
            TrayIconKind.Charging => Green,
            _ => Grey,
        };
        DrawHeadphones(g, s, tint);

        switch (kind)
        {
            case TrayIconKind.Level when percent is not null:
                DrawOutlinedText(g, s, percent.Value.ToString(CultureInfo.InvariantCulture),
                    percent <= LowPercent ? RedText : percent <= WarnPercent ? AmberText : GreenText);
                break;
            case TrayIconKind.Charging:
                DrawBolt(g, s);
                break;
            case TrayIconKind.Waiting:
                DrawOutlinedText(g, s, "?", Color.White);
                break;
            case TrayIconKind.NoAdapter:
                DrawSlash(g, s);
                break;
        }
        return bitmap;
    }

    private static void DrawHeadphones(Graphics g, float s, Color color)
    {
        using var pen = new Pen(color, Math.Max(1.5f, s * 0.12f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(pen, s * 0.15f, s * 0.08f, s * 0.70f, s * 0.80f, 180, 180);

        using var brush = new SolidBrush(color);
        FillRounded(g, brush, new RectangleF(s * 0.03f, s * 0.46f, s * 0.26f, s * 0.48f), s * 0.09f);
        FillRounded(g, brush, new RectangleF(s * 0.71f, s * 0.46f, s * 0.26f, s * 0.48f), s * 0.09f);
    }

    /// <summary>Text scaled to fill the lower-centre of the icon, with a dark outline for contrast.</summary>
    private static void DrawOutlinedText(Graphics g, float s, string text, Color fill)
    {
        using var family = new FontFamily("Segoe UI");
        using var path = new GraphicsPath();
        path.AddString(text, family, (int)FontStyle.Bold, 100f, PointF.Empty, StringFormat.GenericTypographic);

        var bounds = path.GetBounds();
        var box = new RectangleF(s * 0.0f, s * 0.42f, s * 1.0f, s * 0.58f);
        var scale = Math.Min(box.Width / bounds.Width, box.Height / bounds.Height);
        using var matrix = new Matrix();
        matrix.Translate(box.X + (box.Width - bounds.Width * scale) / 2, box.Y + (box.Height - bounds.Height * scale) / 2);
        matrix.Scale(scale, scale);
        matrix.Translate(-bounds.X, -bounds.Y);
        path.Transform(matrix);

        DrawOutlined(g, s, path, fill);
    }

    private static void DrawBolt(Graphics g, float s)
    {
        using var path = new GraphicsPath();
        path.AddPolygon(new[]
        {
            new PointF(0.62f, 0.06f), new PointF(0.26f, 0.56f), new PointF(0.48f, 0.56f),
            new PointF(0.38f, 0.96f), new PointF(0.76f, 0.42f), new PointF(0.54f, 0.42f),
        }.Select(p => new PointF(p.X * s, p.Y * s)).ToArray());
        DrawOutlined(g, s, path, Bolt);
    }

    private static void DrawSlash(Graphics g, float s)
    {
        using var outline = new Pen(Outline, Math.Max(3f, s * 0.24f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var slash = new Pen(Red, Math.Max(1.5f, s * 0.12f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(outline, s * 0.14f, s * 0.86f, s * 0.86f, s * 0.14f);
        g.DrawLine(slash, s * 0.14f, s * 0.86f, s * 0.86f, s * 0.14f);
    }

    private static void DrawOutlined(Graphics g, float s, GraphicsPath path, Color fill)
    {
        using var outline = new Pen(Outline, Math.Max(2f, s * 0.14f)) { LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(fill);
        g.DrawPath(outline, path);
        g.FillPath(brush, path);
    }

    private static void FillRounded(Graphics g, Brush brush, RectangleF r, float radius)
    {
        var d = radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
