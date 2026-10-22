// EventPipe sampled-thread-time stacks plus real OS CPU counter differences.
// 用法：dotnet CpuParse.dll <diagnostic-round-directory>。
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

var directory = Path.GetFullPath(args.Single());
using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "result.json")));
var root = result.RootElement;
if (root.GetProperty("measurement").GetString() != "diagnostic")
    throw new InvalidDataException("CpuParse requires an explicit diagnostic round; experience has no EventPipe trace.");
var pid = root.GetProperty("gamePid").GetInt32();
var mainId = root.GetProperty("mainOsThreadId").GetInt32();
var processStart = Utc(root.GetProperty("engineStartUtc"));
var anchors = root.GetProperty("phaseObservations").EnumerateArray().Select(x => new Anchor(
    x.GetProperty("name").GetString()!, x.GetProperty("milliseconds").GetDouble(),
    Utc(x.GetProperty("observedUtc")), x.GetProperty("suffix").GetString()!)).ToArray();
if (anchors.Length == 0) throw new InvalidDataException("No phase anchors.");
var offsets = anchors.Select(x => x.ObservedUtc.Ticks - (long)Math.Round(x.Milliseconds * TimeSpan.TicksPerMillisecond)).ToArray();
var origin = new DateTime(offsets.Min(), DateTimeKind.Utc);
DateTime At(Anchor anchor) => origin.AddMilliseconds(anchor.Milliseconds);
var boundaries = anchors.ToDictionary(x => x.Name, StringComparer.Ordinal);
if (!boundaries.TryGetValue("title-ready", out var title)) throw new InvalidDataException("Missing title-ready.");
if (!boundaries.TryGetValue("title-input-ready", out var menuInput)) throw new InvalidDataException("Missing static menu title-input-ready.");
var flow = root.GetProperty("mode").GetString() != "title";
if (flow && (!boundaries.TryGetValue("input-ready", out var input) || !input.Suffix.Contains("mode=Walk")))
    throw new InvalidDataException("Missing first Walk input-ready.");
boundaries.TryGetValue("realtime", out var realtime);
if (flow && realtime is null) throw new InvalidDataException("Missing first Playing realtime after formal entry.");
boundaries.TryGetValue("preload-ready", out var preloadReady);
if (!flow && preloadReady is null) throw new InvalidDataException("Diagnostic title requires real new-game preload-ready.");
long? FrameOf(Anchor a) => Regex.Match(a.Suffix, @"(?:^|\s)frame=(\d+)(?:\s|$)") is { Success: true } match
    ? long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
var logText = File.ReadAllText(Path.Combine(directory, "godot.log"));
var rendererFrames = Regex.Matches(logText,
    @"(?m)^STARTUP_FRAME ([0-9.]+) frame=(\d+) rendererCpuMs=([0-9.Ee+-]+) rendererGpuMs=([0-9.Ee+-]+)([^\r\n]*)")
    .Select(m => new RenderFrame(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
        long.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
        double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
        double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture), m.Groups[5].Value)).ToArray();
var evidence = ReadLogEvidence(logText);
var snapshots = root.GetProperty("cpuSnapshots").EnumerateArray().Select(x => new Snapshot(
    x.GetProperty("name").GetString()!, Utc(x.GetProperty("processCpuReadUtc")),
    Utc(x.GetProperty("startedUtc")), Utc(x.GetProperty("finishedUtc")), x.GetProperty("processCpuSeconds").GetDouble(),
    x.GetProperty("threads").EnumerateArray().Select(t => new ThreadCpu(t.GetProperty("osThreadId").GetInt32(),
        Utc(t.GetProperty("threadStartUtc")), t.GetProperty("cpuSeconds").GetDouble(), Utc(t.GetProperty("readUtc")))).ToArray(),
    x.GetProperty("unreadableThreads").GetArrayLength())).ToDictionary(x => x.Name, StringComparer.Ordinal);

