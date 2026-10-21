using Campus.Kernel.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Bake;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Fauna;

/// <summary>Streamed animal owner tiles with per-site display latches (WLD-LIFE).</summary>
[KernelNode("systems.fauna")]
public partial class FaunaNode : Node, IMapAware, IViewContentReadiness, IViewCapturePreparation, IStreamTileParticipant
{
    private const double CheckSeconds = 0.25;
    private readonly FaunaTileMounts _tileMounts = new();
    private readonly N.Plane[] _planes = new N.Plane[6];
    private IWorldState? _world;
    private IRenderHost? _host;
    private IRenderReadiness? _renderReadiness;
    private IStreamMountQueue? _mount;
    private ICameraRig? _rig;
    private IReflectionWorld? _reflection;
    private long _captureEpoch;
    private double _sinceCheck;
    private bool _force = true;
    private Exception? _error;
    public StreamMountKind Kind => StreamMountKind.Auxiliary;
    public Exception? Failure => _error ?? _renderReadiness?.Failure;

    public IReadOnlyDictionary<string, FaunaSet?> Applied => _tileMounts.Tiles
        .SelectMany(tile => tile.Content.Content.Sites.Select((site, i) => (site.Key, tile.Latches[i].Set)))
        .ToDictionary(pair => pair.Key, pair => pair.Set, StringComparer.Ordinal);

    public bool CanMount(BakeKey key) => key.Module == BakeModuleIds.FaunaRuntime;

