using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Gen.Vegetation;
using Campus.Kernel.Instances;
using Campus.Kernel.Maps;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Vegetation;

/// <summary>
/// WP-23's vegetation runtime (WLD-VEGETATION §11.3–§11.4, ENG-PERF §9). On map load a worker takes the module's L0
/// instance streams (<see cref="VegetationModule.Build"/>, every planted tree once, grouped by mother model) and the
/// shared prototypes of every pool; the main thread then asks the kernel's instance pools (<see cref="IInstancePoolHost"/>)
/// for one pool per mother model and pool: L0+, L0, L1 and far on layer 4 without shadows, and the ShadowsOnly proxy on
/// layer 5 holding the near trees. At most 10 Hz the trees are re-bucketed by camera distance (<see cref="VegetationBuckets"/>)
/// and the changed pools rewrite their whole buffer, visible count and bounds. A pool left with no tree is parked: its bounds move far below the ground, since an empty
/// pool whose bounds still meet the camera or a shadow cascade costs its draw calls in that pass (max-011 0n-2).
/// A new world instance re-buckets at once.
/// No engine instance is made here.
/// </summary>
[KernelNode("systems.vegetation", Owner = "WP-23")]
public sealed partial class VegetationRuntime : Node, IMapAware, IViewContentReadiness, IViewCapturePreparation
{
    private const string ModuleId = "vegetation";
    private const uint VisibleLayer = 1u << 3;   // layer 4: vegetation visible geometry (ENG-PERF §4)
    private const uint ProxyLayer = 1u << 4;     // layer 5: ShadowsOnly proxies
    private const double Interval = 0.1;         // re-bucket at most 10 Hz
    private const int MountsPerFrame = 8;        // mother models mounted per frame after loading

    private readonly List<Mother> _mothers = [];
    private IInstancePoolHost? _host;
    private ICameraRig? _rig;
    private IWorldState? _world;
    private Task<Mother[]>? _pending;
    private MapId? _map;
    private int _mounted;
    private double _since = Interval;
    private long _instance = long.MinValue;
    private N.Vector3? _appliedEye;
    private bool _resetForCapture;
    public string? Error { get; private set; }
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

    public override void _EnterTree() => ProcessPriority = FramePriority.Systems;

    public override void _Ready()
    {
        _host = Services.Get<IInstancePoolHost>();
        _rig = Services.Get<ICameraRig>();
        _world = Services.Get<IWorldState>();
    }

