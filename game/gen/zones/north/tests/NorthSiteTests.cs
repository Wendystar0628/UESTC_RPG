using System.Numerics;
using Campus.Contracts;
using Campus.Tests;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.North.Clusters;
using Campus.Gen.Zones.North.Xuezhi;
using GdUnit4;

namespace Campus.Gen.Zones.North.Tests;

using static GdUnit4.Assertions;
using static Campus.Gen.Zones.North.Tests.NorthTestSupport;

[TestSuite]
public sealed class NorthSiteTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void HallPorchMeetsTheDeclaredEntrance()
    {
        var world = RealMapFixture.Load().World;
        var porch = HallEntranceSite.Locate(world) ?? throw new InvalidOperationException("学知苑 1 栋 porch not located");
        var door = new Vector2(porch.Entrance.Center.X, porch.Entrance.Center.Z);
        var local = porch.Frame.Local(door);
        AssertThat(MathF.Abs(local.X) < 0.5f && MathF.Abs(local.Y) < 1.2f).OverrideFailureMessage($"porch axis passes the door centre (local {local})").IsTrue();
        AssertThat(Vector2.Dot(porch.Frame.W, Angles.Heading(porch.Entrance.YawDeg)) > 0.9f).OverrideFailureMessage("porch faces the declared facing").IsTrue();
        AssertThat(porch.Risers is >= NorthTune.Hall.MinRisers and <= NorthTune.Hall.MaxRisers).OverrideFailureMessage("6–8 risers").IsTrue();
        AssertThat(MathF.Abs(porch.Floor - porch.Entrance.Center.Y) < TestTolerance.Position).OverrideFailureMessage("landing at the declared floor level").IsTrue();
        var outside = new Vector2(porch.Entrance.Outside.X, porch.Entrance.Outside.Z);
        var landing = porch.Frame.Local(outside);
        AssertThat(landing.Y > NorthTune.Hall.WallFace && landing.Y < porch.FrontZ).OverrideFailureMessage("teleport landing lies on the porch landing").IsTrue();
    }

    /// <summary>
    /// 20b1 (D-185 R6): the 硕丰苑十组团 rest deck is the registered polygon, lies inside its cluster, and keeps 8 m off every
    /// wall and 3 m off every road edge, the cluster's design drive included.
    /// </summary>
    [TestCase, TestCategory(TestReason.Contract)]
    public void RestDeckSitsInTheShuofengTenCourtyard()
    {
        var world = RealMapFixture.Load().World;
        var deck = Shuofeng.RestDeckSite.Locate(world) ?? throw new InvalidOperationException("rest deck not located");
        var outline = deck.Outline();
        var cluster = world.Feature(NorthFeatureIds.ClusterSf10.Entity).Contour(facade: false);
        var walls = world.Features.Values.SelectMany(NorthBuildings.Rings).Where(r => r.Any(q => Vector2.Distance(q, deck.Frame.Origin) < 80f)).ToArray();
        var roads = world.Network.Roads.Select(r => (r.Line.Points, r.Width))
            .Append((world.Feature(NorthFeatureIds.DriveSf10).Line(), world.Feature(NorthFeatureIds.DriveSf10).Number("width"))).ToArray();
        foreach (var p in outline)
        {
            AssertThat(PlanGeometry.InsideByCrossings(cluster, p)).OverrideFailureMessage($"deck corner {p} is inside 硕丰苑十组团").IsTrue();
            AssertThat(walls.All(r => !PlanGeometry.InsideByCrossings(r, p) && PlanGeometry.DistanceToRing(p, r) >= 8f)).OverrideFailureMessage($"deck corner {p} keeps 8 m off the walls").IsTrue();
            foreach (var (line, width) in roads)
                AssertThat(MathF.Sqrt(PlanLines.PolylineDistanceSquared(line, p)) - width * 0.5f >= 3f).OverrideFailureMessage($"deck corner {p} keeps 3 m off a road edge").IsTrue();
        }
    }

    /// <summary>
    /// Main session 18:26 (20b1 ③), applied by 20b2 to 硕丰苑 and 博翰苑 and by 20a-4 (main session 20:58) to 学知苑 and the
    /// international students' cluster: the main gate faces the highest-grade road beside the cluster (within 15 m of its
    /// outline) — 丹桂路 and 银杏大道, then the other 6 m campus roads, then 5 m service roads: it opens within
    /// <see cref="Faces"/> of such a road, or no vehicle gate is nearer one; and its guard house stands on the right of the
    /// way in. 学知一组团's gate is the open walking route (P13, A11, WP-31c) and keeps its place.
    /// </summary>
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void MainGatesFaceTheHighestGradeRoadWithTheHouseOnTheRight()
    {
        var world = RealMapFixture.Load().World;
        foreach (var cluster in NorthTune.Clusters)
        {
            var layout = ClusterLayout.Of(world, cluster)!;
            if (layout.MainGate!.Open) continue;
            var ring = world.Feature(cluster.Id).Contour(facade: false);
            var outline = Samples([.. ring, ring[0]]).ToArray();
            var beside = world.Network.Roads.Where(r => outline.Min(p => MathF.Sqrt(PlanLines.PolylineDistanceSquared(r.Line.Points, p))) < 15f).ToArray();
            var top = beside.Max(RoadGrade);
            var best = beside.Where(r => RoadGrade(r) == top).ToArray();
            var main = layout.MainGate!;
            float ToBest(GateSpot g) => best.Min(r => MathF.Sqrt(PlanLines.PolylineDistanceSquared(r.Line.Points, g.Frame.Origin)));
            var nearest = layout.Gates.Where(g => g.Kind == GateKind.Vehicle).MinBy(ToBest)!;
            AssertThat(ToBest(main) <= MathF.Max(Faces, ToBest(nearest))).OverrideFailureMessage($"{cluster.Key}: main gate at {main.Frame.Origin} is {ToBest(main):0.0} m from the grade-{top} road; the gate at {nearest.Frame.Origin} is {ToBest(nearest):0.0} m").IsTrue();
            // Walking in faces −W; the right of a facing d is (−d.z, d.x), which for d = −W is +U (PlanFrame: W = (−U.z, U.x)).
            var into = -main.Frame.W;
            var house = main.Frame.P((main.HouseStart + main.HouseEnd) * 0.5f, NorthTune.Gate.HouseOut - NorthTune.Gate.HouseDepth * 0.5f);
            AssertThat(Vector2.Dot(house - main.Frame.Origin, new Vector2(-into.Y, into.X)) > 0).OverrideFailureMessage($"{cluster.Key}: guard house on the right of the way in").IsTrue();
        }
    }

    /// <summary>
    /// Every cluster's guard house stands clear of every building footprint (20a-4 moved 学知二、六、八 and the international
    /// students' main gates; the latter two used to run into a dormitory).
    /// </summary>
    [TestCase, TestCategory(TestReason.Regression)]
    public void GuardHousesStandClearOfBuildings()
    {
        var world = RealMapFixture.Load().World;
        var rings = world.Features.Values.SelectMany(NorthBuildings.Rings).ToArray();
        foreach (var cluster in NorthTune.Clusters)
        {
            var g = ClusterLayout.Of(world, cluster)!.MainGate!;
            var house = g.Frame.Rect(g.HouseStart, NorthTune.Gate.HouseOut - NorthTune.Gate.HouseDepth, g.HouseEnd, NorthTune.Gate.HouseOut);
            foreach (var ring in rings.Where(r => r.Any(q => Vector2.Distance(q, g.Frame.Origin) < 150f)))
            {
                var crosses = Enumerable.Range(0, 4).Any(i => Enumerable.Range(0, ring.Count).Any(k =>
                    PlanLines.SegmentIntersection(house[i], house[(i + 1) % 4], ring[k], ring[(k + 1) % ring.Count], 1e-9f) is not null));
                AssertThat(!crosses && !house.Any(p => PlanGeometry.InsideByCrossings(ring, p)) && !ring.Any(p => PlanGeometry.InsideByCrossings(house, p))).OverrideFailureMessage($"{cluster.Key}: guard house at {g.Frame.Origin} overlaps a building").IsTrue();
            }
        }
    }

    /// <summary>
    /// 20a-4: the international students' cluster has one road in, from the north-west corner, and its dormitory corner stands
    /// at the guard house's right hand there; the main gate is on the design drive that joins the 6 m campus road beside it in
    /// the south-east corner, and the north-west gate stays a closed lane without a house.
    /// </summary>
    [TestCase, TestCategory(TestReason.Contract)]
    public void InternationalClusterMainGateIsOnItsDesignDrive()
    {
        var world = RealMapFixture.Load().World;
        var cluster = NorthTune.Clusters.Single(c => c.Key == "intl");
        var driveFeature = world.Feature(NorthFeatureIds.DriveInternational);
        var drive = driveFeature.Line();
        var driveHalf = driveFeature.Number("width") * 0.5f;
        var layout = ClusterLayout.Of(world, cluster)!;
        var main = layout.MainGate!;
        AssertThat(MathF.Sqrt(PlanLines.PolylineDistanceSquared(drive, main.Frame.Origin)) < 1.5f).OverrideFailureMessage("the main gate stands where the design drive crosses the fence").IsTrue();
        AssertThat(layout.Gates.Any(g => g.Kind == GateKind.Vehicle && !g.Main && !g.Open)).OverrideFailureMessage("the drive adds a lane; the north-west lane stays").IsTrue();
        var joins = world.Network.Roads.Single(r => r.Id.Value == NorthFeatureIds.InternationalDriveJoin.Source);
        AssertThat(MathF.Sqrt(PlanLines.PolylineDistanceSquared(joins.Line.Points, drive[0])) < 0.5f).OverrideFailureMessage("the drive starts on the 6 m campus road it joins").IsTrue();
        var walls = world.Features.Values.SelectMany(NorthBuildings.Rings).ToArray();
        foreach (var p in Samples(drive))
            AssertThat(walls.All(r => !PlanGeometry.InsideByCrossings(r, p) && PlanGeometry.DistanceToRing(p, r) - driveHalf >= 0.5f)).OverrideFailureMessage($"drive point {p} keeps its 5 m clear of the walls").IsTrue();
    }

    /// <summary>
    /// 20a-4: the canal-bank railing carries on from the north-gate plaza's railing (WP-30 30a: the same water edge, railing
    /// at 0.35 m off the water from t −24.6 to −11.4). It starts flush on the
    /// plaza's end post and its collision on the post's centre, panel after panel without a gap or an overlap, runs past the
    /// far end of the street (商业街 reaches t −110.5; the run heads south into the campus, toward negative t), and keeps clear of buildings and road edges.
    /// </summary>
    [TestCase, TestCategory(TestReason.Contract)]
    public void CanalRailCarriesOnFromThePlazaRailing()
    {
        var ctx = Context();
        var frame = ctx.World.ContourFrame(Campus.Gen.Zones.DesignBoundary.BoundaryFeatureIds.NorthBankContour, originAtEnd: false, forward: false);
        var u = frame.U;
        var w = frame.W;
        var joint = frame.Origin + u * NorthTune.Bank.JointT + w * NorthTune.Bank.RailN;
        var panels = Canal.CanalRailSite.Panels;
        AssertThat(Vector2.Distance(Canal.CanalRailSite.Post(frame, 0), joint) < 5 * PlanGeometry.Tolerance).OverrideFailureMessage("the run starts at the plaza's end post").IsTrue();
        var tEnd = Vector2.Dot(Canal.CanalRailSite.Post(frame, panels) - frame.Origin, u);
        AssertThat(tEnd <= NorthTune.Bank.EndT + TestTolerance.Layout).OverrideFailureMessage($"the run ends beyond the far end of 商业街 (t {tEnd})").IsTrue();
        for (var k = 0; k < panels; k++)
        {
            var step = Vector2.Distance(Canal.CanalRailSite.Post(frame, k), Canal.CanalRailSite.Post(frame, k + 1));
            AssertThat(step <= NorthTune.Bank.PostPitch + TestTolerance.Position && step > 1f).OverrideFailureMessage($"panel {k} is a post pitch long ({step})").IsTrue();
        }
        var walls = new NorthModule().CollidersFor(ctx).Select(i => i.Collider).OfType<WallCollider>()
            .Where(c => MathF.Abs(Vector2.Dot(c.A - joint, w)) < TestTolerance.Position && MathF.Abs(Vector2.Dot(c.B - joint, w)) < TestTolerance.Position)
            .OrderBy(c => Vector2.Dot(joint - c.A, u)).ToArray();
        AssertThat(walls.Length == panels).OverrideFailureMessage($"one collider per panel ({walls.Length} of {panels})").IsTrue();
        AssertThat(Vector2.Distance(walls[0].A, joint) < 5 * PlanGeometry.Tolerance).OverrideFailureMessage("the first collider starts on the plaza's end post").IsTrue();
        for (var k = 0; k + 1 < walls.Length; k++)
            AssertThat(Vector2.Distance(walls[k].B, walls[k + 1].A) < 5 * PlanGeometry.Tolerance).OverrideFailureMessage($"colliders {k} and {k + 1} meet").IsTrue();
        AssertThat(walls.All(c => c.Y1 - c.Y0 >= WalkRules.BarrierHeight)).OverrideFailureMessage("the colliders reach 1.5 m").IsTrue();
        var rings = ctx.World.Features.Values.SelectMany(NorthBuildings.Rings).ToArray();
        foreach (var p in Samples([Canal.CanalRailSite.Post(frame, 0), Canal.CanalRailSite.Post(frame, panels)]))
        {
            AssertThat(rings.All(r => !PlanGeometry.InsideByCrossings(r, p) && PlanGeometry.DistanceToRing(p, r) >= 8f)).OverrideFailureMessage($"railing point {p} keeps 8 m off the buildings").IsTrue();
            foreach (var road in ctx.World.Network.Roads)
                AssertThat(MathF.Sqrt(PlanLines.PolylineDistanceSquared(road.Line.Points, p)) - road.Width * 0.5f >= 3f).OverrideFailureMessage($"railing point {p} keeps 3 m off the road edge of {road.Id.Value}").IsTrue();
        }
    }

    /// <summary>A gate whose origin (on the fence line) lies this close to a road's centreline opens onto it.</summary>
    private const float Faces = 8f;

    private static int RoadGrade(NetworkLine road) => WorldProps.TryText(road.Props, "name") is "丹桂路" or "银杏大道" ? 3
        : WorldProps.TryText(road.Props, "highway") switch
        {
            "primary" or "secondary" or "tertiary" or "unclassified" or "residential" => 2,
            "service" => 1,
            _ => 0,
        };

    /// <summary>
    /// 20a-3 (Opus 13:32): the seat wall stands at the courtyard walk facing the hill and the lawn, a paved pad joins it to
    /// the walk, and its back keeps 8 m off every dormitory window.
    /// </summary>
    [TestCase, TestCategory(TestReason.Contract)]
    public void CourtyardSeatWallFacesTheHillFromTheWalk()
    {
        var world = RealMapFixture.Load().World;
        var wall = CourtyardSite.Locate(world) ?? throw new InvalidOperationException("seat wall not located");
        var hill = (TerrainEllipse)Terrain().Ops.OfType<TerrainMoundOp>().Single(o => o.Id == NorthFeatureIds.Hill).Shape;
        var yaw = hill.YawDeg * Math.PI / 180;
        var path = CourtyardSite.Path(wall, NorthTune.Court.SeatSegments);
        var front = CourtyardSite.Front(wall);
        var mid = (wall.Start + wall.End) * 0.5f;
        AssertThat(Vector2.Dot(front, Vector2.Normalize(hill.Center - mid)) > 0.9f).OverrideFailureMessage("the seat faces the hill").IsTrue();
        var walk = world.Network.Roads.Single(r => r.Id.Value == NorthFeatureIds.CourtyardWalk.Source);
        float ToWalk(Vector2 p) => Enumerable.Range(0, walk.Line.Points.Count - 1).Min(i => MathF.Sqrt(PlanLines.SegmentDistanceSquared(p, walk.Line.Points[i], walk.Line.Points[i + 1])));
        AssertThat(ToWalk(mid + front) < ToWalk(mid)).OverrideFailureMessage("the walk lies in front of the seat").IsTrue();
        // The pad's straight edge runs along the walk's paving edge, 2 cm short of it, over the whole length of the wall.
        var pad = CourtyardSite.PadOutline(wall);
        var edge = pad.Where(p => MathF.Abs(Vector2.Dot(p - wall.Start, front) - NorthTune.Court.PadReach) < TestTolerance.Position).ToArray();
        AssertThat(edge.Length == 2).OverrideFailureMessage("the pad has one straight edge toward the walk").IsTrue();
        foreach (var p in edge)
            AssertThat(MathF.Abs(ToWalk(p) - (walk.Width * 0.5f + 0.02f)) < TestTolerance.Layout).OverrideFailureMessage($"pad corner {p} meets the walk's edge").IsTrue();
        var buildings = world.Features.Values.SelectMany(NorthBuildings.Rings)
            .Where(ring => ring.Any(q => Vector2.Distance(q, mid) < 60f)).ToArray();
        AssertThat(buildings.Length > 0).OverrideFailureMessage("dormitories around the courtyard found").IsTrue();
        foreach (var p in path)
        {
            double dx = p.X - hill.Center.X, dz = p.Y - hill.Center.Y;
            double along = dx * Math.Sin(yaw) - dz * Math.Cos(yaw), across = dx * Math.Cos(yaw) + dz * Math.Sin(yaw);
            AssertThat(along * along / (hill.Radii.X * hill.Radii.X) + across * across / (hill.Radii.Y * hill.Radii.Y) > 1.2).OverrideFailureMessage($"{p} is on level ground off the hill").IsTrue();
            foreach (var road in world.Network.Roads)
                for (var i = 0; i + 1 < road.Line.Points.Count; i++)
                    AssertThat(MathF.Sqrt(PlanLines.SegmentDistanceSquared(p, road.Line.Points[i], road.Line.Points[i + 1])) > road.Width * 0.5f + NorthTune.Court.BodyWidth).OverrideFailureMessage($"{p} stands off the paving of {road.Id.Value}").IsTrue();
            foreach (var ring in buildings)
                for (var i = 0; i < ring.Count; i++)
                    AssertThat(MathF.Sqrt(PlanLines.SegmentDistanceSquared(p, ring[i], ring[(i + 1) % ring.Count])) - NorthTune.Court.BodyWidth * 0.5f >= 8f + NorthTune.Hall.WallFace).OverrideFailureMessage($"{p} keeps 8 m off the windows").IsTrue();
        }
    }
}
