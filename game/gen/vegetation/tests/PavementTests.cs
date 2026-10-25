using Campus.Gen.Geometry;
using static Campus.Gen.Vegetation.Tests.VegetationTestSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Tests;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

/// <summary>
/// Hard pavement of the effective world takes no tree except in a tree pit, and only the trees on it give way: every other
/// tree keeps its key, look and place. Algorithm cases use an exclusive map fixture; production records are checked
/// by pavement semantics and repeatability, without fixing other content packages' outputs.
/// </summary>
[TestSuite]
public sealed class PavementTests
{
    private static readonly Lazy<PlantingLayout> WorldVerified = new(() => Verify(VegetationTestSupport.World(), null, "authored world", planted: WorldLayout.Value));

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void NoTreeStandsOnHardPavementAndOnlyThoseTreesGiveWay()
    {
        var layout = WorldVerified.Value;
        Console.WriteLine($"pavement: {layout.Avoided.Count} of {layout.BeforeGivingWay().Count} trees give way, {layout.Trees.Count} stay; "
            + string.Join(", ", layout.Avoided.GroupBy(a => a.Tree.Source).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")));

    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TheAuthoredWalksAndPlazasAreRead()
    {
        // The exclusive fixture supplies nonempty walk, plaza, pit and avoidance cases; production owners may remove theirs.
        var fixture = Fixture(Road("design:fixture-walk", "footway", 2.5f, [new(-30f, -25f), new(-30f, -15f)]),
            """
            {"id":"design:fixture-plaza","kind":"road",
             "geometry":{"type":"polygon","rings":{"outer":[[-40,40],[-3,40],[-3,50],[-40,50]],"holes":[]}},
             "props":{"highway":"pedestrian"},"note":"Fixture: a pedestrian plaza."}
            """,
            """
            {"id":"design:fixture-pit","kind":"tree-pit",
             "geometry":{"type":"point","position":[-70,0,-70]},
             "props":{"plant":"tree","perPit":1,"radius":1},"note":"Fixture: a tree pit."}
            """);
        var fixtureCensus = Census.Of(fixture);
        if (!fixtureCensus.Paving.Any(p => p.Id == "design:fixture-walk" && p.Line is not null) ||
            !fixtureCensus.Paving.Any(p => p.Id == "design:fixture-plaza" && p.Rings is not null) || fixtureCensus.Pits.Count == 0 ||
            Verify(fixture, null, "walk/plaza/pit fixture", fixture: true).Avoided.Count == 0)
            throw new InvalidOperationException("the fixture did not exercise walk, plaza, pit and pavement avoidance");
        foreach (var (world, layout) in new[] { (fixture, PlantFixture(fixture)), (VegetationTestSupport.World(), WorldLayout.Value) })
        {
            var census = Census.Of(world);
            foreach (var t in layout.Trees)
            {
                var p = new Vector2(t.Position.X, t.Position.Z);
                if (census.InPit(p)) continue;
                foreach (var pave in census.Paving.Where(pv => pv.Id.StartsWith("design:", StringComparison.Ordinal)))
                    if (census.Within(pave, p, 0f))
                        throw new InvalidOperationException($"{t.Key} stands on {pave.Id}");
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ADesignWalkAcrossTheAvenueTakesTheTreesOnIt()
    {
        var bare = FixtureLayout.Value;
        var tree = bare.Trees.First(t => t.Source == PlantSource.Road);
        var (x, z) = (tree.Position.X, tree.Position.Z);
        var world = Fixture(Road("design:fixture-avenue-cross-walk", "footway", 3f, [new(x - 5f, z - 5f), new(x + 5f, z + 5f)]));
        var layout = Verify(world, bare, "avenue walk", fixture: true);
        if (layout.Avoided.All(a => a.Tree.Key != tree.Key) || layout.Trees.Any(t => t.Key == tree.Key))
            throw new InvalidOperationException($"{tree.Key} stands on the walk and did not give way");
        var more = layout.Avoided.Count - bare.Avoided.Count;
        if (more is < 1 or > 6) throw new InvalidOperationException($"{more} more trees gave way for a 14 m walk");
        Console.WriteLine($"pavement (avenue walk): {more} more trees give way, {string.Join(", ", layout.Avoided.Select(a => a.Tree.Key).Except(bare.Avoided.Select(a => a.Tree.Key)))}");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ADesignWalkFiledAsFeatureAndAsNetworkLineIsPavedOnce()
    {
        // game/gen/world/'s effective network files a design walk twice under one ID: as the authored feature and as a network line.
        // The walk is one walk: paved once, from the line, so the trees on it give way (they still count as planted, so the planting
        // order is that of the unmodified fixture) instead of vanishing as they would from a road; filed as the feature alone it takes
        // the same trees. A network line alone is an ordinary road and keeps the trunks off all the same.
        var bare = FixtureLayout.Value;
        var tree = bare.Trees.First(t => t.Source == PlantSource.Road);
        var (x, z) = (tree.Position.X, tree.Position.Z);
        const string id = "design:fixture-twice-filed-walk";
        var world = Fixture(Road(id, "footway", 3f, [new(x - 5f, z - 5f), new(x + 5f, z + 5f)]));
        RequireFiled(world, id, line: true, feature: true);
        var both = Verify(world, bare, "filed twice", fixture: true);
        if (both.Avoided.Count(a => a.Tree.Key == tree.Key) != 1 || both.Trees.Any(t => t.Key == tree.Key))
            throw new InvalidOperationException($"{tree.Key} stands on the walk and did not give way exactly once");
        var featureOnly = Filed(world, road => road.Id.Value != id, _ => true);
        RequireFiled(featureOnly, id, line: false, feature: true);
        var asFeature = Verify(featureOnly, bare, "filed as the feature", fixture: true);
        if (!SameLayout(both, asFeature)) throw new InvalidOperationException("the walk filed as the feature alone takes other trees than filed twice");
        var lineOnly = Filed(world, _ => true, feature => feature.Id.Value != id);
        RequireFiled(lineOnly, id, line: true, feature: false);
        var asLine = PlantFixture(lineOnly);
        var walk = lineOnly.Network.Roads.Single(r => r.Id.Value == id);
        foreach (var t in asLine.Trees)
            if (Off(walk, new(t.Position.X, t.Position.Z)) < walk.Width * 0.5f + Planting.TrunkClearance)
                throw new InvalidOperationException($"{t.Key} stands on the walk filed as a network line");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TheTreePitsOfTheMapProtectOnlyTheTreesInsideThem()
    {
        // Production pits may be added or removed by their owners; check each currently protected original tree.
        var withPits = WorldVerified.Value;
        var noPits = Verify(Filed(VegetationTestSupport.World(), _ => true, f => f.Kind.Value != Planting.TreePitKind), withPits, "without pits");
        var standing = withPits.Trees.Select(t => t.Key).ToHashSet(StringComparer.Ordinal);
        var kept = noPits.Avoided.Where(a => standing.Contains(a.Tree.Key)).Select(a => a.Tree).ToList();
        var census = Census.Of(VegetationTestSupport.World());
        if (kept.Any(t => !census.InPit(new(t.Position.X, t.Position.Z)))) throw new InvalidOperationException("a tree outside the pits was protected");
        if (withPits.Avoided.Count != noPits.Avoided.Count - kept.Count) throw new InvalidOperationException("the pits changed which other trees give way");
        Console.WriteLine($"pavement (pits): {noPits.Avoided.Count} trees give way without the pits, {withPits.Avoided.Count} with them; the pits keep {kept.Count} " +
            string.Join(", ", kept.GroupBy(t => $"{t.Source}/{t.Species.Id}").OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} {g.Count()}")));
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ATreePitLetsATreeStandOnPavement()
    {
        // Two trees that give way on the fixture plaza: one gets a square pit, the other a round bed; both stay, nothing else moves.
        var bare = FixtureLayout.Value;
        var square = bare.Avoided.First(a => a.Tree.Source == PlantSource.Road).Tree;
        var round = bare.Avoided.First(a => a.Tree.Source == PlantSource.OsmTree).Tree;
        var (sx, sz) = (square.Position.X, square.Position.Z);
        var ring = $"[[{F(sx - 0.9f)},{F(sz - 0.9f)}],[{F(sx + 0.9f)},{F(sz - 0.9f)}],[{F(sx + 0.9f)},{F(sz + 0.9f)}],[{F(sx - 0.9f)},{F(sz + 0.9f)}]]";
        var world = Fixture(
            "{\"id\":\"design:fixture-square-pit\",\"kind\":\"tree-pit\",\"geometry\":{\"type\":\"polygon\",\"rings\":{\"outer\":" + ring +
            ",\"holes\":[]}},\"height\":0.42,\"props\":{\"plant\":\"tree\",\"perPit\":1,\"size\":1.8},\"note\":\"Fixture: a square tree pit.\"}",
            "{\"id\":\"design:fixture-round-bed\",\"kind\":\"tree-pit\",\"geometry\":{\"type\":\"point\",\"position\":[" + F(round.Position.X) + ",0," + F(round.Position.Z) +
            "]},\"height\":0.45,\"props\":{\"plant\":\"tree\",\"perPit\":1,\"radius\":1.0},\"note\":\"Fixture: a round tree bed.\"}");
        var layout = Verify(world, bare, "tree pits", fixture: true);
        foreach (var tree in new[] { square, round })
            if (layout.Trees.All(t => t.Key != tree.Key) || layout.Avoided.Any(a => a.Tree.Key == tree.Key))
                throw new InvalidOperationException($"{tree.Key} stands in a pit and gave way");
        var inPits = bare.Avoided.Count(a => (MathF.Abs(a.Tree.Position.X - sx) <= 0.9f && MathF.Abs(a.Tree.Position.Z - sz) <= 0.9f) ||
            Vector2.Distance(new(a.Tree.Position.X, a.Tree.Position.Z), new(round.Position.X, round.Position.Z)) <= 1f);
        if (inPits < 2 || layout.Avoided.Count != bare.Avoided.Count - inPits)
            throw new InvalidOperationException($"{layout.Avoided.Count} trees give way, {bare.Avoided.Count} did before, {inPits} stand in the pits");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void APitNamingItsSpeciesGetsOneTreeOfIt()
    {
        // game/gen/maps/qingshuihe/public/ 29a's pocket-garden pits (world/public.json): round beds at a point with radius, perPit 1, props.species,
        // stone = site (game/gen/maps/qingshuihe/public/ draws the frame). Exclusive fixture geometry keeps this rule independent of the authored 29a pits.
        static string Pit(string id, float x, float z, string species, float radius) =>
            "{\"id\":\"" + id + "\",\"kind\":\"tree-pit\",\"geometry\":{\"type\":\"point\",\"position\":[" + F(x) + ",0," + F(z) +
            "]},\"height\":0.2,\"props\":{\"plant\":\"tree\",\"perPit\":1,\"species\":\"" + species + "\",\"radius\":" + F(radius) +
            ",\"kerb\":0.16,\"soilAbove\":0.15,\"stone\":\"site\"},\"note\":\"Fixture: a 29a pocket-garden pit.\"}";
        var pits = new[] { ("design:fixture-29a-dangui-tree", -70f, -70f, "maple", 0.85f), ("design:fixture-29a-canteen-tree", -70f, -40f, "camphor", 1.0f) };
        var world = Fixture(pits.Select(t => Pit(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5)).ToArray());
        var layout = PlantFixture(world);
        foreach (var (id, x, z, species, radius) in pits)
        {
            var planted = layout.Trees.Where(t => Vector2.Distance(new(t.Position.X, t.Position.Z), new(x, z)) <= radius).ToList();
            if (planted.Count != 1 || planted[0].Key != id || planted[0].Species.Id != species || planted[0].Source != PlantSource.Pit)
                throw new InvalidOperationException($"{id}: {string.Join(", ", planted.Select(t => $"{t.Key} {t.Species.Id} {t.Source}"))}; one {species} expected");
        }
    }

    // ---------------------------------------------------------------------------------------------------------

    /// <summary>The walk is filed (or not) as a network line and as a feature of its ID in this world: the precondition of a case.</summary>
    private static void RequireFiled(IWorldModel world, string id, bool line, bool feature)
    {
        if (world.Network.Roads.Any(r => r.Id.Value == id) != line || world.Features.ContainsKey(new FeatureId(id)) != feature)
            throw new InvalidOperationException($"{id} is not filed as expected: network line {line}, feature {feature}");
    }

    private static bool SameLayout(PlantingLayout a, PlantingLayout b) =>
        SameRecords(a.Trees, b.Trees) &&
        a.Avoided.Select(t => t.Index).SequenceEqual(b.Avoided.Select(t => t.Index)) &&
        SameRecords(a.Avoided.Select(a => a.Tree), b.Avoided.Select(a => a.Tree)) && a.PitsHeld == b.PitsHeld;

    private static float Off(NetworkLine line, Vector2 p) => (float)PlanLines.Project(line.Line.Points, p).Distance;

    /// <summary>A world with some of its network roads and features left out, to file a walk one way only.</summary>
    private static FakeWorld Filed(IWorldModel inner, Func<NetworkLine, bool> keepRoad, Func<WorldFeature, bool> keepFeature) => new(inner)
    {
        Features = inner.Features.Where(f => keepFeature(f.Value)).ToDictionary(f => f.Key, f => f.Value),
        Network = inner.Network with { Roads = inner.Network.Roads.Where(keepRoad).ToList() },
    };

    /// <summary>
    /// Builds the layout and checks it against the world by an independent census: the trees before pavement gave way are the
    /// same records and order as the supplied baseline when present. Exactly the trees on pavement and outside every pit
    /// give way; repeated builds keep every record, key and order.
    /// </summary>
    private static PlantingLayout Verify(IWorldModel world, PlantingLayout? baseline, string label, bool fixture = false, PlantingLayout? planted = null)
    {
        var layout = planted ?? (fixture ? PlantFixture(world) : Plant(world));
        // New pit and design sources come last; adding pavement or pits must preserve every earlier record and its order.
        var before = Original(layout.BeforeGivingWay());
        if (baseline is not null && !SameRecords(before, Original(baseline.BeforeGivingWay())))
            throw new InvalidOperationException($"{label}: an original tree record or its order changed");
        if (layout.BeforeGivingWay().Select(t => t.Key).Distinct(StringComparer.Ordinal).Count() != layout.BeforeGivingWay().Count)
            throw new InvalidOperationException($"{label}: duplicate tree keys");
        if (!SameLayout(layout, fixture ? PlantFixture(world) : Plant(world)))
            throw new InvalidOperationException($"{label}: repeated planting changed records or order");
        var census = Census.Of(world);
        var expected = before.Select((t, i) => (Index: i, Tree: t)).Where(e => census.GivesWay(new(e.Tree.Position.X, e.Tree.Position.Z))).ToList();
        if (!expected.Select(e => (e.Index, e.Tree.Key)).SequenceEqual(layout.Avoided.Select(a => (a.Index, a.Tree.Key))))
            throw new InvalidOperationException($"{label}: {layout.Avoided.Count} trees gave way, the census says {expected.Count}");
        foreach (var t in Original(layout.Trees))
        {
            var p = new Vector2(t.Position.X, t.Position.Z);
            if (census.Paving.Any(pave => census.Within(pave, p, 0f)) && !census.InPit(p)) throw new InvalidOperationException($"{label}: {t.Key} stands on pavement");
        }
        return layout;
    }

    private static string Road(string id, string type, float width, IEnumerable<Vector2> points) =>
        Road(id, type, width, "[" + string.Join(',', points.Select(p => $"[{F(p.X)},{F(p.Y)}]")) + "]");

    private static string Road(string id, string type, float width, string points) =>
        "{\"id\":\"" + id + "\",\"kind\":\"road\",\"geometry\":{\"type\":\"polyline\",\"line\":{\"points\":" + points +
        "}},\"props\":{\"highway\":\"" + type + "\",\"width\":" + F(width) + "},\"note\":\"Fixture: a design walk.\"}";


}
