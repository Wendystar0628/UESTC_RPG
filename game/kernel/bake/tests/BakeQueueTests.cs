using Campus.Tests;
using static GdUnit4.Assertions;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.AppFlow;
using GdUnit4;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeQueueTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WorkerFailureDeliversTheOriginalExceptionToBothOwners()
    {
        using var queue = new BakeQueue(1, static () => { });
        var session = new BakeQueue.MapSession(new("fixture"));
        var error = new InvalidDataException("Invalid authoritative bake input.");
        var handle = queue.Ensure(session, new(new("fixture"), "fixture.module", "fixture"), BakePriority.Normal, () => throw error);
        if (!SpinWait.SpinUntil(() => handle.State == BakeState.Failed, 5000)) throw new TimeoutException("Bake failure did not arrive.");
        Exception? awaited = null;
        try { handle.Done.GetAwaiter().GetResult(); }
        catch (Exception actual) { awaited = actual; }
        queue.Dispose();
        if (!ReferenceEquals(awaited, error) || !ReferenceEquals(handle.Failure, error) ||
            queue.Failure is not AppStartupException { Code: AppStartupFailureCodes.BakeFailed } coded || !ReferenceEquals(coded.InnerException, error))
            throw new InvalidOperationException("Bake failure lost its code or original exception object.");
        var cleanup = new IOException("Bake publication cleanup failed.");
        queue.RecordFailure(cleanup);
        if (queue.Failure is not AppStartupException { Code: AppStartupFailureCodes.BakeFailed,
                InnerException: AggregateException causes } summary ||
            !ReferenceEquals(queue.Failure, summary) || !causes.InnerExceptions.SequenceEqual(new Exception[] { error, cleanup }))
            throw new InvalidOperationException("Bake failure publication lost a source or introduced nested code wrappers.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ARealWorkerFaultAfterUnloadReachesEveryDrain()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var queue = new BakeQueue(1, static () => { });
        var session = new BakeQueue.MapSession(new("fixture"));
        var error = new InvalidDataException("Fault after map cancellation.");
        var key = new BakeKey(new("fixture"), "fixture.module", "fixture");
        var handle = queue.Ensure(session, key, BakePriority.Normal, () =>
        {
            started.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Worker fixture did not release.");
            throw error;
        });
        try
        {
            if (!started.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Worker did not start.");
            var first = queue.ForgetMap(session);
            var second = queue.ForgetMap(session);
            if (first.IsCompleted || second.IsCompleted || handle.Done.IsCompleted)
                throw new InvalidOperationException("Unload completed before the worker.");
            AssertThrown(() => queue.Ensure(session, key, BakePriority.Normal, () => new object()))
                .IsInstanceOf<OperationCanceledException>();
            release.Set();
            foreach (var task in new[] { first, second, handle.Done })
            {
                try { task.GetAwaiter().GetResult(); throw new InvalidOperationException("A worker failure was lost."); }
                catch (AggregateException aggregate) when (aggregate.Flatten().InnerExceptions.Any(value => ReferenceEquals(value, error))) { }
                catch (InvalidDataException actual) when (ReferenceEquals(actual, error)) { }
            }
            if (!ReferenceEquals(handle.Failure, error) ||
                queue.Failure is not AppStartupException { Code: AppStartupFailureCodes.BakeFailed } coded || !ReferenceEquals(coded.InnerException, error))
                throw new InvalidOperationException("Unload changed the worker failure identity.");
        }
        finally { release.Set(); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CompletionWaitsForSourceIndexPublicationAndRetainsItsFailure()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var error = new IOException("Source index publication failed.");
        var session = new BakeQueue.MapSession(new("fixture"));
        using var queue = new BakeQueue(1, () =>
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Publication fixture did not release.");
            throw error;
        });
        try
        {
            queue.Ensure(session, new(new("fixture"), "fixture.module", "fixture"), BakePriority.Normal, static () => new object());
            if (!entered.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Publication did not start.");
            if (queue.Progress.Done == queue.Progress.Total) throw new InvalidOperationException("Batch completed before source index publication.");
        }
        finally { release.Set(); queue.Dispose(); }
        if (queue.Failure is not AppStartupException { Code: AppStartupFailureCodes.BakeFailed } coded || !ReferenceEquals(coded.InnerException, error))
            throw new InvalidOperationException("Source index publication lost its original failure.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void UnloadingAnInFlightRequestKeepsItsOldHandleFailed()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var queue = new BakeQueue(1, static () => { });
        var session = new BakeQueue.MapSession(new("fixture"));
        try
        {
            var handle = queue.Ensure(session, key, BakePriority.KnownDestination, () =>
            {
                started.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker release timed out.");
                return new object();
            });
            if (!started.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker did not start.");
            var firstDrain = queue.ForgetMap(session);
            var secondDrain = queue.ForgetMap(session);
            if (firstDrain.IsCompleted || secondDrain.IsCompleted || handle.Done.IsCompleted)
                throw new InvalidOperationException("A repeated drain detached the running worker.");
            release.Set();
            Task.WhenAll(firstDrain, secondDrain).GetAwaiter().GetResult();
            queue.Dispose();
            if (!handle.Done.IsCanceled) throw new InvalidOperationException("An unloaded request did not finish as canceled.");
            if (handle.State != BakeState.Failed || handle.Product is not null || handle.Failure is null)
                throw new InvalidOperationException("An unloaded map's in-flight request became a ready product.");
            if (queue.Progress.Total != 0 || queue.Progress.Done != 0)
                throw new InvalidOperationException("An unloaded map left an unfinished request in progress.");
        }
        finally { release.Set(); queue.Dispose(); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void StoppingRejectsPendingWorkAndLateProductsWhileDisposeStillDrainsWorkers()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var queue = new BakeQueue(1, static () => { });
        var session = new BakeQueue.MapSession(new("fixture"));
        var finished = false;
        var pendingRan = false;
        try
        {
            var running = queue.Ensure(session, key, BakePriority.Normal, () =>
            {
                started.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker release timed out.");
                finished = true;
                return new object();
            });
            if (!started.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker did not start.");
            var pending = queue.Ensure(session, key with { Detail = "pending" }, BakePriority.Background, () =>
            {
                pendingRan = true;
                return new object();
            });
            queue.Stop(); // Must return while generation is still paused.
            AssertThat(running.State == BakeState.Failed && pending.State == BakeState.Failed && !finished)
                .OverrideFailureMessage("Stopping waited for generation or left publishable work.").IsTrue();
            AssertThrown(() => queue.Ensure(session, key, BakePriority.Normal, () => new object()))
                .IsInstanceOf<ObjectDisposedException>();
            release.Set();
            queue.Dispose(); // Even after Stop, ordinary disposal must join the remaining CPU worker.
            AssertThat(finished && !pendingRan && running.Product is null && pending.Product is null && queue.Failure is null)
                .OverrideFailureMessage("A stopped job ran or published, or disposal failed to drain its worker.").IsTrue();
        }
        finally { release.Set(); queue.Dispose(); }
    }

}
