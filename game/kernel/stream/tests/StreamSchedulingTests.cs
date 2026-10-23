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
    public void LibraryAtriumRequestsDeclaredVisibleStoreysAndTracksDestinationReadiness()
    {
        var world = RealMapFixture.Load().World;
        var providerId = typeof(LibraryInteriorProvider).GetCustomAttributes(typeof(InteriorProviderAttribute), false)
            .Cast<InteriorProviderAttribute>().Single().Id;
        using var file = JsonDocument.Parse(RealMapFixture.ReadFiles()["interiors.json"]);
        var entry = file.RootElement.GetProperty("interiors").EnumerateArray()
            .Single(item => item.GetProperty("providerId").GetString() == providerId);
        var features = entry.GetProperty("featureIds").EnumerateArray().Select(item => new FeatureId(item.GetString()!)).ToArray();
        var parts = InteriorEntranceJson.ParseOpenParts(entry);
        var provider = new LibraryInteriorProvider();
        provider.Configure(new(features.Where(feature => InteriorOpenPart.IndoorFloorsFor(parts, feature).Count > 0).ToArray(), parts, new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>(), null, []));
        var context = new TestMapContext(RealMapFixture.Entry().Descriptor, world);
        var layouts = new Dictionary<FeatureId, InteriorLayout>();
        var generation = new BuildContext {
            CodeHash = "fixture", WindowNumbering = new((context.Descriptor).Id, new string('0', 64), []), Map = context.Descriptor, World = world, WorldSeed = 1, InteriorLayouts = layouts,
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            InteriorProviders = new Dictionary<string, IInteriorProvider> { [providerId] = provider } };
        foreach (var feature in provider.FeatureIds) layouts.Add(feature, provider.Layout(generation, feature));
        var layout = layouts.Values.Single(item => item.AtriumVisibleStoreys.Count > 0);
        var atriumFloor = layout.AtriumVisibleStoreys.Min();
        var bake = new InteriorRequests();
        var stream = new InteriorStream(bake, new StreamMountQueue(() => 0), new StreamMaterials(null!, null!), null!, _ => { }, false, new StreamGeometryPreparation());
        stream.Load(context, generation);
        var expected = layout.OpenStoreys.Where(floor => Math.Abs(floor - atriumFloor) <= 1 || layout.AtriumVisibleStoreys.Contains(floor)).Order()
            .Select(floor => new BakeKey(context.Descriptor.Id, "interior." + providerId,
                FormattableString.Invariant($"{layout.Feature.Value}/{floor}/{(floor == atriumFloor ? "IF" : "IS")}"))).ToArray();
        var area = world.Interiors.Get(layout.Feature)!.OpenAreas.First(area => area.Index == atriumFloor);
        var center = Campus.Gen.Geometry.PlanPolygons.Centroid(area.Footprint);
        var destination = new System.Numerics.Vector3(center.X, area.FloorY + WalkRules.StandingEye, center.Y);
        var pins = new HashSet<BakeKey>();
        stream.Pin(new(layout.Feature, checked((short)atriumFloor)), destination, pins);
        if (!pins.SetEquals(expected) || !bake.Keys.SequenceEqual(expected) || stream.ResourcesReady(pins) || !stream.DestinationReady)
            throw new InvalidOperationException("Pinned interior did not use real resource requests independently of the active destination barrier.");
        stream.Prefetch(new(layout.Feature, checked((short)atriumFloor)), destination);
        if (bake.Keys.Distinct().Count() != expected.Length || stream.DestinationReady)
            throw new InvalidOperationException("Atrium-visible open storeys or the pending destination barrier changed.");
        stream.Unload();
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("Slow")]
    public void OutdoorHighPlacesKeepTheirRealSiteCollisionWithoutInteriorShellOrStreamKeys()
    {
        var world = RealMapFixture.Load().World;
        var map = RealMapFixture.Entry().Descriptor;
        var context = new TestMapContext(map, world);
        var generation = new BuildContext { CodeHash = "outdoor-regression", WindowNumbering = new(map.Id, new string('0', 64), []),
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, Map = map, World = world, WorldSeed = map.Seed };
        var south = new Campus.Gen.Maps.Qingshuihe.South.SouthModule();
        var core = new Campus.Gen.Maps.Qingshuihe.Core.CoreModule();
        var sources = south.DescribeOutdoorAccess(generation).Concat(core.DescribeOutdoorAccess(generation)).ToArray();
        var collision = new Campus.Gen.Geometry.SiteColliders(generation);
        foreach (var site in Campus.Gen.Maps.Qingshuihe.South.SouthSites.All.Concat(Campus.Gen.Maps.Qingshuihe.Core.CoreSite.Pieces(world))) site.Collide(collision);
        foreach (var source in sources)
        {
            var area = source.Areas[0];
            var xz = Campus.Gen.Geometry.PlanPolygons.Centroid(area.Footprint);
            var position = new System.Numerics.Vector3(xz.X, area.FloorY + WalkRules.StandingEye, xz.Y);
            var medium = world.Spatial.MediumAt(position);
            if (!world.Interiors.IsOpen(source.Feature, area.Index, position) || medium.Kind != MediumKind.Air ||
                world.Interiors.AreaAt(source.Feature, position)?.Kind != InteriorOpenPartKind.OutdoorHighPlace)
                throw new InvalidOperationException($"Outdoor source lost its access domain: {source.Id}.");
            if (!collision.Items.Any(item => item.Collider is SlabCollider slab && slab.Y == area.FloorY &&
                slab.Foot != Campus.Contracts.Audio.FootMat.Auto && Campus.Gen.Geometry.PlanRings.PointInRing(xz, slab.Ring)))
                throw new InvalidOperationException($"Outdoor source lost its existing site support: {source.Id}.");
            var bake = new InteriorRequests();
            var stream = new InteriorStream(bake, new StreamMountQueue(() => 0), new StreamMaterials(null!, null!), null!, _ => { }, false, new StreamGeometryPreparation());
            stream.Load(context); stream.Select(medium, position);
            if (bake.Keys.Count != 0) throw new InvalidOperationException("Outdoor medium requested an interior shell.");
            stream.Prefetch(new(source.Feature, checked((short)area.Index)), position);
            if (bake.Keys.Count != 0 || stream.Failure is null || stream.DestinationReady)
                throw new InvalidOperationException("An explicit interior space incorrectly authorized an outdoor area.");
            stream.Unload();
        }
        var points = JsonSerializer.Deserialize<WaypointFile>(RealMapFixture.ReadFiles()["waypoints.json"].Span, ContractJson.Options)!.Waypoints;
        foreach (var point in points.Where(point => point.Id is "P17" or "P18"))
            if (world.Spatial.MediumAt(point.Fly.Position).Kind != MediumKind.Air)
                throw new InvalidOperationException($"The unchanged {point.Id} camera is no longer outdoors.");
        using var catalog = JsonDocument.Parse(RealMapFixture.ReadFiles()["interiors.json"]);
        foreach (var item in catalog.RootElement.GetProperty("interiors").EnumerateArray()
            .Where(item => item.GetProperty("openParts").EnumerateArray().Any(part => part.GetProperty("kind").GetString() == "outdoorHighPlace") &&
                item.GetProperty("providerId").GetString()!.StartsWith("interior.box.", StringComparison.Ordinal)))
        {
            var parts = InteriorEntranceJson.ParseOpenParts(item);
            var feature = parts[0].FeatureId;
            var provider = new Campus.Gen.Interior.Shell.Box.BoxInteriorProvider();
            provider.Configure(new([feature], parts, new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>
                { [feature] = world.Interiors.Get(feature)!.Entrances }, null, []));
            var outdoor = sources.Single(source => source.Feature == feature);
            var floor = outdoor.Areas[0].Index;
            if (provider.Layout(generation, feature).OpenStoreys.Contains(floor) ||
                provider.BuildStorey(generation, feature, floor, InteriorDetail.Full).Meshes.Count != 0 ||
                provider.CollisionStorey(generation, feature, floor).Count != 0)
                throw new InvalidOperationException("A high place still acquired box-interior geometry.");
        }
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
            var stream = new InteriorStream(bake, new StreamMountQueue(() => 0), new StreamMaterials(null!, null!), null!, _ => { }, false, new StreamGeometryPreparation());
            stream.Load(context);
            if (knownDestination) stream.Prefetch(new(building, checked((short)medium.Storey)), waypoint.Fly.Position);
            else stream.Select(medium, waypoint.Fly.Position);
            Console.WriteLine($"InteriorRequests waypoint={waypoint.Id} position={waypoint.Fly.Position} medium={medium} " +
                $"selector={(knownDestination ? "Prefetch" : "Select")} receiver=collection-fixture keys=[{string.Join(",", bake.Keys)}]");
            if (!bake.Keys.SequenceEqual(expected) || stream.Failure is not null || knownDestination && stream.DestinationReady)
                throw new InvalidOperationException("Open-floor requests or pending destination readiness changed.");
            stream.Prefetch(new(building, checked((short)(medium.Storey + 1))), waypoint.Fly.Position);
            if (bake.Keys.Count != expected.Length || stream.Failure is null || stream.DestinationReady)
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
        var session = new BakeQueue.MapSession(key.Map);
        var handle = queue.Ensure(session, key, BakePriority.KnownDestination, () =>
        {
            entered.Set(); release.Wait();
            return builder.Build(key, context, module, _ => { });
        });
        try
        {
            if (!entered.Wait(5000) || !ReferenceEquals(handle,
                queue.Ensure(session, key, BakePriority.KnownDestination, () => throw new InvalidOperationException("Duplicate build."))))
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
    public void ActualBakeQueueDrainsARetiringMapWithoutAffectingAnotherMapsResults()
    {
        using var queue = new BakeQueue(1, static () => { });
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var retiringContext = new TestMapContext(BackdropInputsTests.Descriptor() with { Id = new("retiring-fixture-map") });
        var currentContext = new TestMapContext(BackdropInputsTests.Descriptor() with { Id = new("current-fixture-map") });
        var oldKey = new BakeKey(retiringContext.Descriptor.Id, "fixture.root", "L4/0/0");
        var currentKey = new BakeKey(currentContext.Descriptor.Id, "fixture.root", "L4/0/0");
        var oldSession = new BakeQueue.MapSession(retiringContext.Descriptor.Id);
        var old = queue.Ensure(oldSession, oldKey, BakePriority.KnownDestination, () => { entered.Set(); release.Wait(); return new object(); });
        try
        {
            if (!entered.Wait(5000)) throw new InvalidOperationException("Old worker did not start.");
            var firstDrain = queue.ForgetMap(oldSession);
            var secondDrain = queue.ForgetMap(oldSession);
            if (firstDrain.IsCompleted || secondDrain.IsCompleted || old.Done.IsCompleted)
                throw new InvalidOperationException("Forget detached the retiring map's worker before it finished.");
            var currentSession = new BakeQueue.MapSession(currentContext.Descriptor.Id);
            var expected = new object();
            var current = queue.Ensure(currentSession, currentKey, BakePriority.KnownDestination, () => expected);
            release.Set();
            if (!SpinWait.SpinUntil(() => current.State is BakeState.Ready or BakeState.Failed, 5000) ||
                old.State != BakeState.Failed || current.State != BakeState.Ready || !ReferenceEquals(current.Product, expected))
                throw new InvalidOperationException("Retiring one map changed another map's product.");
            Task.WhenAll(firstDrain, secondDrain).GetAwaiter().GetResult();
            var original = new InvalidOperationException("Active build failure.");
            var failed = queue.Ensure(currentSession, currentKey with { Detail = "L3/0/0" }, BakePriority.KnownDestination,
                () => throw original);
            if (!SpinWait.SpinUntil(() => failed.State == BakeState.Failed, 5000) ||
                failed.Product is not null || !ReferenceEquals(failed.Failure, original))
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
    [TestCategory(TestReason.Regression)]
    public void RetargetWaitsForComplementaryFadeAcrossClockWrap()
    {
        var duration = new StreamSettings().FadeSeconds; var visibility = new TileVisibility(duration); var root = StreamTile.Root;
        var start = WorldTime.AnimLoopSeconds - duration * 2; var changing = WorldTime.AnimLoopSeconds - duration / 2;
        var pending = duration / 4; var next = duration; var done = duration * 2;
        var child = new StreamTile(new(0, 0), LodLevel.L3);
        var grandchild = new StreamTile(new(0, 0), LodLevel.L2);
        visibility.Apply(new HashSet<StreamTile> { root }, start, true, (_, _, _) => { });
        if (visibility.IsSettled(start)) throw new InvalidOperationException("Newly entering geometry was certified before its shader fade.");
        visibility.Apply(new HashSet<StreamTile> { root }, start + duration, true, (_, _, _) => { });
        if (!visibility.IsSettled(start + duration)) throw new InvalidOperationException("Fully presented geometry was not ready.");
        visibility.Apply(new HashSet<StreamTile> { child }, changing, true, (_, _, _) => { });
        if (!visibility.Drawn.SetEquals(new HashSet<StreamTile> { root, child }))
            throw new InvalidOperationException("The regression fixture did not start a parent/child blend.");
        visibility.Apply(new HashSet<StreamTile> { grandchild }, pending, true, (_, _, _) => { });
        if (!visibility.Drawn.Contains(root) || visibility.IsSettled(pending)) throw new InvalidOperationException("A partially presented replacement was certified ready.");
        if (visibility.Drawn.Contains(grandchild)) throw new InvalidOperationException("Retargeting interrupted an unfinished complementary pair.");
        visibility.Apply(new HashSet<StreamTile> { grandchild }, next, true, (_, _, _) => { });
        if (visibility.Drawn.Contains(root) || !visibility.Drawn.Contains(child) || !visibility.Drawn.Contains(grandchild) || visibility.IsSettled(next))
            throw new InvalidOperationException("The latest target did not begin its own complete fade.");
        visibility.Apply(new HashSet<StreamTile> { grandchild }, done, true, (_, _, _) => { });
        if (!visibility.Drawn.SetEquals(new HashSet<StreamTile> { grandchild }) || !visibility.IsSettled(done))
            throw new InvalidOperationException("Fade completion left stale coverage or unreadiness.");
        var many = Enumerable.Range(0, TestSamples.PerProperty).Select(index => new StreamTile(new(index, 0), LodLevel.L0)).ToHashSet();
        visibility.Apply(many, duration * 4, true, (_, _, _) => { });
        if (!visibility.Drawn.Contains(grandchild) || visibility.IsSettled(duration * 4))
            throw new InvalidOperationException("A large target change bypassed the complementary fade.");
        visibility.Apply(many, duration * 5, true, (_, _, _) => { });
        if (!visibility.Drawn.SetEquals(many) || !visibility.IsSettled(duration * 5)) throw new InvalidOperationException("A large fade did not settle.");
    }
    private sealed class InteriorRequests : IBakeService, IBakeHandle
    {
        public List<BakeKey> Keys { get; } = [];
        public IBakeHandle Ensure(BakeKey key, BakePriority priority) { Keys.Add(key); return this; }
        public BakeProgress Progress => default;
        public long CacheBytes => 0;
        public BakeState State => BakeState.Pending;
        public object? Product => null;
        public Exception? Failure => null;
        public Task Done => Task.CompletedTask;
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
