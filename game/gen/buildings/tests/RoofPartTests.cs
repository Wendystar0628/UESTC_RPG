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
public sealed class RoofPartTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void RoofsTakeTheWp12PartsByTheirZone()
    {
        WorldFeature[] features = [WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6), WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 48, 20))), props: "{\"category\":\"research\"}", height: 21f, storeys: 5)];
        var plain = BuildingSet.For(Ctx(World(features), 2u));
        var hero = BuildingSet.For(Ctx(World(new ZonedFidelity(FidelityZone.K3, FidelityZone.K3, FidelityZone.K1), features), 2u));
        var module = new BuildingsModule();
        static bool Pump(GeometryBundle b) => HasColor(b, "equip.grey.lit");
        // The fence boards in the light metal grey of RoofRules.FenceBoard (the board class: building line, no effects).
        static bool Fence(GeometryBundle b) => HasColor(b, Rules.RoofRules.FenceBoard + ".lit") &&
            b.Material.Spec.Line == LineClass.Building && b.Material.Spec.Fx == ToonFx.None;
        static bool Strip(GeometryBundle b) => HasColor(b, "roof.silverMetal.lit") && b.Material.Spec.Fx.HasFlag(ToonFx.Rim);
        // Roof plant is the silver metal standing on the roof: the facade's AC cages share its colour but hang below the roof line.
        static bool OnRoof(GeometryBundle b, BuildingPlan p) => ColorVertices(b, "roof.silverMetal.lit").Any(v => b.Position[3 * v + 1] > p.RoofY);
        static bool Tank(GeometryBundle b, BuildingPlan p) => b.Material.Spec.Fx == ToonFx.BevelLift && OnRoof(b, p);
        static int Tris(IEnumerable<GeometryBundle> bs, string token) => bs.Sum(b => Enumerable.Range(0, b.Index.Length / 3).Count(t => TriangleColor(b, t * 3, token)));
        for (var i = 0; i < plain.Plans.Count; i++)
        {
            var (simple, full) = (plain.Plans[i], hero.Plans[i]);
            AssertThat(simple.RoofZone == FidelityZone.K4 && full.RoofZone == FidelityZone.K1).OverrideFailureMessage("Expected: " + ("roof zone from frozen fidelity data, default roofs K4")).IsTrue();
            var near = module.BuildOne(simple, plain.Blocks[i], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
            var nearFull = module.BuildOne(full, hero.Blocks[i], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
            AssertThat(near.Any(Fence) && near.Any(Pump) && near.Any(Strip)).OverrideFailureMessage("Expected: " + ($"{simple.Key}: fenced plant and lightning strip")).IsTrue();
            AssertThat(!near.Concat(nearFull).Any(b => HasColor(b, "equip.fence.lit") && b.Material.Spec.Line == LineClass.Building)).OverrideFailureMessage("Expected: " + ($"{simple.Key}: no dark fence steel left on the boards")).IsTrue();
            AssertThat((simple.Style == Rules.BuildingStyle.Research) == near.Any(b => Tank(b, simple))).OverrideFailureMessage("Expected: " + ($"{simple.Key}: a water tank beside teaching plant only")).IsTrue();
            AssertThat(Tris(nearFull.Where(Pump), "equip.grey.lit") > Tris(near.Where(Pump), "equip.grey.lit")).OverrideFailureMessage("Expected: " + ($"{simple.Key}: a K1 roof carries more and finer units")).IsTrue();
            AssertThat(Tris(nearFull.Where(Strip), "roof.silverMetal.lit") > Tris(near.Where(Strip), "roof.silverMetal.lit")).OverrideFailureMessage("Expected: " + ($"{simple.Key}: K1 strip on supports, elsewhere one rod")).IsTrue();
            var mid = module.BuildOne(simple, plain.Blocks[i], LodLevel.L1, Campus.Contracts.Tests.PaletteFixture.Catalog);
            AssertThat(!mid.Any(b => Strip(b) && OnRoof(b, simple)) && module.BuildOne(full, hero.Blocks[i], LodLevel.L1, Campus.Contracts.Tests.PaletteFixture.Catalog).Any(b => Strip(b) && OnRoof(b, full))).OverrideFailureMessage("Expected: " + ($"{simple.Key}: the rod stops at L0 on simple roofs")).IsTrue();
            // Coping: the game/gen/parts/roof/ cap reaches the parapet top, its drip hangs 60 mm under the slab outside the wall.
            var trim = near.Where(b => HasColor(b, Rules.Materials.Lit(simple.Profile.Trim).Value) && b.Material.Spec.Fx == ToonFx.BevelLift).ToArray();
            var ys = trim.SelectMany(b => ColorVertices(b, Rules.Materials.Lit(simple.Profile.Trim).Value).Select(v => b.Position[3 * v + 1])).ToArray();
            AssertThat(ys.Any(y => MathF.Abs(y - simple.EdgeTopY) < TestTolerance.Position) && ys.Any(y => MathF.Abs(y - (simple.EdgeTopY - 0.21f)) < TestTolerance.Position)).OverrideFailureMessage("Expected: " + ($"{simple.Key}: coping and drip")).IsTrue();
            foreach (var b in near.Where(b => Fence(b) || Pump(b)))
                foreach (var v in ColorVertices(b, Rules.RoofRules.FenceBoard + ".lit", "equip.grey.lit"))
                {
                    var p = new Vector2(b.Position[3 * v], b.Position[3 * v + 2]);
                    AssertThat(PlanRing.InsideFootprint(simple.Outer, simple.Holes, p) && PlanRing.DistanceToEdges(simple.Outer, simple.Holes, p) > 0.4f &&
                        b.Position[3 * v + 1] >= simple.RoofY - TestTolerance.Position).OverrideFailureMessage("Expected: " + ($"{simple.Key}: plant stands on the roof, clear of the parapet")).IsTrue();
                }
        }
    }
}
