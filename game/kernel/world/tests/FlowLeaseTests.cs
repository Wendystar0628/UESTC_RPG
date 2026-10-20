using Campus.Tests;
using Campus.Contracts;
using Campus.Contracts.Save;
using GdUnit4;
using static GdUnit4.Assertions;
using static Campus.Tests.World.WorldTestKit;

namespace Campus.Tests.World;

[TestSuite]
public sealed class FlowLeaseTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FlowLeasesRestorePriorityAndAcquisitionOrderWithoutChangingTheUserRate()
    {
        var world = CreateWorld(new TestWeatherProcess());
        world.SetFlow(60);
        using var title = world.AcquireFlow(FlowPriority.Pause, 0);
        using var older = world.AcquireFlow(FlowPriority.AppFlow, 10);
        using var newer = world.AcquireFlow(FlowPriority.AppFlow, 360);
        using var laterPause = world.AcquireFlow(FlowPriority.Pause, 0);
        older.SetRate(1440);
        AssertThat(world.Flow.Rate).IsEqual(360);
        newer.Dispose();
        AssertThat(world.Flow.Rate).IsEqual(1440);
        older.Dispose();
        AssertThat(world.Flow.Rate).IsEqual(0);
        world.SetFlow(10);
        AssertThat(world.BaseFlow.Rate).IsEqual(10);
        AssertThat(world.LastFlowRate).IsEqual(10);
        title.Dispose(); // Releasing an older pause cannot release the later pause.
        AssertThat(world.Flow.Rate).IsEqual(0);
        laterPause.Dispose();
        laterPause.Dispose();
        AssertThat(world.Flow.Rate).IsEqual(10);
        world.SetFlow(0);
        AssertThat(world.LastFlowRate).IsEqual(10);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PauseLeaseSavesTheUserRateAndSurvivesLoadAndNewGame()
    {
        var world = CreateWorld(new TestWeatherProcess());
        world.SetFlow(60);
        using var pause = world.AcquireFlow(FlowPriority.Pause, 0);
        var start = world.WorldHours;
        world.Tick(.1);
        var saved = world.Capture(SaveContext(world));
        AssertThat(world.WorldHours).IsEqual(start);
        AssertThat(world.GetSaveMoment().FlowRate).IsEqual(60);
        AssertThat(saved.FlowRate).IsEqual(60);
        AssertThat(saved.FlowRateBeforePause).IsEqual(60);
        var loaded = saved with { WorldHours = 7, FlowRate = 10, FlowRateBeforePause = 10 };
        world.Restore(loaded, Restore());
        world.CompleteRestore();
        world.Tick(.1);
        AssertThat(world.WorldHours).IsEqual(loaded.WorldHours);
        AssertThat(world.Flow.Rate).IsEqual(0);
        AssertThat(world.Capture(SaveContext(world)).FlowRate).IsEqual(10);
        var fresh = world.CreateDefault(new NewGameContext(5, Restore().Map, new WorldPreset(start, Season.Autumn), ViewMode.Walk));
        world.Restore(fresh, Restore());
        world.CompleteRestore();
        AssertThat(world.Flow.Rate).IsEqual(0);
        AssertThat(world.BaseFlow.Rate).IsEqual(fresh.FlowRate);
        pause.Dispose();
        AssertThat(world.Flow.Rate).IsEqual(fresh.FlowRate);
    }

}
