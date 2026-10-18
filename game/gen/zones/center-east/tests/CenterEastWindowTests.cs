using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Zones.Common;
using Campus.Tests;
using Campus.Gen.World;
using Campus.Gen.Zones.CenterEast.Framework;
using Campus.Gen.Zones.CenterEast.Pinxue;
using Campus.Gen.Zones.CenterEast.Research;
using Campus.Gen.Zones.CenterEast.Wenke;
using Campus.Gen.Tests.Support;
using GdUnit4;
using M = Campus.Gen.Zones.CenterEast.CenterEastTune.Monuments;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;
using R = Campus.Gen.Zones.CenterEast.CenterEastTune.Research;
using F = Campus.Gen.Zones.CenterEast.CenterEastTune.Friendship;
using K = Campus.Gen.Zones.CenterEast.CenterEastTune.Wenke;

namespace Campus.Gen.Zones.CenterEast.Tests;
using static GdUnit4.Assertions;

public sealed partial class CenterEastModuleTests
{
    [TestCase]
    public void EveryFacadeClaimDeclaresItsWindowGrids()
    {
        // 品学楼's own parts are the frame beam and the two link bridges (ribbon windows); the three research buildings declare
        // one grid per gridded block. Every grid has cells, and the envelope view declares the same set.
        var world = World();
        var layouts = new CenterEastModule().DescribeWindowLayouts(world, 1).ToDictionary(l => l.Feature.Value, l => l.Grids, StringComparer.Ordinal);
        var claimed = new CenterEastModule().Claims.OfType<FeatureClaim>()
            .Where(claim => (claim.Aspects & Aspects.Facade) != 0).Select(claim => claim.Feature.Value).Order(StringComparer.Ordinal);
        AssertThat(layouts.Keys.Order(StringComparer.Ordinal).SequenceEqual(claimed)).OverrideFailureMessage("every facade claim declares its grids").IsTrue();
        AssertThat(layouts.Values.SelectMany(grids => grids).All(grid => grid.Columns > 0 && grid.Rows > 0)).OverrideFailureMessage("every grid has cells").IsTrue();
        var envelope = new CenterEastModule().DescribeWindowLayouts(FakeWorld.Enveloped(world), 1);
        AssertThat(envelope.All(l => l.Grids.SequenceEqual(layouts[l.Feature.Value])) && envelope.Count == layouts.Count).OverrideFailureMessage("the envelope view declares the same grids").IsTrue();
    }

    [TestCase]
    public void NumberedGlassMatchesTheDeclaredWindows()
    {
        var world = World();
        var module = new CenterEastModule();
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
        var ctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = Descriptor(), WorldSeed = 1, WindowNumbering = new(Descriptor().Id, new string('a', 64), allocations) };
        var exceptionThrown = false;
        try { module.DescribeWindows(Context()); }
        catch (InvalidOperationException) { exceptionThrown = true; }
        AssertThat(exceptionThrown).OverrideFailureMessage("positions need the numbering snapshot").IsTrue();

        var metadata = module.DescribeWindows(ctx);
        var declared = new List<int>();
        foreach (var f in metadata.Facades)
            for (var r = 0; r < f.Rows; r++)
                for (var c = 0; c < f.Columns; c++)
                    if (!f.Skip.Contains(r * f.Columns + c)) declared.Add(f.BaseId + r * f.TotalColumns + f.ColumnStart + c);
        declared.AddRange(metadata.Windows.Select(w => w.Id));
        AssertThat(declared.Count == declared.Distinct().Count()).OverrideFailureMessage("every window is declared once").IsTrue();
        AssertThat(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count && a.Feature.Value != "a-earlier"))).OverrideFailureMessage("declared IDs lie in the zone's grids").IsTrue();
        AssertThat(metadata.Windows.Count > 0 && metadata.Facades.Count > 0).OverrideFailureMessage("bridges give loose windows, research blocks facade runs").IsTrue();
        // The portal cells under the entrance frames carry no window.
        AssertThat(metadata.Facades.Count(f => f.Skip.Count > 0) == CenterEastSites.All.OfType<ResearchSite>().Count(site => site.BlockDoorId is not null)).OverrideFailureMessage("科 A and 科 B each skip their portal cells").IsTrue();

        HashSet<int> Glass(LodLevel lod)
        {
            var ids = new HashSet<int>();
            var cells = module.ItemsFor(ctx, lod).Select(i => TileGrid.At(i.Anchor, lod)).Distinct();
            foreach (var tile in lod == LodLevel.L4 ? [new TileKey(0, 0)] : cells)
                foreach (var mesh in module.Build(ctx, tile, lod).Meshes.Where(m => m.Uv2 is not null))
                    for (var v = 0; v < mesh.Uv2!.Length / 2; v++)
                    {
                        var columns = (int)MathF.Floor(mesh.Uv2[2 * v + 1] / 32f);
                        ids.Add((int)mesh.Uv2[2 * v] + (int)MathF.Floor(mesh.Uv![2 * v + 1]) * columns + (int)MathF.Floor(mesh.Uv[2 * v]));
                    }
            return ids;
        }
        var near = Glass(LodLevel.L0);
        AssertThat(near.SetEquals(declared)).OverrideFailureMessage($"L0 glass carries exactly the declared windows ({near.Count} vs {declared.Count})").IsTrue();
        AssertThat(Glass(LodLevel.L3).SetEquals(declared)).OverrideFailureMessage("coarse glass keeps the same numbers").IsTrue();
    }

    [TestCase]
    public void WindowTablePublishesWithTheCenterEastDeclarations()
    {
        // The end-to-end reach before WP-02's orchestration: WP-14's provider allocates the whole real map (every authored overlay)
        // with the zone's real declarations, then the zone describes its windows from the published snapshot.
        var world = RealMapFixture.Load().World;
        var module = new CenterEastModule();
        var claimed = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Facade) != 0).Select(c => c.Feature).ToHashSet();
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)FakeWorld.Enveloped(world)) })
        {
            WpId? Owner(FeatureId f, Aspects a) => claimed.Contains(f) ? new WpId("WP-21") : null;
            var prepass = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner };
            var layouts = module.DescribeWindowLayouts(view, 1);
            var snapshot = new global::Campus.Gen.Buildings.BuildingsModule().PrepareWindowNumbering(prepass, layouts);
            var ours = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
            AssertThat(ours.Length == layouts.Sum(l => l.Grids.Count) && ours.All(a => a.Columns > 0 && a.Rows > 0)).OverrideFailureMessage($"{name}: every center-east grid is allocated with cells ({ours.Length} of {layouts.Sum(l => l.Grids.Count)})").IsTrue();
            AssertThat(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity).OverrideFailureMessage($"{name}: the table fits ({snapshot.WindowCount})").IsTrue();
            var metadata = module.DescribeWindows(new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner, WindowNumbering = snapshot });
            var ids = metadata.Windows.Select(w => w.Id).Concat(metadata.Facades.SelectMany(f => Enumerable.Range(0, f.Rows * f.Columns)
                .Where(cell => !f.Skip.Contains(cell)).Select(cell => f.BaseId + cell / f.Columns * f.TotalColumns + f.ColumnStart + cell % f.Columns))).ToArray();
            AssertThat(ids.Length > 0 && ids.All(id => ours.Any(a => id >= a.BaseId && id < a.BaseId + a.Count))).OverrideFailureMessage($"{name}: the zone's windows lie in its own grids").IsTrue();
        }
    }

}
