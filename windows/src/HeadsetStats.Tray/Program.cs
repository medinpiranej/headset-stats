namespace HeadsetStats.Tray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstance = new Mutex(initiallyOwned: true, @"Local\HeadsetStats.Tray", out var isFirst);
        if (!isFirst) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}
