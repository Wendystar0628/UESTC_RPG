using Campus.Contracts;
using Campus.Kernel.Renderer;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class VeilWaitingTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CoverAndOpaqueDwellDoNotImplyReadinessWaiting()
    {
        var veil = new VeilTimeline();
        using var cover = veil.Cover(VeilReason.Transit, 0);
        cover.Covered.GetAwaiter().GetResult();
        veil.Advance(10);
        AssertThat(!veil.Waiting && veil.Opacity == 1).OverrideFailureMessage("Opaque dwell became loading.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OrdinaryAndFrontispieceThresholdsComeOnlyFromTheExplicitPolicy()
    {
        var ordinary = new VeilTimeline();
        using var wait = ordinary.ObserveReadinessWait(); // Default Reason is Boot, yet this is explicitly ordinary.
        ordinary.Advance(1.5);
        AssertThat(!ordinary.Waiting).OverrideFailureMessage("Ordinary wait must exceed 1.5 seconds.").IsTrue();
        ordinary.Advance(.001);
        AssertThat(ordinary.Waiting).OverrideFailureMessage("Ordinary wait never became visible.").IsTrue();
        var boot = new VeilTimeline();
        using var frontispiece = boot.ObserveReadinessWait(VeilWaitKind.BootFrontispiece);
        using var cover = boot.Cover(VeilReason.Transit, 0); // Reason cannot reclassify this scope.
        cover.Covered.GetAwaiter().GetResult();
        boot.Advance(3.999);
        AssertThat(!boot.Waiting).OverrideFailureMessage("Frontispiece wait became visible before four seconds.").IsTrue();
        boot.Advance(.001);
        AssertThat(boot.Waiting).OverrideFailureMessage("A full four-second frontispiece wait remained hidden.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OwnersAggregateAndLateDisposalCannotClearAnotherScope()
    {
        var veil = new VeilTimeline();
        var old = veil.ObserveReadinessWait();
        veil.Advance(2);
        var next = veil.ObserveReadinessWait();
        veil.Advance(2);
        old.Dispose(); old.Dispose();
        AssertThat(veil.Waiting).OverrideFailureMessage("Old owner cleared another owner's live wait.").IsTrue();
        next.Dispose();
        AssertThat(!veil.Waiting).OverrideFailureMessage("Disposed scope remained visible until the next frame.").IsTrue();
        veil.Advance(0);
        using var retry = veil.ObserveReadinessWait();
        old.Dispose(); veil.Advance(1);
        AssertThat(!veil.Waiting).OverrideFailureMessage("Retry inherited an old scope's elapsed time.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WorkerDisposalDoesNotPublishUiEventsAndFramePublishesTheChange()
    {
        var veil = new VeilTimeline();
        var scope = veil.ObserveReadinessWait();
        veil.Advance(2);
        var changes = 0; veil.Changed += () => changes++;
        OnDedicatedWorker(scope.Dispose);
        AssertThat(!veil.Waiting && changes == 0).OverrideFailureMessage("Worker disposal published UI state or retained visibility.").IsTrue();
        veil.Advance(0);
        AssertThat(changes == 1).OverrideFailureMessage("Owner frame did not publish disposal.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DisposingReadinessObservationsLeavesTheirOwnersCoverInPlace()
    {
        var veil = new VeilTimeline();
        var scope = veil.ObserveReadinessWait();
        using var cover = veil.Cover(VeilReason.Transit, 0);
        scope.Dispose();
        AssertThat(!veil.Waiting && veil.Opacity == 1 && cover.IsCurrent).OverrideFailureMessage("Observation disposal cleared protection or retained the wait indicator.").IsTrue();
        var cancelled = veil.ObserveReadinessWait();
        cancelled.Dispose();
        veil.Advance(10);
        AssertThat(!veil.Waiting).OverrideFailureMessage("Cancelled observation resurfaced after later frames.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BeginningAndAdvancingFromAWorkerAreRejected()
    {
        var veil = new VeilTimeline();
        var rejected = 0;
        OnDedicatedWorker(() =>
        {
            try { veil.ObserveReadinessWait(); } catch (InvalidOperationException) { rejected++; }
            try { veil.Advance(1); } catch (InvalidOperationException) { rejected++; }
        });
        AssertThat(rejected == 2 && !veil.Waiting).OverrideFailureMessage("A worker mutated the observation collection or clock.").IsTrue();
    }

    private static void OnDedicatedWorker(Action action)
    {
        var ownerThreadId = Environment.CurrentManagedThreadId;
        var workerThreadId = 0;
        Exception? failure = null;
        // A synchronously awaited Task.Run can inline on the owner's pool thread; Thread cannot.
        var worker = new Thread(() =>
        {
            workerThreadId = Environment.CurrentManagedThreadId;
            try { action(); }
            catch (Exception error) { failure = error; }
        })
        { IsBackground = true };
        worker.Start();
        if (!worker.Join(TimeSpan.FromSeconds(5)))
            throw new TimeoutException($"Dedicated test worker did not finish: owner={ownerThreadId}, worker={workerThreadId}.");
        AssertThat(workerThreadId != 0 && workerThreadId != ownerThreadId).OverrideFailureMessage($"Test did not cross a thread boundary: owner={ownerThreadId}, worker={workerThreadId}.").IsTrue();
        Console.WriteLine($"VeilWaitingTests threads: owner={ownerThreadId}, worker={workerThreadId}.");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

}
