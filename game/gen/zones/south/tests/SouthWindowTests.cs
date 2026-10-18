using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Zones.South.Tests;

using static Campus.Gen.Zones.South.Tests.SouthTestSupport;

[TestSuite]
public sealed class SouthWindowTests
{
    [TestCase]
    public void WindowLayoutsDeclareEveryFacadeClaimWithItsGrids()
    {
        var layouts = new SouthModule().DescribeWindowLayouts(World(), 1).ToDictionary(l => l.Feature.Value, l => l.Grids, StringComparer.Ordinal);
        Require(layouts.Count == 8, "gate, front steps, Xuanbang, the Liren connector, the two venues, the platform and the stand are declared");
        Require(layouts[Sports.GrandstandSite.Feature].Count == 0, "the stand has no glass: its declaration is empty");
        Require(!layouts.ContainsKey(MainBuilding.MainBuildingSite.B2), "B2 keeps WP-14's generic windows");
        void Grid(string feature, string key, int? columns, int rows)
        {
            var grid = layouts[feature].Single(g => g.Key == key);
            Require(grid.Rows == rows && grid.Columns > 0 && (columns is null || grid.Columns == columns), $"{feature} / {key} is {grid.Columns}x{grid.Rows}");
        }
        Grid(Gate.SouthGateSite.Feature, "guard-houses", 3, 2);
        Grid(MainBuilding.MainBuildingSite.FrontSteps, "front-porch", 5, 1);
        Grid(MainBuilding.MainBuildingSite.FrontSteps, "rear-porch", 1, 1);
        Grid(Xuanbang.XuanbangSite.Feature, "main", null, 8);
        Grid(Xuanbang.XuanbangSite.Feature, "tower", null, 18);
        Grid(Xuanbang.XuanbangSite.Feature, "clock-shaft", 8, SouthTune.Xuanbang.SlitRows);
        Grid(Xuanbang.XuanbangSite.Feature, "clock-portals", 4, 1);
        Grid(Xuanbang.XuanbangSite.Feature, "portico", 3, 2);
        Grid(Liren.LirenSite.Connector, "piers", null, 4);
        Grid(Liren.LirenSite.Connector, "bridge", null, 1);
        Grid(Liren.LirenSite.Connector, "east-link", null, 2);
        Grid(Sports.GymSite.Feature, "ring-hall", null, 2);
        Grid(Sports.NatatoriumSite.Feature, "glass-band", null, 1);
        Grid(Sports.PlatformSite.Feature, "arcade-shops", null, 1);
        Require(layouts.Values.Sum(g => g.Count) == 14, "fourteen grids in all");
    }

