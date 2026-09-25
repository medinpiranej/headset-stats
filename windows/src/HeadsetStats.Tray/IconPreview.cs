using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HeadsetStats.Tray;

/// <summary>
/// Renders every icon state at common tray sizes on dark and light taskbar backgrounds,
/// enlarged with nearest-neighbour scaling so individual pixels are visible.
/// Run: HeadsetStats.exe --render-icons preview.png
/// </summary>
internal static class IconPreview
{
    private static readonly (string Label, TrayIconKind Kind, int? Percent)[] States =
    [
        ("80%", TrayIconKind.Level, 80),
        ("100%", TrayIconKind.Level, 100),
        ("30%", TrayIconKind.Level, 30),
        ("10%", TrayIconKind.Level, 10),
        ("Charging", TrayIconKind.Charging, null),
        ("Headset off", TrayIconKind.HeadsetOff, null),
        ("Waiting", TrayIconKind.Waiting, null),
        ("No adapter", TrayIconKind.NoAdapter, null),
    ];

    private static readonly int[] Sizes = [16, 24, 32];
    private const int Zoom = 4;
    private const int Pad = 12;
    private const int LabelWidth = 110;

    public static void Save(string path)
    {
        var cell = Sizes.Max() * Zoom + Pad;
        var backgrounds = new[] { Color.FromArgb(0x1C, 0x1C, 0x1C), Color.FromArgb(0xEE, 0xEE, 0xEE) };
        var width = LabelWidth + backgrounds.Length * Sizes.Length * cell + Pad;
        var height = 30 + States.Length * cell + Pad;

        using var sheet = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(sheet);
        g.Clear(Color.White);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        using var font = new Font("Segoe UI", 10f);

        for (var b = 0; b < backgrounds.Length; b++)
        {
            var x0 = LabelWidth + b * Sizes.Length * cell;
            using var bg = new SolidBrush(backgrounds[b]);
            g.FillRectangle(bg, x0, 0, Sizes.Length * cell, height);
            using var headerBrush = new SolidBrush(b == 0 ? Color.White : Color.Black);
            for (var i = 0; i < Sizes.Length; i++)
                g.DrawString($"{Sizes[i]} px", font, headerBrush, x0 + i * cell + Pad, 6);
        }

        for (var r = 0; r < States.Length; r++)
        {
            var y = 30 + r * cell;
            g.DrawString(States[r].Label, font, Brushes.Black, 6, y + cell / 2 - 10);
            for (var b = 0; b < backgrounds.Length; b++)
            for (var i = 0; i < Sizes.Length; i++)
            {
                using var icon = BatteryIcon.Render(States[r].Kind, States[r].Percent, Sizes[i]);
                var x = LabelWidth + (b * Sizes.Length + i) * cell + Pad;
                g.DrawImage(icon, x, y, Sizes[i] * Zoom, Sizes[i] * Zoom);
            }
        }

        sheet.Save(path, ImageFormat.Png);
    }
}
