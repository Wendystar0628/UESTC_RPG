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
    bool ViewReady { get; }
    void FinishMountFrame(double remainingMilliseconds);
    void ReleaseRetired(double budgetMilliseconds);
}

[KernelService(typeof(IStreamVisualFrame))]
[KernelService(typeof(IMapStreamState))]
[KernelNode("stream.select")]
public partial class StreamDirector : Node, IMapAware, IMapStreamState, IStreamVisualFrame
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
    public bool IsUnloaded => _context is null && _unloadingMap is null && _selection.IsIdle && _pins.IsIdle &&
        _preparation.IsIdle && _residency.IsIdle && (_interiors?.IsUnloaded ?? true);
    public bool ResidentReady => Failure is null && _context is not null && _preparation.Catalog is not null && _residency.RootMounted;
    public bool DestinationReady => _selection.DestinationReady;
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
    /// <summary>The view shares the complete selected-geometry barrier with a formal arrival.</summary>
    public bool ViewReady => _selection.ViewReady;
    /// <summary>Packed-buffer bytes, not compressed disk bytes or measured VRAM.</summary>
    public long ResidentBytes => _residency.ResidentBytes;

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
        _interiors = new(_bake, _mount, _residency.Materials, _reflections, _residency.QueueRetirement, _captureMode, dependencies.Geometry);
    }

    public override void _EnterTree()
    {
        if (_owner is null) throw new InvalidOperationException("Stream dependencies must be bound before entering the tree.");
        _exiting = false;
        _world.InstanceChanged += OnWorldInstanceChanged;
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
        Stop(_selection.BeginMapChange);
        Stop(() => SetPinnedDestinations(Array.Empty<MapStreamDestination>()));
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
        if (_unloadingMap is { } map) Stop(() => _mount.RemoveMap(map));
        try { await _selection.DrainAsync(); } catch (Exception error) { RecordFailure(error); }
        try { await _pins.DrainAsync(); } catch (Exception error) { RecordFailure(error); }
        try { await _preparation.DrainAsync(); } catch (Exception error) { RecordFailure(error); }
        if (_interiors is not null)
            try { await _interiors.DrainAsync(); } catch (Exception error) { RecordFailure(error); }
        Stop(_participants.UnloadParticipants);
        Stop(_residency.Unload);
        if (_selection.IsIdle && _pins.IsIdle && _preparation.IsIdle && _residency.IsIdle && (_interiors?.IsUnloaded ?? true))
            _unloadingMap = null;
        PollFailures();
        if (Failure is { } failure) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void Stop(Action stop)
    {
        try { stop(); }
        catch (Exception error) { RecordFailure(error); }
    }
    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null) => _selection.Prefetch(destination, mode, space, collisionFeet);
    public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations) => _pins.SetPinnedDestinations(destinations);
    public override void _Process(double delta)
    {
        if (_exiting || _context is null) return;
        PollFailures();
        if (Failure is not null) return;
        try
        {
            _selection.PollRetired(); _pins.PollRetired();
            _preparation.HarvestCatalog();
            _pins.PollPinnedPreparation();
            _interiors?.Poll(Math.Max(0, _selection.Settings.MaximumPreparations - _preparation.Preparations - _interiors.Preparations));
            if (_interiors?.Failure is { } interiorError) RecordFailure(interiorError);
            _preparation.PollHandles();
            if (Failure is not null || _preparation.Catalog is null || _selection.Tree is null) return;
            _selection.Update(delta);
            _preparation.IssueQueued();
            _interiors?.AdvanceRetention(delta, _camera.Pose.Position);
        }
        catch (Exception error) { RecordFailure(error); }
    }
    public void FinishMountFrame(double remainingMilliseconds)
    {
        PollFailures();
        if (_context is null || _exiting || Failure is not null) return;
        _preparation.FinishMountFrame();
        _interiors?.Finish(GetViewport().GetWorld3D().Scenario, remainingMilliseconds >= .1);
        PollFailures();
        if (Failure is not null) return;
        if (_checkRetention) { _checkRetention = false; _residency.RetireUnneeded(); }
        if (_selection.Tree is null || _preparation.Catalog is null || remainingMilliseconds < .1 || !_selection.PlanCurrent) return;
        _residency.RefreshReflectionWorld(remainingMilliseconds * .25);
        _selection.ApplyVisibility();
    }
    public void ReleaseRetired(double budgetMilliseconds)
    {
        if (_context is not null && !_exiting && Failure is null) _residency.ReleaseRetired(budgetMilliseconds);
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
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        if (_reflections is not null) _reflections.Changed -= _residency.OnReflectionChanged;
        _residency.DisposeMaterials();
    }
}
