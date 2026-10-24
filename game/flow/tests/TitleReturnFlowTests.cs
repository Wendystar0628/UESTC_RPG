using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Flow;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class TitleReturnFlowTests
{
    [TestCase(false), TestCase(true), TestCategory(TestReason.Regression)]
    public async Task TitleObserversRunBeforeTheReturnOrRecoveryFadeCompletes(bool recoveringEntry)
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
        await Title(flow, rig);
        if (recoveringEntry) saves.InitialSaveStatus = SaveStatus.Failed;
        else if (await flow.Enter(new(EnterKind.NewGame, new("s1"))) != EnterResult.Entered)
            throw new InvalidOperationException("The fixture must enter play before returning to title.");
        var fade = new TaskCompletionSource(); rig.Loading.HideGate = fade.Task;
        var participantAdvanced = false; var titleObserved = false;
        flow.StateAdvancing += (_, next) => { if (next == AppState.Title) participantAdvanced = true; };
        flow.Changed += (_, next) =>
        {
            if (next == AppState.Title)
                titleObserved = participantAdvanced && rig.Title.Visible && rig.Loading.Active && rig.Camera.InputLocked;
        };
        Task operation = recoveringEntry
            ? flow.Enter(new(EnterKind.NewGame, new("s1")))
            : flow.ReturnToTitle();
        try
        {
            if (operation.IsCompleted || fade.Task.IsCompleted || !rig.Loading.Active || rig.Loading.Progress != 1 ||
                !rig.Title.Visible || rig.Runtime.Arrival is not null || flow.State != AppState.Title)
                throw new InvalidOperationException("The fixture must hold the completed title preparation under an unfinished loading fade.");
            if (!titleObserved || (flow.LastError is not null) != recoveringEntry)
                throw new InvalidOperationException("Title observers missed the prepared page or its recovery error before the fade completed.");
        }
        finally { fade.TrySetResult(); }
        await operation;
        if (rig.Loading.Active || flow.State != AppState.Title || !rig.Title.Visible || !rig.Camera.InputLocked)
            throw new InvalidOperationException("Finishing the title fade changed its page or input ownership.");
    }

    [TestCase(SaveStatus.Saved, null)]
    [TestCase(SaveStatus.Deferred, null)]
    [TestCase(SaveStatus.Failed, SaveErrorKeys.Io)]
    [TestCase(SaveStatus.Failed, SaveErrorKeys.Timeout)]
    [TestCategory(TestReason.Regression)]
    public async Task ReturningToTitleClearsTheWorldAfterAnySaveResult(SaveStatus status, string? errorKey)
    {
        var rig = new FlowTestRig(); var saves = new FlowSaveTestStub(rig);
        using var flow = rig.CreateFlow(saves, FlowTestRig.Maps(), new TestAppExit(false));
        await Title(flow, rig);
        if (await flow.Enter(new(EnterKind.NewGame, new("s1"))) != EnterResult.Entered) throw new InvalidOperationException("New game failed.");
        saves.FlushStatus = status; saves.FlushError = errorKey;
        var arrivals = rig.Runtime.Arrivals;
        await flow.ReturnToTitle();
        if (flow.State != AppState.Title || rig.Runtime.Arrival is not null || rig.Runtime.Arrivals != arrivals ||
            saves.Loads != 0 || saves.Flushes != 1 || rig.World.Instance != 0 || !rig.Title.Visible || rig.Loading.Active ||
            saves.State.Last != status || saves.State.ErrorKey != errorKey)
            throw new InvalidOperationException("Returning to title restored a world or waited for another arrival.");
        if (await flow.Enter(new(EnterKind.Continue)) != EnterResult.Entered || saves.Loads != 1 || saves.NewGames != 1 || saves.Saves != 1)
            throw new InvalidOperationException("Continue did not run a fresh complete load transaction.");
    }

    private static async Task Title(AppFlowService flow, FlowTestRig rig)
    {
        await flow.PrepareBoot(new());
        await rig.Title.Show(); await flow.OpenTitle();
        await flow.PrepareBackground(Task.CompletedTask, Task.CompletedTask);
    }
}
