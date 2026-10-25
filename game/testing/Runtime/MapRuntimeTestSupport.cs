using Campus.Kernel.Diagnostics;
using Campus.Kernel.Post;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using static Campus.Tests.StreamLifecycleTestSupport;

namespace Campus.Tests;

internal static class MapRuntimeTestSupport
{
    internal sealed class MapRuntimeFixture : IDisposable
    {
        private readonly Node _stage = new();
        private readonly Camera3D _view = new();
        public MapRuntime Runtime { get; } = new();
        public MapRuntimeReadinessProducer Producer { get; } = new();
        public RuntimeRenderer Renderer { get; } = new();
        public ArrivalStream Stream { get; } = new();
        public ArrivalCollision Collision { get; } = new();
        public TestCameraRig Camera { get; } = new();
        public Task Mount { get; }
        public MapRuntimeFixture(IMapStreamState? stream = null, IMapContext? context = null, ITerrainPreparation? terrain = null)
        {
            var registry = new ServiceRegistry();
            if (stream is null)
            {
                registry.Discover([typeof(ArrivalStream)], _ => "fixture");
                registry.Add(Stream);
            }
            _stage.AddChild(Producer);
            Runtime.Bind(new(registry, stream ?? Stream, new RuntimeBake(), Collision, Renderer,
                Camera, terrain ?? new FlowTerrainPreparation(), _view));
            _stage.AddChild(Runtime);
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_stage);
            Runtime.SetProcess(false);
            Mount = Runtime.Mount(context ?? new TestMapContext(TestMapContext.BackdropDescriptor()));
        }
        public void Dispose() { Runtime.StopNewWork(); _stage.Free(); _view.Free(); }
    }

    internal sealed class ArrivalPreparation(Task points) : ITerrainPreparation
    {
        public bool IsPrepared(IWorldModel world, N.Vector2 point) => true;
        public Task PreparePoints(IMapContext context, IReadOnlyList<N.Vector2> locations, CancellationToken cancellation = default) => points;
        public Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default) => Task.CompletedTask;
    }

    internal sealed class RuntimeRenderer : IRenderReadiness
    {
        public Exception? Failure { get; set; }
        public bool Pending { get; set; }
    }

    internal sealed class RuntimeBake : IBakeService
    {
        public BakeCacheStatistics CacheStatistics => default;
        public IBakeHandle Ensure(BakeKey key, BakePriority priority) => throw new NotSupportedException();
        public BakeProgress Progress => default;
        public Exception? Failure => null;
        public long CacheBytes => 0;
    }

    [KernelService(typeof(ArrivalStream), Map = "fixture-map")]
    internal sealed class ArrivalStream : IMapStreamState, IMapAware
    {
        public bool Loaded { get; private set; }
        public void OnMapLoaded(IMapContext context) => Loaded = true;
        public void OnMapUnloading(MapId from) => Loaded = false;
        public bool IsUnloaded => !Loaded;
        public bool ResidentReady { get; set; }
        public bool DestinationReady { get; set; }
        public Exception? Failure { get; set; }
        public Task DrainAsync() => Task.CompletedTask;
        public MapStreamDestination? Destination { get; private set; }
        public bool HasPendingDestination => !DestinationReady;
        private readonly TestDestinationPins _pins = new();
        public IReadOnlyList<MapStreamDestination> PinnedDestinations => _pins.Destinations;
        public IDisposable PinDestination(MapStreamDestination destination) => _pins.Acquire(destination);
        public bool PinnedDestinationReady(MapStreamDestination destination) => PinnedDestinations.Contains(destination);
        public void ClearDestination() => Destination = null;
        public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null) => Destination = new(destination, mode, space, collisionFeet);
    }

    internal sealed class ArrivalCollision : ICollisionWorld
    {
        public bool CollisionReady { get; set; }
        public Exception? Failure { get; set; }
        public bool Ready(N.Vector3 point, float radius) => CollisionReady;
        public SweepHit? Sweep(N.Vector3 from, N.Vector3 to, float radius, float height) => null;
        public SupportHit? SupportY(float x, float z, float yHint) => null;
        public SweepHit? SupportRay(N.Vector3 from, N.Vector3 to) => null;
        public int NearbyBoundaries(N.Vector3 feet, float radius, float height, Span<BoundaryHit> hits) => 0;
    }
}
