using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Frame;
using Campus.Kernel.Lod;
using Campus.Kernel.Renderer;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;
using ParticipantTile = Campus.Kernel.Stream.StreamParticipants.ParticipantTile;

namespace Campus.Kernel.Stream;

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
    private readonly StreamPreparation _preparation;
    private readonly StreamResidency _residency;
    private readonly StreamParticipants _participants;
    private readonly StreamPins _pins;
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
        _selection = new(this); _preparation = new(this); _residency = new(this);
        _participants = new(this); _pins = new(this);
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
    public Exception? Failure
    {
        get
        {
            if (_mount?.Failure is { } failure) RecordFailure(failure);
            if (_interiors?.Failure is { } interior) RecordFailure(interior);
            return _failure;
        }
    }
    public int MountedTiles => _residency.MountedTiles;
    public int VisibleTiles => _selection.VisibleTiles;
    public bool CanActivateVisual => _residency.CanActivateVisual;
    /// <summary>The view shares the complete selected-geometry barrier with a formal arrival.</summary>
    public bool ViewReady => _selection.ViewReady;
    /// <summary>Packed-buffer bytes, not compressed disk bytes or measured VRAM.</summary>
    public long ResidentBytes => _residency.ResidentBytes;

    public override void _EnterTree()
    {
        _exiting = false;
        if (_world is not null) _world.InstanceChanged += OnWorldInstanceChanged;
        OnWorldInstanceChanged(0);
    }
    public override void _Ready()
    {
        ProcessPriority = FramePriority.Stream;
        _captureMode = KernelServices.Get<BootRunOptions>().CaptureMode;
        _bake = KernelServices.Get<IBakeService>(); _mount = KernelServices.Get<IStreamMountQueue>();
        _camera = KernelServices.Get<ICameraRig>(); _world = KernelServices.Get<IWorldState>();
        _observer = KernelServices.Get<IObserverState>();
        _world.InstanceChanged += OnWorldInstanceChanged;
        _maps = KernelServices.Get<IMapService>();
        _terrain = KernelServices.Get<ITerrainPreparation>();
        _maps.Arrived += _selection.OnMapArrived;
        _collision = KernelServices.Get<ICollisionWorld>();
        _residency.InitializeMaterials(KernelServices.Get<IToonMaterialProvider>(), KernelServices.Get<IShaderMaterialSource>());
        _reflections = KernelServices.Get<IReflectionWorld>(); _reflections.Changed += _residency.OnReflectionChanged;
        _interiors = new(_bake, _mount, _residency.Materials, _reflections, _residency.QueueRetirement, _captureMode);
    }
    public void OnMapLoaded(IMapContext context)
    {
        if (!IsUnloaded) throw new InvalidOperationException("Stream map must finish unloading before the next mount.");
        _mapGeneration++;
        _selection.BeginMapChange();
        _context = context; _failure = null; _failures.Clear(); _drain = null;
        _selection.Load(context); _residency.Load();
        var contextSource = KernelServices.Get<IBakeContextSource>();
        var firstModule = KernelServices.Services.Query<IContentModule>(RegistrationKind.Content, context.Descriptor.Id).FirstOrDefault();
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

    public Task DrainAsync() => _drain ??= DrainMap();

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
        _unloadingMap = null;
        if (Failure is { } failure) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void Stop(Action stop)
    {
        try { stop(); }
        catch (Exception error) { RecordFailure(error); }
    }
    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) => _selection.Prefetch(destination, mode, space);
    public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations) => _pins.SetPinnedDestinations(destinations);
    public override void _Process(double delta)
    {
        if (_exiting || _context is null || Failure is not null) return;
        try
        {
            _selection.PollRetired(); _pins.PollRetired();
            _preparation.HarvestCatalog();
            _pins.PollPinnedPreparation();
            _interiors?.Poll(Math.Max(0, _selection.Settings.MaximumPreparations - _preparation.Preparations - _interiors.Preparations));
            if (_interiors?.Failure is { } interiorError) RecordFailure(interiorError);
            _preparation.PollHandles();
            _participants.PollParticipantErrors();
            if (Failure is not null || _preparation.Catalog is null || _selection.Tree is null) return;
            _selection.Update(delta);
            _preparation.IssueQueued();
            _interiors?.AdvanceRetention(delta, _camera.Pose.Position);
        }
        catch (Exception error) { RecordFailure(error); }
    }
    public void FinishMountFrame(double remainingMilliseconds)
    {
        if (_context is null || _exiting || Failure is not null) return;
        _preparation.FinishMountFrame();
        _interiors?.Finish(GetViewport().GetWorld3D().Scenario, remainingMilliseconds >= .1);
        if (_checkRetention) { _checkRetention = false; _residency.RetireUnneeded(); }
        if (_selection.Tree is null || _preparation.Catalog is null || remainingMilliseconds < .1 || !_selection.PlanCurrent) return;
        _residency.RefreshReflectionWorld(remainingMilliseconds * .25);
        _selection.ApplyVisibility();
    }
    public void ReleaseRetired(double budgetMilliseconds)
    {
        if (_context is not null && !_exiting) _residency.ReleaseRetired(budgetMilliseconds);
    }
    private void OnWorldInstanceChanged(long _) => _selection.ResetWorld();
    internal void RequestRetention() => _checkRetention = true;
    internal void RecordFailure(Exception error)
    {
        if (error is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions) RecordFailure(inner);
            return;
        }
        if (_failures.Contains(error)) return;
        _failures.Add(error);
        _failure = _failures.Count == 1 ? error : new AggregateException("Stream failed.", _failures);
    }

    internal void Fail(StreamTile tile, Exception error, ParticipantTile? participant = null)
    {
        if (participant is not null) participant.Failure = error;
        else if (_residency.Groups.TryGetValue(tile, out var group)) group.Failure = error;
        RecordFailure(error);
    }

    internal bool CanUseLiveCamera() => _maps.Transition?.Stage switch
    {
        null or MapStage.Done => true,
        MapStage.LoadData or MapStage.Mount or MapStage.Arrival or MapStage.Spawn => false,
        _ => throw new InvalidOperationException("Unknown map transition stage.")
    };

    public override void _ExitTree()
    {
        // Registry awaited IMapLifetime.ShutdownAsync while the main thread and all bindings were still alive.
        // Exit callbacks only detach subscriptions; they never block or abandon asynchronous ownership.
        _exiting = true;
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        if (_reflections is not null) _reflections.Changed -= _residency.OnReflectionChanged;
        if (_maps is not null) _maps.Arrived -= _selection.OnMapArrived;
        _residency.DisposeMaterials();
    }
}
