namespace HeadsetStats.Tray;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args is ["--render-icons", var output])
        {
            IconPreview.Save(output);
            return;
        }
        if (args is ["--screenshots", var folder])
        {
            WindowScreenshots.Save(folder);
            return;
        }

        using var singleInstance = new Mutex(initiallyOwned: true, @"Local\HeadsetStats.Tray", out var isFirst);
        if (!isFirst) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}
