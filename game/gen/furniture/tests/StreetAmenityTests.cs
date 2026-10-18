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
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

[TestSuite]
public sealed class StreetAmenityTests
{
    [TestCase]
    public void SignpostsStandAtLandmarkJunctionsOneToALineOfSight()
    {
        // Opus review 13c2b 21:05 (until WP-44's points): a signpost only by a junction of three or more walks with a landmark within
        // 50 m, beside the paving, facing it; no two within 80 m (one to a line of sight). Judged from the network, not by place names.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
        var signs = layout.Items.Where(i => i.Kind == FurnitureKind.Signpost).ToList();
        var junctions = FurniturePlanner.Junctions(site).Where(j => j.Branches >= FurnitureStyle.SignpostBranches).ToList();
        Console.WriteLine($"signposts: {signs.Count} " + string.Join(", ", signs.Select(s => $"{s.Key} ({s.Anchor.X:F1}, {s.Anchor.Y:F1})")) + "; " +
            layout.Diagnostics.Single(d => d.Code == "wp13.signposts.summary").Message);
        foreach (var sign in signs)
        {
            var junction = junctions.MinBy(j => Vector2.Distance(j.At, sign.Anchor));
            Check(Vector2.Distance(junction.At, sign.Anchor) <= FurnitureStyle.SignpostReach + 1f, $"{sign.Key} stands {Vector2.Distance(junction.At, sign.Anchor):F1} m from a junction of three walks");
            var facing = Front(sign.Part.At);
            Check(site.PavingEdgeFacing(sign.Anchor + facing * 0.6f) is { Gap: < 0.3f }, $"{sign.Key} does not face the paving");
            Check(sign.Pad is not null, $"{sign.Key} has no bay");
        }
        for (var a = 0; a < signs.Count; a++)
            for (var b = a + 1; b < signs.Count; b++)
                Check(Vector2.Distance(signs[a].Anchor, signs[b].Anchor) >= FurnitureStyle.SignpostSpacing - 2f * (FurnitureStyle.SignpostReach + 1f), $"{signs[a].Key} and {signs[b].Key} share a line of sight");
    }

    [TestCase]
    public void AJunctionOfThreeWalksByALandmarkTakesOneSignpost()
    {
        // Made up on open ground: three walks meeting at a point, a landmark 20 m off: one signpost at the junction. Without the landmark,
        // none; a second such junction 40 m away (inside the 80 m line of sight) adds none.
        var open = new FakeWorld(World.Value) { Terrain = FlatTerrain.Default };
        var spot = OpenGround(open);
        var props = JsonSerializer.SerializeToElement(new { roadType = "footway", width = 2.5f });
        NetworkLine Walk(string id, Vector2 a, Vector2 b) => new(new($"design:furniture-test-{id}"), new Polyline([a, b]), 2.5f) { Props = props };
        List<NetworkLine> Star(string tag, Vector2 at) => [Walk(tag + "-w", at, at + new Vector2(-14f, 0f)), Walk(tag + "-e", at, at + new Vector2(14f, 0f)), Walk(tag + "-n", at, at + new Vector2(0f, 14f))];
        var landmarkAt = spot + new Vector2(6f, -20f);
        var landmark = new WorldFeature(new("design:furniture-test-landmark"), FeatureSource.Design, new("poi"), new PointGeometry(new Vector3(landmarkAt.X, 0f, landmarkAt.Y)))
        { Props = JsonSerializer.SerializeToElement(new { category = "landscape", subcategory = "landmark" }), Bounds = new(new(landmarkAt.X, 0f, landmarkAt.Y), new(landmarkAt.X, 0f, landmarkAt.Y)) };
        int Signs(IWorldModel world) => FurniturePlanner.Build(world, null, Inputs()).Count(FurnitureKind.Signpost);
        var baseline = Signs(open);
        var one = Signs(MadeWorld(open, Star("a", spot), [landmark]));
        var bare = Signs(MadeWorld(open, Star("a", spot), []));
        var two = Signs(MadeWorld(open, [.. Star("a", spot), .. Star("b", spot + new Vector2(0f, 40f))], [landmark]));
        Console.WriteLine($"made-up junction at {spot}: baseline {baseline}, with a landmark {one}, without {bare}, two junctions {two}");
        Check(one == baseline + 1, $"{one - baseline} signposts at a three-walk junction by a landmark");
        Check(bare == baseline, $"{bare - baseline} signposts at a junction with no landmark near");
        Check(two == baseline + 1, $"{two - baseline} signposts at two junctions 40 m apart");
    }

