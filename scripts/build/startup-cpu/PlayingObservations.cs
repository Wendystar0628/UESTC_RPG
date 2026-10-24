// 读取分析器派生的公共性能指标与异常，仅关联阶段、日志和 EventPipe 栈。
// 用法：由 CpuPhaseReport 创建；会话分析在游戏正式关闭排空后由 run.ps1 完成。
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

sealed class PlayingObservations
{
    private readonly LogEvidence[] evidence;
    private readonly JsonElement summary;
    private readonly JsonElement anomalies;
    private readonly string derivedDirectory;

    public PlayingObservations(string logText, JsonElement performance)
    {
        evidence = ReadLogEvidence(logText);
        var recordingRoot = performance.GetProperty("recordingRoot").GetString()!;
        var manifests = Directory.GetFiles(recordingRoot, "manifest.json", SearchOption.AllDirectories);
        if (manifests.Length != 1) throw new InvalidDataException($"Expected one closed performance session in {recordingRoot}, found {manifests.Length}.");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifests[0]));
        var id = manifest.RootElement.GetProperty("id").GetString()!;
        if (Path.GetFileName(id) != id || id is "." or "..") throw new InvalidDataException("Invalid performance session ID.");
        derivedDirectory = Path.Combine(performance.GetProperty("derivedRoot").GetString()!, id);
        summary = ReadDerived("summary.json");
        anomalies = ReadDerived("anomalies.json");
        if (!string.Equals(Path.GetFullPath(summary.GetProperty("source").GetString()!),
                Path.GetDirectoryName(manifests[0]), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Performance summary belongs to a different recording.");
        if (summary.GetProperty("id").GetString() != id || anomalies.GetProperty("id").GetString() != id ||
            summary.GetProperty("calc").GetRawText() != anomalies.GetProperty("calc").GetRawText())
            throw new InvalidDataException("Performance derived documents do not describe the same session/calculation.");
    }

    private JsonElement ReadDerived(string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(derivedDirectory, name)));
        return document.RootElement.Clone();
    }

    public object SessionReport() => new
    {
        derivedDirectory, summary, anomalies,
        source = "scripts/perf/analyze.py; values, availability reasons and classifications are copied unchanged",
        scope = "Whole recording, including formal shutdown. Startup phase windows below only select overlapping analyzer windows/anomalies; no phase distribution is recomputed."
    };

    public object LogReport() => new
    {
        observations = evidence, rawLog = "godot.log",
        exceptionLines = evidence.Count(x => x.Kind == "exception-or-error"),
        explicitBakeOrCacheLines = evidence.Count(x => x.Kind == "explicit-bake-or-cache"),
        bakeStatus = "UNKNOWN without explicit cache-hit/miss/build observations; sampled bake stacks below are candidates, not cache-miss counts",
        attribution = "Frame-number association and line-order brackets only. Unstamped exceptions are not assigned an invented timestamp. Absence of an error or cache line is not proof of no stalls or no baking."
    };

    public object Phase(Window window, IReadOnlyDictionary<string, Anchor> boundaries)
    {
        double? fromMs = window.From == "process-start" ? null : boundaries[window.From].Milliseconds;
        var toMs = boundaries[window.To].Milliseconds;
        bool Overlaps(JsonElement item) => item.GetProperty("fromMs").GetDouble() <= toMs &&
            (fromMs is null || item.GetProperty("toMs").GetDouble() >= fromMs);
        return new
        {
            fromMilliseconds = fromMs, toMilliseconds = toMs,
            analyzerWindows = summary.TryGetProperty("windows", out var windows) ?
                windows.EnumerateArray().Where(Overlaps).ToArray() : null,
            analyzerAnomalies = anomalies.GetProperty("items").EnumerateArray().Where(Overlaps).ToArray(),
            unavailableReasons = summary.GetProperty("validity"),
            limit = "Whole-session renderer CPU/GPU, phases, compilation and GC are in performanceSession.summary.all/steady.cost. Existing analyzer windows/anomalies are selected by overlap without splitting, reclassifying or recomputing them. Missing values and reasons remain as produced by the analyzer. EventPipe candidates for this phase are reported alongside this association."
        };
    }

    private static LogEvidence[] ReadLogEvidence(string text)
    {
        var lines = text.Split('\n');
        var before = new long?[lines.Length]; var after = new long?[lines.Length];
        long? previous = null;
        for (var i = 0; i < lines.Length; i++)
        {
            var match = Regex.Match(lines[i], @"^STARTUP_PHASE .*?\bframe=(\d+)");
            if (match.Success) previous = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            before[i] = previous;
        }
        long? next = null;
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var match = Regex.Match(lines[i], @"^STARTUP_PHASE .*?\bframe=(\d+)");
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

record LogEvidence(string Kind, int Line, string Text, long? Frame, long? PreviousFrame, long? NextFrame);
