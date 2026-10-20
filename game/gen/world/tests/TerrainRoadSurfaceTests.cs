using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Geometry;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;
using static Campus.Gen.World.Tests.TerrainCompilerTestFixture;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class TerrainRoadSurfaceTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void RoadCrossfallOffsetIsContinuousAcrossAnInternalCorner()
    {
        var path = new TerrainPath(new([new(0, 10), new(0, 0), new(-10, 0)]));
        const double e = 0.01;
        var a = path.Project(-e, -1); var c = path.Project(e, -1);
        AssertThat(Math.Abs((double)(1) - (double)(a.Offset)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs(a.Offset - c.Offset) < TestTolerance.Position).OverrideFailureMessage("Crossfall jumped at an internal segment's clamped endpoint.").IsTrue();
        AssertThat(Math.Abs((double)(Math.Sqrt(2)) - (double)(path.Project(1, -1).Offset)) <= TestTolerance.Position).IsTrue();
        var straight = new TerrainPath(new([new(0, 0), new(0, 10)]));
        AssertThat(Math.Abs((double)(0) - (double)(straight.Project(0, -2).Offset)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void LongRoadProjectionKeepsSourceOrderAndEndpointGradients()
    {
        var points = Enumerable.Range(0, 17).Select(i => new Vector2(20, i * 4))
            .Concat(Enumerable.Range(0, 17).Select(i => new Vector2(0, 64 - i * 4))).ToArray();
        var path = new TerrainPath(new(points));
        var tied = path.Project(10, 6);
        AssertThat(tied.Segment == 1).OverrideFailureMessage("Equidistant path segments did not retain source order.").IsTrue();
        AssertThat(Math.Abs(tied.Station - 6) <= TestTolerance.Position && Math.Abs(tied.Offset - 10) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs(tied.StationDz - 1) <= TestTolerance.Position && Math.Abs(tied.OffsetDx + 1) <= TestTolerance.Position).IsTrue();
        var end = points[^1];
        var atEnd = path.Project(end.X, end.Y);
        AssertThat(atEnd.Segment == points.Length - 2 && Math.Abs(atEnd.Station - path.Length) <= TestTolerance.Position).IsTrue();
        var outside = path.Project(end.X - 3, end.Y - 4);
        AssertThat(Math.Abs(outside.Station - path.Length) <= TestTolerance.Position && Math.Abs(outside.Distance - 5) <= TestTolerance.Position).IsTrue();
        AssertThat(outside.StationDx == 0 && outside.StationDz == 0).IsTrue();
        AssertThat(Math.Abs(outside.DistanceDx + .6) <= TestTolerance.Position && Math.Abs(outside.DistanceDz + .8) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RoadLongitudinalSurfaceIsContinuousAcrossTheInnerCorner()
    {
        var path = new TerrainPath(new([new(0, 10), new(0, 0), new(-10, 0)]));
        var profile = new TerrainRoadProfile([new(0, 0), new(20, 0.2)], 20, [], 1d / 12, "fixture", "corner");
        TerrainDifferential Sample(double x, double z) => profile.SurfaceAt(path, x, z, 2.5, path.Project(x, z));
        const double e = 0.01;
        AssertThat(Math.Abs(Sample(-1 - e, 1).Height - Sample(-1 + e, 1).Height) < TestTolerance.Position).OverrideFailureMessage("Longitudinal height selected discontinuous nearest-segment station values at the inner bend.").IsTrue();
        AssertThat(Math.Abs((double)(0.1) - (double)(Sample(-1, 1).Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.09) - (double)(Sample(0, 1).Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.11) - (double)(Sample(-1, 0).Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(Sample(0, 10).Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.2) - (double)(Sample(-10, 0).Height)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void StructuralRoadEndpointRetainsItsExactSourceStation()
    {
        var path = new TerrainPath(new([new(-465.7f, 66.61f), new(-436.57f, 111.36f)]));
        var end = path.Line.Points[^1];
        AssertThat(Math.Abs((double)(path.Length) - (double)(path.Project(end.X, end.Y).Station)) <= TestTolerance.Position).IsTrue();
        var profile = new TerrainRoadProfile([new(0, 0), new(path.Length, 0)], path.Length,
            [(0, path.Length - 0.00000000000001)], 1d / 12, "fixture", "bridge");
        AssertThat(profile.Excluded(path.Length)).OverrideFailureMessage("Floating-point endpoint residue invented a ground-road sliver after a complete bridge.").IsTrue();
        AssertThat(!profile.Excluded(path.Length + 0.01)).OverrideFailureMessage("Numerical endpoint tolerance expanded into geometric coverage.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void RealSegmentIntersectionRejectsNearbyParallelProjection()
    {
        var line = new TerrainPath(new([new(-20, 0), new(20, 0)]));
        var parallel = new TerrainPath(new([new(-20, 0.5f), new(20, 0.5f)]));
        AssertThat(line.Intersections(parallel).Count == 0).OverrideFailureMessage("Close projections manufactured a crossing.").IsTrue();
        var cross = line.Intersections(new(new([new(3, -10), new(3, 10)])));
        AssertThat(cross.Count == 1).OverrideFailureMessage("Actual crossing missing.").IsTrue(); AssertThat(Math.Abs((double)(3) - (double)(cross[0].X)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(23) - (double)(cross[0].Station)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(10) - (double)(cross[0].OtherStation)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EndCapDistanceHasBoundedSlopeAtBothFlanksAndObliqueRoadClearance()
    {
        const double radius = 3;
        var tip = TerrainDryChannels.EndCapDistance(3, 0, Vector2.Zero, 1, 0, radius);
        var flank = TerrainDryChannels.EndCapDistance(0, 3, Vector2.Zero, 1, 0, radius);
        var centreline = TerrainDryChannels.EndCapDistance(1, 0, Vector2.Zero, 1, 0, radius);
        AssertThat(Math.Abs((double)(0) - (double)(tip.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-1) - (double)(tip.Dx)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(flank.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-1) - (double)(flank.Dz)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(2) - (double)(centreline.Height)) <= TestTolerance.Position).IsTrue();
        foreach (var point in new[] { new Vector2(0.01f, 2.99f), new Vector2(-0.01f, 2.99f), new Vector2(1, 2), new Vector2(-1, 2) })
        {
            var d = TerrainDryChannels.EndCapDistance(point.X, point.Y, Vector2.Zero, 1, 0, radius);
            AssertThat(double.IsFinite(d.Dx) && double.IsFinite(d.Dz) && d.Dx * d.Dx + d.Dz * d.Dz <= 1.01).OverrideFailureMessage("End-cap distance has an unbounded flank derivative.").IsTrue();
        }
        // A 4 m-wide north/south road occupies x >= -2 on its near side. A radius-3
        // cap centred at x=-6 has exactly 1 m clearance, independently of channel yaw.
        var center = new Vector2(-6, 0); var road = new TerrainPath(new([new(0, -100), new(0, 100)]));
        AssertThat(Math.Abs((double)(1) - (double)(road.Project(center.X, center.Y).Distance - radius - 2)) <= TestTolerance.Position).IsTrue();
        var incorrectlyPlacedCenter = new Vector2(-3, 0);
        AssertThat(road.Project(incorrectlyPlacedCenter.X, 0).Distance - radius - 2 < 0).OverrideFailureMessage("Fixture failed to expose centre-versus-cap clearance.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void ArealHighwayIsNotAContourMasqueradingAsACorridor()
    {
        var road = new NetworkLine(new("fixture:road"), new([new(-10, -10), new(10, -10), new(10, 10), new(-10, 10), new(-10, -10)]), 5)
            { Props = JsonSerializer.SerializeToElement(new { highway = "pedestrian", area = "yes" }) };
        var context = Context([]) with { Network = new([road], [], [], [], []) };
        AssertThat(TerrainCompiler.Compile(context, []).IsFlat(default)).OverrideFailureMessage("Areal paving generated an automatic perimeter road.").IsTrue();
        var corridor = new TerrainCorridorOp("design:terrain:invalid-road", "negative fixture", road.Id, [new(0, 0), new(80, 0)], 0, 0.75) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(context, [Source([corridor])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void RealSourceEndCrossfallExtensionIsContinuousAtBothTerminalStations()
    {
        // Source way 572770116: the real cap corner projects 2.86 mm before
        // the centreline end, so the old two exterior extensions did not meet.
        Vector2[] points = [new(-8.35f, 224.89f), new(5.48f, 234.42f), new(15.62f, 236.44f),
            new(20.8f, 236.31f), new(43.62f, 235.71f)];
        var footprint = new TerrainRegion([new PolygonRings([new(15.51f, 237.7f), new(43.65f, 236.96f),
            new(43.59f, 234.46f), new(15.73f, 235.19f), new(5.98f, 233.24f), new(-7.65f, 223.86f),
            new(-9.06f, 225.92f), new(4.99f, 235.59f)])]);
        var end = points[^1]; var previous = points[^2];
        var dx = (double)end.X - previous.X; var dz = (double)end.Y - previous.Y;
        var length = Math.Sqrt(dx * dx + dz * dz); var tx = dx / length; var tz = dz / length;
        const double lateral = 2.36387369;
        var x = end.X - tz * lateral; var z = end.Y + tx * lateral;
        foreach (var reversed in new[] { false, true })
        {
            var path = new TerrainPath(new(reversed ? points.Reverse().ToArray() : points));
            var road = new TerrainRoad(new("fixture:source-end"), new("fixture:source:way:572770116"), path, 2.5, "footway", footprint);
            var profile = new TerrainRoadProfile([new(0, 0), new(path.Length, -0.03 * path.Length)], path.Length, [], 1d / 12, "fixture", "source-end");
            foreach (var epsilon in new[] { 0.01 })
            {
                var a = Top(x - tx * epsilon, z - tz * epsilon);
                var c = Top(x + tx * epsilon, z + tz * epsilon);
                AssertThat(Math.Abs(c - a) < TestTolerance.Position).OverrideFailureMessage($"Source-end height jumps at epsilon={epsilon:R}: {a:R}->{c:R}, reversed={reversed}.").IsTrue();
            }
            // The source core and its actual 0.75 m shoulder keep planar crossfall,
            // including the part of the shoulder past the source-end plane.
            foreach (var point in new[] { new Vector2(43.6f, 235.8f), new Vector2(44, 235.8f) })
            {
                var contact = TerrainRoadCoreGeometry.CorridorContactAt(road, 0.75, point.X, point.Y);
                AssertThat(contact.Distance.Height <= 0).OverrideFailureMessage("Fixture must be inside the fixed source core/shoulder.").IsTrue();
                var expected = path.Project(point.X, point.Y);
                var actual = TerrainRoadCoreGeometry.CorridorOffsetAt(road, contact, point.X, point.Y);
                AssertThat(Math.Abs(actual.Height - expected.Offset) < TestTolerance.Position).OverrideFailureMessage("Exterior continuity repair changed fixed core/shoulder crossfall.").IsTrue();
            }

            double Top(double px, double pz)
            {
                var contact = TerrainRoadCoreGeometry.CorridorContactAt(road, 0, px, pz);
                var along = profile.SurfaceAt(path, px, pz, road.Width, path.Project(px, pz));
                return along.Height + 0.015 * TerrainRoadCoreGeometry.CorridorOffsetAt(road, contact, px, pz).Height;
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RectangularBridgeApproachKeepsItsActualEndAndShoulder()
    {
        var road = new TerrainRoad(new("fixture:approach"), new("fixture:source:way:1"),
            new(new([new(-20, 0), new(0, 0)])), 2, "footway", new([Box(-20, -1, 0, 1)]));
        var end = TerrainRoadCoreGeometry.CorridorContactAt(road, 0.75, 2, 0);
        AssertThat(Math.Abs((double)(1.25) - (double)(end.Distance.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.75) - (double)(end.X)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(end.Z)) <= TestTolerance.Position).IsTrue();
        var side = TerrainRoadCoreGeometry.CorridorContactAt(road, 0.75, -10, 3);
        AssertThat(Math.Abs((double)(1.25) - (double)(side.Distance.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-10) - (double)(side.X)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1.75) - (double)(side.Z)) <= TestTolerance.Position).IsTrue();
        var corner = TerrainRoadCoreGeometry.CorridorContactAt(road, 0.75, 2, 2);
        AssertThat(Math.Abs((double)(Math.Sqrt(5) - 0.75) - (double)(corner.Distance.Height)) <= TestTolerance.Position).IsTrue();
        var centerline = TerrainRoadCoreGeometry.CorridorContactAt(road with { Footprint = null }, 0.75, 2, 0);
        AssertThat(Math.Abs((double)(0.25) - (double)(centerline.Distance.Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(end.Distance.Height > centerline.Distance.Height).OverrideFailureMessage("Real rectangular port was inflated into a line capsule.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CoreCoverageBoundIncludesAnInteriorFarthestPoint()
    {
        // The boundary follows a square loop. Every footprint vertex is on the source path,
        // while the real core interior reaches five metres from it.
        var road = new TerrainRoad(new("fixture:wide-core"), new("fixture:wide-core"),
            new(new([new(-5, -5), new(5, -5), new(5, 5), new(-5, 5), new(-5, -5)])), 2.5, "footway", new([Box(-5, -5, 5, 5)]));
        var bound = TerrainRoadCoreGeometry.RoadCorePathRadius(road);
        AssertThat(bound >= 5 && bound < 5.4).OverrideFailureMessage("Core radius was inferred from vertices or introduced an unrelated art margin.").IsTrue();
    }
}
