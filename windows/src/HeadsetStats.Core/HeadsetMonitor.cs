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

    private readonly IReadOnlyList<IHeadsetProtocol> _protocols;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public MonitorState State { get; private set; } = MonitorState.NoAdapter;
    public IHeadsetProtocol? ActiveProtocol { get; private set; }
    public HeadsetStatus? LastStatus { get; private set; }

    /// <summary>Most recent battery level reported, kept while charging (when the headset doesn't report one).</summary>
    public int? LastKnownBatteryPercent { get; private set; }

    public event EventHandler? Changed;

    public HeadsetMonitor(IReadOnlyList<IHeadsetProtocol>? protocols = null, StatusStore? store = null)
    {
        _protocols = protocols ?? SupportedHeadsets.All;
        _store = store;
    }

    private readonly StatusStore? _store;

    public void Start() => _loop ??= Task.Run(() => RunAsync(_cts.Token));

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var match = FindAdapter();
            if (match is null)
            {
                SetState(MonitorState.NoAdapter, null, null);
                await Delay(ct).ConfigureAwait(false);
                continue;
            }

            var (protocol, collection) = match.Value;
            try
            {
                using var connection = HidConnection.Open(collection);
                // Show the last saved status until the adapter sends a fresh one.
                SetState(MonitorState.WaitingForHeadset, protocol, LastStatus ?? _store?.Load(protocol));
                while (!ct.IsCancellationRequested)
                {
                    var report = await connection.ReadInputReportAsync(ct).ConfigureAwait(false);
                    var status = protocol.TryParse(report);
                    if (status is null) continue;
                    SetState(MonitorState.Reporting, protocol, status);
                    _store?.Save(protocol, status);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
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
        return null;
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
