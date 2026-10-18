using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Content;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Contracts.Vegetation;
using Campus.Kernel.Instances;
using Campus.Kernel.Maps;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Vegetation;

/// <summary>
/// WP-23's vegetation runtime (WLD-VEGETATION, ENG-PERF). On map load a worker takes the module's L0
/// instance streams (the content producer, every planted tree once, grouped by mother model) and the
/// shared prototypes of every pool; the main thread then asks the kernel's instance pools (<see cref="IInstancePoolHost"/>)
/// for one pool per mother model and pool: L0+, L0, L1 and far on layer 4 without shadows, and the ShadowsOnly proxy on
/// layer 5 holding the near trees. At most 10 Hz the trees are re-bucketed by camera distance (<see cref="VegetationBuckets"/>)
/// and the changed pools rewrite their whole buffer, visible count and bounds. A pool left with no tree is parked: its bounds move far below the ground, since an empty
/// pool whose bounds still meet the camera or a shadow cascade costs its draw calls in that pass (max-011 0n-2).
/// A new world instance re-buckets at once.
/// The near card grass of the authored grass areas comes in the same prepared list under <see cref="GrassPool.KeyPrefix"/>
/// and runs as one <see cref="GrassPool"/> per style, refreshed on the same tick (WLD-VEGETATION §3.5).
/// No engine instance is made here.
/// </summary>
[KernelNode("systems.vegetation", Owner = "WP-23")]
public sealed partial class VegetationRuntime : Node, IMapAware, IViewContentReadiness, IViewCapturePreparation
{
    private static readonly StringName _shaderInstanceShadowProxy = InstanceShaderParameters.ShadowProxy;

    private const string ModuleId = "vegetation";
    private const uint VisibleLayer = 1u << 3;   // layer 4: vegetation visible geometry (ENG-PERF)
    private const uint ProxyLayer = 1u << 4;     // layer 5: ShadowsOnly proxies
    private const double Interval = 0.1;         // re-bucket at most 10 Hz
    private const int MountsPerFrame = 8;        // mother models mounted per frame after loading

    private readonly List<Mother> _mothers = [];
    private readonly List<GrassPool> _grass = [];
    private bool _grassMounted;
    private IInstancePoolHost? _host;
    private ICameraRig? _rig;
    private IWorldState? _world;
    private Task<(Mother[] Trees, GrassPool[] Grass)>? _pending;
    private MapId? _map;
    private int _mounted;
    private double _since = Interval;
    private bool _worldReset = true;
    private N.Vector3? _appliedEye;
    private bool _resetForCapture;
    public string? Error { get; private set; }
    /// <summary>Current map's frozen grass scopes, cleared with its pools on unload. No grass instances have been generated here.</summary>
    public GrassRuntimeData? Grass { get; private set; }
    public bool IsReady(Pose pose) => Error is null && _map is not null && _pending is null &&
        _mounted == _mothers.Count && _appliedEye == pose.Position;

    public void PrepareCapture(Pose pose)
    {
        _appliedEye = null;
        _resetForCapture = true;
    }

    /// <summary>Planted trees and mounted pools (for logs and gates).</summary>
    public int Trees => _mothers.Sum(m => m.Buckets.Count);
    public int Pools => _mounted * Mother.PoolCount;

    public override void _EnterTree()
    {
        ProcessPriority = FramePriority.Systems;
        if (_world is not null) _world.InstanceChanged += OnWorldInstanceChanged;
        OnWorldInstanceChanged(0);
    }

    public override void _Ready()
    {
        _host = Services.Get<IInstancePoolHost>();
        _rig = Services.Get<ICameraRig>();
        _world = Services.Get<IWorldState>();
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
        _appliedEye = null;
    }

