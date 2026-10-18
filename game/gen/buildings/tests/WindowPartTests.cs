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
public sealed class WindowPartTests
{
    private static bool Sash(GeometryBundle b) => b.Material.Spec.Lit.Value == "frame.alu.lit" && b.Material.Spec.Fx == ToonFx.Rim;
    // The WP-10 gasket class (dark equipment steel without outline), not the WP-12 roof fence of the same colour.
    private static bool Gasket(GeometryBundle b) => HasColor(b, "equip.fence.lit") && b.Material.Spec.Line == LineClass.None;

    [TestCase, TestCategory(TestReason.Contract)]
    public void PhysicalCurtainFlagMatchesActualPerOpeningGeometryAtEachLodAndZone()
    {
        var world = World(WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", source: FeatureSource.Osm, height: 19.8f, storeys: 6));
        var set = BuildingSet.For(world, 1u);
        var plan = set.Plans[0];
        var present = 0;
        var absent = 0;
        foreach (var zone in new[] { FidelityZone.K1, FidelityZone.K2, FidelityZone.K3 })
            foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3 })
                for (var fi = 0; fi < plan.Facades.Count; fi++)
                {
                    var layout = plan.Facades[fi];
                    var frame = Mesh.EdgeFrame.Of(plan.SkinAt(layout.Ring), layout.Edge);
                    foreach (var opening in plan.Openings[fi].Where(o => o.Kind != BayKind.Door))
                    {
                        var sink = new Mesh.MeshSink(Campus.Contracts.Tests.PaletteFixture.Catalog);
                        var openings = new FacadeOpenings(plan, set.Blocks[0], lod, sink);
                        if (lod <= LodLevel.L1) openings.Fill(frame, layout, opening, zone);
                        else openings.FlatGlass(frame, layout, opening, -plan.Reveal);
                        var meshes = sink.Finish();
                        var actualCurtain = meshes.Any(Fabric);
                        if (actualCurtain) present++; else absent++;
                        AssertThat(!actualCurtain || (zone <= FidelityZone.K2 && lod == LodLevel.L0)).OverrideFailureMessage("Expected: " + ("Physical curtain remains confined to the existing near LOD/zone.")).IsTrue();
                        var glass = meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
                        AssertThat(glass.Length > 0).OverrideFailureMessage("Expected: " + ("Each opening still has glass.")).IsTrue();
                        var card = (byte)FacadeOpenings.Card(plan, opening, FacadeOpenings.Number(plan, set.Blocks[0], layout, opening));
                        foreach (var mesh in glass)
                            foreach (var encoded in mesh.Reserved!)
                            {
                                AssertThat((encoded & WindowCardEncoding.TypeMask) == card).OverrideFailureMessage("Expected: " + ("Presence flag preserves the window card type.")).IsTrue();
                                AssertThat(((encoded & WindowCardEncoding.PhysicalCurtainFlag) != 0) == actualCurtain).OverrideFailureMessage("Expected: " + ($"{zone} {lod} bay {opening.Bay}: flag exactly matches actual curtain geometry.")).IsTrue();
                            }
                    }
                }
        AssertThat(present > 0 && absent > 0).OverrideFailureMessage("Expected: " + ("Exercise actual curtains and uncurtained openings, including distant LODs.")).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void K1AndK2OpeningsTakeTheWp10Parts()
    {
        // The walking corridor's ground band is K2, the facade above it K3 (ENG-ARCH FacadeSegment).
        var world = World(new ZonedFidelity(FidelityZone.K3, FidelityZone.K2), WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", source: FeatureSource.Osm, height: 19.8f, storeys: 6));
        var set = BuildingSet.For(world, 1u);
        var plan = set.Plans[0];
        for (var fi = 0; fi < plan.Facades.Count; fi++)
            for (var k = 0; k < plan.Openings[fi].Length; k++)
                AssertThat(plan.ZoneOf(fi, k) == (plan.Openings[fi][k].Row == 0 ? FidelityZone.K2 : FidelityZone.K3)).OverrideFailureMessage("Expected: " + ("ground floor in the K2 band, the rest K3")).IsTrue();
        var module = new BuildingsModule();
        var near = module.BuildOne(plan, set.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        var mid = module.BuildOne(plan, set.Blocks[0], LodLevel.L1, Campus.Contracts.Tests.PaletteFixture.Catalog);
        AssertThat(near.Any(Sash)).OverrideFailureMessage("Expected: " + ("K2 windows carry the WP-10 two-level sashes at L0")).IsTrue();
        AssertThat(near.Any(Fabric)).OverrideFailureMessage("Expected: " + ("K2 room windows hang WP-10 curtains at L0")).IsTrue();
        AssertThat(!mid.Any(Sash) && !mid.Any(Fabric)).OverrideFailureMessage("Expected: " + ("L1 keeps the simplified frame ring only")).IsTrue();
        // Without window gaskets the dark steel is only the astragal between the entrance leaves (two boxes).
        var dark = near.Where(Gasket).Sum(b => b.Index.Length / 3);
        AssertThat(Rules.OpeningRules.Gaskets || dark <= 24).OverrideFailureMessage("Expected: " + ("window gaskets follow OpeningRules.Gaskets")).IsTrue();
        AssertThat(near.Concat(mid).All(b => b.Uv2 is null || b.Material.Spec.SurfacePattern is not null || b.Uv2.Where((_, i) => i % 2 == 0).All(x => x == set.Blocks[0].Base))).OverrideFailureMessage("Expected: " + ("no part glass keeps WP-10's placeholder numbering")).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void K3OpeningsTakeTheSimpleWindow()
    {
        var world = World(
            WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", source: FeatureSource.Osm, height: 19.8f, storeys: 6),
            WorldFeatureFixture.Create("t:hall", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 40, 30))), props: "{\"category\":\"sport\"}", source: FeatureSource.Osm, height: 9f, storeys: 1),
            WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 48, 16))), props: "{\"category\":\"research\"}", source: FeatureSource.Osm, height: 21f, storeys: 5),
            WorldFeatureFixture.Create("t:canteen", "building", new PolygonGeometry(new PolygonRings(Rect(300, 0, 40, 24))), props: "{\"category\":\"dining\"}", source: FeatureSource.Osm, height: 9.5f, storeys: 2));
        var set = BuildingSet.For(world, 3u);
        var module = new BuildingsModule();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var plan = set.Plans[i];
            var near = module.BuildOne(plan, set.Blocks[i], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
            AssertThat(!near.Any(Sash) && !near.Any(Fabric)).OverrideFailureMessage("Expected: " + ($"{plan.Key}: K3 has no sashes, handles or curtains")).IsTrue();
            AssertThat(near.All(b => !Gasket(b) || b.Index.Length / 3 <= 24)).OverrideFailureMessage("Expected: " + ($"{plan.Key}: no window gaskets")).IsTrue();
        }
    }
}