    [TestCase]
    public void FountainsStandOutsideTheMainEntrancesOfSportsAndTeachingBuildings()
    {
        // Opus review 13c2b 21:05 (until WP-44's points): one drinking fountain a building, by the main entrance of a sports venue or a
        // teaching building (its first open entrance), 10–20 m in front of the door, beside the paving on a bay, facing it.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
        var fountains = layout.Items.Where(i => i.Kind == FurnitureKind.DrinkingFountain).ToList();
        Console.WriteLine($"fountains: {fountains.Count} " + string.Join(", ", fountains.Select(f => $"{f.Key} ({f.Anchor.X:F1}, {f.Anchor.Y:F1})")) + "; " +
            layout.Diagnostics.Single(d => d.Code == "wp13.fountains.summary").Message);
        Check(fountains.Count >= 1, "no drinking fountain on the map");
        var buildings = new HashSet<FeatureId>();
        foreach (var fountain in fountains)
        {
            var doorId = fountain.Key["fountain:".Length..];
            var entry = world.Features.Values.Select(f => world.Interiors.Get(f.Id)).First(e => e is not null && e.Entrances.Any(d => d.Id == doorId))!;
            var kinds = world.Features.Values.Where(f => world.Interiors.Get(f.Id)?.Feature == entry.Feature).Select(f => Text(f.Props, "category") ?? "").ToList();
            Check(kinds.Any(FurnitureStyle.FountainCategories.Contains), $"{fountain.Key} is not by a sports or teaching building ({string.Join(", ", kinds)})");
            Check(buildings.Add(entry.Feature), $"{fountain.Key}: a second fountain for one building");
            var door = entry.Entrances.Where(d => d.Open).OrderBy(d => d.Id, StringComparer.Ordinal).First();
            Check(door.Id == doorId, $"{fountain.Key} is not by the building's main entrance");
            var plan = new Vector2(door.Center.X, door.Center.Z);
            var away = fountain.Anchor - plan;
            Check(away.Length() is >= FurnitureStyle.FountainNear - 1.5f and <= FurnitureStyle.FountainFar + 1.5f, $"{fountain.Key} stands {away.Length():F1} m from its door");
            Check(Vector2.Dot(away, FurniturePlanner.Facing(door.YawDeg)) > 0f, $"{fountain.Key} stands behind its door");
            Check(fountain.Pad is not null && site.PavingEdgeFacing(fountain.Anchor + Front(fountain.Part.At) * 0.6f) is { Gap: < 0.3f }, $"{fountain.Key} is not on a bay facing the paving");
        }
    }