    public override void _ExitTree() => FreeAll();

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
        try { context = ContextFor(ctx); }
        catch (Exception error)
        {
            Error = error.GetBaseException().Message;
            GD.PushError($"Vegetation build context unavailable, no trees on {ctx.Descriptor.Id.Value}: {error}");
            return;
        }
        _pending = Task.Run(() => Prepare(context));
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
            _mothers.AddRange(done.GetAwaiter().GetResult());
        }
        if (_host is null || _rig is null || _map is not { } map) return;
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
        var instance = _world?.Instance ?? 0;
        var eye = _rig.Pose.Position;
        var reset = instance != _instance || _resetForCapture;
        _since += delta;
        if (!reset && _since < Interval) return;
        _since = 0;
        _instance = instance;
        for (var i = 0; i < _mounted; i++) _mothers[i].Refresh(eye, reset);
        _resetForCapture = false;
        _appliedEye = eye;
    }

    private void FreeAll()
    {
        _pending = null;
        foreach (var mother in _mothers) mother.Free();
        _mothers.Clear();
        _mounted = 0;
        _instance = long.MinValue;
        _appliedEye = null;
        _resetForCapture = false;
    }

    /// <summary>
    /// The build context the worker uses (<see cref="VegetationContext"/>): the prepared production context, or, on the D0 stub
    /// source only, the kernel's own map loading over the map's frozen inputs and claims. Files are read here on the main
    /// thread; the parse runs on the worker. A failing source throws; nothing falls back to another world.
    /// </summary>
    private static Func<BuildContext> ContextFor(IMapContext map) => VegetationContext.Resolve(map, ModuleId,
        Services.Get<IBakeContextSource>(), VegetationContext.SourceIsD0Stub(Services.Services),
        Services.Get<PaletteRegistry>(), Services.GlobalBuildInputs, Services.Get<IWorldModelLoader>(), () =>
        {
            // D0 holds no map catalogue; the entry is the one this map's context was made from.
            var entry = new MapCatalogEntry(map.Descriptor, map.DataRoot, map.BakeRoot, []);
            var data = new MapDataSource();
            return (data.ReadLoadInput(entry), data.ReadClaims(map.Descriptor.Id));
        });

    /// <summary>Worker: the module's L0 streams regrouped per mother model, and every pool's prototype built once.</summary>
    private static Mother[] Prepare(Func<BuildContext> contextFor)
    {
        var ctx = contextFor();
        var module = new VegetationModule();
        var species = VegetationInputs.Species(ctx);
        var palette = ctx.Palette!;
        var groups = new SortedDictionary<string, (SpeciesInfo Info, int Variant, List<float> Records)>(StringComparer.Ordinal);
        foreach (var tile in module.Tiles(ctx))
            foreach (var stream in module.Build(ctx, tile, LodLevel.L0).Instances)
            {
                var (id, variant, _) = VegetationPrototypes.Parse(stream.Prototype.Id);
                var key = id + "." + variant.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!groups.TryGetValue(key, out var group)) groups.Add(key, group = (species.Get(id), variant, []));
                group.Records.AddRange(stream.Data.ToArray());
            }
        var mothers = groups.ToArray();
        var built = new Mother[mothers.Length];
        Parallel.For(0, mothers.Length, i =>
        {
            var (key, (info, variant, records)) = (mothers[i].Key, mothers[i].Value);
            var prototypes = Enumerable.Range(0, Mother.PoolCount)
                .Select(p => VegetationPrototypes.Get(info, variant, (VegPool)p, palette)).ToArray();
            built[i] = new Mother(key, records.ToArray(), prototypes);
        });
        return built;
    }

    /// <summary>One mother model's pools: its trees' records, their buckets and the kernel pools fed from them.</summary>
    private sealed class Mother
    {
        public const int PoolCount = 5;
        private const int Stride = InstanceStreamLayout.FloatsPerInstance;

        private readonly string _key;
        private readonly float[] _records;
        private readonly GeometryPrototype[] _prototypes;
        private readonly Box3[] _bounds;
        private readonly IInstancePool?[] _pools = new IInstancePool?[PoolCount];
        private readonly float[] _scratch;

        /// <summary>Bounds of a pool holding no tree: far below the ground, outside every camera and shadow cascade.</summary>
        private static readonly Aabb Parked = new(new Vector3(0f, -100000f, 0f), Vector3.One);

        public Mother(string key, float[] records, GeometryPrototype[] prototypes)
        {
            _key = key;
            _records = records;
            _prototypes = prototypes;
            Buckets = new VegetationBuckets(records);
            _scratch = new float[records.Length];
            // Per-tree world bounds on the largest level (L0+), shared by every pool of the tree.
            _bounds = new Box3[Buckets.Count];
            for (var i = 0; i < _bounds.Length; i++)
                _bounds[i] = VegetationPrototypes.WorldBounds(prototypes[(int)VegPool.L0Plus].Bounds, Matrix(records.AsSpan(i * Stride, Stride)));
        }

        public VegetationBuckets Buckets { get; }

        public void Mount(IInstancePoolHost host, MapId map)
        {
            for (var p = 0; p < PoolCount; p++)
            {
                var shadow = (VegPool)p == VegPool.Shadow;
                var spec = new GeometryInstancePoolSpec($"vegetation.{_key}.{(VegPool)p}", VegetationPrototypes.Owner, _prototypes[p], Buckets.Count, Parked,
                    shadow ? ProxyLayer : VisibleLayer,
                    shadow ? GeometryInstance3D.ShadowCastingSetting.ShadowsOnly : GeometryInstance3D.ShadowCastingSetting.Off)
                { Map = map };
                var pool = host.Mount(spec);
                _pools[p] = pool;
                if (shadow) pool.SetParameter(InstanceShaderNames.ShadowProxy, 1f);
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

        private static N.Matrix4x4 Matrix(ReadOnlySpan<float> r) =>
            new(r[0], r[4], r[8], 0f, r[1], r[5], r[9], 0f, r[2], r[6], r[10], 0f, r[3], r[7], r[11], 1f);
    }
}
