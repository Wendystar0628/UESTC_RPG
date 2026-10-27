using Campus.Tests;
using static GdUnit4.Assertions;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.AppFlow;
using GdUnit4;
using Campus.Gen.BakeInput;
using Campus.Gen.Tests.Support;
using System.Numerics;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeQueueTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void SourceIndexSnapshotsStayOrderedAndCompleteWhileProductsArrive()
    {
        var context = BakeInputFixture.Context(new FakeWorld { Boundary = new([]) });
        var source = new BakeSourceIndex(context);
        const int rounds = 64, batch = 64;
        var products = Enumerable.Range(0, (rounds + 1) * batch).Select(index =>
            new BakeProductInput(new(context.Map.Id, "fixture.module", (rounds * batch - index).ToString("D5")), "stamp/" + index)).ToArray();
        foreach (var product in products.Take(batch)) source.RememberProduct(product.Key, product.Stamp);
        using var phases = new Barrier(2);
        var writer = Task.Factory.StartNew(() =>
        {
            for (var round = 0; round < rounds; round++)
            {
                Meet();
                foreach (var product in products.Skip((round + 1) * batch).Take(batch))
                {
                    source.RememberProduct(product.Key, product.Stamp);
                    Thread.Yield();
                }
                Meet();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        var snapshots = new List<BakeSourceIndexData>();
        var overlappingWrites = false;
        try
        {
            for (var round = 0; round < rounds; round++)
            {
                Meet();
                var before = source.ProductCount;
                try
                {
                    snapshots.Add(source.Snapshot("fixture-source", "fixture-resident"));
                    overlappingWrites |= source.ProductCount > before;
                }
                finally { Meet(); }
            }
        }
        finally
        {
            phases.RemoveParticipant();
            writer.GetAwaiter().GetResult();
        }
        if (!overlappingWrites) throw new InvalidOperationException("The fixture did not overlap source snapshots with arriving products.");
        var expected = products.ToDictionary(product => product.Key, product => product.Stamp);
        foreach (var snapshot in snapshots.Append(source.Snapshot("fixture-source", "fixture-resident")))
        {
            var sorted = snapshot.Products.OrderBy(product => product.Key.Module, StringComparer.Ordinal)
                .ThenBy(product => product.Key.Detail, StringComparer.Ordinal);
            if (!snapshot.Products.SequenceEqual(sorted) || snapshot.Products.Select(product => product.Key).Distinct().Count() != snapshot.Products.Length ||
                snapshot.Products.Any(product => !expected.TryGetValue(product.Key, out var stamp) || stamp != product.Stamp))
                throw new InvalidOperationException("A concurrent source snapshot lost product identity, stamps or deterministic ordering.");
        }
        if (source.Snapshot("fixture-source", "fixture-resident").Products.Length != products.Length)
            throw new InvalidOperationException("The settled source snapshot omitted completed products.");
        void Meet()
        {
            if (!phases.SignalAndWait(5000)) throw new TimeoutException("Concurrent source snapshot participants did not rendezvous.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void InteractiveIdentityHitsDoNotQueuePlaceholdersAndExplicitBatchStillSchedules()
    {
        var map = CatalogMap();
        using var lifetime = map.Lifetime;
        using var queue = new BakeQueue(1, static () => { });
        var options = new BakeOptions(Path.GetTempPath(), false, false, true, false, 3, null);
        IBakeHandle? handle = null;
        var product = new object();
        var catalog = new BakeCatalog(options, queue, (current, key, priority) =>
        {
            handle = queue.Ensure(current.Work, key, priority, product, static value => value);
        }, static (_, _) => true);
        catalog.SetPhase(AppState.Title);
        var thread = catalog.Begin(map) ?? throw new InvalidOperationException("Interactive catalog did not start.");
        thread.Start();
        catalog.Drain();
        if (handle is not null)
            throw new InvalidOperationException("An identity hit created a job or published a Ready placeholder.");
        catalog.Queue(map);
        if (handle is null) throw new InvalidOperationException("Explicit batch omitted a matching product.");
        handle.Done.GetAwaiter().GetResult();
        if (handle.State != BakeState.Ready ||
            !ReferenceEquals(handle.Product, product))
            throw new InvalidOperationException("Explicit scheduling used the identity shortcut or lost the real product.");
    }

    private static BakeService.MapBuild CatalogMap()
    {
        var context = BakeInputFixture.Context(new FakeWorld { Boundary = new([]) });
        var windows = new BakeWindowPreparation.Result(context.WindowNumbering, new([], []));
        var data = new Lazy<BakeService.LoadedMap>(() => new(1, context.SourceInputs, [], context.InteriorProviders, windows, []));
        _ = data.Value;
        var map = new BakeService.MapBuild(new TestMapContext(context.Map, context.World),
            new Dictionary<string, ContentModuleInput>(), new Dictionary<string, BakeService.Interior>(),
            [new("fixture.map", new CatalogProducer(), "fixture-code")], null!, context.ClaimedBy, data,
            context.GlobalInputs, context.GroundMaskContributors, context.PartLods!, context.Catalog, null, null);
        var lighting = new LightInventoryDocument(context.Map.Id, [], [], [], new([], ReadOnlyMemory<float>.Empty, ReadOnlyMemory<float>.Empty, 0, 0))
            { WindowNumbering = windows.Snapshot };
        var metadata = new BakeMapMetadata(lighting, new(context.Map.Id, []),
            new(new(new(Vector2.Zero, Vector2.One), 1, 1, 1f), new byte[4], new byte[4]));
        map.Directory.Prepare(metadata);
        map.Content.CompletePreparation(metadata);
        return map;
    }

    private sealed class CatalogProducer : IBakeProducer
    {
        public BakePayloadClass PayloadClass => BakePayloadClass.Product;
        public BakeProductKind ProductKind => BakeProductKind.Map;
        public BakeInputDependencies Inputs(BakeKey key) => BakeInputDependencies.World;
        public bool UsesModule(IContentModule module) => false;
        public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context) => [];
        public bool CanBuild(BakeKey key) => key.Module == MapBaker.Module && key.Detail == MapBaker.Detail;
        public object Build(BakeKey key, BuildContext context, string inputHash) => throw new NotSupportedException();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WorkerFailureDeliversTheOriginalExceptionToBothOwners()
    {
        using var queue = new BakeQueue(1, static () => { });
        var session = new BakeQueue.MapSession(new("fixture"));
        var error = new InvalidDataException("Invalid authoritative bake input.");
        var handle = queue.Ensure(session, new(new("fixture"), "fixture.module", "fixture"), BakePriority.Normal, error, static failure => throw failure);
        if (!SpinWait.SpinUntil(() => handle.State == BakeState.Failed, 5000)) throw new TimeoutException("Bake failure did not arrive.");
        Exception? awaited = null;
        try { handle.Done.GetAwaiter().GetResult(); }
        catch (Exception actual) { awaited = actual; }
        queue.Dispose();
        if (!ReferenceEquals(awaited, error) || !ReferenceEquals(handle.Failure, error) ||
            queue.Failure is not AppStartupException terminal || !ReferenceEquals(terminal.InnerException, error))
            throw new InvalidOperationException("Bake failure lost its terminal type or original exception object.");
        if (queue.Progress.Stage != "生成失败 1 项") throw new InvalidOperationException("A counted worker failure was omitted from progress.");
        var cleanup = new IOException("Bake publication cleanup failed.");
        queue.RecordFailure(cleanup);
        if (queue.Failure is not AppStartupException { InnerException: AggregateException causes } summary ||
            !ReferenceEquals(queue.Failure, summary) || !causes.InnerExceptions.SequenceEqual(new Exception[] { error, cleanup }))
            throw new InvalidOperationException("Bake failure publication lost a source or introduced nested terminal wrappers.");
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
        var handle = queue.Ensure(session, key, BakePriority.Normal, (started, release, error), static state =>
        {
            state.started.Set();
            if (!state.release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Worker fixture did not release.");
            throw state.error;
        });
        try
        {
            if (!started.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Worker did not start.");
            var first = queue.ForgetMap(session);
            var second = queue.ForgetMap(session);
            if (first.IsCompleted || second.IsCompleted || handle.Done.IsCompleted)
                throw new InvalidOperationException("Unload completed before the worker.");
            AssertThrown(() => queue.Ensure(session, key, BakePriority.Normal, key, static _ => new object()))
                .IsInstanceOf<OperationCanceledException>();
            release.Set();
            foreach (var task in new[] { first, second, handle.Done })
            {
                try { task.GetAwaiter().GetResult(); throw new InvalidOperationException("A worker failure was lost."); }
                catch (AggregateException aggregate) when (aggregate.Flatten().InnerExceptions.Any(value => ReferenceEquals(value, error))) { }
                catch (InvalidDataException actual) when (ReferenceEquals(actual, error)) { }
            }
            if (!ReferenceEquals(handle.Failure, error) ||
                queue.Failure is not AppStartupException terminal || !ReferenceEquals(terminal.InnerException, error))
                throw new InvalidOperationException("Unload changed the worker failure identity.");
            if (queue.Progress.Total != 0 || queue.Progress.Done != 0 || queue.Progress.Stage != "已完成")
                throw new InvalidOperationException("A retired late failure leaked into counted progress.");
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
            queue.Ensure(session, new(new("fixture"), "fixture.module", "fixture"), BakePriority.Normal, session, static _ => new object());
            if (!entered.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Publication did not start.");
            if (queue.Progress.Done == queue.Progress.Total) throw new InvalidOperationException("Batch completed before source index publication.");
        }
        finally { release.Set(); queue.Dispose(); }
        if (queue.Failure is not AppStartupException terminal || !ReferenceEquals(terminal.InnerException, error))
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
            var handle = queue.Ensure(session, key, BakePriority.KnownDestination, (started, release), static state =>
            {
                state.started.Set();
                if (!state.release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker release timed out.");
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
            var running = queue.Ensure(session, key, BakePriority.Normal, (started, release), state =>
            {
                state.started.Set();
                if (!state.release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker release timed out.");
                finished = true;
                return new object();
            });
            if (!started.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker did not start.");
            var pending = queue.Ensure(session, key with { Detail = "pending" }, BakePriority.Background, key, _ =>
            {
                pendingRan = true;
                return new object();
            });
            queue.Stop(); // Must return while generation is still paused.
            AssertThat(running.State == BakeState.Failed && pending.State == BakeState.Failed && !finished)
                .OverrideFailureMessage("Stopping waited for generation or left publishable work.").IsTrue();
            AssertThrown(() => queue.Ensure(session, key, BakePriority.Normal, key, static _ => new object()))
                .IsInstanceOf<ObjectDisposedException>();
            release.Set();
            queue.Dispose(); // Even after Stop, ordinary disposal must join the remaining CPU worker.
            AssertThat(finished && !pendingRan && running.Product is null && pending.Product is null && queue.Failure is null)
                .OverrideFailureMessage("A stopped job ran or published, or disposal failed to drain its worker.").IsTrue();
        }
        finally { release.Set(); queue.Dispose(); }
    }

}
