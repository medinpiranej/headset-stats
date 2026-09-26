using HeadsetStats.Core.Devices;
using HeadsetStats.Core.Hid;

namespace HeadsetStats.Core;

public enum MonitorState
{
    /// <summary>No supported adapter is plugged in.</summary>
    NoAdapter,
    /// <summary>Adapter found; no live status report yet (LastStatus may be restored from the previous run).</summary>
    WaitingForHeadset,
    /// <summary>At least one status report received from the current adapter.</summary>
    Reporting,
}

/// <summary>
/// Finds a supported adapter, listens for status reports, and reconnects after unplug.
/// Events are raised on a thread-pool thread.
/// </summary>
public sealed class HeadsetMonitor : IDisposable
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    private const int HistoryLimit = 200;

    private readonly IReadOnlyList<IHeadsetProtocol> _protocols;
    private readonly StatusStore? _store;
    private readonly List<ReportEntry> _history = [];
    private readonly Func<bool> _tryUnsupported;
    private DateTimeOffset _lastRawNotify;
    private readonly ChargeSettling _settling = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public MonitorState State { get; private set; } = MonitorState.NoAdapter;
    public IHeadsetProtocol? ActiveProtocol { get; private set; }

    /// <summary>The HID collection currently being read, or null when no adapter is connected.</summary>
    public HidDeviceInfo? ActiveDevice { get; private set; }

    /// <summary>Every report received since the app started (decoded when possible), oldest first.</summary>
    public IReadOnlyList<ReportEntry> History
    {
        get { lock (_history) return _history.ToArray(); }
    }
    public HeadsetStatus? LastStatus { get; private set; }

    /// <summary>Most recent battery level reported, kept while charging (when the headset doesn't report one).</summary>
    public int? LastKnownBatteryPercent { get; private set; }

    /// <summary>True while the battery reading is unreliable because charging just ended (see <see cref="ChargeSettling"/>).</summary>
    public bool IsSettling => _settling.IsSettling(DateTimeOffset.Now);

    /// <summary>The most recent live button press this session, if any.</summary>
    public (HeadsetButton Button, DateTimeOffset At)? LastButtonPress { get; private set; }

    public event EventHandler? Changed;

    /// <summary>A button was pressed on the headset (live reports only, never restored ones). Raised on a thread-pool thread.</summary>
    public event EventHandler<HeadsetButton>? ButtonPressed;

    /// <param name="tryUnsupported">
    /// Checked on every search: when true and no supported adapter is present, listen (read-only) to other Sony devices.
    /// </param>
    public HeadsetMonitor(IReadOnlyList<IHeadsetProtocol>? protocols = null, StatusStore? store = null, Func<bool>? tryUnsupported = null)
    {
        _protocols = protocols ?? SupportedHeadsets.All;
        _store = store;
        _tryUnsupported = tryUnsupported ?? (() => false);
    }

    public void Start() => _loop ??= Task.Run(() => RunAsync(_cts.Token));

    /// <summary>Drops the current device and searches again (e.g. after the experimental setting changed).</summary>
    public void Rescan()
    {
        try { _session?.Cancel(); } catch (ObjectDisposedException) { }
    }

    private CancellationTokenSource? _session;

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var match = FindAdapter();
            if (match is null)
            {
                ActiveDevice = null;
                SetState(MonitorState.NoAdapter, null, null);
                await Delay(ct).ConfigureAwait(false);
                continue;
            }

            var (protocol, collection) = match.Value;
            using var session = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _session = session;
            try
            {
                using var connection = HidConnection.Open(collection);
                ActiveDevice = collection;
                // Show the last saved status until the adapter sends a fresh one.
                SetState(MonitorState.WaitingForHeadset, protocol, LastStatus ?? _store?.Load(protocol));
                while (!ct.IsCancellationRequested)
                {
                    var report = await connection.ReadInputReportAsync(session.Token).ConfigureAwait(false);
                    var status = protocol.TryParse(report);
                    Record(new ReportEntry(status?.ReceivedAt ?? DateTimeOffset.Now, report, status));
                    if (status is null)
                    {
                        NotifyRawReport();
                        continue;
                    }
                    _settling.Observe(LastStatus, status);
                    SetState(MonitorState.Reporting, protocol, status);
                    _store?.Save(protocol, status);
                    if (status.Button is { } button)
                    {
                        LastButtonPress = (button, status.ReceivedAt);
                        ButtonPressed?.Invoke(this, button);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                // Rescan() requested: search again right away.
                continue;
            }
            catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception)
            {
                // Adapter unplugged or not openable; fall through and search again.
            }
            await Delay(ct).ConfigureAwait(false);
        }
    }

    private (IHeadsetProtocol, HidDeviceInfo)? FindAdapter()
    {
        foreach (var protocol in _protocols)
        {
            var collection = HidDeviceInfo.Enumerate(protocol.VendorId, protocol.ProductId)
                .FirstOrDefault(protocol.IsStatusCollection);
            if (collection is not null) return (protocol, collection);
        }

        if (_tryUnsupported())
        {
            foreach (var (protocol, collection) in ExperimentalProtocol.FindCandidates(_protocols))
                return (protocol, collection);
        }
        return null;
    }

    private void Record(ReportEntry entry)
    {
        lock (_history)
        {
            _history.Add(entry);
            if (_history.Count > HistoryLimit) _history.RemoveAt(0);
        }
    }

    // Undecodable reports only change the history; notify at most twice a second in case a device streams.
    private void NotifyRawReport()
    {
        var now = DateTimeOffset.Now;
        if (now - _lastRawNotify < TimeSpan.FromMilliseconds(500)) return;
        _lastRawNotify = now;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void SetState(MonitorState state, IHeadsetProtocol? protocol, HeadsetStatus? status)
    {
        // Keep the last known battery while the adapter stays connected.
        status ??= state == MonitorState.NoAdapter ? null : LastStatus;
        if (state == State && protocol == ActiveProtocol && ReferenceEquals(status, LastStatus)) return;

        State = state;
        ActiveProtocol = protocol;
        LastStatus = status;
        if (status?.BatteryPercent is { } percent) LastKnownBatteryPercent = percent;
        else if (state == MonitorState.NoAdapter) LastKnownBatteryPercent = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static async Task Delay(CancellationToken ct)
    {
        try { await Task.Delay(RetryDelay, ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _loop?.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }
        _cts.Dispose();
    }
}
