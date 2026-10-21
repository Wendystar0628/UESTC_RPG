using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Maps;

/// <summary>The map phase completes the task on the engine thread, after releasing the input lease.</summary>
public sealed class MapTransition(MapId from, MapId to) : IMapTransition
{
    private readonly TaskCompletionSource<MapTravelResult> _completion = new();
    public MapId From { get; } = from;
    public MapId To { get; } = to;
    public MapStage Stage { get; private set; } = MapStage.LoadData;
    public float Progress => Stage switch
    {
        MapStage.LoadData => 0,
        MapStage.Mount => .5f,
        MapStage.Done => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(Stage))
    };
    public Task<MapTravelResult> Done => _completion.Task;
    public Exception? Failure { get; private set; }
    internal void MoveTo(MapStage stage) => Stage = stage;
    internal void Complete(MapTravelResult result, [FailureHandler("SYS-MAPS")] Exception? failure = null)
    {
        Failure = failure;
        Stage = MapStage.Done;
        _completion.TrySetResult(result);
    }
}
