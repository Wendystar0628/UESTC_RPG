using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Frame;
using Campus.Kernel.Lod;
using Godot;
using Campus.Kernel.Registry;
using ParticipantTile = Campus.Kernel.Stream.StreamParticipants.ParticipantTile;

namespace Campus.Kernel.Stream;

public sealed record StreamDirectorDependencies(ServiceRegistry Owner, IBakeContextSource ContextSource,
    IBakeService Bake, IStreamMountQueue Mount, ICameraRig Camera, IObserverState Observer, IWorldState World,
    ICollisionWorld Collision, IMapService Maps, ITerrainPreparation Terrain, IReflectionWorld Reflections,
    IToonMaterialProvider Toon, IShaderMaterialSource Shaders, Camera3D ViewCamera, bool CaptureMode, IStreamGeometryPreparation Geometry);

public interface IStreamVisualFrame
{
    bool CanActivateVisual { get; }
    bool CanReleaseRetiredInstances { get; }
    bool ViewReady { get; }
    void PrepareMountFrame();
    void CompleteMountFrame(double remainingMilliseconds);
    void ReleaseRetired(double budgetMilliseconds);
}

[KernelService(typeof(IStreamVisualFrame))]
[KernelService(typeof(IMapStreamState))]
[KernelService(typeof(IStreamStatistics))]
[KernelNode("stream.select")]
public partial class StreamDirector : Node, IMapAware, IMapStreamState, IStreamVisualFrame, IStreamStatistics
{
    private readonly StreamSelection _selection;
    private StreamPreparation _preparation = null!;
    private readonly StreamResidency _residency;
    private StreamParticipants _participants = null!;
    private readonly StreamPins _pins;
    private ServiceRegistry? _owner;
    private IBakeContextSource _contextSource = null!;
    private IBakeService _bake = null!;
    private IStreamMountQueue _mount = null!;
    private ICameraRig _camera = null!;
    private IObserverState _observer = null!;
    private IWorldState _world = null!;
    private ICollisionWorld _collision = null!;
    private IMapService _maps = null!;
    private ITerrainPreparation _terrain = null!;
    private IReflectionWorld _reflections = null!;
    private InteriorStream? _interiors;
    private IMapContext? _context;
    private long _mapGeneration;
    private Exception? _failure;
    private readonly List<Exception> _failures = [];
    private MapId? _unloadingMap;
    private Task? _drain;
    private bool _exiting, _captureMode, _checkRetention;
    private long _statisticsRevision, _mountedBytes;
    private int _watchdogBaseline, _mountedTiles, _mountedTilesDelta;

