using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using GdUnit4;

namespace Campus.Gen.Buildings.Tests;

using static Campus.Gen.Buildings.Tests.BuildingsTestSupport;

[TestSuite]
public sealed class WindowNumberingTests
{
    [TestCase]
    public void WindowDeclarationsMatchTheGlass()
    {
        var world = World(Feature("t:a", LShape(0, 0), 4, 15.6f, "teaching"));
        var set = BuildingSet.For(world, 2u);
        var plan = set.Plans[0];
        var ctx = Prepared(Ctx(world, 2u));
        var output = new BuildingsModule().Build(ctx, plan.Tile(LodLevel.L0), LodLevel.L0);
        var decls = output.LooseWindows;
        Require(decls.Count == plan.Openings.Sum(o => o.Length), "one declaration per opening");
        Require(decls.Select(d => d.Id).Distinct().Count() == decls.Count, "window numbers are unique");
        var fi = 0;
        for (var r = 0; r < plan.RingCount; r++)
        {
            var skin = plan.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++)
                foreach (var o in plan.Openings[fi])
                {
                    var id = set.Blocks[0].Base + o.Row * plan.TotalColumns + plan.Facades[fi].ColumnStart + o.Bay;
                    var d = decls.Single(x => x.Id == id);
                    var outward = new Vector3(skin.Normal[e].X, 0, skin.Normal[e].Y);
                    Require(Vector3.Distance(Vector3.Cross(d.Right, d.Up), outward) < 1e-4f, "Right × Up points out of the facade");
                    var mid = skin.Points[e] + skin.Dir[e] * ((o.U0 + o.U1) * 0.5f) - skin.Normal[e] * plan.Reveal;
                    Require(Vector3.Distance(d.Center, new Vector3(mid.X, (o.Y0 + o.Y1) * 0.5f, mid.Y)) < 1e-3f, "centred on the opening's glass");
                    Require(MathF.Abs(d.Width - (o.U1 - o.U0)) < 1e-4f && d.BaseId == set.Blocks[0].Base && d.TotalColumns == plan.TotalColumns,
                        "size and numbering as the glass");
                }
        }
        Require(new BuildingsModule().Build(ctx, plan.Tile(LodLevel.L1), LodLevel.L1).LooseWindows.Count == 0, "declared with the L0 cell only");
    }

    [TestCase]
    public void WindowNumbersFollowFeatureOrder()
    {
        var world = World(
            Feature("t:b", Rect(100, 0, 30, 12), 3, 10.5f, "teaching"),
            Feature("t:a", Rect(0, 0, 30, 12), 3, 10.5f, "teaching"));
        var blocks = WindowNumbering.For(world, 1u);
        Require(blocks.Count == 2 && blocks[0].Feature.Value == "t:a" && blocks[0].Base == 0, "ordinal FeatureId order from zero");
        Require(blocks[1].Base == blocks[0].Count, "contiguous allocation");
    }

    // ---------------------------------------------------------------- contract-29: the map-wide numbering provider

    [TestCase]
    public void TheProviderKeepsTheGenericNumbersWhenNothingIsClaimed()
    {
        var world = World(
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"),
            Feature("t:lab", LShape(100, 0), 4, 15.6f, "research"),
            Feature("t:court", Courtyard(0, 100), 3, 12f, "teaching"),
            Feature("t:hall", Rect(200, 0, 40, 30), 1, 9f, "sport"));
        var set = BuildingSet.For(world, 4u);
        var module = new BuildingsModule();
        Require(module is IWindowNumberingProvider and IWindowMetadataSource, "the registered module is the provider");
        var snapshot = module.PrepareWindowNumbering(Ctx(world, 4u), []);
        // The numbers the generator gave before the provider: running sum of columns × storeys in (feature, key) order.
        var next = 0;
        Require(snapshot.Allocations.Count == set.Plans.Count, "one block per generic plan");
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var (plan, a) = (set.Plans[i], snapshot.Allocations[i]);
            Require(a.Feature == plan.Id && a.Key == plan.Key && a.BaseId == next && a.Columns == plan.TotalColumns && a.Rows == plan.Storeys,
                $"{plan.Key}: base {a.BaseId}, expected {next}");
            next += plan.TotalColumns * plan.Storeys;
        }
        Require(snapshot.WindowCount == next && snapshot.Map.Value == "test", "the whole map, nothing else");
        Require(snapshot.Fingerprint.Length == 64 && snapshot.Fingerprint.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c)) &&
            module.PrepareWindowNumbering(Ctx(world, 4u), []).Fingerprint == snapshot.Fingerprint, "stable lowercase SHA-256");
        // Build and the metadata read that table: same glass and the same declarations as the generic blocks.
        var ctx = Ctx(world, 4u, snapshot);
        var declared = new List<WindowDecl>();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var output = module.Build(ctx, set.Plans[i].Tile(LodLevel.L0), LodLevel.L0);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            Require(glass.Length > 0 && glass.All(g => g.Uv2!.Where((_, k) => k % 2 == 0).All(x => x == set.Blocks[i].Base)), $"{set.Plans[i].Key}: glass keeps its base");
            var expected = FacadeOpenings.Declarations(set.Plans[i], set.Blocks[i]);
            Require(output.LooseWindows.Select(d => d.Id).SequenceEqual(expected.Select(d => d.Id)), $"{set.Plans[i].Key}: the same window numbers");
            declared.AddRange(output.LooseWindows);
        }
        Require(module.DescribeWindows(ctx).Windows.Select(d => d.Id).SequenceEqual(declared.Select(d => d.Id)), "metadata lists the L0 declarations");
        Expect<InvalidOperationException>(() => module.Build(Ctx(world, 4u), set.Plans[0].Tile(LodLevel.L0), LodLevel.L0), "no snapshot, no windows");
        var other = new WindowNumberingSnapshot(new MapId("elsewhere"), snapshot.Fingerprint, snapshot.Allocations);
        Expect<InvalidOperationException>(() => module.DescribeWindows(Ctx(world, 4u, other)), "another map's table");
    }

    [TestCase]
    public void AClaimedLayoutReplacesItsWholeFeature()
    {
        // t:a has two building parts; its zone pack claims the facade and numbers it as one 7 × 3 shell, sorted before t:b.
        var parts = new[]
        {
            new WorldFeaturePart("osm:way:1", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), 21f, 6),
            new WorldFeaturePart("osm:way:2", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 12))), 7f, 2),
        };
        var world = World(Feature("t:a", Rect(0, 0, 60, 12), 6, 21f, "teaching") with { Parts = parts },
            Feature("t:b", Rect(200, 0, 30, 12), 3, 10.5f, "teaching"));
        var generic = WindowNumbering.For(world, 1u);
        var module = new BuildingsModule();
        var snapshot = module.PrepareWindowNumbering(Ctx(world, 1u, claims: ["t:a"]), [Layout("t:a", ("shell", 7, 3))]);
        Require(snapshot.Allocations.Count == 2 && snapshot.Get(new("t:a"), "shell") == new WindowAllocation(new("t:a"), "shell", 0, 7, 3),
            "both generic blocks of t:a gave way to the declared shell");
        Expect<KeyNotFoundException>(() => snapshot.Get(new("t:a"), "osm:way:1"), "no generic block left for a claimed part");
        var b = snapshot.Get(new("t:b"), "t:b");
        Require(b.BaseId == 21 && generic[2].Base != 21, $"t:b moves to just behind the shell (21, generic {generic[2].Base})");
        // The enable gate: glass and metadata both decode the moved base, never the generic one.
        var ctx = Ctx(world, 1u, snapshot, ["t:a"]);
        var tileB = BuildingSet.For(world, 1u).Plans.Single(p => p.Id.Value == "t:b").Tile(LodLevel.L0);
        Require(module.Tiles(ctx).SequenceEqual([tileB]), "the claimed feature gets no cell");
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3 })
            foreach (var g in module.Build(ctx, tileB, lod).Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass))
                for (var i = 0; i < g.Position.Length / 3; i++)
                {
                    var id = (int)g.Uv2![2 * i] + (int)MathF.Floor(g.Uv![2 * i + 1]) * b.Columns + (int)MathF.Floor(g.Uv[2 * i]);
                    Require(g.Uv2[2 * i] == b.BaseId && id >= b.BaseId && id < b.BaseId + b.Count, $"{lod}: glass window {id} inside t:b's new block");
                }
        var metadata = module.DescribeWindows(ctx).Windows;
        Require(metadata.Count > 0 && metadata.All(d => d.BaseId == b.BaseId && d.Id >= b.BaseId && d.Id < b.BaseId + b.Count), "metadata on the new block");
        Require(module.Build(ctx, BuildingSet.For(world, 1u).Plans[0].Tile(LodLevel.L0), LodLevel.L0).Meshes.Count == 0, "WP-14 builds nothing of t:a");
    }

    [TestCase]
    public void AnEmptyLayoutDeclaresNoWindowsInAnyOrder()
    {
        var world = World(Feature("t:a", Rect(0, 0, 30, 12), 3, 10.5f, "teaching"), Feature("t:b", Rect(100, 0, 30, 12), 3, 10.5f, "teaching"),
            Feature("t:c", Rect(200, 0, 30, 12), 3, 10.5f, "teaching"));
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a", "t:b"]);
        var snapshot = module.PrepareWindowNumbering(ctx, [Layout("t:b"), Layout("t:a", ("tower", 2, 5), ("main", 4, 3))]);
        Require(snapshot.Allocations.Select(a => (a.Feature.Value, a.Key, a.BaseId)).SequenceEqual([("t:a", "main", 0), ("t:a", "tower", 12), ("t:c", "t:c", 22)]),
            "ordinal (feature, key) order; t:b takes no IDs");
        Expect<KeyNotFoundException>(() => snapshot.Get(new("t:b"), "t:b"), "Grids = [] is no windows, not a fallback");
        var again = module.PrepareWindowNumbering(ctx, [Layout("t:a", ("main", 4, 3), ("tower", 2, 5)), Layout("t:b")]);
        Require(again.Fingerprint == snapshot.Fingerprint && again.Allocations.SequenceEqual(snapshot.Allocations), "declaration order does not matter");
        Require(module.PrepareWindowNumbering(ctx, [Layout("t:a", ("main", 4, 3), ("tower", 2, 5))]).Fingerprint != snapshot.Fingerprint,
            "an omitted empty declaration is a different input");
    }

    [TestCase]
    public void InvalidLayoutsPublishNoTable()
    {
        var parts = new[] { new WorldFeaturePart("osm:way:1", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), 10.5f, 3) };
        var shared = new[] { parts[0], new WorldFeaturePart("osm:way:2", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 12))), 7f, 2) };
        var world = World(Feature("t:a", Rect(0, 0, 30, 12), 3, 10.5f, "teaching") with { Parts = parts },
            Feature("t:b", Rect(0, 0, 60, 12), 3, 10.5f, "teaching") with { Parts = shared });
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a", "t:b"]);
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 1, 1)), Layout("t:a")]), "two layouts for one feature");
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 1, 1), ("x", 2, 2))]), "a key twice");
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 0, 3))]), "an empty grid");
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:zz", ("x", 1, 1))]), "a feature not on the map");
        var roofOnly = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = TestMap, WorldSeed = 1u, ClaimedBy = (_, aspect) => aspect == Aspects.Roof ? new WpId("WP-18") : null };
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(roofOnly, [Layout("t:a")]), "a roof claim grants no window layout");
        // osm:way:1 is numbered by t:a (first in order). t:b may leave it there and number its own grid (the Liren connector
        // beside the generic A/B blocks), but a grid renumbering that footprint would number one facade twice.
        var keep = module.PrepareWindowNumbering(ctx, [Layout("t:b", ("main", 3, 3))]);
        Require(keep.Get(new("t:a"), "osm:way:1").Count == WindowNumbering.For(world, 1u)[0].Count && keep.Get(new("t:b"), "main").Count == 9,
            "the shared footprint keeps its generic block beside the claimed grid");
        Expect<InvalidOperationException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:b", ("osm:way:1", 3, 3))]), "a shared footprint numbered twice");
        Require(module.PrepareWindowNumbering(ctx, [Layout("t:a"), Layout("t:b", ("osm:way:1", 3, 3))]).WindowCount == 9, "once its holder is claimed too");
    }

    [TestCase]
    public void TheTableStopsAtTheOverrideCapacity()
    {
        var world = World(Feature("t:a", Rect(0, 0, 30, 12), 1, 3.5f, "service"), Feature("t:b", Rect(100, 0, 30, 12), 3, 10.5f, "teaching"));
        var generic = WindowNumbering.For(world, 1u)[1].Count;
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a"]);
        var full = module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", WindowNumberingSnapshot.Capacity - generic, 1))]);
        var last = full.Get(new("t:b"), "t:b");
        Require(full.WindowCount == WindowNumberingSnapshot.Capacity && last.IdAt(last.Columns - 1, last.Rows - 1) == 65535, "exactly 65536 IDs fit, the last is 65535");
        Expect<InvalidOperationException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", WindowNumberingSnapshot.Capacity - generic + 1, 1))]),
            "one more is refused, not wrapped or truncated");
        Expect<InvalidOperationException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", int.MaxValue, 2))]), "overflowing sizes are refused");
    }
}