    public void OnMapUnloading(MapId from)
    {
        FreeAll();
        _map = null;
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        FreeAll();
        Error = null;
        _map = ctx.Descriptor.Id;
        Func<BuildContext> context;
        IVegetationRuntimeSource producer;
        try
        {
            context = ContextFor(ctx);
            producer = Services.Get<IMapContentSource>().GetModule<IVegetationRuntimeSource>(ctx, ModuleId);
            Grass = producer.PrepareGrass(context());
        }
        catch (Exception error)
        {
            Error = error.GetBaseException().Message;
            GD.PushError($"Vegetation build context unavailable, no trees on {ctx.Descriptor.Id.Value}: {error}");
            return;
        }
        _pending = Task.Run(() =>
        {
            var prepared = producer.PrepareVegetation(context());
            return (prepared.Where(d => !IsGrass(d)).Select(data => new Mother(data)).ToArray(),
                prepared.Where(IsGrass).Select(data => new GrassPool(data)).ToArray());
        });
    }

    public override void _Process(double delta)
    {
        if (Error is not null) return;
        try { UpdateContent(delta); }
        catch (Exception error)
        {
            Error = error.GetBaseException().Message;
            GD.PushError($"Vegetation content failed: {error}");
        }
    }

    private void UpdateContent(double delta)
    {
        if (_pending is { IsCompleted: true } done)
        {
            _pending = null;
            var (trees, grass) = done.GetAwaiter().GetResult();
            _mothers.AddRange(trees);
            _grass.AddRange(grass);
        }
        if (_host is null || _rig is null || _map is not { } map) return;
        if (!_grassMounted && _pending is null)
        {
            foreach (var pool in _grass) pool.Mount(_host, map);
            _grassMounted = true;
            if (_grass.Count > 0)
                GD.Print($"Grass mounted: {_grass.Sum(g => g.Count)} clumps in {_grass.Count} pools, capacity {_grass.Sum(g => g.Capacity)}");
        }
        for (var n = 0; n < MountsPerFrame && _mounted < _mothers.Count; n++)
        {
            _mothers[_mounted].Mount(_host, map);
            _mounted++;
            if (_mounted == _mothers.Count)
            {
                GD.Print($"Vegetation mounted: {Trees} trees, {_mothers.Count} mother models, {Pools} kernel pools");
                _since = Interval;
            }
        }
        if (_mounted == 0)
        {
            if (_pending is null) _appliedEye = _rig.Pose.Position;
            return;
        }
        var eye = _rig.Pose.Position;
        var reset = _worldReset || _resetForCapture;
        _since += delta;
        if (!reset && _since < Interval) return;
        _since = 0;
        for (var i = 0; i < _mounted; i++) _mothers[i].Refresh(eye, reset);
        foreach (var pool in _grass) pool.Refresh(eye, reset);
        _resetForCapture = false;
        _worldReset = false;
        _appliedEye = eye;
    }

    private void FreeAll()
    {
        _pending = null;
        Grass = null;
        foreach (var mother in _mothers) mother.Free();
        _mothers.Clear();
        foreach (var pool in _grass) pool.Free();
        _grass.Clear();
        _grassMounted = false;
        _mounted = 0;
        _worldReset = true;
        _appliedEye = null;
        _resetForCapture = false;
    }

    private static bool IsGrass(VegetationMotherData data) => data.Key.StartsWith(GrassPool.KeyPrefix, StringComparison.Ordinal);

    /// <summary>Capture the formal service's prepared map context before publishing work.</summary>
    private static Func<BuildContext> ContextFor(IMapContext map) => VegetationContext.Resolve(map, ModuleId,
        Services.Get<IBakeContextSource>());

    /// <summary>One mother model's pools: its trees' records, their buckets and the kernel pools fed from them.</summary>
    private sealed class Mother
    {
        public const int PoolCount = 5;
        private const int Stride = InstanceStreamLayout.FloatsPerInstance;

