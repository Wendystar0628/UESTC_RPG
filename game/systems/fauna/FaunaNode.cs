using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Gen.Fauna;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Fauna;

/// <summary>
/// WP-25 animals at runtime (WLD-LIFE). On map load the site layout and every set's world-space geometry are
/// prepared off the main thread from the map's world model (no feature IDs, no data paths). Each site then keeps a latched
/// "applied set": four times a second the target set is read from the sun altitude and the weather vector
/// (<see cref="FaunaSchedule"/>; IWorldState only), and a changed target is applied only while the site is outside the main
/// camera's frustum, or during a time fast-forward, a clock scrub, a season or weather transition, or a world-instance change
/// (under the veil). So animals never pop in or out in view. Geometry goes through the kernel's IRenderHost; nothing here
/// creates engine instances. Animal reactions (contract-22) and the dynamic sound emitters (WP-53) are not part of 25a.
/// </summary>
[KernelNode("systems.fauna", Owner = "WP-25")]
public partial class FaunaNode : Node, IMapAware, IViewContentReadiness
{
    private const double CheckSeconds = 0.25;              // 4 Hz latch check (WLD-LIFE)
    private static readonly WpId Package = new("WP-25");

    private sealed record Prepared(FaunaLayout Layout, ContentOutput[][] Sets);

    private sealed class Latch
    {
        public bool Applied;
        public FaunaSet? Set;
        public RenderHandle? Handle;
    }

    private IWorldState? _world;
    private IRenderHost? _host;
    private Task<Prepared>? _job;
    private Prepared? _prepared;
    private Latch[] _latches = [];
    private long _instance = long.MinValue;
    private double _sinceCheck;
    private bool _force;
    public string? Error { get; private set; }
    public bool IsReady(Pose pose) => Error is null && _job is null && _prepared is not null && _latches.All(latch => latch.Applied);

    /// <summary>Applied set per site key, for gates and the review sandbox (read only).</summary>
    public IReadOnlyDictionary<string, FaunaSet?> Applied =>
        _prepared is null ? new Dictionary<string, FaunaSet?>() :
        _prepared.Layout.Sites.Select((s, i) => (s.Key, _latches[i].Set)).ToDictionary(p => p.Key, p => p.Set, StringComparer.Ordinal);

    public override void _EnterTree() => ProcessPriority = FramePriority.Systems;

    public override void _Ready() => _world = Services.Get<IWorldState>();

    // Leaving the tree: the render host releases its owned nodes when the stage exits, and the
    // stage is busy removing children, so only drop the references here; map unloads release through the host.
    public override void _ExitTree()
    {
        _latches = [];
        _prepared = null;
        _job = null;
    }

    public void OnMapUnloading(MapId from)
    {
        Release();
        _job = null;
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        Release();
        Error = null;
        var world = ctx.World;
        var seed = ctx.Descriptor.Seed;
        var palette = Services.Get<PaletteCatalog>();
        // Layout and meshes are pure and built off the main thread; mounting happens on the main thread when it is done.
        var lakes = ctx.Descriptor.Env.ReflectLakes;
        _job = Task.Run(() => Prepare(world, seed, lakes, palette));
    }

    private static Prepared Prepare(IWorldModel world, uint seed, IReadOnlyList<FeatureId> lakes, PaletteCatalog palette)
    {
        var layout = FaunaPlanner.Build(world, seed, lakes);
        var scene = new FaunaScene(palette);
        var sets = layout.Sites.Select(site => Enum.GetValues<FaunaSet>().Select(set => scene.Build(site.Set(set))).ToArray()).ToArray();
        return new(layout, sets);
    }

    public override void _Process(double delta)
    {
        if (Error is not null) return;
        try { UpdateContent(delta); }
        catch (Exception error)
        {
            Error = error.GetBaseException().Message;
            GD.PushError($"[WP-25 fauna] content failed: {error}");
        }
    }

    private void UpdateContent(double delta)
    {
        if (_world is null) return;
        if (_job is { IsCompleted: true } job)
        {
            _job = null;
            _prepared = job.GetAwaiter().GetResult();
            _latches = _prepared.Layout.Sites.Select(_ => new Latch()).ToArray();
            foreach (var line in _prepared.Layout.Log) GD.Print("[WP-25 fauna] " + line);
            _force = true;
        }
        if (_prepared is null) return;
        if (_world.Instance != _instance) { _instance = _world.Instance; _force = true; }
        _sinceCheck += Math.Min(delta, 0.1);
        if (!_force && _sinceCheck < CheckSeconds) return;
        _sinceCheck = 0;
        var weather = _world.WeatherNow;
        var sun = _world.Sun.AltDeg;
        var anywhere = _force || _world.Transition != WorldTransition.None || _world.Scrubbing;
        var frustum = anywhere ? null : GetViewport()?.GetCamera3D()?.GetFrustum();
        for (var i = 0; i < _latches.Length; i++)
        {
            var site = _prepared.Layout.Sites[i];
            var latch = _latches[i];
            var target = FaunaSchedule.Target(site.Kind, sun, _world.Clock, weather);
            if (latch.Applied && latch.Set == target) continue;
            if (latch.Applied && !anywhere && (frustum is null || InView(site.Bounds, frustum))) continue;
            Apply(i, target);
        }
        _force = false;
    }

    private void Apply(int index, FaunaSet? target)
    {
        var latch = _latches[index];
        _host ??= Services.Get<IRenderHost>();
        if (latch.Handle is { } old) { _host.Unmount(old); latch.Handle = null; }
        if (target is { } set && _prepared!.Sets[index][(int)set] is { Meshes.Count: > 0 } output)
            latch.Handle = _host.Mount(output, Package);
        latch.Set = target;
        latch.Applied = true;
    }

    /// <summary>Conservative AABB-frustum test: outside only when the box lies fully beyond one of the (outward) frustum planes.</summary>
    private static bool InView(Box3 box, Godot.Collections.Array<Plane> planes)
    {
        foreach (var plane in planes)
        {
            var n = plane.Normal;
            // The box corner deepest along the inward direction (−normal).
            var corner = new Vector3(n.X > 0 ? box.Min.X : box.Max.X, n.Y > 0 ? box.Min.Y : box.Max.Y, n.Z > 0 ? box.Min.Z : box.Max.Z);
            if (plane.DistanceTo(corner) > 0) return false;
        }
        return true;
    }

    private void Release()
    {
        if (_host is not null)
            foreach (var latch in _latches)
                if (latch.Handle is { } handle) _host.Unmount(handle);
        _latches = [];
        _prepared = null;
    }
}
