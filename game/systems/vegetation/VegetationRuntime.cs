using Campus.Kernel.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Stream;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Contracts.Vegetation;
using Campus.Kernel.Instances;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Vegetation;

/// <summary>Tile payload owner. StreamDirector alone requests, schedules and retires each tree/grass tile.</summary>
[KernelNode("systems.vegetation")]
public sealed partial class VegetationRuntime : Node, IMapAware, IViewContentReadiness, IViewCapturePreparation, IStreamTileParticipant
{
    private static readonly StringName _shaderInstanceShadowProxy = InstanceShaderParameters.ShadowProxy;
    private const uint VisibleLayer = RenderLayers.Vegetation;
    private const uint ProxyLayer = RenderLayers.ShadowProxy;
    private const double Interval = 0.1;
    private readonly Dictionary<BakeKey, MountedTile> _tiles = [];
    private IInstancePoolHost? _host;
    private ICameraRig? _rig;
    private IWorldState? _world;
    private MapId? _map;
    private long _generation;
    private double _since = Interval;
    private bool _worldReset = true;
    private Pose? _capture;
    public StreamMountKind Kind => StreamMountKind.Auxiliary;
    public Exception? Failure { get; private set; }
    public int Trees => _tiles.Values.Sum(t => t.Mothers.Sum(m => m.Buckets.Count));
    public int Pools => _tiles.Values.Sum(t => t.Mothers.Length * Mother.PoolCount + t.Grass.Length);

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        _host = KernelServices.Get<IInstancePoolHost>();
        _rig = KernelServices.Get<ICameraRig>();
        _world = KernelServices.Get<IWorldState>();
        _world.InstanceChanged += OnWorldInstanceChanged;
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        FreeAll();
    }

    private void OnWorldInstanceChanged(long _)
    {
        _worldReset = true;
        foreach (var tile in _tiles.Values) tile.AppliedEye = null;
    }

    public void OnMapLoaded(IMapContext context)
    {
        FreeAll();
        Failure = null;
        _map = context.Descriptor.Id;
    }

    public void OnMapUnloading(MapId from)
    {
        FreeAll();
        _map = null;
    }

    public bool CanMount(BakeKey key) => key.Module is BakeModuleIds.VegetationTrees or BakeModuleIds.VegetationGrass;

    public StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident)
    {
        if (!CanMount(entry.Key)) return StreamTileNeed.None;
        var reach = entry.Key.Module == BakeModuleIds.VegetationGrass ? GrassPool.Reach : VegFade.NearFadeEnd;
        // Full near exit envelope; no view-frustum filter, because offscreen near trees still cast visible shadows.
        var eye = view.Eye.Position;
        var d = entry.Key.Module == BakeModuleIds.VegetationGrass
            ? MathF.Sqrt(entry.Bounds.HorizontalDistanceSquared(eye)) : MathF.Sqrt(entry.Bounds.DistanceSquared(eye));
        if (d <= reach) return StreamTileNeed.Required;
        return resident && entry.Key.Module == BakeModuleIds.VegetationTrees && d <= reach * VegetationBuckets.Hysteresis
            ? StreamTileNeed.Retain : StreamTileNeed.None;
    }

    public IStreamMountWork CreateMount(BakeKey key, object product, BakePriority priority, long revision)
    {
        if (!CanMount(key) || key.Map != _map || product is not VegetationTileContent content || content.Key != key ||
            key.Detail != FormattableString.Invariant($"L0/{content.Tile.X}/{content.Tile.Z}"))
            throw new ArgumentException("Vegetation payload does not match its tile request.");
        foreach (var mother in content.Mothers)
        {
            var grass = key.Module == BakeModuleIds.VegetationGrass;
            if (mother.Prototypes.Count != (grass ? 1 : Mother.PoolCount) ||
                mother.Records.Length != mother.Bounds.Count * InstanceStreamLayout.FloatsPerInstance ||
                grass != mother.Key.StartsWith(GrassPool.KeyPrefix, StringComparison.Ordinal))
                throw new ArgumentException("Vegetation payload has an invalid mother layout.");
        }
        return new MountWork(this, content, priority, revision, _generation);
    }

    public bool IsReady(BakeKey key) => Failure is null && _tiles.TryGetValue(key, out var tile) && tile.Ready &&
        (_rig is null || !tile.NeededAt(_rig.Pose.Position) || tile.AppliedEye == _rig.Pose.Position);

    public bool IsReady(Pose pose)
    {
        if (Failure is not null || _map is null) return false;
        foreach (var tile in _tiles.Values)
            if (tile.NeededAt(pose.Position) && (!tile.Ready || tile.AppliedEye != pose.Position)) return false;
        return true;
    }

    public void PrepareCapture(Pose pose) => _capture = pose;

    public void Unmount(BakeKey key)
    {
        if (_tiles.Remove(key, out var tile)) tile.Free();
    }

    public override void _Process(double delta)
    {
        if (_map is null || _rig is null || Failure is not null) return;
        try
        {
            var eye = (_capture ?? _rig.Pose).Position;
            var reset = _worldReset || _capture is not null;
            _since += delta;
            if (!reset && _since < Interval) return;
            _since = 0;
            foreach (var tile in _tiles.Values)
            {
                if (!tile.NeededAt(eye)) continue;
                foreach (var mother in tile.Mothers) mother.Refresh(eye, reset || tile.AppliedEye is null);
                foreach (var grass in tile.Grass) grass.Refresh(eye, reset || tile.AppliedEye is null);
                tile.AppliedEye = eye;
            }
            _worldReset = false;
            _capture = null;
        }
        catch (Exception error) { RecordFailure(error); }
    }

    // StreamDirector and view readiness publish this same producer failure (ENG-PERF-STREAM).
    private void RecordFailure([FailureHandler("ENG-PERF-STREAM")] Exception error)
        => Failure = RetainedFailure.Append(Failure, error);

    private void FreeAll()
    {
        _generation++;
        foreach (var tile in _tiles.Values) tile.Free();
        _tiles.Clear();
        _worldReset = true;
        _capture = null;
        _since = Interval;
    }

    private sealed class MountedTile
    {
        private readonly Box3[] _bounds;
        public MountedTile(VegetationTileContent content)
        {
            var id = content.Key.Detail.Replace('/', '.');
            Mothers = content.Key.Module == BakeModuleIds.VegetationTrees ? content.Mothers.Select(d => new Mother(d, id)).ToArray() : [];
            Grass = content.Key.Module == BakeModuleIds.VegetationGrass ? content.Mothers.Select(d => new GrassPool(d, id)).ToArray() : [];
            _bounds = content.Mothers.SelectMany(d => d.Bounds).ToArray();
        }
        public Mother[] Mothers { get; }
        public GrassPool[] Grass { get; }
        public N.Vector3? AppliedEye { get; set; }
        public bool Ready
        {
            get
            {
                foreach (var mother in Mothers) if (!mother.Ready) return false;
                foreach (var grass in Grass) if (!grass.Ready) return false;
                return true;
            }
        }
        public bool NeededAt(N.Vector3 eye)
        {
            var reach = Grass.Length > 0 ? GrassPool.Reach : VegFade.NearFadeEnd * VegetationBuckets.Hysteresis;
            foreach (var bounds in _bounds)
            {
                var distance = Grass.Length > 0 ? MathF.Sqrt(bounds.HorizontalDistanceSquared(eye)) :
                    MathF.Sqrt(bounds.DistanceSquared(eye));
                if (distance <= reach) return true;
            }
            return false;
        }
        public void Free()
        {
            foreach (var mother in Mothers) mother.Free();
            foreach (var grass in Grass) grass.Free();
        }
    }

    private sealed class MountWork : StreamMountWork
    {
        private readonly VegetationRuntime _owner;
        private readonly long _generation;
        private readonly MountedTile _tile;
        private int _unit;
        public MountWork(VegetationRuntime owner, VegetationTileContent content, BakePriority priority, long revision, long generation)
            : base(content.Key, priority, revision, StreamMountKind.Auxiliary)
        {
            _owner = owner;
            _generation = generation;
            _tile = new(content);
        }
        protected override bool IsCurrent => _owner._generation == _generation && _owner._map == Key.Map;
        protected override bool MountStep(double budgetMilliseconds)
        {
            var host = _owner._host ?? throw new InvalidOperationException("Vegetation pool host is unavailable.");
            var treeUnits = _tile.Mothers.Length * Mother.PoolCount;
            // At most one native pool upload in a queue step.
            if (_unit < treeUnits) _tile.Mothers[_unit / Mother.PoolCount].Mount(host, Key.Map, _unit % Mother.PoolCount);
            else if (_unit < treeUnits + _tile.Grass.Length) _tile.Grass[_unit - treeUnits].Mount(host, Key.Map);
            _unit++;
            if (_unit < treeUnits + _tile.Grass.Length) return false;
            if (!_owner._tiles.TryAdd(Key, _tile)) throw new InvalidOperationException("Vegetation tile is already mounted.");
            _owner._since = Interval;
            return true;
        }
        protected override void Release() => _tile.Free();
        protected override void Failed(Exception error) => _owner.RecordFailure(error);
    }

    /// <summary>One mother model's pools: its trees' records, their buckets and the kernel pools fed from them.</summary>
    private sealed class Mother
    {
        public const int PoolCount = 4;
        private static readonly VegPool[] Order = [VegPool.L0Plus, VegPool.L0, VegPool.L1, VegPool.Shadow];
        private const int Stride = InstanceStreamLayout.FloatsPerInstance;

        private readonly string _key;
        public bool Ready
        {
            get
            {
                foreach (var pool in _pools) if (pool is not { IsReady: true }) return false;
                return true;
            }
        }
        private readonly float[] _records;
        private readonly GeometryPrototype[] _prototypes;
        private readonly Box3[] _bounds;
        private readonly IInstancePool?[] _pools = new IInstancePool?[PoolCount];
        private readonly float[] _scratch;
        private readonly int[] _members;

        /// <summary>Bounds of a pool holding no tree: far below the ground, outside every camera and shadow cascade.</summary>
        private static readonly Aabb Parked = new(new Vector3(0f, -100000f, 0f), Vector3.One);

        public Mother(VegetationMotherData data, string tileId)
        {
            _key = tileId + "." + data.Key;
            var records = data.Records.ToArray();
            var prototypes = data.Prototypes.ToArray();
            _records = records;
            _prototypes = prototypes;
            Buckets = new VegetationBuckets(records);
            _scratch = new float[records.Length];
            _members = new int[Buckets.Count];
            _bounds = data.Bounds.ToArray();
        }

        public VegetationBuckets Buckets { get; }

        public void Mount(IInstancePoolHost host, MapId map, int p)
        {
            var kind = Order[p];
            var shadow = kind == VegPool.Shadow;
            var spec = new GeometryInstancePoolSpec($"vegetation.{_key}.{kind}", _prototypes[p], Buckets.Count, Parked,
                shadow ? ProxyLayer : VisibleLayer,
                shadow ? GeometryInstance3D.ShadowCastingSetting.ShadowsOnly : GeometryInstance3D.ShadowCastingSetting.Off)
            { Map = map };
            var pool = host.Mount(spec);
            _pools[p] = pool;
            pool.WriteBuffer(_scratch);
            if (shadow) pool.SetParameter(_shaderInstanceShadowProxy, 1f);
        }

        public void Refresh(N.Vector3 eye, bool reset)
        {
            if (!Buckets.Update(eye, reset) && !reset) return;
            for (var p = 0; p < PoolCount; p++)
            {
                if (_pools[p] is not { } pool) continue;
                var count = 0;
                var min = new N.Vector3(float.MaxValue);
                var max = new N.Vector3(float.MinValue);
                for (var i = 0; i < Buckets.Count; i++)
                {
                    var level = Buckets.Level(i);
                    if (!(Order[p] == VegPool.Shadow ? level <= (byte)VegPool.L1 : level == (byte)Order[p])) continue;
                    _members[count++] = i;
                    min = N.Vector3.Min(min, _bounds[i].Min);
                    max = N.Vector3.Max(max, _bounds[i].Max);
                }
                pool.VisibleCount = 0;
                if (count == 0) { pool.SetAabb(Parked); continue; }
                Array.Clear(_scratch);
                for (var k = 0; k < count; k++)
                    _records.AsSpan(_members[k] * Stride, Stride).CopyTo(_scratch.AsSpan(k * Stride, Stride));
                pool.WriteBuffer(_scratch);
                var size = max - min;
                pool.SetAabb(new Aabb(new Vector3(min.X, min.Y, min.Z), new Vector3(size.X, size.Y, size.Z)));
                pool.VisibleCount = count;
            }
        }

        public void Free()
        {
            for (var p = 0; p < PoolCount; p++)
            {
                _pools[p]?.Free();
                _pools[p] = null;
            }
        }

    }
}
