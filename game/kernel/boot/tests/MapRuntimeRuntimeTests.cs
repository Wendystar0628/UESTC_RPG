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

public sealed partial class MaterialWarmupRuntimeTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task CaptureQueriesOutsideDrawSignalsAndWaitsForFreshDraw()
    {
        await using var fixture = new Fixture();
        var tree = fixture.Parent.GetTree();
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
            var waiting = FrameAwaiter.ReadyDrawAsync(fixture.Parent, Ready, TimeSpan.FromSeconds(5));
            await FrameAwaiter.ProcessAsync(fixture.Parent);
            AssertThat(!waiting.IsCompleted).OverrideFailureMessage("Unready content completed its presentation barrier.").IsTrue();
            ready = true;
            await waiting;
            AssertThat(readyAtDraw >= 0 && draws > readyAtDraw).OverrideFailureMessage("Ready content completed without a fresh draw.").IsTrue();
            RenderingServer.RenderLoopEnabled = false;
            using var cancellation = new CancellationTokenSource();
            var pending = FrameAwaiter.ReadyDrawAsync(fixture.Parent, Ready, TimeSpan.FromSeconds(5), cancellation.Token);
            await FrameAwaiter.ProcessAsync(fixture.Parent);
            AssertThat(!pending.IsCompleted).OverrideFailureMessage("An undrawn view became presented.").IsTrue();
            cancellation.Cancel();
            try { await pending; throw new InvalidOperationException("Disabled rendering did not remain cancellable."); }
            catch (OperationCanceledException) { }
            try { await FrameAwaiter.ReadyDrawAsync(fixture.Parent, Ready, TimeSpan.FromMilliseconds(50)); throw new InvalidOperationException("Disabled rendering completed instead of timing out."); }
            catch (TimeoutException) { }
        }
        finally
        {
            RenderingServer.RenderLoopEnabled = rendering;
            RenderingServer.Singleton.FramePreDraw -= BeforeDraw;
            RenderingServer.Singleton.FramePostDraw -= AfterDraw;
            tree.ProcessFrame -= BeforeProcess;
        }
    }
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task RevealedViewWarmsNewDemandWithoutArrivalAndSharesItsCoverWithExplicitArrival()
    {
        await using var fixture = new Fixture();
        var first = fixture.Mount("runtime-initial", fixture.Spec);
        var runtime = fixture.StartArrivalRuntime();
        var destination = new Pose(new N.Vector3(2f, 0f, -3f), 0f, 0f);
        var initial = runtime.Arrive(destination, ViewMode.Fly);
        await fixture.Until(() => initial.Done.IsCompleted);
        await initial.Done;
        await fixture.Boot.Reveal(0f);
        await fixture.Draw();
        var captures = fixture.Provider.Captures;
        for (var i = 0; i < 5; i++) runtime._Process(0);
        AssertThat(fixture.Provider.Captures == captures && fixture.Veil.Opacity == 0f).
            OverrideFailureMessage("The unchanged warm coordinator repeatedly captured pending batches or acquired a cover.").IsTrue();

        var accepted = fixture.Provider.Accepted;
        fixture.Cover.Visible = false;
        var lateSpec = fixture.Spec with { Line = LineClass.Fine };
        var late = fixture.Mount("runtime-late", lateSpec);
        runtime._Process(0);
        for (var i = 0; i < MaterialWarmup.StableFrames + 5; i++) await fixture.Draw();
        AssertThat(fixture.Provider.Accepted == accepted && fixture.Warmup.SamplesReleased && !late.Visible &&
            !fixture.Warmup.IsReady && ReferenceEquals(runtime.Arrival, initial)).
            OverrideFailureMessage("A revealed view must wait for real cover presentation before sampling new demand.").IsTrue();
        fixture.Cover.Visible = true;
        await fixture.Until(() => fixture.ReadyForReveal && late.Visible && fixture.Veil.Opacity == 0f);
        AssertThat(fixture.Provider.Accepted > accepted && ReferenceEquals(runtime.Arrival, initial)).
            OverrideFailureMessage("New current-view demand stalled after an explicit arrival ended.").IsTrue();

        accepted = fixture.Provider.Accepted;
        var layout = new WarmDrawLayout(MeshSurfaceFormat.Read((ArrayMesh)late.Multimesh.Mesh), false);
        fixture.Library.TrackLayout(fixture.Library.Get(lateSpec), layout);
        await fixture.Until(() => !fixture.Warmup.SamplesReleased);
        using var external = fixture.Veil.Cover(VeilReason.Teleport, 0f);
        await fixture.Until(() => fixture.ReadyForReveal && fixture.Provider.Accepted > accepted);
        // The automatic coordinator also waits for its final stable destination draw before releasing its own lease.
        for (var i = 0; i < Campus.Kernel.Boot.BootTuning.WarmFrames + 5; i++) await fixture.Draw();
        AssertThat(external.IsCurrent && fixture.Veil.Opacity == 1f && fixture.Warmup.SamplesReleased).
            OverrideFailureMessage("Automatic warming released the external cover or retained samples.").IsTrue();
        await external.Reveal(0f);
        AssertThat(fixture.Veil.Opacity == 0f).OverrideFailureMessage("Automatic warming leaked its own cover underneath an external lease.").IsTrue();

        fixture.Mount("runtime-handoff", fixture.Spec with { Line = LineClass.Tree });
        await fixture.Until(() => !fixture.Warmup.SamplesReleased);
        using var transit = fixture.Veil.Cover(VeilReason.Transit, 0f);
        var explicitArrival = runtime.Arrive(destination, ViewMode.Fly);
        await fixture.Until(() => explicitArrival.Done.IsCompleted);
        await explicitArrival.Done;
        AssertThat(transit.IsCurrent && fixture.Veil.Opacity == 1f && fixture.Warmup.SamplesReleased).
            OverrideFailureMessage("Explicit arrival and automatic warming interfered with cover ownership or sample cleanup.").IsTrue();
        await transit.Reveal(0f);
        AssertThat(fixture.Veil.Opacity == 0f).OverrideFailureMessage("Handing automatic warming to an arrival leaked its cover.").IsTrue();

        fixture.Library.TrackLayout(fixture.Library.Get(fixture.Spec), new(MeshSurfaceFormat.Read((ArrayMesh)first.Multimesh.Mesh), false));
        await fixture.Until(() => !fixture.Warmup.SamplesReleased);
        accepted = fixture.Provider.Accepted;
        runtime.StopNewWork();
        await fixture.Until(() => fixture.Warmup.SamplesReleased);
        for (var i = 0; i < MaterialWarmup.StableFrames + 5; i++) await fixture.Draw();
        AssertThat(fixture.Provider.Accepted == accepted && fixture.Veil.Opacity == 0f && !fixture.Warmup.IsReady).
            OverrideFailureMessage("Stopping automatic warming retained its cover or accepted later draw evidence.").IsTrue();
        GD.Print("MATERIAL_WARMUP_EVIDENCE revealedDemand=True uncoveredAccepted=0 idleSnapshots=0 externalCoverRetained=True arrivalHandoff=True stoppedReleased=True");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task VisibleArrivalReusesWarmDrawsAndReleasesOnlyItsOwnFallbackCover()
    {
        await using var fixture = new Fixture();
        fixture.Mount("arrival-initial", fixture.Spec);
        await fixture.Until(() => fixture.ReadyForReveal);
        await fixture.Boot.Reveal(0f);
        await fixture.Draw();
        var runtime = fixture.StartArrivalRuntime();
        var arrivals = new List<IMapArrival?>();
        runtime.ArrivalChanged += arrivals.Add;
        var destination = new Pose(new N.Vector3(2f, 0f, -3f), 0f, 0f);
        fixture.Stream.ResidentReady = false;
        fixture.Stream.DestinationReady = false;
        var visible = runtime.Arrive(destination, ViewMode.Fly);
        var progress = new List<int> { visible.Completed };
        visible.ProgressChanged += () => progress.Add(visible.Completed);
        var total = visible.Total;
        runtime._Process(0);
        AssertThat(!visible.Done.IsCompleted && visible.Completed > 0 && visible.Completed < total &&
            fixture.Veil.Opacity == 0f && ReferenceEquals(arrivals.Last(), visible)).IsTrue();
        await fixture.Draw();
        fixture.Stream.ResidentReady = true;
        runtime._Process(0);
        AssertThat(!visible.Done.IsCompleted).OverrideFailureMessage("A warm draw skipped destination residency.").IsTrue();
        fixture.Stream.DestinationReady = true;
        runtime._Process(0);
        AssertThat(!visible.Done.IsCompleted && visible.Completed < total).OverrideFailureMessage("Arrival completed before drawing the ready destination.").IsTrue();
        await fixture.Draw();
        await visible.Done;
        AssertThat(visible.Total == total && visible.Completed == total && progress.SequenceEqual(progress.Order())).
            OverrideFailureMessage("Real arrival progress changed its plan, regressed or failed to finish.").IsTrue();
        AssertThat(fixture.Veil.Opacity == 0f).OverrideFailureMessage("An already warm arrival unnecessarily covered the visible viewport.").IsTrue();
        var next = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        AssertThat(!next.Done.IsCompleted).OverrideFailureMessage("A new arrival inherited the previous arrival's draw.").IsTrue();
        var replaced = next;
        next = runtime.Arrive(destination, ViewMode.Fly);
        replaced.Cancel();
        runtime._Process(0);
        AssertThat(replaced.Done.IsCanceled && !next.Done.IsCompleted && next.Completed < next.Total &&
            ReferenceEquals(runtime.Arrival, next)).OverrideFailureMessage("Cancelling an older handle affected its replacement.").IsTrue();
        await fixture.Draw();
        await next.Done;

        var late = fixture.Mount("arrival-late", fixture.Spec with { Line = LineClass.Fine });
        var releaseContent = false;
        fixture.Renderer.ContentReady = () => late.Visible && releaseContent;
        var accepted = fixture.Provider.Accepted;
        var warming = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        AssertThat(!warming.Done.IsCompleted && !late.Visible && fixture.Veil.Opacity == 1f &&
            fixture.Provider.Accepted == accepted).OverrideFailureMessage("New materials must obtain coverage before producing draw evidence.").IsTrue();
        await fixture.Until(() => late.Visible && fixture.Warmup.SamplesReleased);
        AssertThat(!warming.Done.IsCompleted && warming.Completed < warming.Total).
            OverrideFailureMessage("Warming must unblock shader-dependent content without skipping final content readiness.").IsTrue();
        releaseContent = true;
        await fixture.Until(() => warming.Done.IsCompleted);
        await warming.Done;
        AssertThat(late.Visible && fixture.Warmup.SamplesReleased && fixture.Veil.Opacity == 0f &&
            fixture.Provider.Accepted > accepted).OverrideFailureMessage("The visible arrival did not finish its real warm batch and release its cover.").IsTrue();
        await fixture.Draw();

        fixture.Mount("arrival-loading", fixture.Spec with { Line = LineClass.Tree });
        fixture.Loading.Active = true;
        var loading = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        await fixture.Draw();
        AssertThat(!loading.Done.IsCompleted && fixture.Veil.Opacity == 0f).
            OverrideFailureMessage("An active loading page started a second veil or certified coverage before presentation.").IsTrue();
        fixture.Loading.IsCovered = true;
        await fixture.Until(() => loading.Done.IsCompleted);
        await loading.Done;
        AssertThat(loading.Completed == loading.Total && fixture.Veil.Opacity == 0f).
            OverrideFailureMessage("Presented application coverage did not finish real pipeline warming.").IsTrue();
        fixture.Loading.Active = fixture.Loading.IsCovered = false;
        await fixture.Draw();

        fixture.Mount("arrival-cancel", fixture.Spec with { Line = LineClass.None });
        var cancelled = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        using var external = fixture.Veil.Cover(VeilReason.Teleport, 0f);
        cancelled.Cancel();
        AssertThat(cancelled.Done.IsCanceled && external.IsCurrent && fixture.Veil.Opacity == 1f).IsTrue();
        await external.Reveal(0f);
        AssertThat(fixture.Veil.Opacity == 0f).OverrideFailureMessage("Cancelling left the arrival cover underneath the external lease.").IsTrue();
        await fixture.Draw();
        var failed = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        fixture.Stream.Error = "arrival preparation failed";
        runtime._Process(0);
        AssertThat(failed.Done.IsFaulted && fixture.Veil.Opacity == 0f).OverrideFailureMessage("Arrival failure retained its temporary cover.").IsTrue();
        _ = failed.Done.Exception;
        fixture.Stream.Error = null;
        await fixture.Draw();
        var stopped = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        runtime.StopNewWork();
        AssertThat(stopped.Done.IsCanceled && stopped.Completed < stopped.Total && arrivals.Last() is null &&
            fixture.Veil.Opacity == 0f).OverrideFailureMessage("Stopping retained the arrival's temporary cover or published success.").IsTrue();
    }

    private sealed partial class Fixture
    {
        private MapRuntime? _runtime;
        private Action? _arrivalDraw;
        public ArrivalStream Stream { get; } = new();
        public ArrivalLoading Loading { get; } = new();
        public ArrivalRenderer Renderer { get; private set; } = null!;

        public MapRuntime StartArrivalRuntime()
        {
            // Bind the real coordinator to this existing native-render fixture without changing the global registry.
            var runtime = _runtime = new MapRuntime();
            void Set(string name, object value) => typeof(MapRuntime).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(runtime, value);
            Set("_mountPrepared", true);
            Set("_stream", Stream);
            Parent.AddChild(Renderer = new ArrivalRenderer());
            Set("_renderer", Renderer);
            Set("_materials", Warmup);
            Set("_camera", new TestCameraRig());
            Set("_terrain", new FlowTerrainPreparation());
            Set("_context", new TestMapContext(RealMapFixture.Entry().Descriptor, new Campus.Gen.Tests.Support.FakeWorld()));
            Set("_veil", Veil);
            Set("_presentation", Cover);
            Set("_loading", Loading);
            Set("_contentReadiness", ViewContentReadiness.Capture(Parent));
            _arrivalDraw = typeof(MapRuntime).GetMethod("CountDraw", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action>(runtime);
            RenderingServer.Singleton.FramePostDraw += _arrivalDraw;
            return runtime;
        }
    }

    private sealed class ArrivalLoading : IAppLoadingPresentation
    {
        public bool Active { get; set; }
        public bool IsCovered { get; set; }
        public float Progress { get; private set; }
        public Task Show(Campus.Contracts.AppFlow.EnterRequest? request = null) { Active = true; return Task.CompletedTask; }
        public void Report(int completed, int total) => Progress = (float)completed / total;
        public Task Hide() { Active = IsCovered = false; return Task.CompletedTask; }
    }

    private sealed class ArrivalStream : IMapStreamState
    {
        public bool IsUnloaded => false;
        public bool ResidentReady { get; set; } = true;
        public bool DestinationReady { get; set; } = true;
        public string? Error { get; set; }
        public MapStreamDestination? Destination { get; private set; }
        public bool HasPendingDestination => !DestinationReady;
        public IReadOnlyList<MapStreamDestination> PinnedDestinations { get; private set; } = Array.Empty<MapStreamDestination>();
        public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations) =>
            PinnedDestinations = Array.AsReadOnly(destinations.ToArray());
        public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) => Destination = new(destination, mode, space);
    }

    private sealed partial class ArrivalRenderer : Node, IRenderReadiness, IViewContentReadiness
    {
        public Func<bool>? ContentReady { get; set; }
        public bool IsReady(Pose pose) => ContentReady?.Invoke() ?? true;
        public string? Error => null;
        public bool Pending => false;
        public bool PipelineReady => true;
    }
}
