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

    /// <summary>
    /// Store listing art for Partner Center (Store listing → Store logos / promotional images).
    /// Run: HeadsetStats.exe --render-store-listing-art &lt;folder&gt;
    /// </summary>
    public static void SaveListingArt(string folder)
    {
        Directory.CreateDirectory(folder);
        SaveSquare(folder, "app-tile-icon-300x300.png", 300, padding: 0.1f);
        SaveArt(folder, "box-art-2160x2160.png", 2160, 2160);
        SaveArt(folder, "poster-art-1440x2160.png", 1440, 2160);
        SaveArt(folder, "hero-art-3840x2160.png", 3840, 2160);
    }

    /// <summary>Brand artwork: gradient, large headphones, name, tagline and a row of tray-icon states.</summary>
    private static void SaveArt(string folder, string name, int width, int height)
    {
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using (var gradient = new LinearGradientBrush(new Rectangle(0, 0, width, height),
                       Color.FromArgb(0x1F, 0x4D, 0x36), Color.FromArgb(0x0B, 0x10, 0x0E), 60f))
                g.FillRectangle(gradient, 0, 0, width, height);

            // Soft glow behind the icon.
            var unit = Math.Min(width, height);
            var landscape = width > height * 1.2f;
            var square = width == height;
            var iconSize = (int)(unit * (landscape ? 0.42f : square ? 0.30f : 0.40f));
            var iconX = landscape ? (int)(width * 0.12f) : (width - iconSize) / 2;
            var iconY = landscape ? (height - iconSize) / 2 - (int)(unit * 0.06f) : (int)(height * (square ? 0.09f : 0.16f));
            using (var glowPath = new GraphicsPath())
            {
                var glow = new RectangleF(iconX - iconSize * 0.35f, iconY - iconSize * 0.35f, iconSize * 1.7f, iconSize * 1.7f);
                glowPath.AddEllipse(glow);
                using var glowBrush = new PathGradientBrush(glowPath)
                {
                    CenterColor = Color.FromArgb(90, 0x52, 0xB8, 0x83),
                    SurroundColors = [Color.FromArgb(0, 0x52, 0xB8, 0x83)],
                };
                g.FillEllipse(glowBrush, glow);
            }
            using (var icon = BatteryIcon.Render(TrayIconKind.App, null, iconSize))
                g.DrawImage(icon, iconX, iconY, iconSize, iconSize);

            // Text block: below the icon (square/portrait) or to its right (landscape).
            var textX = landscape ? iconX + iconSize + unit * 0.10f : width * 0.08f;
            var textWidth = landscape ? width - textX - width * 0.06f : width * 0.84f;
            var titleY = landscape ? height * 0.30f : iconY + iconSize + unit * 0.06f;
            var align = landscape ? StringAlignment.Near : StringAlignment.Center;

            using var title = new Font("Segoe UI Semibold", unit * (square ? 0.095f : 0.105f), FontStyle.Regular, GraphicsUnit.Pixel);
            using var tagline = new Font("Segoe UI", unit * 0.042f, FontStyle.Regular, GraphicsUnit.Pixel);
            using var white = new SolidBrush(Color.White);
            using var soft = new SolidBrush(Color.FromArgb(0xB8, 0xD8, 0xC6));
            using var format = new StringFormat { Alignment = align };
            g.DrawString("Headset Stats", title, white, new RectangleF(textX, titleY, textWidth, title.Height * 1.3f), format);
            var taglineY = titleY + title.Height * 1.25f;
            // Deliberate line break, so no layout ends with a lone word.
            g.DrawString("Your headset's battery,\nright in the Windows tray", tagline, soft,
                new RectangleF(textX, taglineY, textWidth, tagline.Height * 2.6f), format);

            // Row of tray states, like the real taskbar.
            (TrayIconKind Kind, int? Percent, bool Muted)[] states =
            [
                (TrayIconKind.Level, 80, false), (TrayIconKind.Charging, null, false),
                (TrayIconKind.Level, 50, true), (TrayIconKind.Level, 10, false),
            ];
            var chip = (int)(unit * 0.11f);
            var gap = (int)(chip * 0.35f);
            var rowWidth = states.Length * chip + (states.Length - 1) * gap;
            var rowX = landscape ? (int)textX : (width - rowWidth) / 2;
            var rowY = (int)(taglineY + tagline.Height * (landscape ? 3.2f : 3.0f));
            using var plate = new SolidBrush(Color.FromArgb(0x1A, 0x1F, 0x1D));
            using var platePath = RoundedRect(new RectangleF(rowX - gap, rowY - gap, rowWidth + 2 * gap, chip + 2 * gap), gap);
            g.FillPath(plate, platePath);
            for (var i = 0; i < states.Length; i++)
            {
                // Render at 32 px and scale up with smoothing, matching how the tray icon looks at high DPI.
                using var icon = BatteryIcon.Render(states[i].Kind, states[i].Percent, chip, states[i].Muted);
                g.DrawImage(icon, rowX + i * (chip + gap), rowY, chip, chip);
            }
        }
        Save(bitmap, Path.Combine(folder, name));
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
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

