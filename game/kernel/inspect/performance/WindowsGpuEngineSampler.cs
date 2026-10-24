using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Campus.Contracts.Perf;

namespace Campus.Kernel.Inspect;

internal sealed record GpuEngineRow
{
    public required string Instance { get; init; }
    public uint? ProcessId { get; init; }
    public string? AdapterLuid { get; init; }
    public uint? PhysicalAdapter { get; init; }
    public uint? Engine { get; init; }
    public string? EngineType { get; init; }
    public uint CStatus { get; init; }
    public double? UtilFraction { get; init; }
    public uint? RawStatus { get; init; }
    public long? RawFirstValue { get; init; }
    public long? RawSecondValue { get; init; }
    public ulong? RawTimestamp100ns { get; init; }
    public uint? RawMultiCount { get; init; }
    public ulong? AverageStartTimestamp100ns { get; init; }
    public ulong? AverageEndTimestamp100ns { get; init; }
    public long? PreviousRawFirstValue { get; init; }
    public long? PreviousRawSecondValue { get; init; }
}
internal sealed record GpuEngineSamplingRead(PerfSamplingFact Fact, long? CollectTimestamp100ns,
    double? PreviousCollectMs, IReadOnlyList<GpuEngineRow> Engines)
{
    public int? RawQueryCode { get; init; }
}

/// <summary>Godot has no cross-process GPU counters. PDH GPU Engine is the single process authority;
/// the recording worker owns this query and collects it synchronously (no PDH timer thread).</summary>
internal sealed class WindowsGpuEngineSampler : IDisposable
{
    internal const string Source = "Windows.PDH.GPU Engine";
    private const uint MoreData = 0x800007D2, FormatDoubleNoCap = 0x00000200 | 0x00008000;
    private static readonly Regex InstancePattern = new(
        @"^pid_(\d+)_luid_0x([0-9a-fA-F]+)_0x([0-9a-fA-F]+)_phys_(\d+)_eng_(\d+)_engtype_(.+)$",
        RegexOptions.CultureInvariant);
    private IntPtr _query, _counter;
    private uint _openStatus;
    private double? _previousCollectMs;
    private Dictionary<string, RawValue> _previousRaw = new(StringComparer.Ordinal);
    private bool _disposed;
    public int? CloseCode { get; private set; }

    public WindowsGpuEngineSampler()
    {
        _openStatus = PdhOpenQueryW(null, UIntPtr.Zero, out _query);
        if (_openStatus == 0)
            _openStatus = PdhAddEnglishCounterW(_query, @"\GPU Engine(*)\Utilization Percentage", UIntPtr.Zero, out _counter);
    }

