using System.Text.Json;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.North.Clusters;
using Campus.Gen.Zones.North.Framework;
using Campus.Gen.Zones.North.Xuezhi;
using GdUnit4;

namespace Campus.Gen.Zones.North.Tests;

using static Campus.Gen.Zones.North.Tests.NorthTestSupport;

[TestSuite]
public sealed class NorthSiteTests
{
    [TestCase]
    public void HallPorchMeetsTheDeclaredEntrance()
    {
        var world = World();
        var porch = HallEntranceSite.Locate(world) ?? throw new InvalidOperationException("学知苑 1 栋 porch not located");
        var door = new Vector2(porch.Entrance.Center.X, porch.Entrance.Center.Z);
        var local = porch.Frame.Local(door);
        Require(MathF.Abs(local.X) < 0.5f && MathF.Abs(local.Y) < 1.2f, $"porch axis passes the door centre (local {local})");
        Require(Vector2.Dot(porch.Frame.W, PlanMath.Heading(porch.Entrance.YawDeg)) > 0.9f, "porch faces the declared facing");
        Require(porch.Risers is >= NorthTune.Hall.MinRisers and <= NorthTune.Hall.MaxRisers, "6–8 risers");
        Require(MathF.Abs(porch.Floor - porch.Entrance.Center.Y) < 0.01f, "landing at the declared floor level");
        var outside = new Vector2(porch.Entrance.Outside.X, porch.Entrance.Outside.Z);
        var landing = porch.Frame.Local(outside);
        Require(landing.Y > NorthTune.Hall.WallFace && landing.Y < porch.FrontZ, "teleport landing lies on the porch landing");
    }

    /// <summary>
    /// 20b1 (D-185 R6): the 硕丰苑十组团 rest deck is the registered polygon, lies inside its cluster, and keeps 8 m off every
    /// wall and 3 m off every road edge, the cluster's design drive included.
    /// </summary>
    [TestCase]
    public void RestDeckSitsInTheShuofengTenCourtyard()
    {
        var world = World();
        var outline = Shuofeng.RestDeckSite.Outline();
        var registered = ((PolygonGeometry)world.Features[new(NorthTune.Deck.Feature)].Geometry).Rings.Outer;
        Require(registered.Count >= 4 && outline.All(p => registered.Any(q => Vector2.Distance(p, q) < 0.02f)), "overlay and tune agree on the deck");
        var cluster = Framework.Plan.OuterRing(world.Features[new("qsh:way:1061419119")])!;
        var walls = world.Features.Values.SelectMany(Framework.Plan.BuildingRings).Where(r => r.Any(q => Vector2.Distance(q, NorthTune.Deck.Centre) < 80f)).ToArray();
        var roads = world.Network.Roads.Select(r => (r.Line.Points, r.Width))
            .Append((((PolylineGeometry)world.Features[new("design:north-drive-sf10")].Geometry).Line.Points, 5f)).ToArray();
        foreach (var p in outline)
        {
            Require(PlanGeometry.InsideByCrossings(cluster, p), $"deck corner {p} is inside 硕丰苑十组团");
            Require(walls.All(r => !PlanGeometry.InsideByCrossings(r, p) && RingDistance(p, r) >= 8f), $"deck corner {p} keeps 8 m off the walls");
            foreach (var (line, width) in roads)
                Require(PolylineDistance(p, line) - width * 0.5f >= 3f, $"deck corner {p} keeps 3 m off a road edge");
        }
    }

