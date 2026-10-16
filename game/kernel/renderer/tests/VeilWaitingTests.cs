using Campus.Contracts;
using Campus.Kernel.Renderer;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class VeilWaitingTests
{
    [TestCase]
    public void LegacyCompatibilityDoesNotClaimObservationSupport()
    {
        IVeilService legacy = new LegacyVeil();
        Check(!legacy.Waiting, "An old implementation must default to hidden.");
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
        Check(!veil.Waiting, "Authored opaque dwell became loading.");
        veil.HoldUntil(VeilReason.Teleport, () => false, 30);
        veil.Advance(10);
        Check(!veil.Waiting && veil.Opacity == 1, "HoldUntil silently acquired a readiness observation.");
    }

    [TestCase]
    public void OrdinaryAndFrontispieceThresholdsComeOnlyFromTheExplicitPolicy()
    {
        var ordinary = new VeilTimeline();
        using var wait = ordinary.ObserveReadinessWait(); // Default Reason is Boot, yet this is explicitly ordinary.
        ordinary.Advance(1.5);
        Check(!ordinary.Waiting, "Ordinary wait must exceed 1.5 seconds.");
        ordinary.Advance(.001);
        Check(ordinary.Waiting, "Ordinary wait never became visible.");
        var boot = new VeilTimeline();
        using var frontispiece = boot.ObserveReadinessWait(VeilWaitKind.BootFrontispiece);
        boot.Cover(VeilReason.Cinematic, 0).GetAwaiter().GetResult(); // Reason cannot reclassify this scope.
        boot.Advance(3.999);
        Check(!boot.Waiting, "Frontispiece wait became visible before four seconds.");
        boot.Advance(.001);
        Check(boot.Waiting, "A full four-second frontispiece wait remained hidden.");
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
        Check(veil.Waiting, "Old owner cleared another owner's live wait.");
        next.Dispose();
        Check(!veil.Waiting, "Disposed scope remained visible until the next frame.");
        veil.Advance(0);
        using var retry = veil.ObserveReadinessWait();
        old.Dispose(); veil.Advance(1);
        Check(!veil.Waiting, "Retry inherited an old scope's elapsed time.");
    }

    [TestCase]
    public void WorkerDisposalDoesNotPublishUiEventsAndFramePublishesTheChange()
    {
        var veil = new VeilTimeline();
        var scope = veil.ObserveReadinessWait();
        veil.Advance(2);
        var changes = 0; veil.Changed += () => changes++;
        OnDedicatedWorker(scope.Dispose);
        Check(!veil.Waiting && changes == 0, "Worker disposal published UI state or retained visibility.");
        veil.Advance(0);
        Check(changes == 1, "Owner frame did not publish disposal.");
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
        Check(!veil.Waiting && veil.Opacity == 1 && reveal.IsFaulted, "Failure cleared protection or retained the wait indicator.");
        _ = reveal.Exception;
        var cancelled = veil.ObserveReadinessWait();
        try { throw new OperationCanceledException(); }
        catch (OperationCanceledException) { }
        finally { cancelled.Dispose(); }
        veil.Advance(10);
        Check(!veil.Waiting, "Cancelled observation resurfaced after later frames.");
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
        Check(rejected == 2 && !veil.Waiting, "A worker mutated the observation collection or clock.");
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
        Check(workerThreadId != 0 && workerThreadId != ownerThreadId,
            $"Test did not cross a thread boundary: owner={ownerThreadId}, worker={workerThreadId}.");
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

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
