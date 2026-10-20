using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using Campus.Contracts.Bake;
using Campus.Kernel.Registry;
using N = System.Numerics;

namespace Campus.Tests;

[TestSuite]
public sealed class MapRuntimeRuntimeTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task CaptureQueriesOutsideDrawSignalsAndWaitsForFreshDraw()
    {
        var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Godot runtime required.");
        var host = new Node();
        tree.Root.AddChild(host);
        var ready = false;
        var inDraw = false;
        var processes = 0;
        var draws = 0;
        var readyAtDraw = -1;
        void BeforeDraw() => inDraw = true;
        void AfterDraw() { inDraw = true; draws++; }
        void BeforeProcess() { inDraw = false; processes++; }
        bool Ready()
        {
            AssertThat(GodotThread.IsMainThread() && processes > 0 && !inDraw).OverrideFailureMessage("Readiness ran outside a safe process boundary.").IsTrue();
            if (ready && readyAtDraw < 0) readyAtDraw = draws;
            return ready;
        }
        RenderingServer.Singleton.FramePreDraw += BeforeDraw;
        RenderingServer.Singleton.FramePostDraw += AfterDraw;
        tree.ProcessFrame += BeforeProcess;
        var rendering = RenderingServer.RenderLoopEnabled;
        try
        {
            var waiting = FrameAwaiter.ReadyDrawAsync(host, Ready, TimeSpan.FromSeconds(5));
            await FrameAwaiter.ProcessAsync(host);
            AssertThat(!waiting.IsCompleted).OverrideFailureMessage("Unready content completed its presentation barrier.").IsTrue();
            ready = true;
            await waiting;
            AssertThat(readyAtDraw >= 0 && draws > readyAtDraw).OverrideFailureMessage("Ready content completed without a fresh draw.").IsTrue();
            RenderingServer.RenderLoopEnabled = false;
            using var cancellation = new CancellationTokenSource();
            var pending = FrameAwaiter.ReadyDrawAsync(host, Ready, TimeSpan.FromSeconds(5), cancellation.Token);
            await FrameAwaiter.ProcessAsync(host);
            AssertThat(!pending.IsCompleted).OverrideFailureMessage("An undrawn view became presented.").IsTrue();
            cancellation.Cancel();
            try { await pending; throw new InvalidOperationException("Disabled rendering did not remain cancellable."); }
            catch (OperationCanceledException) { }
            try { await FrameAwaiter.ReadyDrawAsync(host, Ready, TimeSpan.FromMilliseconds(50)); throw new InvalidOperationException("Disabled rendering completed instead of timing out."); }
            catch (TimeoutException) { }
        }
        finally
        {
            RenderingServer.RenderLoopEnabled = rendering;
            RenderingServer.Singleton.FramePreDraw -= BeforeDraw;
            RenderingServer.Singleton.FramePostDraw -= AfterDraw;
            tree.ProcessFrame -= BeforeProcess;
            host.Free();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void ArrivalRequiresResidentRingsCurrentFloorAndWalkingCollision()
    {
        var stage = new Node();
        var runtime = new MapRuntime();
        var viewCamera = new Camera3D();
        try
        {
            var producer = new MapRuntimeReadinessProducer();
            stage.AddChild(producer);
            var stream = new ArrivalStream();
            var collision = new ArrivalCollision();
            var camera = new TestCameraRig();
            var destination = camera.Pose;
            var registry = new ServiceRegistry();
            registry.Discover([typeof(ArrivalStream)], _ => "fixture");
            registry.Add(stream);
            runtime.Bind(new(registry, stream, new RuntimeBake(), collision, producer,
                new RuntimePipelines(), camera, new FlowTerrainPreparation(), viewCamera));
            stage.AddChild(runtime);
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(stage);
            var mount = runtime.Mount(new TestMapContext(BackdropInputsTests.Descriptor()));
            runtime._Process(0);
            AssertThat(mount.IsCompleted).IsFalse();
            stream.ResidentReady = true;
            runtime._Process(0);
            AssertThat(mount.IsCompletedSuccessfully && stream.Loaded).IsTrue();
            stream.ResidentReady = false;
            var arrival = runtime.Arrive(destination, ViewMode.Walk);
            runtime._Process(0);
            AssertThat(arrival.Done.IsCompleted).IsFalse();
            stream.ResidentReady = true;
            runtime._Process(0);
            AssertThat(arrival.Done.IsCompleted).IsFalse();
            stream.DestinationReady = true;
            runtime._Process(0);
            AssertThat(arrival.Done.IsCompleted).IsFalse();
            producer.ContentReady = true;
            runtime._Process(0);
            AssertThat(arrival.Done.IsCompleted).IsFalse();
            collision.CollisionReady = true;
            runtime._Process(0);
            AssertThat(arrival.Done.IsCompletedSuccessfully && arrival.Completed == arrival.Total).IsTrue();
            var replaced = runtime.Arrive(destination, ViewMode.Fly);
            var current = runtime.Arrive(destination, ViewMode.Fly);
            AssertThat(replaced.Done.IsCanceled).IsTrue();
            runtime._Process(0);
            AssertThat(current.Done.IsCompletedSuccessfully).IsTrue();
            var original = new InvalidOperationException("destination failed");
            stream.Failure = original;
            var failed = runtime.Arrive(destination, ViewMode.Fly);
            AssertThat(runtime.Failure is null && !failed.Done.IsCompleted).OverrideFailureMessage("Reading failure advanced runtime collection.").IsTrue();
            runtime._Process(0);
            AssertThat(failed.Done.IsFaulted).IsTrue();
            AssertThat(ReferenceEquals(failed.Done.Exception!.InnerException, original) && ReferenceEquals(runtime.Failure, original)).IsTrue();
            var rendering = new InvalidDataException("rendering failed");
            var physical = new InvalidOperationException("collision failed");
            producer.Failure = rendering; collision.Failure = physical;
            AssertThat(ReferenceEquals(runtime.Failure, original)).OverrideFailureMessage("Reading failure collected new producer faults.").IsTrue();
            runtime._Process(0);
            var collected = runtime.Failure as AggregateException ?? throw new InvalidOperationException("Producer failures were not collected by Update.");
            AssertThat(collected.Flatten().InnerExceptions.Count == 3 && collected.Flatten().InnerExceptions.Contains(original) &&
                collected.Flatten().InnerExceptions.Contains(rendering) && collected.Flatten().InnerExceptions.Contains(physical)).IsTrue();
            runtime._Process(0);
            AssertThat(ReferenceEquals(runtime.Failure, collected)).OverrideFailureMessage("Unchanged faults were collected twice.").IsTrue();
        }
        finally
        {
            runtime.StopNewWork();
            stage.Free();
            viewCamera.Free();
        }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task PreparationFailureStillStopsAllProducersDrainsStreamAndAwaitsEveryDataOwner()
    {
        var runtime = new MapRuntime();
        var stage = new Node();
        var viewCamera = new Camera3D();
        var order = new List<string>();
        var preparation = new TaskCompletionSource();
        var stream = new DrainStream(order);
        var original = new InvalidDataException("preparation failure");
        var stopping = new InvalidOperationException("producer stop failure");
        var draining = new InvalidOperationException("stream cleanup failure");
        var releasing = new InvalidOperationException("data cleanup failure");
        var first = new FirstLifetimeParticipant(order, preparation.Task, stopping);
        var second = new SecondLifetimeParticipant(order);
        var context = new TestMapContext(BackdropInputsTests.Descriptor() with { Id = new("lifetime-fixture") });
        var map = context.Descriptor.Id;
        var registry = new ServiceRegistry();
        registry.Discover([typeof(FirstLifetimeParticipant), typeof(SecondLifetimeParticipant)], _ => "fixture");
        registry.Add(first); registry.Add(second);
        var renderer = new MapRuntimeReadinessProducer(); stage.AddChild(renderer);
        runtime.Bind(new(registry, stream, new RuntimeBake(), new ArrivalCollision(), renderer,
            new RuntimePipelines(), new TestCameraRig(), new FlowTerrainPreparation(), viewCamera));
        stage.AddChild(runtime);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(stage);
        var mounting = runtime.Mount(context);
        try
        {
            var shutdown = runtime.ShutdownAsync(map);
            AssertThat(order.Count == 0 && !shutdown.IsCompleted).IsTrue();
            preparation.SetException(original);
            await stream.Entered.Task;
            AssertThat(order.SequenceEqual(["stop-first", "stop-second", "stream"]) && !shutdown.IsCompleted).IsTrue();
            stream.Completion.SetException(draining);
            await first.Entered.Task;
            AssertThat(order.Last() == "data-first" && !order.Contains("data-second") && !shutdown.IsCompleted).IsTrue();
            first.Completion.SetException(releasing);
            await second.Entered.Task;
            AssertThat(!shutdown.IsCompleted).IsTrue();
            second.Completion.SetResult();
            try { await shutdown; throw new InvalidOperationException("Shutdown lost its preparation and cleanup failures."); }
            catch (AggregateException error)
            {
                var failures = error.Flatten().InnerExceptions;
                AssertThat(failures.Contains(original) && failures.Contains(stopping) && failures.Contains(draining) && failures.Contains(releasing)).IsTrue();
            }
            AssertThat(order.SequenceEqual(["stop-first", "stop-second", "stream", "data-first", "data-second"])).IsTrue();
            AssertThat(stream.IsUnloaded && !mounting.IsCompletedSuccessfully && ReferenceEquals(runtime.ShutdownAsync(map), shutdown)).IsTrue();
            try { await mounting; } catch (OperationCanceledException) { }
        }
        finally { stage.Free(); viewCamera.Free(); }
    }

    private class LifetimeParticipant(string name, List<string> order, Task preparation, Exception? stopping = null) : IMapAware, IMapDataParticipant
    {
        public TaskCompletionSource Entered { get; } = new();
        public TaskCompletionSource Completion { get; } = new();
        public Task Prepare(IMapContext context) => preparation;
        public void OnMapLoaded(IMapContext context) { }
        public void OnMapUnloading(MapId from)
        {
            order.Add("stop-" + name);
            if (stopping is not null) throw stopping;
        }
        public Task UnloadAsync(MapId from)
        {
            order.Add("data-" + name); Entered.SetResult(); return Completion.Task;
        }
    }

    [KernelService(typeof(FirstLifetimeParticipant), Map = "lifetime-fixture")]
    private sealed class FirstLifetimeParticipant(List<string> order, Task preparation, Exception stopping)
        : LifetimeParticipant("first", order, preparation, stopping) { }
    [KernelService(typeof(SecondLifetimeParticipant), Map = "lifetime-fixture")]
    private sealed class SecondLifetimeParticipant(List<string> order)
        : LifetimeParticipant("second", order, Task.CompletedTask) { }

    private sealed class RuntimePipelines : IPipelinePreparation
    {
        public Task PrepareAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class RuntimeBake : IBakeService
    {
        public IBakeHandle Ensure(BakeKey key, BakePriority priority) => throw new NotSupportedException();
        public BakeProgress Progress => default;
        public Exception? Failure => null;
        public long CacheBytes => 0;
        public void Clear() { }
        public void ClearFailures() { }
    }

    private sealed class DrainStream(List<string> order) : IMapStreamState
    {
        public TaskCompletionSource Entered { get; } = new();
        public TaskCompletionSource Completion { get; } = new();
        public Exception? Failure => null;
        public bool IsUnloaded => Completion.Task.IsCompleted;
        public bool ResidentReady => false;
        public bool DestinationReady => false;
        public MapStreamDestination? Destination => null;
        public bool HasPendingDestination => false;
        public IReadOnlyList<MapStreamDestination> PinnedDestinations => [];
        public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations) { }
        public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) => throw new NotSupportedException();
        public Task DrainAsync() { order.Add("stream"); Entered.SetResult(); return Completion.Task; }
    }

    [KernelService(typeof(ArrivalStream), Map = "fixture-map")]
    private sealed class ArrivalStream : IMapStreamState, IMapAware
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

    private sealed class ArrivalCollision : ICollisionWorld
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

internal sealed partial class MapRuntimeReadinessProducer : Node, IRenderReadiness, IViewContentReadiness
{
    public bool ContentReady { get; set; }
    public bool IsReady(Pose pose) => ContentReady;
    public Exception? Failure { get; set; }
    public bool Pending => false;
}
