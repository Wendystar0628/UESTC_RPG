using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class StreetAmenityTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void SignpostsStandAtLandmarkJunctionsOneToALineOfSight()
    {
        // Opus review 13c2b 21:05 (until WP-44's points): a signpost only by a junction of three or more walks with a landmark within
        // 50 m, beside the paving, facing it; no two within 80 m (one to a line of sight). Judged from the network, not by place names.
        var world = World.Value;
        var layout = RealLayout.Value;
        var site = Site.Value;
        var signs = layout.Items.Where(i => i.Kind == FurnitureKind.Signpost).ToList();
        var junctions = FurniturePlanner.Junctions(site).Where(j => j.Branches >= FurnitureStyle.SignpostBranches).ToList();
        foreach (var sign in signs)
        {
            var junction = junctions.MinBy(j => Vector2.Distance(j.At, sign.Anchor));
            AssertThat(Vector2.Distance(junction.At, sign.Anchor) <= FurnitureStyle.SignpostReach + 1f).OverrideFailureMessage($"{sign.Key} stands {Vector2.Distance(junction.At, sign.Anchor):F1} m from a junction of three walks").IsTrue();
            var facing = Front(sign.Part.At);
            AssertThat(site.PavingEdgeFacing(sign.Anchor + facing * 0.6f) is { Gap: < 0.3f }).OverrideFailureMessage($"{sign.Key} does not face the paving").IsTrue();
            AssertThat(sign.Pad is not null).OverrideFailureMessage($"{sign.Key} has no bay").IsTrue();
        }
        for (var a = 0; a < signs.Count; a++)
            for (var b = a + 1; b < signs.Count; b++)
                AssertThat(Vector2.Distance(signs[a].Anchor, signs[b].Anchor) >= FurnitureStyle.SignpostSpacing - 2f * (FurnitureStyle.SignpostReach + 1f)).OverrideFailureMessage($"{signs[a].Key} and {signs[b].Key} share a line of sight").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AJunctionOfThreeWalksByALandmarkTakesOneSignpost()
    {
        // Made up on open ground: three walks meeting at a point, a landmark 20 m off: one signpost at the junction. Without the landmark,
        // none; a second such junction 40 m away (inside the 80 m line of sight) adds none.
        var open = OpenWorld.Value;
        var spot = OpenSpot;
        var props = JsonSerializer.SerializeToElement(new { highway = "footway", width = 2.5f });
        NetworkLine Walk(string id, Vector2 a, Vector2 b) => new(new($"design:furniture-test-{id}"), new Polyline([a, b]), 2.5f) { Props = props };
        List<NetworkLine> Star(string tag, Vector2 at) => [Walk(tag + "-w", at, at + new Vector2(-14f, 0f)), Walk(tag + "-e", at, at + new Vector2(14f, 0f)), Walk(tag + "-n", at, at + new Vector2(0f, 14f))];
        var landmarkAt = spot + new Vector2(6f, -20f);
        var landmark = WorldFeatureFixture.Create("design:furniture-test-landmark", "poi", new PointGeometry(new Vector3(landmarkAt.X, 0f, landmarkAt.Y)), JsonSerializer.Serialize(new { category = "landscape", subcategory = "landmark" }));
        int Signs(IWorldModel world) => FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, world, null, Inputs()).Count(FurnitureKind.Signpost);
        var baseline = Signs(open);
        var one = Signs(MadeWorld(open, Star("a", spot), [landmark]));
        var bare = Signs(MadeWorld(open, Star("a", spot), []));
        var two = Signs(MadeWorld(open, [.. Star("a", spot), .. Star("b", spot + new Vector2(0f, 40f))], [landmark]));
        AssertThat(one == baseline + 1).OverrideFailureMessage($"{one - baseline} signposts at a three-walk junction by a landmark").IsTrue();
        AssertThat(bare == baseline).OverrideFailureMessage($"{bare - baseline} signposts at a junction with no landmark near").IsTrue();
        AssertThat(two == baseline + 1).OverrideFailureMessage($"{two - baseline} signposts at two junctions 40 m apart").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void FountainsStandOutsideTheMainEntrancesOfSportsAndTeachingBuildings()
    {
        // Opus review 13c2b 21:05 (until WP-44's points): one drinking fountain a building, by the main entrance of a sports venue or a
        // teaching building (its first open entrance), 10–20 m in front of the door, beside the paving on a bay, facing it.
        var world = World.Value;
        var layout = RealLayout.Value;
        var site = Site.Value;
        var fountains = layout.Items.Where(i => i.Kind == FurnitureKind.DrinkingFountain).ToList();
        AssertThat(fountains.Count >= 1).OverrideFailureMessage("no drinking fountain on the map").IsTrue();
        var buildings = new HashSet<FeatureId>();
        foreach (var fountain in fountains)
        {
            var doorId = fountain.Key["fountain:".Length..];
            var entry = world.Features.Values.Select(f => world.Interiors.Get(f.Id)).First(e => e is not null && e.Entrances.Any(d => d.Id == doorId))!;
            var kinds = world.Features.Values.Where(f => world.Interiors.Get(f.Id)?.Feature == entry.Feature).Select(f => WorldProps.TryText(f.Props, "category") ?? "").ToList();
            AssertThat(kinds.Any(FurnitureStyle.FountainCategories.Contains)).OverrideFailureMessage($"{fountain.Key} is not by a sports or teaching building ({string.Join(", ", kinds)})").IsTrue();
            AssertThat(buildings.Add(entry.Feature)).OverrideFailureMessage($"{fountain.Key}: a second fountain for one building").IsTrue();
            var door = entry.Entrances.Where(d => d.Open).OrderBy(d => d.Id, StringComparer.Ordinal).First();
            AssertThat(door.Id == doorId).OverrideFailureMessage($"{fountain.Key} is not by the building's main entrance").IsTrue();
            var plan = new Vector2(door.Center.X, door.Center.Z);
            var away = fountain.Anchor - plan;
            AssertThat(away.Length() is >= FurnitureStyle.FountainNear - 1.5f and <= FurnitureStyle.FountainFar + 1.5f).OverrideFailureMessage($"{fountain.Key} stands {away.Length():F1} m from its door").IsTrue();
            AssertThat(Vector2.Dot(away, Angles.Heading(door.YawDeg)) > 0f).OverrideFailureMessage($"{fountain.Key} stands behind its door").IsTrue();
            AssertThat(fountain.Pad is not null && site.PavingEdgeFacing(fountain.Anchor + Front(fountain.Part.At) * 0.6f) is { Gap: < 0.3f }).OverrideFailureMessage($"{fountain.Key} is not on a bay facing the paving").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void VendingLockersAndBaysStandWhereTheyAreUsedAndFew()
    {
        // 13c2c: vending groups of 2–3 at the cluster gates, canteens and sports venues, one to a line of sight and none on the main
        // axis; at most two lockers, each beside a gate's group; bicycle bays apart, their stand-in bikes inside the frame.
        var world = World.Value;
        var layout = RealLayout.Value;
        var site = Site.Value;
        bool Serves(FurnitureItem item, float front)
        {
            var q = item.Anchor + Front(item.Part.At) * (((BoxPartParams)item.Part.Params).Depth * 0.5f + front + 0.3f);
            return site.PavingEdgeFacing(q) is { Gap: <= 0f } || !site.OffCarriageways(q, 0f, 0f);
        }
        var groups = layout.Items.Where(i => i.Kind == FurnitureKind.VendingMachine).GroupBy(i => i.Key[..i.Key.LastIndexOf(':')]).ToList();
        AssertThat(groups.Count > 0).OverrideFailureMessage($"{groups.Count} vending groups").IsTrue();
        var axis = Avenue.Lines;
        var centres = new List<(string Site, Vector2 At)>();
        foreach (var group in groups)
        {
            var machines = group.ToList();
            var facing = Front(machines[0].Part.At);
            AssertThat(machines.Count is >= FurnitureStyle.VendingMin and <= FurnitureStyle.VendingMax).OverrideFailureMessage($"{group.Key}: {machines.Count} machines").IsTrue();
            AssertThat(machines.Count(m => ((EquipmentParams)m.Part.Params).Variation == 1) <= 1).OverrideFailureMessage($"{group.Key}: more than one odd machine").IsTrue();
            AssertThat(machines.All(m => Vector2.Dot(Front(m.Part.At), facing) > 0.999f && MathF.Abs(m.Part.At.M42 - machines[0].Part.At.M42) < TestTolerance.Position)).OverrideFailureMessage($"{group.Key}: machines not in one row").IsTrue();
            for (var k = 1; k < machines.Count; k++)
                AssertThat(MathF.Abs(Vector2.Distance(machines[k].Anchor, machines[k - 1].Anchor) - FurnitureStyle.Vending.Width - FurnitureStyle.VendingSideGap) < TestTolerance.Position).OverrideFailureMessage($"{group.Key}: machines not side by side").IsTrue();
            AssertThat(machines[0].Pad is { } bay && MathF.Abs(bay.Top - machines[0].Part.At.M42) < TestTolerance.Position).OverrideFailureMessage($"{group.Key} is off its bay").IsTrue();
            AssertThat(machines.All(m => Serves(m, FurnitureStyle.ServiceFront))).OverrideFailureMessage($"{group.Key} does not face the paving or the road").IsTrue();
            var centre = machines.Aggregate(Vector2.Zero, (a, m) => a + m.Anchor) / machines.Count;
            AssertThat(axis.All(l => (float)PlanLines.Project(l.Line.Points, centre).Distance >= FurnitureStyle.MainAxisClear - FurnitureStyle.ServiceReach - 2f)).OverrideFailureMessage($"{group.Key} stands on the main axis").IsTrue();
            var siteId = group.Key["vending:".Length..];
            if (world.Features.TryGetValue(new(siteId), out var feature) && feature.Kind.Value == "clusterGate")
            {
                var cluster = world.Features[new(feature.Props!.Value.GetProperty("cluster").GetString()!)];
                var rings = Polygons(cluster).Single();
                foreach (var m in machines)
                    foreach (var corner in Corners(m, FurnitureStyle.ServiceFront))
                        AssertThat(!PlanGeometry.Contains(rings, corner) && PlanGeometry.DistanceToRing(corner, rings.Outer) >= FurnitureStyle.GateOutside - TestTolerance.Position).OverrideFailureMessage($"{m.Key} is inside its cluster's fence line").IsTrue();
                var gate = (PointGeometry)feature.Geometry;
                AssertThat(Vector2.Distance(centre, new(gate.Position.X, gate.Position.Z)) <= FurnitureStyle.ServiceReach + 2f).OverrideFailureMessage($"{group.Key} strays from its gate").IsTrue();
            }
            centres.Add((siteId, centre));
        }
        for (var a = 0; a < centres.Count; a++)
            for (var b = a + 1; b < centres.Count; b++)
                AssertThat(Vector2.Distance(centres[a].At, centres[b].At) >= FurnitureStyle.VendingSpacing - 2f * (FurnitureStyle.ServiceReach + FurnitureStyle.BayNear)).OverrideFailureMessage($"{centres[a].Site} and {centres[b].Site} share a line of sight").IsTrue();
        var lockers = layout.Items.Where(i => i.Kind == FurnitureKind.DeliveryLocker).ToList();
        AssertThat(lockers.Count is >= 1 and <= FurnitureStyle.LockerSites).OverrideFailureMessage($"{lockers.Count} delivery lockers").IsTrue();
        foreach (var locker in lockers)
        {
            var at = centres.Single(c => c.Site == locker.Key["locker:".Length..]).At;
            AssertThat(Vector2.Distance(at, locker.Anchor) <= 2f * FurnitureStyle.ServiceReach).OverrideFailureMessage($"{locker.Key} is not beside its gate's vending group").IsTrue();
            AssertThat(Serves(locker, FurnitureStyle.ServiceFront) && locker.Pad is not null).OverrideFailureMessage($"{locker.Key} does not face the paving on its bay").IsTrue();
        }
        var bays = layout.Items.Where(i => i.Kind == FurnitureKind.BicycleBay).ToList();
        AssertThat(bays.Count >= 1).OverrideFailureMessage("no bicycle bay").IsTrue();
        for (var a = 0; a < bays.Count; a++)
            for (var b = a + 1; b < bays.Count; b++)
                AssertThat(Vector2.Distance(bays[a].Anchor, bays[b].Anchor) >= FurnitureStyle.BaySpacing - 2f * FurnitureStyle.BayFar).OverrideFailureMessage($"{bays[a].Key} and {bays[b].Key} crowd each other").IsTrue();
        foreach (var bay in bays)
        {
            AssertThat(Serves(bay, FurnitureStyle.WalkGap) && bay.Pad is not null).OverrideFailureMessage($"{bay.Key} does not open onto the paving on its bay").IsTrue();
            var p = (RoofParams)bay.Part.Params;
            Matrix4x4.Invert(bay.Part.At, out var local);
            var bikes = layout.Items.Where(i => i.Key.StartsWith(bay.Key + ":bike:", StringComparison.Ordinal)).ToList();
            AssertThat(bikes.Count <= FurnitureStyle.BayBikesMax).OverrideFailureMessage($"{bay.Key}: {bikes.Count} bikes").IsTrue();
            foreach (var bike in bikes)
            {
                var q = Vector3.Transform(new Vector3(bike.Part.At.M41, bike.Part.At.M42, bike.Part.At.M43), local);
                var bp = (VehicleParams)bike.Part.Params;
                AssertThat(MathF.Abs(q.X) + bp.Width * 0.5f <= p.Width * 0.5f + TestTolerance.Position && MathF.Abs(q.Z) + bp.Depth * 0.5f <= p.Depth * 0.5f + TestTolerance.Position).OverrideFailureMessage($"{bike.Key} leaves its bay's frame").IsTrue();
                AssertThat(Vector2.Dot(Front(bike.Part.At), Front(bay.Part.At)) < -0.999f && bike.Colliders.Count == 0).OverrideFailureMessage($"{bike.Key}: front wheel not to the back line, or a collider").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AClusterGateTakesAVendingGroupOutsideItsFence()
    {
        // Made up on open ground: a 5 m service road running north into a 40 × 36 m cluster, a closed gate where it crosses the outline.
        // The gate takes one vending group (and, as the first closed gate's, a locker) beside the road, outside the cluster, facing the road.
        var open = OpenWorld.Value;
        var spot = OpenSpot;
        var road = new NetworkLine(new("design:furniture-test-drive"), new Polyline([spot + new Vector2(0f, 24f), spot + new Vector2(0f, -30f)]), 5f)
        { Props = JsonSerializer.SerializeToElement(new { highway = "service" }) };
        Vector2[] ring = [spot + new Vector2(-20f, -3f), spot + new Vector2(20f, -3f), spot + new Vector2(20f, -39f), spot + new Vector2(-20f, -39f)];
        var cluster = WorldFeatureFixture.Create("design:furniture-test-cluster", "poi", new PolygonGeometry(new PolygonRings(ring, [])));
        var gateAt = spot + new Vector2(0f, -3f);
        var gate = WorldFeatureFixture.Create("design:furniture-test-gate", "clusterGate", new PointGeometry(new Vector3(gateAt.X, 0f, gateAt.Y)), JsonSerializer.Serialize(new { cluster = "design:furniture-test-cluster", open = false }));
        var layout = Build(MadeWorld(open, [road], [cluster, gate]));
        var machines = layout.Items.Where(i => i.Key.StartsWith("vending:design:furniture-test-gate:", StringComparison.Ordinal)).ToList();
        AssertThat(machines.Count is >= FurnitureStyle.VendingMin and <= FurnitureStyle.VendingMax).OverrideFailureMessage($"{machines.Count} machines at the made-up gate").IsTrue();
        var rings = new PolygonRings(ring, []);
        foreach (var m in machines)
        {
            foreach (var corner in Corners(m, FurnitureStyle.ServiceFront))
                AssertThat(!PlanGeometry.Contains(rings, corner) && PlanGeometry.DistanceToRing(corner, ring) >= FurnitureStyle.GateOutside - TestTolerance.Position).OverrideFailureMessage($"{m.Key} is inside the cluster").IsTrue();
            var facing = Front(m.Part.At);
            AssertThat(MathF.Abs(facing.X) > 0.99f && Vector2.Dot(facing, new Vector2(spot.X - m.Anchor.X, 0f)) > 0f).OverrideFailureMessage($"{m.Key} does not face the road").IsTrue();
            AssertThat((float)PlanLines.Project(road.Line.Points, m.Anchor).Distance - 2.5f >= FurnitureStyle.ServiceFront + FurnitureStyle.Vending.Depth * 0.5f - TestTolerance.Layout).OverrideFailureMessage($"{m.Key} crowds the road").IsTrue();
        }
        AssertThat(layout.Items.Any(i => i.Key == "locker:design:furniture-test-gate")).OverrideFailureMessage("the first closed gate's group has no locker beside it").IsTrue();
    }
}
