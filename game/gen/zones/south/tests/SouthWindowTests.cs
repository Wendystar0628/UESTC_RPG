using Campus.Tests;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Zones.South.Tests;

using static GdUnit4.Assertions;
using static Campus.Gen.Zones.South.Tests.SouthTestSupport;

[TestSuite]
public sealed class SouthWindowTests
{
    [TestCase]
    public void WindowLayoutsDeclareEveryFacadeClaimWithItsGrids()
    {
        var module = new SouthModule();
        var world = RealMapFixture.Load().World;
        var layouts = module.DescribeWindowLayouts(world, 1).ToDictionary(layout => layout.Feature, layout => layout.Grids);
        var claimed = module.Claims.OfType<FeatureClaim>().Where(claim => (claim.Aspects & Aspects.Facade) != 0).Select(claim => claim.Feature).ToHashSet();
        AssertThat(claimed.SetEquals(layouts.Keys)).OverrideFailureMessage("every facade claim has exactly one declaration").IsTrue();
        AssertThat(layouts[new(Sports.GrandstandSite.Feature)].Count == 0).OverrideFailureMessage("the stand has no glass").IsTrue();
        AssertThat(layouts.Values.SelectMany(grids => grids).All(grid => grid.Columns > 0 && grid.Rows > 0)).OverrideFailureMessage("declared grids have cells").IsTrue();
    }

