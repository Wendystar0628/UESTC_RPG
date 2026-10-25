using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Sites;
using Campus.Gen.Sites.Tests;
using Campus.Tests;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Pinxue;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Research;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Wenke;
using Campus.Gen.Tests.Support;
using GdUnit4;
using M = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Monuments;
using P = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Pinxue;
using R = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Research;
using F = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Friendship;
using K = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Wenke;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Tests;
using static GdUnit4.Assertions;
using static Campus.Gen.Maps.Qingshuihe.CenterEast.Tests.CenterEastTestFixture;

[TestSuite]
public sealed class CenterEastWindowTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
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

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void NumberedGlassMatchesTheDeclaredWindows()
    {
        var world = World();
        var module = new CenterEastModule();
        var ctx = RealMapFixture.ZoneContext(module, world, globalInputs: TextInputs());
        var allocations = ctx.WindowNumbering.Allocations;
        var exceptionThrown = false;
        try { module.DescribeWindows(RealMapFixture.ZoneContext(module, withWindowNumbering: false)); }
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
        AssertThat(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count))).OverrideFailureMessage("declared IDs lie in the zone's grids").IsTrue();
        AssertThat(metadata.Windows.Count > 0 && metadata.Facades.Count > 0).OverrideFailureMessage("bridges give loose windows, research blocks facade runs").IsTrue();
        // The portal cells under the entrance frames carry no window.
        var research = CenterEastSites.All.OfType<ResearchSite>().ToArray();
        var researchIds = research.Select(site => site.Entity).ToHashSet(StringComparer.Ordinal);
        AssertThat(metadata.Facades.Count(f => f.Skip.Count > 0 && allocations.Any(a => a.BaseId == f.BaseId && researchIds.Contains(a.Feature.Value)))
            == research.Count(site => site.BlockDoorId is not null)).OverrideFailureMessage("research entrances skip their own portal cells").IsTrue();

        HashSet<int> Glass(LodLevel lod)
        {
            var cells = ZoneTestCells.Items(module, ctx, lod).Select(i => TileGrid.At(i.Anchor, lod)).Distinct();
            return ZoneTestCells.WindowIds((lod == LodLevel.L4 ? [new TileKey(0, 0)] : cells)
                .SelectMany(tile => module.Build(ctx, tile, lod).Meshes)).Ids;
        }
        var near = Glass(LodLevel.L0);
        AssertThat(near.SetEquals(declared)).OverrideFailureMessage($"L0 glass carries exactly the declared windows ({near.Count} vs {declared.Count})").IsTrue();
        AssertThat(Glass(LodLevel.L3).SetEquals(declared)).OverrideFailureMessage("coarse glass keeps the same numbers").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WindowTablePublishesWithTheCenterEastDeclarations()
    {
        // The end-to-end reach before game/kernel/bake/'s orchestration: game/gen/buildings/'s provider allocates the whole real map (all authored world records)
        // with the zone's real declarations, then the zone describes its windows from the published snapshot.
        var world = RealMapFixture.Load().World;
        var module = new CenterEastModule();
        var claimed = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Facade) != 0).Select(c => c.Feature).ToHashSet();
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)FakeWorld.Enveloped(world)) })
        {
            var context = RealMapFixture.ZoneContext(module, view);
            var layouts = module.DescribeWindowLayouts(view, context.WorldSeed);
            var snapshot = context.WindowNumbering;
            var ours = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
            AssertThat(ours.Length == layouts.Sum(l => l.Grids.Count) && ours.All(a => a.Columns > 0 && a.Rows > 0)).OverrideFailureMessage($"{name}: every center-east grid is allocated with cells ({ours.Length} of {layouts.Sum(l => l.Grids.Count)})").IsTrue();
            AssertThat(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity).OverrideFailureMessage($"{name}: the table fits ({snapshot.WindowCount})").IsTrue();
            var metadata = module.DescribeWindows(context);
            var ids = metadata.Windows.Select(w => w.Id).Concat(metadata.Facades.SelectMany(f => Enumerable.Range(0, f.Rows * f.Columns)
                .Where(cell => !f.Skip.Contains(cell)).Select(cell => f.BaseId + cell / f.Columns * f.TotalColumns + f.ColumnStart + cell % f.Columns))).ToArray();
            AssertThat(ids.Length > 0 && ids.All(id => ours.Any(a => id >= a.BaseId && id < a.BaseId + a.Count))).OverrideFailureMessage($"{name}: the zone's windows lie in its own grids").IsTrue();
        }
    }
}
