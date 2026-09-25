using System.Diagnostics;
using System.Globalization;
using HeadsetStats.Core.Hid;

// Read-only diagnostics for reverse-engineering headset adapters. Never writes to the device.
//
//   headset-probe list [VID]            list HID collections (default VID 054C = Sony)
//   headset-probe features VID:PID      dump every feature report the device answers
//   headset-probe listen VID:PID [sec]  print input reports from all collections (default 120 s)
//   headset-probe log VID:PID FILE      append every report (timestamped, decoded when supported) to FILE
//                                       until stopped; survives the adapter being unplugged

var command = args.Length > 0 ? args[0] : "list";
try
{
    return command switch
    {
        "list" => List(args.Length > 1 ? ParseHex(args[1]) : (ushort)0x054C),
        "features" when args.Length > 1 => Features(ParseIds(args[1])),
        "listen" when args.Length > 1 => await Listen(ParseIds(args[1]), args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 120),
        "log" when args.Length > 2 => await Log(ParseIds(args[1]), args[2]),
        _ => Usage(),
    };
}
catch (FormatException)
{
    return Usage();
}

static int Usage()
{
    Console.Error.WriteLine("usage: headset-probe list [VID] | features VID:PID | listen VID:PID [seconds] | log VID:PID FILE");
    return 2;
}

static async Task<int> Log((ushort Vid, ushort Pid) ids, string path)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    await using var writer = new StreamWriter(path, append: true) { AutoFlush = true };
    var protocol = HeadsetStats.Core.Devices.SupportedHeadsets.All.FirstOrDefault(p => p.VendorId == ids.Vid && p.ProductId == ids.Pid);
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    var gate = new object();

    void Write(string line)
    {
        var stamped = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}  {line}";
        lock (gate) { writer.WriteLine(stamped); Console.WriteLine(stamped); }
    }

    Write($"--- log started for {ids.Vid:X4}:{ids.Pid:X4}" + (protocol is null ? "" : $" ({protocol.DisplayName})"));
    var connected = false;
    while (!cts.IsCancellationRequested)
    {
        var collections = HidDeviceInfo.Enumerate(ids.Vid, ids.Pid).Where(d => d.InputReportLength > 0).ToList();
        if (collections.Count == 0)
        {
            if (connected) Write("--- adapter disconnected");
            connected = false;
            try { await Task.Delay(3000, cts.Token); } catch (OperationCanceledException) { }
            continue;
        }

        Write($"--- adapter connected (firmware {collections[0].VersionNumber >> 8:X}.{collections[0].VersionNumber & 0xFF:X2})");
        connected = true;
        using var session = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        await Task.WhenAll(collections.Select(async d =>
        {
            try
            {
                using var connection = HidConnection.Open(d);
                while (true)
                {
                    var report = await connection.ReadInputReportAsync(session.Token);
                    var decoded = protocol is not null && protocol.IsStatusCollection(d) && protocol.TryParse(report) is { } s
                        ? $"  battery={(s.BatteryPercent is { } p ? p + "%" : "-")} charging={s.IsCharging} on={s.IsHeadsetOn} linkUp={s.IsLinkUp}"
                        : "";
                    Write($"page 0x{d.UsagePage:X4}: {FormatHex(report)}{decoded}");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                // One collection failing (unplugged) ends this session; reconnect below.
                session.Cancel();
            }
        }));
        try { await Task.Delay(1000, cts.Token); } catch (OperationCanceledException) { }
    }
    Write("--- log stopped");
    return 0;
}

static int List(ushort vendorId)
{
    var devices = HidDeviceInfo.Enumerate(vendorId);
    if (devices.Count == 0) Console.WriteLine($"No HID collections with vendor id {vendorId:X4}.");
    foreach (var d in devices) Console.WriteLine($"{d}  \"{d.ProductName}\"\n    {d.Path}");
    return 0;
}

static int Features((ushort Vid, ushort Pid) ids)
{
    foreach (var d in HidDeviceInfo.Enumerate(ids.Vid, ids.Pid).Where(d => d.FeatureReportLength > 0))
    {
        Console.WriteLine(d);
        try
        {
            using var connection = HidConnection.Open(d);
            for (var id = 0; id <= 0xFF; id++)
            {
                var report = connection.GetFeatureReport((byte)id);
                if (report is not null) Console.WriteLine($"  id 0x{id:X2}: {Convert.ToHexString(report)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  cannot open: {ex.Message}");
        }
    }
    return 0;
}

static async Task<int> Listen((ushort Vid, ushort Pid) ids, int seconds)
{
    var collections = HidDeviceInfo.Enumerate(ids.Vid, ids.Pid).Where(d => d.InputReportLength > 0).ToList();
    if (collections.Count == 0)
    {
        Console.WriteLine("No matching collections with input reports.");
        return 1;
    }

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    var clock = Stopwatch.StartNew();
    var output = new object();
    Console.WriteLine($"Listening for {seconds} s on {collections.Count} collection(s). Ctrl+C to stop.");

    await Task.WhenAll(collections.Select(async d =>
    {
        var label = $"page 0x{d.UsagePage:X4}";
        try
        {
            using var connection = HidConnection.Open(d);
            while (true)
            {
                var report = await connection.ReadInputReportAsync(cts.Token);
                lock (output) Console.WriteLine($"[{clock.Elapsed:mm\\:ss\\.fff}] {label}: {FormatHex(report)}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            lock (output) Console.WriteLine($"{label}: {ex.Message}");
        }
    }));
    return 0;
}

static string FormatHex(byte[] bytes) => string.Join(' ', bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));

static ushort ParseHex(string value) => ushort.Parse(value.Replace("0x", "", StringComparison.OrdinalIgnoreCase), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

static (ushort, ushort) ParseIds(string value)
{
    var parts = value.Split(':');
    if (parts.Length != 2) throw new FormatException();
    return (ParseHex(parts[0]), ParseHex(parts[1]));
}
