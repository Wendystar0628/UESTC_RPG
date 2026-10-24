using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Flow;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class AppLoadingFlowTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public async Task ThrowingFlowObserversPreserveEntryLoadingAndLaterObservers()
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
        var original = new IOException("milestone observation failed");
        List<string> phases = [];
        var playingObservedAfterInput = false;
        flow.PhaseReached += _ => throw original;
        flow.PhaseReached += phases.Add;
        flow.Changed += (_, _) => throw original;
        flow.Changed += (_, next) => { if (next == AppState.Playing) playingObservedAfterInput = !rig.Camera.InputLocked; };
        await Title(flow, rig);
        if (!phases.Contains("title-ready") || flow.State != AppState.Title)
            throw new InvalidOperationException("Title observation blocked later observers or title readiness.");
        var result = await flow.Enter(new(EnterKind.NewGame, new("s1")));
        if (result != EnterResult.Entered || flow.State != AppState.Playing || flow.Failure is not null ||
            !phases.Contains("new-game-enter") || !phases.Contains("loading-complete") ||
            rig.Loading.Active || rig.Camera.InputLocked || saves.Saves != 1 || !playingObservedAfterInput)
            throw new InvalidOperationException("Milestone observation changed entry or loading completion.");
        Exception? retained = null;
        try { flow.Dispose(); } catch (Exception error) { retained = error; }
        if (retained is not AggregateException aggregate || !aggregate.Flatten().InnerExceptions.Contains(original))
            throw new InvalidOperationException("Flow disposal did not receive the original milestone exception.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task StaticTitleIsInteractiveBeforeBackgroundAndNeverLoadsAWorld()
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig); var maps = FlowTestRig.Maps();
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
        var rig = new FlowTestRig(false); var saves = new FlowSaveTestStub(rig); var maps = FlowTestRig.Maps();
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
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
        await flow.PrepareBoot(new());
        var map = new TaskCompletionSource(); var global = new TaskCompletionSource();
        var work = flow.PrepareBackground(map.Task, global.Task);
        await rig.Title.Show(); await flow.OpenTitle();
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
        // StageAppFailure receives preparation failures; disposal only observes both projections.
        flow.Dispose();
        if (rig.Camera.InputLocked || !flow.MapReady.IsFaulted || !flow.BackgroundReady.IsFaulted)
            throw new InvalidOperationException("Terminal readiness observation skipped cleanup or changed failure to cancellation.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void DisposalObservesBootFailureBeforeAnyBackgroundPreparationExists()
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        foreach (var failure in new Exception[] { new InvalidDataException("BeforeMap failed"), new OperationCanceledException() })
        {
            var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
            flow.FailBoot(failure);
            // StageAppFailure has already received this failure before publishing FailBoot.
            flow.Dispose();
            if (!flow.MapReady.IsFaulted || !flow.BackgroundReady.IsFaulted ||
                !ReferenceEquals(flow.MapReady.Exception?.InnerException, failure) ||
                !ReferenceEquals(flow.BackgroundReady.Exception?.InnerException, failure))
                throw new InvalidOperationException("Closing replaced the original readiness failure.");
        }
        var canceled = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
        canceled.Dispose();
        if (!canceled.MapReady.IsCanceled || !canceled.BackgroundReady.IsCanceled)
            throw new InvalidOperationException("Disposal left readiness pending without a background owner.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task BackgroundCancellationWaitsForItsSiblingAndRemainsCanceled()
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
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
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
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
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        var exit = new TestAppExit();
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), exit);
        List<EnterCommandEvent> commands = []; flow.EnterCommand += commands.Add;
        await flow.PrepareBoot(new());
        var map = new TaskCompletionSource(); var global = new TaskCompletionSource();
        var work = flow.PrepareBackground(map.Task, global.Task);
        await rig.Title.Show(); await flow.OpenTitle();
        var entering = flow.Enter(new(EnterKind.NewGame, new("s1")));
        if (commands.Count != 1 || commands[0].Phase != EnterCommandPhase.Started || entering.IsCompleted)
            throw new InvalidOperationException("The fixture must start entry while its background wait is still active.");
        await flow.Quit();
        if (await entering != EnterResult.Rejected || saves.NewGames != 0 || flow.State != AppState.Quitting || exit.Calls != 1 || work.IsCompleted)
            throw new InvalidOperationException("Early quit created a world or abandoned its background task owner.");
        if (commands.Count != 2 || commands[1] != (commands[0] with { Phase = EnterCommandPhase.Completed, Result = EnterResult.Rejected }))
            throw new InvalidOperationException("An admitted entry canceled by quit must complete once with its real Rejected result.");
        map.SetCanceled(); global.SetResult();
        try { await work; throw new InvalidOperationException("Canceled background completed successfully."); }
        catch (OperationCanceledException) when (work.IsCanceled) { }
    }

    [TestCase(SaveStatus.Saved, true)]
    [TestCase(SaveStatus.Disabled, true)]
    [TestCase(SaveStatus.Deferred, false)]
    [TestCase(SaveStatus.Failed, false)]
    [TestCategory(TestReason.Regression)]
    public async Task NewGameRequiresItsInitialSaveOrExplicitReadOnlyMode(SaveStatus status, bool accepted)
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig) { InitialSaveStatus = status };
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
        await Title(flow, rig);
        var entered = await flow.Enter(new(EnterKind.NewGame, new("s1")));
        if (saves.Saves != 1 || entered != (accepted ? EnterResult.Entered : EnterResult.Failed) ||
            flow.State != (accepted ? AppState.Playing : AppState.Title) || rig.Loading.Active || rig.Camera.InputLocked == accepted)
            throw new InvalidOperationException("New game treated an unfinished or failed first save as playable.");
        if (!accepted && (flow.Failure is not IOException || flow.LastError != "title.enter_failed" || rig.Diagnostics.Count != 1))
            throw new InvalidOperationException("Initial-save failure did not keep its exception and localized flow error.");
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    public async Task DirectFreshEntryWithoutASlotDoesNotAuthorizeAnySave()
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
        List<EnterCommandEvent> commands = []; flow.EnterCommand += commands.Add;
        await flow.PrepareBoot(new(ShowTitle: false));
        await flow.PrepareBackground(Task.CompletedTask, Task.CompletedTask);
        var arrival = await flow.EnterDirect();
        if (flow.State != AppState.Playing || saves.NewGames != 1 || saves.Active is not null || saves.Saves != 0 ||
            !ReferenceEquals(arrival, rig.Runtime.Arrival) || rig.Runtime.Arrivals != 1 || rig.Title.Visible)
            throw new InvalidOperationException("Direct entry activated a save slot or constructed a title world.");
        var target = new EnterRequest(EnterKind.NewGame);
        if (commands.Count != 2 || commands[0] != new EnterCommandEvent(1, EnterCommandPhase.Started, target, null, true, null) ||
            commands[1] != (commands[0] with { Phase = EnterCommandPhase.Completed, Result = EnterResult.Entered }))
            throw new InvalidOperationException("Direct entry fabricated a slot or skipped its shared command boundary.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task ReturnAndQuitWaitForTransitCleanupBeforeSavingOrClosing()
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig); var exit = new TestAppExit();
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), exit);
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
}
