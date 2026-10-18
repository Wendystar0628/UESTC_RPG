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
public sealed class RoofPartTests
{
    [TestCase]
    public void RoofsTakeTheWp12PartsByTheirZone()
    {
        WorldFeature[] features = [Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"), Feature("t:lab", Rect(100, 0, 48, 20), 5, 21f, "research")];
        var plain = BuildingSet.For(World(features), 2u);
        var hero = BuildingSet.For(World(new ZonedFidelity(FidelityZone.K3, FidelityZone.K3, FidelityZone.K1), features), 2u);
        var module = new BuildingsModule();
        static bool Pump(GeometryBundle b) => HasColor(b, "equip.grey.lit");
        // The fence boards in the light metal grey of RoofRules.FenceBoard (the board class: building line, no effects).
        static bool Fence(GeometryBundle b) => HasColor(b, Rules.RoofRules.FenceBoard + ".lit") &&
            b.Material.Spec.Line == LineClass.Building && b.Material.Spec.Fx == ToonFx.None;
        static bool Strip(GeometryBundle b) => HasColor(b, "roof.silverMetal.lit") && b.Material.Spec.Fx.HasFlag(ToonFx.Rim);
        static bool Tank(GeometryBundle b) => HasColor(b, "roof.silverMetal.lit") && b.Material.Spec.Fx == ToonFx.BevelLift;
        static int Tris(IEnumerable<GeometryBundle> bs, string token) => bs.Sum(b => Enumerable.Range(0, b.Index.Length / 3).Count(t => TriangleColor(b, t * 3, token)));
        for (var i = 0; i < plain.Plans.Count; i++)
        {
            var (simple, full) = (plain.Plans[i], hero.Plans[i]);
            Require(simple.RoofZone == FidelityZone.K4 && full.RoofZone == FidelityZone.K1, "roof zone from the fidelity data, K4 without it");
            var near = module.BuildOne(simple, plain.Blocks[i], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
            var nearFull = module.BuildOne(full, hero.Blocks[i], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
            Require(near.Any(Fence) && near.Any(Pump) && near.Any(Strip), $"{simple.Key}: fenced plant and lightning strip");
            Require(!near.Concat(nearFull).Any(b => HasColor(b, "equip.fence.lit") && b.Material.Spec.Line == LineClass.Building),
                $"{simple.Key}: no dark fence steel left on the boards");
            Require((simple.Style == Rules.BuildingStyle.Research) == near.Any(Tank), $"{simple.Key}: a water tank beside teaching plant only");
            Require(Tris(nearFull.Where(Pump), "equip.grey.lit") > Tris(near.Where(Pump), "equip.grey.lit"), $"{simple.Key}: a K1 roof carries more and finer units");
            Require(Tris(nearFull.Where(Strip), "roof.silverMetal.lit") > Tris(near.Where(Strip), "roof.silverMetal.lit"), $"{simple.Key}: K1 strip on supports, elsewhere one rod");
            var mid = module.BuildOne(simple, plain.Blocks[i], LodLevel.L1, Campus.Contracts.Tests.PaletteFixture.Catalog);
            Require(!mid.Any(Strip) && module.BuildOne(full, hero.Blocks[i], LodLevel.L1, Campus.Contracts.Tests.PaletteFixture.Catalog).Any(Strip), $"{simple.Key}: the rod stops at L0 on simple roofs");
            // Coping: the WP-12 cap reaches the parapet top, its drip hangs 60 mm under the slab outside the wall.
            var trim = near.Where(b => HasColor(b, Rules.Materials.Lit(simple.Profile.Trim).Value) && b.Material.Spec.Fx == ToonFx.BevelLift).ToArray();
            var ys = trim.SelectMany(b => ColorVertices(b, Rules.Materials.Lit(simple.Profile.Trim).Value).Select(v => b.Position[3 * v + 1])).ToArray();
            Require(ys.Any(y => MathF.Abs(y - simple.EdgeTopY) < 0.01f) && ys.Any(y => MathF.Abs(y - (simple.EdgeTopY - 0.21f)) < 0.01f), $"{simple.Key}: coping and drip");
            foreach (var b in near.Where(b => Fence(b) || Pump(b)))
                foreach (var v in ColorVertices(b, Rules.RoofRules.FenceBoard + ".lit", "equip.grey.lit"))
                {
                    var p = new Vector2(b.Position[3 * v], b.Position[3 * v + 2]);
                    Require(PlanRing.InsideFootprint(simple.Outer, simple.Holes, p) && PlanRing.DistanceToEdges(simple.Outer, simple.Holes, p) > 0.4f &&
                        b.Position[3 * v + 1] >= simple.RoofY - 0.01f, $"{simple.Key}: plant stands on the roof, clear of the parapet");
                }
        }
    }
}
