using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Lod;
using Campus.Kernel.Stream;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class StreamSchedulingTests
{
    [TestCase]
    public void RetirementBlocksVisualsButPreservesCollisionAndResumesAfterDrain()
    {
        var retiring = true; var collisionSteps = 0; var visualSteps = 0;
        var queue = new StreamMountQueue(() => 0);
        queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision, _ => visualSteps++));
        queue.Enqueue(new MountWork(StreamMountKind.Collision, queue.Revision, _ => collisionSteps++));
        queue.Drain(release: budget =>
        {
            if (budget > .7) throw new InvalidOperationException("Retirement took the reserved collision budget.");
        }, canActivateVisual: () => !retiring);
        if (collisionSteps != 1 || visualSteps != 0 || queue.Pending != 1 || queue.BlockedByRetirementFrames != 1)
            throw new InvalidOperationException("Retirement did not apply visual-only backpressure.");
        retiring = false; queue.Drain(canActivateVisual: () => !retiring);
        if (visualSteps != 1 || queue.Pending != 0) throw new InvalidOperationException("Visual activation did not resume after retirement.");
    }
    [TestCase]
    public void FailedMountCancelsAndLeavesNoQueuedWork()
    {
        var errors = new List<string>();
        var queue = new StreamMountQueue(() => 0, errors.Add);
        var failed = new MountWork(StreamMountKind.Visual, queue.Revision, _ => throw new InvalidOperationException("fixture failure"));
        queue.Enqueue(failed); queue.Drain();
        if (failed.Cancels != 1 || queue.Pending != 0 || errors.Count != 1)
            throw new InvalidOperationException("Failed mount was not cancelled and removed.");
    }
    [TestCase]
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
        queue.Drain();
        if (!order.SequenceEqual([StreamMountKind.Collision, StreamMountKind.Visual]) || queue.Pending != 1)
            throw new InvalidOperationException("Shared frame ordering or limits failed.");
    }
    [TestCase]
    public void SameMapReloadRejectsAnOldRevisionAndCancelsItOnce()
    {
        var queue = new StreamMountQueue(() => 0);
        var old = new MountWork(StreamMountKind.Visual, queue.Revision, _ => throw new InvalidOperationException("A stale result mounted."));
        queue.RemoveMap(new("map")); queue.Enqueue(old); queue.Drain();
        if (old.Cancels != 1 || queue.Pending != 0) throw new InvalidOperationException("Old revision was retained.");
    }
    [TestCase]
    public void ThreeDoubleBudgetOverrunsPauseActivationFor250Milliseconds()
    {
        var watchdog = new MountWatchdog(1);
        watchdog.Observe(2.1, 10); watchdog.Observe(2.1, 20); watchdog.Observe(2.1, 30);
        if (watchdog.CanActivate(279) || !watchdog.CanActivate(280) || watchdog.VisualBudgetMilliseconds != .5)
            throw new InvalidOperationException("Mount watchdog did not preserve its cooldown and reduced budget.");
    }
    [TestCase]
    public void CollisionWinsOverCoarseGeometryAndReplanDropsOnlyUnissuedRequests()
    {
        var queue = new StreamRequestQueue();
        var fine = new StreamTile(new(0, 0), LodLevel.L0);
        var collision = new StreamRequest(new(new("map"), "collision", "C/0/0"), fine, BakePriority.Collision, 1);
        queue.Replace([new(new(new("map"), "far", "L4/0/0"), StreamTile.Root, BakePriority.Normal, 0), collision]);
        if (!queue.TryDequeue(out var first) || first != collision) throw new InvalidOperationException("Collision did not get the first slot.");
        queue.Replace([]);
        if (queue.TryDequeue(out _)) throw new InvalidOperationException("A cancelled unissued request survived replan.");
    }
    [TestCase]
    public void ParentStaysDrawnUntilComplementaryFadeFinishesAcrossClockWrap()
    {
        var visibility = new TileVisibility(.25f); var root = StreamTile.Root;
        var child = new StreamTile(new(0, 0), LodLevel.L3);
        visibility.Apply(new HashSet<StreamTile> { root }, 479.8f, true, (_, _, _) => { });
        visibility.Apply(new HashSet<StreamTile> { child }, 479.9f, true, (_, _, _) => { });
        visibility.Apply(new HashSet<StreamTile> { child }, .1f, true, (_, _, _) => { });
        if (!visibility.Drawn.Contains(root)) throw new InvalidOperationException("Parent faded out before 250 ms.");
        visibility.Apply(new HashSet<StreamTile> { child }, .2f, true, (_, _, _) => { });
        if (visibility.Drawn.Contains(root) || !visibility.Drawn.Contains(child)) throw new InvalidOperationException("Fade completion left stale parent coverage.");
    }
    private sealed class MountWork(StreamMountKind kind, long revision, Action<double> step, string module = "module") : IStreamMountWork
    {
        public BakeKey Key => new(new("map"), module, "L0/0/0");
        public BakePriority Priority => Kind == StreamMountKind.Collision ? BakePriority.Collision : BakePriority.Normal;
        public long Revision { get; } = revision;
        public StreamMountKind Kind { get; } = kind;
        public int Cancels;
        public bool Step(double budgetMilliseconds) { step(budgetMilliseconds); return true; }
        public void Cancel() => Cancels++;
    }
}
