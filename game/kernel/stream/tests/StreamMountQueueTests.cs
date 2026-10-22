using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Frame;
using Campus.Kernel.Stream;
using GdUnit4;
using static Campus.Tests.StreamLifecycleTestSupport;

namespace Campus.Tests;

[TestSuite]
public sealed class StreamMountQueueTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RetirementBlocksVisualsButPreservesCollisionAndResumesAfterDrain()
    {
        foreach (var covered in new[] { false, true })
        {
            var retiring = true; var collisionSteps = 0; var visualSteps = 0;
            var queue = new StreamMountQueue(() => 0);
            queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision, _ => visualSteps++));
            queue.Enqueue(new MountWork(StreamMountKind.Collision, queue.Revision, _ => collisionSteps++));
            queue.Drain(covered ? CoveredScreen : VisibleWorld, release: budget =>
            {
                if (!covered && budget > KernelBudgets.StreamMountMilliseconds - KernelBudgets.CollisionMountMilliseconds)
                    throw new InvalidOperationException("Retirement took the reserved collision budget.");
            }, canActivateVisual: () => !retiring);
            if (collisionSteps != 1 || visualSteps != 0 || queue.Pending != 1)
                throw new InvalidOperationException("Retirement did not apply visual-only backpressure.");
            retiring = false; queue.Drain(covered ? CoveredScreen : VisibleWorld, canActivateVisual: () => !retiring);
            if (visualSteps != 1 || queue.Pending != 0) throw new InvalidOperationException("Visual activation did not resume after retirement.");
        }
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FailedMountCancelsAndLeavesNoQueuedWork()
    {
        var original = new InvalidOperationException("fixture failure");
        var queue = new StreamMountQueue(() => 0);
        var failed = new MountWork(StreamMountKind.Visual, queue.Revision, _ => throw original);
        queue.Enqueue(failed); queue.Drain(VisibleWorld);
        if (failed.Cancels != 1 || queue.Pending != 0 || !ReferenceEquals(queue.Failure, original))
            throw new InvalidOperationException("Failed mount was not cancelled and removed.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SharedMountBudgetPutsOneCollisionBodyBeforeOneVisualTile()
    {
        var clock = 0d; var order = new List<StreamMountKind>();
        var queue = new StreamMountQueue(() => clock);
        var visual = new MountWork(StreamMountKind.Visual, queue.Revision, budget => { order.Add(StreamMountKind.Visual); clock += .6; });
        var collision = new MountWork(StreamMountKind.Collision, queue.Revision, budget =>
        {
            if (budget > .3) throw new InvalidOperationException("Collision budget exceeds 0.3 ms.");
            order.Add(StreamMountKind.Collision); clock += .2;
        });
        queue.Enqueue(visual); queue.Enqueue(collision);
        queue.Enqueue(new MountWork(StreamMountKind.Collision, queue.Revision, _ => throw new InvalidOperationException("Mounted two collision bodies."), "z-last"));
        queue.Drain(VisibleWorld);
        if (!order.SequenceEqual([StreamMountKind.Collision, StreamMountKind.Visual]) || queue.Pending != 1)
            throw new InvalidOperationException("Shared frame ordering or limits failed.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MapRemovalRejectsLateWorkAndCancelsItOnce()
    {
        var queue = new StreamMountQueue(() => 0);
        var old = new MountWork(StreamMountKind.Visual, queue.Revision, _ => throw new InvalidOperationException("A stale result mounted."));
        queue.RemoveMap(new("map")); queue.Enqueue(old); queue.Drain(VisibleWorld);
        if (old.Cancels != 1 || queue.Pending != 0) throw new InvalidOperationException("Old revision was retained.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ThreeDoubleBudgetOverrunsPauseActivationFor250Milliseconds()
    {
        var watchdog = new MountWatchdog(1);
        watchdog.Observe(2.1, 10); watchdog.Observe(2.1, 20); watchdog.Observe(2.1, 30);
        if (watchdog.CanActivate(279) || !watchdog.CanActivate(280) || watchdog.VisualBudgetMilliseconds != .5)
            throw new InvalidOperationException("Mount watchdog did not preserve its cooldown and reduced budget.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CoveredMountDrainsMultipleTilesAndSlicesOfEveryKind()
    {
        var clock = 0d; var steps = 0;
        var queue = new StreamMountQueue(() => clock);
        foreach (var kind in new[] { StreamMountKind.Collision, StreamMountKind.Visual, StreamMountKind.Auxiliary })
            for (var tile = 0; tile < 2; tile++)
                queue.Enqueue(new MountWork(kind, queue.Revision, budget =>
                {
                    if (budget <= KernelBudgets.StreamMountMilliseconds)
                        throw new InvalidOperationException("Covered work retained the visible-frame budget.");
                    steps++; clock += KernelBudgets.StreamCoveredMountMilliseconds / 36;
                }, tile.ToString(), slices: 3));
        queue.Drain(CoveredScreen);
        if (queue.Pending != 0 || steps != 18 || queue.LastMilliseconds <= KernelBudgets.StreamMountMilliseconds)
            throw new InvalidOperationException("Covered collision, visual or auxiliary work remained frame-throttled.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CoveredMountYieldsAtTheUiRefreshSlice()
    {
        var clock = 0d; var finishBudget = -1d; var steps = 0;
        var queue = new StreamMountQueue(() => clock);
        queue.Enqueue(new MountWork(StreamMountKind.Collision, queue.Revision,
            _ => { steps++; clock += KernelBudgets.StreamCoveredMountMilliseconds / 2; }, slices: 3));
        queue.Drain(CoveredScreen, remaining => finishBudget = remaining);
        if (queue.Pending != 1 || steps != 2 || finishBudget != 0)
            throw new InvalidOperationException("Covered mounting failed to yield for the loading UI.");
        queue.Drain(CoveredScreen);
        if (queue.Pending != 0 || steps != 3)
            throw new InvalidOperationException("Covered work failed to resume after the UI refresh.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CoveredMountBypassesVisibleWatchdogWithoutChangingItsCooldown()
    {
        var clock = 0d; var mounted = 0;
        var queue = new StreamMountQueue(() => clock);
        for (var frame = 0; frame < 3; frame++)
        {
            queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision,
                _ => clock += KernelBudgets.StreamMountMilliseconds * 3));
            queue.Drain(VisibleWorld);
        }
        if (queue.WatchdogTrips != 1) throw new InvalidOperationException("Visible mount overruns failed to trip the watchdog.");
        for (var frame = 0; frame < 3; frame++)
        {
            queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision,
                _ => { mounted++; clock += KernelBudgets.StreamCoveredMountMilliseconds; }));
            queue.Drain(CoveredScreen);
        }
        queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision, _ => mounted++));
        queue.Drain(VisibleWorld);
        if (mounted != 3 || queue.Pending != 1 || queue.WatchdogTrips != 1)
            throw new InvalidOperationException("Covered mounts were paused by or changed the visible-frame watchdog.");
    }
    [TestCase, TestCategory(TestReason.Regression)]
    public void MountAndCleanupFailuresKeepBothExceptionsAndMapRemovalContinues()
    {
        var queue = new StreamMountQueue(() => 0);
        var original = new InvalidDataException("mount failure");
        var cleanup = new InvalidOperationException("cancel failure");
        var failed = new MountWork(StreamMountKind.Visual, queue.Revision, _ => throw original,
            cancel: () => throw cleanup);
        queue.Enqueue(failed); queue.Drain(VisibleWorld);
        var failure = queue.Failure as AggregateException ?? throw new InvalidOperationException("Cleanup replaced the mount failure.");
        if (!failure.Flatten().InnerExceptions.Contains(original) || !failure.Flatten().InnerExceptions.Contains(cleanup) || queue.Pending != 0)
            throw new InvalidOperationException("The queue lost the mount or release failure.");
        var other = new MountWork(StreamMountKind.Auxiliary, queue.Revision, _ => { });
        var cannotCancel = new MountWork(StreamMountKind.Visual, queue.Revision, _ => { }, cancel: () => throw cleanup);
        queue.Enqueue(other); queue.Enqueue(cannotCancel); queue.RemoveMap(new("map"));
        if (other.Cancels != 1 || cannotCancel.Cancels != 1 || queue.Pending != 0)
            throw new InvalidOperationException("One cancellation failure skipped the remaining map work.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void CurrentVisualPrecedesBackgroundCollisionAndQueuedWorkUpgradesWithoutRestart()
    {
        var order = new List<string>();
        var queue = new StreamMountQueue(() => 0);
        var background = new MountWork(StreamMountKind.Collision, queue.Revision, _ => order.Add("background"), priority: BakePriority.Background);
        var current = new MountWork(StreamMountKind.Visual, queue.Revision, _ => order.Add("current"));
        queue.Enqueue(background); queue.Enqueue(current); queue.Drain(CoveredScreen);
        if (!order.SequenceEqual(["current", "background"]))
            throw new InvalidOperationException("Background collision displaced current geometry.");
        order.Clear();
        var promoted = new MountWork(StreamMountKind.Collision, queue.Revision, _ => order.Add("promoted"), priority: BakePriority.Background);
        queue.Enqueue(promoted);
        queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision, _ => order.Add("visual")));
        promoted.EffectivePriority = BakePriority.Collision;
        queue.Drain(CoveredScreen);
        if (!order.SequenceEqual(["promoted", "visual"]) || promoted.Cancels != 0 || queue.Pending != 0)
            throw new InvalidOperationException("An existing mount did not upgrade in place.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void SortingAndEnqueueMetadataFaultsReachFailureAndRetainCancellationCauses()
    {
        var queue = new StreamMountQueue(() => 0);
        var original = new InvalidDataException("sort metadata failed");
        var cleanup = new InvalidOperationException("release failed");
        var steps = 0;
        var failed = new MountWork(StreamMountKind.Visual, queue.Revision, _ => steps++,
            cancel: () => throw cleanup) { KeyFailure = original };
        var other = new MountWork(StreamMountKind.Visual, queue.Revision, _ => steps++);
        queue.Enqueue(failed); queue.Enqueue(other); queue.Drain(VisibleWorld);
        var causes = (queue.Failure as AggregateException ?? throw new InvalidOperationException("Sort cleanup lost a cause.")).Flatten().InnerExceptions;
        if (!causes.Any(error => ReferenceEquals(error.GetBaseException(), original)) || !causes.Contains(cleanup) ||
            steps != 0 || failed.Cancels != 1 || other.Cancels != 1 || queue.Pending != 0)
            throw new InvalidOperationException("Sort failure did not reach the owner and stop its frame.");
        var enqueue = new InvalidDataException("request revision failed");
        var rejected = new MountWork(StreamMountKind.Visual, queue.Revision, _ => { }) { RevisionFailure = enqueue };
        queue.Enqueue(rejected);
        if (rejected.Cancels != 1 || queue.Pending != 0 || queue.Failure is not AggregateException retained ||
            !retained.Flatten().InnerExceptions.Contains(enqueue))
            throw new InvalidOperationException("Enqueue failure was not retained with its rejected work released.");
        queue.Drain(VisibleWorld);
        if (failed.Cancels != 1 || other.Cancels != 1)
            throw new InvalidOperationException("The faulted frame was retried.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void FrameCoverageAdmissionReleaseFinishAndClockFaultsReachTheQueueOwner()
    {
        for (var point = 0; point < 5; point++)
        {
            var original = new InvalidDataException("frame boundary failed");
            var queue = new StreamMountQueue(point == 3 ? () => throw original : () => 0);
            var steps = 0;
            var work = new MountWork(StreamMountKind.Visual, queue.Revision, _ => steps++);
            queue.Enqueue(work);
            queue.Drain(new Coverage(() => point == 4 ? throw original : false),
                finish: _ => { if (point == 2) throw original; },
                release: _ => { if (point == 1) throw original; },
                canActivateVisual: () => point == 0 ? throw original : true);
            if (!ReferenceEquals(queue.Failure, original))
                throw new InvalidOperationException("A frame callback or clock fault escaped the failure projection.");
            if ((point == 0 || point >= 3) && (steps != 0 || work.Cancels != 1 || queue.Pending != 0))
                throw new InvalidOperationException("A failed admission or clock still advanced mount readiness.");
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task FailedMapRetainsItsCausesAndReleasesQueuedWorkThroughDrain()
    {
        using var fixture = new StreamLifecycleTestSupport.StreamFixture();
        await fixture.Until(() => fixture.Director.ViewReady);
        var original = new InvalidDataException("mount failure");
        var cleanup = new InvalidOperationException("cancel failure");
        var failed = new MountWork(StreamMountKind.Visual, fixture.Mount.Revision, _ => throw original,
            map: fixture.Context.Descriptor.Id);
        fixture.Mount.Enqueue(failed);
        fixture.Mount.Drain(CoveredScreen); fixture.Director._Process(.1);
        if (!ReferenceEquals(fixture.Director.Failure, original))
            throw new InvalidOperationException("The map lost the original mount failure.");
        var queued = new MountWork(StreamMountKind.Visual, fixture.Mount.Revision,
            _ => throw new InvalidOperationException("The failed map mounted queued work during shutdown."),
            cancel: () => throw cleanup, map: fixture.Context.Descriptor.Id);
        fixture.Mount.Enqueue(queued);
        try { await fixture.Unload(); throw new InvalidOperationException("Failed map drained without delivering its failure."); }
        catch (AggregateException error)
        {
            if (!error.Flatten().InnerExceptions.Contains(original) || !error.Flatten().InnerExceptions.Contains(cleanup)) throw;
        }
        if (!fixture.Director.IsUnloaded || fixture.Director.Failure is null || fixture.Mount.Failure is null ||
            fixture.Mount.Pending != 0 || failed.Cancels != 1 || queued.Cancels != 1)
            throw new InvalidOperationException("The failed map did not drain each queued owner once while retaining its failure.");
    }

    private sealed class MountWork(StreamMountKind kind, long revision, Action<double> step, string module = "module", int slices = 1, Action? cancel = null, BakePriority? priority = null, MapId? map = null) : IStreamMountWork
    {
        public Exception? KeyFailure, RevisionFailure;
        public BakeKey Key => KeyFailure is { } error ? throw error : new(map ?? new("map"), module, "L0/0/0");
        public BakePriority EffectivePriority { get; set; } = priority ?? (kind == StreamMountKind.Collision ? BakePriority.Collision : BakePriority.Normal);
        public BakePriority Priority => EffectivePriority;
        public long Revision => RevisionFailure is { } error ? throw error : revision;
        public StreamMountKind Kind { get; } = kind;
        public int Cancels;
        private int _steps;
        public bool Step(double budgetMilliseconds) { step(budgetMilliseconds); return ++_steps >= slices; }
        public void Cancel() { Cancels++; cancel?.Invoke(); }
    }
}
