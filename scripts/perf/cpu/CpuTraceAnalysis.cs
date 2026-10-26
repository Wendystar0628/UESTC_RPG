// 读取 TraceLog，在线聚合原生栈索引的分配量子，并产出性能样段的采样汇总。
// 用法：由 CpuParse 创建并调用 ReadEvents；借用的 TraceLog 须活到报告序列化完成。
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

sealed class CpuTraceAnalysis
{
    private readonly TraceLog trace;
    private readonly int pid;
    private readonly IReadOnlyList<Window> windows;
    private readonly List<ThreadSample> samples = new();
    private readonly AllocationWindow[] phaseAllocations;
    private readonly Dictionary<GCAllocationKind, string> allocationKinds = new();
    private readonly Dictionary<string, string> textPool = new(StringComparer.Ordinal);
    private readonly Dictionary<CodeAddressIndex, Frame> frameDescriptions = new();
    private readonly Dictionary<CallStackIndex, bool> stackUnresolved = new();
    private readonly Dictionary<CallStackIndex, Frame[]> frameNames = new();

    public IReadOnlyList<ThreadSample> Samples => samples;

    public CpuTraceAnalysis(TraceLog trace, int pid, IReadOnlyList<Window> windows)
    {
        this.trace = trace;
        this.pid = pid;
        this.windows = windows;
        phaseAllocations = windows.Select(_ => new AllocationWindow()).ToArray();
    }

    public void ReadEvents()
    {
        Console.WriteLine($"CpuParse: reading indexed events ({trace.EventCount} indexed events, {trace.CallStacks.Count} stacks)");
        foreach (var data in trace.Events)
        {
            if (data.ProcessID != pid) continue;
            if (data is not ClrThreadSampleTraceData && data is not GCAllocationTickTraceData) continue;
            if (data is GCAllocationTickTraceData tick)
            {
                // Keep only native stack/type identities and aggregate counters, never a raw tick object.
                var index = trace.GetCallStackIndexForEvent(data);
                if (!allocationKinds.TryGetValue(tick.AllocationKind, out var kind))
                    allocationKinds.Add(tick.AllocationKind, kind = tick.AllocationKind.ToString());
                var type = new AllocationType(tick.ClrInstanceID, tick.Version >= 2 ? tick.TypeID : null,
                    tick.Version >= 2 ? Pool(tick.TypeName) : null, kind);
                var bytes = tick.Version >= 2 ? tick.AllocationAmount64 : (long)(uint)tick.AllocationAmount;
                var utc = data.TimeStamp.ToUniversalTime();
                var key = new AllocationKey(data.ThreadID, type, index);
                var unresolved = HasUnresolvedFrame(index);
                for (var i = 0; i < windows.Count; i++)
                    if (utc >= windows[i].Start && utc < windows[i].End)
                        phaseAllocations[i].Add(key, tick.Version, bytes > 0 ? bytes : null, unresolved);
                continue;
            }
            var stack = trace.GetCallStackForEvent(data);
            Frame[] frames;
            if (stack is null) frames = Array.Empty<Frame>();
            else if (!frameNames.TryGetValue(stack.CallStackIndex, out frames!))
            {
                var list = new List<Frame>();
                for (var cursor = stack; cursor is not null; cursor = cursor.Caller) list.Add(DescribeCached(cursor.CodeAddress));
                frames = list.ToArray();
                frameNames.Add(stack.CallStackIndex, frames);
            }
            if (data is ClrThreadSampleTraceData sample)
                samples.Add(new(data.TimeStamp.ToUniversalTime(), data.ThreadID, sample.Type.ToString(), frames));
        }
        Console.WriteLine($"CpuParse: aggregated allocation ticks into {phaseAllocations.Sum(window => window.Groups.Count)} window/thread/type/stack groups, {samples.Count} thread samples");
    }

    private string? Pool(string? text)
    {
        if (text is null) return null;
        if (!textPool.TryGetValue(text, out var shared)) textPool.Add(text, shared = text);
        return shared;
    }

