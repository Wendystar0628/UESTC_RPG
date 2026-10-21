using Campus.Kernel.Diagnostics;
using Campus.Kernel.Post;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
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
    [TestCase, TestCategory(TestReason.Regression)]
    public void SharedRetentionPreservesNestedAndEmptyCausesAndConcurrentEffectFailures()
    {
        var original = new InvalidDataException("producer failed");
        var empty = new AggregateException();
        AssertThat(ReferenceEquals(RetainedFailure.Append(null, original), original) &&
            ReferenceEquals(RetainedFailure.Append(null, empty), empty)).IsTrue();
        var nested = new AggregateException(original, new AggregateException(empty, original));
        var retained = RetainedFailure.Append(null, nested);
        var causes = retained as AggregateException ?? throw new InvalidOperationException("Producer causes were not retained.");
        AssertThat(causes.InnerExceptions.Count == 2 && ReferenceEquals(causes.InnerExceptions[0], original) &&
            ReferenceEquals(causes.InnerExceptions[1], empty) &&
            ReferenceEquals(RetainedFailure.Append(retained, nested), retained) &&
            ReferenceEquals(RetainedFailure.Append(retained, original), retained)).IsTrue();
        Exception? effects = null;
        var errors = Enumerable.Range(0, 16).Select(index => new InvalidOperationException($"effect {index}")).ToArray();
        Parallel.For(0, errors.Length * 2, index => EffectFailure.Record(ref effects, errors[index % errors.Length]));
        var effectCauses = (effects as AggregateException ?? throw new InvalidOperationException("Concurrent effect failures were lost."))
            .Flatten().InnerExceptions;
        AssertThat(effectCauses.Count == errors.Length && errors.All(error => effectCauses.Any(cause => ReferenceEquals(cause, error)))).IsTrue();
        var observed = effects;
        foreach (var error in errors) EffectFailure.Record(ref effects, error);
        AssertThat(ReferenceEquals(effects, observed)).IsTrue();
    }

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
            var published = globalSource.Failure as AppStartupException ?? throw new InvalidOperationException("Map failure has no startup code.");
            AssertThat(published.Code == AppStartupFailureCodes.MapPreparation && ReferenceEquals(published.InnerException, original)).IsTrue();
            AssertThat(arrival is null || arrival.Done.IsCompletedSuccessfully).IsTrue();
            // The same cause reaching another route still occupies one slot in the global projection.
            fixture.Renderer.Failure = original;
            fixture.Runtime._Process(0);
            AssertThat(ReferenceEquals(globalSource.Failure, published)).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void ReportedWorldFailureUsesTheExistingMapLifetimeProjection()
    {
        using var fixture = new MapRuntimeFixture();
        IMapRuntime source = fixture.Runtime;
        var original = new InvalidOperationException("world crossfade failed");
        var published = source.ReportFailure(original);
        AssertThat(ReferenceEquals(source.Failure, published) && ReferenceEquals(published.InnerException, original) &&
            published.Code == AppStartupFailureCodes.MapPreparation && source.Arrival is null).IsTrue();
        AssertThat(ReferenceEquals(source.ReportFailure(original), published)).IsTrue();
        var later = new AppStartupException(AppStartupFailureCodes.Installation, "map resource is missing");
        var collected = source.ReportFailure(later);
        var causes = collected.InnerException as AggregateException ?? throw new InvalidOperationException("Reported map failures were not aggregated.");
        AssertThat(ReferenceEquals(source.Failure, collected) && collected.Code == later.Code &&
            causes.InnerExceptions.Count == 2 && causes.InnerExceptions.Contains(original) && causes.InnerExceptions.Contains(later)).IsTrue();
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(fixture.Mount.Exception!.InnerException, collected)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void ExplicitStartupCodeSurvivesProducerAggregation()
    {
        using var fixture = new MapRuntimeFixture();
        var original = new AppStartupException(AppStartupFailureCodes.Installation, "map resource is missing");
        fixture.Stream.Failure = original;
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(fixture.Runtime.Failure, original) &&
            ReferenceEquals(fixture.Mount.Exception!.InnerException, original)).IsTrue();
        var later = new InvalidDataException("visible producer cleanup failed");
        fixture.Producer.Failure = later;
        fixture.Runtime._Process(0);
        var published = fixture.Runtime.Failure as AppStartupException ?? throw new InvalidOperationException("Map failure has no startup code.");
        var causes = published.InnerException as AggregateException ?? throw new InvalidOperationException("Map failure lost its diagnostic causes.");
        AssertThat(published.Code == AppStartupFailureCodes.Installation && causes.InnerExceptions.Count == 2 &&
            causes.InnerExceptions.Contains(original) && causes.InnerExceptions.Contains(later)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void EmptyAggregateRemainsAPublishedFailureAcrossRepeatedPollingAndAggregation()
    {
        using var fixture = new MapRuntimeFixture();
        var original = new AggregateException();
        fixture.Stream.Failure = original;
        fixture.Runtime._Process(0);
        var published = fixture.Runtime.Failure as AppStartupException ?? throw new InvalidOperationException("Empty aggregate was not published.");
        AssertThat(published.Code == AppStartupFailureCodes.MapPreparation && ReferenceEquals(published.InnerException, original) &&
            ReferenceEquals(fixture.Mount.Exception!.InnerException, published)).IsTrue();
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(fixture.Runtime.Failure, published)).IsTrue();
        var later = new AppStartupException(AppStartupFailureCodes.Installation, "map resource is missing");
        var nested = new AggregateException();
        fixture.Producer.Failure = new AggregateException(original, later, nested);
        fixture.Runtime._Process(0);
        var collected = fixture.Runtime.Failure as AppStartupException ?? throw new InvalidOperationException("Aggregate failure was not published.");
        var causes = collected.InnerException as AggregateException ?? throw new InvalidOperationException("Aggregate failure lost its diagnostic causes.");
        AssertThat(collected.Code == AppStartupFailureCodes.Installation && causes.InnerExceptions.Count == 3 &&
            causes.InnerExceptions.Contains(original) && causes.InnerExceptions.Contains(later) && causes.InnerExceptions.Contains(nested)).IsTrue();
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(fixture.Runtime.Failure, collected)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task RetainedParticipantFailuresReachTheSameGlobalProjectionAndKeepTheirIdentity()
    {
        using var stream = new StreamFixture();
        var participant = stream.CollisionParticipant;
        participant.Need = StreamTileNeed.Required;
        stream.Director.Prefetch(stream.Camera.Pose, ViewMode.Fly);
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
        var published = globalSource.Failure as AppStartupException ?? throw new InvalidOperationException("Map failure has no startup code.");
        AssertThat(published.Code == AppStartupFailureCodes.MapPreparation && ReferenceEquals(published.InnerException, original)).IsTrue();
        AssertThat(!stream.Director.ResidentReady && !stream.Director.DestinationReady && !stream.Director.ViewReady).IsTrue();
        var later = new InvalidOperationException("another participant failure");
        participant.Failure = new AggregateException(original, later);
        stream.Director._Process(0);
        fixture.Runtime._Process(0);
        var collected = globalSource.Failure as AppStartupException ?? throw new InvalidOperationException("Participant faults have no startup code.");
        var causes = collected.InnerException as AggregateException ?? throw new InvalidOperationException("Participant faults were not projected.");
        var failures = causes.Flatten().InnerExceptions;
        AssertThat(collected.Code == AppStartupFailureCodes.MapPreparation && failures.Count == 2 && failures.Contains(original) && failures.Contains(later)).IsTrue();
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
                Camera, new FlowTerrainPreparation(), _view));
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
