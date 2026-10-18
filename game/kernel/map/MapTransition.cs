using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Maps;

/// <summary>The map phase completes the task on the engine thread, after releasing the input lease.</summary>
public sealed class MapTransition(MapId from, MapTravelRequest request) : IMapTransition
{
    private readonly TaskCompletionSource<MapTravelResult> _completion = new();
    public MapTravelRequest Request { get; } = request;
    public MapId From { get; } = from;
    public MapId To => Request.Target;
    public MapTravelCause Cause => Request.Cause;
    public MapStage Stage { get; private set; } = MapStage.LoadData;
    public float Progress => Stage switch
    {
        MapStage.LoadData => 0,
        MapStage.Mount => 1f / 4,
        MapStage.Warm => 2f / 4,
        MapStage.Spawn => 3f / 4,
        MapStage.Done => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(Stage))
    };
    public Task<MapTravelResult> Done => _completion.Task;
    internal void MoveTo(MapStage stage) => Stage = stage;
    internal void Complete(MapTravelResult result)
    {
        Stage = MapStage.Done;
        _completion.TrySetResult(result);
    }
}
