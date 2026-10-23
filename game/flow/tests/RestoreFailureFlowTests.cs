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
        var bootFailure = new InvalidOperationException("boot.failed");
        if (bootFailed) f.Flow.FailBoot(bootFailure);
        await f.Flow.ReturnToTitle();
        var entering = f.Flow.Enter(new(EnterKind.NewGame, new("s1")));
        var result = await entering;
        if (result != EnterResult.Failed || f.Save.NewGames != 1 || f.Flow.State != AppState.Title)
            throw new InvalidOperationException("Recovery was rejected before reaching the save transaction.");
        if (!f.Camera.InputLocked || f.Loading.Active)
            throw new InvalidOperationException("A failed recovery exposed or unlocked the partial world.");
        if (f.Flow.Failure is not InvalidDataException || f.Flow.LastError != SaveErrorKeys.RestoreWorld)
            throw new InvalidOperationException("The flow session must retain its original exception type while keeping the existing restore-error display.");
        if (f.Diagnostics.Count != 1)
            throw new InvalidOperationException("The expected restore failure was not logged exactly once: " + string.Join(Environment.NewLine, f.Diagnostics));
        f.Flow.Dispose();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task FaultedQuitDefersWritingAndRetainsInputLock()
    {
        var f = new Fixture();
        await f.Flow.ReturnToTitle();
        await f.Flow.Quit();
        if (f.Flow.State != AppState.Quitting || f.Save.Flushes != 1 || f.Save.Saves != 0 || !f.Camera.InputLocked || f.Loading.Active || f.Exit.Calls != 1)
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

    [TestCase(SaveErrorKeys.Io)]
    [TestCase(SaveErrorKeys.Timeout)]
    [TestCategory(TestReason.Regression)]
    public async Task FaultedWorldFlushFailureStillAwaitsResourceShutdown(string errorKey)
    {
        var f = new Fixture(); f.Save.FlushStatus = SaveStatus.Failed; f.Save.FlushError = errorKey; f.Exit.Gate = new();
        var quit = f.Flow.Quit(); await Task.WhenAny(quit, f.Exit.Started.Task);
        if (f.Exit.Calls != 1 || quit.IsCompleted || f.Flow.State != AppState.Quitting || !f.Camera.InputLocked || f.Save.Saves != 0)
            throw new InvalidOperationException("A failed faulted-world flush blocked exit or bypassed resource shutdown.");
        f.Exit.Gate.SetResult(); await quit;
        if (!ReferenceEquals(quit, f.Flow.Quit()) || f.Save.Flushes != 1 || f.Exit.Calls != 1)
            throw new InvalidOperationException("The faulted-world exit repeated saving or resource disposal.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ThrownSaveFailureRetainsTheOriginalExceptionAndStillCloses()
    {
        var f = new Fixture(); var error = new IOException("Private disk failure details.");
        f.Save.FlushFailure = error; f.Exit.Gate = new();
        var quit = f.Flow.Quit(); await Task.WhenAny(quit, f.Exit.Started.Task);
        if (quit.IsCompleted || f.Flow.State != AppState.Quitting || !ReferenceEquals(f.Flow.Failure, error) ||
            f.Flow.LastError != SaveErrorKeys.RestoreWorld || f.Diagnostics.Count != 1)
            throw new InvalidOperationException("Thrown save failure blocked exit or exposed exception text.");
        f.Exit.Gate.SetResult(); await quit;
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
        public Fixture()
        {
            Flow = _rig.CreateFlow(Save, new TestMapService(), Exit, new Fault());
            Flow.PrepareBoot(new()).GetAwaiter().GetResult();
        }
    }
    private sealed class Fault : IWorldRestore
    {
        public WorldSaveMoment GetSaveMoment() => throw new InvalidOperationException("Restore fault fixture cannot capture a world.");
        public bool RestoreFaulted { get; private set; } = true;
        public void AbortRestore() => RestoreFaulted = true;
        public void CompleteRestore() => RestoreFaulted = false;
        public void EndInstance() { }
    }
    private sealed class FaultedSave : TestSaveService
    {
        public int NewGames { get; private set; }
        public int Flushes { get; private set; }
        public int Saves { get; private set; }
        public SaveStatus FlushStatus { get; set; } = SaveStatus.Deferred;
        public string? FlushError { get; set; }
        public Exception? FlushFailure { get; set; }
        public TaskCompletionSource<SaveResult>? FlushGate { get; set; }
        public override IReadOnlyList<SlotInfo> Slots { get; } = [new(new("s1"), SlotKind.Auto, SlotHealth.Empty, null, 0)];
        public override SlotId? Continue => new("s1");
        public override SaveState State { get; } = new(null, SaveStatus.Failed, SaveErrorKeys.RestoreWorld, null, ReadOnlyReason.None);
        public override Task<SaveResult> SaveNow(SaveReason reason) { Saves++; return Task.FromResult(new SaveResult(SaveStatus.Failed, null, 0)); }
        public override Task<LoadResult> Load(SlotId slot) => Failed(slot);
        public override Task<LoadResult> LoadFile(string path) => Failed(null);
        public override Task<LoadResult> NewGame(SlotId? slot, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null) { NewGames++; return Failed(slot); }
        public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
        { Flushes++; return FlushFailure is { } error ? Task.FromException<SaveResult>(error) : FlushGate?.Task ?? Task.FromResult(new SaveResult(FlushStatus, null, 0, FlushError)); }
        private static Task<LoadResult> Failed(SlotId? slot) => Task.FromResult(new LoadResult(LoadStatus.Failed, slot, 0, [], SaveErrorKeys.RestoreWorld));
    }
}
