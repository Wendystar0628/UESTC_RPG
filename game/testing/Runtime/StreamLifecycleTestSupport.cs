using Campus.Kernel.Boot;
using Campus.Gen.Tests.Support;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Bake;
using Campus.Style.Materials;
using Campus.Kernel.Lod;
using Campus.Kernel.Registry;
using Campus.Kernel.Stream;
using GdUnit4;
using Godot;
using N = System.Numerics;
namespace Campus.Tests;
internal static class StreamLifecycleTestSupport
{
    internal static readonly IAppScreenCoverage CoveredScreen = new Coverage(() => true);
    internal static readonly IAppScreenCoverage VisibleWorld = new Coverage(() => false);
    internal sealed class Coverage(Func<bool> covered) : IAppScreenCoverage
    {
        public bool IsCovered => covered();
    }
    internal static void Check(bool condition) { if (!condition) throw new InvalidOperationException("Stream lifecycle contract failed."); }
    internal sealed class StreamFixture : IDisposable
    {
        private readonly Node3D _stage = new();
        private readonly Camera3D _view = new() { Current = true };
        private readonly ShaderMaterial _material = new ShaderMaterialSource(new RequiredResources()).Create(ShaderId.VegTrunk);
        public StreamDirector Director { get; } = new();
        public TestCameraRig Camera { get; }
        public TestMapContext Context { get; }
        public ControlledTerrain Terrain { get; } = new();
        public StreamBake Bake { get; }
        public ControlledGeometry Geometry { get; } = new();
        public FixtureCollisionParticipant CollisionParticipant { get; } = new();
        public Collision Collision { get; } = new();
        public StreamMountQueue Mount { get; }
        public StreamFixture(bool geometry = false, bool elevated = false, Task<PreparedBakeTiles>? catalog = null,
            bool staticTitle = false, StreamMountQueue? mount = null)
        {
            Mount = mount ?? new(() => 0);
            var bounds = elevated ? new Box3(new(-512, 0, -512), new(512, 10, 512)) : new Box3(N.Vector3.Zero, new(128, 10, 128));
            var map = TestMapContext.BackdropDescriptor() with { Id = new("stream-fixture"), Bounds = bounds, TileMin = new((int)bounds.Min.X / TileGrid.BaseEdge, (int)bounds.Min.Z / TileGrid.BaseEdge),
                TileMax = new((int)bounds.Max.X / TileGrid.BaseEdge - 1, (int)bounds.Max.Z / TileGrid.BaseEdge - 1) };
            Context = new(map, new FakeWorld { Boundary = new([]) });
            Camera = new(pose: elevated ? new Pose(new(-193, 158, 111), 0, -68, 60) : new Pose(new(64, 2, 64), 0, 0, 60));
            Bake = new(bounds);
            var modules = new Dictionary<string, IReadOnlyList<TileKey>>();
            if (geometry)
            {
                var tiles = new List<TileKey>();
                for (var x = map.TileMin.X; x <= map.TileMax.X; x++) for (var z = map.TileMin.Z; z <= map.TileMax.Z; z++) tiles.Add(new(x, z));
                modules.Add("fixture.geometry", tiles);
            }
            var registry = new ServiceRegistry(); registry.Discover([typeof(FixtureCollisionParticipant)], _ => "fixture");
            registry.Add(CollisionParticipant);
            var flow = new FlowTestRig();
            Director.Bind(new(registry, new CatalogSource(catalog ?? Task.FromResult(new PreparedBakeTiles(modules, []))), Bake, Mount,
                Camera, new Observer(Camera), flow.World, Collision, new TestMapService(), Terrain, new Reflection(),
                new Materials(_material), new Materials(_material), _view, true, Geometry));
            _stage.AddChild(_view); _stage.AddChild(Director);
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_stage);
            Director.SetProcess(false); Director.OnMapLoaded(Context);
            if (!staticTitle) Director.Prefetch(Camera.Pose, ViewMode.Fly);
        }
        public void Tick(bool mount = true)
        {
            Director._Process(.1);
            if (mount) Mount.Drain(CoveredScreen, Director.FinishMountFrame, Director.ReleaseRetired, () => Director.CanActivateVisual);
            else Director.FinishMountFrame(100);
            if (Director.Failure is { } failure) throw failure;
        }
        public async Task Until(Func<bool> ready, bool mount = true)
        {
            for (var frame = 0; frame < TestSamples.FrameLoop; frame++)
            { Tick(mount); if (ready()) return; await FrameAwaiter.ProcessAsync(Director); }
            throw new TimeoutException("Stream fixture did not reach its public lifecycle condition.");
        }
        public Task Unload() { Director.OnMapUnloading(Context.Descriptor.Id); return Director.DrainAsync(); }
        public void Dispose() { _stage.Free(); _material.Dispose(); }
    }
    private sealed class CatalogSource(Task<PreparedBakeTiles> catalog) : IBakeContextSource
    {
        public GroundCoverData GetPreparedGroundCover(IMapContext context) => throw new NotSupportedException();
        public Task<PreparedBakeTiles> GetPreparedModuleTilesAsync(IMapContext context) => catalog;
        public BuildContext GetPreparedContext(IMapContext context, string moduleId) => throw new NotSupportedException();
    }
    internal sealed class ControlledTerrain : ITerrainPreparation
    {
        public bool Prepared { get; set; } = true;
        public List<TaskCompletionSource> Work { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public bool IsPrepared(IWorldModel world, N.Vector2 point) => Prepared;
        public Task PreparePoints(IMapContext context, IReadOnlyList<N.Vector2> points, CancellationToken cancellation = default)
        { if (Prepared) return Task.CompletedTask; var work = new TaskCompletionSource(); Work.Add(work); Tokens.Add(cancellation); return work.Task; }
        public Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default) => Task.CompletedTask;
    }
    internal sealed class StreamBake(Box3 bounds) : IBakeService
    {
        private readonly Dictionary<BakeKey, Handle> _handles = [];
        public Func<BakeKey, bool> Hold { get; set; } = _ => false;
        public IReadOnlyList<Handle> Waiting => _handles.Values.Where(handle => !handle.Done.IsCompleted).ToArray();
        public IBakeHandle Ensure(BakeKey key, BakePriority priority)
        { if (_handles.TryGetValue(key, out var value)) return value; var handle = new Handle(Bundle(key, bounds)); _handles.Add(key, handle); if (!Hold(key)) handle.Complete(); return handle; }
        public void CompleteLevel(LodLevel level) { foreach (var handle in Waiting.Where(handle => handle.TileKey.Level == level)) handle.Complete(); }
        public void Release() { Hold = _ => false; foreach (var handle in Waiting) handle.Complete(); }
        public BakeCacheStatistics CacheStatistics => default;
        public BakeProgress Progress => default;
        public Exception? Failure => null;
        public long CacheBytes => 0;
    }
    internal sealed class Handle(TileBundle tile) : IBakeHandle
    {
        private readonly TaskCompletionSource _done = new();
        public (TileKey Tile, LodLevel Level, bool Collision) TileKey => BakeRequestKeys.Parse(tile.Key);
        public BakeState State => Done.IsCompleted ? BakeState.Ready : BakeState.Pending;
        public object? Product => Done.IsCompleted ? tile : null;
        public TileBundle? Tile => Product as TileBundle;
        public Exception? Failure => null;
        public Task Done => _done.Task;
        public void Complete() => _done.TrySetResult();
    }
    internal sealed class ControlledGeometry : IStreamGeometryPreparation
    {
        public bool Hold { get; set; }
        public List<(PreparedTile Owner, TaskCompletionSource Done)> Waiting { get; } = [];
        public List<PreparedTile> Prepared { get; } = [];
        public async Task PrepareAsync(PreparedTile owner, TileBundle bundle)
        {
            Prepared.Add(owner);
            if (Hold)
            {
                var done = new TaskCompletionSource(); Waiting.Add((owner, done));
                await done.Task;
            }
            owner.Initialize(bundle);
        }
        public void Release() { Hold = false; foreach (var (_, done) in Waiting) done.SetResult(); Waiting.Clear(); }
        public void Fail(Exception error) { Hold = false; foreach (var (_, done) in Waiting) done.SetException(error); Waiting.Clear(); }
    }
    internal static TileBundle Bundle(BakeKey key, Box3 bounds)
    {
        var parsed = BakeRequestKeys.Parse(key);
        var source = new GeometryBundle(new(new ToonSpec(new("wall.lit"), new("wall.shade"))),
            [0, 0, 0, 1, 0, 0, 0, 0, 1], [0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2]);
        return new(key, parsed.Tile, parsed.Level, bounds, [BakeSurfacePacker.Pack(source, bounds, Campus.Contracts.Tests.PaletteFixture.Catalog)]);
    }
    [KernelService(typeof(FixtureCollisionParticipant), Map = "stream-fixture")]
    internal sealed class FixtureCollisionParticipant : IStreamTileParticipant
    {
        public StreamTileNeed Need { get; set; }
        public bool Retained { get; private set; }
        public HashSet<BakeKey> Mounted { get; } = [];
        public StreamMountKind Kind => StreamMountKind.Collision;
        public bool CanMount(BakeKey key) => BakeRequestKeys.Parse(key).Collision;
        public StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident)
        {
            var need = Need switch
            {
                StreamTileNeed.None => StreamTileNeed.None,
                StreamTileNeed.Required => StreamTileNeed.Required,
                StreamTileNeed.Retain => resident ? StreamTileNeed.Retain : StreamTileNeed.None,
                _ => throw new ArgumentOutOfRangeException(nameof(Need))
            };
            Retained |= need == StreamTileNeed.Retain;
            return need;
        }
        public IStreamMountWork CreateMount(BakeKey key, object product, BakePriority priority, long revision) =>
            new MountWork(this, key, priority, revision);
        public bool ResourcesUploaded { get; set; } = true;
        public bool IsReady(BakeKey key) => ResourcesUploaded && Mounted.Contains(key);
        public void Unmount(BakeKey key) => Mounted.Remove(key);
        public Exception? Failure { get; set; }

        private sealed class MountWork(FixtureCollisionParticipant owner, BakeKey key, BakePriority priority, long revision)
            : StreamMountWork(key, priority, revision, StreamMountKind.Collision)
        {
            protected override bool IsCurrent => true;
            protected override bool MountStep(double budgetMilliseconds) { owner.Mounted.Add(Key); return true; }
            protected override void Release() { }
            protected override void Failed(Exception error) => owner.Failure = error;
        }
    }
    private sealed class Observer(TestCameraRig camera) : IObserverState { public N.Vector3 Feet => camera.Pose.Position; public N.Vector3 Eye => Feet; public float YawDeg => camera.Pose.YawDeg; public NavSpace? Space => null; public ViewMode Mode => ViewMode.Fly; }
    internal sealed class Collision : ICollisionWorld
    {
        public bool CollisionReady { get; set; } = true;
        public Exception? Failure => null;
        public bool Ready(N.Vector3 point, float radius) => CollisionReady;
        public SweepHit? Sweep(N.Vector3 from, N.Vector3 to, float radius, float height) => null;
        public SupportHit? SupportY(float x, float z, float yHint) => null;
        public SweepHit? SupportRay(N.Vector3 from, N.Vector3 to) => null;
        public int NearbyBoundaries(N.Vector3 feet, float radius, float height, Span<BoundaryHit> hits) => 0;
    }
    private sealed class Materials(Material material) : IToonMaterialProvider, IShaderMaterialSource { public Material Get(ToonSpec spec) => material; public Material CreatePipelineMaterial(ToonKind kind) => throw new NotSupportedException(); public ShaderMaterial Create(ShaderId shader) => throw new NotSupportedException(); }
    private sealed class Reflection : IReflectionWorld
    {
        public Rid Scenario => default; public Material ProxyMaterial => throw new NotSupportedException();
        public event Action? Changed { add { } remove { } }
        public bool TryGetDemand(in StreamDemandView source, out StreamDemandView reflected) { reflected = default; return false; }
    }
    internal sealed class MountWork(StreamMountKind kind, long revision, Action<double> step, string module = "module", int slices = 1, Action? cancel = null, BakePriority? priority = null, MapId? map = null) : IStreamMountWork
    {
        public Exception? KeyFailure, RevisionFailure;
        public BakeKey Key => KeyFailure is { } error ? throw error : new(map ?? new("map"), module, "L0/0/0");
        public BakePriority EffectivePriority { get; set; } = priority ?? (kind == StreamMountKind.Collision ? BakePriority.Collision : BakePriority.Normal);
        public BakePriority Priority => EffectivePriority;
        public long Revision => RevisionFailure is { } error ? throw error : revision;
        public StreamMountKind Kind { get; } = kind;
        public int Cancels;
        private int _steps;
        public bool Step(double budgetMilliseconds) { step(budgetMilliseconds); return ++_steps >= slices; }
        public void Cancel() { Cancels++; cancel?.Invoke(); }
    }
}