    public StreamDirector()
    {
        _selection = new(this); _residency = new(this); _pins = new(this);
    }
    internal StreamSelection Selection => _selection;
    internal StreamPreparation Preparation => _preparation;
    internal StreamResidency Residency => _residency;
    internal StreamParticipants Participants => _participants;
    internal StreamPins Pins => _pins;
    internal IMapContext? Context => _context;
    internal long MapGeneration => _mapGeneration;
    internal IBakeService Bake => _bake;
    internal IStreamMountQueue Mount => _mount;
    internal ICameraRig Camera => _camera;
    internal IObserverState Observer => _observer;
    internal ICollisionWorld Collision => _collision;
    internal IMapService Maps => _maps;
    internal ITerrainPreparation Terrain => _terrain;
    internal IReflectionWorld Reflections => _reflections;
    internal InteriorStream? Interiors => _interiors;
    internal bool CaptureMode => _captureMode;
    public MapStreamDestination? Destination => _selection.Destination;
    public bool HasPendingDestination => _selection.HasPendingDestination;
    public IReadOnlyList<MapStreamDestination> PinnedDestinations => _pins.PinnedDestinations;
    public bool IsUnloaded => _owner is null || (_context is null && _unloadingMap is null && _selection.IsIdle && _pins.IsIdle &&
        _preparation.IsIdle && _residency.IsIdle && (_interiors?.IsUnloaded ?? true) && _mount.Pending == 0 && _mount.PendingRetirements == 0);
    public bool ResidentReady => Failure is null && _context is not null && _preparation.Catalog is not null && _residency.RootMounted;
    public bool DestinationReady => _selection.DestinationReady;
    public bool PinnedDestinationReady(MapStreamDestination destination) => _pins.DestinationReady(destination);
    public Exception? Failure => _failure;
    private void PollFailures()
    {
        if (_mount?.Failure is { } failure) RecordFailure(failure);
        if (_interiors?.Failure is { } interior) RecordFailure(interior);
        _participants?.PollParticipantErrors();
    }
    public int MountedTiles => _residency.MountedTiles;
    public int VisibleTiles => _selection.VisibleTiles;
    public bool CanActivateVisual => _residency.CanActivateVisual;
    public bool CanReleaseRetiredInstances => _residency.CanReleaseRetiredInstances;
    /// <summary>The view shares the complete selected-geometry barrier with a formal arrival.</summary>
    public bool ViewReady => _selection.ViewReady;
    /// <summary>Packed-buffer bytes, not compressed disk bytes or measured VRAM.</summary>
    public long ResidentBytes => _residency.ResidentBytes;
    public event Action<StreamTileChange>? TileChanged;
    public StreamStatistics Statistics
    {
        get
        {
            SynchronizeStatisticsRevision();
            return new()
            {
                Revision = _mount.Revision,
                PendingRequests = _preparation.Requests,
                PendingBakes = _preparation.Bakes + (_interiors?.Bakes ?? 0),
                PendingPreparations = _preparation.Preparations + (_interiors?.Preparations ?? 0) +
                    _selection.Preparations + _pins.Preparations,
                PendingMounts = _mount.Pending,
                PendingRetirements = _mount.PendingRetirements + _residency.Retirements + _preparation.Retirements +
                    (_interiors?.Retirements(_context is null) ?? 0) + _selection.Retirements + _pins.Retirements,
                MountMilliseconds = _context is null ? 0 : _mount.LastMilliseconds,
                WatchdogTrips = _context is null ? 0 : _mount.WatchdogTrips - _watchdogBaseline,
                MountedBytes = _mountedBytes,
                MountedTiles = _mountedTiles,
                MountedTilesDelta = _mountedTilesDelta
            };
        }
    }

    private void SynchronizeStatisticsRevision()
    {
        if (_statisticsRevision == _mount.Revision) return;
        _statisticsRevision = _mount.Revision; _watchdogBaseline = _mount.WatchdogTrips;
        _mountedBytes = 0; _mountedTiles = 0; _mountedTilesDelta = 0;
    }

    private void BeginStatisticsFrame()
    {
        SynchronizeStatisticsRevision();
        _mountedBytes = 0; _mountedTiles = 0; _mountedTilesDelta = 0;
    }

    internal void RecordTileChange(StreamTileChange change)
    {
        SynchronizeStatisticsRevision();
        // Old owners retain their identity for events, but never add to a new map's frame.
        if (_context is not null && change.Revision == _statisticsRevision)
        {
            switch (change.Change)
            {
                case StreamTileChangeKind.Mounted:
                    _mountedTiles++;
                    _mountedTilesDelta++;
                    if (change.Bytes is { } bytes) _mountedBytes += bytes;
                    break;
                case StreamTileChangeKind.Unmounted: _mountedTilesDelta--; break;
                default: throw new ArgumentOutOfRangeException(nameof(change));
            }
        }
        TileChanged?.Invoke(change);
    }

    public void Bind(StreamDirectorDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        if (IsInsideTree() || _owner is not null) throw new InvalidOperationException("Stream dependencies bind once before entering the tree.");
        _owner = dependencies.Owner; _contextSource = dependencies.ContextSource;
        _captureMode = dependencies.CaptureMode; _bake = dependencies.Bake; _mount = dependencies.Mount;
        _camera = dependencies.Camera; _observer = dependencies.Observer; _world = dependencies.World;
        _collision = dependencies.Collision; _maps = dependencies.Maps; _terrain = dependencies.Terrain;
        _reflections = dependencies.Reflections;
        _preparation = new(this, dependencies.Geometry);
        _participants = new(this, dependencies.Owner, dependencies.ViewCamera);
        _residency.InitializeMaterials(dependencies.Toon, dependencies.Shaders);
        _interiors = new(_bake, _mount, _residency.Materials, _reflections, _residency.QueueRetirement,
            _captureMode, dependencies.Geometry, RecordTileChange, _residency.QueueDetach, () => _residency.CanReleaseRetiredInstances);
        BeginStatisticsFrame();
    }