    [TestCase]
    public void VendingLockersAndBaysStandWhereTheyAreUsedAndFew()
    {
        // 13c2c: vending groups of 2–3 at the cluster gates, canteens and sports venues, one to a line of sight and none on the main
        // axis; at most two lockers, each beside a gate's group; bicycle bays apart, their stand-in bikes inside the frame.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
        foreach (var line in layout.Diagnostics.Where(d => d.Code is "wp13.vending.summary" or "wp13.lockers.summary" or "wp13.bicycles.summary").Select(d => d.Message))
            Console.WriteLine(line);
        bool Serves(FurnitureItem item, float front)
        {
            var q = item.Anchor + Front(item.Part.At) * (((BoxPartParams)item.Part.Params).Depth * 0.5f + front + 0.3f);
            return site.PavingEdgeFacing(q) is { Gap: <= 0f } || !site.OffCarriageways(q, 0f, 0f);
        }
        var groups = layout.Items.Where(i => i.Kind == FurnitureKind.VendingMachine).GroupBy(i => i.Key[..i.Key.LastIndexOf(':')]).ToList();
        Check(groups.Count >= 3, $"{groups.Count} vending groups");
        var axis = world.Network.Roads.Where(l => StreetSite.RoadName(l) == FurnitureStyle.Roads.First(r => r.Tier == RoadTier.Main).Name).ToList();
        var centres = new List<(string Site, Vector2 At)>();
        foreach (var group in groups)
        {
            var machines = group.ToList();
            var facing = Front(machines[0].Part.At);
            Check(machines.Count is >= FurnitureStyle.VendingMin and <= FurnitureStyle.VendingMax, $"{group.Key}: {machines.Count} machines");
            Check(machines.Count(m => ((EquipmentParams)m.Part.Params).Variation == 1) <= 1, $"{group.Key}: more than one odd machine");
            Check(machines.All(m => Vector2.Dot(Front(m.Part.At), facing) > 0.999f && MathF.Abs(m.Part.At.M42 - machines[0].Part.At.M42) < 1e-5f), $"{group.Key}: machines not in one row");
            for (var k = 1; k < machines.Count; k++)
                Check(MathF.Abs(Vector2.Distance(machines[k].Anchor, machines[k - 1].Anchor) - FurnitureStyle.Vending.Width - FurnitureStyle.VendingSideGap) < 1e-3f, $"{group.Key}: machines not side by side");
            Check(machines[0].Pad is { } bay && MathF.Abs(bay.Top - machines[0].Part.At.M42) < 1e-4f, $"{group.Key} is off its bay");
            Check(machines.All(m => Serves(m, FurnitureStyle.ServiceFront)), $"{group.Key} does not face the paving or the road");
            var centre = machines.Aggregate(Vector2.Zero, (a, m) => a + m.Anchor) / machines.Count;
            Check(axis.All(l => Distance(centre, l) >= FurnitureStyle.MainAxisClear - FurnitureStyle.ServiceReach - 2f), $"{group.Key} stands on the main axis");
            var siteId = group.Key["vending:".Length..];
            if (world.Features.TryGetValue(new(siteId), out var feature) && feature.Kind.Value == "clusterGate")
            {
                var cluster = world.Features[new(feature.Props!.Value.GetProperty("cluster").GetString()!)];
                var rings = Polygons(cluster).Single();
                foreach (var m in machines)
                    foreach (var corner in Corners(m, FurnitureStyle.ServiceFront))
                        Check(!PlanGeometry.Contains(rings, corner) && PlanGeometry.DistanceToRing(corner, rings.Outer) >= FurnitureStyle.GateOutside - 1e-3f, $"{m.Key} is inside its cluster's fence line");
                var gate = (PointGeometry)feature.Geometry;
                Check(Vector2.Distance(centre, new(gate.Position.X, gate.Position.Z)) <= FurnitureStyle.ServiceReach + 2f, $"{group.Key} strays from its gate");
            }
            centres.Add((siteId, centre));
        }
        for (var a = 0; a < centres.Count; a++)
            for (var b = a + 1; b < centres.Count; b++)
                Check(Vector2.Distance(centres[a].At, centres[b].At) >= FurnitureStyle.VendingSpacing - 2f * (FurnitureStyle.ServiceReach + FurnitureStyle.BayNear), $"{centres[a].Site} and {centres[b].Site} share a line of sight");
        var lockers = layout.Items.Where(i => i.Kind == FurnitureKind.DeliveryLocker).ToList();
        Check(lockers.Count is >= 1 and <= FurnitureStyle.LockerSites, $"{lockers.Count} delivery lockers");
        foreach (var locker in lockers)
        {
            var at = centres.Single(c => c.Site == locker.Key["locker:".Length..]).At;
            Check(Vector2.Distance(at, locker.Anchor) <= 2f * FurnitureStyle.ServiceReach, $"{locker.Key} is not beside its gate's vending group");
            Check(Serves(locker, FurnitureStyle.ServiceFront) && locker.Pad is not null, $"{locker.Key} does not face the paving on its bay");
        }
        var bays = layout.Items.Where(i => i.Kind == FurnitureKind.BicycleBay).ToList();
        Check(bays.Count >= 1, "no bicycle bay");
        for (var a = 0; a < bays.Count; a++)
            for (var b = a + 1; b < bays.Count; b++)
                Check(Vector2.Distance(bays[a].Anchor, bays[b].Anchor) >= FurnitureStyle.BaySpacing - 2f * FurnitureStyle.BayFar, $"{bays[a].Key} and {bays[b].Key} crowd each other");
        foreach (var bay in bays)
        {
            Check(Serves(bay, FurnitureStyle.WalkGap) && bay.Pad is not null, $"{bay.Key} does not open onto the paving on its bay");
            var p = (RoofParams)bay.Part.Params;
            Matrix4x4.Invert(bay.Part.At, out var local);
            var bikes = layout.Items.Where(i => i.Key.StartsWith(bay.Key + ":bike:", StringComparison.Ordinal)).ToList();
            Check(bikes.Count <= FurnitureStyle.BayBikesMax, $"{bay.Key}: {bikes.Count} bikes");
            foreach (var bike in bikes)
            {
                var q = Vector3.Transform(new Vector3(bike.Part.At.M41, bike.Part.At.M42, bike.Part.At.M43), local);
                var bp = (VehicleParams)bike.Part.Params;
                Check(MathF.Abs(q.X) + bp.Width * 0.5f <= p.Width * 0.5f + 1e-3f && MathF.Abs(q.Z) + bp.Depth * 0.5f <= p.Depth * 0.5f + 1e-3f, $"{bike.Key} leaves its bay's frame");
                Check(Vector2.Dot(Front(bike.Part.At), Front(bay.Part.At)) < -0.999f && bike.Colliders.Count == 0, $"{bike.Key}: front wheel not to the back line, or a collider");
            }
        }
    }

