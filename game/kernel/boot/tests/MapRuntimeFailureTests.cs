using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using GdUnit4;
using Godot;
using N = System.Numerics;
using static GdUnit4.Assertions;
using static Campus.Tests.MapRuntimeTestSupport;
using static Campus.Tests.StreamLifecycleTestSupport;

namespace Campus.Tests;

[TestSuite]
public sealed class MapRuntimeFailureTests
{
    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void VisibleProducerFailureReachesTheGlobalProjectionWithoutAPendingArrival()
    {
        foreach (var completeArrival in new[] { false, true })
        {
            using var fixture = new MapRuntimeFixture();
            fixture.Stream.ResidentReady = fixture.Stream.DestinationReady = true;
            fixture.Producer.ContentReady = true;
            fixture.Runtime._Process(0);
            AssertThat(fixture.Mount.IsCompletedSuccessfully).IsTrue();
            IMapArrival? arrival = null;
            if (completeArrival)
            {
                arrival = fixture.Runtime.Arrive(fixture.Camera.Pose, ViewMode.Fly);
                fixture.Runtime._Process(0);
                AssertThat(arrival.Done.IsCompletedSuccessfully).IsTrue();
            }
            var original = new InvalidDataException("visible producer failed");
            fixture.Producer.Failure = original;
            IMapRuntime globalSource = fixture.Runtime;
            AssertThat(globalSource.Failure is null && fixture.Renderer.Failure is null).IsTrue();
            fixture.Runtime._Process(0);
            AssertThat(globalSource.Failure is InvalidDataException && ReferenceEquals(globalSource.Failure, original)).IsTrue();
            AssertThat(arrival is null || arrival.Done.IsCompletedSuccessfully).IsTrue();
            // The same cause reaching another route still occupies one slot in the global projection.
            fixture.Renderer.Failure = original;
            fixture.Runtime._Process(0);
            AssertThat(ReferenceEquals(globalSource.Failure, original)).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task RetainedParticipantFailuresReachTheSameGlobalProjectionAndKeepTheirIdentity()
    {
        using var stream = new StreamFixture();
        var participant = stream.CollisionParticipant;
        participant.Need = StreamTileNeed.Required;
        await stream.Until(() => participant.Mounted.Count > 0);
        participant.Need = StreamTileNeed.Retain;
        await stream.Until(() => participant.Retained);
        using var fixture = new MapRuntimeFixture(stream.Director, stream.Context);
        fixture.Runtime._Process(0);
        AssertThat(fixture.Mount.IsCompletedSuccessfully && participant.Mounted.Count > 0).IsTrue();
        var original = new InvalidDataException("retained participant failed");
        participant.Failure = original;
        IMapRuntime globalSource = fixture.Runtime;
        AssertThat(stream.Director.Failure is null && globalSource.Failure is null).IsTrue();
        stream.Director._Process(0);
        AssertThat(stream.Director.Failure is InvalidDataException && ReferenceEquals(stream.Director.Failure, original) && globalSource.Failure is null).IsTrue();
        fixture.Runtime._Process(0);
        AssertThat(globalSource.Failure is InvalidDataException && ReferenceEquals(globalSource.Failure, original)).IsTrue();
        AssertThat(!stream.Director.ResidentReady && !stream.Director.DestinationReady && !stream.Director.ViewReady).IsTrue();
        var later = new InvalidOperationException("another participant failure");
        participant.Failure = new AggregateException(original, later);
        stream.Director._Process(0);
        fixture.Runtime._Process(0);
        var collected = globalSource.Failure as AggregateException ?? throw new InvalidOperationException("Participant faults were not projected.");
        var failures = collected.Flatten().InnerExceptions;
        AssertThat(failures.Count == 2 && failures.Contains(original) && failures.Contains(later)).IsTrue();
        stream.Director._Process(0);
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(globalSource.Failure, collected)).IsTrue();
        try { await stream.Unload(); throw new InvalidOperationException("Drain lost retained participant failures."); }
        catch (AggregateException error)
        {
            AssertThat(error.Flatten().InnerExceptions.Contains(original) && error.Flatten().InnerExceptions.Contains(later)).IsTrue();
        }
        AssertThat(stream.Director.IsUnloaded && participant.Mounted.Count == 0).IsTrue();
    }
}

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
        public MapRuntimeFixture(IMapStreamState? stream = null, IMapContext? context = null)
        {
            var registry = new ServiceRegistry();
            if (stream is null)
            {
                registry.Discover([typeof(ArrivalStream)], _ => "fixture");
                registry.Add(Stream);
            }
            _stage.AddChild(Producer);
            Runtime.Bind(new(registry, stream ?? Stream, new RuntimeBake(), Collision, Renderer,
                new RuntimePipelines(), Camera, new FlowTerrainPreparation(), _view));
            _stage.AddChild(Runtime);
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_stage);
            Runtime.SetProcess(false);
            Mount = Runtime.Mount(context ?? new TestMapContext(BackdropInputsTests.Descriptor()));
        }
        public void Dispose() { Runtime.StopNewWork(); _stage.Free(); _view.Free(); }
    }

    internal sealed class RuntimeRenderer : IRenderReadiness
    {
        public Exception? Failure { get; set; }
        public bool Pending => false;
    }

    internal sealed class RuntimePipelines : IPipelinePreparation
    {
        public Task PrepareAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    internal sealed class RuntimeBake : IBakeService
    {
        public IBakeHandle Ensure(BakeKey key, BakePriority priority) => throw new NotSupportedException();
        public BakeProgress Progress => default;
        public Exception? Failure => null;
        public long CacheBytes => 0;
        public void Clear() { }
        public void ClearFailures() { }
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
        public IReadOnlyList<MapStreamDestination> PinnedDestinations { get; private set; } = [];
        public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations) => PinnedDestinations = destinations;
        public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) => Destination = new(destination, mode, space);
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