    private Frame DescribeCached(TraceCodeAddress address)
    {
        if (!frameDescriptions.TryGetValue(address.CodeAddressIndex, out var frame))
        {
            var described = Describe(address);
            frameDescriptions.Add(address.CodeAddressIndex, frame = new Frame(Pool(described.Name)!, described.Unresolved));
        }
        return frame;
    }

    private bool HasUnresolvedFrame(CallStackIndex index)
    {
        if (index == CallStackIndex.Invalid) return false;
        if (stackUnresolved.TryGetValue(index, out var known)) return known;
        // Cache suffix results by TraceEvent's shared stack nodes, without expanded stack arrays/strings.
        var pending = new List<CallStackIndex>();
        var unresolved = false;
        for (var cursor = trace.CallStacks[index]; cursor is not null; cursor = cursor.Caller)
        {
            if (stackUnresolved.TryGetValue(cursor.CallStackIndex, out known)) { unresolved = known; break; }
            pending.Add(cursor.CallStackIndex);
            if (DescribeCached(cursor.CodeAddress).Unresolved) { unresolved = true; break; }
        }
        foreach (var item in pending) stackUnresolved[item] = unresolved;
        return unresolved;
    }

    public object SummarizeAllocations(int windowIndex, int mainId) =>
        AllocationSummary(phaseAllocations[windowIndex], mainId, trace.EventsLost > 0 || trace.Truncated);

    private static object AllocationSummary(AllocationWindow window, int mainId, bool incomplete)
    {
        object Totals(IEnumerable<AllocationCounts> values)
        {
            var sum = new AllocationCounts();
            foreach (var value in values) sum.Merge(value);
            return sum.Report();
        }
        IEnumerable<object> Types(IEnumerable<KeyValuePair<AllocationKey, AllocationCounts>> values) => values
            .GroupBy(x => x.Key.Type)
            .OrderByDescending(g => g.Sum(x => x.Value.Bytes)).ThenByDescending(g => g.Sum(x => x.Value.Events))
            .ThenBy(g => g.Key.TypeName, StringComparer.Ordinal).ThenBy(g => g.Key.TypeId)
            .ThenBy(g => g.Key.ClrInstanceId).ThenBy(g => g.Key.Kind, StringComparer.Ordinal)
            .Select(g => {
                // Groups contain accumulated counters, not copies of every tick.
                var stacks = g.GroupBy(x => x.Key.Stack).Select(s => {
                    var total = new AllocationCounts();
                    foreach (var item in s) total.Merge(item.Value);
                    return (Index: s.Key, Totals: total);
                }).OrderByDescending(s => s.Totals.Bytes).ThenByDescending(s => s.Totals.Events)
                    .ThenBy(s => (int)s.Index).ToArray();
                return (object)new {
                    clrInstanceId = g.Key.ClrInstanceId, typeId = g.Key.TypeId is { } id ? $"0x{id:x}" : null,
                    typeName = g.Key.TypeName, typeStatus = string.IsNullOrWhiteSpace(g.Key.TypeName) ? "NOT_MEASURED" : "CAPTURED_TICK_TYPE",
                    allocationKind = g.Key.Kind, totals = Totals(g.Select(x => x.Value)), capturedStackGroups = stacks.Length,
                    topAllocationStacks = stacks.Take(25).Select(s => new {
                        totals = s.Totals.Report(), stackStatus = s.Index != CallStackIndex.Invalid ? "CAPTURED; unresolved frames retained" : "NOT_MEASURED",
                        stackId = s.Index == CallStackIndex.Invalid ? (int?)null : (int)s.Index
                    }),
                    omittedStackGroups = Math.Max(0, stacks.Length - 25), omittedStackTotals = Totals(stacks.Skip(25).Select(s => s.Totals))
                };
            });
        return new {
            status = window.Totals.Events == 0 ? "NOT_MEASURED; no captured allocation ticks, not evidence of zero allocations" :
                incomplete ? "CAPTURED_ALLOCATION_QUANTA; trace incomplete" : "CAPTURED_ALLOCATION_QUANTA; not exact per-object accounting",
            totals = window.Totals.Report(),
            eventVersions = window.Versions.OrderBy(x => x.Key).Select(x => new { version = x.Key, allocationTickEvents = x.Value }),
            byType = Types(window.Groups),
            perThread = window.Groups.GroupBy(x => x.Key.ThreadId).OrderByDescending(g => g.Sum(x => x.Value.Bytes)).ThenBy(g => g.Key)
                .Select(g => new { osThreadId = g.Key, role = g.Key == mainId ? "main" : "worker-or-other",
                    totals = Totals(g.Select(x => x.Value)), byType = Types(g) })
        };
    }