    public override void _EnterTree()
    {
        if (_owner is null) throw new InvalidOperationException("Stream dependencies must be bound before entering the tree.");
        _exiting = false;
        _world.InstanceAdvancing += OnWorldInstanceChanged;
        _reflections.Changed += _residency.OnReflectionChanged;
        OnWorldInstanceChanged(0);
    }
    public override void _Ready() => ProcessPriority = FramePriority.Stream;
    public void OnMapLoaded(IMapContext context)
    {
        if (_owner is null) throw new InvalidOperationException("Stream dependencies have not been bound.");
        if (!IsUnloaded) throw new InvalidOperationException("Stream map must finish unloading before the next mount.");
        _mapGeneration++;
        _selection.BeginMapChange();
        _context = context; _failure = null; _failures.Clear(); _drain = null;
        BeginStatisticsFrame(); _watchdogBaseline = _mount.WatchdogTrips;
        _selection.Load(context); _residency.Load();
        var contextSource = _contextSource;
        var firstModule = _owner!.Query<IContentModule>(RegistrationKind.Content, context.Descriptor.Id).FirstOrDefault();
        _interiors!.Load(context, firstModule is null ? null : contextSource.GetPreparedContext(context, firstModule.Id));
        _preparation.LoadCatalog(contextSource.GetPreparedModuleTilesAsync(context));
        _participants.LoadParticipants(context.Descriptor.Id);
    }
    public void OnMapUnloading(MapId from)
    {
        _unloadingMap = from;
        _mapGeneration++;
        _context = null;
        BeginStatisticsFrame(); _watchdogBaseline = _mount.WatchdogTrips;
        Stop(_selection.BeginMapChange);
        Stop(_pins.StopNewWork);
        Stop(_selection.Unload);
        Stop(_preparation.Unload);
        Stop(_selection.ClearDemand);
        // No data/native release here. Other MapAware producers must stop before the lifetime owner drains us.
    }

    public Task DrainAsync()
    {
        if (_drain is null || _drain.IsCompleted && !IsUnloaded) _drain = DrainMap();
        return _drain;
    }