    [TestCase]
    public void EveryDeclaredGridIsValidOnTheRealMap()
    {
        // The map as the kernel loads it: every authored overlay, not only the south one. One invalid grid withholds the whole table.
        var world = RealMapFixture.Load().World;
        // The contract only promises an aggregate query envelope in WorldFeature.Geometry (the D0 world model gives a bounding
        // rectangle): the sites must read footprints from the parts, so the same declarations hold over such a view.
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)FakeWorld.Enveloped(world)) })
        {
            var layouts = new SouthModule().DescribeWindowLayouts(view, 1);
            var grids = layouts.SelectMany(l => l.Grids.Select(g => $"{l.Feature.Value}/{g.Key} {g.Columns}x{g.Rows}")).ToArray();
            AssertThat(layouts.All(l => l.Grids.All(g => g.Columns > 0 && g.Rows > 0))).OverrideFailureMessage($"{name}: every declared grid has cells: {string.Join(", ", grids)}").IsTrue();
            AssertThat(layouts.All(l => l.Grids.Select(g => g.Key).Distinct(StringComparer.Ordinal).Count() == l.Grids.Count)).OverrideFailureMessage($"{name}: keys are unique per feature").IsTrue();
        }
        AssertThat(new SouthModule().DescribeWindowLayouts(FakeWorld.Enveloped(world), 1).SequenceEqual(new SouthModule().DescribeWindowLayouts(world, 1), LayoutComparer.Instance)).OverrideFailureMessage("the envelope view declares the same grids").IsTrue();
        var lake = ((PolygonGeometry)world.Features[new(EastLake.EastLakeSite.Lake)].Geometry).Rings;
        AssertThat(EastLake.EastLakeSite.Water(FakeWorld.Enveloped(world)).Outer.SequenceEqual(lake.Outer) && EastLake.EastLakeSite.Water(world).Outer.SequenceEqual(lake.Outer)).OverrideFailureMessage("the east lake's water ring comes from its own part in both views").IsTrue();
    }

    [TestCase]
    public void WindowTablePublishesWithTheSouthDeclarations()
    {
        // The end-to-end reach before WP-02's orchestration: WP-14's provider allocates the whole real map with the zone's
        // real declarations, then the zone describes its windows from the published snapshot. The map loads as above.
        var world = RealMapFixture.Load().World;
        var module = new SouthModule();
        var claimed = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Facade) != 0).Select(c => c.Feature).ToHashSet();
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)FakeWorld.Enveloped(world)) })
        {
            WpId? Owner(FeatureId f, Aspects a) => claimed.Contains(f) ? new WpId("WP-18") : null;
            var prepass = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = view, Map = RealMapFixture.Entry().Descriptor, WorldSeed = 1, ClaimedBy = Owner };
            var layouts = module.DescribeWindowLayouts(view, 1);
            var snapshot = new global::Campus.Gen.Buildings.BuildingsModule().PrepareWindowNumbering(prepass, layouts);
            var south = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
            AssertThat(south.Length == layouts.Sum(l => l.Grids.Count) && south.All(a => a.Columns > 0 && a.Rows > 0)).OverrideFailureMessage($"{name}: every south grid is allocated with cells ({south.Length} of {layouts.Sum(l => l.Grids.Count)})").IsTrue();
            AssertThat(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity).OverrideFailureMessage($"{name}: the table fits ({snapshot.WindowCount})").IsTrue();
            var metadata = module.DescribeWindows(new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = view, Map = RealMapFixture.Entry().Descriptor, WorldSeed = 1, ClaimedBy = Owner, WindowNumbering = snapshot });
            var ids = metadata.Windows.Select(w => w.Id).Concat(metadata.Facades.SelectMany(f => Enumerable.Range(0, f.Rows * f.Columns)
                .Where(cell => !f.Skip.Contains(cell)).Select(cell => f.BaseId + cell / f.Columns * f.TotalColumns + f.ColumnStart + cell % f.Columns))).ToArray();
            AssertThat(ids.Length > 0 && ids.All(id => south.Any(a => id >= a.BaseId && id < a.BaseId + a.Count))).OverrideFailureMessage($"{name}: the zone's windows lie in its own blocks").IsTrue();
        }
    }

    private sealed class LayoutComparer : IEqualityComparer<ClaimedWindowLayout>
    {
        public static readonly LayoutComparer Instance = new();
        public bool Equals(ClaimedWindowLayout? a, ClaimedWindowLayout? b) => a is not null && b is not null && a.Feature == b.Feature && a.Grids.SequenceEqual(b.Grids);
        public int GetHashCode(ClaimedWindowLayout l) => l.Feature.GetHashCode();
    }

    [TestCase]
    public void GuardHouseAndPorticoGlassIsNumberedOutward()
    {
        var world = RealMapFixture.Load().World;
        var gate = Gate.SouthGateSite.LayoutOf(world);
        var houses = Gate.SouthGateSite.Openings(gate, 0f);
        AssertThat(houses.Length == 6 && houses.Select(o => (o.Column, o.Row)).Distinct().Count() == 6).OverrideFailureMessage("six guard-house windows, one cell each").IsTrue();
        foreach (var o in houses)
        {
            // One-sided glass: each pane must face away from its own house's centre.
            var house = o.Row == 0 ? gate.HouseWest : gate.HouseEast;
            var d = new Vector2(o.Origin.X, o.Origin.Z) - house;
            AssertThat(Vector2.Dot(d, new(o.Outward.X, o.Outward.Z)) > 2.5f).OverrideFailureMessage($"guard-house pane ({o.Column}, {o.Row}) faces out of its house").IsTrue();
        }
        var f = Xuanbang.XuanbangSite.Rings(world);
        var porticoes = Xuanbang.XuanbangSite.Porticoes(f);
        AssertThat(porticoes.Count == 1).OverrideFailureMessage("one pediment portico on the main front").IsTrue();
        var glass = Xuanbang.XuanbangSite.PorticoOpenings(porticoes[0], 0.45f, 0);
        AssertThat(glass.Length == 6 && glass.Count(o => o.Row == 1) == 3).OverrideFailureMessage("three doors and three arched windows").IsTrue();
        AssertThat(glass.All(o => Vector2.Dot(new(o.Outward.X, o.Outward.Z), porticoes[0].W) > 0.99f)).OverrideFailureMessage("the hall glass faces out through the colonnade").IsTrue();
        AssertThat(glass.Where(o => o.Row == 1).All(o => o.Origin.Y > 0.45f + SouthTune.Xuanbang.DoorSpring)).OverrideFailureMessage("the windows sit above the doors").IsTrue();
    }

    [TestCase]
    public void NumberedGlassMatchesTheDeclaredWindows()
    {
        var world = RealMapFixture.Load().World;
        var module = new SouthModule();
        // A stand-in for WP-14's allocation: the declared grids in canonical order after a block that shifts every base.
        var specs = module.DescribeWindowLayouts(world, 1).SelectMany(l => l.Grids.Select(g => (l.Feature, g)))
            .Prepend((new FeatureId("a-earlier"), new WindowGridSpec("block", 37, 3)))
            .OrderBy(p => p.Item1.Value, StringComparer.Ordinal).ThenBy(p => p.Item2.Key, StringComparer.Ordinal);
        var allocations = new List<WindowAllocation>();
        var next = 0;
        foreach (var (feature, grid) in specs)
        {
            allocations.Add(new(feature, grid.Key, next, grid.Columns, grid.Rows));
            next += grid.Columns * grid.Rows;
        }
        var ctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = RealMapFixture.Entry().Descriptor, WorldSeed = 1, WindowNumbering = new(RealMapFixture.Entry().Descriptor.Id, new string('a', 64), allocations) };
        var threwWithoutSnapshot = false;
        try { module.DescribeWindows(Context()); }
        catch (InvalidOperationException) { threwWithoutSnapshot = true; }
        AssertThat(threwWithoutSnapshot).OverrideFailureMessage("positions need the numbering snapshot").IsTrue();

        var metadata = module.DescribeWindows(ctx);
        var declared = new List<int>();
        foreach (var f in metadata.Facades)
            for (var r = 0; r < f.Rows; r++)
                for (var c = 0; c < f.Columns; c++)
                    if (!f.Skip.Contains(r * f.Columns + c)) declared.Add(f.BaseId + r * f.TotalColumns + f.ColumnStart + c);
        declared.AddRange(metadata.Windows.Select(w => w.Id));
        AssertThat(declared.Count == declared.Distinct().Count()).OverrideFailureMessage("every window is declared once").IsTrue();
        AssertThat(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count && a.Feature.Value != "a-earlier"))).OverrideFailureMessage("declared IDs lie in the zone's blocks").IsTrue();

        HashSet<int> Glass(LodLevel lod)
        {
            var ids = new HashSet<int>();
            var cells = module.ItemsFor(ctx, lod).Select(i => (TileGrid.At(i.Anchor, lod).X, TileGrid.At(i.Anchor, lod).Z)).Distinct();
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var glass = module.Build(ctx, new(x, z), lod).Meshes.Where(m => m.Uv2 is not null).ToArray();
                AssertThat(glass.Length <= 2).OverrideFailureMessage($"{lod} cell ({x}, {z}): numbered panes merge into one bundle ({glass.Length})").IsTrue();
                foreach (var mesh in glass)
                    for (var v = 0; v < mesh.Uv2!.Length / 2; v++)
                    {
                        var columns = (int)MathF.Floor(mesh.Uv2[2 * v + 1] / 32f);
                        ids.Add((int)mesh.Uv2[2 * v] + (int)MathF.Floor(mesh.Uv![2 * v + 1]) * columns + (int)MathF.Floor(mesh.Uv[2 * v]));
                    }
            }
            return ids;
        }
        var near = Glass(LodLevel.L0);
        AssertThat(near.SetEquals(declared)).OverrideFailureMessage($"L0 glass carries exactly the declared windows ({near.Count} vs {declared.Count})").IsTrue();
        AssertThat(Glass(LodLevel.L3).IsSubsetOf(declared)).OverrideFailureMessage("coarse glass keeps the same numbers").IsTrue();
    }

}