    /// <summary>
    /// Main session 18:26 (20b1 ③), applied by 20b2 to 硕丰苑 and 博翰苑 and by 20a-4 (main session 20:58) to 学知苑 and the
    /// international students' cluster: the main gate faces the highest-grade road beside the cluster (within 15 m of its
    /// outline) — 丹桂路 and 银杏大道, then the other 6 m campus roads, then 5 m service roads: it opens within
    /// <see cref="Faces"/> of such a road, or no vehicle gate is nearer one; and its guard house stands on the right of the
    /// way in. 学知一组团's gate is the open walking route (P13, A11, WP-31c) and keeps its place.
    /// </summary>
    [TestCase]
    public void MainGatesFaceTheHighestGradeRoadWithTheHouseOnTheRight()
    {
        var world = World();
        foreach (var cluster in NorthTune.Clusters.Where(c => !c.Open))
        {
            var layout = ClusterLayout.Of(world, cluster)!;
            var ring = Framework.Plan.OuterRing(world.Features[new(cluster.Id)])!;
            var outline = Samples([.. ring, ring[0]]).ToArray();
            var beside = world.Network.Roads.Where(r => outline.Min(p => PolylineDistance(p, r.Line.Points)) < 15f).ToArray();
            var top = beside.Max(RoadGrade);
            var best = beside.Where(r => RoadGrade(r) == top).ToArray();
            var main = layout.MainGate!;
            float ToBest(GateSpot g) => best.Min(r => PolylineDistance(g.Frame.Origin, r.Line.Points));
            var nearest = layout.Gates.Where(g => g.Kind == GateKind.Vehicle).MinBy(ToBest)!;
            Require(ToBest(main) <= MathF.Max(Faces, ToBest(nearest)),
                $"{cluster.Name}: main gate at {main.Frame.Origin} is {ToBest(main):0.0} m from the grade-{top} road; the gate at {nearest.Frame.Origin} is {ToBest(nearest):0.0} m");
            // Walking in faces −W; the right of a facing d is (−d.z, d.x), which for d = −W is +U (PlanFrame: W = (−U.z, U.x)).
            var into = -main.Frame.W;
            var house = main.Frame.P((main.HouseStart + main.HouseEnd) * 0.5f, NorthTune.Gate.HouseOut - NorthTune.Gate.HouseDepth * 0.5f);
            Require(Vector2.Dot(house - main.Frame.Origin, new Vector2(-into.Y, into.X)) > 0, $"{cluster.Name}: guard house on the right of the way in");
        }
    }

    /// <summary>
    /// Every cluster's guard house stands clear of every building footprint (20a-4 moved 学知二、六、八 and the international
    /// students' main gates; the latter two used to run into a dormitory).
    /// </summary>
    [TestCase]
    public void GuardHousesStandClearOfBuildings()
    {
        var world = World();
        var rings = world.Features.Values.SelectMany(Framework.Plan.BuildingRings).ToArray();
        foreach (var cluster in NorthTune.Clusters)
        {
            var g = ClusterLayout.Of(world, cluster)!.MainGate!;
            var house = g.Frame.Rect(g.HouseStart, NorthTune.Gate.HouseOut - NorthTune.Gate.HouseDepth, g.HouseEnd, NorthTune.Gate.HouseOut);
            foreach (var ring in rings.Where(r => r.Any(q => Vector2.Distance(q, g.Frame.Origin) < 150f)))
            {
                var crosses = Enumerable.Range(0, 4).Any(i => Enumerable.Range(0, ring.Count).Any(k =>
                    Framework.Plan.Intersect(house[i], house[(i + 1) % 4], ring[k], ring[(k + 1) % ring.Count]) is not null));
                Require(!crosses && !house.Any(p => PlanGeometry.InsideByCrossings(ring, p)) && !ring.Any(p => PlanGeometry.InsideByCrossings(house, p)),
                    $"{cluster.Name}: guard house at {g.Frame.Origin} overlaps a building");
            }
        }
    }

    /// <summary>
    /// 20a-4: the international students' cluster has one road in, from the north-west corner, and its dormitory corner stands
    /// at the guard house's right hand there; the main gate is on the design drive that joins the 6 m campus road beside it in
    /// the south-east corner, and the north-west gate stays a closed lane without a house.
    /// </summary>
    [TestCase]
    public void InternationalClusterMainGateIsOnItsDesignDrive()
    {
        var world = World();
        var cluster = NorthTune.Clusters.Single(c => c.Key == "intl");
        var drive = ((PolylineGeometry)world.Features[new("design:north-drive-intl")].Geometry).Line.Points;
        var layout = ClusterLayout.Of(world, cluster)!;
        var main = layout.MainGate!;
        Require(PolylineDistance(main.Frame.Origin, drive) < 1.5f, "the main gate stands where the design drive crosses the fence");
        Require(layout.Gates.Count(g => g.Kind == GateKind.Vehicle) == 2, "the drive adds a lane; the north-west lane stays");
        var joins = world.Network.Roads.Single(r => r.Id.Value == "qsh:source:way:630914106");
        Require(PolylineDistance(drive[0], joins.Line.Points) < 0.5f, "the drive starts on the 6 m campus road it joins");
        var walls = world.Features.Values.SelectMany(Framework.Plan.BuildingRings).ToArray();
        foreach (var p in Samples(drive))
            Require(walls.All(r => !PlanGeometry.InsideByCrossings(r, p) && RingDistance(p, r) - 2.5f >= 0.5f), $"drive point {p} keeps its 5 m clear of the walls");
    }

