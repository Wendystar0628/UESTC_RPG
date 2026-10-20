using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Frame;
using Campus.Kernel.Stream;
using GdUnit4;

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
            queue.Drain(covered, release: budget =>
            {
                if (!covered && budget > KernelBudgets.StreamMountMilliseconds - KernelBudgets.CollisionMountMilliseconds)
                    throw new InvalidOperationException("Retirement took the reserved collision budget.");
            }, canActivateVisual: () => !retiring);
            if (collisionSteps != 1 || visualSteps != 0 || queue.Pending != 1 || queue.BlockedByRetirementFrames != 1)
                throw new InvalidOperationException("Retirement did not apply visual-only backpressure.");
            retiring = false; queue.Drain(covered, canActivateVisual: () => !retiring);
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
        queue.Enqueue(failed); queue.Drain(false);
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
        queue.Drain(false);
        if (!order.SequenceEqual([StreamMountKind.Collision, StreamMountKind.Visual]) || queue.Pending != 1)
            throw new InvalidOperationException("Shared frame ordering or limits failed.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SameMapReloadRejectsAnOldRevisionAndCancelsItOnce()
    {
        var queue = new StreamMountQueue(() => 0);
        var old = new MountWork(StreamMountKind.Visual, queue.Revision, _ => throw new InvalidOperationException("A stale result mounted."));
        queue.RemoveMap(new("map")); queue.Enqueue(old); queue.Drain(false);
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
        queue.Drain(true);
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
        queue.Drain(true, remaining => finishBudget = remaining);
        if (queue.Pending != 1 || steps != 2 || finishBudget != 0)
            throw new InvalidOperationException("Covered mounting failed to yield for the loading UI.");
        queue.Drain(true);
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
            queue.Drain(false);
        }
        if (queue.WatchdogTrips != 1) throw new InvalidOperationException("Visible mount overruns failed to trip the watchdog.");
        for (var frame = 0; frame < 3; frame++)
        {
            queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision,
                _ => { mounted++; clock += KernelBudgets.StreamCoveredMountMilliseconds; }));
            queue.Drain(true);
        }
        queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision, _ => mounted++));
        queue.Drain(false);
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
        queue.Enqueue(failed); queue.Drain(false);
        var failure = queue.Failure as AggregateException ?? throw new InvalidOperationException("Cleanup replaced the mount failure.");
        if (!failure.Flatten().InnerExceptions.Contains(original) || !failure.Flatten().InnerExceptions.Contains(cleanup) || queue.Pending != 0)
            throw new InvalidOperationException("The queue lost the mount or release failure.");
        var other = new MountWork(StreamMountKind.Auxiliary, queue.Revision, _ => { });
        var cannotCancel = new MountWork(StreamMountKind.Visual, queue.Revision, _ => { }, cancel: () => throw cleanup);
        queue.Enqueue(other); queue.Enqueue(cannotCancel); queue.RemoveMap(new("map"));
        if (other.Cancels != 1 || cannotCancel.Cancels != 1 || queue.Pending != 0)
            throw new InvalidOperationException("One cancellation failure skipped the remaining map work.");
    }

    private sealed class MountWork(StreamMountKind kind, long revision, Action<double> step, string module = "module", int slices = 1, Action? cancel = null) : IStreamMountWork
    {
        public BakeKey Key => new(new("map"), module, "L0/0/0");
        public BakePriority Priority => Kind == StreamMountKind.Collision ? BakePriority.Collision : BakePriority.Normal;
        public long Revision { get; } = revision;
        public StreamMountKind Kind { get; } = kind;
        public int Cancels;
        private int _steps;
        public bool Step(double budgetMilliseconds) { step(budgetMilliseconds); return ++_steps >= slices; }
        public void Cancel() { Cancels++; cancel?.Invoke(); }
    }
}
