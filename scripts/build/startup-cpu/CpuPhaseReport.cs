// 读取轮次证据、构造阶段时窗，产出实际 OS CPU、阶段与总诊断报告。
// 用法：由 CpuParse 创建并调用 CreateReport；报告借用的 JsonDocument 须活到序列化完成。
using System.Globalization;
using System.Text.Json;
using static CpuTraceAnalysis;

sealed class CpuPhaseReport
{
    private readonly JsonElement root;
    private readonly int pid;
    private readonly int mainId;
    private readonly DateTime processStart;
    private readonly Anchor[] anchors;
    private readonly long[] offsets;
    private readonly DateTime origin;
    private readonly Dictionary<string, Anchor> boundaries;
    private readonly Anchor title;
    private readonly Anchor menuInput;
    private readonly bool flow;
    private readonly Anchor? realtime;
    private readonly Anchor? preloadReady;
    private readonly Dictionary<string, Snapshot> snapshots;
    private readonly List<Window> windows;
    private readonly PlayingObservations playing;

    public int Pid => pid;
    public IReadOnlyList<Window> Windows => windows;

    public CpuPhaseReport(JsonElement root, string directory)
    {
        this.root = root;
        if (root.GetProperty("measurement").GetString() != "diagnostic")
            throw new InvalidDataException("CpuParse requires an explicit diagnostic round; experience has no EventPipe trace.");
        pid = root.GetProperty("gamePid").GetInt32();
        mainId = root.GetProperty("mainOsThreadId").GetInt32();
        processStart = Utc(root.GetProperty("engineStartUtc"));
        anchors = root.GetProperty("phaseObservations").EnumerateArray().Select(x => new Anchor(
            x.GetProperty("name").GetString()!, x.GetProperty("milliseconds").GetDouble(),
            Utc(x.GetProperty("observedUtc")), x.GetProperty("suffix").GetString()!)).ToArray();
        if (anchors.Length == 0) throw new InvalidDataException("No phase anchors.");
        offsets = anchors.Select(x => x.ObservedUtc.Ticks - (long)Math.Round(x.Milliseconds * TimeSpan.TicksPerMillisecond)).ToArray();
        origin = new DateTime(offsets.Min(), DateTimeKind.Utc);
        boundaries = anchors.ToDictionary(x => x.Name, StringComparer.Ordinal);
        if (!boundaries.TryGetValue("title-ready", out title!)) throw new InvalidDataException("Missing title-ready.");
        if (!boundaries.TryGetValue("title-input-ready", out menuInput!)) throw new InvalidDataException("Missing static menu title-input-ready.");
        flow = root.GetProperty("mode").GetString() != "title";
        if (flow && (!boundaries.TryGetValue("input-ready", out var input) || !input.Suffix.Contains("mode=Walk")))
            throw new InvalidDataException("Missing first Walk input-ready.");
        boundaries.TryGetValue("realtime", out realtime);
        if (flow && realtime is null) throw new InvalidDataException("Missing first Playing realtime after formal entry.");
        boundaries.TryGetValue("preload-ready", out preloadReady);
        if (!flow && preloadReady is null) throw new InvalidDataException("Diagnostic title requires real new-game preload-ready.");
        playing = new PlayingObservations(File.ReadAllText(Path.Combine(directory, "godot.log")), root.GetProperty("performance"));
        snapshots = root.GetProperty("cpuSnapshots").EnumerateArray().Select(x => new Snapshot(
            x.GetProperty("name").GetString()!, Utc(x.GetProperty("processCpuReadUtc")),
            Utc(x.GetProperty("startedUtc")), Utc(x.GetProperty("finishedUtc")), x.GetProperty("processCpuSeconds").GetDouble(),
            x.GetProperty("threads").EnumerateArray().Select(t => new ThreadCpu(t.GetProperty("osThreadId").GetInt32(),
                Utc(t.GetProperty("threadStartUtc")), t.GetProperty("cpuSeconds").GetDouble(), Utc(t.GetProperty("readUtc")))).ToArray(),
            x.GetProperty("unreadableThreads").GetArrayLength())).ToDictionary(x => x.Name, StringComparer.Ordinal);
        windows = BuildWindows();
    }