    /// <summary>
    /// 20a-4: the canal-bank railing carries on from the north-gate plaza's railing (WP-30 30a, 04767c7: water edge through
    /// (−75.21, −778.91) toward (0.371, −0.92863), railing at 0.35 m off the water from t −24.6 to −11.4). It starts flush on the
    /// plaza's end post and its collision on the post's centre, panel after panel without a gap or an overlap, runs past the
    /// far end of the street (商业街 reaches t −110.5; the run heads south into the campus, toward negative t), and keeps clear of buildings and road edges.
    /// </summary>
    [TestCase]
    public void CanalRailCarriesOnFromThePlazaRailing()
    {
        var ctx = Context();
        var u = Vector2.Normalize(new Vector2(0.37100f, -0.92863f));
        var w = new Vector2(-u.Y, u.X);
        var joint = new Vector2(-75.21f, -778.91f) + u * -24.6f + w * 0.35f;
        var panels = Canal.CanalRailSite.Panels;
        Require(Vector2.Distance(Canal.CanalRailSite.Post(0), joint) < 0.01f, "the run starts at the plaza's end post");
        var tEnd = Vector2.Dot(Canal.CanalRailSite.Post(panels) - new Vector2(-75.21f, -778.91f), u);
        Require(tEnd < -110.5f - 1f, $"the run ends beyond the far end of 商业街 (t {tEnd})");
        for (var k = 0; k < panels; k++)
        {
            var step = Vector2.Distance(Canal.CanalRailSite.Post(k), Canal.CanalRailSite.Post(k + 1));
            Require(step <= NorthTune.Bank.PostPitch + 0.01f && step > 1f, $"panel {k} is a post pitch long ({step})");
        }
        var walls = new NorthModule().CollidersFor(ctx).Select(i => i.Collider).OfType<WallCollider>()
            .Where(c => MathF.Abs(Vector2.Dot(c.A - joint, w)) < 0.01f && MathF.Abs(Vector2.Dot(c.B - joint, w)) < 0.01f)
            .OrderBy(c => Vector2.Dot(joint - c.A, u)).ToArray();
        Require(walls.Length == panels, $"one collider per panel ({walls.Length} of {panels})");
        Require(Vector2.Distance(walls[0].A, joint) < 0.01f, "the first collider starts on the plaza's end post");
        for (var k = 0; k + 1 < walls.Length; k++)
            Require(Vector2.Distance(walls[k].B, walls[k + 1].A) < 0.01f, $"colliders {k} and {k + 1} meet");
        Require(walls.All(c => c.Y1 - c.Y0 >= WalkRules.BarrierHeight), "the colliders reach 1.5 m");
        var rings = ctx.World.Features.Values.SelectMany(Framework.Plan.BuildingRings).ToArray();
        foreach (var p in Samples([Canal.CanalRailSite.Post(0), Canal.CanalRailSite.Post(panels)]))
        {
            Require(rings.All(r => !PlanGeometry.InsideByCrossings(r, p) && RingDistance(p, r) >= 8f), $"railing point {p} keeps 8 m off the buildings");
            foreach (var road in ctx.World.Network.Roads)
                Require(PolylineDistance(p, road.Line.Points) - road.Width * 0.5f >= 3f, $"railing point {p} keeps 3 m off the road edge of {road.Id.Value}");
        }
    }

