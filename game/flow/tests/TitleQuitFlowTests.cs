using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class TitleQuitFlowTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task TitleQuitWaitsForTheExistingWriterBeforeRequestingResourceShutdown()
    {
        var rig = new FlowTestRig(); var save = new TitleWriter(); var exit = new TestAppExit();
        using var flow = rig.CreateFlow(save, new TestMapService(), exit);
        await flow.PrepareBoot(new()); await flow.OpenTitle();
        var quit = flow.Quit();
        if (save.Flushes != 1 || exit.Calls != 0 || quit.IsCompleted || rig.World.Instance != 0)
            throw new InvalidOperationException("Title exit did not wait for its existing save writer.");
        save.Complete(SaveStatus.Deferred); await quit;
        if (exit.Calls != 1 || flow.State != AppState.Quitting || rig.World.Instance != 0)
            throw new InvalidOperationException("Title exit created a world or bypassed resource shutdown.");
    }

    [TestCase(false, SaveErrorKeys.Io)]
    [TestCase(true, SaveErrorKeys.Io)]
    [TestCase(false, SaveErrorKeys.Timeout)]
    [TestCase(true, SaveErrorKeys.Timeout)]
    [TestCategory(TestReason.Regression)]
    public async Task AFailedTitleWriterStillAwaitsResourceShutdown(bool alreadyFailed, string errorKey)
    {
        var rig = new FlowTestRig(); var save = new TitleWriter(); var exit = new TestAppExit { Gate = new() };
        using var flow = rig.CreateFlow(save, new TestMapService(), exit);
        await flow.PrepareBoot(new()); await flow.OpenTitle();
        if (alreadyFailed) save.Complete(SaveStatus.Failed, errorKey);
        var quit = flow.Quit();
        if (!alreadyFailed) save.Complete(SaveStatus.Failed, errorKey);
        await Task.WhenAny(quit, exit.Started.Task);
        if (exit.Calls != 1 || quit.IsCompleted || flow.State != AppState.Quitting || rig.World.Instance != 0 || !rig.Camera.InputLocked ||
            save.State.Last != SaveStatus.Failed || save.State.ErrorKey != errorKey)
            throw new InvalidOperationException("A failed title writer blocked exit or skipped the actual resource drain.");
        exit.Gate.SetResult(); await quit;
        if (!ReferenceEquals(quit, flow.Quit()) || save.Flushes != 1 || exit.Calls != 1)
            throw new InvalidOperationException("The completed exit repeated saving or resource disposal.");
    }

    private sealed class TitleWriter : TestSaveService
    {
        private readonly TaskCompletionSource<SaveResult> _writing = new();
        public int Flushes { get; private set; }
        private SaveState _state = new(null, null, null, null, ReadOnlyReason.None);
        public override SaveState State => _state;
        public override bool Busy => !_writing.Task.IsCompleted;
        public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
        { Flushes++; return _writing.Task; }
        internal void Complete(SaveStatus status, string? errorKey = null)
        { _state = _state with { Last = status, ErrorKey = errorKey }; _writing.SetResult(new(status, null, 0, errorKey)); }
    }
}