    private DateTime At(Anchor anchor) => origin.AddMilliseconds(anchor.Milliseconds);

    private List<Window> BuildWindows()
    {
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
        return windows;
    }

    public object CreateReport(CpuTraceAnalysis analysis)
    {
        var finalBoundary = FinalBoundary();
        var samples = analysis.Samples;
        var clrThreadIds = analysis.ClrThreadIds;
        Console.WriteLine("CpuParse: preparing phase summaries");
        return new
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
            gcAllocationSampling = analysis.CreateAllocationReport(mainId),
            caveat = root.GetProperty("caveat").GetString(),
            input = root.GetProperty("input").Clone(),
            playingSample = root.TryGetProperty("playingSample", out var playingSample) ? playingSample.Clone() : (JsonElement?)null,
            logEvidence = playing.LogReport(),
            performanceSession = playing.SessionReport(),
            playingCpuIntervals = PlayingCpuIntervals(root),
            condition = ConditionReport(),
            clock = ClockReport(),
            coverage = CoverageReport(analysis, finalBoundary),
            limitations = Limitations(),
            phases = windows.Select(window => Summarize(window, analysis)).ToArray()
        };
    }

    private object ConditionReport() => new
    {
        menuReadyMilliseconds = title.Milliseconds,
        staticMenuInputReadyMilliseconds = menuInput.Milliseconds,
        newGamePreloadReadyMilliseconds = preloadReady?.Milliseconds,
        playingRealtimeMilliseconds = realtime?.Milliseconds,
        newGameEnterMilliseconds = flow ? boundaries["new-game-enter"].Milliseconds : (double?)null,
        actualMenuDwellMilliseconds = flow ? boundaries["new-game-enter"].Milliseconds - title.Milliseconds : (double?)null,
        enteredBeforeNewGamePreloadReady = flow && preloadReady is not null ? boundaries["new-game-enter"].Milliseconds < preloadReady.Milliseconds : (bool?)null,
        titleIsStaticPage = true
    };

    private object ClockReport() => new
    {
        originUtc = origin, nominalPollingMilliseconds = 20,
        method = "minimum observedUtc minus STARTUP_PHASE milliseconds; CPU counters retain their actual read timestamps, without calibration or interpolation",
        anchorOffsetSpreadMilliseconds = (offsets.Max() - offsets.Min()) / (double)TimeSpan.TicksPerMillisecond,
        limit = "Polling, log flush and scheduling delay can shift phase edges. Counter enumeration is not atomic; read timestamps and duration are supplied."
    };

    private DateTime FinalBoundary()
    {
        var finalBoundary = flow ? At(boundaries["input-ready"]) : At(preloadReady!);
        if (At(menuInput) > finalBoundary) finalBoundary = At(menuInput);
        if (realtime is not null && At(realtime) > finalBoundary) finalBoundary = At(realtime);
        var lastObservedBoundary = anchors.Select(At).Max();
        if (lastObservedBoundary > finalBoundary) finalBoundary = lastObservedBoundary;
        return finalBoundary;
    }

    private object CoverageReport(CpuTraceAnalysis analysis, DateTime finalBoundary)
    {
        var trace = analysis.Trace;
        var samples = analysis.Samples;
        var coverageStart = trace.SessionStartTime.ToUniversalTime();
        var coverageEnd = trace.SessionEndTime.ToUniversalTime();
        return new {
            traceSessionStartUtc = coverageStart, traceSessionEndUtc = coverageEnd, trace.EventsLost, trace.Truncated,
            attachRequestedUtc = Utc(root.GetProperty("attachRequestedUtc")),
            firstTargetThreadSampleUtc = samples.Count == 0 ? (DateTime?)null : samples.Min(x => x.Utc),
            lastTargetThreadSampleUtc = samples.Count == 0 ? (DateTime?)null : samples.Max(x => x.Utc),
            sampledThreadTimeEventsInWholeTrace = samples.Count,
            sampledThreadTimeEventsAfterFinalBoundary = samples.Count(x => x.Utc >= finalBoundary),
            processStartBeforeTraceSession = processStart < coverageStart,
            untracedInitialMilliseconds = Math.Max(0, (coverageStart - processStart).TotalMilliseconds),
            initialGap = "Before session start NOT_MEASURED; first sample is also reported. OS process CPU lifetime still includes startup before attach.",
            nativeOnlyAndDeadThreads = "EventPipe is not a complete native-thread census; OS snapshots only retain readable live thread counters."
        };
    }

    private static string[] Limitations() => new[]
    {
        "Process.TotalProcessorTime is the authoritative CPU total (user+kernel CPU seconds). It may exceed wall seconds on multiple cores.",
        "Main CPU is the primary OS thread counter. Process-minus-main includes every other thread, including native or already exited threads; it is not a sum of sampled/live workers.",
        "Per-thread differences require readable counters at both endpoints with the same OS TID and creation time. Missing, new, dead or reused threads remain null; their sum is not a complete process total.",
        "Function self/inclusive counts and per-thread stacks are sampled-thread-time candidates, including waiting time. Never multiply their shares by process or thread CPU seconds.",
        "Native, truncated and unresolved stacks stay unresolved; no native PDB downloads. No stack or no samples means NOT_MEASURED, never zero CPU.",
        "Inclusive frame counts overlap. Phase windows exclude extra collector idle/rundown, but counter snapshots occur after observed markers, so their read delay is reported separately.",
        "EventsLost or Truncated means incomplete captured events; no synthetic backfill or attribution correction is applied."
    };

    private object Summarize(Window window, CpuTraceAnalysis analysis)
    {
        var trace = analysis.Trace;
        var samples = analysis.Samples;
        var coverageStart = trace.SessionStartTime.ToUniversalTime();
        var coverageEnd = trace.SessionEndTime.ToUniversalTime();
        var selected = samples.Where(x => x.Utc >= window.Start && x.Utc < window.End).ToArray();
        var cpu = CpuCounters(window);
        return new {
            name = window.Name, requestedStartUtc = window.Start, requestedEndUtc = window.End,
            markerElapsedMilliseconds = (window.End - window.Start).TotalMilliseconds,
            startsBeforeTraceSession = window.Start < coverageStart, endsAfterTraceSession = window.End > coverageEnd,
            actualCpuCounters = cpu,
            gcAllocationSampling = analysis.SummarizeAllocations(windows.IndexOf(window), mainId),
            performanceAssociation = playing.Phase(window, boundaries),
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

    private object CpuCounters(Window window)
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

    private static object PlayingCpuIntervals(JsonElement root)
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

    private static DateTime Utc(JsonElement element) => DateTime.Parse(element.GetString()!, CultureInfo.InvariantCulture,
        DateTimeStyles.RoundtripKind).ToUniversalTime();
    private static double? Delta(double start, double end) => end >= start ? end - start : null;
    private static double? ThreadDelta(ThreadCpu? start, ThreadCpu? end) => start is not null && end is not null && start.StartUtc == end.StartUtc
        ? Delta(start.Seconds, end.Seconds) : null;
}

record Anchor(string Name, double Milliseconds, DateTime ObservedUtc, string Suffix);
record Window(string Name, string From, string To, DateTime Start, DateTime End);
record ThreadCpu(int Id, DateTime StartUtc, double Seconds, DateTime ReadUtc);
record Snapshot(string Name, DateTime ReadUtc, DateTime StartedUtc, DateTime FinishedUtc, double ProcessCpuSeconds, ThreadCpu[] Threads, int Unreadable);
