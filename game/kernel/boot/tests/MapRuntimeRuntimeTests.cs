using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
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
            Set("_materialSource", Provider);
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
