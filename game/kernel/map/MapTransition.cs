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
    public MapStage Stage { get; private set; } = MapStage.VeilIn;
    public float Progress => Stage switch
    {
        MapStage.VeilIn => 0,
        MapStage.Snapshot => 1f / 8,
        MapStage.Unload => 2f / 8,
        MapStage.LoadData => 3f / 8,
        MapStage.Mount => 4f / 8,
        MapStage.Warm => 5f / 8,
        MapStage.Spawn => 6f / 8,
        MapStage.VeilOut => 7f / 8,
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
