using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class NodeBenchTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ARoadWithRestNodesHasItsBenchesAtTheNodesNotAlongItsLine()
    {
        // Opus review 16:23: a road with rest nodes has no rest stops along its line; its benches stand at the nodes, on the pedestrian
        // paving there, facing it, under the walk benches' rules (3 m off carriageways, no carriageway 12 m ahead). Judged on the
        // avenue with a node put where one of its own benches stands, so the spot is known to take a bench.
        var world = World.Value;
        var site = Site.Value;
        var baseline = RealLayout.Value;
        var spot = baseline.Items.First(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith($"rest:{Avenue.Key}:", StringComparison.Ordinal)).Anchor;
        var layout = FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, NodeWorld(world, spot), null, Inputs());
        AssertThat(!layout.Items.Any(i => i.Key.StartsWith($"rest:{Avenue.Key}:", StringComparison.Ordinal))).OverrideFailureMessage("rest stops along a road with nodes").IsTrue();
        AssertThat(layout.Items.Count(i => i.Key.StartsWith($"lamp:{Avenue.Key}:", StringComparison.Ordinal)) == baseline.Items.Count(i => i.Key.StartsWith($"lamp:{Avenue.Key}:", StringComparison.Ordinal))).OverrideFailureMessage("the node rule changed the road's lamps").IsTrue();
        var benches = layout.Items.Where(i => i.Key.StartsWith($"node:{Avenue.Key}:0:", StringComparison.Ordinal)).ToList();
        AssertThat(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches).OverrideFailureMessage($"{benches.Count} benches at the node").IsTrue();
        AssertThat(!layout.Diagnostics.Any(d => d.Code == "furniture.node.no-bench" && d.Message.Contains(TestNode, StringComparison.Ordinal))).OverrideFailureMessage("a node with benches logs 'no bench'").IsTrue();
        foreach (var bench in benches)
        {
            var facing = Front(bench.Part.At);
            var p = (FurnitureParams)bench.Part.Params;
            var front = bench.Anchor + facing * (p.Depth * 0.5f);
            AssertThat(Vector2.Distance(bench.Anchor, spot) <= FurnitureStyle.NodeReach + TestTolerance.Layout * 2).OverrideFailureMessage($"{bench.Key} is {Vector2.Distance(bench.Anchor, spot):F1} m from its node").IsTrue();
            AssertThat(site.Paving.PavingEdgeFacing(front + facing * 0.3f) is { Gap: <= 0f }).OverrideFailureMessage($"{bench.Key} does not face pedestrian paving").IsTrue();
            AssertThat(site.OffCarriageways(bench.Anchor, p.Depth * 0.5f, FurnitureStyle.RoadSetback - 0.1f)).OverrideFailureMessage($"{bench.Key} is within {FurnitureStyle.RoadSetback} m of a carriageway").IsTrue();
            AssertThat(!site.CarriagewayAhead(front, facing, FurnitureStyle.FaceClear)).OverrideFailureMessage($"{bench.Key} faces a carriageway within {FurnitureStyle.FaceClear} m").IsTrue();
            AssertThat(bench.Pad is { } bay && MathF.Abs(bay.Top - bench.Part.At.M42) < TestTolerance.Position).OverrideFailureMessage($"{bench.Key} is off its bay").IsTrue();
        }
        AssertThat(benches.Count < 2 || Vector2.Distance(benches[0].Anchor, benches[1].Anchor) >= FurnitureStyle.NodeBenchGap - TestTolerance.Position).OverrideFailureMessage("the node's benches stand closer than the gap").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TheMapsRestNodesTakeBenchesOrSayWhyNot()
    {
        // The road's walks carry no benches (the design walks sit 7.5 m off the road: a bench on either side is too near the carriageway
        // or looks at it); its two nodes take benches under the same rules or report why none fits, never by relaxing them.
        var world = World.Value;
        var layout = RealLayout.Value;
        var group = Groups.Value.FirstOrDefault(g => g.Nodes.Count > 0);
        AssertThat(group is not null).OverrideFailureMessage("no road has rest nodes").IsTrue();
        AssertThat(!layout.Items.Any(i => i.Key.StartsWith($"rest:{group!.Key}:", StringComparison.Ordinal))).OverrideFailureMessage($"benches or bins along {group!.Name}").IsTrue();
        for (var n = 0; n < group.Nodes.Count; n++)
        {
            var node = group.Nodes[n];
            var placed = layout.Items.Count(i => i.Key.StartsWith($"node:{group.Key}:{n}:", StringComparison.Ordinal));
            var says = layout.Diagnostics.Where(d => d.Code == "furniture.node.no-bench" && d.Message.StartsWith($"road {group.Name} node {node.Id.Value} ", StringComparison.Ordinal)).Select(d => d.Message).ToList();
            AssertThat(placed > 0 ? says.Count == 0 : says.Count == 1).OverrideFailureMessage($"node {node.Id.Value}: {placed} benches and {says.Count} 'no bench' lines").IsTrue();
        }
        // Opus review 17:28: a node bench may stand on the plaza, its back to the paving edge and parallel to it, facing into the
        // plaza, wholly on its paving and off the walks' strips, 3 m off carriageways (judged in general on a made-up plaza below).
        var site = Site.Value;
        var standing = layout.Items.Where(i => i.Key.StartsWith($"node:{group.Key}:", StringComparison.Ordinal) && i.Pad is null).ToList();
        var plaza = standing.Where(b => site.Paving.OnPlaza(b.Anchor)).ToList();
        var walk = standing.Except(plaza).ToList();
        CheckPlazaBenches(site, plaza);
        // Opus review 13c2b 21:05: or on a walk 4 m or wider, at its edge, back to it, 2.5 m left to pass.
        CheckWalkEdgeBenches(world, site, walk);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ANodeOnAPlazaSeatsItsBenchesOnItWithTheirBacksToItsEdge()
    {
        // Opus review 17:28, judged on a made-up 12 × 10 m pedestrian square on open ground with a rest node at its middle: the
        // benches stand on the square (no bay on flat ground), backs to its edge, facing in; a square that is not there takes none.
        var open = OpenWorld.Value;
        var spot = OpenSpot;
        Vector2[] ring = [spot + new Vector2(-6f, -5f), spot + new Vector2(6f, -5f), spot + new Vector2(6f, 5f), spot + new Vector2(-6f, 5f)];
        var props = JsonSerializer.SerializeToElement(new { highway = "pedestrian" });
        var square = WorldFeatureFixture.Create("design:furniture-test-square", "road", new PolygonGeometry(new PolygonRings(ring, [])), props.GetRawText());
        IWorldModel world = MadeWorld(NodeWorld(WalkWorld(open, null, square), spot), [], [], SquareTrees(spot));
        var layout = FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, world, null, Inputs());
        var benches = layout.Items.Where(i => i.Key.StartsWith($"node:{OpenAvenue.Key}:0:", StringComparison.Ordinal)).ToList();
        AssertThat(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches && benches.All(b => b.Pad is null)).OverrideFailureMessage($"{benches.Count} benches on the square, or on bays").IsTrue();
        CheckPlazaBenches(new StreetSite(world), benches);
        var bare = FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, NodeWorld(open, spot), null, Inputs());
        AssertThat(!bare.Items.Any(i => i.Key.StartsWith($"node:{OpenAvenue.Key}:0:", StringComparison.Ordinal))).OverrideFailureMessage("benches at a node with no square").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ANodeOnAWideWalkSeatsItsBenchesAtTheWalksEdge()
    {
        // Opus review 13c2b 21:05, judged on made-up walks on open ground with a rest node on them: a 5 m walk seats the node's benches on
        // itself at its edge (backs to the edge, 2.5 m left to pass); a 3.5 m walk takes none on itself (they stand beside it, 13c1).
        var open = OpenWorld.Value;
        var spot = OpenSpot;
        foreach (var (width, takes) in new[] { (5f, true), (3.5f, false) })
        {
            var (line, feature) = DesignWalk(spot, width);
            var world = NodeWorld(WalkWorld(open, line, feature), spot);
            var layout = FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, world, null, Inputs());
            var benches = layout.Items.Where(i => i.Key.StartsWith($"node:{OpenAvenue.Key}:0:", StringComparison.Ordinal)).ToList();
            var onWalk = benches.Where(b => (float)PlanLines.Project(line.Line.Points, b.Anchor).Distance <= Half(line)).ToList();
            if (!takes)
            {
                AssertThat(onWalk.Count == 0).OverrideFailureMessage($"{onWalk.Count} benches on a {width} m walk").IsTrue();
                continue;
            }
            AssertThat(onWalk.Count is >= 1 and <= FurnitureStyle.NodeBenches && onWalk.Count == benches.Count && benches.All(b => b.Pad is null)).OverrideFailureMessage($"{onWalk.Count} of {benches.Count} node benches on the {width} m walk, or on bays").IsTrue();
            CheckWalkEdgeBenches(world, new StreetSite(world), onWalk);
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void APlazaCarryingAWalkLineStillSeatsItsNodeBenches()
    {
        // Opus review 13c2b rework, on the made-up 12 × 10 m square with a 2.5 m footway running through its middle: the walk line does
        // not count against the benches (they stand on the square, backs to its edge, 2.5 m of paving in front), but none sits across it.
        var open = OpenWorld.Value;
        var spot = OpenSpot;
        Vector2[] ring = [spot + new Vector2(-6f, -5f), spot + new Vector2(6f, -5f), spot + new Vector2(6f, 5f), spot + new Vector2(-6f, 5f)];
        var square = WorldFeatureFixture.Create("design:furniture-test-square", "road", new PolygonGeometry(new PolygonRings(ring, [])), JsonSerializer.Serialize(new { highway = "pedestrian" }));
        var walkProps = JsonSerializer.SerializeToElement(new { highway = "footway", width = 2.5f });
        var walk = new NetworkLine(new("design:furniture-test-through"), new Polyline([spot + new Vector2(-14f, 0f), spot + new Vector2(14f, 0f)]), 2.5f) { Props = walkProps };
        IWorldModel world = MadeWorld(NodeWorld(open, spot), [walk], [square], SquareTrees(spot));
        var layout = FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, world, null, Inputs());
        var benches = layout.Items.Where(i => i.Key.StartsWith($"node:{OpenAvenue.Key}:0:", StringComparison.Ordinal)).ToList();
        var site = new StreetSite(world);
        AssertThat(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches && benches.All(b => site.Paving.OnPlaza(b.Anchor))).OverrideFailureMessage($"{benches.Count} benches on the square").IsTrue();
        CheckPlazaBenches(site, benches);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ADesignWalkFiledAsFeatureAndAsNetworkLineHasItsNodeBenchesOnce()
    {
        // game/gen/world/'s effective network files a design walk twice under one ID: as the authored feature and as a network line.
        // The walk is one walk: whichever way it is filed (line only, feature only, both) its node gets the same benches, standing
        // just off its edge and facing it, and a walk that is filed nowhere (hidden) gets none. Judged on a walk made up on open
        // ground, so it holds on a world with or without the effective network.
        var open = OpenWorld.Value;
        var spot = OpenSpot;
        var (line, feature) = DesignWalk(spot);
        FurnitureLayout Layout(NetworkLine? asLine, WorldFeature? asFeature) => FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, NodeWorld(WalkWorld(open, asLine, asFeature), spot), null, Inputs());
        List<FurnitureItem> Benches(FurnitureLayout layout) => layout.Items.Where(i => i.Key.StartsWith($"node:{OpenAvenue.Key}:0:", StringComparison.Ordinal)).ToList();
        var none = Layout(null, null);
        var both = Layout(line, feature);
        var benches = Benches(both);
        AssertThat(Benches(none).Count == 0 && none.Diagnostics.Any(d => d.Code == "furniture.node.no-bench" && d.Message.Contains(TestNode, StringComparison.Ordinal))).OverrideFailureMessage("a node with no walk has benches or no diagnostic message").IsTrue();
        AssertThat(benches.Count is >= 1 and <= FurnitureStyle.NodeBenches).OverrideFailureMessage($"{benches.Count} benches at the node of a walk filed twice").IsTrue();
        AssertThat(benches.Count < 2 || Vector2.Distance(benches[0].Anchor, benches[1].Anchor) >= FurnitureStyle.NodeBenchGap - TestTolerance.Position).OverrideFailureMessage("the walk's node benches stand closer than the gap").IsTrue();
        AssertThat(both.Items.Count == none.Items.Count + benches.Count && !both.Diagnostics.Any(d => d.Code == "furniture.node.no-bench" && d.Message.Contains(TestNode, StringComparison.Ordinal))).OverrideFailureMessage("the walk changed more than its node's benches").IsTrue();
        var site = new StreetSite(WalkWorld(open, line, feature));
        foreach (var bench in benches)
        {
            var facing = Front(bench.Part.At);
            var p = (FurnitureParams)bench.Part.Params;
            var front = bench.Anchor + facing * (p.Depth * 0.5f);
            var edge = (float)PlanLines.Project(line.Line.Points, front).Distance - Half(line);
            AssertThat(edge >= -TestTolerance.Position && edge <= FurnitureStyle.WalkGap + TestTolerance.Layout * 2).OverrideFailureMessage($"{bench.Key}: front {edge:F2} m off the walk's edge").IsTrue();
            AssertThat(site.Paving.PavingEdgeFacing(front + facing * 0.3f) is { Gap: <= 0f, Road: { } walk } && walk.Id == line.Id).OverrideFailureMessage($"{bench.Key} does not face the walk").IsTrue();
        }
        foreach (var (name, filed) in new[] { ("line only", Layout(line, null)), ("feature only", Layout(null, feature)) })
            AssertThat(Benches(filed).Select(b => (b.Key, b.Anchor, b.Part.Params)).SequenceEqual(benches.Select(b => (b.Key, b.Anchor, b.Part.Params)))).OverrideFailureMessage($"the walk filed as {name} has other node benches than filed twice").IsTrue();

    }

    /// <summary>
    /// Walk-edge node benches (Opus review 13c2b 21:05): on a walk at least 4 m wide, the whole footprint on its strip, the back
    /// <see cref="FurnitureStyle.WalkBenchClear"/> inside its edge and facing across it, at least <see cref="FurnitureStyle.NodeWalkPass"/>
    /// of the walk left beyond the sitters' feet; 3 m off carriageways and never facing one within 12 m.
    /// </summary>
    private static void CheckWalkEdgeBenches(IWorldModel world, StreetSite site, IEnumerable<FurnitureItem> benches)
    {
        var walks = world.Network.Roads.Where(r => Highway(r) is "footway" or "path" or "pedestrian").ToList();
        foreach (var bench in benches)
        {
            var p = (FurnitureParams)bench.Part.Params;
            var facing = Front(bench.Part.At);
            var right = new Vector2(-facing.Y, facing.X);
            var walk = walks.Where(w => (float)PlanLines.Project(w.Line.Points, bench.Anchor).Distance <= Half(w)).MinBy(w => (float)PlanLines.Project(w.Line.Points, bench.Anchor).Distance)
                ?? throw new InvalidOperationException($"{bench.Key} does not stand on a walk");
            AssertThat(2f * Half(walk) >= FurnitureStyle.NodeWalkMin).OverrideFailureMessage($"{bench.Key} stands on a walk {2f * Half(walk):F1} m wide").IsTrue();
            foreach (var u in new[] { -p.Width * 0.5f, 0f, p.Width * 0.5f })
                foreach (var v in new[] { -p.Depth * 0.5f, p.Depth * 0.5f + FurnitureStyle.FeetReach })
                    AssertThat((float)PlanLines.Project(walk.Line.Points, bench.Anchor + right * u + facing * v).Distance <= Half(walk) + TestTolerance.Position).OverrideFailureMessage($"{bench.Key} leaves its walk at ({u:F2}, {v:F2})").IsTrue();
            var back = bench.Anchor - facing * (p.Depth * 0.5f);
            AssertThat(MathF.Abs(Half(walk) - (float)PlanLines.Project(walk.Line.Points, back).Distance - FurnitureStyle.WalkBenchClear) < TestTolerance.Layout).OverrideFailureMessage($"{bench.Key} has its back {Half(walk) - (float)PlanLines.Project(walk.Line.Points, back).Distance:F2} m inside the walk's edge").IsTrue();
            var toLine = Vector2.Normalize(PlanLines.Project(walk.Line.Points, back).Point - back);
            AssertThat(Vector2.Dot(toLine, facing) > 0.97f).OverrideFailureMessage($"{bench.Key} does not face across its walk").IsTrue();
            var feet = bench.Anchor + facing * (p.Depth * 0.5f + FurnitureStyle.FeetReach);
            var across = Vector2.Dot(feet - PlanLines.Project(walk.Line.Points, feet).Point, facing);
            AssertThat(Half(walk) - across >= FurnitureStyle.NodeWalkPass - TestTolerance.Position).OverrideFailureMessage($"{bench.Key} leaves {Half(walk) - across:F2} m to pass").IsTrue();
            AssertThat(site.OffCarriageways(bench.Anchor, p.Depth * 0.5f, FurnitureStyle.RoadSetback - 0.1f)).OverrideFailureMessage($"{bench.Key} is within {FurnitureStyle.RoadSetback} m of a carriageway").IsTrue();
            AssertThat(!site.CarriagewayAhead(bench.Anchor + facing * (p.Depth * 0.5f), facing, FurnitureStyle.FaceClear)).OverrideFailureMessage($"{bench.Key} faces a carriageway within {FurnitureStyle.FaceClear} m").IsTrue();
        }
    }

    /// <summary>Plaza benches: on the plaza's own paving with their feet, backs to its edge, 3 m off carriageways, never facing one.</summary>
    private static void CheckPlazaBenches(StreetSite site, IEnumerable<FurnitureItem> plaza)
    {
        foreach (var bench in plaza)
        {
            var facing = Front(bench.Part.At);
            var right = new Vector2(-facing.Y, facing.X);
            var p = (FurnitureParams)bench.Part.Params;
            foreach (var u in new[] { -p.Width * 0.5f, 0f, p.Width * 0.5f })
                foreach (var v in new[] { -p.Depth * 0.5f, p.Depth * 0.5f + FurnitureStyle.FeetReach })
                    AssertThat(site.Paving.OnPlaza(bench.Anchor + right * u + facing * v)).OverrideFailureMessage($"{bench.Key} leaves the plaza at ({u:F2}, {v:F2})").IsTrue();
            var behind = site.Paving.PavingEdgeFacing(bench.Anchor - facing * (p.Depth * 0.5f + FurnitureStyle.PlazaBenchClear + 0.2f));
            AssertThat(behind is { Gap: > 0f } edge && Vector2.Dot(edge.Inward, facing) > 0.95f).OverrideFailureMessage($"{bench.Key} does not have its back to the plaza's edge").IsTrue();
            AssertThat(site.OffCarriageways(bench.Anchor, p.Depth * 0.5f, FurnitureStyle.RoadSetback - 0.1f)).OverrideFailureMessage($"{bench.Key} is within {FurnitureStyle.RoadSetback} m of a carriageway").IsTrue();
            AssertThat(!site.CarriagewayAhead(bench.Anchor + facing * (p.Depth * 0.5f), facing, FurnitureStyle.FaceClear)).OverrideFailureMessage($"{bench.Key} faces a carriageway within {FurnitureStyle.FaceClear} m").IsTrue();
            // Opus review 13c2b rework: at least 2.5 m of paving in front of the sitters' feet, and never across a walk line.
            foreach (var u in new[] { -p.Width * 0.5f, 0f, p.Width * 0.5f })
            {
                var ahead = bench.Anchor + right * u + facing * (p.Depth * 0.5f + FurnitureStyle.FeetReach + FurnitureStyle.PlazaPass);
                AssertThat(site.Paving.OnPavedGround(ahead)).OverrideFailureMessage($"{bench.Key} has less than {FurnitureStyle.PlazaPass} m of paving in front at {u:F2}").IsTrue();
            }
            AssertThat(!site.NearWalkLine([bench.Anchor], p.Depth * 0.5f)).OverrideFailureMessage($"{bench.Key} sits across a walk line").IsTrue();
        }
    }

    private const string TestNode = "design:furniture-test-node";

    /// <summary><paramref name="inner"/> with one rest node at <paramref name="spot"/>, naming the same planting-rule avenue in real and local fixtures.</summary>
    private static FakeWorld NodeWorld(IWorldModel inner, Vector2 spot) =>
        MadeWorld(inner, [], [WorldFeatureFixture.Create(TestNode, FurnitureStyle.RestNodeKind, new PointGeometry(new(spot.X, 0f, spot.Y)), JsonSerializer.Serialize(new { road = OpenAvenue.Name }))]);

    /// <summary>A 16 m east–west design walk centred on <paramref name="spot"/>, as the network line and as the feature of one ID.</summary>
    private static (NetworkLine Line, WorldFeature Feature) DesignWalk(Vector2 spot, float width = 2.5f)
    {
        var id = new FeatureId("design:furniture-test-walk");
        var points = new Polyline([spot + new Vector2(-8f, 0f), spot + new Vector2(8f, 0f)]);
        var props = JsonSerializer.SerializeToElement(new { highway = "footway", width });
        return (new NetworkLine(id, points, width) { Props = props }, WorldFeatureFixture.Create(id.Value, "road", new PolylineGeometry(points), props.GetRawText()));
    }

    /// <summary>The world with a walk filed the way the effective network files a design walk: as a network line, as a feature, or both (null leaves that stack alone).</summary>
    private static FakeWorld WalkWorld(IWorldModel inner, NetworkLine? line, WorldFeature? feature) =>
        MadeWorld(inner, line is null ? Array.Empty<NetworkLine>() : [line], feature is null ? Array.Empty<WorldFeature>() : [feature]);

    /// <summary>Trees 1.5 m outside the made-up 12 × 10 m square's edges (a plaza bench's back stands against planting, Opus review 13c2c).</summary>
    private static IReadOnlyList<NetworkTree> SquareTrees(Vector2 spot)
    {
        var list = new List<NetworkTree>();
        var n = 0;
        for (var x = -6f; x <= 6.01f; x += 3f)
            foreach (var z in new[] { -6.5f, 6.5f }) list.Add(new NetworkTree(new($"design:furniture-test-tree-{n++}"), new Vector3(spot.X + x, 0f, spot.Y + z)));
        for (var z = -3f; z <= 3.01f; z += 3f)
            foreach (var x in new[] { -7.5f, 7.5f }) list.Add(new NetworkTree(new($"design:furniture-test-tree-{n++}"), new Vector3(spot.X + x, 0f, spot.Y + z)));
        return list;
    }
}
