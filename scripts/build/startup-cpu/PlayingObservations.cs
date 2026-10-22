// 解析 Playing 日志，产出渲染/帧间隔、尖峰证据及实际位移观察。
// 用法：由 CpuPhaseReport 经 CpuParse 入口创建；Frames 只读取既有证据与采样候选。
using System.Globalization;
using System.Text.RegularExpressions;
using static CpuTraceAnalysis;

sealed class PlayingObservations
{
    private readonly RenderFrame[] rendererFrames;
    private readonly LogEvidence[] evidence;

    public PlayingObservations(string logText)
    {
        rendererFrames = Regex.Matches(logText,
            @"(?m)^STARTUP_FRAME ([0-9.]+) frame=(\d+) rendererCpuMs=([0-9.Ee+-]+) rendererGpuMs=([0-9.Ee+-]+)([^\r\n]*)")
            .Select(m => new RenderFrame(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                long.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture), m.Groups[5].Value)).ToArray();
        evidence = ReadLogEvidence(logText);
    }

    public object LogReport() => new
    {
        observations = evidence, rawLog = "godot.log",
        exceptionLines = evidence.Count(x => x.Kind == "exception-or-error"),
        explicitBakeOrCacheLines = evidence.Count(x => x.Kind == "explicit-bake-or-cache"),
        bakeStatus = "UNKNOWN without explicit cache-hit/miss/build observations; sampled bake stacks below are candidates, not cache-miss counts",
        attribution = "Frame-number association and line-order brackets only. Unstamped exceptions are not assigned an invented timestamp. Absence of an error or cache line is not proof of no stalls or no baking."
    };

    public object Frames(Window window, CpuPhaseReport phases, CpuTraceAnalysis analysis)
    {
        var boundaries = phases.Boundaries;
        var samples = analysis.Samples;
        var mainId = phases.MainId;
        var origin = phases.Origin;
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

    private static long? FrameOf(Anchor a) => Regex.Match(a.Suffix, @"(?:^|\s)frame=(\d+)(?:\s|$)") is { Success: true } match
        ? long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;

    private static object Distribution(double[] input)
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

    private static object Movement(RenderFrame[] frames)
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

    private static LogEvidence[] ReadLogEvidence(string text)
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
}

record RenderFrame(double Milliseconds, long Frame, double Cpu, double Gpu, string Details);
record MovementFrame(long Frame, double Milliseconds, double X, double Y, double Z, double Yaw, bool Pressed, bool? CursorVisible, bool? CursorHeld);
record LogEvidence(string Kind, int Line, string Text, long? Frame, long? PreviousFrame, long? NextFrame);
