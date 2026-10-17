using Campus.Camera.Cinematic;
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
    public async Task FaultedWorldRetainsCoverAndAcceptsNewGameWithoutMapContext()
    {
        var f = new Fixture();
        await f.Flow.ReturnToTitle();
        var entering = f.Flow.Enter(new(EnterKind.NewGame, new("s1")));
        await CinematicTestFrames.Complete(entering, f.Playback, f.Presentation, f.Veil);
        var result = await entering;
        if (result != EnterResult.Failed || f.Save.NewGames != 1 || f.Flow.State != AppState.Title)
            throw new InvalidOperationException("Recovery was rejected before reaching the save transaction.");
        if (!f.Camera.InputLocked || f.Veil.Opacity != 1 || f.Veil.Reveals != 0)
            throw new InvalidOperationException("A failed recovery exposed or unlocked the partial world.");
        if (f.Diagnostics.Count != 1 || !f.Diagnostics[0].Contains("System.IO.InvalidDataException: save.error.restore_world", StringComparison.Ordinal) ||
            !f.Diagnostics[0].Contains("StartNewTimeline", StringComparison.Ordinal))
            throw new InvalidOperationException("The expected restore failure lost its original exception and stack: " + string.Join(Environment.NewLine, f.Diagnostics));
    }

    [TestCase]
    public async Task FaultedQuitDefersWritingAndRetainsInputLock()
    {
        var f = new Fixture();
        await f.Flow.ReturnToTitle();
        await f.Flow.Quit();
        if (f.Flow.State != AppState.Quitting || f.Save.Flushes != 1 || f.Save.Saves != 0 || !f.Camera.InputLocked || f.Veil.Reveals != 0 || f.Exit.Calls != 1)
            throw new InvalidOperationException("Faulted quit wrote or resumed the partial world.");
    }

    [TestCase]
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
    public async Task FailedAutomaticPreviewCannotFallBackAndRevealDuringBoot()
    {
        var f = new Fixture();
        await f.Flow.PrepareBoot(new());
        try { await f.Flow.PrepareWorld(); }
        catch (InvalidOperationException error) when (error.Message == "save.error.restore_world")
        {
            if (f.Flow.State != AppState.Boot || f.Veil.Opacity != 1 || f.Veil.Reveals != 0 || !f.Camera.InputLocked)
                throw new InvalidOperationException("Boot restore failure did not retain its recovery boundary.");
            return;
        }
        throw new InvalidOperationException("Faulted automatic preview silently fell back to a usable world.");
    }

    private sealed class Fixture
    {
        public TestCameraRig Camera { get; }
        public FaultedSave Save { get; } = new();
        public ImmediateVeil Veil { get; } = new();
        public ExitService Exit { get; } = new();
        public CinematicTestPresentation Presentation { get; } = new();
        public CinematicPlayback Playback { get; }
        public List<string> Diagnostics { get; } = [];
        public AppFlowService Flow { get; }
        public Fixture()
        {
            var rules = new Rules(); var world = new TestWorldState(new TestWeatherProcess(), rules);
            Camera = new(world);
            Playback = new(Camera, Veil, rules, Presentation);
            var runtime = new CinematicRuntimeTestStub(() => Presentation.IsCovered);
            Flow = new(Camera, Veil, Save, new UnavailableMapService(), world, world, rules,
                new Motion(), world, new TestCollisionWorld(), Playback, new Fault(), Exit, runtime, runtime)
            { DiagnosticSink = Diagnostics.Add };
        }
    }
    private sealed class ExitService : IAppExit
    {
        public int Calls { get; private set; }
        public TaskCompletionSource? Gate { get; set; }
        public TaskCompletionSource Started { get; } = new();
        public Task FinishQuit() { Calls++; Started.TrySetResult(); return Gate?.Task ?? Task.CompletedTask; }
    }
    private sealed class Fault : IWorldRestore
    {
        public bool RestoreFaulted { get; private set; } = true;
        public void AbortRestore() => RestoreFaulted = true;
        public void CompleteRestore() => RestoreFaulted = false;
    }
    private sealed class Rules : IRuleset
    {
        public RulesetDescriptor Current { get; } = new(new("test"), [ViewMode.Fly], WorldControlMask.All, "none", [], [AppState.Title, AppState.Entering, AppState.Leaving]);
        public long Rev => 1;
    }
    private sealed class Motion : IMotionSettings
    {
        public bool ReduceMotion => false;
        public event Action? Changed { add { } remove { } }
    }
    private sealed class ImmediateVeil : IVeilService
    {
        public float Opacity { get; private set; }
        public VeilReason Reason { get; private set; }
        public int Reveals { get; private set; }
        public Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog) { Reason = reason; Opacity = 1; return Task.CompletedTask; }
        public Task Reveal(VeilReason reason, float seconds) { Reason = reason; Opacity = 0; Reveals++; return Task.CompletedTask; }
        // Explicit test-only observation; this fixture does not simulate waiting-indicator timing.
        public IDisposable ObserveReadinessWait(VeilWaitKind kind = VeilWaitKind.Readiness) => new Observation();
        private sealed class Observation : IDisposable { public void Dispose() { } }
        public void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds) { }
    }
    private sealed class FaultedSave : ISaveService
    {
        public int NewGames { get; private set; }
        public int Flushes { get; private set; }
        public int Saves { get; private set; }
        public SaveStatus FlushStatus { get; set; } = SaveStatus.Deferred;
        public TaskCompletionSource<SaveResult>? FlushGate { get; set; }
        public bool Enabled => true;
        public IReadOnlyList<SlotInfo> Slots { get; } = [new(new("s1"), SlotKind.Auto, SlotHealth.Empty, null, 0)];
        public SlotId? Continue => new("s1");
        public SlotId? Active => null;
        public bool Busy => false;
        public SaveState State { get; } = new(null, SaveStatus.Failed, "save.error.restore_world", null, ReadOnlyReason.None);
        public event Action? SlotsChanged { add { } remove { } }
        public bool HasPendingPose => false;
        public void Request(SaveReason reason) { }
        public Task<SaveResult> SaveNow(SaveReason reason) { Saves++; return Task.FromResult(new SaveResult(SaveStatus.Failed, null, 0)); }
        public Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full) => Failed(slot);
        public Task CompleteResume() => throw new InvalidOperationException("Cannot resume a faulted world.");
        public Task<LoadResult> LoadFile(string path) => Failed(null);
        public Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null) { NewGames++; return Failed(slot); }
        public Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) { Flushes++; return FlushGate?.Task ?? Task.FromResult(new SaveResult(FlushStatus, null, 0)); }
        public byte[]? Thumbnail(SlotId slot) => null;
        private static Task<LoadResult> Failed(SlotId? slot) => Task.FromResult(new LoadResult(LoadStatus.Failed, slot, 0, [], "save.error.restore_world"));
    }
}
