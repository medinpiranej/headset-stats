using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HeadsetStats.Core.Hid;

/// <summary>One HID top-level collection as Windows exposes it (one device path per collection).</summary>
public sealed record HidDeviceInfo(
    string Path,
    ushort VendorId,
    ushort ProductId,
    ushort UsagePage,
    ushort Usage,
    int InputReportLength,
    int OutputReportLength,
    int FeatureReportLength,
    string? ProductName)
{
    public override string ToString() =>
        $"{VendorId:X4}:{ProductId:X4} page=0x{UsagePage:X4} usage=0x{Usage:X4} in={InputReportLength} out={OutputReportLength} feat={FeatureReportLength}";

    /// <summary>Lists every present HID collection, optionally filtered by vendor/product id.</summary>
    public static IReadOnlyList<HidDeviceInfo> Enumerate(ushort? vendorId = null, ushort? productId = null)
    {
        var result = new List<HidDeviceInfo>();
        foreach (var path in EnumeratePaths())
        {
            var info = TryDescribe(path);
            if (info is null) continue;
            if (vendorId is not null && info.VendorId != vendorId) continue;
            if (productId is not null && info.ProductId != productId) continue;
            result.Add(info);
        }
        return result;
    }

    /// <summary>Reads attributes and capabilities without requesting read/write access.</summary>
    public static HidDeviceInfo? TryDescribe(string path)
    {
        using var handle = HidNative.CreateFile(path, 0, HidNative.FileShareReadWrite, IntPtr.Zero, HidNative.OpenExisting, 0, IntPtr.Zero);
        if (handle.IsInvalid) return null;

        var attributes = new HidNative.HiddAttributes { Size = Marshal.SizeOf<HidNative.HiddAttributes>() };
        if (!HidNative.HidD_GetAttributes(handle, ref attributes)) return null;

        if (!HidNative.HidD_GetPreparsedData(handle, out var preparsed)) return null;
        var caps = new HidNative.HidpCaps();
        try
        {
            if (HidNative.HidP_GetCaps(preparsed, ref caps) != HidNative.HidpStatusSuccess) return null;
        }
        finally
        {
            HidNative.HidD_FreePreparsedData(preparsed);
        }

        return new HidDeviceInfo(path, attributes.VendorId, attributes.ProductId, caps.UsagePage, caps.Usage,
            caps.InputReportByteLength, caps.OutputReportByteLength, caps.FeatureReportByteLength, ReadProductName(handle));
    }

    private static string? ReadProductName(SafeFileHandle handle)
    {
        var buffer = new char[128];
        if (!HidNative.HidD_GetProductString(handle, buffer, buffer.Length * sizeof(char))) return null;
        var end = Array.IndexOf(buffer, '\0');
        return new string(buffer, 0, end < 0 ? buffer.Length : end);
    }

    private static IEnumerable<string> EnumeratePaths()
    {
        HidNative.HidD_GetHidGuid(out var hidGuid);
        var set = HidNative.SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, HidNative.DigcfPresent | HidNative.DigcfDeviceInterface);
        if (set == new IntPtr(-1)) yield break;

        try
        {
            var data = new HidNative.SpDeviceInterfaceData { CbSize = Marshal.SizeOf<HidNative.SpDeviceInterfaceData>() };
            for (uint i = 0; HidNative.SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref hidGuid, i, ref data); i++)
            {
                HidNative.SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out var required, IntPtr.Zero);
                if (required <= 0) continue;

                var detail = Marshal.AllocHGlobal(required);
                try
                {
                    // SP_DEVICE_INTERFACE_DETAIL_DATA_W.cbSize: 8 on 64-bit, 6 on 32-bit.
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!HidNative.SetupDiGetDeviceInterfaceDetail(set, ref data, detail, required, out _, IntPtr.Zero)) continue;
                    var path = Marshal.PtrToStringUni(detail + 4);
                    if (path is not null) yield return path;
                }
                finally
                {
                    Marshal.FreeHGlobal(detail);
                }
            }
        }
        finally
        {
            HidNative.SetupDiDestroyDeviceInfoList(set);
        }
    }
}
