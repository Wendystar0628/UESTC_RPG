using Campus.Contracts;
using Campus.Kernel.Renderer;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class VeilWaitingTests
{
    [TestCase]
    public void LegacyCompatibilityDoesNotClaimObservationSupport()
    {
        IVeilService legacy = new LegacyVeil();
        AssertThat(!legacy.Waiting).OverrideFailureMessage("An old implementation must default to hidden.").IsTrue();
        try { legacy.ObserveReadinessWait(); }
        catch (NotSupportedException) { return; }
        throw new InvalidOperationException("An unimplemented production observation API silently reported support.");
    }

    [TestCase]
    public void CoverAndAuthoredHoldDoNotImplyReadinessWaiting()
    {
        var veil = new VeilTimeline();
        veil.Cover(VeilReason.Cinematic, 0).GetAwaiter().GetResult();
        veil.Advance(10);
        AssertThat(!veil.Waiting).OverrideFailureMessage("Authored opaque dwell became loading.").IsTrue();
        veil.HoldUntil(VeilReason.Teleport, () => false, 30);
        veil.Advance(10);
        AssertThat(!veil.Waiting && veil.Opacity == 1).OverrideFailureMessage("HoldUntil silently acquired a readiness observation.").IsTrue();
    }

    [TestCase]
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
        boot.Cover(VeilReason.Cinematic, 0).GetAwaiter().GetResult(); // Reason cannot reclassify this scope.
        boot.Advance(3.999);
        AssertThat(!boot.Waiting).OverrideFailureMessage("Frontispiece wait became visible before four seconds.").IsTrue();
        boot.Advance(.001);
        AssertThat(boot.Waiting).OverrideFailureMessage("A full four-second frontispiece wait remained hidden.").IsTrue();
    }

    [TestCase]
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
    public void ReadinessFailureAndCancellationStillLeaveTheWorldCovered()
    {
        var veil = new VeilTimeline();
        var scope = veil.ObserveReadinessWait();
        veil.HoldUntil(VeilReason.Cinematic, () => false, 2);
        var reveal = veil.Reveal(VeilReason.Cinematic, 1);
        try { veil.Advance(3); }
        finally { scope.Dispose(); }
        AssertThat(!veil.Waiting && veil.Opacity == 1 && reveal.IsFaulted).OverrideFailureMessage("Failure cleared protection or retained the wait indicator.").IsTrue();
        _ = reveal.Exception;
        var cancelled = veil.ObserveReadinessWait();
        try { throw new OperationCanceledException(); }
        catch (OperationCanceledException) { }
        finally { cancelled.Dispose(); }
        veil.Advance(10);
        AssertThat(!veil.Waiting).OverrideFailureMessage("Cancelled observation resurfaced after later frames.").IsTrue();
    }

    [TestCase]
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

    private sealed class LegacyVeil : IVeilService
    {
        public float Opacity => 1;
        public VeilReason Reason => VeilReason.Boot;
        public Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog) => Task.CompletedTask;
        public void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds) { }
        public Task Reveal(VeilReason reason, float seconds) => Task.CompletedTask;
    }

}
