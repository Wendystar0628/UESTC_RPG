// 复用 CpuTraceAnalysis 解析性能诊断会话的 EventPipe 附件，报告墙钟栈与实际样本间隔。
// 用法：dotnet CpuParse.dll --perf-session <trace-directory> <closed-perf-session-directory>。
using System.Text.Json;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;

static class PerfSessionCpuReport
{
    public static void Run(string diagnosticDirectory, string sessionDirectory)
    {
        string directory = Path.GetFullPath(diagnosticDirectory);
        using var condition = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "condition.json")));
        if (condition.RootElement.GetProperty("purpose").GetString() != "diagnostic" ||
            condition.RootElement.GetProperty("tool").GetString() != "EventPipe")
            throw new InvalidDataException("EventPipe report requires explicit diagnostic conditions.");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(sessionDirectory, "manifest.json")));
        var root = manifest.RootElement;
        if (root.GetProperty("status").GetString() != "closed") throw new InvalidDataException("Performance session is not closed.");
        string index = Path.Combine(directory, "threads.etlx");
        Exception? failure = null;
        try
        {
            TraceLog.CreateFromEventPipeDataFile(Path.Combine(directory, "threads.nettrace"), index);
            using var trace = new TraceLog(index);
            int[] ids = trace.Events.Where(e => e.ProviderName == "Microsoft-Windows-DotNETRuntime" && e.ProcessID > 0)
                .Select(e => e.ProcessID).Distinct().ToArray();
            if (ids.Length != 1) throw new InvalidDataException("Trace does not uniquely identify one managed target process.");
            var anchors = root.GetProperty("clock").GetProperty("anchors").EnumerateArray().ToArray();
            if (anchors.Length == 0) throw new InvalidDataException("Session clock anchors are missing.");
            var anchor = anchors[0];
            double mono = anchor.GetProperty("monotonicMs").GetDouble();
            DateTime utc = DateTimeOffset.FromUnixTimeMilliseconds(anchor.GetProperty("unixMs").GetInt64()).UtcDateTime;
            var events = File.ReadLines(Path.Combine(sessionDirectory, "events.jsonl"))
                .Select(line => JsonSerializer.Deserialize<JsonElement>(line)).ToArray();
            string segmentId = condition.RootElement.GetProperty("collection").GetProperty("sampleSegmentId").GetString()
                ?? throw new InvalidDataException("Diagnostic sample segment identity is missing.");
            var marks = events.Where(e => e.GetProperty("event").GetString() == "segment" &&
                e.GetProperty("payload").GetProperty("id").GetString() == segmentId).ToArray();
            if (marks.Length != 2 || marks[0].GetProperty("payload").GetProperty("phase").GetString() != "begin" ||
                marks[1].GetProperty("payload").GetProperty("phase").GetString() != "end" ||
                marks[0].GetProperty("sequence").GetInt64() >= marks[1].GetProperty("sequence").GetInt64())
                throw new InvalidDataException("Diagnostic sample must have exactly one ordered Begin/End pair.");
            double start = marks[0].GetProperty("monotonicMs").GetDouble();
            double end = marks[1].GetProperty("monotonicMs").GetDouble();
            if (!double.IsFinite(start) || !double.IsFinite(end) || start >= end)
                throw new InvalidDataException("Diagnostic sample Begin/End timestamps are invalid.");
            Window[] windows = [new(segmentId, "segment.begin", "segment.end", utc.AddMilliseconds(start - mono), utc.AddMilliseconds(end - mono))];
            var analysis = new CpuTraceAnalysis(trace, ids[0], windows);
            analysis.ReadEvents();
            var samples = analysis.Samples.Where(s => s.Utc >= windows[0].Start && s.Utc < windows[0].End).ToArray();
            var report = new
            {
                purpose = "diagnostic", session = root.GetProperty("id").GetString(), processId = ids[0],
                profile = condition.RootElement.GetProperty("cpuProfile").GetString(),
                requestedSamplingRateHz = (double?)null,
                nominalSamplingRateHz = condition.RootElement.GetProperty("nominalSampleRateHz").GetDouble(),
                configuredSamplingRateHz = (double?)null,
                samplingRateSource = "Not asserted: collector stdout retains effective providers; observed per-thread timestamp intervals follow.",
                sampleMeaning = "dotnet-sampled-thread-time wall-clock managed thread stacks; not on-CPU samples or CPU utilization",
                trace.EventsLost, trace.Truncated,
                sampleSegmentId = segmentId, windows, clockAnchorReadSpanMs = anchors.Max(a => a.GetProperty("readSpanMs").GetDouble()),
                managedStackResolution = StackResolution(samples),
                threads = samples.GroupBy(s => s.ThreadId).Select(group => ThreadReport(group)).ToArray(),
                gcAllocationSampling = analysis.CreateAllocationReport(0),
                sampleGcAllocationSampling = analysis.SummarizeAllocations(0, 0),
                mainOsThreadId = (int?)null,
                limitations = new[] { "Main OS thread identity is not supplied by the launched-process trace; no main-thread attribution is invented.",
                    "Observed sample rate is affected by thread lifetime, sample loss and profiler state; it is not CPU utilization or a guarantee of configured frequency.",
                    "Sample window UTC is derived from the exact segment Begin/End and captured monotonic/UTC anchors; boundary uncertainty is retained.",
                    "Managed sample classification comes from the sample profiler. Stack-frame occurrence totals include native-or-unresolved frames inside those samples; that frame fraction is not the managed-sample method-availability fraction." }
            };
            using var output = File.Create(Path.Combine(directory, "cpu-hotspots.json"));
            JsonSerializer.Serialize(output, report, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception error) { failure = error; throw; }
        finally
        {
            try { if (File.Exists(index)) File.Delete(index); }
            catch (Exception cleanup)
            {
                if (failure is null) throw;
                throw new AggregateException("EventPipe parsing and trace-index cleanup both failed.", failure, cleanup);
            }
        }
    }

    private static object StackResolution(ThreadSample[] samples)
    {
        var managed = samples.Where(s => s.Type == nameof(ClrThreadSampleType.Managed)).ToArray();
        var frames = managed.SelectMany(s => s.Frames).ToArray();
        var resolved = frames.Where(f => !f.Unresolved).ToArray();
        return new
        {
            managedSamples = managed.Length,
            managedSamplesWithResolvedMethods = managed.Count(s => s.Frames.Any(f => !f.Unresolved)),
            stackFrameOccurrences = frames.Length,
            resolvedMethodFrameOccurrences = resolved.Length,
            resolvedMethodFrameFraction = frames.Length == 0 ? (double?)null : (double)resolved.Length / frames.Length,
            distinctResolvedMethods = resolved.Select(f => f.Name).Distinct(StringComparer.Ordinal).Count()
        };
    }

    private static object ThreadReport(IGrouping<int, ThreadSample> group)
    {
        var ordered = group.OrderBy(s => s.Utc).ToArray();
        var intervals = ordered.Zip(ordered.Skip(1), (a, b) => (b.Utc - a.Utc).TotalMilliseconds).Where(ms => ms > 0).Order().ToArray();
        double? median = intervals.Length == 0 ? null : intervals[intervals.Length / 2];
        return new
        {
            osThreadId = group.Key, samples = ordered.Length, sampleTypes = ordered.GroupBy(s => s.Type).ToDictionary(g => g.Key, g => g.Count()),
            observedIntervalP50Ms = median, observedRateFromMedianIntervalHz = median > 0 ? 1000 / median : null,
            missingStackSamples = ordered.Count(s => !s.HasStack),
            managedStackResolution = StackResolution(ordered),
            topStacks = ordered.GroupBy(s => string.Join("\n", s.Frames.Select(f => f.Name)))
                .OrderByDescending(g => g.Count()).Take(30).Select(g => new { samples = g.Count(),
                    wallClockSampleFraction = (double)g.Count() / ordered.Length, framesLeafToRoot = g.First().Frames })
        };
    }
}