    public static KeyValuePair<string, int>[] Ranked(ThreadSample[] samples, bool inclusive)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var sample in samples)
        {
            var names = inclusive ? sample.Frames.Select(f => f.Name).Distinct(StringComparer.Ordinal) : sample.Frames.Take(1).Select(f => f.Name);
            foreach (var name in names) counts[name] = counts.GetValueOrDefault(name) + 1;
        }
        return counts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal).Take(25).ToArray();
    }

    private static Frame Describe(TraceCodeAddress address)
    {
        var module = string.IsNullOrEmpty(address.ModuleName) ? "unknown-module" : address.ModuleName;
        var method = address.FullMethodName;
        var unresolved = address.Method is null || string.IsNullOrEmpty(method);
        return new(unresolved ? $"{module}!0x{address.Address:x} [native-or-unresolved]" : $"{module}!{method}", unresolved);
    }
}

record Frame(string Name, bool Unresolved);
record ThreadSample(DateTime Utc, int ThreadId, string Type, Frame[] Frames);
readonly record struct AllocationType(int ClrInstanceId, ulong? TypeId, string? TypeName, string Kind);
readonly record struct AllocationKey(int ThreadId, AllocationType Type, CallStackIndex Stack);
sealed class AllocationWindow
{
    public readonly Dictionary<AllocationKey, AllocationCounts> Groups = new();
    public readonly Dictionary<int, long> Versions = new();
    public readonly AllocationCounts Totals = new();
    public void Add(AllocationKey key, int version, long? bytes, bool unresolved)
    {
        Versions[version] = Versions.GetValueOrDefault(version) + 1;
        if (!Groups.TryGetValue(key, out var counts)) Groups.Add(key, counts = new AllocationCounts());
        counts.Add(bytes, string.IsNullOrWhiteSpace(key.Type.TypeName), key.Stack == CallStackIndex.Invalid, unresolved);
        Totals.Add(bytes, string.IsNullOrWhiteSpace(key.Type.TypeName), key.Stack == CallStackIndex.Invalid, unresolved);
    }
}
sealed class AllocationCounts
{
    public long Events, Bytes, ValidByteEvents, MissingType, MissingStack, UnresolvedFrame;
    public void Add(long? bytes, bool missingType, bool missingStack, bool unresolved)
    {
        checked {
            Events++;
            if (bytes.HasValue) { Bytes += bytes.Value; ValidByteEvents++; }
            if (missingType) MissingType++;
            if (missingStack) MissingStack++;
            if (unresolved) UnresolvedFrame++;
        }
    }
    public void Merge(AllocationCounts other)
    {
        checked {
            Events += other.Events; Bytes += other.Bytes; ValidByteEvents += other.ValidByteEvents;
            MissingType += other.MissingType; MissingStack += other.MissingStack; UnresolvedFrame += other.UnresolvedFrame;
        }
    }
    public object Report() => new {
        allocationTickEvents = Events, sampledAllocationBytes = ValidByteEvents == 0 ? (long?)null : Bytes,
        ticksMissingValidByteAmount = Events - ValidByteEvents, ticksMissingTypeName = MissingType,
        ticksMissingCallStack = MissingStack, ticksWithUnresolvedFrame = UnresolvedFrame
    };
}
