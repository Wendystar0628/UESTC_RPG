using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Tests;

/// <summary>
/// Shared IMapService fake. Defaults describe a service with no maps; tests set the properties they need
/// and subclass only to override Travel, Transition or Visits. It does not implement ResolveSpawn, so the
/// interface's default NotSupported method stays observable.
/// </summary>
internal class TestMapService : IMapService
{
    public long Rev { get; set; }
    public MapId Default { get; set; }
    public MapId Current { get; set; }
    public IReadOnlyList<MapDescriptor> Maps { get; set; } = [];
    public IMapContext? Context { get; set; }
    public virtual IReadOnlyList<MapVisit> Visits => [];
    public virtual IMapTransition? Transition => LastTransition;
    protected IMapTransition? LastTransition { get; set; }
    public event Action<MapId, MapTravelCause>? Arrived;
    protected void RaiseArrived(MapId map, MapTravelCause cause) => Arrived?.Invoke(map, cause);
    public virtual IMapTransition Travel(MapTravelRequest request) => throw new NotSupportedException();
}

/// <summary>A map transition whose outcome is already decided by the test.</summary>
internal sealed class TestMapTransition(MapId from, MapId to, MapTravelCause cause, Task<MapTravelResult> done) : IMapTransition
{
    public static TestMapTransition Completed(MapId from, MapId to, MapTravelCause cause, MapTravelResult result) =>
        new(from, to, cause, Task.FromResult(result));
    public MapId From => from;
    public MapId To => to;
    public MapTravelCause Cause => cause;
    public MapStage Stage => done.IsCompleted ? MapStage.Done : MapStage.LoadData;
    public float Progress => 1;
    public Task<MapTravelResult> Done => done;
}

/// <summary>Shared IMapContext fake; World throws unless a test supplies one.</summary>
internal sealed record TestMapContext(MapDescriptor Descriptor, IWorldModel? WorldModel = null,
    BuildInputSnapshot? Inputs = null, string DataRoot = "res://test/") : IMapContext
{
    public BuildInputSnapshot SourceInputs => Inputs ?? BuildInputSnapshot.Empty;
    public IWorldModel World => WorldModel ?? throw new NotSupportedException("This map context fixture has no world model.");
}
