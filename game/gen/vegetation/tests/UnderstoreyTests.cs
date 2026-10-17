using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

/// <summary>
/// max-011 23c2b: the empty tree pits and the overlays' design trees are two new tree sources planted after the others; the
/// understorey (pit cover, border shrubs and hedges, grass tussocks) keeps off hard pavement and roads.
/// </summary>
[TestSuite]
public sealed class UnderstoreyTests
{
    /// <summary>
    /// The pit and design trees that were in the map when 23c2b was taken, in planting order, with the first 16 hex digits of
    /// each one's record fingerprint (PavementTests.Fingerprint of that tree alone). Other packages may add or drop pits and
    /// design trees (D-199), so the test checks these by ID and does not pin the whole set.
    /// </summary>
    private static readonly (string Key, string Print)[] KnownNewSources =
    [
        ("design:center-east-pinxue-round-bed", "41e7dd2bb4d6c0e2"),
        ("design:center-east-pinxue-tree-pits:0", "3f09744f930c4cf5"),
        ("design:center-east-pinxue-tree-pits:1", "4d576bef25bdf198"),
        ("design:center-east-pinxue-tree-pits:2", "cf68dd8410e935dc"),
        ("design:center-east-pinxue-tree-pits:3", "dba13a448f2bd8f0"),
        ("design:center-east-pinxue-tree-pits:4", "09cd336330f1478c"),
        ("design:center-east-pinxue-tree-pits:5", "57ca50a48d8fa1cf"),
        ("design:center-east-pinxue-tree-pits:6", "6efd633289ec125b"),
        ("design:center-east-pinxue-tree-pits:7", "a44e68251dbe3540"),
        ("design:center-east-pinxue-tree-pits:8", "c0a9f049087a5e61"),
        ("design:center-east-pinxue-tree-pits:9", "89dc6f90108b6b93"),
        ("design:core-library-camphor-1", "4d3fbc9ef3a849eb"),
        ("design:core-library-camphor-2", "e0259e8ca2f61b06"),
    ];

    private static readonly Lazy<PlantingLayout> Overlaid = new(() => PavementTests.Plant(PavementTests.World(true)));

    [TestCase]
    public void NewSourcesComeLastAndAreStable()
    {
        var layout = Overlaid.Value;
        var all = layout.BeforeGivingWay();
        var first = all.TakeWhile(t => t.Source <= PlantSource.Grove).Count();
        if (all.Skip(first).Any(t => t.Source <= PlantSource.Grove)) throw new InvalidOperationException("a rule tree follows a pit or design tree");
        var added = all.Skip(first).ToList();
        if (layout.Avoided.Any(a => a.Tree.Source > PlantSource.Grove)) throw new InvalidOperationException("a pit or design tree gave way");
        var again = PavementTests.Plant(PavementTests.Load(true)).Trees.Where(t => t.Source > PlantSource.Grove).ToList();
        var print = PavementTests.Fingerprint(added);
        if (PavementTests.Fingerprint(again) != print) throw new InvalidOperationException("the new sources differ between two builds");
        Console.WriteLine($"new sources: {added.Count(t => t.Source == PlantSource.Pit)} pit trees, {added.Count(t => t.Source == PlantSource.Design)} design trees, " +
            $"{layout.PitsHeld} pits already held a tree; fingerprint {print}");
        // Pit trees come before design trees; the known ones keep their bytes and their order (by ID, D-199).
        var firstDesign = added.FindIndex(t => t.Source == PlantSource.Design);
        if (firstDesign >= 0 && added.Skip(firstDesign).Any(t => t.Source == PlantSource.Pit)) throw new InvalidOperationException("a pit tree follows a design tree");
        var at = added.Select((t, i) => (t, i)).ToDictionary(e => e.t.Key, e => e.i, StringComparer.Ordinal);
        var last = -1;
        foreach (var (key, known) in KnownNewSources)
        {
            if (!at.TryGetValue(key, out var index)) continue;
            var now = PavementTests.Fingerprint([added[index]])[..16];
            if (now != known) throw new InvalidOperationException($"{key} changed: {now}, was {known}");
            if (index < last) throw new InvalidOperationException($"{key} moved before an earlier known source");
            last = index;
        }
        if (KnownNewSources.Count(k => at.ContainsKey(k.Key)) < 2) throw new InvalidOperationException("the known pit and design trees are gone; check the fixture");
    }

    [TestCase]
    public void TheTwoLibraryCamphorsArePlanted()
    {
        foreach (var id in new[] { "design:core-library-camphor-1", "design:core-library-camphor-2" })
        {
            var tree = Overlaid.Value.Trees.SingleOrDefault(t => t.Key == id) ?? throw new InvalidOperationException($"{id} is not planted");
            var feature = PavementTests.World(true).Features.Values.Single(f => f.Id.Value == id);
            var at = ((PointGeometry)feature.Geometry).Position;
            if (tree.Source != PlantSource.Design || tree.Species.Id != "camphor" || MathF.Abs(tree.Height - feature.Height!.Value) > 0.01f ||
                MathF.Abs(tree.Position.X - at.X) > 1e-3f || MathF.Abs(tree.Position.Z - at.Z) > 1e-3f)
                throw new InvalidOperationException($"{id}: {tree.Source} {tree.Species.Id} {tree.Height:F2} m at ({tree.Position.X:F2}, {tree.Position.Z:F2})");
        }
    }