    public GpuEngineSamplingRead Read(string? luid, uint gameProcessId, Func<double> clock)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        double start = clock();
        uint status = _openStatus;
        long timestamp = 0;
        if (status == 0) status = PdhCollectQueryDataWithTime(_query, out timestamp);
        bool collected = status == 0;
        double collectMs = clock();
        IReadOnlyList<GpuEngineRow> rows = [];
        uint? rawStatus = null;
        if (status == 0) rows = ReadRows(out status, out rawStatus);
        double end = clock();
        var fact = Aggregate(rows, luid, gameProcessId, status, start, end, _previousCollectMs, collectMs);
        var result = new GpuEngineSamplingRead(fact, collected ? timestamp : null,
            _previousCollectMs, rows) { RawQueryCode = rawStatus.HasValue ? unchecked((int)rawStatus.Value) : null };
        _previousCollectMs = status == 0 ? collectMs : null;
        if (status != 0) _previousRaw.Clear();
        return result;
    }

    internal static PerfSamplingFact Aggregate(IReadOnlyList<GpuEngineRow> rows, string? luid, uint gameProcessId,
        uint status, double start, double end, double? previousCollectMs, double collectMs)
    {
        string? reason = null;
        double? value = null;
        if (status != 0) reason = "PDH GPU Engine query failed";
        else if (luid is null) reason = "Actual rendering adapter LUID is unknown";
        else if (previousCollectMs is null) reason = "Initial sample has no GPU Engine counter interval";
        else if (rows.Any(row => row.AdapterLuid is null || row.ProcessId is null))
            reason = "GPU Engine instance identity could not be parsed";
        else
        {
            var matching = rows.Where(row => row.AdapterLuid == luid).ToArray();
            if (matching.Length == 0) reason = "No GPU Engine instances for the actual rendering adapter";
            else if (matching.Any(row => row.CStatus > 1 || row.UtilFraction is not >= 0))
                reason = "Rendering adapter GPU Engine counter has no valid interval";
            else
            {
                // Same engine is shared by PIDs; summing engines would double-count parallel execution.
                var engines = matching.Where(row => row.ProcessId != gameProcessId)
                    .GroupBy(row => (row.PhysicalAdapter, row.Engine))
                    .Select(engine => engine.Sum(row => row.UtilFraction!.Value)).ToArray();
                value = engines.Length == 0 ? 0 : engines.Max();
                if (!double.IsFinite(value.Value) || value > 1)
                { value = null; reason = "GPU Engine aggregate is outside the physical fraction range"; }
            }
        }
        return new()
        {
            Value = value, Capability = value.HasValue ? PerfFactCapability.Supported : PerfFactCapability.Unavailable,
            Reason = reason ?? "PDH raw averaging endpoints are FILETIME; exact process-monotonic mapping is unavailable",
            Source = Source, NativeCode = unchecked((int)status), QueryStartMs = start, QueryEndMs = end
            // Raw counter timestamps preserve actual per-instance average windows below; collection latency is not that window.
        };
    }

    private IReadOnlyList<GpuEngineRow> ReadRows(out uint status, out uint? rawStatus)
    {
        rawStatus = null;
        uint size = 0, count = 0;
        status = PdhGetFormattedCounterArrayW(_counter, FormatDoubleNoCap, ref size, out count, IntPtr.Zero);
        if (status != MoreData) return [];
        IntPtr buffer = Marshal.AllocHGlobal(checked((int)size));
        try
        {
            status = PdhGetFormattedCounterArrayW(_counter, FormatDoubleNoCap, ref size, out count, buffer);
            if (status != 0) return [];
            var raw = ReadRawRows(out uint rawCode);
            rawStatus = rawCode;
            var rows = new List<GpuEngineRow>();
            int stride = Marshal.SizeOf<FormattedItem>();
            for (int index = 0; index < count; index++)
            {
                var item = Marshal.PtrToStructure<FormattedItem>(IntPtr.Add(buffer, checked(index * stride)));
                string name = Marshal.PtrToStringUni(item.Name) ?? "";
                RawValue? counter = raw.TryGetValue(name, out var found) ? found : null;
                var row = Parse(name, item.Value.Status,
                    item.Value.Status <= 1 && double.IsFinite(item.Value.Value) ? item.Value.Value / 100 : null, counter);
                if (counter is { Status: <= 1 } current && _previousRaw.TryGetValue(name, out var previous) && previous.Status <= 1 &&
                    current.Timestamp.Value > previous.Timestamp.Value)
                    row = row with { AverageStartTimestamp100ns = previous.Timestamp.Value,
                        AverageEndTimestamp100ns = current.Timestamp.Value, PreviousRawFirstValue = previous.First,
                        PreviousRawSecondValue = previous.Second };
                rows.Add(row);
            }
            _previousRaw = raw;
            return rows.ToArray();
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private Dictionary<string, RawValue> ReadRawRows(out uint status)
    {
        var rows = new Dictionary<string, RawValue>(StringComparer.Ordinal);
        uint size = 0;
        status = PdhGetRawCounterArrayW(_counter, ref size, out uint count, IntPtr.Zero);
        if (status != MoreData) return rows;
        IntPtr buffer = Marshal.AllocHGlobal(checked((int)size));
        try
        {
            status = PdhGetRawCounterArrayW(_counter, ref size, out count, buffer);
            if (status != 0) return rows;
            int stride = Marshal.SizeOf<RawItem>();
            for (int index = 0; index < count; index++)
            {
                var item = Marshal.PtrToStructure<RawItem>(IntPtr.Add(buffer, checked(index * stride)));
                string? name = Marshal.PtrToStringUni(item.Name);
                if (name is not null) rows[name] = item.Value;
            }
            return rows;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static GpuEngineRow Parse(string instance, uint status, double? value, RawValue? raw)
    {
        var match = InstancePattern.Match(instance);
        var row = new GpuEngineRow { Instance = instance, CStatus = status, UtilFraction = value,
            RawStatus = raw?.Status, RawFirstValue = raw?.First, RawSecondValue = raw?.Second,
            RawTimestamp100ns = raw?.Timestamp.Value, RawMultiCount = raw?.MultiCount };
        if (!match.Success) return row;
        return row with
        {
            ProcessId = uint.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            AdapterLuid = uint.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture).ToString("x8", CultureInfo.InvariantCulture) + ":" +
                uint.Parse(match.Groups[3].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture).ToString("x8", CultureInfo.InvariantCulture),
            PhysicalAdapter = uint.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
            Engine = uint.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture), EngineType = match.Groups[6].Value
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_query != IntPtr.Zero) { CloseCode = unchecked((int)PdhCloseQuery(_query)); _query = IntPtr.Zero; _counter = IntPtr.Zero; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FormattedValue { public uint Status; public double Value; }
    [StructLayout(LayoutKind.Sequential)]
    private struct FormattedItem { public IntPtr Name; public FormattedValue Value; }
    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime { public uint Low, High; public readonly ulong Value => ((ulong)High << 32) | Low; }
    [StructLayout(LayoutKind.Sequential)]
    private struct RawValue { public uint Status; public FileTime Timestamp; public long First, Second; public uint MultiCount; }
    [StructLayout(LayoutKind.Sequential)]
    private struct RawItem { public IntPtr Name; public RawValue Value; }
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhOpenQueryW(string? source, UIntPtr userData, out IntPtr query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhAddEnglishCounterW(IntPtr query, string path, UIntPtr userData, out IntPtr counter);
    [DllImport("pdh.dll")]
    private static extern uint PdhCollectQueryDataWithTime(IntPtr query, out long timestamp100ns);
    [DllImport("pdh.dll")]
    private static extern uint PdhGetFormattedCounterArrayW(IntPtr counter, uint format, ref uint size, out uint count, IntPtr items);
    [DllImport("pdh.dll")]
    private static extern uint PdhGetRawCounterArrayW(IntPtr counter, ref uint size, out uint count, IntPtr items);
    [DllImport("pdh.dll")]
    private static extern uint PdhCloseQuery(IntPtr query);
}
