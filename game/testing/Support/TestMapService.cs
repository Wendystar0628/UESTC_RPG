using Campus.Gen.World;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Tests;

/// <summary>
/// Shared IMapService fake. Defaults describe a service with no maps; tests set the properties they need
/// and subclass only to override Prepare, Transition or Visits. Spawn resolution is opt-in and can use a
/// real catalogue resolver without introducing another IMapService implementation.
/// </summary>
internal class TestMapService : IMapService
{
    public long Rev { get; set; }
    public MapId Default { get; set; }
    public MapId Current { get; set; }
    public IReadOnlyList<MapDescriptor> Maps { get; set; } = [];
    public IMapContext? Context { get; set; }
    public virtual IReadOnlyList<MapVisit> Visits => [];
    public virtual IMapTransition? Transition => null;
    public event Action<MapPrepareEvent>? PrepareCommand;
    public void PublishPrepareCommand(MapPrepareEvent value) => PrepareCommand?.Invoke(value);
    public Func<MapId, string?, MapSpawn>? SpawnResolver { get; set; }
    public virtual MapSpawn ResolveSpawn(MapId map, string? spawn = null) =>
        SpawnResolver is { } resolve ? resolve(map, spawn) : throw new NotSupportedException();
    public virtual Task Prepare(MapId map) => Task.CompletedTask;
    public virtual void StopNewWork() { }
    public virtual Task DrainAsync() => Task.CompletedTask;
}

/// <summary>A map transition whose outcome is already decided by the test.</summary>
internal sealed class TestMapTransition(MapId from, MapId to, Task<MapTravelResult> done) : IMapTransition
{
    public MapId From => from;
    public MapId To => to;
    public MapStage Stage => done.IsCompleted ? MapStage.Done : MapStage.LoadData;
    public float Progress => 1;
    public Task<MapTravelResult> Done => done;
    public Exception? Failure { get; init; }
}

/// <summary>Shared IMapContext fake; World throws unless a test supplies one.</summary>
internal sealed record TestMapContext(MapDescriptor Descriptor, IWorldModel? WorldModel = null,
    BuildInputSnapshot? Inputs = null, string DataRoot = "res://test/") : IMapContext, IMapInteriorPreparation
{
    public BuildInputSnapshot SourceInputs => Inputs ?? BuildInputSnapshot.Empty;
    private IWorldModel? _preparedWorld;
    public IWorldModel World => _preparedWorld ?? WorldModel ?? throw new NotSupportedException("This map context fixture has no world model.");
    public void PublishInteriors(IReadOnlyList<InteriorIndexEntry> entries)
    {
        if (_preparedWorld is not null) throw new InvalidOperationException("Map interiors have already been published.");
        _preparedWorld = WorldModelSnapshot.WithInteriors(World, entries);
    }
}
