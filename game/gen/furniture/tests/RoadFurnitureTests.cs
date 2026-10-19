using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class RoadFurnitureTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void CarsParkSparselyInTheLotsAndKerbRampsMeetTheWalks()
    {
        // WLD-LIFE: a few cars in each car park, whole inside it, noses to its longest edge; never more than the cap a lot.
        var layout = RealLayout.Value;
        var site = Site.Value;
        var cars = layout.Items.Where(i => i.Kind == FurnitureKind.ParkedCar).ToList();
        AssertThat(cars.Count > 0).OverrideFailureMessage("no parked cars").IsTrue();
        foreach (var group in cars.GroupBy(c => c.Key.Split(':')[1]))
            AssertThat(group.Count() <= FurnitureStyle.CarsPerLot).OverrideFailureMessage($"lot {group.Key} holds {group.Count()} cars").IsTrue();
        foreach (var car in cars)
        {
            var p = (VehicleParams)car.Part.Params;
            var lot = site.ParkingLots[int.Parse(car.Key.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture)];
            foreach (var (u, v) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var q = Xf.Point(car.Part.At, new(u * p.Width * 0.5f, 0, v * p.Depth * 0.5f));
                AssertThat(PlanGeometry.Contains(lot, new(q.X, q.Z))).OverrideFailureMessage($"{car.Key} sticks out of its lot").IsTrue();
            }
            AssertThat(car.Colliders.Count == 1 && car.Colliders[0].Foot == FootMat.Metal).OverrideFailureMessage($"{car.Key}: one Metal box (WLD-LIFE)").IsTrue();
            AssertThat(StreetStyle.CarBodies.Any(c => p.Material.Lit.Value == c + ".lit")).OverrideFailureMessage($"{car.Key} is not in a car body colour").IsTrue();
        }
        // Kerb ramps on their authored road, where its walks reach the kerb: on the carriageway against the kerb, rising towards the walk.
        var ramped = Groups.Value.First(g => g.KerbRamps);
        var ramps = layout.Items.Where(i => i.Kind == FurnitureKind.KerbRamp).ToList();
        AssertThat(ramps.All(r => r.Key.StartsWith($"ramp:{ramped.Key}:", StringComparison.Ordinal))).OverrideFailureMessage("a kerb ramp on another road").IsTrue();
        var road = ramped.Lines.Where(StreetSite.Carriageway).ToList();
        foreach (var ramp in ramps)
        {
            var p = (RampParams)ramp.Part.Params;
            var back = ramp.Anchor - Front(ramp.Part.At) * (p.Depth * 0.5f);
            var kerb = road.Min(l => (float)PlanLines.Project(l.Line.Points, back).Distance - StreetSite.HalfWidth(l));
            AssertThat(MathF.Abs(kerb) < TestTolerance.Layout).OverrideFailureMessage($"{ramp.Key}: its top edge is {kerb:F2} m off the kerb").IsTrue();
            AssertThat(road.Min(l => (float)PlanLines.Project(l.Line.Points, ramp.Anchor + Front(ramp.Part.At) * 2f).Distance) < road.Min(l => (float)PlanLines.Project(l.Line.Points, ramp.Anchor).Distance)).OverrideFailureMessage($"{ramp.Key} does not face the road").IsTrue();
        }
        for (var i = 0; i < ramps.Count; i++)
            for (var k = i + 1; k < ramps.Count; k++)
                AssertThat(Vector2.Distance(ramps[i].Anchor, ramps[k].Anchor) >= FurnitureStyle.RampGap - TestTolerance.Position).OverrideFailureMessage($"{ramps[i].Key} and {ramps[k].Key} crowd each other").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void AvenueLampsKeepTheirRhythm()
    {
        // MAP-QSH-ADDITIONS: the avenue's lamps keep a regular pitch; only junctions may nudge one by a step or two.
        var pitch = FurnitureStyle.Rule(RoadTier.Avenue).LampSpacing;
        foreach (var side in new[] { ":l:", ":r:" })
        {
            var row = RealLayout.Value.Items.Where(i => i.Kind == FurnitureKind.StreetLamp && i.Key.StartsWith($"lamp:{Avenue.Key}:0" + side, StringComparison.Ordinal)).ToList();
            var gaps = row.Zip(row.Skip(1), (p, q) => Vector2.Distance(p.Anchor, q.Anchor)).ToList();
            AssertThat(gaps.Count > 0).OverrideFailureMessage($"{gaps.Count} gaps on side {side}").IsTrue();
            var regular = gaps.Count(g => MathF.Abs(g - pitch) <= pitch * 0.15f);
            AssertThat(regular >= gaps.Count * 0.75f).OverrideFailureMessage($"side {side}: only {regular} of {gaps.Count} gaps keep the lamp pitch").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryLitRoadClassHasLampsAndKeepsItsRestRhythm()
    {
        // D-247: lamps, rest stops and bins follow the road class (FurnitureStyle.Rule), whatever the road is called. Judged on properties:
        // nearly every long run of the avenue and the trunk roads that lies inside the campus has a lamp; the rest stops of a side stand at
        // their class's rhythm and keep its distance; the bins number the order of WLD-LIFE-STREET §2's 92 (judged by order of magnitude only).
        const int DocumentedBins = 92;
        var world = World.Value;
        var layout = RealLayout.Value;
        var lamps = layout.Items.Where(i => i.Kind == FurnitureKind.StreetLamp).ToList();
        foreach (var tier in new[] { RoadTier.Avenue, RoadTier.Trunk })
        {
            var rule = FurnitureStyle.Rule(tier);
            var runs = Groups.Value.Where(g => g.Tier == tier)
                .SelectMany(g => StreetSite.Chains(g.Lines).Select(chain => (g.Key, Chain: chain, Reach: g.Lines.Max(StreetSite.HalfWidth) + FurnitureStyle.LampKerbGap + FurnitureStyle.LampMaxShift)))
                .Where(r => new StreetSite.Run(r.Chain).Length >= 3f * rule.LampSpacing && r.Chain.All(p => PlanGeometry.Contains(world.Boundary, p)))
                .ToList();
            AssertThat(runs.Count > 0).OverrideFailureMessage($"no long {tier} run inside the campus").IsTrue();
            var lit = runs.Count(r => lamps.Any(l => l.Key.StartsWith($"lamp:{r.Key}:", StringComparison.Ordinal) && (float)PlanLines.Project(r.Chain, l.Anchor).Distance <= r.Reach));
            AssertThat(lit >= runs.Count * (1f - TestTolerance.Proportion)).OverrideFailureMessage($"{tier}: {lit} of {runs.Count} long runs have a lamp").IsTrue();
        }
        var tiers = Groups.Value.ToDictionary(g => g.Key, g => g.Tier);
        foreach (var side in layout.Items.Where(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith("rest:", StringComparison.Ordinal))
                     .GroupBy(i => string.Join(':', i.Key.Split(':').Take(4))))
        {
            var parts = side.Key.Split(':');
            var rule = FurnitureStyle.Rule(tiers[parts[1]]);
            AssertThat(rule.RestEvery > 0).OverrideFailureMessage($"{side.Key}: rest stops on a class that has none").IsTrue();
            var phase = rule.BothSides && parts[3] == "r" ? rule.RestEvery / 2 : 0;
            var stops = side.Select(i => (K: int.Parse(i.Key.Split(':')[4], System.Globalization.CultureInfo.InvariantCulture), i.Anchor)).OrderBy(s => s.K).ToList();
            AssertThat(stops.All(s => (s.K + phase) % rule.RestEvery == rule.RestEvery / 2)).OverrideFailureMessage($"{side.Key}: a stop off its class's rhythm").IsTrue();
            var arc = rule.RestEvery * rule.LampSpacing;
            for (var i = 1; i < stops.Count; i++)
                if (stops[i].K - stops[i - 1].K == rule.RestEvery)
                {
                    var d = Vector2.Distance(stops[i].Anchor, stops[i - 1].Anchor);
                    AssertThat(d >= arc * 0.5f && d <= arc + 2f * (FurnitureStyle.RestSlide + FurnitureStyle.RestReach)).OverrideFailureMessage($"{side.Key}: stops {d:F0} m apart for a {arc:F0} m rhythm").IsTrue();
                }
        }
        var bins = layout.Count(FurnitureKind.Bin);
        AssertThat(bins >= DocumentedBins / TestTolerance.BudgetFactor && bins <= DocumentedBins * TestTolerance.BudgetFactor).OverrideFailureMessage($"{bins} bins against the documented {DocumentedBins}").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void RoadFurnitureKeepsOffRoadsBuildingsAndWater()
    {
        var world = World.Value;
        var outlines = world.Features.Values.Where(f => f.Kind.Value is "building" or "water").SelectMany(Polygons).ToList();
        foreach (var item in RealLayout.Value.Items.Where(i => i.Kind is FurnitureKind.StreetLamp or FurnitureKind.Bench or FurnitureKind.Bin or FurnitureKind.BusStop))
        {
            var p = item.Anchor;
            AssertThat(PlanGeometry.Contains(world.Boundary, p)).OverrideFailureMessage($"{item.Key} outside the campus").IsTrue();
            AssertThat(!outlines.Any(o => PlanGeometry.Contains(o, p))).OverrideFailureMessage($"{item.Key} inside a building or water").IsTrue();
            foreach (var road in world.Network.Roads)
            {
                var d = MathF.Sqrt(road.Line.Points.Zip(road.Line.Points.Skip(1), (u, v) => PlanLines.SegmentDistanceSquared(p, u, v)).DefaultIfEmpty(float.MaxValue).Min());
                AssertThat(d > MathF.Max(road.Width, 2f) * 0.5f + 0.2f).OverrideFailureMessage($"{item.Key} stands on {road.Id.Value} ({d:F2} m from its centre line)").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AWideWalkMeetingARoadGetsARowOfWhiteStoneSpheresAcrossItsMouth()
    {
        // WLD-LIFE-STREET 路障: a row inside the walk, Setback from the kerb, across it, Pitch apart; cars stop, people still pass.
        var spot = OpenSpot;
        var road = new NetworkLine(new("design:furniture-test-road"), new Polyline([spot + new Vector2(-12f, 0f), spot + new Vector2(12f, 0f)]), 6f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "residential" }) };
        var walk = new NetworkLine(new("design:furniture-test-mouth"), new Polyline([spot + new Vector2(0f, -14f), spot + new Vector2(0f, -1.1f)]), 4f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway", width = 4f }) };
        var layout = FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, MadeWorld(OpenWorld.Value, [road, walk], []), null, Inputs());
        var spheres = layout.Items.Where(i => i.Kind == FurnitureKind.Barrier).OrderBy(i => i.Anchor.X).ToList();
        AssertThat(spheres.Count >= 2).OverrideFailureMessage($"{spheres.Count} spheres across a 4 m walk's mouth").IsTrue();
        var radius = ((PostCollider)spheres[0].Colliders[0]).R;
        foreach (var sphere in spheres)
        {
            AssertThat(sphere.Part.Part.Value == PartCatalog.StoneSphere && ((FurnitureParams)sphere.Part.Params).Variation == 0).OverrideFailureMessage($"{sphere.Key} is not the white default sphere").IsTrue();
            var behindKerb = spot.Y - sphere.Anchor.Y - Half(road);
            AssertThat(behindKerb >= FurnitureStyle.BarrierSetback - TestTolerance.Layout && behindKerb <= FurnitureStyle.BarrierSetback + FurnitureStyle.BarrierStep + TestTolerance.Layout)
                .OverrideFailureMessage($"{sphere.Key} stands {behindKerb:F2} m inside the walk from the kerb").IsTrue();
            AssertThat(MathF.Abs(sphere.Anchor.X - spot.X) + radius <= Half(walk)).OverrideFailureMessage($"{sphere.Key} sticks out of the walk").IsTrue();
        }
        for (var i = 1; i < spheres.Count; i++)
        {
            var pitch = Vector2.Distance(spheres[i].Anchor, spheres[i - 1].Anchor);
            AssertThat(MathF.Abs(pitch - FurnitureStyle.BarrierPitch) < TestTolerance.Layout).OverrideFailureMessage($"spheres {pitch:F2} m apart").IsTrue();
            AssertThat(pitch - 2f * radius >= 0.8f).OverrideFailureMessage($"only {pitch - 2f * radius:F2} m between two spheres: a person cannot pass").IsTrue();
        }
    }
}
