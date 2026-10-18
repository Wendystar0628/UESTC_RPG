using Campus.Tests;
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

using static GdUnit4.Assertions;

[TestSuite]
public sealed class WindowNumberingTests
{
    [TestCase]
    public void WindowDeclarationsMatchTheGlass()
    {
        var world = World(WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(LShape(0, 0))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 15.6f, storeys: 4));
        var set = BuildingSet.For(world, 2u);
        var plan = set.Plans[0];
        var ctx = Prepared(Ctx(world, 2u));
        var output = new BuildingsModule().Build(ctx, plan.Tile(LodLevel.L0), LodLevel.L0);
        var decls = output.LooseWindows;
        AssertThat(decls.Count == plan.Openings.Sum(o => o.Length)).OverrideFailureMessage("Expected: " + ("one declaration per opening")).IsTrue();
        AssertThat(decls.Select(d => d.Id).Distinct().Count() == decls.Count).OverrideFailureMessage("Expected: " + ("window numbers are unique")).IsTrue();
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
                    AssertThat(Vector3.Distance(Vector3.Cross(d.Right, d.Up), outward) < 0.01f).OverrideFailureMessage("Expected: " + ("Right × Up points out of the facade")).IsTrue();
                    var mid = skin.Points[e] + skin.Dir[e] * ((o.U0 + o.U1) * 0.5f) - skin.Normal[e] * plan.Reveal;
                    AssertThat(Vector3.Distance(d.Center, new Vector3(mid.X, (o.Y0 + o.Y1) * 0.5f, mid.Y)) < 0.01f).OverrideFailureMessage("Expected: " + ("centred on the opening's glass")).IsTrue();
                    AssertThat(MathF.Abs(d.Width - (o.U1 - o.U0)) < 0.01f && d.BaseId == set.Blocks[0].Base && d.TotalColumns == plan.TotalColumns).OverrideFailureMessage("Expected: " + ("size and numbering as the glass")).IsTrue();
                }
        }
        AssertThat(new BuildingsModule().Build(ctx, plan.Tile(LodLevel.L1), LodLevel.L1).LooseWindows.Count == 0).OverrideFailureMessage("Expected: " + ("declared with the L0 cell only")).IsTrue();
    }

    [TestCase]
    public void WindowNumbersFollowFeatureOrder()
    {
        var world = World(
            WorldFeatureFixture.Create("t:b", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 30, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3),
            WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3));
        var blocks = WindowNumbering.For(world, 1u);
        AssertThat(blocks.Count == 2 && blocks[0].Feature.Value == "t:a" && blocks[0].Base == 0).OverrideFailureMessage("Expected: " + ("ordinal FeatureId order from zero")).IsTrue();
        AssertThat(blocks[1].Base == blocks[0].Count).OverrideFailureMessage("Expected: " + ("contiguous allocation")).IsTrue();
    }

    // ---------------------------------------------------------------- contract-29: the map-wide numbering provider

    [TestCase]
    public void TheProviderKeepsTheGenericNumbersWhenNothingIsClaimed()
    {
        var world = World(
            WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", source: FeatureSource.Osm, height: 19.8f, storeys: 6),
            WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(LShape(100, 0))), props: "{\"category\":\"research\"}", source: FeatureSource.Osm, height: 15.6f, storeys: 4),
            WorldFeatureFixture.Create("t:court", "building", new PolygonGeometry(Courtyard(0, 100)), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 12f, storeys: 3),
            WorldFeatureFixture.Create("t:hall", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 40, 30))), props: "{\"category\":\"sport\"}", source: FeatureSource.Osm, height: 9f, storeys: 1));
        var set = BuildingSet.For(world, 4u);
        var module = new BuildingsModule();
        AssertThat(module is IWindowNumberingProvider and IWindowMetadataSource).OverrideFailureMessage("Expected: " + ("the registered module is the provider")).IsTrue();
        var snapshot = module.PrepareWindowNumbering(Ctx(world, 4u), []);
        // The numbers the generator gave before the provider: running sum of columns × storeys in (feature, key) order.
        var next = 0;
        AssertThat(snapshot.Allocations.Count == set.Plans.Count).OverrideFailureMessage("Expected: " + ("one block per generic plan")).IsTrue();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var (plan, a) = (set.Plans[i], snapshot.Allocations[i]);
            AssertThat(a.Feature == plan.Id && a.Key == plan.Key && a.BaseId == next && a.Columns == plan.TotalColumns && a.Rows == plan.Storeys).OverrideFailureMessage("Expected: " + ($"{plan.Key}: base {a.BaseId}, expected {next}")).IsTrue();
            next += plan.TotalColumns * plan.Storeys;
        }
        AssertThat(snapshot.WindowCount == next && snapshot.Map.Value == "test").OverrideFailureMessage("Expected: " + ("the whole map, nothing else")).IsTrue();
        AssertThat(snapshot.Fingerprint.Length == 64 && snapshot.Fingerprint.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c)) &&
            module.PrepareWindowNumbering(Ctx(world, 4u), []).Fingerprint == snapshot.Fingerprint).OverrideFailureMessage("Expected: " + ("stable lowercase SHA-256")).IsTrue();
        // Build and the metadata read that table: same glass and the same declarations as the generic blocks.
        var ctx = Ctx(world, 4u, snapshot);
        var declared = new List<WindowDecl>();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var output = module.Build(ctx, set.Plans[i].Tile(LodLevel.L0), LodLevel.L0);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            AssertThat(glass.Length > 0 && glass.All(g => g.Uv2!.Where((_, k) => k % 2 == 0).All(x => x == set.Blocks[i].Base))).OverrideFailureMessage("Expected: " + ($"{set.Plans[i].Key}: glass keeps its base")).IsTrue();
            var expected = FacadeOpenings.Declarations(set.Plans[i], set.Blocks[i]);
            AssertThat(output.LooseWindows.Select(d => d.Id).SequenceEqual(expected.Select(d => d.Id))).OverrideFailureMessage("Expected: " + ($"{set.Plans[i].Key}: the same window numbers")).IsTrue();
            declared.AddRange(output.LooseWindows);
        }
        AssertThat(module.DescribeWindows(ctx).Windows.Select(d => d.Id).SequenceEqual(declared.Select(d => d.Id))).OverrideFailureMessage("Expected: " + ("metadata lists the L0 declarations")).IsTrue();
        AssertThrown(() => module.Build(Ctx(world, 4u), set.Plans[0].Tile(LodLevel.L0), LodLevel.L0)).IsInstanceOf<InvalidOperationException>();
        var other = new WindowNumberingSnapshot(new MapId("elsewhere"), snapshot.Fingerprint, snapshot.Allocations);
        AssertThrown(() => module.DescribeWindows(Ctx(world, 4u, other))).IsInstanceOf<InvalidOperationException>();
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
        var world = World(WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 21f, storeys: 6) with { Parts = parts },
            WorldFeatureFixture.Create("t:b", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 30, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3));
        var generic = WindowNumbering.For(world, 1u);
        var module = new BuildingsModule();
        var snapshot = module.PrepareWindowNumbering(Ctx(world, 1u, claims: ["t:a"]), [Layout("t:a", ("shell", 7, 3))]);
        AssertThat(snapshot.Allocations.Count == 2 && snapshot.Get(new("t:a"), "shell") == new WindowAllocation(new("t:a"), "shell", 0, 7, 3)).OverrideFailureMessage("Expected: " + ("both generic blocks of t:a gave way to the declared shell")).IsTrue();
        AssertThrown(() => snapshot.Get(new("t:a"), "osm:way:1")).IsInstanceOf<KeyNotFoundException>();
        var b = snapshot.Get(new("t:b"), "t:b");
        AssertThat(b.BaseId == 21 && generic[2].Base != 21).OverrideFailureMessage("Expected: " + ($"t:b moves to just behind the shell (21, generic {generic[2].Base})")).IsTrue();
        // The enable gate: glass and metadata both decode the moved base, never the generic one.
        var ctx = Ctx(world, 1u, snapshot, ["t:a"]);
        var tileB = BuildingSet.For(world, 1u).Plans.Single(p => p.Id.Value == "t:b").Tile(LodLevel.L0);
        AssertThat(module.Tiles(ctx).SequenceEqual([tileB])).OverrideFailureMessage("Expected: " + ("the claimed feature gets no cell")).IsTrue();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3 })
            foreach (var g in module.Build(ctx, tileB, lod).Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass))
                for (var i = 0; i < g.Position.Length / 3; i++)
                {
                    var id = (int)g.Uv2![2 * i] + (int)MathF.Floor(g.Uv![2 * i + 1]) * b.Columns + (int)MathF.Floor(g.Uv[2 * i]);
                    AssertThat(g.Uv2[2 * i] == b.BaseId && id >= b.BaseId && id < b.BaseId + b.Count).OverrideFailureMessage("Expected: " + ($"{lod}: glass window {id} inside t:b's new block")).IsTrue();
                }
        var metadata = module.DescribeWindows(ctx).Windows;
        AssertThat(metadata.Count > 0 && metadata.All(d => d.BaseId == b.BaseId && d.Id >= b.BaseId && d.Id < b.BaseId + b.Count)).OverrideFailureMessage("Expected: " + ("metadata on the new block")).IsTrue();
        AssertThat(module.Build(ctx, BuildingSet.For(world, 1u).Plans[0].Tile(LodLevel.L0), LodLevel.L0).Meshes.Count == 0).OverrideFailureMessage("Expected: " + ("WP-14 builds nothing of t:a")).IsTrue();
    }

    [TestCase]
    public void AnEmptyLayoutDeclaresNoWindowsInAnyOrder()
    {
        var world = World(WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3), WorldFeatureFixture.Create("t:b", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 30, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3),
            WorldFeatureFixture.Create("t:c", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 30, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3));
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a", "t:b"]);
        var snapshot = module.PrepareWindowNumbering(ctx, [Layout("t:b"), Layout("t:a", ("tower", 2, 5), ("main", 4, 3))]);
        AssertThat(snapshot.Allocations.Select(a => (a.Feature.Value, a.Key, a.BaseId)).SequenceEqual([("t:a", "main", 0), ("t:a", "tower", 12), ("t:c", "t:c", 22)])).OverrideFailureMessage("Expected: " + ("ordinal (feature, key) order; t:b takes no IDs")).IsTrue();
        AssertThrown(() => snapshot.Get(new("t:b"), "t:b")).IsInstanceOf<KeyNotFoundException>();
        var again = module.PrepareWindowNumbering(ctx, [Layout("t:a", ("main", 4, 3), ("tower", 2, 5)), Layout("t:b")]);
        AssertThat(again.Fingerprint == snapshot.Fingerprint && again.Allocations.SequenceEqual(snapshot.Allocations)).OverrideFailureMessage("Expected: " + ("declaration order does not matter")).IsTrue();
        AssertThat(module.PrepareWindowNumbering(ctx, [Layout("t:a", ("main", 4, 3), ("tower", 2, 5))]).Fingerprint != snapshot.Fingerprint).OverrideFailureMessage("Expected: " + ("an omitted empty declaration is a different input")).IsTrue();
    }

    [TestCase]
    public void InvalidLayoutsPublishNoTable()
    {
        var parts = new[] { new WorldFeaturePart("osm:way:1", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), 10.5f, 3) };
        var shared = new[] { parts[0], new WorldFeaturePart("osm:way:2", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 12))), 7f, 2) };
        var world = World(WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3) with { Parts = parts },
            WorldFeatureFixture.Create("t:b", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3) with { Parts = shared });
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a", "t:b"]);
        AssertThrown(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 1, 1)), Layout("t:a")])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 1, 1), ("x", 2, 2))])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 0, 3))])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => module.PrepareWindowNumbering(ctx, [Layout("t:zz", ("x", 1, 1))])).IsInstanceOf<ArgumentException>();
        var roofOnly = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = TestMap, WorldSeed = 1u, ClaimedBy = (_, aspect) => aspect == Aspects.Roof ? new WpId("WP-18") : null };
        AssertThrown(() => module.PrepareWindowNumbering(roofOnly, [Layout("t:a")])).IsInstanceOf<ArgumentException>();
        // osm:way:1 is numbered by t:a (first in order). t:b may leave it there and number its own grid (the Liren connector
        // beside the generic A/B blocks), but a grid renumbering that footprint would number one facade twice.
        var keep = module.PrepareWindowNumbering(ctx, [Layout("t:b", ("main", 3, 3))]);
        AssertThat(keep.Get(new("t:a"), "osm:way:1").Count == WindowNumbering.For(world, 1u)[0].Count && keep.Get(new("t:b"), "main").Count == 9).OverrideFailureMessage("Expected: " + ("the shared footprint keeps its generic block beside the claimed grid")).IsTrue();
        AssertThrown(() => module.PrepareWindowNumbering(ctx, [Layout("t:b", ("osm:way:1", 3, 3))])).IsInstanceOf<InvalidOperationException>();
        AssertThat(module.PrepareWindowNumbering(ctx, [Layout("t:a"), Layout("t:b", ("osm:way:1", 3, 3))]).WindowCount == 9).OverrideFailureMessage("Expected: " + ("once its holder is claimed too")).IsTrue();
    }

    [TestCase]
    public void TheTableStopsAtTheOverrideCapacity()
    {
        var world = World(WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), props: "{\"category\":\"service\"}", source: FeatureSource.Osm, height: 3.5f, storeys: 1), WorldFeatureFixture.Create("t:b", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 30, 12))), props: "{\"category\":\"teaching\"}", source: FeatureSource.Osm, height: 10.5f, storeys: 3));
        var generic = WindowNumbering.For(world, 1u)[1].Count;
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a"]);
        var full = module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", WindowNumberingSnapshot.Capacity - generic, 1))]);
        var last = full.Get(new("t:b"), "t:b");
        AssertThat(full.WindowCount == WindowNumberingSnapshot.Capacity && last.IdAt(last.Columns - 1, last.Rows - 1) == WindowNumberingSnapshot.Capacity - 1).OverrideFailureMessage("Expected: " + ("exactly 65536 IDs fit, the last is 65535")).IsTrue();
        AssertThrown(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", WindowNumberingSnapshot.Capacity - generic + 1, 1))])).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", int.MaxValue, 2))])).IsInstanceOf<InvalidOperationException>();
    }
}
