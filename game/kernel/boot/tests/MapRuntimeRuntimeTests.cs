using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Inspect;
using Campus.Kernel.World;
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
    public void FlyCaptureBindsEachPreparedWorldAndRejectsThePreviousWorld()
    {
        var context = new TestMapContext(BackdropInputsTests.Descriptor());
        using var fixture = new MapRuntimeFixture(context: context);
        var world = new WorldStateService(new TestWeatherProcess());
        var rig = fixture.Camera;
        rig.Mode = ViewMode.Fly;
        fixture.Stream.ResidentReady = fixture.Stream.DestinationReady = fixture.Producer.ContentReady = true;
        fixture.Runtime._Process(0);
        GameCaptureNode.CaptureDestination? previous = null;
        for (var row = 0; row < 2; row++)
        {
            var before = world.Instance;
            var preset = context.Descriptor.World.Default;
            world.Restore(new(preset.Clock, 0, (int)preset.Season, preset.Season, 0, 1, WorldTime.InitialAnimTime),
                new() { Map = context.Descriptor.Id, FromVersion = 1, Missing = false });
            world.CompleteRestore();
            AssertThat(world.Instance != before).IsTrue();
            if (previous is not null)
            {
                // The old arrival remains current here: the world change alone must invalidate it.
                AssertThat(ReferenceEquals(fixture.Runtime.Arrival, previous.Arrival)).IsTrue();
                try
                {
                    previous.ReadCurrentPose(context, world.Instance, rig, fixture.Runtime, default);
                    throw new InvalidOperationException("The previous world's capture remained current.");
                }
                catch (OperationCanceledException) { }
            }
            var pose = rig.Pose with { Position = rig.Pose.Position + N.Vector3.UnitX };
            rig.SetPose(pose);
            var arrival = fixture.Runtime.Arrive(pose, rig.Mode);
            var capture = new GameCaptureNode.CaptureDestination(nameof(FlyCaptureBindsEachPreparedWorldAndRejectsThePreviousWorld),
                rig.Mode, context, world.Instance, arrival, pose);
            fixture.Runtime._Process(0);
            AssertThat(arrival.Done.IsCompletedSuccessfully).IsTrue();
            AssertThat(capture.ReadCurrentPose(context, world.Instance, rig, fixture.Runtime, default) == pose).IsTrue();
            previous = capture;
        }
    }

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
        catch (AppStartupException actual) { AssertThat(ReferenceEquals(actual.InnerException, failure)).IsTrue(); }
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
            catch (TimeoutException error) { AssertThat(error.InnerException is OperationCanceledException).IsTrue(); }
            try { await FrameAwaiter.StartupReadyDrawAsync(host, Ready, TimeSpan.FromMilliseconds(50)); throw new InvalidOperationException("Startup rendering completed instead of timing out."); }
            catch (AppStartupException error)
            {
                AssertThat(error.InnerException is TimeoutException
                    { InnerException: OperationCanceledException }).IsTrue();
            }
            try { await FrameAwaiter.StartupReadyDrawAsync(host, Ready, TimeSpan.FromSeconds(5), cancellation.Token); throw new InvalidOperationException("Startup rendering ignored cancellation."); }
            catch (OperationCanceledException) { }
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
        var published = runtime.Failure as AppStartupException ?? throw new InvalidOperationException("Map failure is not terminal.");
        AssertThat(ReferenceEquals(published.InnerException, original) &&
            ReferenceEquals(failed.Done.Exception!.InnerException, published)).IsTrue();
        var rendering = new InvalidDataException("rendering failed");
        var physical = new InvalidOperationException("collision failed");
        fixture.Renderer.Failure = rendering; collision.Failure = physical;
        AssertThat(ReferenceEquals(runtime.Failure, published)).OverrideFailureMessage("Reading failure collected new producer faults.").IsTrue();
        runtime._Process(0);
        var collected = runtime.Failure as AppStartupException ?? throw new InvalidOperationException("Producer failures are not terminal.");
        var causes = collected.InnerException as AggregateException ?? throw new InvalidOperationException("Producer failures were not collected by Update.");
        AssertThat(causes.InnerExceptions.Count == 3 &&
            causes.InnerExceptions.Contains(original) && causes.InnerExceptions.Contains(rendering) && causes.InnerExceptions.Contains(physical)).IsTrue();
        runtime._Process(0);
        AssertThat(ReferenceEquals(runtime.Failure, collected)).OverrideFailureMessage("Unchanged faults were collected twice.").IsTrue();
    }

}

internal sealed partial class MapRuntimeReadinessProducer : Node, IViewContentReadiness
{
    public bool ContentReady { get; set; }
    public Pose? RequiredPose { get; set; }
    public bool IsReady(Pose pose) => ContentReady && (RequiredPose is null || pose == RequiredPose);
    public Exception? Failure { get; set; }
}
