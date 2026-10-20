using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using Campus.Kernel.Registry;
using N = System.Numerics;
using static Campus.Tests.MapRuntimeTestSupport;

namespace Campus.Tests;

[TestSuite]
public sealed class MapRuntimeRuntimeTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void CaptureKeepsArrivalIdentityWhileWalkingViewChanges()
    {
        var context = new TestMapContext(BackdropInputsTests.Descriptor());
        using var fixture = new MapRuntimeFixture(context: context);
        var runtime = fixture.Runtime;
        var rig = fixture.Camera;
        var initial = rig.Pose;
        fixture.Stream.ResidentReady = fixture.Stream.DestinationReady = true;
        fixture.Collision.CollisionReady = fixture.Producer.ContentReady = true;
        runtime._Process(0);
        var arrival = runtime.Arrive(initial, ViewMode.Walk);
        rig.Mode = ViewMode.Walk;
        runtime._Process(0);
        AssertThat(arrival.Done.IsCompletedSuccessfully).IsTrue();
        var capture = new GameCaptureNode.CaptureDestination(nameof(CaptureKeepsArrivalIdentityWhileWalkingViewChanges),
            rig.Mode, context, 1, arrival, initial);
        Pose Read(IMapContext? current = null, long instance = 1, CancellationToken token = default) =>
            capture.ReadCurrentPose(current ?? context, instance, rig, runtime, token);
        void Cancelled(Action read)
        {
            try { read(); throw new InvalidOperationException("A replaced capture remained current."); }
            catch (OperationCanceledException) { }
        }
        var live = initial with { Position = initial.Position + N.Vector3.UnitY };
        rig.SetPose(live);
        fixture.Producer.RequiredPose = live;
        var producers = ViewContentReadiness.Capture(fixture.Producer);
        AssertThat(Read() == live && producers.IsReady(Read()) && !producers.IsReady(initial)).IsTrue();
        Cancelled(() => Read(new TestMapContext(context.Descriptor)));
        Cancelled(() => Read(instance: capture.WorldInstance + 1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Cancelled(() => Read(token: cancellation.Token));
        rig.Mode = ViewMode.Fly;
        Cancelled(() => Read());
        capture = capture with { Mode = ViewMode.Fly };
        Cancelled(() => Read());
        rig.SetPose(initial);
        AssertThat(Read() == initial).IsTrue();
        rig.Mode = ViewMode.Walk;
        capture = capture with { Mode = ViewMode.Walk };
        fixture.Producer.RequiredPose = null;
        var replacement = runtime.Arrive(initial, ViewMode.Walk);
        AssertThat(arrival.Done.IsCompletedSuccessfully && replacement.Destination == arrival.Destination).IsTrue();
        Cancelled(() => Read());
        arrival.Cancel();
        runtime._Process(0);
        AssertThat(replacement.Done.IsCompletedSuccessfully).IsTrue();
        var pending = runtime.Arrive(initial, ViewMode.Walk);
        capture = capture with { Arrival = pending };
        pending.Cancel();
        Cancelled(() => Read());
        capture = capture with { Arrival = runtime.Arrive(initial, ViewMode.Walk) };
        var failure = new InvalidDataException("capture producer failure");
        fixture.Stream.Failure = failure;
        runtime._Process(0);
        try { Read(); throw new InvalidOperationException("A producer failure became capture success."); }
        catch (InvalidDataException actual) { AssertThat(ReferenceEquals(actual, failure)).IsTrue(); }
    }

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
        using var fixture = new MapRuntimeFixture();
        var runtime = fixture.Runtime;
        var producer = fixture.Producer;
        var stream = fixture.Stream;
        var collision = fixture.Collision;
        var destination = fixture.Camera.Pose;
        var mount = fixture.Mount;
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
        fixture.Renderer.Failure = rendering; collision.Failure = physical;
        AssertThat(ReferenceEquals(runtime.Failure, original)).OverrideFailureMessage("Reading failure collected new producer faults.").IsTrue();
        runtime._Process(0);
        var collected = runtime.Failure as AggregateException ?? throw new InvalidOperationException("Producer failures were not collected by Update.");
        AssertThat(collected.Flatten().InnerExceptions.Count == 3 && collected.Flatten().InnerExceptions.Contains(original) &&
            collected.Flatten().InnerExceptions.Contains(rendering) && collected.Flatten().InnerExceptions.Contains(physical)).IsTrue();
        runtime._Process(0);
        AssertThat(ReferenceEquals(runtime.Failure, collected)).OverrideFailureMessage("Unchanged faults were collected twice.").IsTrue();
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
        var renderer = new RuntimeRenderer();
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

}

internal sealed partial class MapRuntimeReadinessProducer : Node, IViewContentReadiness
{
    public bool ContentReady { get; set; }
    public Pose? RequiredPose { get; set; }
    public bool IsReady(Pose pose) => ContentReady && (RequiredPose is null || pose == RequiredPose);
    public Exception? Failure { get; set; }
}
