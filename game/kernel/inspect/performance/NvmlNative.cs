using System.Runtime.InteropServices;
using System.Text;

namespace Campus.Kernel.Inspect;

/// <summary>
/// Godot does not expose these hardware counters or driver sample buffers. Use the public NVML C ABI
/// directly, with .NET marshaling; no engine/private interop or external diagnostic process.
/// Signatures: https://docs.nvidia.com/deploy/nvml-api/latest/api/group__nvmlDeviceQueries.html
/// </summary>
internal static class NvmlNative
{
    private const string Library = "nvml.dll";
    internal const int Success = 0, NotSupported = 3, NotFound = 6, InsufficientSize = 7;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Memory { public ulong Total, Free, Used; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Utilization { public uint Gpu, Memory; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Violation { public ulong ReferenceUs, Nanoseconds; }
    [StructLayout(LayoutKind.Sequential)]
    private struct FieldValue
    {
        public uint FieldId, ScopeId;
        public long TimestampUs, LatencyUs;
        public int ValueType, ReturnCode;
        public Value Value;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct Pci
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] public string LegacyBusId;
        public uint Domain, Bus, Device, DeviceId, SubSystemId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string BusId;
    }
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct Value
    {
        [FieldOffset(0)] public uint UnsignedInt;
        [FieldOffset(0)] public ulong UnsignedLongLong;
        [FieldOffset(0)] public double Double;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Sample { public ulong TimestampUs; public Value Value; }

    [DllImport(Library, EntryPoint = "nvmlInit_v2", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Init();
    [DllImport(Library, EntryPoint = "nvmlShutdown", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Shutdown();
    [DllImport(Library, EntryPoint = "nvmlDeviceGetCount_v2", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int GetCount(out uint count);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetHandleByIndex_v2", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int GetHandle(uint index, out IntPtr device);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetUUID", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int GetUuid(IntPtr device, StringBuilder uuid, uint length);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetPciInfo_v3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetPci(IntPtr device, out Pci pci);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetClockInfo", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetClock(IntPtr device, uint type, out uint clock);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetMaxClockInfo", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetMaxClock(IntPtr device, uint type, out uint clock);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetTemperature", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetTemperature(IntPtr device, uint sensor, out uint temperature);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetMemoryInfo", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetMemory(IntPtr device, out Memory memory);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetUtilizationRates", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetUtilization(IntPtr device, out Utilization utilization);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetPowerUsage", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetPower(IntPtr device, out uint milliwatts);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetPerformanceState", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetState(IntPtr device, out uint state);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetCurrentClocksEventReasons", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetClockReasons(IntPtr device, out ulong reasons);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetTotalEnergyConsumption", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetEnergy(IntPtr device, out ulong millijoules);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetFieldValues", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetFieldValues(IntPtr device, int count, [In, Out] FieldValue[] values);
    [DllImport(Library, EntryPoint = "nvmlDeviceGetSamples", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetSamples(IntPtr device, NvmlMetric type, ulong lastSeenTimestamp,
        out int valueType, ref uint count, [Out] Sample[]? samples);

    internal static bool IsLoadFailure(Exception error) =>
        error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;

    internal static NvmlValue<T> Read<T>(Func<(int Code, T Value)> query) where T : struct
    {
        try
        {
            var result = query();
            return new(result.Code == Success ? result.Value : null, NvmlStatus.FromCode(result.Code));
        }
        catch (Exception error) when (IsLoadFailure(error)) { return new(null, NvmlStatus.Missing(error)); }
    }

    internal static NvmlIdentity Identity(uint index, IntPtr handle)
    {
        string? uuid = null;
        NvmlStatus uuidStatus;
        try
        {
            var buffer = new StringBuilder(96); // NVML_DEVICE_UUID_BUFFER_SIZE
            uuidStatus = NvmlStatus.FromCode(GetUuid(handle, buffer, (uint)buffer.Capacity));
            if (uuidStatus.IsSuccess) uuid = buffer.ToString();
        }
        catch (Exception error) when (IsLoadFailure(error)) { uuidStatus = NvmlStatus.Missing(error); }
        var pci = Read(() => { int code = GetPci(handle, out var value); return (code, value); });
        return new(index, uuid, pci.Value?.BusId, NvmlStatus.FromCode(Success), uuidStatus, pci.Status);
    }

    internal static NvmlValue<uint> Clock(IntPtr device, uint type, bool maximum = false) => Read(() =>
    {
        uint value;
        int code = maximum ? GetMaxClock(device, type, out value) : GetClock(device, type, out value);
        return (code, value);
    });
    internal static NvmlValue<uint> Temperature(IntPtr device) => Read(() =>
    { int code = GetTemperature(device, 0, out uint value); return (code, value); });
    internal static NvmlValue<Memory> MemoryInfo(IntPtr device) => Read(() =>
    { int code = GetMemory(device, out var value); return (code, value); });
    internal static NvmlValue<Utilization> UtilizationInfo(IntPtr device) => Read(() =>
    { int code = GetUtilization(device, out var value); return (code, value); });
    internal static NvmlValue<uint> Power(IntPtr device) => Read(() =>
    { int code = GetPower(device, out uint value); return (code, value); });
    internal static NvmlValue<uint> State(IntPtr device)
    {
        var state = Read(() => { int code = GetState(device, out uint value); return (code, value); });
        return state.Value is > 15 ? new(null, new("unavailable", "NVML returned unknown performance state")) : state;
    }
    internal static NvmlValue<ulong> ClockReasons(IntPtr device) => Read(() =>
    { int code = GetClockReasons(device, out ulong value); return (code, value); });
    internal static NvmlValue<ulong> Energy(IntPtr device) => Read(() =>
    { int code = GetEnergy(device, out ulong value); return (code, value); });
    internal static NvmlValue<Violation> Policy(IntPtr device, NvmlPolicy policy)
    {
        // Public nvml.h field identifiers; all these counters are nanoseconds. No sum is formed.
        uint field = policy switch
        {
            NvmlPolicy.Power => 74, NvmlPolicy.Thermal => 269, NvmlPolicy.SyncBoost => 76,
            NvmlPolicy.BoardLimit => 77, NvmlPolicy.LowUtilization => 78, NvmlPolicy.Reliability => 79,
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
        var read = Read(() =>
        {
            FieldValue[] values = [new() { FieldId = field }];
            int code = GetFieldValues(device, values.Length, values);
            return (code == Success ? values[0].ReturnCode : code, values[0]);
        });
        if (read.Value is not { } value) return new(null, read.Status);
        if (value.ValueType != 3 || value.TimestampUs < 0)
            return new(null, new("unavailable", "Unexpected NVML policy counter type or timestamp"));
        return new(new Violation { ReferenceUs = (ulong)value.TimestampUs, Nanoseconds = value.Value.UnsignedLongLong }, read.Status);
    }

    internal static (NvmlStatus Status, Sample[] Samples) Samples(IntPtr device, NvmlMetric type, ulong cursor,
        Func<bool> stopping)
    {
        var result = ReadSamples(device, type, cursor, stopping);
        if (result.Status.NativeCode != NotFound || cursor == 0 || stopping()) return result;
        // A wall-clock adjustment can put every retained timestamp behind the cursor. Probe only on
        // NOT_FOUND, and reset only with evidence that the newest retained timestamp regressed.
        var retained = ReadSamples(device, type, 0, stopping);
        return retained.Status.IsSuccess && retained.Samples.Length > 0 &&
            retained.Samples.Max(sample => sample.TimestampUs) < cursor ? retained : result;
    }

    private static (NvmlStatus Status, Sample[] Samples) ReadSamples(IntPtr device, NvmlMetric type,
        ulong cursor, Func<bool> stopping)
    {
        try
        {
            uint count = 0;
            int code = GetSamples(device, type, cursor, out _, ref count, null);
            if (code != Success && code != InsufficientSize) return (NvmlStatus.SampleResult(code), []);
            if (count == 0) return (NvmlStatus.FromCode(Success), []);
            if (stopping()) return (new("unavailable", "Sampling stopped before buffer read"), []);
            // Size is supplied by NVML, never a guessed number of samples per second.
            var values = new Sample[checked((int)count)];
            code = GetSamples(device, type, cursor, out int valueType, ref count, values);
            if (code != Success) return (NvmlStatus.SampleResult(code), []);
            if (valueType != 1) return (new("unavailable", "Unexpected NVML sample value type"), []);
            if (count > values.Length) return (new("unavailable", "NVML returned an invalid sample count"), []);
            Array.Resize(ref values, (int)count);
            return (NvmlStatus.FromCode(Success), values);
        }
        catch (Exception error) when (IsLoadFailure(error)) { return (NvmlStatus.Missing(error), []); }
    }
}
