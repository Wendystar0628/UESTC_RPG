using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Contracts.Startup;

namespace Campus.Gates;

/// <summary>ENG-PERF-MEASURE: evaluates cached owner facts; Godot has no combined collection condition.</summary>
internal sealed class PerfCollectionConditions(PerfCollectionOptions options)
{
    private FramePipelineCounts? _previous;
    private int _stableFrames;
    private double? _readyAt;
    public int StableFrames => _stableFrames;

    public bool Stable(bool ready, bool drained, FramePipelineCounts pipelines, double now)
    {
        if (!ready || !drained)
        {
            _readyAt = null;
            _stableFrames = 0;
            _previous = pipelines;
            return false;
        }
        _readyAt ??= now;
        _stableFrames = _previous == pipelines ? Math.Min(options.StableFrames, _stableFrames + 1) : 0;
        _previous = pipelines;
        return _stableFrames >= options.StableFrames && now - _readyAt >= options.RealtimeSeconds * 1000;
    }

    public string? Missing(in PerfSamplingFacts facts, double now)
    {
        if (facts.SessionId is null) return "Active recording has no sampling session.";
        if (facts.RenderAdapterLuid is null || string.IsNullOrEmpty(facts.RenderGpuUuid))
            return "Actual rendering GPU identity is unconfirmed.";
        return Missing(facts.RenderGpuPerformanceState, now, "render GPU P-state") ??
            Missing(facts.OtherProcessesGpuUtilFraction, now, "other-process GPU");
    }

    public bool RejectPollution(in PerfSamplingFacts facts, out string? reason)
    {
        reason = facts.OtherProcessesGpuUtilFraction.Value > options.OtherGpuFraction
            ? "Other processes exceeded the frozen PDH same-engine GPU threshold." : null;
        // DECISION-344: functional collection retains contamination while completing the activity.
        return reason is not null && !options.FunctionalSmoke;
    }

    public bool PerformanceReady(in PerfSamplingFacts facts) => facts.RenderGpuPerformanceState.Value == 0;

    private string? Missing(in PerfSamplingFact fact, double now, string name)
    {
        if (fact.Capability != PerfFactCapability.Supported || fact.Value is not { } value || !double.IsFinite(value))
            return fact.Reason ?? name;
        if (fact.QueryEndMs is not { } end || end > now || now - end > options.MaximumFactAgeSeconds * 1000)
            return "A cached hardware observation has no fresh query endpoint.";
        return null;
    }
}
