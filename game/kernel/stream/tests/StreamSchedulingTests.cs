using Campus.Gen.Tests.Support;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Lod;
using Campus.Kernel.Stream;
using Campus.Kernel.Bake;
using Campus.Kernel.Registry;
using GdUnit4;
using System.Text.Json;

namespace Campus.Tests;

[TestSuite]
public sealed class StreamSchedulingTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void OpenTopFloorPrefetchAndSelectionNeverRequestItsClosedNeighbour()
    {
        var files = RealMapFixture.ReadFiles();
        var waypoint = JsonSerializer.Deserialize<WaypointFile>(files["waypoints.json"].Span, ContractJson.Options)!
            .Waypoints.Single(point => point.Id == "P16");
        var world = RealMapFixture.Load().World;
        var medium = world.Spatial.MediumAt(waypoint.Fly.Position);
        var building = medium.Feature ?? throw new InvalidOperationException("P16 is no longer indoors.");
        var entry = world.Interiors.Get(building)!;
        if (medium.Storey != entry.OpenStoreys.Max() || !entry.Storeys.Any(s => s.Index == medium.Storey + 1))
            throw new InvalidOperationException("The fixture no longer covers an open top floor next to a closed floor.");
        var context = new TestMapContext(RealMapFixture.Entry().Descriptor, world);
        var expected = entry.OpenStoreys.Where(floor => Math.Abs(floor - medium.Storey) <= 1).Order()
            .Select(floor => new BakeKey(context.Descriptor.Id, "interior." + entry.ProviderId,
                FormattableString.Invariant($"{building.Value}/{floor}/{(floor == medium.Storey ? "IF" : "IS")}"))).ToArray();
        foreach (bool knownDestination in new[] { false, true })
        {
            var bake = new InteriorRequests();
            var stream = new InteriorStream(bake, new StreamMountQueue(() => 0), new StreamMaterials(null!, null!), null!, _ => { });
            stream.Load(context);
            if (knownDestination) stream.Prefetch(new(building, checked((short)medium.Storey)));
            else stream.Select(medium);
            Console.WriteLine($"InteriorRequests waypoint={waypoint.Id} position={waypoint.Fly.Position} medium={medium} " +
                $"selector={(knownDestination ? "Prefetch" : "Select")} receiver=collection-fixture keys=[{string.Join(",", bake.Keys)}]");
            if (!bake.Keys.SequenceEqual(expected) || stream.Error is not null || knownDestination && stream.DestinationReady)
                throw new InvalidOperationException("Open-floor requests or pending destination readiness changed.");
            stream.Prefetch(new(building, checked((short)(medium.Storey + 1))));
            if (bake.Keys.Count != expected.Length || stream.Error is null || stream.DestinationReady)
                throw new InvalidOperationException("A closed destination reached the baker or became ready.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OffOriginModuleProducesOneWholeMapRootAndAnHonestEmptyBundleOnTheBakeWorker()
    {
        var context = new BuildContext
        {
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            Map = BackdropInputsTests.Descriptor(),
            World = new FakeWorld { Boundary = new([]) },
            WorldSeed = 1
        };
        var module = new RootModule();
        var catalog = ModuleTileCatalog.Build(context, [new("fixture.root", module)]);
        if (!catalog.Modules(StreamTile.Root).SequenceEqual(["fixture.root"]))
            throw new InvalidOperationException("Off-origin discovery failed to produce one whole-map root.");
        var key = BakeRequestKeys.Tile(context.Map.Id, "fixture.root", StreamTile.Root.Tile, LodLevel.L4);
        var builder = new BakeGeometryBuilder(context.Catalog, new ServiceRegistry());
        using var queue = new BakeQueue(1);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var caller = Environment.CurrentManagedThreadId;
        var handle = queue.Ensure(key, BakePriority.KnownDestination, () =>
        {
            entered.Set(); release.Wait();
            return builder.Build(key, context, module, _ => { });
        });
        try
        {
            if (!entered.Wait(5000) || !ReferenceEquals(handle,
                queue.Ensure(key, BakePriority.KnownDestination, () => throw new InvalidOperationException("Duplicate build."))))
                throw new InvalidOperationException("A repeated root request created another bake operation.");
            release.Set();
            if (!SpinWait.SpinUntil(() => handle.State is BakeState.Ready or BakeState.Failed, 5000))
                throw new InvalidOperationException("The actual bake worker did not finish.");
            if (handle.State != BakeState.Ready || handle.Tile is not { } tile || tile.Surfaces.Count != 0 ||
                tile.Key != key || tile.Tile != StreamTile.Root.Tile || tile.Level != LodLevel.L4 ||
                module.Calls != 1 || module.Thread == caller || module.Tile != new TileKey(0, 0) || module.Level != LodLevel.L4)
                throw new InvalidOperationException("Whole-map build identity, empty product or worker ownership changed.");
        }
        finally { release.Set(); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ActualBakeQueueKeepsFailuresAndDiscardsForgottenSameIdResults()
    {
        using var queue = new BakeQueue(1);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var key = new BakeKey(new("fixture-map"), "fixture.root", "L4/0/0");
        var old = queue.Ensure(key, BakePriority.KnownDestination, () => { entered.Set(); release.Wait(); return new object(); });
        try
        {
            if (!entered.Wait(5000)) throw new InvalidOperationException("Old worker did not start.");
            queue.ForgetMap(key.Map);
            var expected = new object();
            var current = queue.Ensure(key, BakePriority.KnownDestination, () => expected);
            release.Set();
            if (!SpinWait.SpinUntil(() => current.State is BakeState.Ready or BakeState.Failed, 5000) ||
                old.State != BakeState.Failed || current.State != BakeState.Ready || !ReferenceEquals(current.Product, expected))
                throw new InvalidOperationException("An old same-ID worker replaced the current map product.");
            var failed = queue.Ensure(key with { Detail = "L3/0/0" }, BakePriority.KnownDestination,
                () => throw new InvalidOperationException("Active build failure."));
            if (!SpinWait.SpinUntil(() => failed.State == BakeState.Failed, 5000) ||
                failed.Product is not null || failed.Error?.Contains("Active build failure.", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("An active build failure became a ready or empty product.");
        }
        finally { release.Set(); }
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
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
        queue.Drain();
        if (!order.SequenceEqual([StreamMountKind.Collision, StreamMountKind.Visual]) || queue.Pending != 1)
            throw new InvalidOperationException("Shared frame ordering or limits failed.");
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SameMapReloadRejectsAnOldRevisionAndCancelsItOnce()
    {
        var queue = new StreamMountQueue(() => 0);
        var old = new MountWork(StreamMountKind.Visual, queue.Revision, _ => throw new InvalidOperationException("A stale result mounted."));
        queue.RemoveMap(new("map")); queue.Enqueue(old); queue.Drain();
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
    [TestCategory(TestReason.Contract)]
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
    private sealed class InteriorRequests : IBakeService, IBakeHandle
    {
        public List<BakeKey> Keys { get; } = [];
        public IBakeHandle Ensure(BakeKey key, BakePriority priority) { Keys.Add(key); return this; }
        public BakeProgress Progress => default;
        public long CacheBytes => 0;
        public void Clear() => Keys.Clear();
        public void ClearFailures() { }
        public BakeState State => BakeState.Pending;
        public object? Product => null;
        public string? Error => null;
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
    private sealed class RootModule : IContentModule
    {
        public int Calls, Thread;
        public TileKey Tile;
        public LodLevel Level;
        public IReadOnlyList<Claim> Claims => [];
        public ModuleBudget Budget => new(0, 0, 0);
        public IEnumerable<TileKey> Tiles(BuildContext context) => [new(79, -108), new(79, -108)];
        public ContentOutput Build(BuildContext context, TileKey tile, LodLevel level)
        { Calls++; Thread = Environment.CurrentManagedThreadId; Tile = tile; Level = level; return ContentOutput.Empty; }
    }
}
