using System.Globalization;
using System.Runtime.InteropServices;
using Godot;

namespace Campus.Kernel.Inspect;

/// <summary>Only detached values cross out of the rendering thread. Godot provides the actual
/// D3D12 IDXGIAdapter; public DXGI and D3DKMT APIs supply identity, never an enumeration/name guess.</summary>
internal sealed record RenderAdapterIdentity
{
    public ulong? Luid { get; init; }
    public string? LuidText => Luid is { } value ? ((uint)(value >> 32)).ToString("x8", CultureInfo.InvariantCulture) + ":" +
        ((uint)value).ToString("x8", CultureInfo.InvariantCulture) : null;
    public uint? VendorId { get; init; }
    public uint? DeviceId { get; init; }
    public uint? SubSystemId { get; init; }
    public uint? Bus { get; init; }
    public uint? Device { get; init; }
    public uint? Function { get; init; }
    public NvmlStatus Status { get; init; } = new("unavailable", "Rendering adapter not captured");
    public NvmlStatus AddressStatus { get; init; } = new("unavailable", "Windows PCI address not queried");
    public int? CloseStatus { get; init; }

    public static Task<RenderAdapterIdentity> CaptureAsync()
    {
        var completion = new TaskCompletionSource<RenderAdapterIdentity>(TaskCreationOptions.RunContinuationsAsynchronously);
        string driver = RenderingServer.GetCurrentRenderingDriverName();
        if (!OperatingSystem.IsWindows() || !string.Equals(driver, "d3d12", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new RenderAdapterIdentity { Status = new("unsupported", "Actual adapter capture requires Windows D3D12") });
        try
        {
            RenderingServer.CallOnRenderThread(Callable.From(() =>
            {
                try { completion.TrySetResult(CaptureOnRenderThread()); }
                catch (Exception error) { completion.TrySetResult(new() { Status = NvmlStatus.Missing(error) }); }
            }));
        }
        catch (Exception error) { completion.TrySetResult(new() { Status = NvmlStatus.Missing(error) }); }
        return completion.Task;
    }

    private static RenderAdapterIdentity CaptureOnRenderThread()
    {
        var device = RenderingServer.GetRenderingDevice();
        if (device is null) return new() { Status = new("unavailable", "Global rendering device is unavailable") };
        ulong resource = device.GetDriverResource(RenderingDevice.DriverResource.PhysicalDevice, default, 0);
        if (resource == 0) return new() { Status = new("unavailable", "D3D12 rendering adapter pointer is unavailable") };
        // Borrowed pointer: consume GetDesc here, without AddRef/Release or retaining a COM object.
        var adapter = new IntPtr(unchecked((long)resource));
        IntPtr table = Marshal.ReadIntPtr(adapter);
        var getDescription = Marshal.GetDelegateForFunctionPointer<GetDescription>(Marshal.ReadIntPtr(table, 8 * IntPtr.Size));
        int code = getDescription(adapter, out var description);
        if (code < 0) return new() { Status = new("unavailable", "IDXGIAdapter.GetDesc failed", code) };
        return new()
        {
            Luid = ((ulong)(uint)description.Luid.High << 32) | description.Luid.Low,
            VendorId = description.VendorId, DeviceId = description.DeviceId, SubSystemId = description.SubSystemId,
            Status = new("supported", NativeCode: code)
        };
    }

    // Called once by the existing hardware worker. Every opened kernel adapter is closed in this scope.
    public RenderAdapterIdentity ResolveAddress()
    {
        if (Luid is not { } luid) return this;
        var open = new OpenAdapter { Luid = new() { Low = (uint)luid, High = unchecked((int)(luid >> 32)) } };
        int code = D3DKMTOpenAdapterFromLuid(ref open);
        if (code < 0) return this with { AddressStatus = new("unavailable", "D3DKMTOpenAdapterFromLuid failed", code) };
        IntPtr buffer = IntPtr.Zero;
        var result = this;
        try
        {
            buffer = Marshal.AllocHGlobal(Marshal.SizeOf<AdapterAddress>());
            var query = new QueryAdapter { Adapter = open.Adapter, Type = 6, Data = buffer, Size = (uint)Marshal.SizeOf<AdapterAddress>() };
            code = D3DKMTQueryAdapterInfo(ref query);
            if (code < 0) result = this with { AddressStatus = new("unavailable", "D3DKMTQueryAdapterInfo(ADAPTERADDRESS) failed", code) };
            else
            {
                var address = Marshal.PtrToStructure<AdapterAddress>(buffer);
                result = this with { Bus = address.Bus, Device = address.Device, Function = address.Function,
                    AddressStatus = new("supported", NativeCode: code) };
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            var close = new CloseAdapter { Adapter = open.Adapter };
            int closed = D3DKMTCloseAdapter(ref close);
            result = result with { CloseStatus = closed };
        }
        return result;
    }

    internal NvmlDeviceSample? Match(IReadOnlyList<NvmlDeviceSample> devices)
    {
        if (Bus is null || Device is null || Function is null || !AddressStatus.IsSuccess) return null;
        var matches = devices.Where(device => device.Identity.PciBus == Bus && device.Identity.PciDevice == Device &&
            device.Identity.PciFunction == Function && device.Identity.PciVendorId == VendorId &&
            device.Identity.PciDeviceId == DeviceId && device.Identity.PciSubSystemId == SubSystemId).ToArray();
        // Windows has no PCI domain here. Multiple domains with the same BDF remain ambiguous.
        return matches.Length == 1 ? matches[0] : null;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDescription(IntPtr adapter, out AdapterDescription description);
    [StructLayout(LayoutKind.Sequential)]
    private struct AdapterLuid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct AdapterDescription
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Description;
        public uint VendorId, DeviceId, SubSystemId, Revision;
        public UIntPtr DedicatedVideoMemory, DedicatedSystemMemory, SharedSystemMemory;
        public AdapterLuid Luid;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct OpenAdapter { public AdapterLuid Luid; public uint Adapter; }
    [StructLayout(LayoutKind.Sequential)]
    private struct QueryAdapter { public uint Adapter; public int Type; public IntPtr Data; public uint Size; }
    [StructLayout(LayoutKind.Sequential)]
    private struct AdapterAddress { public uint Bus, Device, Function; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CloseAdapter { public uint Adapter; }
    [DllImport("gdi32.dll")]
    private static extern int D3DKMTOpenAdapterFromLuid(ref OpenAdapter adapter);
    [DllImport("gdi32.dll")]
    private static extern int D3DKMTQueryAdapterInfo(ref QueryAdapter query);
    [DllImport("gdi32.dll")]
    private static extern int D3DKMTCloseAdapter(ref CloseAdapter adapter);
}
