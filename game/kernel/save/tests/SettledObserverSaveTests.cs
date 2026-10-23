using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Kernel.Save.Tests;

[TestSuite]
public sealed class SettledObserverSaveTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public async Task ForcedCaptureMetadataUsesTheSameSettledObserverAsTheViewAndWalkSections()
    {
        var walking = new SaveLifecycleFixture.FakeWalk();
        using var fixture = new SaveLifecycleFixture { Walk = walking };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        var target = new Pose(new(17, 23, 31), 44, -12);
        var settledWalk = walking.Current with { Feet = target with { Position = target.Position - Vector3.UnitY * WalkRules.StandingEye } };
        var snapshot = new ObserverSaveSnapshot(fixture.Maps.Current, new(ViewMode.Walk, target, null, 2, 2), settledWalk);
        fixture.Camera.Held = snapshot;
        fixture.Camera.SetPose(new(new(100, 200, 300), 0, 0));
        var result = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        AssertThat(result.Status == SaveStatus.Saved && fixture.Save.Slots[0].Meta?.Pose == target).IsTrue();
        var document = SaveContainer.Decode(File.ReadAllBytes(fixture.EntryFile(result.Gen)));
        var view = (ViewSaveData)fixture.Save.Sections.Prepare(document).Sections.Single(section => section.Dto is ViewSaveData).Dto!;
        var walk = (WalkSaveData)fixture.Save.Sections.Prepare(document).Sections.Single(section => section.Dto is WalkSaveData).Dto!;
        AssertThat(view == snapshot.View && walk.Feet == settledWalk.Feet
            && document.Meta.Pose == view.Pose && document.Meta.Mode == view.Mode && view.Pose == walk.EyePose()).IsTrue();
        AssertThat(fixture.Camera.Pose.Position == new Vector3(100, 200, 300)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task WalkingPreparationRepairsReachThisLoadBeforeItsSingleImmediateRestore()
    {
        var walking = new SaveLifecycleFixture.FakeWalk();
        using var fixture = new SaveLifecycleFixture { Walk = walking };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        fixture.Camera.Held = new(fixture.Maps.Current, new(ViewMode.Fly, fixture.Camera.Pose with { FovDeg = null }, null, 1, 1), walking.Current);
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        walking.Barrier = new();
        walking.Entered = new();
        int restores = walking.Restores;
        var loading = fixture.Save.Load(new("s1"));
        for (int frame = 0; frame < TestSamples.FrameLoop && !walking.Entered.Task.IsCompleted; frame++) await fixture.Frames(1);
        AssertThat(walking.Entered.Task.IsCompleted && !loading.IsCompleted && walking.Restores == restores).IsTrue();
        walking.Barrier.SetResult();
        var loaded = await fixture.Pump(loading);
        AssertThat(loaded.Status == LoadStatus.Loaded && loaded.Repairs.Count(repair => repair == "walk: unsafe history point removed") == 1
            && walking.Restores == restores + 1 && walking.Current.SafePoints.Count == 1).IsTrue();
        int repairs = loaded.Repairs.Count;
        await fixture.Frames(3);
        AssertThat(loaded.Repairs.Count == repairs && walking.Restores == restores + 1).IsTrue();
    }
}
