// 复用 CpuTraceAnalysis 解析性能诊断的 EventPipe 附件：只取采样段内的墙钟栈，写 cpu-hotspots.json 并打印热点。
// 用法：dotnet CpuParse.dll --perf-session <diagnostic-directory> <closed-perf-session-directory>。
using System.Text.Json;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;

static class PerfSessionCpuReport
{
    public static void Run(string diagnosticDirectory, string sessionDirectory)
    {
        string directory = Path.GetFullPath(diagnosticDirectory);
        using var request = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "collection.json")));
        string segmentId = request.RootElement.GetProperty("sampleSegmentId").GetString()
            ?? throw new InvalidDataException("Diagnostic sample segment identity is missing.");
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
            var anchor = root.GetProperty("clock").GetProperty("anchors").EnumerateArray().First();
            double mono = anchor.GetProperty("monotonicMs").GetDouble();
            DateTime utc = DateTimeOffset.FromUnixTimeMilliseconds(anchor.GetProperty("unixMs").GetInt64()).UtcDateTime;
            var marks = File.ReadLines(Path.Combine(sessionDirectory, "events.jsonl"))
                .Select(line => JsonSerializer.Deserialize<JsonElement>(line))
                .Where(e => e.GetProperty("event").GetString() == "segment" && e.GetProperty("payload").GetProperty("id").GetString() == segmentId)
                .OrderBy(e => e.GetProperty("sequence").GetInt64()).ToArray();
            if (marks.Length != 2) throw new InvalidDataException("Diagnostic sample must have one Begin/End pair.");
            Window[] windows = [new(segmentId, "segment.begin", "segment.end",
                utc.AddMilliseconds(marks[0].GetProperty("monotonicMs").GetDouble() - mono),
                utc.AddMilliseconds(marks[1].GetProperty("monotonicMs").GetDouble() - mono))];
            var analysis = new CpuTraceAnalysis(trace, ids[0], windows);
            analysis.ReadEvents();
            var samples = analysis.Samples.Where(s => s.Utc >= windows[0].Start && s.Utc < windows[0].End).ToArray();
            var managed = samples.Where(s => s.Type == nameof(ClrThreadSampleType.Managed)).ToArray();
            var self = CpuTraceAnalysis.Ranked(samples, false);
            var inclusive = CpuTraceAnalysis.Ranked(samples, true);
            var report = new
            {
                session = root.GetProperty("id").GetString(), processId = ids[0],
                sampleMeaning = "dotnet-sampled-thread-time wall-clock managed thread stacks; not on-CPU samples or CPU utilization",
                trace.EventsLost, trace.Truncated, sampleSegmentId = segmentId, windows,
                samples = samples.Length, managedSamples = managed.Length,
                managedSamplesWithResolvedMethods = managed.Count(s => s.Frames.Any(f => !f.Unresolved)),
                self = self.Select(x => new { frame = x.Key, samples = x.Value }),
                inclusive = inclusive.Select(x => new { frame = x.Key, samples = x.Value }),
                threads = samples.GroupBy(s => s.ThreadId).Select(ThreadReport).ToArray(),
                sampleGcAllocationSampling = analysis.SummarizeAllocations(0, 0)
            };
            using (var output = File.Create(Path.Combine(directory, "cpu-hotspots.json")))
                JsonSerializer.Serialize(output, report, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"EventPipe sample: {samples.Length} stacks, {managed.Length} managed, {report.managedSamplesWithResolvedMethods} with resolved methods; events lost {trace.EventsLost}, truncated {trace.Truncated}.");
            Console.WriteLine("Self (leaf) samples:");
            foreach (var row in self.Take(15)) Console.WriteLine($"{row.Value,8}  {row.Key}");
            Console.WriteLine("Inclusive samples:");
            foreach (var row in inclusive.Take(15)) Console.WriteLine($"{row.Value,8}  {row.Key}");
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

    private static object ThreadReport(IGrouping<int, ThreadSample> group) => new
    {
        osThreadId = group.Key, samples = group.Count(),
        topStacks = group.GroupBy(s => string.Join("\n", s.Frames.Select(f => f.Name)))
            .OrderByDescending(g => g.Count()).Take(30).Select(g => new { samples = g.Count(), framesLeafToRoot = g.First().Frames })
    };
}

record Window(string Name, string From, string To, DateTime Start, DateTime End);