    public StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident)
        => FaunaDemand.Evaluate(in entry, in view, resident);

    public IStreamMountWork CreateMount(BakeKey key, object product, BakePriority priority, long revision)
    {
        if (!CanMount(key) || _tileMounts.Map != key.Map || product is not FaunaTileContent content || content.Key != key)
            throw new ArgumentException("Fauna mount product does not match its key or current map.");
        var request = BakeRequestKeys.Parse(key);
        if (request.Collision || request.Level != LodLevel.L0 || request.Tile != content.Tile)
            throw new ArgumentException("Fauna mount owner tile does not match its key.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var site in content.Content.Sites)
        {
            if (TileGrid.At(site.Bounds.Center) != content.Tile || site.Sets.Count != Enum.GetValues<FaunaSet>().Length || !keys.Add(site.Key))
                throw new ArgumentException("Fauna tile contains invalid site ownership or display states.");
        }
        return _tileMounts.CreateMount(content, priority, revision);
    }

    public bool IsReady(BakeKey key) => Failure is null && !_force && _tileMounts.TryGet(key, out var tile) && TileReady(tile);

    public bool IsReady(Pose pose)
    {
        if (Failure is not null || _tileMounts.Map is null || _world is not { Instance: > 0 } || _force ||
            _rig?.Pose != pose || GetViewport()?.GetCamera3D() is not { } camera) return false;
        CameraFrustum.Fill(camera, _planes);
        var view = new StreamDemandView(pose, _planes, ReadOnlyMemory<Box3>.Empty);
        var mirrored = _reflection!.TryGetDemand(in view, out var reflection);
        foreach (var tile in _tileMounts.Tiles)
        {
            if (!Required(tile, in view) && (!mirrored || !Required(tile, in reflection))) continue;
            if (!TileReady(tile)) return false;
        }
        return true;
    }

    private static bool Required(FaunaTile tile, in StreamDemandView view)
    {
        // Reuse the producer's demand; retained owner tiles outside the actual views do not block capture.
        foreach (var site in tile.Content.Content.Sites)
        {
            var entry = new BakeTileEntry(tile.Content.Key, site.Bounds);
            if (FaunaDemand.Evaluate(in entry, in view, resident: true) == StreamTileNeed.Required) return true;
        }
        return false;
    }

    private bool TileReady(FaunaTile tile)
    {
        if (_world is not { Instance: > 0 } || !tile.Mounted) return false;
        for (var i = 0; i < tile.Latches.Length; i++)
        {
            var latch = tile.Latches[i];
            if (!latch.Applied || latch.AppliedEpoch != _captureEpoch) return false;
            if ((_world!.Transition != WorldTransition.None || _world.Scrubbing) &&
                latch.Set != Target(tile.Content.Content.Sites[i])) return false;
            foreach (var handle in latch.Handles) if (!_host!.IsReady(handle)) return false;
        }
        return true;
    }

    public void PrepareCapture(Pose pose)
    {
        // Every requested still starts from the same target state, independent of the preceding still's latch history.
        _captureEpoch++;
        _force = true;
    }

    public override void _EnterTree() { ProcessPriority = FramePriority.Systems; _force = true; }

    public override void _Ready()
    {
        _world = KernelServices.Get<IWorldState>();
        _host = KernelServices.Get<IRenderHost>();
        _renderReadiness = KernelServices.Get<IRenderReadiness>();
        _mount = KernelServices.Get<IStreamMountQueue>();
        _rig = KernelServices.Get<ICameraRig>();
        _reflection = KernelServices.Get<IReflectionWorld>();
        _tileMounts.Initialize(_host, InitializeTile, VisibleNow, RecordFailure);
        _world.InstanceChanged += OnWorldInstanceChanged;
    }

    private void OnWorldInstanceChanged(long _)
    {
        _captureEpoch++;
        _force = true;
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        Release();
    }

    public void OnMapUnloading(MapId from) { Release(); _tileMounts.ClearMap(); }

    public void OnMapLoaded(IMapContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Release();
        _tileMounts.LoadMap(context.Descriptor.Id);
        _error = null;
        _force = true;
    }

    public override void _Process(double delta)
    {
        if (Failure is not null || _world is not { Instance: > 0 } || _tileMounts.Map is null) return;
        _sinceCheck += Math.Min(delta, 0.1);
        if (!_force && _sinceCheck < CheckSeconds) return;
        _sinceCheck = 0;
        try
        {
            var anywhere = _force || _world.Transition != WorldTransition.None || _world.Scrubbing;
            var hasView = false;
            var mirrored = false;
            StreamDemandView reflection = default;
            if (!anywhere && GetViewport()?.GetCamera3D() is { } camera)
            {
                var projection = camera.GetCameraProjection();
                var transform = camera.GetCameraTransform();
                CameraFrustum.Fill(transform, projection, _planes);
                hasView = true;
                var view = new StreamDemandView(_rig!.Pose, _planes, ReadOnlyMemory<Box3>.Empty);
                mirrored = _reflection!.TryGetDemand(in view, out reflection);
            }
            foreach (var tile in _tileMounts.Tiles)
            {
                for (var i = 0; i < tile.Latches.Length; i++)
                {
                    var site = tile.Content.Content.Sites[i];
                    var visible = !hasView || site.Bounds.Intersects(_planes) || mirrored && site.Bounds.Intersects(reflection.Frustum.Span);
                    Request(tile.Latches[i], Target(site), anywhere, visible);
                }
                if (tile.Work is null && NeedsWork(tile))
                {
                    tile.Work = _tileMounts.CreateUpdate(tile);
                    _mount!.Enqueue(tile.Work);
                }
            }
            _force = false;
        }
        catch (Exception error) { RecordFailure(error); }
    }

    // Both readiness consumers observe the original producer failure (ENG-PERF-STREAM).
    private void RecordFailure([FailureHandler("ENG-PERF-STREAM")] Exception error)
        => _error = RetainedFailure.Append(_error, error);

    private FaunaSet? Target(FaunaSiteContent site) =>
        FaunaSchedule.Target(site.Kind, _world!.Sun.AltDeg, _world.Clock, _world.WeatherNow);

    private void Request(FaunaLatch latch, FaunaSet? target, bool anywhere, bool visible)
    {
        if (latch.Applied && !anywhere && visible)
        {
            if (!latch.AllowVisible) latch.Requested = false;
            return;
        }
        if (latch.Applied && latch.Set == target && latch.AppliedEpoch == _captureEpoch && !latch.Requested) return;
        if (latch.Requested && latch.Desired == target && latch.Epoch == _captureEpoch)
        { latch.AllowVisible |= anywhere; return; }
        latch.Desired = target;
        latch.Epoch = _captureEpoch;
        latch.Version++;
        latch.Requested = true;
        latch.AllowVisible = anywhere;
    }

    private bool VisibleNow(Box3 bounds)
    {
        if (GetViewport()?.GetCamera3D() is not { } camera) return true;
        var projection = camera.GetCameraProjection();
        var transform = camera.GetCameraTransform();
        CameraFrustum.Fill(transform, projection, _planes);
        if (bounds.Intersects(_planes)) return true;
        var view = new StreamDemandView(_rig!.Pose, _planes, ReadOnlyMemory<Box3>.Empty);
        return _reflection!.TryGetDemand(in view, out var reflected) && bounds.Intersects(reflected.Frustum.Span);
    }

    private static bool NeedsWork(FaunaTile tile)
    {
        foreach (var latch in tile.Latches) if (!latch.Applied || latch.Requested) return true;
        return false;
    }

    private void InitializeTile(FaunaTile tile)
    {
        // Resource-only demand excludes fauna; an ended world cannot select display variants.
        if (_world is not { Instance: > 0 }) return;
        for (var i = 0; i < tile.Latches.Length; i++)
            Request(tile.Latches[i], Target(tile.Content.Content.Sites[i]), anywhere: true, visible: false);
    }

    public void Unmount(BakeKey key) => _tileMounts.Unmount(key);

    private void Release()
    {
        _tileMounts.Release();
        _sinceCheck = 0;
    }
}
