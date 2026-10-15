using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Campus.Contracts.Quality;
using Godot;

namespace Campus.Kernel.Quality.Adaptive;

public sealed record GraphicsDevice(string Name, string Vendor, string Driver, ulong DedicatedBytes,
    ulong BudgetBytes, bool? Uma, bool MemoryKnown)
{
    public string CacheKey(int width, int height) => $"{Name}|{Driver}|{width}x{height}";
}

/// <summary>Only borrowed D3D12 handles are used directly; Vulkan handles are never treated as COM.</summary>
public static class DeviceProbe
{
    public static GraphicsDevice Read()
    {
        var name = RenderingServer.GetVideoAdapterName();
        var vendor = RenderingServer.GetVideoAdapterVendor();
        var driver = string.Join(" / ", OS.GetVideoAdapterDriverInfo());
        var result = new GraphicsDevice(name, vendor, driver, 0, 0, null, false);
        if (!OperatingSystem.IsWindows()) return result;
        try
        {
            var rd = RenderingServer.GetRenderingDevice();
            if (rd is not null && RenderingServer.GetCurrentRenderingDriverName().Equals("d3d12", StringComparison.OrdinalIgnoreCase))
            {
                var adapter = (nint)rd.GetDriverResource(RenderingDevice.DriverResource.PhysicalDevice, default, 0);
                var device = (nint)rd.GetDriverResource(RenderingDevice.DriverResource.LogicalDevice, default, 0);
                if (adapter != 0) result = ReadAdapter(adapter, result);
                if (device != 0)
                {
                    var architecture = new Architecture();
                    if (Method<CheckFeature>(device, 13)(device, 1, ref architecture, (uint)Marshal.SizeOf<Architecture>()) >= 0)
                        result = result with { Uma = architecture.Uma != 0 };
                }
                return result;
            }
            // DXGI enumeration is only a metadata fallback for Vulkan, matched to the active name.
            var factoryId = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
            if (CreateDXGIFactory1(ref factoryId, out var factory) < 0) return result;
            try
            {
                for (uint index = 0; index < 32; index++)
                {
                    if (Method<EnumAdapter>(factory, 12)(factory, index, out var adapter) < 0) break;
                    try
                    {
                        var candidate = ReadAdapter(adapter, result);
                        if (candidate.MemoryKnown && candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return candidate;
                    }
                    finally { Marshal.Release(adapter); }
                }
            }
            finally { Marshal.Release(factory); }
        }
        catch (Exception error) when (error is COMException or DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            GD.PushWarning($"[WP-32] Device metadata unavailable: {error.Message}");
        }
        return result;
    }

    public static DeviceTier? Classify(GraphicsDevice device)
    {
        if (device.Uma == true || device.MemoryKnown && device.DedicatedBytes < 1UL << 30) return DeviceTier.MinSpec;
        if (!device.MemoryKnown) return null;
        if (device.BudgetBytes >= 8UL << 30 && Regex.IsMatch(device.Name,
            @"RTX\s*(40[7-9]0|50[7-9]0)|RX\s*(7[89]00|9[789]00)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return DeviceTier.DgpuHigh;
        if (Regex.IsMatch(device.Name, @"\b(RTX|GTX|RX)\s*\d", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return DeviceTier.DgpuMid;
        return null;
    }

    public static QualityPreset Recommend(DeviceTier tier) => tier switch
    {
        DeviceTier.MinSpec => QualityPreset.Balanced,
        DeviceTier.DgpuMid => QualityPreset.Fine,
        DeviceTier.DgpuHigh => QualityPreset.Ultra,
        _ => throw new ArgumentOutOfRangeException(nameof(tier)),
    };

    public static DeviceTier Conservative(GraphicsDevice device) => device.Uma == false ||
        device.MemoryKnown && device.DedicatedBytes >= 1UL << 30 ? DeviceTier.DgpuMid : DeviceTier.MinSpec;

    public static DeviceTier FromSamples(GraphicsDevice device, IReadOnlyList<double> full, IReadOnlyList<double> reduced, double pixels)
    {
        if (full.Count < 20 || reduced.Count < 20 || pixels <= 0) return Conservative(device);
        var high = Percentile90(full);
        var low = Percentile90(reduced);
        var slope = (high - low) / (pixels * (1 - 0.67 * 0.67));
        var constant = high - slope * pixels;
        if (!double.IsFinite(slope) || slope <= 0 || constant < 0) return Conservative(device);
        if (device.Uma == true || device.MemoryKnown && device.DedicatedBytes < 1UL << 30) return DeviceTier.MinSpec;
        if (device.BudgetBytes >= 8UL << 30 && slope * Math.Min(pixels, 3_700_000) + constant <= 16.7 * 0.8)
            return DeviceTier.DgpuHigh;
        return slope * Math.Min(pixels, 2_100_000) + constant <= 16.7 * 0.8 ? DeviceTier.DgpuMid : DeviceTier.MinSpec;
    }

    private static double Percentile90(IReadOnlyList<double> values) => values.Order().ElementAt((int)Math.Ceiling(values.Count * 0.9) - 1);

    private static GraphicsDevice ReadAdapter(nint borrowed, GraphicsDevice fallback)
    {
        var adapterId = new Guid("645967a4-1392-4310-a798-8053ce3e93fd");
        if (Marshal.QueryInterface(borrowed, in adapterId, out var adapter) < 0) return fallback;
        try
        {
            if (Method<GetDesc>(adapter, 10)(adapter, out var desc) < 0) return fallback;
            var budget = Method<QueryMemory>(adapter, 14)(adapter, 0, 0, out var memory) >= 0 ? memory.Budget : 0;
            return fallback with { Name = desc.Description, DedicatedBytes = desc.DedicatedVideoMemory, BudgetBytes = budget, MemoryKnown = true };
        }
        finally { Marshal.Release(adapter); }
    }

    private static T Method<T>(nint instance, int slot) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));
    [DllImport("dxgi.dll", ExactSpelling = true)]
    private static extern int CreateDXGIFactory1(ref Guid id, out nint factory);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int EnumAdapter(nint self, uint index, out nint adapter);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetDesc(nint self, out AdapterDesc description);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int QueryMemory(nint self, uint node, int segment, out MemoryInfo info);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int CheckFeature(nint self, int feature, ref Architecture data, uint size);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct AdapterDesc
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Description;
        public uint VendorId, DeviceId, SubsystemId, Revision;
        public nuint DedicatedVideoMemory, DedicatedSystemMemory, SharedSystemMemory;
        public uint LuidLow; public int LuidHigh; public uint Flags;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MemoryInfo { public ulong Budget, CurrentUsage, AvailableForReservation, CurrentReservation; }
    [StructLayout(LayoutKind.Sequential)] private struct Architecture { public uint NodeIndex; public int TileBasedRenderer, Uma, CacheCoherentUma; }
}