    /// <summary>
    /// 20b2: 博翰苑 9 栋 and the WLD-INTERIORS blocks (4499, 5247, 6735) carry their family and preset; an annex preset
    /// names the family it was written for, and it matches.
    /// </summary>
    [TestCase]
    public void LoneBuildingsCarryTheirFamilyAndPreset()
    {
        var world = World();
        var game = Path.GetFullPath(Path.Combine(DataDirectory(), "..", "..", ".."));
        using var table = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(game, "gen", "zones", "north", "DormPresets.json")));
        var presets = table.RootElement.GetProperty("presets").EnumerateArray().ToDictionary(p => p.GetProperty("key").GetString()!);
        foreach (var s in NorthTune.Singles)
        {
            var feature = world.Features[new(s.Id)];
            Require(feature.Family?.Value == s.Family && feature.Facade?.Value == s.Facade, $"{s.Id} ({s.Note}) carries {s.Family} / {s.Facade}");
            if (presets[s.Facade].TryGetProperty("family", out var family))
                Require(family.GetString() == s.Family, $"{s.Facade} was written for {family.GetString()}, not {s.Family}");
        }
    }

    /// <summary>A gate whose origin (on the fence line) lies this close to a road's centreline opens onto it.</summary>
    private const float Faces = 8f;

    private static int RoadGrade(NetworkLine road) => Text(road.Props, "name") is "丹桂路" or "银杏大道" ? 3
        : Text(road.Props, "highway") switch
        {
            "primary" or "secondary" or "tertiary" or "unclassified" or "residential" => 2,
            "service" => 1,
            _ => 0,
        };

    /// <summary>
    /// 20a-3 (Opus 13:32): the seat wall stands at the courtyard walk facing the hill and the lawn, a paved pad joins it to
    /// the walk, and its back keeps 8 m off every dormitory window.
    /// </summary>
    [TestCase]
    public void CourtyardSeatWallFacesTheHillFromTheWalk()
    {
        var world = World();
        var registered = (PolylineGeometry)world.Features[new("design:north-xz1-seatwall")].Geometry;
        Require(registered.Line.Points.SequenceEqual([NorthTune.Court.SeatStart, NorthTune.Court.SeatMid, NorthTune.Court.SeatEnd]), "overlay and tune agree on the seat wall");
        var hill = (TerrainEllipse)Terrain().Ops.OfType<TerrainMoundOp>().Single(o => o.Id == "design:terrain:north-xz1-court-hill").Shape;
        var yaw = hill.YawDeg * Math.PI / 180;
        var path = CourtyardSite.Path(NorthTune.Court.SeatSegments);
        var front = CourtyardSite.Front();
        var mid = (NorthTune.Court.SeatStart + NorthTune.Court.SeatEnd) * 0.5f;
        Require(Vector2.Dot(front, Vector2.Normalize(hill.Center - mid)) > 0.9f, "the seat faces the hill");
        var walk = world.Network.Roads.Single(r => r.Id.Value == NorthTune.Court.Walk);
        float ToWalk(Vector2 p) => Enumerable.Range(0, walk.Line.Points.Count - 1).Min(i => DistanceToSegment(p, walk.Line.Points[i], walk.Line.Points[i + 1]));
        Require(ToWalk(mid + front) < ToWalk(mid), "the walk lies in front of the seat");
        // The pad's straight edge runs along the walk's paving edge, 2 cm short of it, over the whole length of the wall.
        var pad = CourtyardSite.PadOutline();
        var edge = pad.Where(p => MathF.Abs(Vector2.Dot(p - NorthTune.Court.SeatStart, front) - NorthTune.Court.PadReach) < 0.01f).ToArray();
        Require(edge.Length == 2, "the pad has one straight edge toward the walk");
        foreach (var p in edge)
            Require(MathF.Abs(ToWalk(p) - (walk.Width * 0.5f + 0.02f)) < 0.05f, $"pad corner {p} meets the walk's edge");
        var buildings = world.Features.Values.SelectMany(Framework.Plan.BuildingRings)
            .Where(ring => ring.Any(q => Vector2.Distance(q, mid) < 60f)).ToArray();
        Require(buildings.Length > 0, "dormitories around the courtyard found");
        foreach (var p in path)
        {
            double dx = p.X - hill.Center.X, dz = p.Y - hill.Center.Y;
            double along = dx * Math.Sin(yaw) - dz * Math.Cos(yaw), across = dx * Math.Cos(yaw) + dz * Math.Sin(yaw);
            Require(along * along / (hill.Radii.X * hill.Radii.X) + across * across / (hill.Radii.Y * hill.Radii.Y) > 1.2, $"{p} is on level ground off the hill");
            foreach (var road in world.Network.Roads)
                for (var i = 0; i + 1 < road.Line.Points.Count; i++)
                    Require(DistanceToSegment(p, road.Line.Points[i], road.Line.Points[i + 1]) > road.Width * 0.5f + NorthTune.Court.BodyWidth, $"{p} stands off the paving of {road.Id.Value}");
            foreach (var ring in buildings)
                for (var i = 0; i < ring.Count; i++)
                    Require(DistanceToSegment(p, ring[i], ring[(i + 1) % ring.Count]) - NorthTune.Court.BodyWidth * 0.5f >= 8f + NorthTune.Hall.WallFace,
                        $"{p} keeps 8 m off the windows");
        }
    }
}