        private readonly string _key;
        private readonly WpId _owner;
        private readonly float[] _records;
        private readonly GeometryPrototype[] _prototypes;
        private readonly Box3[] _bounds;
        private readonly IInstancePool?[] _pools = new IInstancePool?[PoolCount];
        private readonly float[] _scratch;

        /// <summary>Bounds of a pool holding no tree: far below the ground, outside every camera and shadow cascade.</summary>
        private static readonly Aabb Parked = new(new Vector3(0f, -100000f, 0f), Vector3.One);

        public Mother(VegetationMotherData data)
        {
            _key = data.Key;
            _owner = data.Owner;
            var records = data.Records.ToArray();
            var prototypes = data.Prototypes.ToArray();
            _records = records;
            _prototypes = prototypes;
            Buckets = new VegetationBuckets(records);
            _scratch = new float[records.Length];
            _bounds = data.Bounds.ToArray();
        }

        public VegetationBuckets Buckets { get; }

        public void Mount(IInstancePoolHost host, MapId map)
        {
            for (var p = 0; p < PoolCount; p++)
            {
                var shadow = (VegPool)p == VegPool.Shadow;
                var spec = new GeometryInstancePoolSpec($"vegetation.{_key}.{(VegPool)p}", _owner.Value, _prototypes[p], Buckets.Count, Parked,
                    shadow ? ProxyLayer : VisibleLayer,
                    shadow ? GeometryInstance3D.ShadowCastingSetting.ShadowsOnly : GeometryInstance3D.ShadowCastingSetting.Off)
                { Map = map };
                var pool = host.Mount(spec);
                _pools[p] = pool;
                if (shadow) pool.SetParameter(_shaderInstanceShadowProxy, 1f);
            }
        }

        public void Refresh(N.Vector3 eye, bool reset)
        {
            if (!Buckets.Update(eye, reset) && !reset) return;
            for (var p = 0; p < PoolCount; p++)
            {
                if (_pools[p] is not { } pool) continue;
                var members = Members((VegPool)p).ToArray();
                pool.VisibleCount = 0;
                if (members.Length == 0)
                {
                    // An empty pool still costs its draw calls in every pass its bounds meet; parked out of every view it costs none.
                    pool.SetAabb(Parked);
                    continue;
                }
                Array.Clear(_scratch);
                for (var k = 0; k < members.Length; k++)
                    _records.AsSpan(members[k] * Stride, Stride).CopyTo(_scratch.AsSpan(k * Stride, Stride));
                pool.WriteBuffer(_scratch);
                pool.SetAabb(Union(members));
                pool.VisibleCount = members.Length;
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

        /// <summary>
        /// A near pool holds its own level's trees; the far pool every tree in the far pool's range, so the cross-fade band is
        /// in both (23c2c); the shadow proxy holds every near tree (L0+, L0, L1).
        /// </summary>
        private IEnumerable<int> Members(VegPool pool)
        {
            for (var i = 0; i < Buckets.Count; i++)
            {
                var level = Buckets.Level(i);
                var member = pool switch
                {
                    VegPool.L0Plus or VegPool.L0 or VegPool.L1 => level == (byte)pool,
                    VegPool.Far => Buckets.Far(i),
                    VegPool.Shadow => level <= (byte)VegPool.L1,
                    _ => throw new ArgumentOutOfRangeException(nameof(pool), pool, "Unknown vegetation pool.")
                };
                if (member) yield return i;
            }
        }

        private Aabb Union(IEnumerable<int> trees)
        {
            var min = new N.Vector3(float.MaxValue);
            var max = new N.Vector3(float.MinValue);
            foreach (var i in trees)
            {
                min = N.Vector3.Min(min, _bounds[i].Min);
                max = N.Vector3.Max(max, _bounds[i].Max);
            }
            if (min.X > max.X) return new Aabb(Vector3.Zero, Vector3.Zero);
            var size = max - min;
            return new Aabb(new Vector3(min.X, min.Y, min.Z), new Vector3(size.X, size.Y, size.Z));
        }

    }
}
