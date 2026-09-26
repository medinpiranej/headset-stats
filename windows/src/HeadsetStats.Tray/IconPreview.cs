using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HeadsetStats.Tray;

/// <summary>
/// Renders every icon state on dark and light taskbar backgrounds, enlarged with nearest-neighbour
/// scaling so individual pixels are visible. By default only the largest tray size (32 px) is shown;
/// pass "all" for 16, 24 and 32 px.
/// Run: HeadsetStats.exe --render-icons preview.png [all]
/// </summary>
internal static class IconPreview
{
    private static readonly (string Label, TrayIconKind Kind, int? Percent, bool Muted)[] States =
    [
        ("80%", TrayIconKind.Level, 80, false),
        ("100%", TrayIconKind.Level, 100, false),
        ("30%", TrayIconKind.Level, 30, false),
        ("10%", TrayIconKind.Level, 10, false),
        ("Charging", TrayIconKind.Charging, null, false),
        ("Settling 70%", TrayIconKind.Settling, 70, false),
        ("50%, mic muted", TrayIconKind.Level, 50, true),
        ("Headset off", TrayIconKind.HeadsetOff, null, false),
        ("Waiting", TrayIconKind.Waiting, null, false),
        ("No adapter", TrayIconKind.NoAdapter, null, false),
    ];

    private static readonly int[] AllSizes = [16, 24, 32];
    private const int Zoom = 4;
    private const int Pad = 12;
    private const int LabelWidth = 130;

    public static void Save(string path, bool allSizes = false)
    {
        if (!allSizes)
        {
            SaveStrip(path, AllSizes[^1]);
            return;
        }
        int[] sizes = AllSizes;
        var cell = sizes.Max() * Zoom + Pad;
        var backgrounds = new[] { Color.FromArgb(0x1C, 0x1C, 0x1C), Color.FromArgb(0xEE, 0xEE, 0xEE) };
        var width = LabelWidth + backgrounds.Length * sizes.Length * cell + Pad;
        var height = 30 + States.Length * cell + Pad;

        using var sheet = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(sheet);
        g.Clear(Color.White);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        using var font = new Font("Segoe UI", 10f);

        for (var b = 0; b < backgrounds.Length; b++)
        {
            var x0 = LabelWidth + b * sizes.Length * cell;
            using var bg = new SolidBrush(backgrounds[b]);
            g.FillRectangle(bg, x0, 0, sizes.Length * cell, height);
            using var headerBrush = new SolidBrush(b == 0 ? Color.White : Color.Black);
            for (var i = 0; i < sizes.Length; i++)
                g.DrawString($"{sizes[i]} px", font, headerBrush, x0 + i * cell + Pad, 6);
        }

        for (var r = 0; r < States.Length; r++)
        {
            var y = 30 + r * cell;
            g.DrawString(States[r].Label, font, Brushes.Black, 6, y + cell / 2 - 10);
            for (var b = 0; b < backgrounds.Length; b++)
            for (var i = 0; i < sizes.Length; i++)
            {
                using var icon = BatteryIcon.Render(States[r].Kind, States[r].Percent, sizes[i], States[r].Muted);
                var x = LabelWidth + (b * sizes.Length + i) * cell + Pad;
                g.DrawImage(icon, x, y, sizes[i] * Zoom, sizes[i] * Zoom);
            }
        }

        using var file = File.Create(path); // stream, not a file name: GDI+ paths are limited to MAX_PATH
        sheet.Save(file, ImageFormat.Png);
    }

    /// <summary>One size, laid out horizontally: a dark and a light taskbar row, one column per state.</summary>
    private static void SaveStrip(string path, int size)
    {
        var cell = size * Zoom + Pad * 2;
        const int labelHeight = 40;
        var width = States.Length * cell;
        var height = 2 * cell + labelHeight;

        using var sheet = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(sheet);
        g.Clear(Color.White);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe UI", 9.5f);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        Color[] backgrounds = [Color.FromArgb(0x1C, 0x1C, 0x1C), Color.FromArgb(0xEE, 0xEE, 0xEE)];

        for (var row = 0; row < backgrounds.Length; row++)
        {
            using var bg = new SolidBrush(backgrounds[row]);
            g.FillRectangle(bg, 0, row * cell, width, cell);
        }
        for (var c = 0; c < States.Length; c++)
        {
            var state = States[c];
            for (var row = 0; row < backgrounds.Length; row++)
            {
                using var icon = BatteryIcon.Render(state.Kind, state.Percent, size, state.Muted);
                g.DrawImage(icon, c * cell + Pad, row * cell + Pad, size * Zoom, size * Zoom);
            }
            g.DrawString(state.Label, font, Brushes.Black, new RectangleF(c * cell, 2 * cell, cell, labelHeight), format);
        }

        using var file = File.Create(path); // stream, not a file name: GDI+ paths are limited to MAX_PATH
        sheet.Save(file, ImageFormat.Png);
    }
}
