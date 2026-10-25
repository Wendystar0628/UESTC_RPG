namespace Campus.Contracts.Startup;

/// <summary>One immutable AI collection request. The entry script owns all collection values;
/// the driver receives no independent defaults or thresholds.</summary>
public sealed record PerfCollectionOptions
{
    public required string SceneId { get; init; }
    public required string SampleSegmentId { get; init; }
    public bool SaveAfterSample { get; init; }
    /// <summary>EventPipe diagnostics keep the target alive after the real sample End until the owner closes its window.</summary>
    public bool ExternalCloseAfterSample { get; init; }
    public string[] RoutePointIds { get; init; } = [];
    public bool TravelPrefetch { get; init; }
    public required double SampleSeconds { get; init; }
}
