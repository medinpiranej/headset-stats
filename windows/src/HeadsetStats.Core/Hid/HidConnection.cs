using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HeadsetStats.Core.Hid;

/// <summary>An open handle to one HID collection that can read input reports and get feature reports.</summary>
public sealed class HidConnection : IDisposable
{
    private readonly SafeFileHandle _handle;
    private readonly FileStream _stream;

    public HidDeviceInfo Device { get; }

    private HidConnection(HidDeviceInfo device, SafeFileHandle handle)
    {
        Device = device;
        _handle = handle;
        _stream = new FileStream(handle, FileAccess.Read, bufferSize: 0, isAsync: true);
    }

    public static HidConnection Open(HidDeviceInfo device)
    {
        var handle = HidNative.CreateFile(device.Path, HidNative.GenericRead | HidNative.GenericWrite,
            HidNative.FileShareReadWrite, IntPtr.Zero, HidNative.OpenExisting, HidNative.FileFlagOverlapped, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            handle = HidNative.CreateFile(device.Path, HidNative.GenericRead,
                HidNative.FileShareReadWrite, IntPtr.Zero, HidNative.OpenExisting, HidNative.FileFlagOverlapped, IntPtr.Zero);
        }
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, $"Could not open HID device {device.Path}");
        }
        return new HidConnection(device, handle);
    }

    /// <summary>
    /// Waits for the next input report. The first byte is the report id (0 when the device uses none).
    /// Throws <see cref="IOException"/> when the device is unplugged.
    /// </summary>
    public async Task<byte[]> ReadInputReportAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[Device.InputReportLength];
        var read = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (read == 0) throw new IOException("HID device returned no data (disconnected?).");
        return read == buffer.Length ? buffer : buffer[..read];
    }

    /// <summary>Requests a feature report by id. Returns null when the device rejects the request.</summary>
    public byte[]? GetFeatureReport(byte reportId)
    {
        var buffer = new byte[Device.FeatureReportLength];
        if (buffer.Length == 0) return null;
        buffer[0] = reportId;
        return HidNative.HidD_GetFeature(_handle, buffer, buffer.Length) ? buffer : null;
    }

    public void Dispose()
    {
        _stream.Dispose();
        _handle.Dispose();
    }
}
