using Campus.Kernel.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Stream;
using Campus.Kernel.Frame;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Contracts.Vegetation;
using Campus.Contracts.Quality;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Vegetation;

/// <summary>Tile payload owner. StreamDirector alone requests, schedules and retires each tree/grass tile.</summary>
[KernelNode("systems.vegetation")]
[QualityResponsive(Reads = [QualityKnob.ShadowQuality, QualityKnob.ShadowDistance])]
public sealed partial class VegetationRuntime : Node, IMapAware, IViewContentReadiness, IViewCapturePreparation, IStreamTileParticipant, IQualityAware
{
    private static readonly StringName _shaderInstanceShadowProxy = InstanceShaderParameters.ShadowProxy;
    private static readonly StringName _shaderInstanceVegLevel = InstanceShaderParameters.VegLevel;
    private static readonly StringName _shaderInstanceVegTransitionBase = InstanceShaderParameters.VegTransitionBase;
    private static readonly StringName _shaderGlobalNearReady = ShaderGlobals.VegetationNearReady;
    private static readonly StringName _shaderGlobalTransition = ShaderGlobals.VegetationLodTransition;
    private const uint VisibleLayer = RenderLayers.Vegetation;
    private const uint ProxyLayer = RenderLayers.ShadowProxy;
    private const double Interval = 0.1;
    private readonly Dictionary<BakeKey, MountedTile> _tiles = [];
    private IInstancePoolHost? _host;
    private IStreamMountQueue? _mountQueue;
    private IStreamVisualFrame? _streamFrame;
    private ICameraRig? _rig;
    private IWorldState? _world;
    private MapId? _map;
    private long _generation;
    private double _since = Interval;
    private bool _worldReset = true;
    private Pose? _capture;
    private float _shadowReach;
    private bool _forceRefresh;
    private MapRect? _mapRect;
    private Image? _nearReadyImage;
    private ImageTexture? _nearReadyTexture;
    private IDisposable? _nearReadyRegistration;
    private VegetationTransitionMap? _transitionMap;
    private ImageTexture? _transitionTexture;
    private IDisposable? _transitionRegistration;
    private bool _nearReadyDirty;
    private bool _hasNearReady;
    private float TreeReach => MathF.Max(VegetationBuckets.NearReach, _shadowReach);
    public StreamMountKind Kind => StreamMountKind.Auxiliary;
    public Exception? Failure { get; private set; }
    public int Trees => _tiles.Values.Sum(t => t.Mothers.Sum(m => m.Buckets.Count));
    public int Pools => _tiles.Values.Sum(t => t.Mothers.Length * Mother.PoolCount + t.Grass.Length);

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        _host = KernelServices.Get<IInstancePoolHost>();
        _mountQueue = KernelServices.Get<IStreamMountQueue>();
        _streamFrame = KernelServices.Get<IStreamVisualFrame>();
        _rig = KernelServices.Get<ICameraRig>();
        _world = KernelServices.Get<IWorldState>();
        SetShadowReach(KernelServices.Get<IQualityService>().Effective);
        _world.InstanceAdvancing += OnWorldInstanceChanged;
    }

    public void ApplyQuality(QualityView view, in QualityChange change) => SetShadowReach(view);

    private void SetShadowReach(QualityView view)
    {
        var shadows = ShadowSettings.From(view);
        _shadowReach = shadows.MaxDistance * (shadows.Splits == 2 ? shadows.Split1 : shadows.Split2);
        _forceRefresh = true;
        _since = Interval;
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceAdvancing -= OnWorldInstanceChanged;
        if (_tiles.Count != 0 || _nearReadyImage is not null || _transitionMap is not null)
            throw new InvalidOperationException("Vegetation owners must drain before exit.");
    }

    private void OnWorldInstanceChanged(long _)
    {
        _worldReset = true;
        ClearNearReady();
        foreach (var tile in _tiles.Values) { tile.ResetNear(); tile.Hide(); }
    }

    public void OnMapLoaded(IMapContext context)
    {
        if (_tiles.Count != 0) throw new InvalidOperationException("Vegetation owners must drain before map load.");
        _generation++;
        Failure = null;
        _map = context.Descriptor.Id;
        PrepareDataTextures(context.Descriptor.DataRect);
    }

    public void OnMapUnloading(MapId from)
    {
        if (_map != from) return;
        _generation++; _map = null;
        _mountQueue!.Retire(new TextureRetirement(this));
        _worldReset = true; _capture = null; _since = Interval;
    }

    public bool CanMount(BakeKey key) => key.Module is BakeModuleIds.VegetationTrees or BakeModuleIds.VegetationGrass;

    public StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident)
    {
        if (!CanMount(entry.Key)) return StreamTileNeed.None;
        var reach = entry.Key.Module == BakeModuleIds.VegetationGrass ? GrassPoolIndex.Reach : TreeReach;
        // Full near exit envelope; no view-frustum filter, because offscreen near trees still cast visible shadows.
        var eye = view.Eye.Position;
        var d = entry.Key.Module == BakeModuleIds.VegetationGrass
            ? MathF.Sqrt(entry.Bounds.HorizontalDistanceSquared(eye)) : MathF.Sqrt(entry.Bounds.DistanceSquared(eye));
        if (d <= reach) return StreamTileNeed.Required;
        if (resident && _tiles.TryGetValue(entry.Key, out var mounted) && mounted.NearActive)
            return StreamTileNeed.Retain;
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
                grass != mother.Key.StartsWith(GrassPool.KeyPrefix, StringComparison.Ordinal) ||
                grass != (mother.GrassIndex is not null))
                throw new ArgumentException("Vegetation payload has an invalid mother layout.");
        }
        return new MountWork(this, content, priority, revision, _generation);
    }

    public bool IsReady(BakeKey key) => Failure is null && _map == key.Map && _tiles.TryGetValue(key, out var tile) && tile.Ready;

    public bool IsReady(Pose pose)
    {
        if (Failure is not null || _map is null || _world is not { Instance: > 0 }) return false;
        foreach (var tile in _tiles.Values)
            if (tile.NeededAt(pose.Position, TreeReach) && (!tile.Ready || tile.AppliedEye != pose.Position ||
                tile.NearTransition || tile.HasTreeTransitions)) return false;
        return true;
    }

    public void PrepareCapture(Pose pose) => _capture = pose;

    public override void _Process(double delta)
    {
        if (_map is null || _rig is null || Failure is not null) return;
        if (_world is not { Instance: > 0 })
        {
            ClearNearReady();
            foreach (var tile in _tiles.Values) { tile.ResetNear(); tile.Hide(); }
            return;
        }
        try
        {
            var eye = (_capture ?? _rig.Pose).Position;
            var reset = _worldReset || _capture is not null;
            _since += delta;
            if (!reset && !_forceRefresh && _since < Interval) return;
            _since = 0;
            var clock = FrameClock.FadeSeconds;
            foreach (var tile in _tiles.Values)
            {
                if (!tile.Mounted || tile.Retiring) continue;
                tile.UpdateNear(eye, clock);
                if (!tile.NeededAt(eye, TreeReach) && !tile.NearActive) { tile.Hide(); WriteNearReady(tile); continue; }
                foreach (var mother in tile.Mothers) mother.Refresh(eye, reset, _shadowReach, clock, tile.NearActive,
                    _forceRefresh || tile.AppliedEye is null);
                foreach (var grass in tile.Grass) grass.Refresh(eye, reset || tile.AppliedEye is null);
                tile.AppliedEye = eye;
                WriteNearReady(tile);
            }
            UploadDataTextures();
            _worldReset = false;
            _forceRefresh = false;
            _capture = null;
        }
        catch (Exception error) { RecordFailure(error); }
    }

    // StreamDirector and view readiness publish this same producer failure (ENG-PERF-STREAM).
    private void RecordFailure([FailureHandler("ENG-PERF-STREAM")] Exception error)
    {
        Failure = RetainedFailure.Append(Failure, error);
        try
        {
            ClearNearReady();
            foreach (var tile in _tiles.Values) { tile.ResetNear(); tile.Hide(); }
        }
        catch (Exception cleanup) { Failure = RetainedFailure.Append(Failure, cleanup); }
    }

    private sealed class TextureRetirement(VegetationRuntime owner) : IStreamReleaseWork
    {
        public bool ReleaseStep(double budgetMilliseconds)
        {
            if (owner._tiles.Count != 0) return false;
            owner.ReleaseDataTextures(); return true;
        }
    }

    private void PrepareDataTextures(MapRect rect)
    {
        _mapRect = rect;
        var registry = KernelServices.Get<IGlobalDataTextureRegistry>();
        _nearReadyImage = Image.CreateEmpty(VegetationNearReadyEncoding.Width(rect), VegetationNearReadyEncoding.Height(rect), false, Image.Format.Rgf);
        _nearReadyImage.Fill(Colors.Transparent);
        _nearReadyTexture = ImageTexture.CreateFromImage(_nearReadyImage);
        _nearReadyRegistration = registry.RegisterOwned(ShaderGlobals.VegetationNearReady, _nearReadyTexture, _nearReadyTexture.GetRid());
        _transitionMap = new VegetationTransitionMap();
        _transitionTexture = ImageTexture.CreateFromImage(_transitionMap.Image);
        _transitionRegistration = registry.RegisterOwned(ShaderGlobals.VegetationLodTransition, _transitionTexture, _transitionTexture.GetRid());
        _transitionMap.Uploaded();
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalNearReady, _nearReadyTexture);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalTransition, _transitionTexture);
    }

    private void ReleaseDataTextures()
    {
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalNearReady, default(Variant));
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalTransition, default(Variant));
        if (_nearReadyRegistration is not null) _nearReadyRegistration.Dispose();
        else _nearReadyTexture?.Dispose();
        if (_transitionRegistration is not null) _transitionRegistration.Dispose();
        else _transitionTexture?.Dispose();
        _nearReadyRegistration = _transitionRegistration = null;
        _nearReadyTexture = _transitionTexture = null;
        _nearReadyImage?.Dispose();
        _nearReadyImage = null;
        _transitionMap?.Dispose();
        _transitionMap = null;
        _mapRect = null;
        _nearReadyDirty = false;
        _hasNearReady = false;
    }

    private void ClearNearReady()
    {
        if (!_hasNearReady || _nearReadyImage is null || _nearReadyTexture is null) return;
        _nearReadyImage.Fill(Colors.Transparent);
        _nearReadyTexture.Update(_nearReadyImage);
        _nearReadyDirty = false;
        _hasNearReady = false;
    }

    private void WriteNearReady(MountedTile tile)
    {
        if (!tile.IsTrees || _nearReadyImage is null || _mapRect is null) return;
        var ready = tile.NearActive && tile.Ready && tile.AppliedEye is not null;
        var code = ready ? tile.NearCode : 0f;
        if (tile.PublishedReady == ready && tile.PublishedCode == code) return;
        if (VegetationNearReadyEncoding.TryTexel(_mapRect, tile.Tile, out var x, out var y))
        {
            _nearReadyImage.SetPixel(x, y, new Color(ready ? 1f : 0f, code, 0f));
            _nearReadyDirty = true;
        }
        tile.PublishedReady = ready;
        tile.PublishedCode = code;
        _hasNearReady |= ready;
    }

    private void UploadDataTextures()
    {
        if (_transitionMap is { Dirty: true } map && _transitionTexture is { } texture)
        {
            if (map.LayoutChanged) texture.SetImage(map.Image);
            else texture.Update(map.Image);
            map.Uploaded();
        }
        if (!_nearReadyDirty || _nearReadyImage is null || _nearReadyTexture is null) return;
        _nearReadyTexture.Update(_nearReadyImage);
        _nearReadyDirty = false;
    }

    private sealed class MountedTile
    {
        private readonly IReadOnlyList<VegetationMotherData> _data;
        private readonly Box3? _treeBounds;
        private readonly VegetationCellFade _near = new();
        public MountedTile(VegetationTileContent content, VegetationTransitionMap transitionMap)
        {
            var id = content.Key.Detail.Replace('/', '.');
            IsTrees = content.Key.Module == BakeModuleIds.VegetationTrees;
            Tile = content.Tile;
            Mothers = IsTrees ? content.Mothers.Select(d => new Mother(d, id, transitionMap)).ToArray() : [];
            Grass = content.Key.Module == BakeModuleIds.VegetationGrass ? content.Mothers.Select(d => new GrassPool(d, id)).ToArray() : [];
            _data = content.Mothers;
            if (!IsTrees) return;
            var min = new N.Vector3(float.MaxValue);
            var max = new N.Vector3(float.MinValue);
            foreach (var mother in content.Mothers)
                foreach (var bounds in mother.Bounds)
                {
                    min = N.Vector3.Min(min, bounds.Min);
                    max = N.Vector3.Max(max, bounds.Max);
                }
            if (min.X <= max.X) _treeBounds = new(min, max);
        }
        public bool Mounted;
        public bool Retiring;
        private int _release;
        public bool IsTrees { get; }
        public TileKey Tile { get; }
        public bool NearActive => _near.Active;
        public bool NearTransition => _near.Transitioning;
        public bool PublishedReady { get; set; }
        public float PublishedCode { get; set; } = float.NaN;
        public float NearCode => _near.Code;
        public bool HasTreeTransitions
        {
            get { foreach (var mother in Mothers) if (mother.Buckets.IsTransitioning) return true; return false; }
        }
        public Mother[] Mothers { get; }
        public GrassPool[] Grass { get; }
        public N.Vector3? AppliedEye { get; set; }
        public void UpdateNear(N.Vector3 eye, float clock)
        {
            if (_treeBounds is { } bounds) _near.Update(MathF.Sqrt(bounds.DistanceSquared(eye)), clock);
        }
        public void ResetNear()
        {
            _near.Reset();
            PublishedReady = false;
            PublishedCode = float.NaN;
        }
        public bool Ready
        {
            get
            {
                if (!Mounted || Retiring) return false;
                foreach (var mother in Mothers) if (!mother.Ready) return false;
                foreach (var grass in Grass) if (!grass.Ready) return false;
                return true;
            }
        }
        public bool NeededAt(N.Vector3 eye, float treeReach)
        {
            var reach = Grass.Length > 0 ? GrassPoolIndex.Reach : treeReach * VegetationBuckets.Hysteresis;
            for (var m = 0; m < _data.Count; m++)
            {
                var mother = _data[m];
                for (var b = 0; b < mother.Bounds.Count; b++)
                {
                    var bounds = mother.Bounds[b];
                    var distance = Grass.Length > 0 ? MathF.Sqrt(bounds.HorizontalDistanceSquared(eye)) :
                        MathF.Sqrt(bounds.DistanceSquared(eye));
                    if (distance <= reach) return true;
                }
            }
            return false;
        }
        public void Hide()
        {
            if (Mounted && AppliedEye is null) return;
            foreach (var mother in Mothers) mother.Hide();
            foreach (var grass in Grass) grass.Hide();
            AppliedEye = null;
        }
        public bool FreeOne()
        {
            var treeUnits = Mothers.Length * Mother.PoolCount;
            if (_release < treeUnits) Mothers[_release / Mother.PoolCount].FreePool(_release % Mother.PoolCount);
            else if (_release < treeUnits + Grass.Length) Grass[_release - treeUnits].Free();
            else return true;
            _release++; return false;
        }
    }

    private sealed class MountWork : StreamMountWork
    {
        private readonly VegetationRuntime _owner;
        private readonly long _generation;
        private readonly MountedTile _tile;
        private int _unit;
        private int _initialUnit;
        private long _initialWorld = -1;
        private N.Vector3 _initialEye;
        private float _initialShadowReach;
        public MountWork(VegetationRuntime owner, VegetationTileContent content, BakePriority priority, long revision, long generation)
            : base(content.Key, priority, revision, StreamMountKind.Auxiliary)
        {
            _owner = owner;
            _generation = generation;
            _tile = new(content, owner._transitionMap ?? throw new InvalidOperationException("The vegetation transition map is unavailable."));
            if (!owner._tiles.TryAdd(Key, _tile)) throw new InvalidOperationException("Vegetation tile is already owned.");
        }
        protected override bool IsCurrent => _owner._generation == _generation && _owner._map == Key.Map;
        protected override bool MountStep(double budgetMilliseconds)
        {
            var host = _owner._host ?? throw new InvalidOperationException("Vegetation pool host is unavailable.");
            var treeUnits = _tile.Mothers.Length * Mother.PoolCount;
            var units = treeUnits + _tile.Grass.Length;
            // At most one native pool upload in a queue step.
            if (_unit < units)
            {
                if (_unit < treeUnits) _tile.Mothers[_unit / Mother.PoolCount].Mount(host, Key.Map, _unit % Mother.PoolCount);
                else _tile.Grass[_unit - treeUnits].Mount(host, Key.Map);
                _unit++;
                return false;
            }
            var world = _owner._world?.Instance ?? 0;
            if (_initialWorld != world)
            {
                _tile.Hide(); _tile.ResetNear();
                _initialUnit = 0; _initialWorld = world;
                _initialEye = (_owner._capture ?? _owner._rig!.Pose).Position;
                _initialShadowReach = _owner._shadowReach;
                if (world > 0) _tile.UpdateNear(_initialEye, FrameClock.FadeSeconds);
            }
            if (_initialUnit < treeUnits)
            {
                var mother = _tile.Mothers[_initialUnit / Mother.PoolCount];
                var p = _initialUnit % Mother.PoolCount;
                // Keep the bucket change flags intact until every pool has consumed this initial view.
                if (p == 0) mother.Buckets.Update(_initialEye, true, _initialShadowReach, FrameClock.FadeSeconds, _tile.NearActive);
                mother.RefreshPool(p, initial: true);
                if (world == 0) mother.Hide();
            }
            // Static preload has no display world; it still finishes real uploads without waiting for one.
            else if (_initialUnit < units)
            {
                var grass = _tile.Grass[_initialUnit - treeUnits];
                grass.Refresh(_initialEye, true);
                if (world == 0) grass.Hide();
            }
            _initialUnit++;
            if (_initialUnit < units) return false;
            _tile.AppliedEye = world > 0 ? _initialEye : null;
            _tile.Mounted = true;
            if (world > 0)
            {
                // Near coverage starts only when every initial pool is ready to replace its far layer.
                _tile.ResetNear(); _tile.UpdateNear(_initialEye, FrameClock.FadeSeconds);
            }
            _owner.WriteNearReady(_tile); _owner.UploadDataTextures();
            _owner._since = Interval;
            return true;
        }
        protected override void Cancelled() { _tile.Retiring = true; _tile.ResetNear(); }
        protected override bool Release(double budgetMilliseconds)
        {
            if (!_owner._streamFrame!.CanReleaseRetiredInstances) return false;
            var start = FrameClock.MonotonicMilliseconds;
            do
            {
                if (_tile.FreeOne())
                {
                    if (_owner._tiles.TryGetValue(Key, out var current) && ReferenceEquals(current, _tile)) _owner._tiles.Remove(Key);
                    _owner.WriteNearReady(_tile); _owner.UploadDataTextures();
                    return true;
                }
            } while (FrameClock.MonotonicMilliseconds - start < budgetMilliseconds);
            return false;
        }
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
        private readonly VegetationTransitionMap _transitions;
        private readonly int[] _transitionSlots = [-1, -1, -1];

        /// <summary>Bounds of a pool holding no tree: far below the ground, outside every camera and shadow cascade.</summary>
        private static readonly Aabb Parked = new(new Vector3(0f, -100000f, 0f), Vector3.One);

        public Mother(VegetationMotherData data, string tileId, VegetationTransitionMap transitions)
        {
            _transitions = transitions;
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
            else
            {
                var slot = _transitions.Allocate(Buckets.Count);
                _transitionSlots[p] = slot;
                pool.SetParameter(_shaderInstanceVegLevel, (float)kind);
                pool.SetParameter(_shaderInstanceVegTransitionBase, slot);
            }
        }

        public void Refresh(N.Vector3 eye, bool reset, float shadowReach, float clock, bool nearVisible, bool force)
        {
            if (!Buckets.Update(eye, reset, shadowReach, clock, nearVisible) && !reset && !force) return;
            for (var p = 0; p < PoolCount; p++) RefreshPool(p);
        }

        public void RefreshPool(int p, bool initial = false)
        {
            if (_pools[p] is not { } pool) return;
            // Records, bounds and traversal order are frozen for this mother; only membership changes their prefix.
            var changed = initial || Buckets.MembershipChanged(Order[p]);
            var count = 0;
            var min = new N.Vector3(float.MaxValue);
            var max = new N.Vector3(float.MinValue);
            for (var i = 0; i < Buckets.Count; i++)
            {
                if (!(Order[p] == VegPool.Shadow ? Buckets.InShadow(i) : Buckets.Contains(i, Order[p]))) continue;
                _members[count++] = i;
                min = N.Vector3.Min(min, _bounds[i].Min);
                max = N.Vector3.Max(max, _bounds[i].Max);
            }
            if (count == 0)
            {
                if (pool.VisibleCount != 0) pool.VisibleCount = 0;
                if (changed) pool.SetAabb(Parked);
                return;
            }
            if (changed) Array.Clear(_scratch);
            for (var k = 0; k < count; k++)
            {
                var member = _members[k];
                if (changed) _records.AsSpan(member * Stride, Stride).CopyTo(_scratch.AsSpan(k * Stride, Stride));
                if (p < _transitionSlots.Length)
                {
                    var slot = _transitionSlots[p];
                    if (slot < 0) throw new InvalidOperationException("A visible near pool has no transition slots.");
                    _transitions.Write(slot + k, Buckets.Level(member), Buckets.TargetLevel(member), Buckets.Started(member));
                }
            }
            if (changed)
            {
                pool.WriteBuffer(_scratch);
                var size = max - min;
                pool.SetAabb(new Aabb(new Vector3(min.X, min.Y, min.Z), new Vector3(size.X, size.Y, size.Z)));
            }
            if (pool.VisibleCount != count) pool.VisibleCount = count;
        }

        public void Hide()
        {
            foreach (var pool in _pools) if (pool is { VisibleCount: > 0 }) pool.VisibleCount = 0;
        }

        public void FreePool(int p)
        {
            _pools[p]?.Free();
            _pools[p] = null;
            if (p >= _transitionSlots.Length) return;
            _transitions.Release(_transitionSlots[p], Buckets.Count);
            _transitionSlots[p] = -1;
        }

    }
}
