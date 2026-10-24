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
}
internal sealed record GpuEngineSamplingRead(PerfSamplingFact Fact, long? CollectTimestamp100ns,
    double? PreviousCollectMs, GpuEngineRecording Recording)
{
    public int? RawQueryCode { get; init; }
}

internal sealed record GpuEngineTypeSummary(string EngineType, double? OtherProcessesUtilFraction,
    int InstanceCount, int InvalidInstanceCount);
internal sealed record GpuProcessSummary(uint ProcessId, double? UtilFraction, IReadOnlyList<GpuEngineRow> Engines);
internal sealed record GpuEngineRecording(string? AdapterLuid, int InstanceCount, int InvalidInstanceCount,
    IReadOnlyList<GpuEngineTypeSummary> EngineTypes, IReadOnlyList<GpuProcessSummary> TopProcesses);

/// <summary>Godot has no cross-process GPU counters. PDH GPU Engine is the single process authority;
/// the recording worker owns this query and collects it synchronously (no PDH timer thread).</summary>
internal sealed class WindowsGpuEngineSampler : IDisposable
{
    internal const string Source = "Windows.PDH.GPU Engine";
    internal const int TopProcessCount = 3;
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
        if (status == 0) rows = ReadRows(luid, out status, out rawStatus);
        double end = clock();
        var fact = Aggregate(rows, luid, gameProcessId, status, start, end, _previousCollectMs, collectMs);
        var result = new GpuEngineSamplingRead(fact, collected ? timestamp : null,
            _previousCollectMs, ForRecording(rows, luid, gameProcessId))
            { RawQueryCode = rawStatus.HasValue ? unchecked((int)rawStatus.Value) : null };
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
        else
        {
            var matching = rows.Where(row => row.AdapterLuid == luid).ToArray();
            if (matching.Length == 0) reason = "No GPU Engine instances for the actual rendering adapter";
            else if (matching.Any(row => row.ProcessId is null || row.PhysicalAdapter is null || row.Engine is null || row.EngineType is null))
                reason = "GPU Engine instance identity could not be parsed";
            else if (matching.Any(row => row.CStatus > 1 || row.UtilFraction is not >= 0))
                reason = "Rendering adapter GPU Engine counter has no valid interval";
            else
            {
                // Same engine is shared by PIDs; summing engines would double-count parallel execution.
                value = BusiestEngine(matching.Where(row => row.ProcessId != gameProcessId).ToArray());
                if (value is null) reason = "GPU Engine aggregate is outside the physical fraction range";
            }
        }
        return new()
        {
            Value = value, Capability = value.HasValue ? PerfFactCapability.Supported : PerfFactCapability.Unavailable,
            Reason = reason,
            Source = Source, NativeCode = unchecked((int)status), QueryStartMs = start, QueryEndMs = end
            // Raw counter timestamps preserve actual per-instance average windows below; collection latency is not that window.
        };
    }

    internal static GpuEngineRecording ForRecording(IReadOnlyList<GpuEngineRow> rows, string? luid,
        uint gameProcessId)
    {
        var matching = rows.Where(row => luid is not null && row.AdapterLuid == luid && row.ProcessId != gameProcessId).ToArray();
        var types = matching.Where(row => row.EngineType is not null)
            .GroupBy(row => row.EngineType!, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new GpuEngineTypeSummary(group.Key, BusiestEngine(group), group.Count(),
                group.Count(row => !Valid(row)))).ToArray();
        var processes = matching.Where(row => row.ProcessId is not null).GroupBy(row => row.ProcessId!.Value)
            .Select(group => new { ProcessId = group.Key, UtilFraction = BusiestEngine(group), Engines = group })
            .OrderByDescending(process => process.UtilFraction).ThenBy(process => process.ProcessId)
            .Take(TopProcessCount)
            .Select(process => new GpuProcessSummary(process.ProcessId, process.UtilFraction,
                process.Engines.OrderBy(row => row.PhysicalAdapter).ThenBy(row => row.Engine)
                    .ThenBy(row => row.Instance, StringComparer.Ordinal).ToArray())).ToArray();
        return new(luid, matching.Length, matching.Count(row => !Valid(row)), types, processes);
    }

    private static bool Valid(GpuEngineRow row) => row.ProcessId.HasValue && row.PhysicalAdapter.HasValue &&
        row.Engine.HasValue && row.EngineType is not null && row.CStatus <= 1 &&
        row.UtilFraction is { } fraction && fraction >= 0 && double.IsFinite(fraction);

    private static double? BusiestEngine(IEnumerable<GpuEngineRow> rows)
    {
        if (rows.Any(row => !Valid(row))) return null;
        double value = rows.GroupBy(row => (row.PhysicalAdapter, row.Engine))
            .Select(engine => engine.Sum(row => row.UtilFraction!.Value)).DefaultIfEmpty(0).Max();
        return double.IsFinite(value) && value <= 1 ? value : null;
    }

    private IReadOnlyList<GpuEngineRow> ReadRows(string? luid, out uint status, out uint? rawStatus)
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
            string? prefix = luid is null ? null : "_luid_0x" + luid.Replace(":", "_0x", StringComparison.Ordinal) + "_";
            var raw = ReadRawRows(prefix, out uint rawCode);
            rawStatus = rawCode;
            var rows = new List<GpuEngineRow>();
            int stride = Marshal.SizeOf<FormattedItem>();
            for (int index = 0; index < count; index++)
            {
                var item = Marshal.PtrToStructure<FormattedItem>(IntPtr.Add(buffer, checked(index * stride)));
                string name = Marshal.PtrToStringUni(item.Name) ?? "";
                if (!MatchesAdapter(name, prefix)) continue;
                RawValue? counter = raw.TryGetValue(name, out var found) ? found : null;
                var row = Parse(name, item.Value.Status,
                    item.Value.Status <= 1 && double.IsFinite(item.Value.Value) ? item.Value.Value / 100 : null, counter)
                    with { AdapterLuid = luid };
                if (counter is { Status: <= 1 } current && _previousRaw.TryGetValue(name, out var previous) && previous.Status <= 1 &&
                    current.Timestamp.Value > previous.Timestamp.Value)
                    row = row with { AverageStartTimestamp100ns = previous.Timestamp.Value,
                        AverageEndTimestamp100ns = current.Timestamp.Value };
                rows.Add(row);
            }
            _previousRaw = raw;
            return rows.ToArray();
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private Dictionary<string, RawValue> ReadRawRows(string? prefix, out uint status)
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
                if (name is not null && MatchesAdapter(name, prefix)) rows[name] = item.Value;
            }
            return rows;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static bool MatchesAdapter(string instance, string? prefix) => prefix is not null &&
        instance.Contains(prefix, StringComparison.OrdinalIgnoreCase);

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
