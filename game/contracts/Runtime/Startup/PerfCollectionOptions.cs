namespace Campus.Contracts.Startup;

/// <summary>One immutable AI collection request. The entry script owns all protocol values;
/// the driver receives no independent defaults or thresholds.</summary>
public sealed record PerfCollectionOptions
{
    public required string SceneId { get; init; }
    public required string SampleSegmentId { get; init; }
    public required bool FunctionalSmoke { get; init; }
    public bool SaveAfterSample { get; init; }
    public string[] RoutePointIds { get; init; } = [];
    public bool TravelPrefetch { get; init; }
    public required int StableFrames { get; init; }
    public required double RealtimeSeconds { get; init; }
    public required double SampleSeconds { get; init; }
    public required double OtherGpuFraction { get; init; }
    public required double OtherCpuFraction { get; init; }
    public required double MaximumFactAgeSeconds { get; init; }
}