    private async Task DrainMap()
    {
        if (_context is { } context) OnMapUnloading(context.Descriptor.Id);
        Stop(_residency.DetachMap);
        if (_interiors is not null) Stop(_interiors.Unload);
        if (_unloadingMap is { } map) Stop(() => _mount.RemoveMap(map));
        try { await _selection.DrainAsync(); } catch (Exception error) { RecordFailure(error); }
        try { await _pins.DrainAsync(); } catch (Exception error) { RecordFailure(error); }
        try { await _preparation.DrainAsync(); } catch (Exception error) { RecordFailure(error); }
        if (_interiors is not null)
            try { await _interiors.DrainAsync(); } catch (Exception error) { RecordFailure(error); }
        Stop(_participants.UnloadParticipants);
        Stop(_residency.Unload);
        // ProcessFrame precedes node processing. Let the executor get a complete frame on every
        // explicit drain attempt before observing its release-failure flag from the next boundary.
        if (_mount.Pending > 0 || _mount.PendingRetirements > 0 || !_residency.IsIdle)
            await FrameAwaiter.ProcessAsync(this);
        while (_mount.Pending > 0 || _mount.PendingRetirements > 0 || !_residency.IsIdle)
        {
            await FrameAwaiter.ProcessAsync(this);
            if (_mount is IStreamMountFrame { RetirementFailed: true }) break;
        }
        if (_selection.IsIdle && _pins.IsIdle && _preparation.IsIdle && _residency.IsIdle && (_interiors?.IsUnloaded ?? true) &&
            _mount.Pending == 0 && _mount.PendingRetirements == 0)
        { _preparation.CompleteUnload(); _unloadingMap = null; }
        PollFailures();
        if (Failure is { } failure) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void Stop(Action stop)
    {
        try { stop(); }
        catch (Exception error) { RecordFailure(error); }
    }
    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null) => _selection.Prefetch(destination, mode, space, collisionFeet);
    public IDisposable PinDestination(MapStreamDestination destination) => _pins.PinDestination(destination);
    public void ClearDestination() => _selection.ClearDestination();
    public override void _Process(double delta)
    {
        BeginStatisticsFrame();
        if (_exiting || _context is null) return;
        try
        {
            PollFailures();
            if (Failure is not null) return;
            _selection.PollRetired(); _pins.PollRetired();
            _preparation.HarvestCatalog();
            _pins.PollPinnedPreparation();
            if (Failure is not null || _preparation.Catalog is null || _selection.Tree is null) return;
            _selection.Update(delta);
            _preparation.IssueQueued();
            _interiors?.AdvanceRetention(delta, _camera.Pose.Position);
        }
        catch (Exception error) { RecordFailure(error); }
    }
    public void PrepareMountFrame()
    {
        PollFailures();
        if (_context is null || _exiting || Failure is not null) return;
        _preparation.HarvestPreparations();
        _interiors?.HarvestPreparations(GetViewport().GetWorld3D().Scenario);
        PollFailures();
        if (Failure is not null || _preparation.Catalog is null || _selection.Tree is null) return;
        // Current demand is already applied; reclaim completed slots before admitting more preparation.
        _preparation.PollHandles(background: false);
        _interiors?.Poll(Math.Max(0, _selection.Settings.MaximumPreparations - _preparation.Preparations - _interiors.Preparations));
        _preparation.PollHandles(background: true);
        _interiors?.Poll(Math.Max(0, _selection.Settings.MaximumPreparations - _preparation.Preparations - _interiors.Preparations), background: true);
        PollFailures();
    }
    public void CompleteMountFrame(double remainingMilliseconds)
    {
        PollFailures();
        if (_context is null || _exiting || Failure is not null) return;
        if (remainingMilliseconds >= .1) _interiors?.ApplyVisibility();
        if (_checkRetention) { _checkRetention = false; _residency.RetireUnneeded(); }
        if (_selection.Tree is null || _preparation.Catalog is null || remainingMilliseconds < .1 || !_selection.PlanCurrent) return;
        _residency.RefreshReflectionWorld(remainingMilliseconds * .25);
        _selection.ApplyVisibility();
    }
    public void ReleaseRetired(double budgetMilliseconds)
    {
        _residency.ReleaseRetired(budgetMilliseconds);
    }
    private void OnWorldInstanceChanged(long _) => _selection.ResetWorld();
    internal void RequestRetention() => _checkRetention = true;
    internal void RecordFailure([FailureHandler("ENG-PERF-STREAM")] Exception error)
    {
        if (error is AggregateException { InnerExceptions.Count: > 0 } aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions) RecordFailure(inner);
            return;
        }
        if (_failures.Contains(error)) return;
        _failures.Add(error);
        _failure = _failures.Count == 1 ? error : new AggregateException("Stream failed.", _failures);
    }

    internal void Fail(StreamTile tile, [FailureHandler("ENG-PERF-STREAM")] Exception error, ParticipantTile? participant = null)
    {
        if (participant is not null) participant.Failure = error;
        else if (_residency.Groups.TryGetValue(tile, out var group)) group.Failure = error;
        RecordFailure(error);
    }

    internal bool CanUseLiveCamera() => _selection.Destination is not null && (_maps.Transition?.Stage switch
    {
        null or MapStage.Done => true,
        MapStage.LoadData or MapStage.Mount => false,
        _ => throw new InvalidOperationException("Unknown map transition stage.")
    });

    public override void _ExitTree()
    {
        // Registry awaited IMapLifetime.ShutdownAsync while the main thread and all bindings were still alive.
        // Exit callbacks only detach subscriptions; they never block or abandon asynchronous ownership.
        _exiting = true;
        if (_world is not null) _world.InstanceAdvancing -= OnWorldInstanceChanged;
        if (_reflections is not null) _reflections.Changed -= _residency.OnReflectionChanged;
        _residency.RequireMaterialsDrained();
    }
}
