using Campus.Contracts;
using Campus.Kernel.Frame;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class FrameStatisticsTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public void MainViewportCostsNeedCurrentDrawRequestAndDoNotClaimExactProvenance()
    {
        var viewport = ((SceneTree)Engine.GetMainLoop()).Root.GetViewportRid();
        using (var measured = new MeasuredViewports(viewport))
        {
            using var registration = measured.Register(viewport, "main");
            measured.ReportDraw(viewport, 1, true);
            measured.Sample(1);
            var sample = measured.MainSample;
            AssertThat(sample.CpuRenderMilliseconds == RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewport)).IsTrue();
            AssertThat(sample.GpuRenderMilliseconds == RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewport)).IsTrue();
            AssertThat(sample.SourceDrawFrame.HasValue || sample.ResultUpdated.HasValue).IsFalse();
            AssertThat(sample.DrewThisFrame.HasValue).IsFalse();

            measured.Sample(2);
            AssertThat(measured.MainSample.CpuRenderMilliseconds.HasValue || measured.MainSample.GpuRenderMilliseconds.HasValue).IsFalse();
            measured.ReportDraw(viewport, 3, false);
            measured.Sample(3);
            AssertThat(measured.MainSample.DrewThisFrame is false).IsTrue();
            AssertThat(measured.MainSample.CpuRenderMilliseconds.HasValue || measured.MainSample.GpuRenderMilliseconds.HasValue).IsFalse();
        }

        // A role label alone cannot turn an on-demand RID into the owner's main viewport.
        using var secondary = new MeasuredViewports(default);
        using var secondaryRegistration = secondary.Register(viewport, "main");
        secondary.ReportDraw(viewport, 1, true);
        secondary.Sample(1);
        var unconfirmed = secondary.Samples[0];
        AssertThat(unconfirmed.CpuRenderMilliseconds.HasValue || unconfirmed.GpuRenderMilliseconds.HasValue).IsFalse();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ZeroPhysicsAndMultiplePhysicsStepsPartitionTheRawInterval()
    {
        var phases = new FramePhaseAccumulator(true);
        phases.Begin(100, 1000);
        AssertThat(phases.FrameMilliseconds.HasValue).IsFalse();
        phases.ProcessEnd(102);
        phases.PreDraw(105, 1010);
        phases.PostDraw(109, 1020);
        phases.Begin(120, 1040);
        AssertThat(phases.Phases.Validity).IsEqual(FramePhaseValidity.All);
        AssertThat(phases.Phases.PhysicsSteps).IsEqual(0);
        AssertThat(phases.Phases.PhysicsMilliseconds == 0).IsTrue();
        AssertThat(Sum(phases.Phases) == phases.FrameMilliseconds).IsTrue();
        AssertThat(phases.MainCycles == 40 && phases.DrawCycles == 10).IsTrue();

        phases.ProcessEnd(123);
        phases.PreDraw(126, 1050);
        phases.PostDraw(130, 1060);
        phases.Physics(132);
        phases.Physics(135);
        phases.Begin(140, 1080);
        AssertThat(phases.Phases.PhysicsSteps).IsEqual(2);
        AssertThat(phases.Phases.PhysicsMilliseconds == 8).IsTrue();
        AssertThat(Sum(phases.Phases) == phases.FrameMilliseconds).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MissingDrawDoesNotReusePreviousBoundariesAndUnsafeModelIsUnavailable()
    {
        var phases = new FramePhaseAccumulator(true);
        phases.Begin(0, null);
        phases.ProcessEnd(1); phases.PreDraw(2, null); phases.PostDraw(3, null);
        phases.Begin(4, null);
        AssertThat(phases.Phases.Validity).IsEqual(FramePhaseValidity.All);
        phases.ProcessEnd(5);
        phases.Begin(8, null);
        AssertThat(phases.Phases.Validity).IsEqual(FramePhaseValidity.None);
        AssertThat(Sum(phases.Phases).HasValue).IsFalse();
        AssertThat(phases.MainCycles.HasValue || phases.DrawCycles.HasValue).IsFalse();
        phases.ProcessEnd(9); phases.PostDraw(10, null); phases.PreDraw(11, null);
        phases.Begin(12, null);
        AssertThat(phases.Phases.Validity).IsEqual(FramePhaseValidity.None);

        var separate = new FramePhaseAccumulator(false);
        separate.Begin(0, null);
        separate.ProcessEnd(1); separate.PreDraw(2, null); separate.PostDraw(3, null);
        separate.Begin(4, null);
        AssertThat(separate.Phases.Validity).IsEqual(FramePhaseValidity.None);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DisplayWindowRetainsLongIntervalsAndSeparatelyCountsAvailableMeasurements()
    {
        var accumulator = new FrameDisplayAccumulator();
        var first = new FrameSample();
        AssertThat(accumulator.Add(in first, out _)).IsFalse();
        var duration = FrameDisplayAccumulator.WindowMilliseconds / 2;
        var measured = new FrameSample
        {
            FrameMilliseconds = duration,
            MainViewport = new() { CpuRenderMilliseconds = 2 }
        };
        AssertThat(accumulator.Add(in measured, out _)).IsFalse();
        var stalled = new FrameSample
        {
            FrameMilliseconds = FrameDisplayAccumulator.WindowMilliseconds * 2,
            MainViewport = new() { GpuRenderMilliseconds = 7 }
        };
        AssertThat(accumulator.Add(in stalled, out var window)).IsTrue();
        var total = duration + stalled.FrameMilliseconds!.Value;
        AssertThat(window.FrameCount).IsEqual(2);
        AssertThat(window.IntervalMilliseconds).IsEqual(total);
        AssertThat(window.SlowestFrameMilliseconds).IsEqual(stalled.FrameMilliseconds.Value);
        AssertThat(window.FramesPerSecond).IsEqual(2000 / total);
        AssertThat(window.AverageFrameMilliseconds).IsEqual(total / 2);
        AssertThat(window.MainViewportCpuMilliseconds == 2 && window.MainViewportGpuMilliseconds == 7).IsTrue();

        var unavailable = new FrameSample { FrameMilliseconds = FrameDisplayAccumulator.WindowMilliseconds };
        AssertThat(accumulator.Add(in unavailable, out var empty)).IsTrue();
        AssertThat(empty.Sequence).IsEqual(window.Sequence + 1);
        AssertThat(empty.MainViewportCpuMilliseconds.HasValue || empty.MainViewportGpuMilliseconds.HasValue).IsFalse();
    }

    private static double? Sum(FramePhases phases) => phases.PhysicsMilliseconds + phases.ProcessMilliseconds +
        phases.LateMilliseconds + phases.DrawMilliseconds + phases.TailMilliseconds;
}