var traceIndexPath = Path.Combine(directory, "threads.etlx");
try
{
    // TraceEvent associates captured managed stacks and CLR rundown names; no custom stack unwinder.
    TraceLog.CreateFromEventPipeDataFile(Path.Combine(directory, "threads.nettrace"), traceIndexPath);
    using var trace = new TraceLog(traceIndexPath);
    var samples = new List<ThreadSample>();
    var clrThreadIds = new Dictionary<int, HashSet<long>>();
    var frameNames = new Dictionary<CallStackIndex, Frame[]>();
    foreach (var data in trace.Events)
    {
        if (data.ProcessID != pid) continue;
        if (data is ThreadCreatedTraceData thread)
        {
            if (!clrThreadIds.TryGetValue(thread.OSThreadID, out var ids))
                clrThreadIds[thread.OSThreadID] = ids = new HashSet<long>();
            ids.Add(thread.ManagedThreadID);
        }
        if (data is not ClrThreadSampleTraceData sample) continue;
        var stack = trace.GetCallStackForEvent(data);
        Frame[] frames;
        if (stack is null) frames = Array.Empty<Frame>();
        else if (!frameNames.TryGetValue(stack.CallStackIndex, out frames!))
        {
            var list = new List<Frame>();
            for (var cursor = stack; cursor is not null; cursor = cursor.Caller) list.Add(Describe(cursor.CodeAddress));
            frames = list.ToArray();
            frameNames.Add(stack.CallStackIndex, frames);
        }
        samples.Add(new(data.TimeStamp.ToUniversalTime(), data.ThreadID, sample.Type.ToString(), stack is not null, frames));
    }
    var windows = new List<Window> {
        new("process-to-static-menu-input-ready", "process-start", "title-input-ready", processStart, At(menuInput)) };
    if (preloadReady is not null)
    {
        windows.Add(new("process-to-new-game-preload-ready", "process-start", "preload-ready", processStart, At(preloadReady)));
        if (preloadReady.Milliseconds >= menuInput.Milliseconds)
            windows.Add(new("static-menu-input-ready-to-new-game-preload-ready", "title-input-ready", "preload-ready", At(menuInput), At(preloadReady)));
    }
    if (realtime is not null)
        windows.Add(new("process-to-Playing-realtime", "process-start", "realtime", processStart, At(realtime)));
    var previous = processStart;
    var previousName = "process-start";
    foreach (var anchor in anchors.OrderBy(x => x.Milliseconds))
    {
        windows.Add(new(previousName + " -> " + anchor.Name, previousName, anchor.Name, previous, At(anchor)));
        previous = At(anchor); previousName = anchor.Name;
    }
    if (flow)
    {
        var enter = boundaries["new-game-enter"];
        var ready = boundaries["input-ready"];
        windows.Add(new("normal-new-game-enter-to-input-ready-Walk", "new-game-enter", "input-ready", At(enter), At(ready)));
        windows.Add(new("menu-ready-to-new-game-enter", "title-ready", "new-game-enter", At(title), At(enter)));
        if (preloadReady is not null && enter.Milliseconds < preloadReady.Milliseconds)
            windows.Add(new("new-game-enter-to-new-game-preload-ready", "new-game-enter", "preload-ready", At(enter), At(preloadReady)));
    }
    if (boundaries.ContainsKey("playing-idle-start"))
    {
        foreach (var (name, from, to) in new[] {
            ("playing-idle-10s", "playing-idle-start", "playing-idle-end"),
            ("playing-forward-20s-request", "playing-forward-start", "playing-forward-end"),
            ("playing-action-release", "playing-forward-end", "playing-release-observed") })
            if (boundaries.TryGetValue(from, out var begin) && boundaries.TryGetValue(to, out var end))
                windows.Add(new(name, from, to, At(begin), At(end)));
    }
    var finalBoundary = flow ? At(boundaries["input-ready"]) : At(preloadReady!);
    if (At(menuInput) > finalBoundary) finalBoundary = At(menuInput);
    if (realtime is not null && At(realtime) > finalBoundary) finalBoundary = At(realtime);
    var lastObservedBoundary = anchors.Select(At).Max();
    if (lastObservedBoundary > finalBoundary) finalBoundary = lastObservedBoundary;
    var coverageStart = trace.SessionStartTime.ToUniversalTime();
    var coverageEnd = trace.SessionEndTime.ToUniversalTime();
    var report = new
    {
        pid, mode = root.GetProperty("mode").GetString(), mainOsThreadId = mainId,
        measurement = "diagnostic; not an experience-target pass",
        captureEndpoint = root.GetProperty("captureEndpoint").GetString(),
        launcher = root.GetProperty("launcher").Clone(),
        preparation = root.GetProperty("preparation").Clone(),
        cacheCondition = root.GetProperty("condition").Clone(),
        observedTiming = root.GetProperty("timing").Clone(),
        budgetObservations = root.GetProperty("budgetObservations").Clone(),
        mainThreadIdentification = root.GetProperty("mainThreadIdentification").GetString(),
        threadIdJoin = "ProcessThread.Id == EventPipe sample header ThreadID == CLR OSThreadID; never join CLR ManagedThreadID. ThreadStartUtc guards counter differences against OS TID reuse.",
        clrOsThreadIdCrossCheck = clrThreadIds.OrderBy(x => x.Key).Select(x => new {
            osThreadId = x.Key, managedThreadIdsInformationalOnly = x.Value.OrderBy(x => x).ToArray(),
            presentInOsSnapshots = snapshots.Values.Any(s => s.Threads.Any(t => t.Id == x.Key)),
            presentInThreadSamples = samples.Any(s => s.ThreadId == x.Key) }).ToArray(),
        clrOsThreadIdCrossCheckStatus = clrThreadIds.Count == 0 ? "NOT_OBSERVED; no captured CLR thread/rundown mapping, OS header IDs still used" : "CAPTURED_OSThreadID_MAPPINGS_ONLY; absent mappings are not negative evidence",
        sampleKind = "sampled-thread-time; Windows all runtime-managed threads, including waiting threads; NOT on-CPU",
        cpuFunctionShare = "NOT_MEASURED",
        caveat = root.GetProperty("caveat").GetString(),
        input = root.GetProperty("input").Clone(),
        playingSample = root.TryGetProperty("playingSample", out var playing) ? playing.Clone() : (JsonElement?)null,
        logEvidence = new {
            observations = evidence, rawLog = "godot.log",
            exceptionLines = evidence.Count(x => x.Kind == "exception-or-error"),
            explicitBakeOrCacheLines = evidence.Count(x => x.Kind == "explicit-bake-or-cache"),
            bakeStatus = "UNKNOWN without explicit cache-hit/miss/build observations; sampled bake stacks below are candidates, not cache-miss counts",
            attribution = "Frame-number association and line-order brackets only. Unstamped exceptions are not assigned an invented timestamp. Absence of an error or cache line is not proof of no stalls or no baking."
        },
        playingCpuIntervals = PlayingCpuIntervals(root),
        condition = new {
            menuReadyMilliseconds = title.Milliseconds,
            staticMenuInputReadyMilliseconds = menuInput.Milliseconds,
            newGamePreloadReadyMilliseconds = preloadReady?.Milliseconds,
            playingRealtimeMilliseconds = realtime?.Milliseconds,
            newGameEnterMilliseconds = flow ? boundaries["new-game-enter"].Milliseconds : (double?)null,
            actualMenuDwellMilliseconds = flow ? boundaries["new-game-enter"].Milliseconds - title.Milliseconds : (double?)null,
            enteredBeforeNewGamePreloadReady = flow && preloadReady is not null ? boundaries["new-game-enter"].Milliseconds < preloadReady.Milliseconds : (bool?)null,
            titleIsStaticPage = true },
        clock = new { originUtc = origin, nominalPollingMilliseconds = 20,
            method = "minimum observedUtc minus STARTUP_PHASE milliseconds; CPU counters retain their actual read timestamps, without calibration or interpolation",
            anchorOffsetSpreadMilliseconds = (offsets.Max() - offsets.Min()) / (double)TimeSpan.TicksPerMillisecond,
            limit = "Polling, log flush and scheduling delay can shift phase edges. Counter enumeration is not atomic; read timestamps and duration are supplied." },
        coverage = new { traceSessionStartUtc = coverageStart, traceSessionEndUtc = coverageEnd, trace.EventsLost, trace.Truncated,
            attachRequestedUtc = Utc(root.GetProperty("attachRequestedUtc")),
            firstTargetThreadSampleUtc = samples.Count == 0 ? (DateTime?)null : samples.Min(x => x.Utc),
            lastTargetThreadSampleUtc = samples.Count == 0 ? (DateTime?)null : samples.Max(x => x.Utc),
            sampledThreadTimeEventsInWholeTrace = samples.Count,
            sampledThreadTimeEventsAfterFinalBoundary = samples.Count(x => x.Utc >= finalBoundary),
            processStartBeforeTraceSession = processStart < coverageStart,
            untracedInitialMilliseconds = Math.Max(0, (coverageStart - processStart).TotalMilliseconds),
            initialGap = "Before session start NOT_MEASURED; first sample is also reported. OS process CPU lifetime still includes startup before attach.",
            nativeOnlyAndDeadThreads = "EventPipe is not a complete native-thread census; OS snapshots only retain readable live thread counters." },
        limitations = new[] {
            "Process.TotalProcessorTime is the authoritative CPU total (user+kernel CPU seconds). It may exceed wall seconds on multiple cores.",
            "Main CPU is the primary OS thread counter. Process-minus-main includes every other thread, including native or already exited threads; it is not a sum of sampled/live workers.",
            "Per-thread differences require readable counters at both endpoints with the same OS TID and creation time. Missing, new, dead or reused threads remain null; their sum is not a complete process total.",
            "Function self/inclusive counts and per-thread stacks are sampled-thread-time candidates, including waiting time. Never multiply their shares by process or thread CPU seconds.",
            "Native, truncated and unresolved stacks stay unresolved; no native PDB downloads. No stack or no samples means NOT_MEASURED, never zero CPU.",
            "Inclusive frame counts overlap. Phase windows exclude extra collector idle/rundown, but counter snapshots occur after observed markers, so their read delay is reported separately.",
            "EventsLost or Truncated means incomplete captured events; no synthetic backfill or attribution correction is applied." },
        phases = windows.Select(Summarize).ToArray()
    };
    File.WriteAllText(Path.Combine(directory, "cpu-hotspots.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

    object Summarize(Window window)
    {
        var selected = samples.Where(x => x.Utc >= window.Start && x.Utc < window.End).ToArray();
        var cpu = CpuCounters(window);
        return new {
            name = window.Name, requestedStartUtc = window.Start, requestedEndUtc = window.End,
            markerElapsedMilliseconds = (window.End - window.Start).TotalMilliseconds,
            startsBeforeTraceSession = window.Start < coverageStart, endsAfterTraceSession = window.End > coverageEnd,
            actualCpuCounters = cpu,
            frameStatistics = Frames(window),
            sampledThreadTimeEvents = selected.Length, cpuFunctionShare = "NOT_MEASURED",
            samplesMissingCallStack = selected.Count(x => !x.HasStack),
            samplesWithUnresolvedFrame = selected.Count(x => x.Frames.Any(f => f.Unresolved)),
            sampleTypes = selected.GroupBy(x => x.Type).ToDictionary(g => g.Key, g => g.Count()),
            sampledThreadTimeSelf = Top(selected, false), sampledThreadTimeInclusive = Top(selected, true),
            perThreadSampledThreadTime = selected.GroupBy(x => x.ThreadId).OrderByDescending(g => g.Count()).Select(g => new {
                osThreadId = g.Key, role = g.Key == mainId ? "main" : "worker-or-other",
                sampledThreadTimeEvents = g.Count(), samplesMissingCallStack = g.Count(x => !x.HasStack),
                cpuFunctionShare = "NOT_MEASURED", sampledThreadTimeSelf = Top(g.ToArray(), false),
                sampledThreadTimeInclusive = Top(g.ToArray(), true) }).ToArray(),
            status = selected.Length == 0 ? "NO_CAPTURED_THREAD_SAMPLES; CPU counters are separate" :
                trace.EventsLost > 0 || trace.Truncated ? "CAPTURED_SAMPLED_THREAD_TIME_ONLY; trace incomplete" : "CAPTURED_SAMPLED_THREAD_TIME; NOT_CPU_ATTRIBUTION"
        };
    }
    object Frames(Window window)
    {
        long? startFrame = window.From == "process-start" ? 0 : FrameOf(boundaries[window.From]);
        var endFrame = FrameOf(boundaries[window.To]);
        long? count = startFrame is not null && endFrame is not null && endFrame >= startFrame ? endFrame-startFrame : null;
        var fromMs = window.From == "process-start" ? double.NegativeInfinity : boundaries[window.From].Milliseconds;
        var toMs = boundaries[window.To].Milliseconds;
        // Include the shared boundary observation so adjacent phase intervals meet without a fabricated gap.
        var frames = rendererFrames.Where(x => x.Milliseconds >= fromMs && x.Milliseconds <= toMs).ToArray();
        // Consecutive engine observations, never marker wall-time divided by a frame count.
        var pairs = frames.Zip(frames.Skip(1), (a, b) => new {
            fromFrame = a.Frame, toFrame = b.Frame, fromMilliseconds = a.Milliseconds,
            toMilliseconds = b.Milliseconds, frameDelta = b.Frame - a.Frame,
            intervalMilliseconds = b.Milliseconds - a.Milliseconds, end = b }).ToArray();
        var valid = pairs.Where(x => x.frameDelta == 1 && x.intervalMilliseconds > 0 &&
            double.IsFinite(x.intervalMilliseconds)).ToArray();
        var spikes = valid.Where(x => x.intervalMilliseconds > 33).Select(x => new {
            x.fromFrame, x.toFrame, x.fromMilliseconds, x.toMilliseconds, x.intervalMilliseconds,
            rendererCpuAtEndMilliseconds = x.end.Cpu, rendererGpuAtEndMilliseconds = x.end.Gpu,
            engineObservationSuffix = x.end.Details,
            logEvidenceCandidates = evidence.Where(e => e.Frame is { } f ? f >= x.fromFrame && f <= x.toFrame :
                e.PreviousFrame is { } before && e.NextFrame is { } after && before >= x.fromFrame && after <= x.toFrame).ToArray(),
            clockAlignment = "EventPipe UTC inferred from observed engine anchors; nominal 20ms polling plus flush/scheduling uncertainty",
            nearbyMainThreadSampledTimeCandidates = Top(samples.Where(s => s.ThreadId == mainId &&
                s.Utc >= origin.AddMilliseconds(x.fromMilliseconds) && s.Utc < origin.AddMilliseconds(x.toMilliseconds)).ToArray(), true),
            nearbyBakeBuildSampledTimeCandidates = Top(samples.Where(s =>
                s.Utc >= origin.AddMilliseconds(x.fromMilliseconds) && s.Utc < origin.AddMilliseconds(x.toMilliseconds) &&
                s.Frames.Any(f => f.Name.Contains("Campus.Kernel.Bake.BakeProductBuilder.Build", StringComparison.Ordinal) ||
                    f.Name.Contains("Campus.Kernel.Bake.BakeGeometryBuilder.Build", StringComparison.Ordinal))).ToArray(), true),
            attribution = "COINCIDENCE_ONLY; renderer timings may describe the preceding rendered frame; sampled-thread-time includes waits; no function CPU share"
        }).ToArray();
        return new {
            status = count is null ? "FRAME_COUNT_NOT_MEASURED; marker frame field absent" : "ENGINE_PROCESS_FRAME_COUNTER_DIFFERENCE",
            startFrame, endFrame, processFrameCount=count,
            meanEngineObservationIntervalMilliseconds=count is > 0 ? (window.End-window.Start).TotalMilliseconds/count : (double?)null,
            experiencePacingStatus="NOT_MEASURED; private-desktop cadence does not establish presented FPS or experience stutter; inspect measured renderer CPU/GPU work",
            rendererStatus=frames.Length==0 ? "NOT_MEASURED; STARTUP_FRAME telemetry absent in window" : "FRAME_STATISTICS_VIEWPORT_RENDER_MEASUREMENTS",
            rendererSamples=frames.Length,
            rendererCpu=Distribution(frames.Select(x=>x.Cpu).ToArray()), rendererGpu=Distribution(frames.Select(x=>x.Gpu).ToArray()),
            movement = Movement(frames),
            processIntervals = new {
                status = valid.Length == 0 ? "NOT_MEASURED; no consecutive positive finite STARTUP_FRAME intervals" :
                    valid.Length == pairs.Length ? "CONTIGUOUS_ENGINE_PROCESS_OBSERVATIONS; DIAGNOSTIC_ONLY" : "PARTIAL_CONTIGUOUS_OBSERVATIONS; gaps excluded, never divided or backfilled",
                distribution = Distribution(valid.Select(x => x.intervalMilliseconds).ToArray()),
                rejectedPairs = pairs.Where(x => x.frameDelta != 1 || x.intervalMilliseconds <= 0 || !double.IsFinite(x.intervalMilliseconds))
                    .Select(x => new { x.fromFrame, x.toFrame, x.frameDelta, x.intervalMilliseconds }).ToArray(),
                spikesAbove33Milliseconds = spikes,
                caveat = "Main-loop observation intervals, not presented frames or GPU completion latency. Both boundary observations are included; adjacent windows share one observation and no interval. Per-frame logging/EventPipe/CPU enumeration overhead is included, not subtracted. Raw godot.log is authoritative."
            },
            limit="Engine process frames are not presented-frame counts. Viewport renderer CPU/GPU samples describe render work (typically previous rendered frame); they are not process/main/worker OS CPU. Window assignment follows observation time. Render CPU and GPU overlap; never add them to wall time or OS CPU." };
    }
    object CpuCounters(Window window)
    {
        var lifetime = window.From == "process-start";
        if (!snapshots.TryGetValue(window.To, out var end) || (!lifetime && !snapshots.ContainsKey(window.From)))
            return new { status = "NOT_MEASURED; missing CPU snapshot" };
        Snapshot? start = lifetime ? null : snapshots[window.From];
        double? processCpu = lifetime ? end.ProcessCpuSeconds : Delta(start!.ProcessCpuSeconds, end.ProcessCpuSeconds);
        var mainEnd = end.Threads.SingleOrDefault(t => t.Id == mainId);
        var mainStart = start?.Threads.SingleOrDefault(t => t.Id == mainId);
        double? mainCpu = lifetime ? mainEnd?.Seconds : ThreadDelta(mainStart, mainEnd);
        double? othersCpu = processCpu is not null && mainCpu is not null ? Delta(mainCpu.Value, processCpu.Value) : null;
        var ids = (start?.Threads.Select(t => t.Id) ?? Array.Empty<int>()).Concat(end.Threads.Select(t => t.Id))
            .Distinct().OrderBy(x => x).ToArray();
        var threads = ids.Select(id => {
            var a = start?.Threads.SingleOrDefault(t => t.Id == id);
            var b = end.Threads.SingleOrDefault(t => t.Id == id);
            var seconds = lifetime ? b?.Seconds : ThreadDelta(a, b);
            return new { osThreadId = id, role = id == mainId ? "main" : "worker-or-other", cpuSeconds = seconds,
                startCounterSeconds = a?.Seconds, endCounterSeconds = b?.Seconds,
                startReadUtc = a?.ReadUtc, endReadUtc = b?.ReadUtc,
                startThreadCreationUtc = a?.StartUtc, endThreadCreationUtc = b?.StartUtc,
                status = seconds is not null ? (lifetime ? "LIVE_THREAD_LIFETIME_COUNTER; dead threads absent" : "MATCHED_LIVE_THREAD_COUNTER_DIFFERENCE") :
                    "NOT_MEASURED; absent/unreadable endpoint, reused TID or counter regression" };
        }).ToArray();
        return new {
            status = "OS_CPU_COUNTERS; actual read boundaries below, approximate marker alignment",
            definition = lifetime ? "Process/main lifetime CPU at end snapshot, not a fabricated process-start snapshot; per-thread lifetime covers only threads readable at end" :
                "End minus start OS cumulative counters; no interpolation to marker UTC",
            processCpuSeconds = processCpu, mainThreadCpuSeconds = mainCpu,
            workerOrOtherCpuSecondsProcessMinusMain = othersCpu,
            startProcessReadUtc = start?.ReadUtc ?? processStart, endProcessReadUtc = end.ReadUtc,
            startReadDelayFromMarkerMilliseconds = start is null ? (double?)null : (start.ReadUtc - window.Start).TotalMilliseconds,
            endReadDelayFromMarkerMilliseconds = (end.ReadUtc - window.End).TotalMilliseconds,
            startSnapshotDurationMilliseconds = start is null ? (double?)null : (start.FinishedUtc - start.StartedUtc).TotalMilliseconds,
            endSnapshotDurationMilliseconds = (end.FinishedUtc - end.StartedUtc).TotalMilliseconds,
            unreadableThreadsAtStart = start?.Unreadable, unreadableThreadsAtEnd = end.Unreadable,
            observedThreadCpuSecondsSumIncomplete = threads.Where(x => x.cpuSeconds.HasValue).Sum(x => x.cpuSeconds!.Value),
            observedThreadCpuSecondsSumIsProcessTotal = false,
            perThread = threads
        };
    }
}
finally { if (File.Exists(traceIndexPath)) File.Delete(traceIndexPath); }
static object[] Top(ThreadSample[] samples, bool inclusive)
{
    var counts = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var sample in samples)
    {
        var names = inclusive ? sample.Frames.Select(f => f.Name).Distinct(StringComparer.Ordinal) : sample.Frames.Take(1).Select(f => f.Name);
        foreach (var name in names) counts[name] = counts.GetValueOrDefault(name) + 1;
    }
    return counts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal).Take(25)
        .Select(x => (object)new { frame = x.Key, sampledThreadTimeEvents = x.Value, cpuFunctionShare = "NOT_MEASURED" }).ToArray();
}
static object Distribution(double[] input)
{
    var values = input.Where(double.IsFinite).OrderBy(x => x).ToArray();
    // Nearest rank retains measured values; no interpolation or estimated missing frames.
    double? Percentile(double fraction) => values.Length == 0 ? null : values[Math.Max(0, (int)Math.Ceiling(values.Length * fraction) - 1)];
    return new {
        count = values.Length, rejectedNonFinite = input.Length - values.Length,
        percentileMethod = "nearest-rank over finite measured observations",
        p50Milliseconds = Percentile(.50), p95Milliseconds = Percentile(.95), p99Milliseconds = Percentile(.99),
        meanMilliseconds = values.Length == 0 ? (double?)null : values.Average(),
        maxMilliseconds = values.Length == 0 ? (double?)null : values[^1],
        sumMilliseconds = values.Length == 0 ? (double?)null : values.Sum(),
        above33Milliseconds = values.Count(x => x > 33), above100Milliseconds = values.Count(x => x > 100)
    };
}
static object Movement(RenderFrame[] frames)
{
    var observations = frames.Select(frame => {
        var fields = Regex.Matches(frame.Details, @"(\w+)=([^ ]+)")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value, StringComparer.Ordinal);
        double? Number(string key) => fields.TryGetValue(key, out var text) &&
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number) ? number : null;
        if (fields.GetValueOrDefault("flow") != "Playing" || fields.GetValueOrDefault("mode") != "Walk" ||
            Number("feetX") is not { } x || Number("feetY") is not { } y || Number("feetZ") is not { } z ||
            Number("yawDeg") is not { } yaw || !bool.TryParse(fields.GetValueOrDefault("forwardPressed"), out var pressed)) return null;
        return new MovementFrame(frame.Frame, frame.Milliseconds, x, y, z, yaw, pressed,
            bool.TryParse(fields.GetValueOrDefault("cursorVisible"), out var visible) ? visible : null,
            bool.TryParse(fields.GetValueOrDefault("cursorHeld"), out var held) ? held : null);
    }).Where(x => x is not null).Select(x => x!).ToArray();
    if (observations.Length < 2) return new { status = "NOT_MEASURED; fewer than two complete Playing Walk pose/action observations", completeObservations = observations.Length };
    var first = observations[0]; var last = observations[^1];
    var pairs = observations.Zip(observations.Skip(1), (a, b) => new {
        a, b, consecutive = b.Frame == a.Frame + 1, dx = b.X - a.X, dz = b.Z - a.Z }).ToArray();
    var pressed = pairs.Where(p => p.consecutive && p.a.Pressed && p.b.Pressed).ToArray();
    var direction = first.Yaw * Math.PI / 180;
    var displacementX = last.X - first.X; var displacementZ = last.Z - first.Z;
    var forward = displacementX * Math.Sin(direction) - displacementZ * Math.Cos(direction);
    var positiveForward = pressed.Count(p => p.dx * Math.Sin(p.a.Yaw * Math.PI / 180) - p.dz * Math.Cos(p.a.Yaw * Math.PI / 180) > 0);
    return new {
        status = pressed.Length == 0 ? "NO_CONTIGUOUS_PRESSED_POSE_PAIRS; forward input not confirmed" :
            positiveForward > 0 ? "PRESSED_ACTION_AND_POSITIVE_FORWARD_DISPLACEMENT_OBSERVED; route completion or unobstructed movement not established" :
            "PRESSED_ACTION_WITHOUT_POSITIVE_FORWARD_DISPLACEMENT; blocked, consumed input or another cause remains unknown",
        completeObservations = observations.Length, rejectedOrNonPlayingObservations = frames.Length - observations.Length,
        first, last, pressedObservations = observations.Count(x => x.Pressed),
        cursorVisibleObservations = observations.Count(x => x.CursorVisible == true),
        cursorHeldObservations = observations.Count(x => x.CursorHeld == true),
        cursorStateMissingObservations = observations.Count(x => x.CursorVisible is null || x.CursorHeld is null),
        consecutivePressedPairs = pressed.Length, positiveForwardPressedPairs = positiveForward,
        exactlyStationaryPressedPairs = pressed.Count(p => p.dx == 0 && p.dz == 0),
        horizontalDisplacementMeters = Math.Sqrt(displacementX * displacementX + displacementZ * displacementZ),
        forwardProjectionFromFirstYawMeters = forward,
        observedHorizontalPathMeters = pairs.Where(p => p.consecutive).Sum(p => Math.Sqrt(p.dx * p.dx + p.dz * p.dz)),
        limit = "Read-only IObserverState.Feet plus native action state; no coordinate edits, dead reckoning or expected-speed substitution. Stationary pressed frames are retained; collision is not inferred from zero displacement."
    };
}
static LogEvidence[] ReadLogEvidence(string text)
{
    var lines = text.Split('\n');
    var before = new long?[lines.Length]; var after = new long?[lines.Length];
    long? previous = null;
    for (var i = 0; i < lines.Length; i++)
    {
        var match = Regex.Match(lines[i], @"^STARTUP_(?:FRAME|PHASE) .*?\bframe=(\d+)");
        if (match.Success) previous = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        before[i] = previous;
    }
    long? next = null;
    for (var i = lines.Length - 1; i >= 0; i--)
    {
        var match = Regex.Match(lines[i], @"^STARTUP_(?:FRAME|PHASE) .*?\bframe=(\d+)");
        if (match.Success) next = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        after[i] = next;
    }
    var result = new List<LogEvidence>();
    for (var i = 0; i < lines.Length; i++)
    {
        var line = lines[i].TrimEnd('\r');
        var kind = line.Contains("[phase=render-instances]", StringComparison.Ordinal) ? "render-host" :
            Regex.IsMatch(line, @"^(?:(?:SCRIPT )?ERROR:|Unhandled exception|(?: ---> )?System\.[\w.]+Exception:)") ? "exception-or-error" :
            Regex.IsMatch(line, @"\b(?:cache[ -](?:hit|miss)|Strict cache miss)\b|\[phase=bake", RegexOptions.IgnoreCase) ? "explicit-bake-or-cache" : null;
        if (kind is null) continue;
        var match = Regex.Match(line, @"\bframe=(\d+)");
        result.Add(new(kind, i + 1, line, match.Success ? long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null, before[i], after[i]));
    }
    return result.ToArray();
}
static object PlayingCpuIntervals(JsonElement root)
{
    if (!root.TryGetProperty("playingSample", out var playing) || !playing.TryGetProperty("processCpuReads", out var values))
        return new { status = "NOT_MEASURED; no Playing OS process CPU reads" };
    var reads = values.EnumerateArray().Select(x => new { utc = Utc(x.GetProperty("readUtc")),
        cpu = x.GetProperty("processCpuSeconds").GetDouble(), observerMilliseconds = x.GetProperty("observerMilliseconds").GetDouble() }).ToArray();
    return new {
        status = reads.Length < 2 ? "NOT_MEASURED; fewer than two CPU reads" : "OS_PROCESS_CPU_COUNTER_DIFFERENCES; nominal 1s resolution, not per-frame CPU",
        reads,
        intervals = reads.Zip(reads.Skip(1), (a, b) => new {
            startUtc = a.utc, endUtc = b.utc, wallSeconds = (b.utc - a.utc).TotalSeconds,
            cpuSeconds = Delta(a.cpu, b.cpu),
            averageOccupiedCores = b.utc > a.utc && b.cpu >= a.cpu ? (b.cpu - a.cpu) / (b.utc - a.utc).TotalSeconds : (double?)null
        }).ToArray(),
        limit = "Actual whole-process CPU, including all threads. Coarse intervals can contain several frame spikes; no per-frame CPU attribution or sampled-stack share conversion. Observer read cost is reported, never subtracted."
    };
}
static DateTime Utc(JsonElement element) => DateTime.Parse(element.GetString()!, CultureInfo.InvariantCulture,
    DateTimeStyles.RoundtripKind).ToUniversalTime();
