using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace HeadsetStats.Tray;

/// <summary>
/// Generates the package logos (MSIX), the listing logo and the app .ico from the vector tray icon,
/// so every image stays in sync with <see cref="BatteryIcon"/>.
/// Run: HeadsetStats.exe --render-store-assets &lt;folder&gt;
/// </summary>
internal static class StoreAssets
{
    public static void Save(string folder)
    {
        Directory.CreateDirectory(folder);
        SaveSquare(folder, "Square44x44Logo.png", 44, padding: 0.06f);
        SaveSquare(folder, "Square150x150Logo.png", 150, padding: 0.22f);
        SaveSquare(folder, "StoreLogo.png", 50, padding: 0.06f);
        SaveWide(folder, "Wide310x150Logo.png", 310, 150);
        SaveIco(Path.Combine(folder, "app.ico"), [16, 24, 32, 48, 64, 256]);
    }

    private static Bitmap Square(int size, float padding)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        var inner = (int)Math.Round(size * (1 - 2 * padding));
        using var icon = BatteryIcon.Render(TrayIconKind.App, null, inner);
        var offset = (size - inner) / 2;
        g.DrawImage(icon, offset, offset, inner, inner);
        return bitmap;
    }

    private static void SaveSquare(string folder, string name, int size, float padding)
    {
        using var bitmap = Square(size, padding);
        Save(bitmap, Path.Combine(folder, name));
    }

    private static void SaveWide(string folder, string name, int width, int height)
    {
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            var iconSize = (int)(height * 0.56f);
            using var icon = BatteryIcon.Render(TrayIconKind.App, null, iconSize);
            g.DrawImage(icon, (int)(width * 0.08f), (height - iconSize) / 2, iconSize, iconSize);
            using var font = new Font("Segoe UI Semibold", height * 0.15f, FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(Color.White);
            var format = new StringFormat { LineAlignment = StringAlignment.Center };
            g.DrawString("Headset Stats", font, brush, new RectangleF(width * 0.08f + iconSize + 12, 0, width, height), format);
        }
        Save(bitmap, Path.Combine(folder, name));
    }

    /// <summary>Multi-size .ico with PNG-compressed entries (supported since Windows Vista).</summary>
    private static void SaveIco(string path, int[] sizes)
    {
        var images = sizes.Select(size =>
        {
            using var bitmap = Square(size, padding: 0.04f);
            using var png = new MemoryStream();
            bitmap.Save(png, ImageFormat.Png);
            return (Size: size, Data: png.ToArray());
        }).ToList();

        using var file = File.Create(path);
        using var w = new BinaryWriter(file);
        w.Write((ushort)0);            // reserved
        w.Write((ushort)1);            // type: icon
        w.Write((ushort)images.Count);
        var offset = 6 + 16 * images.Count;
        foreach (var (size, data) in images)
        {
            w.Write((byte)(size >= 256 ? 0 : size)); // 0 means 256
            w.Write((byte)(size >= 256 ? 0 : size));
            w.Write((byte)0);          // palette colours
            w.Write((byte)0);          // reserved
            w.Write((ushort)1);        // colour planes
            w.Write((ushort)32);       // bits per pixel
            w.Write(data.Length);
            w.Write(offset);
            offset += data.Length;
        }
        foreach (var (_, data) in images) w.Write(data);
    }

    private static void Save(Bitmap bitmap, string path)
    {
        using var file = File.Create(path); // stream, not a file name: GDI+ paths are limited to MAX_PATH
        bitmap.Save(file, ImageFormat.Png);
    }
}