    [TestCase]
    public void AClusterGateTakesAVendingGroupOutsideItsFence()
    {
        // Made up on open ground: a 5 m service road running north into a 40 × 36 m cluster, a closed gate where it crosses the outline.
        // The gate takes one vending group (and, as the first closed gate's, a locker) beside the road, outside the cluster, facing the road.
        var open = new FakeWorld(World.Value) { Terrain = FlatTerrain.Default };
        var spot = OpenGround(open);
        var road = new NetworkLine(new("design:furniture-test-drive"), new Polyline([spot + new Vector2(0f, 24f), spot + new Vector2(0f, -30f)]), 5f)
        { Props = JsonSerializer.SerializeToElement(new { highway = "service" }) };
        Vector2[] ring = [spot + new Vector2(-20f, -3f), spot + new Vector2(20f, -3f), spot + new Vector2(20f, -39f), spot + new Vector2(-20f, -39f)];
        var cluster = new WorldFeature(new("design:furniture-test-cluster"), FeatureSource.Design, new("poi"), new PolygonGeometry(new PolygonRings(ring, [])));
        var gateAt = spot + new Vector2(0f, -3f);
        var gate = new WorldFeature(new("design:furniture-test-gate"), FeatureSource.Design, new("clusterGate"), new PointGeometry(new Vector3(gateAt.X, 0f, gateAt.Y)))
        { Props = JsonSerializer.SerializeToElement(new { cluster = "design:furniture-test-cluster", open = false }) };
        var layout = Build(MadeWorld(open, [road], [cluster, gate]));
        var machines = layout.Items.Where(i => i.Key.StartsWith("vending:design:furniture-test-gate:", StringComparison.Ordinal)).ToList();
        Console.WriteLine($"made-up gate at {gateAt}: " + string.Join(", ", machines.Select(m => $"{m.Key} ({m.Anchor.X:F1}, {m.Anchor.Y:F1})")) +
            $"; locker {layout.Items.Any(i => i.Key == "locker:design:furniture-test-gate")}");
        Check(machines.Count is >= FurnitureStyle.VendingMin and <= FurnitureStyle.VendingMax, $"{machines.Count} machines at the made-up gate");
        var rings = new PolygonRings(ring, []);
        foreach (var m in machines)
        {
            foreach (var corner in Corners(m, FurnitureStyle.ServiceFront))
                Check(!PlanGeometry.Contains(rings, corner) && PlanGeometry.DistanceToRing(corner, ring) >= FurnitureStyle.GateOutside - 1e-3f, $"{m.Key} is inside the cluster");
            var facing = Front(m.Part.At);
            Check(MathF.Abs(facing.X) > 0.99f && Vector2.Dot(facing, new Vector2(spot.X - m.Anchor.X, 0f)) > 0f, $"{m.Key} does not face the road");
            Check(Distance(m.Anchor, road) - 2.5f >= FurnitureStyle.ServiceFront + FurnitureStyle.Vending.Depth * 0.5f - 0.05f, $"{m.Key} crowds the road");
        }
        Check(layout.Items.Any(i => i.Key == "locker:design:furniture-test-gate"), "the first closed gate's group has no locker beside it");
    }
}
