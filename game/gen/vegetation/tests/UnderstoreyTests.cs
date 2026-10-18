using Campus.Gen.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Vegetation.Tests;

/// <summary>
/// max-011 23c2b: the empty tree pits and the overlays' design trees are two new tree sources planted after the others; the
/// understorey (pit cover, border shrubs and hedges, grass tussocks) keeps off hard pavement and roads.
/// </summary>
[TestSuite]
public sealed class UnderstoreyTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void NewSourcesComeLastAndAreStable()
    {
        var layout = PavementTests.OverlaidLayout.Value;
        var all = layout.BeforeGivingWay();
        var first = all.TakeWhile(t => t.Source <= PlantSource.Grove).Count();
        if (all.Skip(first).Any(t => t.Source <= PlantSource.Grove)) throw new InvalidOperationException("a rule tree follows a pit or design tree");
        var added = all.Skip(first).ToList();
        if (layout.Avoided.Any(a => a.Tree.Source > PlantSource.Grove)) throw new InvalidOperationException("a pit or design tree gave way");
        var again = PavementTests.Plant(PavementTests.World(true)).Trees.Where(t => t.Source > PlantSource.Grove).ToList();
        if (!PavementTests.SameRecords(again, added)) throw new InvalidOperationException("the new sources differ between two builds");
        // Pit trees come before design trees; repeatability above also checks their complete order.
        var firstDesign = added.FindIndex(t => t.Source == PlantSource.Design);
        if (firstDesign >= 0 && added.Skip(firstDesign).Any(t => t.Source == PlantSource.Pit)) throw new InvalidOperationException("a pit tree follows a design tree");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AddingSourcesPreservesExistingRecordsAndOrder()
    {
        var original = PavementTests.FixtureLayout.Value;
        var first = new[] { Pit("design:fixture-held-pit", -70f, -20f), DesignTree("design:fixture-tree", -70f, 20f) };
        var before = PavementTests.PlantFixture(PavementTests.Fixture(first));
        var after = PavementTests.PlantFixture(PavementTests.Fixture(first.Concat(
            [Pit("design:fixture-added-pit", -70f, -50f), DesignTree("design:fixture-added-tree", -70f, 50f)]).ToArray()));
        foreach (var layout in new[] { before, after })
        {
            if (!PavementTests.SameRecords(PavementTests.Original(layout.BeforeGivingWay()), original.BeforeGivingWay()))
                throw new InvalidOperationException("a new source moved or reordered an original tree");
            var all = layout.BeforeGivingWay();
            var newSources = all.SkipWhile(t => t.Source <= PlantSource.Grove).Select(t => t.Source).ToList();
            if (newSources.Any(s => s <= PlantSource.Grove) || !newSources.SequenceEqual(newSources.Order()))
                throw new InvalidOperationException("tree sources are out of order");
            if (layout.Avoided.Any(a => a.Tree.Source > PlantSource.Grove)) throw new InvalidOperationException("a new source gave way");
        }
        var existing = before.Trees.Select(t => t.Key).ToHashSet(StringComparer.Ordinal);
        if (before.Count(PlantSource.Pit) != 1 || before.Count(PlantSource.Design) != 1 || after.Trees.Count != before.Trees.Count + 2 ||
            !PavementTests.SameRecords(before.Trees, after.Trees.Where(t => existing.Contains(t.Key))))
            throw new InvalidOperationException("adding pit and design trees changed existing records or their order");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TheTwoLibraryCamphorsArePlanted()
    {
        foreach (var id in new[] { "design:core-library-camphor-1", "design:core-library-camphor-2" })
        {
            var tree = PavementTests.OverlaidLayout.Value.Trees.SingleOrDefault(t => t.Key == id) ?? throw new InvalidOperationException($"{id} is not planted");
            var feature = PavementTests.World(true).Features.Values.Single(f => f.Id.Value == id);
            var at = ((PointGeometry)feature.Geometry).Position;
            if (tree.Source != PlantSource.Design || tree.Species.Id != "camphor" || MathF.Abs(tree.Height - feature.Height!.Value) > TestTolerance.Position ||
                MathF.Abs(tree.Position.X - at.X) > TestTolerance.Position || MathF.Abs(tree.Position.Z - at.Z) > TestTolerance.Position)
                throw new InvalidOperationException($"{id}: {tree.Source} {tree.Species.Id} {tree.Height:F2} m at ({tree.Position.X:F2}, {tree.Position.Z:F2})");
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void APitThatHoldsATreeGetsNoOther()
    {
        // Exclusive fixture: one occupied square pit and one empty pit well away from every original tree.
        var bare = PavementTests.FixtureLayout.Value;
        var held = bare.Trees.First(t => t.Source == PlantSource.Grove);
        var empty = new Vector2(-70f, -70f);
        var world = PavementTests.Fixture(Pit("design:fixture-held-pit", held.Position.X, held.Position.Z),
            Pit("design:fixture-empty-pit", empty.X, empty.Y));
        var layout = PavementTests.PlantFixture(world);
        var pitTrees = layout.Trees.Where(t => t.Source == PlantSource.Pit).ToList();
        if (layout.PitsHeld != 1 || pitTrees.Count != 1 || pitTrees[0].Key != "design:fixture-empty-pit" ||
            Vector2.Distance(new(pitTrees[0].Position.X, pitTrees[0].Position.Z), empty) > TestTolerance.Position)
            throw new InvalidOperationException($"{layout.PitsHeld} pits held, pit trees: {string.Join(", ", pitTrees.Select(t => t.Key))}");
        if (!PavementTests.SameRecords(PavementTests.Original(layout.Trees), PavementTests.Original(bare.Trees)))
            throw new InvalidOperationException("the pits moved a rule tree");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void UnderstoreyKeepsOffPavementAndRoads()
    {
        var world = PavementTests.Fixture(Pit("design:fixture-understorey-pit", -70f, -70f));
        var species = PavementTests.Data.Species;
        var rules = PavementTests.FixtureRules;
        var trees = PavementTests.PlantFixture(world);
        CheckUnderstorey(world, species, rules, trees, requireKinds: true);
        CheckUnderstorey(PavementTests.World(true), species, PavementTests.Data.Rules, PavementTests.OverlaidLayout.Value, requireKinds: false);
    }

    private static void CheckUnderstorey(IWorldModel world, SpeciesTable species, PlantingRules rules, PlantingLayout trees, bool requireKinds)
    {
        var under = Understorey.Build(world, species, rules, trees, 1u);
        foreach (var kind in Enum.GetValues<UnderKind>())
        {
            if (kind == UnderKind.Tuft)
            {
                // Disabled tussocks are absent everywhere; only the fixture requires a nonempty enabled case.
                if ((!Understorey.TuftsEnabled && under.Count(kind) != 0) || (requireKinds && Understorey.TuftsEnabled && under.Count(kind) == 0))
                    throw new InvalidOperationException($"{under.Count(kind)} tussocks with TuftsEnabled = {Understorey.TuftsEnabled}");
                continue;
            }
            if (requireKinds && under.Count(kind) == 0) throw new InvalidOperationException($"no {kind} in the exclusive fixture");
        }
        var census = PavementTests.Census.Of(world);
        foreach (var p in under.Plants)
        {
            var at = new Vector2(p.Position.X, p.Position.Z);
            if (p.Kind == UnderKind.PitCover)
            {
                if (!census.InPit(at)) throw new InvalidOperationException($"{p.Key} is outside its pit");
                continue;
            }
            if (census.InPit(at) || census.Paving.Any(pave => census.Within(pave, at, 0f))) throw new InvalidOperationException($"{p.Key} stands on pavement");
            foreach (var road in world.Network.Roads)
            {
                var r = road.Width * 0.5f;
                for (var i = 1; i < road.Line.Points.Count; i++)
                    if (PlanGeometry.SegmentDistanceSquared(at, road.Line.Points[i - 1], road.Line.Points[i]) < r * r)
                        throw new InvalidOperationException($"{p.Key} stands on {road.Id.Value}");
            }
        }
        var again = Understorey.Build(world, species, rules, trees, 1u);
        if (!again.Plants.SequenceEqual(under.Plants))
            throw new InvalidOperationException("the understorey differs between two builds");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void UnderstoreyModelsAreClippedBalls()
    {
        // Shrubs, hedge modules and tussocks are many and small: their mother models are the clipped-ball form.
        var (species, _) = PavementTests.Data;
        foreach (var id in new[] { "photinia", "loropetalum", "boxhedge", "grass" })
            if (!TreeForms.IsBall(species.Get(id).Form)) throw new InvalidOperationException($"{id} is not a ball form");
    }

    // ---------------------------------------------------------------------------------------------------------

    private static string DesignTree(string id, float x, float z) =>
        "{\"op\":\"add\",\"id\":\"" + id + "\",\"kind\":\"tree\",\"geometry\":{\"type\":\"point\",\"position\":[" + PavementTests.F(x) + ",0," + PavementTests.F(z) +
        "]},\"height\":8,\"props\":{\"planter\":\"WP-23\",\"species\":\"camphor\"},\"reason\":\"Fixture: an explicitly planted tree.\"}";

    private static string Pit(string id, float x, float z)
    {
        string F(float v) => PavementTests.F(v);
        var ring = $"[[{F(x - 0.9f)},{F(z - 0.9f)}],[{F(x + 0.9f)},{F(z - 0.9f)}],[{F(x + 0.9f)},{F(z + 0.9f)}],[{F(x - 0.9f)},{F(z + 0.9f)}]]";
        return "{\"op\":\"add\",\"id\":\"" + id + "\",\"kind\":\"tree-pit\",\"geometry\":{\"type\":\"polygon\",\"rings\":{\"outer\":" + ring +
            ",\"holes\":[]}},\"height\":0.42,\"props\":{\"plant\":\"tree\",\"perPit\":1,\"size\":1.8},\"reason\":\"Fixture: a square tree pit.\"}";
    }
}