static double? Delta(double start, double end) => end >= start ? end - start : null;
static double? ThreadDelta(ThreadCpu? start, ThreadCpu? end) => start is not null && end is not null && start.StartUtc == end.StartUtc
    ? Delta(start.Seconds, end.Seconds) : null;
static Frame Describe(TraceCodeAddress address)
{
    var module = string.IsNullOrEmpty(address.ModuleName) ? "unknown-module" : address.ModuleName;
    var method = address.FullMethodName;
    var unresolved = address.Method is null || string.IsNullOrEmpty(method);
    return new(unresolved ? $"{module}!0x{address.Address:x} [native-or-unresolved]" : $"{module}!{method}", unresolved);
}
record Anchor(string Name, double Milliseconds, DateTime ObservedUtc, string Suffix);
record Window(string Name, string From, string To, DateTime Start, DateTime End);
record Frame(string Name, bool Unresolved);
record ThreadSample(DateTime Utc, int ThreadId, string Type, bool HasStack, Frame[] Frames);
record ThreadCpu(int Id, DateTime StartUtc, double Seconds, DateTime ReadUtc);
record Snapshot(string Name, DateTime ReadUtc, DateTime StartedUtc, DateTime FinishedUtc, double ProcessCpuSeconds, ThreadCpu[] Threads, int Unreadable);
record RenderFrame(double Milliseconds, long Frame, double Cpu, double Gpu, string Details);
record MovementFrame(long Frame, double Milliseconds, double X, double Y, double Z, double Yaw, bool Pressed, bool? CursorVisible, bool? CursorHeld);
record LogEvidence(string Kind, int Line, string Text, long? Frame, long? PreviousFrame, long? NextFrame);

