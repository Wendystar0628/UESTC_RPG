using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Frame;
using Campus.Kernel.Lod;
using Campus.Kernel.Renderer;
using Campus.Kernel.Materials;
using Campus.Kernel.Bake;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using Services = Campus.Kernel.Registry.Kernel;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

/// <summary>Selection at -500; no instances or geometry are created in this phase.</summary>
public interface IStreamVisualFrame
{
    bool CanActivateVisual { get; }
    bool ViewReady { get; }
    void FinishMountFrame(double remainingMilliseconds);
    void ReleaseRetired(double budgetMilliseconds);
}

[KernelService(typeof(IStreamVisualFrame), Owner = "WP-02b")]
[KernelService(typeof(IMapStreamState), Owner = "WP-02b")]
[KernelNode("stream.select", Owner = "WP-02b")]
public partial class StreamDirector : Node, IMapAware, IMapStreamState, IStreamVisualFrame
{
    private readonly StreamSettings _settings = new();
    private readonly LodSpeedPolicy _speed = new();
    private readonly StreamRequestQueue _requests = new();
    private readonly Dictionary<StreamTile, TileGroup> _groups = [];
    private readonly HashSet<StreamTile> _required = [], _refined = [], _destinationRequired = [], _destinationRefined = [], _visible = [];
    private readonly HashSet<StreamTile> _viewRequired = [], _displayRefined = [], _predictionRequired = [], _predictionRefined = [];
    private readonly List<Pending> _pending = [];
    private readonly List<Preparing> _preparing = [];
    private readonly HashSet<BakeKey> _issued = [];
    private TileVisibility _visibility = new(.25f);
    private IBakeService _bake = null!;
    private IStreamMountQueue _mount = null!;
    private ICameraRig _camera = null!;
    private IWorldState _world = null!;
    private ICollisionWorld _collision = null!;
    private IMapService _maps = null!;
    private ITerrainPreparation _terrain = null!;
    private TerrainPlan? _terrainPlan;
    private IMapContext? _plannedContext;
    private IWorldModel? _plannedWorld;
    private Pose _plannedPose;
    private bool _plannedLive, _planReady;
    private long _plannedSequence, _interiorDestinationSequence = -1;
    private StreamMaterials _materials = null!;
    private IReflectionWorld _reflections = null!;
    private readonly Queue<PreparedTile> _reflectionRefresh = [];
    private readonly Queue<PreparedTile> _retiringBundles = [];
    private readonly Queue<PreparedTile> _detachPending = [];
    private int _reflectionCursor;
    private bool _reflectionDirty = true, _reflectionReady;
    private Rid _reflectionScenario;
    private Material? _reflectionMaterial;
    private InteriorStream? _interiors;
    private IMapContext? _context;
    private LodTree? _tree;
    private ModuleTileCatalog? _catalog;
    private Task<ModuleTileCatalog>? _catalogTask;
    private long _mapGeneration, _catalogGeneration;
    private Pose? _destination;
    private ViewMode _destinationMode;
    private MapStreamDestination? _destinationState;
    private float _replan;
    private bool _exiting;
    private bool _destinationPlanned;
    private bool _destinationConsumed, _checkRetention;
    private long _destinationSequence, _mapRequestSequence, _arrivedSequence;
    private IMapTransition? _destinationTransition;
    private bool _haveCamera;
    private bool _selectionApplied;
    private N.Vector3 _lastCamera, _velocity;
    public MapStreamDestination? Destination => _destinationState;
    public bool HasPendingDestination => _destinationState is not null && !_destinationConsumed;
    public bool IsUnloaded => _context is null && _pending.Count == 0 && _preparing.Count == 0 && _catalogTask is null && _retiringBundles.Count == 0 && (_interiors?.IsUnloaded ?? true);
    public bool ResidentReady => Error is null && _context is not null && _catalog is not null &&
        _groups.TryGetValue(StreamTile.Root, out var root) && root.Error is null && root.Mounted;
    public bool DestinationReady => _destinationConsumed ? Error is null && _context is not null : _destinationPlanned && _destination is { } destination &&
        SelectionReady(_destinationRequired, _destinationRefined) && AuxiliaryReady(destination: true) && (_interiors?.DestinationReady ?? true) && (_destinationMode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => _collision.Ready(destination.Position, WalkRules.ArrivalCollisionRadius),
            _ => throw new ArgumentOutOfRangeException(nameof(_destinationMode))
        });
    private string? _error;
    public string? Error => _error ?? _collision?.Error;
    public int MountedTiles => _groups.Values.Count(group => group.Mounted);
    public int VisibleTiles => _visibility.Drawn.Count;
    public bool CanActivateVisual => _retiringBundles.Count == 0 && _detachPending.Count == 0;
    /// <summary>The current view uses the same complete selected-geometry barrier as a formal arrival.</summary>
    public bool ViewReady => SelectionReady(_viewRequired, _refined) && AuxiliaryReady(destination: false);
    /// <summary>Raw packed-buffer bytes from TileBundle stats; not compressed disk size or measured VRAM.</summary>
    public long ResidentBytes => _groups.Values.Sum(group => group.Bundles.Sum(bundle => bundle.Bytes));

    public override void _EnterTree()
    {
        _exiting = false;
        if (_world is not null) _world.InstanceChanged += OnWorldInstanceChanged;
        OnWorldInstanceChanged(0);
    }

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Stream;
        _bake = Services.Get<IBakeService>(); _mount = Services.Get<IStreamMountQueue>();
        _camera = Services.Get<ICameraRig>(); _world = Services.Get<IWorldState>();
        _world.InstanceChanged += OnWorldInstanceChanged;
        _maps = Services.Get<IMapService>(); // Resolve at node Ready, never from a constructor dependency cycle.
        _terrain = Services.Get<ITerrainPreparation>();
        _maps.Arrived += OnMapArrived;
        _collision = Services.Get<ICollisionWorld>();
        _materials = new(Services.Get<IToonMaterialProvider>(), Services.Get<IShaderMaterialSource>());
        _reflections = Services.Get<IReflectionWorld>(); _reflections.Changed += OnReflectionChanged;
        _interiors = new(_bake, _mount, _materials, _reflections, QueueRetirement);
    }
    public void OnMapLoaded(IMapContext context)
    {
        if (!IsUnloaded) throw new InvalidOperationException("Stream map must finish unloading before the next mount.");
        _mapGeneration++;
        CancelTerrainPlan(); _planReady = false; _interiorDestinationSequence = -1;
        _context = context; _error = null; _speed.Reset();
        _selectionApplied = false;
        _destinationConsumed = false;
        _haveCamera = false; _velocity = N.Vector3.Zero;
        _reflectionDirty = true;
        _tree = new(context.Descriptor.Bounds, _settings);
        _visibility = new(_settings.FadeSeconds);
        var contextSource = Services.Get<IBakeContextSource>();
        var firstModule = Services.Services.ForMap(context.Descriptor.Id.Value).FirstOrDefault(entry => entry.Kind == "content");
        _interiors!.Load(context, firstModule is null ? null : contextSource.GetPreparedContext(context, firstModule.Id));
        _catalogGeneration = _mapGeneration;
        _catalogTask = BuildCatalog(contextSource.GetPreparedModuleTilesAsync(context));
        LoadParticipants(context.Descriptor.Id);
        _replan = _settings.ReplanSeconds;
    }
    public void OnMapUnloading(MapId from)
    {
        // Detach the ENTIRE retiring scene before the first FreeRid. FreeRid itself flushes all dirty
        // instances; freeing one bundle at a time would initialize uniforms for the other doomed bundles.
        foreach (var bundle in _groups.Values.SelectMany(group => group.Bundles)
                     .Concat(_interiors?.Prepared ?? []).Concat(_retiringBundles).Distinct()) bundle.DetachAll();
        _mapGeneration++;
        CancelTerrainPlan(); _planReady = false; _interiorDestinationSequence = -1;
        SetPinnedDestinations(Array.Empty<MapStreamDestination>());
        _context = null; _error = null; _destination = null; _destinationState = null; _catalog = null; _tree = null;
        _plannedContext = null; _plannedWorld = null;
        _selectionApplied = false;
        _destinationPlanned = false; _destinationConsumed = false;
        _destinationSequence++; _destinationTransition = null; _mapRequestSequence = _arrivedSequence = 0;
        _mount.RemoveMap(from);
        UnloadAuxiliary();
        _interiors?.Unload();
        _requests.Clear(); _issued.Clear(); _required.Clear(); _viewRequired.Clear(); _refined.Clear();
        _destinationRequired.Clear(); _destinationRefined.Clear(); _displayRefined.Clear(); _visible.Clear(); _visibility.Clear();
        foreach (var group in _groups.Values) foreach (var bundle in group.Bundles) bundle.Dispose();
        _groups.Clear();
        while (_retiringBundles.TryDequeue(out var retired)) retired.Dispose();
        _detachPending.Clear();
        _reflectionRefresh.Clear(); _reflectionCursor = 0; _reflectionMaterial = null; _reflectionScenario = default;
        _materials.Dispose(); // All instances and meshes with assigned materials were released above.
        // Issued CPU jobs retain their world inputs until terminal; prepared GPU jobs are harvested at 900.
    }
    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null)
    {
        if (_context is null) throw new InvalidOperationException("A map must be mounted before destination prefetch.");
        _destination = destination; _destinationMode = mode; _destinationState = new(destination, mode, space); _error = null;
        _selectionApplied = false; _planReady = false; CancelTerrainPlan();
        _destinationPlanned = false; _destinationConsumed = false; _destinationRequired.Clear(); _destinationRefined.Clear();
        _destinationSequence++; _requests.Clear();
        var targetTransition = _maps.Transition is { Stage: MapStage.Mount or MapStage.Warm or MapStage.Spawn } transition ? transition : null;
        if (!ReferenceEquals(targetTransition, _destinationTransition))
        {
            _destinationTransition = targetTransition; _arrivedSequence = 0;
            _mapRequestSequence = targetTransition is null ? 0 : _destinationSequence;
        }
        _interiors?.Prefetch(null);
        _replan = _settings.ReplanSeconds;
    }
    public override void _Process(double delta)
    {
        if (_exiting) return;
        HarvestCatalog();
        PollPinnedPreparation();
        _interiors?.Poll(Math.Max(0, _settings.MaximumPreparations - _preparing.Count - _interiors.Preparations));
        if (_interiors?.Error is { } interiorError) _error ??= interiorError;
        PollHandles();
        PollAuxiliaryErrors();
        if (_context is null || _catalog is null || _tree is null) return;
        if (!_destinationConsumed && _destination is { } target && DestinationReady &&
            _camera.ActiveTransit is null && _camera.Mode == _destinationMode &&
            (_destinationTransition is not null ? _mapRequestSequence > 0 && _arrivedSequence == _mapRequestSequence &&
                (_destinationSequence == _mapRequestSequence || N.Vector3.DistanceSquared(_camera.Pose.Position, target.Position) <= 4) :
                CanUseLiveCamera() && N.Vector3.DistanceSquared(_camera.Pose.Position, target.Position) <= 4))
        {
            _destinationConsumed = true; _destinationRequired.Clear(); _destinationRefined.Clear();
            _interiors?.Prefetch(null); _replan = _settings.ReplanSeconds;
        }
        var step = (float)Math.Min(delta, .1);
        if (_haveCamera && step > 0)
        {
            var measured = (_camera.Pose.Position - _lastCamera) / step;
            var length = measured.Length();
            var maximum = Math.Max(1, _camera.Speed) * 1.5f;
            if (length > maximum) measured *= maximum / length;
            _velocity = N.Vector3.Lerp(_velocity, measured, step / (.3f + step));
        }
        _lastCamera = _camera.Pose.Position; _haveCamera = true;
        var exempt = _camera.Mode switch
        {
            ViewMode.Fly => _camera.ActiveTransit is not null,
            ViewMode.Walk => true,
            _ => throw new ArgumentOutOfRangeException(nameof(_camera.Mode))
        };
        _speed.Step(_camera.Speed, (float)Math.Min(delta, .1), exempt);
        _replan += (float)Math.Min(delta, .1);
        if (_replan >= _settings.ReplanSeconds)
        {
            _replan = 0; Replan(exempt);
        }
        // Ensure only queues work. Bake owns CPU concurrency and the three ResourceLoader slots.
        for (var sent = 0; sent < 64 && _requests.TryDequeue(out var request); sent++)
        {
            if (!_issued.Add(request.Key)) continue;
            var handle = _bake.Ensure(request.Key, request.Priority);
            if (request.PreloadOnly)
            {
                _pending.Add(new(request, handle, _mount.Revision, null, 0, null) { Pinned = true });
                continue;
            }
            var group = _groups[request.Tile];
            group.Auxiliary.TryGetValue(request.Key, out var auxiliary);
            _pending.Add(new(request, handle, _mount.Revision, group, group.GeometryRevision, auxiliary));
        }
        _interiors?.AdvanceRetention(delta, _camera.Pose.Position);
    }
    private void OnWorldInstanceChanged(long _)
    {
        CancelTerrainPlan(); _planReady = false;
        _selectionApplied = false;
        _speed.Reset();
        _replan = _settings.ReplanSeconds;
        _haveCamera = false;
        _velocity = N.Vector3.Zero;
    }

    private void HarvestCatalog()
    {
        if (_catalogTask is not { IsCompleted: true } task) return;
        _catalogTask = null;
        if (task.IsCompletedSuccessfully)
        {
            if (_context is not null && _catalogGeneration == _mapGeneration)
            {
                try { ValidateAuxiliary(task.Result); _catalog = task.Result; }
                catch (Exception error) { Fail(StreamTile.Root, error.GetBaseException().Message); }
            }
        }
        else
        {
            var error = task.Exception?.GetBaseException().Message;
            if (_context is not null && _catalogGeneration == _mapGeneration)
                Fail(StreamTile.Root, "Module tile enumeration failed: " + error);
        }
    }
    private void PollHandles()
    {
        for (var i = 0; i < _pending.Count;)
        {
            var pending = _pending[i];
            if (pending.Handle.State is BakeState.Pending or BakeState.Running) { i++; continue; }
            if (pending.Request.PreloadOnly)
            {
                if (pending.Pinned && _context?.Descriptor.Id == pending.Request.Key.Map && pending.Revision == _mount.Revision)
                { i++; continue; }
                if (_context?.Descriptor.Id == pending.Request.Key.Map && pending.Revision == _mount.Revision)
                    _issued.Remove(pending.Request.Key);
                _pending.RemoveAt(i); continue;
            }
            if (!IsCurrent(pending)) { _pending.RemoveAt(i); continue; }
            if (pending.Handle.State == BakeState.Failed)
            {
                Fail(pending.Request.Tile, pending.Handle.Error ?? $"Bake failed: {pending.Request.Key}", pending.Auxiliary);
                _pending.RemoveAt(i); continue;
            }
            if (pending.Handle.State != BakeState.Ready) throw new InvalidDataException("Unknown bake state.");
            if (pending.Auxiliary is { } auxiliary)
            {
                MountAuxiliary(pending, auxiliary);
                _pending.RemoveAt(i); continue;
            }
            if (_preparing.Count + (_interiors?.Preparations ?? 0) >= _settings.MaximumPreparations) { i++; continue; }
            var bundle = pending.Handle.Tile;
            if (bundle is null || bundle.Key != pending.Request.Key || bundle.Tile != pending.Request.Tile.Tile || bundle.Level != pending.Request.Tile.Level)
            {
                Fail(pending.Request.Tile, $"Bake returned the wrong product for {pending.Request.Key}.");
                _pending.RemoveAt(i); continue;
            }
            _preparing.Add(new(pending.Request.Tile, pending.Revision, RunWorker(() => PreparedTile.Create(bundle)), pending.Group!, pending.GeometryRevision));
            _pending.RemoveAt(i);
        }
    }
    public void FinishMountFrame(double remainingMilliseconds)
    {
        // This method is called only inside the 900 budget accounting, including stale-result disposal.
        for (var i = _preparing.Count - 1; i >= 0; i--)
        {
            var preparing = _preparing[i];
            if (!preparing.Task.IsCompleted) continue;
            _preparing.RemoveAt(i);
            if (!preparing.Task.IsCompletedSuccessfully)
            {
                var error = preparing.Task.Exception?.GetBaseException().Message;
                if (IsCurrentGeometry(preparing.Tile, preparing.Group, preparing.Revision, preparing.GeometryRevision))
                    Fail(preparing.Tile, "GPU preparation failed: " + error);
                continue;
            }
            var prepared = preparing.Task.Result;
            if (!IsCurrentGeometry(preparing.Tile, preparing.Group, preparing.Revision, preparing.GeometryRevision))
            {
                QueueRetirement(prepared); continue;
            }
            var group = _groups[preparing.Tile]; group.Bundles.Add(prepared);
            if (group.Bundles.Count == group.Expected && !group.Queued)
            {
                group.Queued = true;
                group.Work = new VisualMountWork(group.Bundles, _mount.Revision, GetViewport().GetWorld3D().Scenario,
                    group.Priority, _materials, _reflections, () => group.Mounted = true, () => group.Mounted = false,
                    reason => Fail(group.Tile, reason));
                _mount.Enqueue(group.Work);
            }
        }
        _interiors?.Finish(GetViewport().GetWorld3D().Scenario, remainingMilliseconds >= .1);
        if (_checkRetention) { _checkRetention = false; RetireUnneeded(); }
        if (_tree is null || _catalog is null || remainingMilliseconds < .1 || !PlanCurrent) return;
        RefreshReflectionWorld(remainingMilliseconds * .25);
        ResolveVisible();
        // Capture uses the existing no-fade path: new tiles get fade_t0=0 and old tiles hide immediately.
        _visibility.Apply(_visible, FrameClock.FadeSeconds, !AppBoot.Options.CaptureMode, (tile, visible, fade) =>
        {
            foreach (var bundle in _groups[tile].Bundles) bundle.SetVisible(visible, fade);
        });
        _selectionApplied = true;
    }
    private bool PlanCurrent => _planReady && _context is not null && ReferenceEquals(_plannedContext, _context) &&
        ReferenceEquals(_plannedWorld, _context.World) && _plannedSequence == _destinationSequence && _plannedLive == CanUseLiveCamera() &&
        _plannedPose == (_plannedLive ? _camera.Pose : _destination ?? _camera.Pose);

    private bool PreparePlanningTerrain(Pose pose, bool live)
    {
        var context = _context!;
        var same = _terrainPlan is { } pending && ReferenceEquals(pending.Context, context) && ReferenceEquals(pending.World, context.World) &&
            pending.Generation == _mapGeneration && pending.Sequence == _destinationSequence && pending.Pose == pose && pending.Live == live;
        if (!same) CancelTerrainPlan();
        var points = new List<N.Vector2>(2);
        if (live || _destination is not null) points.Add(new(pose.Position.X, pose.Position.Z));
        if (!_destinationConsumed && _destination is { } destination)
        {
            var target = new N.Vector2(destination.Position.X, destination.Position.Z);
            if (!points.Contains(target)) points.Add(target);
        }
        if (points.All(point => _terrain.IsPrepared(context.World, point))) { CancelTerrainPlan(); return true; }
        _planReady = false;
        if (_terrainPlan is null)
        {
            var cancellation = new CancellationTokenSource();
            var product = "stream/" + string.Join("/", points.Select(point => FormattableString.Invariant($"{point.X:R}/{point.Y:R}")));
            _terrainPlan = new(context, context.World, _mapGeneration, _destinationSequence, pose, live,
                _terrain.PreparePoints(context, product, points, cancellation.Token), cancellation);
            _requests.Clear();
        }
        if (!_terrainPlan.Preparation.IsCompleted) return false;
        try { _terrainPlan.Preparation.GetAwaiter().GetResult(); }
        catch (Exception error) { _error = error.GetBaseException().Message; CancelTerrainPlan(); return false; }
        if (!points.All(point => _terrain.IsPrepared(context.World, point)))
            throw new InvalidOperationException("Terrain preparation completed without the current planning points.");
        CancelTerrainPlan(); return true;
    }
    private void CancelTerrainPlan()
    {
        if (_terrainPlan is not { } pending) return;
        _terrainPlan = null;
        pending.Cancellation.Cancel(); pending.Cancellation.Dispose();
        _ = ObserveDiscardedTerrain(pending.Preparation);
    }
    private static async Task ObserveDiscardedTerrain(Task preparation)
    { try { await preparation.ConfigureAwait(false); } catch (Exception) { } }
    private bool IsReady(StreamTile tile) => _groups.TryGetValue(tile, out var group) && group.GeometryRequested && group.Error is null && group.Mounted &&
        group.Bundles.All(bundle => bundle.Draws.All(_materials.IsReady));
    private void ResolveVisible()
    {
        _displayRefined.Clear(); _displayRefined.UnionWith(_refined);
        // An active arrival must be presentable even when its required detail differs from the current eye.
        // Predictions and CPU pins schedule products but never refine the displayed frontier.
        if (!_destinationConsumed && _destinationPlanned) _displayRefined.UnionWith(_destinationRefined);
        _tree!.ResolveVisible(_displayRefined, IsReady, _visible);
    }
    private bool SelectionReady(IReadOnlySet<StreamTile> required, IReadOnlySet<StreamTile> refined)
    {
        if (Error is not null || _context is null || _catalog is null || !_selectionApplied || !PlanCurrent ||
            _tree is null || required.Count == 0 || !_visibility.IsSettled(FrameClock.FadeSeconds)) return false;
        foreach (var tile in required)
            if (!refined.Contains(tile) && !HasVisibleCoverage(tile)) return false;
        return true;
    }
    private bool HasVisibleCoverage(StreamTile tile)
    {
        if (_visibility.Drawn.Contains(tile) && IsReady(tile)) return true;
        var children = _tree!.Children(tile);
        if (children.Count == 0) return false;
        foreach (var child in children)
            if (!HasVisibleCoverage(child)) return false;
        return true;
    }
    private bool IsCurrent(StreamTile tile, TileGroup group, long revision) => _context is not null && revision == _mount.Revision &&
        _groups.TryGetValue(tile, out var current) && ReferenceEquals(current, group);
    private bool CanUseLiveCamera() => _maps.Transition?.Stage switch
    {
        null or MapStage.Done => true,
        MapStage.LoadData or MapStage.Mount or MapStage.Warm or MapStage.Spawn => false,
        _ => throw new InvalidOperationException("Unknown map transition stage.")
    };
    private void OnMapArrived(MapId map, MapTravelCause cause)
    {
        // MapService emits this only after a successful Spawn/mode completion. The captured operation and
        // request sequence prevent an earlier arrival from consuming a later Prefetch request.
        if (_destinationTransition is { Stage: MapStage.Spawn or MapStage.Done } transition &&
            transition.To == map && transition.Cause == cause && _mapRequestSequence > 0)
            _arrivedSequence = _mapRequestSequence;
    }
    private void RetireUnneeded()
    {
        RetireAuxiliary();
        foreach (var tile in _groups.Keys.Where(tile => tile != StreamTile.Root && !_required.Contains(tile) &&
                     !_destinationRequired.Contains(tile) && !_visibility.Drawn.Contains(tile)).Order().ToArray())
        {
            var group = _groups[tile];
            if (!group.GeometryRequested && group.Auxiliary.Count > 0) continue;
            group.Work?.Abandon(); group.Work = null;
            foreach (var bundle in group.Bundles) QueueRetirement(bundle);
            foreach (var key in group.Keys) _issued.Remove(key);
            group.Bundles.Clear(); group.Keys.Clear(); group.Expected = 0;
            group.Queued = group.Mounted = group.GeometryRequested = false; group.GeometryRevision++;
            group.Error = null;
            if (group.Auxiliary.Count == 0) _groups.Remove(tile);
        }
    }
    private void QueueRetirement(PreparedTile bundle)
    {
        _materials.Forget(bundle.Draws); bundle.BeginRetirement();
        _retiringBundles.Enqueue(bundle); _detachPending.Enqueue(bundle);
    }
    public void ReleaseRetired(double budgetMilliseconds)
    {
        var start = FrameClock.MonotonicMilliseconds;
        while (_detachPending.TryPeek(out var pending) && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds)
            if (pending.DetachOne()) _detachPending.Dequeue();
        if (_detachPending.Count > 0) return;
        while (_retiringBundles.TryPeek(out var bundle) && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds)
            if (bundle.ReleaseOne()) _retiringBundles.Dequeue();
    }
    private void Fail(StreamTile tile, string error, AuxiliaryTile? auxiliary = null)
    {
        if (auxiliary is not null) auxiliary.Error = error;
        else if (_groups.TryGetValue(tile, out var group)) group.Error = error;
        if (auxiliary is not null ? auxiliary.DestinationRequired : tile == StreamTile.Root || _destinationRequired.Contains(tile)) _error ??= error;
        GD.PushError(error);
    }
    private static async Task<ModuleTileCatalog> BuildCatalog(Task<PreparedBakeTiles> tiles)
    {
        var index = await tiles.ConfigureAwait(false);
        return await RunWorker(() => ModuleTileCatalog.Build(index)).ConfigureAwait(false);
    }
    private static Task<T> RunWorker<T>(Func<T> work) => Task.Factory.StartNew(() =>
    {
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
        return work();
    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    public override void _ExitTree()
    {
        CancelTerrainPlan(); _planReady = false;
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        _exiting = true;
        if (_context is { } context) OnMapUnloading(context.Descriptor.Id);
        if (_catalogTask is { } catalog)
        {
            _catalogTask = null;
            _ = ObserveDiscardedCatalog(catalog);
        }
        // Shutdown is under the veil and is allowed to wait; no GPU task survives engine teardown.
        foreach (var preparing in _preparing)
            try { preparing.Task.GetAwaiter().GetResult().Dispose(); } catch (Exception error) { GD.PushError(error.Message); }
        _preparing.Clear();
        foreach (var retiringBundle in _retiringBundles) retiringBundle.DetachAll();
        while (_retiringBundles.TryDequeue(out var retired)) retired.Dispose();
        _detachPending.Clear();
        _interiors?.Shutdown();
        if (_reflections is not null) _reflections.Changed -= OnReflectionChanged;
        if (_maps is not null) _maps.Arrived -= OnMapArrived;
        _materials?.Dispose();
    }
    private static async Task ObserveDiscardedCatalog(Task<ModuleTileCatalog> catalog)
    {
        // Pure CPU results need no native release. Observe a late fault without touching an exited node or engine.
        try { await catalog.ConfigureAwait(false); }
        catch (Exception) { }
    }
    private sealed record Pending(StreamRequest Request, IBakeHandle Handle, long Revision, TileGroup? Group,
        long GeometryRevision, AuxiliaryTile? Auxiliary)
    { public bool Pinned { get; set; } }
    private sealed record Preparing(StreamTile Tile, long Revision, Task<PreparedTile> Task, TileGroup Group, long GeometryRevision);
    private sealed record TerrainPlan(IMapContext Context, IWorldModel World, long Generation, long Sequence,
        Pose Pose, bool Live, Task Preparation, CancellationTokenSource Cancellation);
    private sealed class TileGroup(StreamTile tile)
    {
        public StreamTile Tile { get; } = tile;
        public int Expected;
        public bool GeometryRequested;
        public long GeometryRevision;
        public Dictionary<BakeKey, AuxiliaryTile> Auxiliary { get; } = [];
        public List<PreparedTile> Bundles { get; } = [];
        public HashSet<BakeKey> Keys { get; } = [];
        public VisualMountWork? Work;
        public bool Queued, Mounted;
        public BakePriority Priority;
        public string? Error;
    }
}
