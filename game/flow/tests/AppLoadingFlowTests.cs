using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Flow;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class AppLoadingFlowTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public async Task StaticTitleIsInteractiveBeforeBackgroundAndNeverLoadsAWorld()
    {
        var rig = new FlowTestRig(); var saves = new FullSave(rig); var maps = Maps();
        using var flow = rig.CreateFlow(saves, maps, new TestAppExit(false));
        var map = new TaskCompletionSource(); var global = new TaskCompletionSource();
        await flow.PrepareBoot(new());
        var background = flow.PrepareBackground(map.Task, global.Task);
        await rig.Title.Show(); await flow.OpenTitle();
        if (flow.State != AppState.Title || flow.MapReady.IsCompleted || flow.BackgroundReady.IsCompleted ||
            saves.Loads != 0 || saves.NewGames != 0 || rig.Runtime.Arrivals != 0 || !rig.Title.Visible)
            throw new InvalidOperationException("Static title waited for or initialized its background world.");
        map.SetResult(); await flow.MapReady;
        if (rig.Runtime.PinnedDestinations.Count != 2 || flow.BackgroundReady.IsCompleted || flow.NewGamePreloadReady)
            throw new InvalidOperationException("Map readiness did not immediately pin both title endpoints.");
        rig.Runtime.PinsReady = true;
        if (!flow.NewGamePreloadReady) throw new InvalidOperationException("Preload readiness did not query held stream resources.");
        rig.Runtime.SetPinnedDestinations([]);
        if (flow.NewGamePreloadReady) throw new InvalidOperationException("Released pins retained a copied ready flag.");
        global.SetResult(); await background;
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task EarlyNewGameJoinsTheSameBackgroundThenKeepsItsPinUntilArrival()
    {
        var rig = new FlowTestRig(false); var saves = new FullSave(rig); var maps = Maps();
        using var flow = rig.CreateFlow(saves, maps, new TestAppExit(false));
        var map = new TaskCompletionSource(); var global = new TaskCompletionSource();
        await flow.PrepareBoot(new()); var background = flow.PrepareBackground(map.Task, global.Task);
        await rig.Title.Show(); await flow.OpenTitle();
        var entering = flow.Enter(new(EnterKind.NewGame, new("s1")));
        if (entering.IsCompleted || saves.NewGames != 0 || !rig.Loading.Active || rig.Title.Visible)
            throw new InvalidOperationException("Early entry did not cover and join background preparation.");
        map.SetResult(); await flow.MapReady;
        if (saves.NewGames != 0 || rig.Runtime.PinnedDestinations.Count != 2)
            throw new InvalidOperationException("Entry bypassed global preparation or lost its pins.");
        global.SetResult(); await background; await saves.ArrivalStarted.Task;
        if (entering.IsCompleted || saves.Saves != 0 || rig.Runtime.PinnedDestinations.Count == 0)
            throw new InvalidOperationException("Entry released residency or saved before its real arrival.");
        rig.Runtime.CompleteArrival();
        if (await entering != EnterResult.Entered || rig.Runtime.Arrivals != 1 || saves.NewGames != 1 || saves.Saves != 1 ||
            flow.State != AppState.Playing || rig.Loading.Active || rig.Camera.InputLocked || rig.Runtime.PinnedDestinations.Count != 0)
            throw new InvalidOperationException("Entry duplicated its arrival or skipped first-save/input handoff.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task BackgroundFailureProjectsBeforeItsSiblingAndRetainsBothCauses()
    {
        var rig = new FlowTestRig(); var saves = new FullSave(rig);
        using var flow = rig.CreateFlow(saves, Maps(), new TestAppExit(false));
        await flow.PrepareBoot(new());
        var map = new TaskCompletionSource(); var global = new TaskCompletionSource();
        var work = flow.PrepareBackground(map.Task, global.Task);
        var first = new InvalidDataException("map failed"); var second = new IOException("warmup failed");
        map.SetException(first);
        try { await flow.BackgroundReady; throw new InvalidOperationException("Failure was not projected immediately."); }
        catch (InvalidDataException actual) when (ReferenceEquals(actual, first)) { }
        if (work.IsCompleted) throw new InvalidOperationException("Background lifetime abandoned its running sibling.");
        global.SetException(second);
        try { await work; throw new InvalidOperationException("Both failures were lost."); }
        catch (AggregateException error)
        {
            var causes = error.Flatten().InnerExceptions;
            if (!causes.Contains(first) || !causes.Contains(second)) throw;
        }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task BackgroundCancellationWaitsForItsSiblingAndRemainsCanceled()
    {
        var rig = new FlowTestRig(); var saves = new FullSave(rig);
        using var flow = rig.CreateFlow(saves, Maps(), new TestAppExit(false));
        await flow.PrepareBoot(new());
        var map = new TaskCompletionSource(); var global = new TaskCompletionSource();
        var work = flow.PrepareBackground(map.Task, global.Task);
        map.SetCanceled();
        try { await flow.MapReady; throw new InvalidOperationException("Map cancellation became success."); }
        catch (OperationCanceledException) when (flow.MapReady.IsCanceled) { }
        if (work.IsCompleted) throw new InvalidOperationException("Canceled background abandoned its running sibling.");
        global.SetResult();
        try { await work; throw new InvalidOperationException("Background cancellation became success."); }
        catch (OperationCanceledException) when (work.IsCanceled) { }
        if (!flow.BackgroundReady.IsCanceled || rig.Runtime.PinnedDestinations.Count != 0)
            throw new InvalidOperationException("Background cancellation faulted readiness or retained title pins.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task BackgroundCancellationCannotHideItsSiblingsRealFailure()
    {
        var rig = new FlowTestRig(); var saves = new FullSave(rig);
        using var flow = rig.CreateFlow(saves, Maps(), new TestAppExit(false));
        await flow.PrepareBoot(new());
        var map = new TaskCompletionSource(); var global = new TaskCompletionSource();
        var work = flow.PrepareBackground(map.Task, global.Task);
        map.SetCanceled();
        var failure = new InvalidDataException("Global preparation failed during exit.");
        global.SetException(failure);
        try { await work; throw new InvalidOperationException("Background cancellation hid its sibling failure."); }
        catch (AggregateException error) when (error.Flatten().InnerExceptions.Contains(failure)) { }
        if (!work.IsFaulted) throw new InvalidOperationException("A real background fault became cancellation.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task QuitDuringEarlyEntryCancelsItsBackgroundWaitBeforeCreatingAWorld()
    {
        var rig = new FlowTestRig(); var saves = new FullSave(rig);
        var exit = new TestAppExit();
        using var flow = rig.CreateFlow(saves, Maps(), exit);
        await flow.PrepareBoot(new());
        var map = new TaskCompletionSource(); var global = new TaskCompletionSource();
        var work = flow.PrepareBackground(map.Task, global.Task);
        await rig.Title.Show(); await flow.OpenTitle();
        var entering = flow.Enter(new(EnterKind.NewGame, new("s1")));
        await flow.Quit();
        if (await entering != EnterResult.Rejected || saves.NewGames != 0 || flow.State != AppState.Quitting || exit.Calls != 1 || work.IsCompleted)
            throw new InvalidOperationException("Early quit created a world or abandoned its background task owner.");
        map.SetCanceled(); global.SetResult();
        try { await work; throw new InvalidOperationException("Canceled background completed successfully."); }
        catch (OperationCanceledException) when (work.IsCanceled) { }
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    public async Task ReturningToTitleClearsTheWorldWithoutLoadingAnotherEndpoint()
    {
        var rig = new FlowTestRig(); var saves = new FullSave(rig);
        using var flow = rig.CreateFlow(saves, Maps(), new TestAppExit(false));
        await Title(flow, rig);
        if (await flow.Enter(new(EnterKind.NewGame, new("s1"))) != EnterResult.Entered) throw new InvalidOperationException("New game failed.");
        var arrivals = rig.Runtime.Arrivals;
        await flow.ReturnToTitle();
        if (flow.State != AppState.Title || rig.Runtime.Arrival is not null || rig.Runtime.Arrivals != arrivals ||
            saves.Loads != 0 || saves.Flushes != 1 || rig.World.Instance != 0 || !rig.Title.Visible || rig.Loading.Active)
            throw new InvalidOperationException("Returning to title restored a world or waited for another arrival.");
        if (await flow.Enter(new(EnterKind.Continue)) != EnterResult.Entered || saves.Loads != 1 || saves.NewGames != 1 || saves.Saves != 1)
            throw new InvalidOperationException("Continue did not run a fresh complete load transaction.");
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    public async Task DirectFreshEntryWithoutASlotDoesNotAuthorizeAnySave()
    {
        var rig = new FlowTestRig(); var saves = new FullSave(rig);
        using var flow = rig.CreateFlow(saves, Maps(), new TestAppExit(false));
        await flow.PrepareBoot(new(ShowTitle: false));
        await flow.PrepareBackground(Task.CompletedTask, Task.CompletedTask);
        var arrival = await flow.EnterDirect();
        if (flow.State != AppState.Playing || saves.NewGames != 1 || saves.Active is not null || saves.Saves != 0 ||
            !ReferenceEquals(arrival, rig.Runtime.Arrival) || rig.Runtime.Arrivals != 1 || rig.Title.Visible)
            throw new InvalidOperationException("Direct entry activated a save slot or constructed a title world.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task ReturnAndQuitWaitForTransitCleanupBeforeSavingOrClosing()
    {
        var rig = new FlowTestRig(); var saves = new FullSave(rig); var exit = new TestAppExit();
        using var flow = rig.CreateFlow(saves, Maps(), exit);
        await Title(flow, rig); await flow.Enter(new(EnterKind.NewGame, new("s1")));
        var transit = new TaskCompletionSource(); rig.Transit.Draining = transit.Task;
        var leaving = flow.ReturnToTitle();
        if (leaving.IsCompleted || saves.Flushes != 0)
            throw new InvalidOperationException("Return captured the world before transit cleanup.");
        transit.SetResult(); await leaving;
        transit = new(); rig.Transit.Draining = transit.Task;
        var quitting = flow.Quit();
        if (quitting.IsCompleted || exit.Calls != 0)
            throw new InvalidOperationException("Quit released resources before transit cleanup.");
        transit.SetResult(); await quitting;
        if (exit.Calls != 1) throw new InvalidOperationException("Drained title quit did not finish.");
    }

    private static async Task Title(AppFlowService flow, FlowTestRig rig)
    {
        await flow.PrepareBoot(new());
        await rig.Title.Show(); await flow.OpenTitle();
        await flow.PrepareBackground(Task.CompletedTask, Task.CompletedTask);
    }
    private static TestMapService Maps()
    {
        var source = RealMapFixture.Entry(); var descriptor = source.Descriptor;
        return new()
        {
            Default = descriptor.Id, Current = descriptor.Id, Maps = [descriptor],
            Context = new TestMapContext(descriptor, new FakeWorld { Boundary = new([]) }),
            SpawnResolver = (_, spawn) => source.ResolveSpawn(spawn ?? descriptor.DefaultSpawn)
        };
    }
    private sealed class FullSave(FlowTestRig rig) : TestSaveService
    {
        private SlotId? _active;
        public int NewGames { get; private set; }
        public int Loads { get; private set; }
        public int Saves { get; private set; }
        public int Flushes { get; private set; }
        public TaskCompletionSource ArrivalStarted { get; } = new();
        private static readonly Pose Landing = new(new(120, 1.7f, -70), 20, 0, UiSettingDefinitions.WalkFov.Default);
        private SaveMeta Meta => new(1, "test", new("s1"), SlotKind.Auto, 1, 1, 0, Maps().Default,
            17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Walk, Landing, null, null, null, 0, SaveReason.Test);
        public override IReadOnlyList<SlotInfo> Slots => [new(new("s1"), SlotKind.Auto, SlotHealth.Ok, Meta, 1)];
        public override SlotId? Continue => new("s1");
        public override SlotId? Active => _active;
        public override Task<SaveResult> SaveNow(SaveReason reason)
        {
            if (rig.Runtime.Arrival?.Done.IsCompletedSuccessfully != true) throw new InvalidOperationException("Save preceded arrival.");
            Saves++; return Task.FromResult(new SaveResult(SaveStatus.Saved, Active, 1));
        }
        public override Task<LoadResult> NewGame(SlotId? slot, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null)
        { NewGames++; return Restore(slot); }
        public override Task<LoadResult> Load(SlotId slot) { Loads++; return Restore(slot); }
        private async Task<LoadResult> Restore(SlotId? slot)
        {
            _active = slot; rig.Camera.Mode = ViewMode.Walk; rig.Camera.SetPose(Landing);
            var arrival = rig.Runtime.Arrive(Landing, ViewMode.Walk);
            ArrivalStarted.TrySetResult(); await arrival.Done;
            return new(LoadStatus.Loaded, slot, 1, []);
        }
        public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
        { Flushes++; return Task.FromResult(new SaveResult(SaveStatus.Saved, Active, 1)); }
    }
}