    [TestCase]
    public void EveryDeclaredGridIsValidOnTheRealMap()
    {
        // The map as the kernel loads it: every authored overlay, not only the south one. One invalid grid withholds the whole table.
        var data = DataRoot();
        var world = FullWorld();
        // The contract only promises an aggregate query envelope in WorldFeature.Geometry (the D0 world model gives a bounding
        // rectangle): the sites must read footprints from the parts, so the same declarations hold over such a view.
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)FakeWorld.Enveloped(world)) })
        {
            var layouts = new SouthModule().DescribeWindowLayouts(view, 1);
            var grids = layouts.SelectMany(l => l.Grids.Select(g => $"{l.Feature.Value}/{g.Key} {g.Columns}x{g.Rows}")).ToArray();
            Require(layouts.All(l => l.Grids.All(g => g.Columns > 0 && g.Rows > 0)), $"{name}: every declared grid has cells: {string.Join(", ", grids)}");
            Require(layouts.All(l => l.Grids.Select(g => g.Key).Distinct(StringComparer.Ordinal).Count() == l.Grids.Count), $"{name}: keys are unique per feature");
        }
        Require(new SouthModule().DescribeWindowLayouts(FakeWorld.Enveloped(world), 1).SequenceEqual(new SouthModule().DescribeWindowLayouts(world, 1), LayoutComparer.Instance),
            "the envelope view declares the same grids");
        var lake = ((PolygonGeometry)world.Features[new(EastLake.EastLakeSite.Lake)].Geometry).Rings;
        Require(EastLake.EastLakeSite.Water(FakeWorld.Enveloped(world)).Outer.SequenceEqual(lake.Outer) && EastLake.EastLakeSite.Water(world).Outer.SequenceEqual(lake.Outer),
            "the east lake's water ring comes from its own part in both views");
    }

    [TestCase]
    public void WindowTablePublishesWithTheSouthDeclarations()
    {
        // The end-to-end reach before WP-02's orchestration: WP-14's provider allocates the whole real map with the zone's
        // real declarations, then the zone describes its windows from the published snapshot. The map loads as above.
        var data = DataRoot();
        var world = FullWorld();
        var module = new SouthModule();
        var claimed = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Facade) != 0).Select(c => c.Feature).ToHashSet();
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)FakeWorld.Enveloped(world)) })
        {
            WpId? Owner(FeatureId f, Aspects a) => claimed.Contains(f) ? new WpId("WP-18") : null;
            var prepass = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner };
            var layouts = module.DescribeWindowLayouts(view, 1);
            var snapshot = new global::Campus.Gen.Buildings.BuildingsModule().PrepareWindowNumbering(prepass, layouts);
            var south = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
            Require(south.Length == layouts.Sum(l => l.Grids.Count) && south.All(a => a.Columns > 0 && a.Rows > 0),
                $"{name}: every south grid is allocated with cells ({south.Length} of {layouts.Sum(l => l.Grids.Count)})");
            Require(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity, $"{name}: the table fits ({snapshot.WindowCount})");
            var metadata = module.DescribeWindows(new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner, WindowNumbering = snapshot });
            var ids = metadata.Windows.Select(w => w.Id).Concat(metadata.Facades.SelectMany(f => Enumerable.Range(0, f.Rows * f.Columns)
                .Where(cell => !f.Skip.Contains(cell)).Select(cell => f.BaseId + cell / f.Columns * f.TotalColumns + f.ColumnStart + cell % f.Columns))).ToArray();
            Require(ids.Length > 0 && ids.All(id => south.Any(a => id >= a.BaseId && id < a.BaseId + a.Count)), $"{name}: the zone's windows lie in its own blocks");
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
        var world = World();
        var gate = Gate.SouthGateSite.LayoutOf(world);
        var houses = Gate.SouthGateSite.Openings(gate, 0f);
        Require(houses.Length == 6 && houses.Select(o => (o.Column, o.Row)).Distinct().Count() == 6, "six guard-house windows, one cell each");
        foreach (var o in houses)
        {
            // One-sided glass: each pane must face away from its own house's centre.
            var house = o.Row == 0 ? gate.HouseWest : gate.HouseEast;
            var d = new Vector2(o.Origin.X, o.Origin.Z) - house;
            Require(Vector2.Dot(d, new(o.Outward.X, o.Outward.Z)) > 2.5f, $"guard-house pane ({o.Column}, {o.Row}) faces out of its house");
        }
        var f = Xuanbang.XuanbangSite.Rings(world);
        var porticoes = Xuanbang.XuanbangSite.Porticoes(f);
        Require(porticoes.Count == 1, "one pediment portico on the main front");
        var glass = Xuanbang.XuanbangSite.PorticoOpenings(porticoes[0], 0.45f, 0);
        Require(glass.Length == 6 && glass.Count(o => o.Row == 1) == 3, "three doors and three arched windows");
        Require(glass.All(o => Vector2.Dot(new(o.Outward.X, o.Outward.Z), porticoes[0].W) > 0.99f), "the hall glass faces out through the colonnade");
        Require(glass.Where(o => o.Row == 1).All(o => o.Origin.Y > 0.45f + SouthTune.Xuanbang.DoorSpring), "the windows sit above the doors");
    }

    [TestCase]
    public void NumberedGlassMatchesTheDeclaredWindows()
    {
        var world = World();
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
        var ctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = Descriptor(), WorldSeed = 1, WindowNumbering = new(new("qingshuihe"), new string('a', 64), allocations) };
        Throws(() => module.DescribeWindows(Context()), "positions need the numbering snapshot");

        var metadata = module.DescribeWindows(ctx);
        var declared = new List<int>();
        foreach (var f in metadata.Facades)
            for (var r = 0; r < f.Rows; r++)
                for (var c = 0; c < f.Columns; c++)
                    if (!f.Skip.Contains(r * f.Columns + c)) declared.Add(f.BaseId + r * f.TotalColumns + f.ColumnStart + c);
        declared.AddRange(metadata.Windows.Select(w => w.Id));
        Require(declared.Count == declared.Distinct().Count(), "every window is declared once");
        Require(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count && a.Feature.Value != "a-earlier")), "declared IDs lie in the zone's blocks");

        HashSet<int> Glass(LodLevel lod)
        {
            var ids = new HashSet<int>();
            var size = 64 << (int)lod;
            var cells = module.ItemsFor(ctx, lod).Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct();
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var glass = module.Build(ctx, new(x, z), lod).Meshes.Where(m => m.Uv2 is not null).ToArray();
                Require(glass.Length <= 2, $"{lod} cell ({x}, {z}): numbered panes merge into one bundle ({glass.Length})");
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
        Require(near.SetEquals(declared), $"L0 glass carries exactly the declared windows ({near.Count} vs {declared.Count})");
        Require(Glass(LodLevel.L3).IsSubsetOf(declared), "coarse glass keeps the same numbers");
    }

    private static MapDescriptor Descriptor()
    {
        var rect = new MapRect(new(-1280f, -1280f), new(1280f, 1280f));
        return new MapDescriptor(new MapId("qingshuihe"), new LocalizedName("清水河", "Qingshuihe"), MapKind.Outdoor, 1, 1u,
            new GeoFrame(0d, 0d, 32648, new(-200f, -200f), new(200f, 200f)), Vector2.Zero, new Box3(new(-1280f, -28f, -1280f), new(1280f, 2000f, 1280f)), rect,
            new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f), new TileKey(-20, -20), new TileKey(19, 19), "P00", [],
            new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
            new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
    }

    private static void Throws(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException(message);
    }
}
