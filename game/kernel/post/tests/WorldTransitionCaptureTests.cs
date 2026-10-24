using Campus.Kernel.Post;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

// WorldTransitionCapture contains only CLR Task state; these tests never initialize a Godot node.
[TestSuite]
public sealed class WorldTransitionCaptureTests
{
    [TestCase(false), TestCase(true), TestCategory(TestReason.Regression)]
    public async Task TransferredFailureIsReceivedByTheCallerAndNotReportedAgainAtExit(bool alreadyFailed)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var original = new InvalidDataException("capture failed");
        var capture = new WorldTransitionCapture { Work = completion.Task };
        using var stop = capture.Stop;
        List<WorldTransitionCapture> owned = [capture];
        if (alreadyFailed) completion.SetException(original);
        var caller = capture.TakeResult();
        if (!alreadyFailed) completion.SetException(original);
        Exception? received = null;
        try { await caller; }
        catch (InvalidDataException error) { received = error; }
        // Establish the original bug's trigger: failed work is retained after its caller receives the failure.
        AssertThat(caller.IsFaulted && ReferenceEquals(received, original) && owned.Contains(capture)).IsTrue();

        await WorldTransitionCapture.DrainAsync(owned);

        AssertThat(owned.Count == 0).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public async Task UnclaimedFailureStillReachesExitWithAllOriginalCauses()
    {
        var original = new InvalidDataException("capture failed");
        var cleanup = new IOException("release failed");
        var capture = new WorldTransitionCapture { Work = Task.FromException(new AggregateException(original, cleanup)) };
        using var stop = capture.Stop;
        List<WorldTransitionCapture> owned = [capture];
        AggregateException? received = null;

        try { await WorldTransitionCapture.DrainAsync(owned); }
        catch (AggregateException error) { received = error; }

        AssertThat(received is not null && received.Flatten().InnerExceptions.Contains(original) &&
            received.Flatten().InnerExceptions.Contains(cleanup) && owned.Count == 0).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task TransferringTheResultDoesNotLetDrainFinishBeforeCleanup()
    {
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var original = new InvalidDataException("capture failed before cleanup");
        async Task WorkIncludingCleanup()
        {
            try { throw original; }
            finally
            {
                cleanupEntered.SetResult();
                await cleanupReleased.Task;
            }
        }
        var capture = new WorldTransitionCapture { Work = WorkIncludingCleanup() };
        using var stop = capture.Stop;
        List<WorldTransitionCapture> owned = [capture];
        var caller = capture.TakeResult();
        await cleanupEntered.Task;
        var draining = WorldTransitionCapture.DrainAsync(owned);
        try
        {
            AssertThat(!caller.IsCompleted && !draining.IsCompleted).IsTrue();
        }
        finally { cleanupReleased.TrySetResult(); }
        Exception? received = null;
        try { await caller; }
        catch (InvalidDataException error) { received = error; }

        await draining;

        AssertThat(ReferenceEquals(received, original) && capture.Work.IsCompleted && owned.Count == 0).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public async Task UnclaimedUnexpectedCancellationStillReachesExit()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var capture = new WorldTransitionCapture { Work = Task.FromCanceled(cancellation.Token) };
        using var stop = capture.Stop;
        List<WorldTransitionCapture> owned = [capture];
        AggregateException? received = null;

        try { await WorldTransitionCapture.DrainAsync(owned); }
        catch (AggregateException error) { received = error; }

        AssertThat(received is not null && received.Flatten().InnerExceptions.Any(error =>
            error is OperationCanceledException canceled && canceled.CancellationToken == cancellation.Token)).IsTrue();
    }
}
