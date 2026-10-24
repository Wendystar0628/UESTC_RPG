namespace Campus.Contracts.Perf;

public enum PerfFactCapability : byte { NotRecording, Starting, Supported, Unsupported, Unavailable }

/// <summary>Detached sampling fact. Times are process-monotonic milliseconds; an undisclosed
/// averaging window stays null and is never inferred from the query duration.</summary>
public readonly record struct PerfSamplingFact
{
    public double? Value { get; init; }
    public PerfFactCapability Capability { get; init; }
    public string? Reason { get; init; }
    public string? Source { get; init; }
    public int? NativeCode { get; init; }
    public double? QueryStartMs { get; init; }
    public double? QueryEndMs { get; init; }
    public double? AverageStartMs { get; init; }
    public double? AverageEndMs { get; init; }
}

/// <summary>One active recording session's immutable cached facts. A default value means
/// NotRecording. Adapter identity is independently nullable; P-state requires a confirmed GPU.</summary>
public readonly record struct PerfSamplingFacts(
    string? SessionId,
    ulong? RenderAdapterLuid,
    string? RenderGpuUuid,
    PerfSamplingFact RenderGpuPerformanceState,
    PerfSamplingFact WholeGpuUtilFraction,
    PerfSamplingFact OtherProcessesGpuUtilFraction,
    PerfSamplingFact OtherProcessesCpuUtilFraction);
