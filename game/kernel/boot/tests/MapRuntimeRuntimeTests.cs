using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using System.Reflection;
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
        try
        {
            var producer = new ArrivalRenderer();
            stage.AddChild(producer);
            var stream = new ArrivalStream();
            var collision = new ArrivalCollision();
            var camera = new TestCameraRig();
            var destination = camera.Pose;
            void Set(string name, object value) => typeof(MapRuntime).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(runtime, value);
            Set("_mountPrepared", true);
            Set("_stream", stream);
            Set("_collision", collision);
            Set("_renderer", producer);
            Set("_camera", camera);
            Set("_terrain", new FlowTerrainPreparation());
            Set("_context", new TestMapContext(RealMapFixture.Entry().Descriptor));
            Set("_contentReadiness", ViewContentReadiness.Capture(stage));
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
            stream.Error = "destination failed";
            var failed = runtime.Arrive(destination, ViewMode.Fly);
            runtime._Process(0);
            AssertThat(failed.Done.IsFaulted).IsTrue();
            _ = failed.Done.Exception;
        }
        finally
        {
            runtime.StopNewWork();
            stage.Free();
            runtime.Free();
        }
    }

    private sealed class ArrivalStream : IMapStreamState
    {
        public bool IsUnloaded => false;
        public bool ResidentReady { get; set; }
        public bool DestinationReady { get; set; }
        public string? Error { get; set; }
        public MapStreamDestination? Destination { get; private set; }
        public bool HasPendingDestination => !DestinationReady;
        public IReadOnlyList<MapStreamDestination> PinnedDestinations { get; private set; } = [];
        public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations) => PinnedDestinations = destinations;
        public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) => Destination = new(destination, mode, space);
    }

    private sealed partial class ArrivalRenderer : Node, IRenderReadiness, IViewContentReadiness
    {
        public bool ContentReady { get; set; }
        public bool IsReady(Pose pose) => ContentReady;
        public string? Error => null;
        public bool Pending => false;
    }

    private sealed class ArrivalCollision : ICollisionWorld
    {
        public bool CollisionReady { get; set; }
        public string? Error => null;
        public bool Ready(N.Vector3 point, float radius) => CollisionReady;
        public SweepHit? Sweep(N.Vector3 from, N.Vector3 to, float radius, float height) => null;
        public SupportHit? SupportY(float x, float z, float yHint) => null;
        public SweepHit? SupportRay(N.Vector3 from, N.Vector3 to) => null;
        public int NearbyBoundaries(N.Vector3 feet, float radius, float height, Span<BoundaryHit> hits) => 0;
    }
}
