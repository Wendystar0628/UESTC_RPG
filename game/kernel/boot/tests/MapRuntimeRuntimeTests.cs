using Campus.Contracts;
using Campus.Contracts.Maps;
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
        var destination = new Pose(new N.Vector3(2f, 0f, -3f), 0f, 0f);
        fixture.Stream.ResidentReady = false;
        fixture.Stream.DestinationReady = false;
        var visible = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        AssertThat(!visible.Done.IsCompleted && fixture.Veil.Opacity == 0f).IsTrue();
        await fixture.Draw();
        fixture.Stream.ResidentReady = true;
        runtime._Process(0);
        AssertThat(!visible.Done.IsCompleted).OverrideFailureMessage("A warm draw skipped destination residency.").IsTrue();
        fixture.Stream.DestinationReady = true;
        runtime._Process(0);
        await visible.Done;
        AssertThat(fixture.Veil.Opacity == 0f).OverrideFailureMessage("An already warm arrival unnecessarily covered the visible viewport.").IsTrue();
        var next = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        AssertThat(!next.Done.IsCompleted).OverrideFailureMessage("A new arrival inherited the previous arrival's draw.").IsTrue();
        await fixture.Draw();
        await next.Done;

        var late = fixture.Mount("arrival-late", fixture.Spec with { Line = LineClass.Fine });
        var accepted = fixture.Provider.Accepted;
        var warming = runtime.Arrive(destination, ViewMode.Fly);
        runtime._Process(0);
        AssertThat(!warming.Done.IsCompleted && !late.Visible && fixture.Veil.Opacity == 1f &&
            fixture.Provider.Accepted == accepted).OverrideFailureMessage("New materials must obtain coverage before producing draw evidence.").IsTrue();
        await fixture.Until(() => warming.Done.IsCompleted);
        await warming.Done;
        AssertThat(late.Visible && fixture.Warmup.SamplesReleased && fixture.Veil.Opacity == 0f &&
            fixture.Provider.Accepted > accepted).OverrideFailureMessage("The visible arrival did not finish its real warm batch and release its cover.").IsTrue();
        await fixture.Draw();

        fixture.Mount("arrival-cancel", fixture.Spec with { Line = LineClass.Tree });
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
        AssertThat(stopped.Done.IsCanceled && fixture.Veil.Opacity == 0f).OverrideFailureMessage("Stopping retained the arrival's temporary cover.").IsTrue();
    }

    private sealed partial class Fixture
    {
        private MapRuntime? _runtime;
        private Action? _arrivalDraw;
        public ArrivalStream Stream { get; } = new();

        public MapRuntime StartArrivalRuntime()
        {
            // Bind the real coordinator to this existing native-render fixture without changing the global registry.
            var runtime = _runtime = new MapRuntime();
            void Set(string name, object value) => typeof(MapRuntime).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(runtime, value);
            Set("_mountPrepared", true);
            Set("_stream", Stream);
            Set("_renderer", new ArrivalRenderer());
            Set("_materials", Warmup);
            Set("_materialSource", Provider);
            Set("_camera", new TestCameraRig());
            Set("_veil", Veil);
            Set("_presentation", Cover);
            Set("_contentReadiness", ViewContentReadiness.Capture(Parent));
            _arrivalDraw = typeof(MapRuntime).GetMethod("CountDraw", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action>(runtime);
            RenderingServer.Singleton.FramePostDraw += _arrivalDraw;
            return runtime;
        }
    }

    private sealed class ArrivalStream : IMapStreamState
    {
        public bool IsUnloaded => false;
        public bool ResidentReady { get; set; } = true;
        public bool DestinationReady { get; set; } = true;
        public string? Error { get; set; }
        public MapStreamDestination? Destination { get; private set; }
        public bool HasPendingDestination => !DestinationReady;
        public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) => Destination = new(destination, mode, space);
    }

    private sealed class ArrivalRenderer : IRenderReadiness
    {
        public string? Error => null;
        public bool Pending => false;
        public bool PipelineReady => true;
    }
}
