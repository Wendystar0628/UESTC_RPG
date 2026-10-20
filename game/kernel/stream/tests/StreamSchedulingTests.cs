using Campus.Gen.Tests.Support;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Lod;
using Campus.Kernel.Stream;
using Campus.Kernel.Bake;
using Campus.Kernel.Registry;
using GdUnit4;
using System.Text.Json;
using Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;

namespace Campus.Tests;

[TestSuite]
public sealed class StreamSchedulingTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    [TestCategory("Slow")]
    public void LibraryAtriumRequestsAllDeclaredOpenStoreysAndClearsThemOnReload()
    {
        var world = RealMapFixture.Load().World;
        var providerId = typeof(LibraryInteriorProvider).GetCustomAttributes(typeof(InteriorProviderAttribute), false)
            .Cast<InteriorProviderAttribute>().Single().Id;
        using var file = JsonDocument.Parse(RealMapFixture.ReadFiles()["interiors.json"]);
        var entry = file.RootElement.GetProperty("interiors").EnumerateArray()
            .Single(item => item.GetProperty("providerId").GetString() == providerId);
        var features = entry.GetProperty("featureIds").EnumerateArray().Select(item => new FeatureId(item.GetString()!)).ToArray();
        var parts = InteriorEntranceJson.ParseOpenParts(entry);
        var floors = parts.SelectMany(part => part.Floors).Distinct().Order().ToArray();
        var provider = new LibraryInteriorProvider();
        provider.Configure(new(features, parts, new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>(), null, []));
        var context = new TestMapContext(RealMapFixture.Entry().Descriptor, world);
        var layouts = new Dictionary<FeatureId, InteriorLayout>();
        var generation = new BuildContext {
            CodeHash = "fixture", WindowNumbering = new((context.Descriptor).Id, new string('0', 64), []), Map = context.Descriptor, World = world, WorldSeed = 1, InteriorLayouts = layouts,
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            InteriorProviders = new Dictionary<string, IInteriorProvider> { [providerId] = provider } };
        foreach (var feature in features) layouts.Add(feature, provider.Layout(generation, feature));
        var layout = layouts.Values.Single(item => item.AtriumVisibleStoreys.Count > 0);
        var atriumFloor = layout.AtriumVisibleStoreys.Min();
        var bake = new InteriorRequests();
        var stream = new InteriorStream(bake, new StreamMountQueue(() => 0), new StreamMaterials(null!, null!), null!, _ => { }, false);
        stream.Load(context, generation);
        var expected = layout.OpenStoreys.Where(floor => Math.Abs(floor - atriumFloor) <= 1 || layout.AtriumVisibleStoreys.Contains(floor)).Order()
            .Select(floor => new BakeKey(context.Descriptor.Id, "interior." + providerId,
                FormattableString.Invariant($"{layout.Feature.Value}/{floor}/{(floor == atriumFloor ? "IF" : "IS")}"))).ToArray();
        var area = world.Interiors.Get(layout.Feature)!.OpenAreas.First(area => area.Index == atriumFloor);
        var center = Campus.Gen.Geometry.PlanGeometry.Centroid(area.Footprint);
        var destination = new System.Numerics.Vector3(center.X, area.FloorY + WalkRules.StandingEye, center.Y);
        var preloads = new List<BakeKey>();
        stream.Preload(new(layout.Feature, checked((short)atriumFloor)), destination, preloads.Add);
        if (!preloads.SequenceEqual(expected) || bake.Keys.Count != 0 || stream.Prepared.Any() || !stream.DestinationReady)
            throw new InvalidOperationException("Interior CPU preload changed native residency or the active destination barrier.");
        stream.Prefetch(new(layout.Feature, checked((short)atriumFloor)), destination);
        if (!bake.Keys.SequenceEqual(expected)) throw new InvalidOperationException("Atrium-visible open storeys did not reach the baker.");
        stream.Unload(); bake.Clear(); stream.Load(context);
        stream.Prefetch(new(layout.Feature, checked((short)atriumFloor)), destination);
        if (bake.Keys.Count != layout.OpenStoreys.Count(floor => Math.Abs(floor - atriumFloor) <= 1))
            throw new InvalidOperationException("Atrium selection leaked into another map load.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [TestCategory("Slow")]
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
            var stream = new InteriorStream(bake, new StreamMountQueue(() => 0), new StreamMaterials(null!, null!), null!, _ => { }, false);
            stream.Load(context);
            if (knownDestination) stream.Prefetch(new(building, checked((short)medium.Storey)), waypoint.Fly.Position);
            else stream.Select(medium, waypoint.Fly.Position);
            Console.WriteLine($"InteriorRequests waypoint={waypoint.Id} position={waypoint.Fly.Position} medium={medium} " +
                $"selector={(knownDestination ? "Prefetch" : "Select")} receiver=collection-fixture keys=[{string.Join(",", bake.Keys)}]");
            if (!bake.Keys.SequenceEqual(expected) || stream.Error is not null || knownDestination && stream.DestinationReady)
                throw new InvalidOperationException("Open-floor requests or pending destination readiness changed.");
            stream.Prefetch(new(building, checked((short)(medium.Storey + 1))), waypoint.Fly.Position);
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
            CodeHash = "fixture", WindowNumbering = new((BackdropInputsTests.Descriptor()).Id, new string('0', 64), []),
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            Map = BackdropInputsTests.Descriptor(),
            World = new FakeWorld { Boundary = new([]) },
            WorldSeed = 1
        };
        var module = new RootModule();
        var catalog = ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>> { ["fixture.root"] = module.Tiles(context).ToArray() }, []));
        if (!catalog.Modules(StreamTile.Root).SequenceEqual(["fixture.root"]))
            throw new InvalidOperationException("Off-origin discovery failed to produce one whole-map root.");
        var key = BakeRequestKeys.Tile(context.Map.Id, "fixture.root", StreamTile.Root.Tile, LodLevel.L4);
        var builder = new BakeGeometryBuilder(context.Catalog, new ServiceRegistry());
        using var queue = new BakeQueue(1, static () => { });
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
        using var queue = new BakeQueue(1, static () => { });
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
        visibility.Apply(new HashSet<StreamTile> { root }, 479.5f, true, (_, _, _) => { });
        if (visibility.IsSettled(479.5f)) throw new InvalidOperationException("Newly entering geometry was certified before its shader fade.");
        visibility.Apply(new HashSet<StreamTile> { root }, 479.8f, true, (_, _, _) => { });
        if (!visibility.IsSettled(479.8f)) throw new InvalidOperationException("Fully presented geometry was not ready.");
        visibility.Apply(new HashSet<StreamTile> { child }, 479.9f, true, (_, _, _) => { });
        visibility.Apply(new HashSet<StreamTile> { child }, .1f, true, (_, _, _) => { });
        if (!visibility.Drawn.Contains(root) || visibility.IsSettled(.1f)) throw new InvalidOperationException("A partially presented replacement was certified ready.");
        visibility.Apply(new HashSet<StreamTile> { child }, .2f, true, (_, _, _) => { });
        if (visibility.Drawn.Contains(root) || !visibility.Drawn.Contains(child) || !visibility.IsSettled(.2f)) throw new InvalidOperationException("Fade completion left stale parent coverage or unreadiness.");
    }
    private sealed class InteriorRequests : IBakeService, IBakeHandle
    {
        public List<BakeKey> Keys { get; } = [];
        public IBakeHandle Ensure(BakeKey key, BakePriority priority) { Keys.Add(key); return this; }
        public BakeProgress Progress => default;
        public long CacheBytes => 0;
        public void Clear() => Keys.Clear();
        public void ClearFailures() => throw new NotSupportedException();
        public BakeState State => BakeState.Pending;
        public object? Product => null;
        public string? Error => null;
    }
    private sealed class RootModule : IContentModule
    {
        public int Calls, Thread;
        public TileKey Tile;
        public LodLevel Level;
        public BakeInputDependencies BakeInputs => BakeInputDependencies.World;
        public BakeInputDependencies CollisionInputs => BakeInputDependencies.World;
        public IReadOnlyList<Claim> Claims => [];
        public IEnumerable<TileKey> Tiles(BuildContext context) => [new(79, -108), new(79, -108)];
        public ContentOutput Build(BuildContext context, TileKey tile, LodLevel level)
        { Calls++; Thread = Environment.CurrentManagedThreadId; Tile = tile; Level = level; return ContentOutput.Empty; }
    }
}