    [TestCase]
    public void APitThatHoldsATreeGetsNoOther()
    {
        // On the campus data: a square pit round a planted rule tree, and an empty square pit in a lawn away from every tree.
        var bare = PavementTests.Plant(PavementTests.World(false));
        var held = bare.Trees.First(t => t.Source == PlantSource.Grove);
        var empty = EmptySpot(bare);
        var world = PavementTests.Load(false, ("fixture-pits.d/0000-fixture.json", PavementTests.Overlay(Pit("design:fixture-held-pit", held.Position.X, held.Position.Z),
            Pit("design:fixture-empty-pit", empty.X, empty.Y))));
        var layout = PavementTests.Plant(world);
        var pitTrees = layout.Trees.Where(t => t.Source == PlantSource.Pit).ToList();
        if (layout.PitsHeld != 1 || pitTrees.Count != 1 || pitTrees[0].Key != "design:fixture-empty-pit" ||
            Vector2.Distance(new(pitTrees[0].Position.X, pitTrees[0].Position.Z), empty) > 0.01f)
            throw new InvalidOperationException($"{layout.PitsHeld} pits held, pit trees: {string.Join(", ", pitTrees.Select(t => t.Key))}");
        if (PavementTests.Fingerprint(PavementTests.Original(layout.Trees)) != PavementTests.Fingerprint(PavementTests.Original(bare.Trees)))
            throw new InvalidOperationException("the pits moved a rule tree");
        // The map: the Pinxue Building's ten square pits and its round bed were all empty.
        var map = Overlaid.Value;
        var pinxue = map.Trees.Count(t => t.Source == PlantSource.Pit && t.Key.StartsWith("design:center-east-pinxue-", StringComparison.Ordinal));
        if (pinxue != 11) throw new InvalidOperationException($"{pinxue} trees in the Pinxue pits");
    }

    [TestCase]
    public void UnderstoreyKeepsOffPavementAndRoads()
    {
        var world = PavementTests.World(true);
        var (species, rules) = PavementTests.Data;
        var trees = Overlaid.Value;
        var under = Understorey.Build(world, species, rules, trees, 1u);
        foreach (var kind in Enum.GetValues<UnderKind>())
        {
            if (kind == UnderKind.Tuft)
            {
                // Tussocks are switched off (Understorey.TuftsEnabled): the rule stays, nothing is laid.
                if (Understorey.TuftsEnabled != (under.Count(kind) > 0)) throw new InvalidOperationException($"{under.Count(kind)} tussocks with TuftsEnabled = {Understorey.TuftsEnabled}");
                continue;
            }
            if (under.Count(kind) == 0) throw new InvalidOperationException($"no {kind}");
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
        if (!again.Plants.Select(p => (p.Key, p.Position, p.Size, p.Yaw)).SequenceEqual(under.Plants.Select(p => (p.Key, p.Position, p.Size, p.Yaw))))
            throw new InvalidOperationException("the understorey differs between two builds");
        Console.WriteLine("understorey: " + string.Join(", ", Enum.GetValues<UnderKind>().Select(k => $"{k} {under.Count(k)}")) + "; " +
            string.Join(", ", under.Plants.GroupBy(p => p.Species.Id).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} {g.Count()}")));
    }

    [TestCase]
    public void UnderstoreyModelsStayLight()
    {
        // Shrubs, hedge modules and tussocks are many and small: their mother models are the clipped-ball form at every level.
        var (species, _) = PavementTests.Data;
        var palette = MotherModelTests.Palette;
        foreach (var id in new[] { "photinia", "loropetalum", "boxhedge", "grass" })
        {
            var s = species.Get(id);
            var counts = Enum.GetValues<VegLevel>().Select(level => Enumerable.Range(0, TreeForms.Variants)
                .Max(v => MotherModels.Build(s, v, level, palette).Triangles)).ToArray();
            Console.WriteLine($"{id}: L0+ {counts[0]}, L0 {counts[1]}, L1 {counts[2]} triangles (largest variant)");
            if (!TreeForms.IsBall(s.Form)) throw new InvalidOperationException($"{id} is not a ball form");
        }
    }

    // ---------------------------------------------------------------------------------------------------------

    /// <summary>A lawn spot of the campus data at least 12 m from every planted tree.</summary>
    private static Vector2 EmptySpot(PlantingLayout layout)
    {
        var world = PavementTests.World(false);
        foreach (var green in world.Features.Values.Where(f => f.Kind.Value == "green").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            var box = green.Bounds;
            var p = new Vector2((box.Min.X + box.Max.X) * 0.5f, (box.Min.Z + box.Max.Z) * 0.5f);
            if (green.Geometry is PolygonGeometry polygon && PlanGeometry.Contains(polygon.Rings, p) && PlanGeometry.Contains(world.Boundary, p) &&
                layout.Trees.All(t => Vector2.Distance(new(t.Position.X, t.Position.Z), p) > 12f))
                return p;
        }
        throw new InvalidOperationException("no empty lawn spot");
    }

    private static string Pit(string id, float x, float z)
    {
        string F(float v) => PavementTests.F(v);
        var ring = $"[[{F(x - 0.9f)},{F(z - 0.9f)}],[{F(x + 0.9f)},{F(z - 0.9f)}],[{F(x + 0.9f)},{F(z + 0.9f)}],[{F(x - 0.9f)},{F(z + 0.9f)}]]";
        return "{\"op\":\"add\",\"id\":\"" + id + "\",\"kind\":\"tree-pit\",\"geometry\":{\"type\":\"polygon\",\"rings\":{\"outer\":" + ring +
            ",\"holes\":[]}},\"height\":0.42,\"props\":{\"plant\":\"tree\",\"perPit\":1,\"size\":1.8},\"reason\":\"Fixture: a square tree pit.\"}";
    }
}
