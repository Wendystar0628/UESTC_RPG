using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Flow;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class RestoreFailureFlowTests
{
    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task FaultedWorldRetainsCoverAndAcceptsNewGameWithoutMapContext() =>
        await FaultedRecoveryCase(bootFailed: false);

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task FailedBootStillAllowsTheFaultedWorldNewGameRecoveryTransaction() =>
        await FaultedRecoveryCase(bootFailed: true);

    private static async Task FaultedRecoveryCase(bool bootFailed)
    {
        var f = new Fixture();
        if (bootFailed) f.Flow.FailBoot(new InvalidOperationException("boot.failed"));
        await f.Flow.ReturnToTitle();
        var entering = f.Flow.Enter(new(EnterKind.NewGame, new("s1")));
        var result = await entering;
        if (result != EnterResult.Failed || f.Save.NewGames != 1 || f.Flow.State != AppState.Title)
            throw new InvalidOperationException("Recovery was rejected before reaching the save transaction.");
        if (!f.Camera.InputLocked || !f.Loading.Active || f.Loading.Hides != 0)
            throw new InvalidOperationException("A failed recovery exposed or unlocked the partial world.");
        if (f.Diagnostics.Count != 1 || !f.Diagnostics[0].Contains("System.IO.InvalidDataException: save.error.restore_world", StringComparison.Ordinal) ||
            !f.Diagnostics[0].Contains("StartNewTimeline", StringComparison.Ordinal))
            throw new InvalidOperationException("The expected restore failure lost its original exception and stack: " + string.Join(Environment.NewLine, f.Diagnostics));
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task FaultedQuitDefersWritingAndRetainsInputLock()
    {
        var f = new Fixture();
        await f.Flow.ReturnToTitle();
        await f.Flow.Quit();
        if (f.Flow.State != AppState.Quitting || f.Save.Flushes != 1 || f.Save.Saves != 0 || !f.Camera.InputLocked || f.Loading.Hides != 0 || f.Exit.Calls != 1)
            throw new InvalidOperationException("Faulted quit wrote or resumed the partial world.");
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task QuitRequestsShareOneTaskAcrossFlushReentryAndResourceShutdown()
    {
        var f = new Fixture();
        f.Save.FlushGate = new(); f.Exit.Gate = new();
        Task? reentrant = null;
        f.Flow.Changed += (_, next) => { if (next is AppState.Quitting) reentrant = f.Flow.Quit(); };
        var first = f.Flow.Quit();
        if (!ReferenceEquals(first, reentrant) || !ReferenceEquals(first, f.Flow.Quit()) || f.Save.Flushes != 1 || f.Exit.Calls != 0)
            throw new InvalidOperationException("Quitting state announced completion or duplicated the flush.");
        f.Save.FlushGate.SetResult(new(SaveStatus.Deferred, null, 0));
        await f.Exit.Started.Task;
        if (f.Exit.Calls != 1 || first.IsCompleted || !ReferenceEquals(first, f.Flow.Quit()))
            throw new InvalidOperationException("Shutdown was not awaited by the shared quit task.");
        f.Exit.Gate.SetResult(); await first;
        if (!ReferenceEquals(first, f.Flow.Quit()) || f.Save.Flushes != 1 || f.Exit.Calls != 1)
            throw new InvalidOperationException("A completed quit repeated final resource disposal.");
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task FailedFlushNeverCallsExitAndACompletedFailureCanBeRetried()
    {
        var f = new Fixture(); f.Save.FlushStatus = SaveStatus.Failed;
        var failed = f.Flow.Quit(); await failed;
        if (f.Exit.Calls != 0 || f.Flow.State != AppState.Title || !f.Camera.InputLocked)
            throw new InvalidOperationException("A failed flush invoked shutdown or released the faulty world.");
        f.Save.FlushStatus = SaveStatus.Deferred;
        var retry = f.Flow.Quit(); await retry;
        if (ReferenceEquals(failed, retry) || f.Save.Flushes != 2 || f.Exit.Calls != 1)
            throw new InvalidOperationException("A refused quit could not start a distinct retry.");
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task FailedAutomaticPreviewCannotFallBackAndRevealDuringBoot()
    {
        var f = new Fixture();
        await f.Flow.PrepareBoot(new());
        try { await f.Flow.PrepareWorld(); }
        catch (InvalidOperationException)
        {
            if (f.Flow.State != AppState.Boot || !f.Loading.Active || f.Loading.Hides != 0 || !f.Camera.InputLocked)
                throw new InvalidOperationException("Boot restore failure did not retain its recovery boundary.");
            return;
        }
        throw new InvalidOperationException("Faulted automatic preview silently fell back to a usable world.");
    }

    private sealed class Fixture
    {
        private readonly FlowTestRig _rig = new();
        public TestCameraRig Camera => _rig.Camera;
        public FaultedSave Save { get; } = new();
        public TestAppLoadingPresentation Loading => _rig.Loading;
        public TestAppExit Exit { get; } = new();
        public List<string> Diagnostics => _rig.Diagnostics;
        public AppFlowService Flow { get; }
        public Fixture() => Flow = _rig.CreateFlow(Save, new TestMapService(), Exit, new Fault());
    }
    private sealed class Fault : IWorldRestore
    {
        public WorldSaveMoment GetSaveMoment() => throw new InvalidOperationException("Restore fault fixture cannot capture a world.");
        public bool RestoreFaulted { get; private set; } = true;
        public void AbortRestore() => RestoreFaulted = true;
        public void CompleteRestore() => RestoreFaulted = false;
    }
    private sealed class FaultedSave : TestSaveService
    {
        public int NewGames { get; private set; }
        public int Flushes { get; private set; }
        public int Saves { get; private set; }
        public SaveStatus FlushStatus { get; set; } = SaveStatus.Deferred;
        public TaskCompletionSource<SaveResult>? FlushGate { get; set; }
        public override IReadOnlyList<SlotInfo> Slots { get; } = [new(new("s1"), SlotKind.Auto, SlotHealth.Empty, null, 0)];
        public override SlotId? Continue => new("s1");
        public override SaveState State { get; } = new(null, SaveStatus.Failed, "save.error.restore_world", null, ReadOnlyReason.None);
        public override Task<SaveResult> SaveNow(SaveReason reason) { Saves++; return Task.FromResult(new SaveResult(SaveStatus.Failed, null, 0)); }
        public override Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full) => Failed(slot);
        public override Task CompleteResume() => throw new InvalidOperationException("Cannot resume a faulted world.");
        public override Task<LoadResult> LoadFile(string path) => Failed(null);
        public override Task<LoadResult> RestoreTitlePreview() => Failed(null);
        public override Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null) { NewGames++; return Failed(slot); }
        public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) { Flushes++; return FlushGate?.Task ?? Task.FromResult(new SaveResult(FlushStatus, null, 0)); }
        private static Task<LoadResult> Failed(SlotId? slot) => Task.FromResult(new LoadResult(LoadStatus.Failed, slot, 0, [], "save.error.restore_world"));
    }
}
