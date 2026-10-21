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

    [TestCase(false)]
    [TestCase(true)]
    [TestCategory(TestReason.Regression)]
    public async Task AFailedTitleWriterKeepsTitleAndAllowsAnExplicitExitRetry(bool alreadyFailed)
    {
        var rig = new FlowTestRig(); var save = new TitleWriter(); var exit = new TestAppExit();
        using var flow = rig.CreateFlow(save, new TestMapService(), exit);
        await flow.PrepareBoot(new()); await flow.OpenTitle();
        if (alreadyFailed) save.Complete(SaveStatus.Failed);
        var failed = flow.Quit();
        if (!alreadyFailed) save.Complete(SaveStatus.Failed);
        await failed;
        if (exit.Calls != 0 || flow.State != AppState.Title || rig.World.Instance != 0 || !rig.Camera.InputLocked)
            throw new InvalidOperationException("A failed title writer resumed a nonexistent game world.");
        save.RetryStatus = SaveStatus.Deferred;
        var retry = flow.Quit(); await retry;
        if (ReferenceEquals(failed, retry) || save.Flushes != 2 || exit.Calls != 1 || flow.State != AppState.Quitting)
            throw new InvalidOperationException("The refused title exit could not be retried.");
    }

    private sealed class TitleWriter : TestSaveService
    {
        private readonly TaskCompletionSource<SaveResult> _writing = new();
        public int Flushes { get; private set; }
        internal SaveStatus? RetryStatus { get; set; }
        public override bool Busy => !_writing.Task.IsCompleted;
        public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
        { Flushes++; return RetryStatus is { } status ? Task.FromResult(new SaveResult(status, null, 0)) : _writing.Task; }
        internal void Complete(SaveStatus status) => _writing.SetResult(new(status, null, 0));
    }
}
