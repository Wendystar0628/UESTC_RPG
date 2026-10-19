using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Frame;
using Campus.Kernel.Lod;
using Campus.Kernel.Renderer;
using Campus.Kernel.Materials;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

/// <summary>Selection at -500; no instances or geometry are created in this phase.</summary>
public interface IStreamVisualFrame
{
    bool CanActivateVisual { get; }
    bool CaptureReady { get; }
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
    private readonly HashSet<StreamTile> _predictionRequired = [], _predictionRefined = [];
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
    public bool DestinationReady => Error is null && _context is not null && (_destinationConsumed || _destinationPlanned && _destination is { } destination && _catalog is not null &&
        _destinationRequired.All(IsReady) && (_interiors?.DestinationReady ?? true) && (_destinationMode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => _collision.Ready(destination.Position, WalkRules.ArrivalCollisionRadius),
            _ => throw new ArgumentOutOfRangeException(nameof(_destinationMode))
        }));
    private string? _error;
    public string? Error => _error ?? _collision?.Error;
    public int MountedTiles => _groups.Values.Count(group => group.Mounted);
    public int VisibleTiles => _visibility.Drawn.Count;
    public bool CanActivateVisual => _retiringBundles.Count == 0 && _detachPending.Count == 0;
    public bool CaptureReady
    {
        get
        {
            if (Error is not null || _context is null || _catalog is null || !_selectionApplied ||
                !_visibility.CaptureReady(FrameClock.FadeSeconds) ||
                !(_interiors?.CaptureReady(FrameClock.FadeSeconds, _settings.FadeSeconds) ?? false)) return false;
            foreach (var tile in _required)
                if (!IsReady(tile)) return false;
            return true;
        }
    }
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
        _replan = _settings.ReplanSeconds;
    }
    public void OnMapUnloading(MapId from)
    {
        // Detach the ENTIRE retiring scene before the first FreeRid. FreeRid itself flushes all dirty
        // instances; freeing one bundle at a time would initialize uniforms for the other doomed bundles.
        foreach (var bundle in _groups.Values.SelectMany(group => group.Bundles)
                     .Concat(_interiors?.Prepared ?? []).Concat(_retiringBundles).Distinct()) bundle.DetachAll();
        _mapGeneration++;
        _context = null; _error = null; _destination = null; _destinationState = null; _catalog = null; _tree = null;
        _selectionApplied = false;
        _destinationPlanned = false; _destinationConsumed = false;
        _destinationSequence++; _destinationTransition = null; _mapRequestSequence = _arrivedSequence = 0;
        _mount.RemoveMap(from);
        _interiors?.Unload();
        _requests.Clear(); _issued.Clear(); _required.Clear(); _refined.Clear();
        _destinationRequired.Clear(); _destinationRefined.Clear(); _visible.Clear(); _visibility.Clear();
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
        if (space is null && _context.World.Spatial.MediumAt(destination.Position) is { Kind: MediumKind.Interior, Feature: { } building } medium)
            space = new(building, checked((short)medium.Storey));
        _destination = destination; _destinationMode = mode; _destinationState = new(destination, mode, space); _error = null;
        _selectionApplied = false;
        _destinationPlanned = false; _destinationConsumed = false; _destinationRequired.Clear(); _destinationRefined.Clear();
        _destinationSequence++; _requests.Clear();
        var targetTransition = _maps.Transition is { Stage: MapStage.Mount or MapStage.Warm or MapStage.Spawn } transition ? transition : null;
        if (!ReferenceEquals(targetTransition, _destinationTransition))
        {
            _destinationTransition = targetTransition; _arrivedSequence = 0;
            _mapRequestSequence = targetTransition is null ? 0 : _destinationSequence;
        }
        _interiors!.Prefetch(space);
        _replan = _settings.ReplanSeconds;
    }
    public override void _Process(double delta)
    {
        if (_exiting) return;
        HarvestCatalog();
        _interiors?.Poll(Math.Max(0, _settings.MaximumPreparations - _preparing.Count - _interiors.Preparations));
        if (_interiors?.Error is { } interiorError) _error ??= interiorError;
        PollHandles();
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
            _pending.Add(new(request, handle, _mount.Revision, _groups[request.Tile]));
        }
        _interiors?.AdvanceRetention(delta, _camera.Pose.Position);
    }
    private void OnWorldInstanceChanged(long _)
    {
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
            if (_context is not null && _catalogGeneration == _mapGeneration) _catalog = task.Result;
        }
        else
        {
            var error = task.Exception?.GetBaseException().Message;
            if (_context is not null && _catalogGeneration == _mapGeneration)
                Fail(StreamTile.Root, "Module tile enumeration failed: " + error);
        }
    }
    private void Replan(bool exempt)
    {
        _selectionApplied = false;
        var tree = _tree ?? throw new InvalidOperationException("A map tree is required before planning.");
        var viewport = GetViewport();
        var height = viewport.GetVisibleRect().Size.Y * viewport.Scaling3DScale;
        var pose = _camera.Pose;
        var live = CanUseLiveCamera();
        _required.Clear(); _refined.Clear(); _required.Add(StreamTile.Root);
        if (live)
        {
            _interiors!.Select(_context!.World.Spatial.MediumAt(pose.Position));
            tree.Select(pose.Position, exempt ? 0 : _camera.Speed, height, pose.FovDeg ?? 60, _speed.Minimum, _required, _refined);
        }
        else if (_destination is { } mountingDestination)
        {
            pose = mountingDestination;
            _interiors!.Select(_context!.World.Spatial.MediumAt(pose.Position));
            tree.Select(pose.Position, 0, height, pose.FovDeg ?? 60, LodLevel.L0, _required, _refined);
        }
        if (live && !exempt)
            foreach (var seconds in _settings.PrefetchSeconds)
            {
                var map = _context!.Descriptor;
                var predicted = pose.Position + _velocity * seconds;
                predicted.Y = Math.Clamp(predicted.Y, map.Roam.FloorY, map.Roam.CeilY);
                var offset = new N.Vector2(predicted.X, predicted.Z) - map.Center;
                if (offset.Length() > map.Roam.MaxRadius)
                {
                    var limit = map.Center + N.Vector2.Normalize(offset) * map.Roam.MaxRadius;
                    predicted.X = limit.X; predicted.Z = limit.Y;
                }
                tree.Select(predicted, _camera.Speed, height, pose.FovDeg ?? 60, _speed.Minimum, _predictionRequired, _predictionRefined);
                _required.UnionWith(_predictionRequired);
            }
        if (!_destinationConsumed && _destination is { } destination)
        {
            tree.Select(destination.Position, 0, height, destination.FovDeg ?? 60, LodLevel.L0, _destinationRequired, _destinationRefined);
            _destinationPlanned = true;
        }
        var wanted = _required.Concat(_destinationRequired).Distinct().Order().ToArray();
        var requests = new List<StreamRequest>();
        foreach (var tile in wanted)
        {
            if (!_groups.TryGetValue(tile, out var group))
            {
                group = new(tile, _catalog!.Modules(tile).Count);
                if (group.Expected == 0) group.Mounted = true; // Empty quadrants are valid coverage, not missing files.
                _groups.Add(tile, group);
            }
            var priority = tile == StreamTile.Root || _destinationRequired.Contains(tile) ? BakePriority.KnownDestination : BakePriority.Normal;
            if (group.Error is not null && priority == BakePriority.KnownDestination) _error ??= group.Error;
            group.Priority = priority;
            var distance = LodTree.Distance(pose.Position, tree.Bounds(tile));
            var toTile = tree.Bounds(tile).Center - pose.Position;
            var approach = toTile.LengthSquared() > 0 ? N.Vector3.Dot(_velocity, N.Vector3.Normalize(toTile)) : 0;
            var radius = _settings.Distances[Math.Min((int)tile.Level, 3)];
            var neededIn = (distance - radius) / Math.Max(1, approach);
            foreach (var module in _catalog!.Modules(tile))
            {
                var key = new BakeKey(_context!.Descriptor.Id, module,
                    FormattableString.Invariant($"L{(int)tile.Level}/{tile.Tile.X}/{tile.Tile.Z}"));
                group.Keys.Add(key);
                if (!_issued.Contains(key)) requests.Add(new(key, tile, priority, neededIn));
                // Re-Ensure promotes an already issued background request without restarting it.
                else if (priority == BakePriority.KnownDestination) _bake.Ensure(key, priority);
            }
        }
        _requests.Replace(requests);
        _checkRetention = true;
    }
    private void PollHandles()
    {
        for (var i = 0; i < _pending.Count;)
        {
            var pending = _pending[i];
            if (pending.Handle.State is BakeState.Pending or BakeState.Running) { i++; continue; }
            if (!IsCurrent(pending.Request.Tile, pending.Group, pending.Revision)) { _pending.RemoveAt(i); continue; }
            if (pending.Handle.State == BakeState.Failed)
            {
                Fail(pending.Request.Tile, pending.Handle.Error ?? $"Bake failed: {pending.Request.Key}");
                _pending.RemoveAt(i); continue;
            }
            if (pending.Handle.State != BakeState.Ready) throw new InvalidDataException("Unknown bake state.");
            if (_preparing.Count + (_interiors?.Preparations ?? 0) >= _settings.MaximumPreparations) { i++; continue; }
            var bundle = pending.Handle.Tile;
            if (bundle is null || bundle.Key != pending.Request.Key || bundle.Tile != pending.Request.Tile.Tile || bundle.Level != pending.Request.Tile.Level)
            {
                Fail(pending.Request.Tile, $"Bake returned the wrong product for {pending.Request.Key}.");
                _pending.RemoveAt(i); continue;
            }
            _preparing.Add(new(pending.Request.Tile, pending.Revision, RunWorker(() => PreparedTile.Create(bundle)), pending.Group));
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
                if (IsCurrent(preparing.Tile, preparing.Group, preparing.Revision))
                    Fail(preparing.Tile, "GPU preparation failed: " + error);
                continue;
            }
            var prepared = preparing.Task.Result;
            if (!IsCurrent(preparing.Tile, preparing.Group, preparing.Revision))
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
        if (_tree is null || _catalog is null || remainingMilliseconds < .1) return;
        RefreshReflectionWorld(remainingMilliseconds * .25);
        _tree.ResolveVisible(_refined, IsReady, _visible);
        _visibility.Apply(_visible, FrameClock.FadeSeconds, true, (tile, visible, fade) =>
        {
            foreach (var bundle in _groups[tile].Bundles) bundle.SetVisible(visible, fade);
        });
        _selectionApplied = true;
    }
    private bool IsReady(StreamTile tile) => _groups.TryGetValue(tile, out var group) && group.Error is null && group.Mounted &&
        group.Bundles.All(bundle => bundle.Draws.All(_materials.IsReady));
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
        foreach (var tile in _groups.Keys.Where(tile => tile != StreamTile.Root && !_required.Contains(tile) &&
                     !_destinationRequired.Contains(tile) && !_visibility.Drawn.Contains(tile)).Order().ToArray())
        {
            var group = _groups[tile]; group.Work?.Abandon();
            foreach (var bundle in group.Bundles) QueueRetirement(bundle);
            foreach (var key in group.Keys) _issued.Remove(key);
            _groups.Remove(tile);
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
    private void OnReflectionChanged() => _reflectionDirty = true;
    private void RefreshReflectionWorld(double budgetMilliseconds)
    {
        var scenario = _reflections.Scenario;
        var material = scenario.IsValid ? _reflections.ProxyMaterial : null;
        if (material is not null && (_reflectionDirty || !ReferenceEquals(material, _reflectionMaterial)))
        {
            foreach (var group in _groups.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                foreach (var bundle in group.Bundles)
                    foreach (var draw in bundle.Draws.Where(draw => draw.Reflects)) _materials.TrackReflection(material, draw);
            if (_interiors is not null)
                foreach (var bundle in _interiors.Prepared)
                    foreach (var draw in bundle.Draws.Where(draw => draw.Reflects)) _materials.TrackReflection(material, draw);
        }
        var ready = material is not null && _materials.ReflectionReady(material);
        if (_reflectionDirty || scenario != _reflectionScenario || !ReferenceEquals(material, _reflectionMaterial) || ready != _reflectionReady)
        {
            _reflectionDirty = false; _reflectionScenario = scenario; _reflectionMaterial = material; _reflectionReady = ready;
            _reflectionRefresh.Clear(); _reflectionCursor = 0;
            foreach (var group in _groups.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                foreach (var bundle in group.Bundles) _reflectionRefresh.Enqueue(bundle);
            if (_interiors is not null) foreach (var bundle in _interiors.Prepared) _reflectionRefresh.Enqueue(bundle);
        }
        var start = FrameClock.MonotonicMilliseconds;
        while (_reflectionRefresh.TryPeek(out var bundle) && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds)
        {
            if (bundle.IsDisposed || bundle.IsRetiring || _reflectionCursor >= bundle.Instances.Count)
            {
                _reflectionRefresh.Dequeue(); _reflectionCursor = 0; continue;
            }
            var index = _reflectionCursor++;
            bundle.RefreshReflectionOne(index, scenario, material,
                material is not null && _materials.ReflectionReady(bundle.Draws[index], material));
        }
    }
    private void Fail(StreamTile tile, string error)
    {
        if (_groups.TryGetValue(tile, out var group)) group.Error = error;
        if (tile == StreamTile.Root || _destinationRequired.Contains(tile)) _error ??= error;
        GD.PushError(error);
    }
    private static async Task<ModuleTileCatalog> BuildCatalog(Task<IReadOnlyDictionary<string, IReadOnlyList<TileKey>>> tiles)
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
    private sealed record Pending(StreamRequest Request, IBakeHandle Handle, long Revision, TileGroup Group);
    private sealed record Preparing(StreamTile Tile, long Revision, Task<PreparedTile> Task, TileGroup Group);
    private sealed class TileGroup(StreamTile tile, int expected)
    {
        public StreamTile Tile { get; } = tile;
        public int Expected { get; } = expected;
        public List<PreparedTile> Bundles { get; } = [];
        public HashSet<BakeKey> Keys { get; } = [];
        public VisualMountWork? Work;
        public bool Queued, Mounted;
        public BakePriority Priority;
        public string? Error;
    }
}
