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
    private readonly Dictionary<BakeKey, TileState> _tiles = [];
    private readonly N.Plane[] _planes = new N.Plane[6];
    private IWorldState? _world;
    private IRenderHost? _host;
    private IRenderReadiness? _renderReadiness;
    private IStreamMountQueue? _mount;
    private ICameraRig? _rig;
    private IReflectionWorld? _reflection;
    private MapId? _map;
    private long _generation;
    private long _captureEpoch;
    private double _sinceCheck;
    private bool _force = true;
    private string? _error;
    public StreamMountKind Kind => StreamMountKind.Auxiliary;
    public string? Error => _error ?? _renderReadiness?.Error;

    private sealed class Latch
    {
        public bool Applied;
        public FaunaSet? Set;
        public FaunaSet? Desired;
        public bool Requested;
        public long Epoch;
        public long AppliedEpoch = -1;
        public long Version;
        public bool AllowVisible;
        public readonly List<RenderHandle> Handles = [];
    }

    private sealed class TileState(FaunaTileContent content, BakePriority priority, long revision, long generation)
    {
        public readonly FaunaTileContent Content = content;
        public readonly BakePriority Priority = priority;
        public readonly long Revision = revision;
        public readonly long Generation = generation;
        public readonly Latch[] Latches = content.Content.Sites.Select(_ => new Latch()).ToArray();
        public TileWork? Work;
        public bool Mounted;
    }

    public IReadOnlyDictionary<string, FaunaSet?> Applied => _tiles.Values
        .SelectMany(tile => tile.Content.Content.Sites.Select((site, i) => (site.Key, tile.Latches[i].Set)))
        .ToDictionary(pair => pair.Key, pair => pair.Set, StringComparer.Ordinal);

    public bool CanMount(BakeKey key) => key.Module == BakeModuleIds.FaunaRuntime;

    public StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident)
        => FaunaDemand.Evaluate(in entry, in view, resident);

    public IStreamMountWork CreateMount(BakeKey key, object product, BakePriority priority, long revision)
    {
        if (!CanMount(key) || _map != key.Map || product is not FaunaTileContent content || content.Key != key)
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
        var tile = new TileState(content, priority, revision, _generation);
        return tile.Work = new TileWork(this, tile, initial: true);
    }

    public bool IsReady(BakeKey key) => Error is null && !_force && _tiles.TryGetValue(key, out var tile) && TileReady(tile);

    public bool IsReady(Pose pose)
    {
        if (Error is not null || _map is null || _force) return false;
        foreach (var tile in _tiles.Values) if (!TileReady(tile)) return false;
        return true;
    }

    private bool TileReady(TileState tile)
    {
        if (!tile.Mounted) return false;
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
        _world.InstanceChanged += OnWorldInstanceChanged;
    }

    private void OnWorldInstanceChanged(long _) { _captureEpoch++; _force = true; }

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        Release();
    }

    public void OnMapUnloading(MapId from) { Release(); _map = null; }

    public void OnMapLoaded(IMapContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Release();
        _map = context.Descriptor.Id;
        _error = null;
        _force = true;
    }

    public override void _Process(double delta)
    {
        if (Error is not null || _world is null || _map is null) return;
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
            foreach (var tile in _tiles.Values)
            {
                for (var i = 0; i < tile.Latches.Length; i++)
                {
                    var site = tile.Content.Content.Sites[i];
                    var visible = !hasView || site.Bounds.Intersects(_planes) || mirrored && site.Bounds.Intersects(reflection.Frustum.Span);
                    Request(tile.Latches[i], Target(site), anywhere, visible);
                }
                if (tile.Work is null && NeedsWork(tile))
                {
                    tile.Work = new TileWork(this, tile, initial: false);
                    _mount!.Enqueue(tile.Work);
                }
            }
            _force = false;
        }
        catch (Exception error) { _error = error.GetBaseException().Message; }
    }

    private FaunaSet? Target(FaunaSiteContent site) =>
        FaunaSchedule.Target(site.Kind, _world!.Sun.AltDeg, _world.Clock, _world.WeatherNow);

    private void Request(Latch latch, FaunaSet? target, bool anywhere, bool visible)
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

    private static bool NeedsWork(TileState tile)
    {
        foreach (var latch in tile.Latches) if (!latch.Applied || latch.Requested) return true;
        return false;
    }

    public void Unmount(BakeKey key)
    {
        if (!_tiles.Remove(key, out var tile)) return;
        tile.Work?.Cancel();
        Free(tile);
    }

    private void Free(TileState tile)
    {
        tile.Mounted = false;
        foreach (var latch in tile.Latches)
        {
            foreach (var handle in latch.Handles) _host!.Unmount(handle);
            latch.Handles.Clear();
            latch.Applied = false;
            latch.Requested = false;
        }
    }

    private void Release()
    {
        _generation++;
        foreach (var tile in _tiles.Values) { tile.Work?.Cancel(); Free(tile); }
        _tiles.Clear();
        _sinceCheck = 0;
    }
}
