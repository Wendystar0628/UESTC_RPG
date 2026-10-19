using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Maps;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

public sealed partial class TerrainCompilerTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void FixedRoadAnchorsSurviveSmoothingAndInfeasibleGradeFails()
    {
        var profile = new TerrainRoadProfile([new(0, 0), new(20, 0.5), new(40, 0)], 40, [], 0.05, "fixture", "road");
        AssertThat(Math.Abs((double)(0) - (double)(profile.At(0).Y)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.5) - (double)(profile.At(20).Y)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(profile.At(40).Y)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(profile.At(20).Grade)) <= TestTolerance.Position).IsTrue();
        {
            WorldDataException? rejection = null;
            try { new TerrainRoadProfile([new(0, 0), new(10, 2)], 10, [], 0.05, "fixture", "road"); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        {
            WorldDataException? rejection = null;
            try { new TerrainRoadProfile([new(0, 0), new(9, 0)], 10, [], 0.05, "fixture", "road"); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var roundedEnd = new TerrainRoadProfile([new(0, 0), new(55.5, 1)], 55.49793264276604, [], 0.05, "fixture", "rounded-end");
        AssertThat(Math.Abs((double)(1) - (double)(roundedEnd.At(roundedEnd.Length).Y)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(roundedEnd.Length) - (double)(TerrainRoadProfile.ResolveStation(55.5, 55.5, roundedEnd.Length, "fixture", "steps"))) <= TestTolerance.Position).IsTrue();
        {
            WorldDataException? rejection = null;
            try { TerrainRoadProfile.ResolveStation(55.499, 55.5, roundedEnd.Length, "fixture", "steps"); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var end = new Vector2(10, 0);
        var paving = new PolygonGeometry(Box(0, -1, end.X - (float)TerrainMath.RoadCoreJoinTolerance / 2, 1));
        var feature = new WorldFeature(new("fixture:core-join-road"), new("road"), paving)
            { Parts = [new("osm:way:1", paving, null, null) { Props = Kind("road") }] };
        var high = new NetworkLine(new("fixture:source:way:1"), new([Vector2.Zero, end]), 2)
            { Props = JsonSerializer.SerializeToElement(new { highway = "service" }) };
        var low = high with { Id = new("fixture:source:way:2"), Line = new([new(0, 4), new(20, 4)]) };
        var highOp = new TerrainCorridorOp("design:terrain:core-high", "fixed endpoint/core join regression", high.Id,
            [new(0, 0.6), new(10, 0.6)], 0, Defaults.CorridorShoulder) { Zone = Zone };
        var lowOp = highOp with { Id = "design:terrain:core-low", Road = low.Id, Anchors = [new(0, 0), new(20, 0)] };
        var terrain = TerrainCompiler.Compile(Context([feature]) with { Network = new([high, low], [], [], [], []) }, [Source([highOp, lowOp])]);
        AssertThat(Math.Abs(terrain.HeightAt(end.X, end.Y) - highOp.Anchors[^1].Y) <= TestTolerance.Position)
            .OverrideFailureMessage("An endpoint within the core join tolerance was left in outer grading and cut by another road's falloff.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AutomaticCorridorUsesNonRoadPlateauAndExplicitAnchorsOverrideIt()
    {
        var road = new NetworkLine(new("fixture:plateau-road"), new([new(-30, 0), new(30, 0)]), 2.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var plateau = new TerrainPlatformOp("design:terrain:road-plateau", "terrain-following road regression",
            Box(-80, -80, 80, 80), 4, new("1:4", "1:4"), 2) { Zone = Zone };
        var context = Context([]) with { Network = new([road], [], [], [], []) };
        var automatic = TerrainCompiler.Compile(context, [Source([plateau])]);
        AssertThat(Math.Abs((double)(4) - (double)(automatic.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(4) - (double)(automatic.HeightAt(-30, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(4) - (double)(automatic.HeightAt(30, 0))) <= TestTolerance.Position).IsTrue();
        var explicitRoad = new TerrainCorridorOp("design:terrain:authored-road", "explicit anchors win", road.Id,
            [new(0, 5), new(60, 5)], 0, 0.75) { Zone = Zone };
        var authored = TerrainCompiler.Compile(context, [Source([plateau, explicitRoad])]);
        AssertThat(Math.Abs((double)(5) - (double)(authored.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(5) - (double)(authored.HeightAt(-30, 0))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AutomaticTerrainConsumesAuthoredHighwayClassification()
    {
        foreach (var kind in new[] { "footway", "path", "pedestrian" })
        {
            var road = new NetworkLine(new("design:road:effective-" + kind), new([new(-10, 0), new(10, 0)]), 2.5f)
            {
                Props = JsonSerializer.SerializeToElement(new { highway = kind })
            };
            var context = Context([]) with { Network = new([road], [], [], [], []) };
            var references = new TerrainReferences(context);
            AssertThat(references.Road(road.Id, "fixture", "effective-road").Kind == kind
                && references.AutomaticRoads().Single().Kind == kind).OverrideFailureMessage("Explicit and automatic road classification disagree.").IsTrue();
            var terrain = TerrainCompiler.Compile(context, []);
            AssertThat(!terrain.IsFlat(default)).OverrideFailureMessage("An effective WorldNetwork road was ignored by automatic terrain.").IsTrue();
            AssertThat(Math.Abs((double)(0.0075) - (double)(terrain.HeightAt(0, 0.5f))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-0.0075) - (double)(terrain.HeightAt(0, -0.5f))) <= TestTolerance.Position).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AutomaticNetworkFitMovesSoftSamplesAndKeepsSharedAuthorNodes()
    {
        var a = new TerrainRoad(new("fixture:left-road"), new("fixture:left-road"), new(new([new(-10, 0), new(0, 0)])), 2.5, "footway", null);
        var c = new TerrainRoad(new("fixture:right-road"), new("fixture:right-road"), new(new([new(0, 0), new(10, 0)])), 2.5, "footway", null);
        var baseline = new TerrainProgram([new("base", "fixture", Zone, TerrainGroup.Regional, 0, new(new(-20), new(20)),
            (previous, x, _) => previous with { Surface = new(x * 0.2, 0.2, 0) })]);
        TerrainRoadProfile.Request[] requests = [new(a, [], 1d / 12), new(c, [], 1d / 12)];
        var fitted = TerrainRoadProfile.SmoothNetwork(requests, baseline, [], (_, _) => false);
        var reverse = TerrainRoadProfile.SmoothNetwork(requests.Reverse().ToArray(), baseline, [], (_, _) => false);
        AssertThat(Math.Abs((double)(0) - (double)(fitted[a.Source].At(10).Y)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(fitted[c.Source].At(0).Y)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(-0.3) - (double)(fitted[a.Source].At(0).Y)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.3) - (double)(fitted[c.Source].At(10).Y)) <= TestTolerance.Position).IsTrue();
        for (var s = 0d; s <= 10; s += 0.25)
        {
            AssertThat(Math.Abs((double)(fitted[a.Source].At(s).Y) - (double)(reverse[a.Source].At(s).Y)) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs(fitted[a.Source].At(s).Grade) <= requests[0].MaximumGrade + TerrainMath.GradeTolerance).OverrideFailureMessage("Default network smoothing broke the shared-node grade bound.").IsTrue();
        }
        var authorRoad = new TerrainRoad(new("fixture:author-road"), new("fixture:author-road"), new(new([new(0, 0), new(0, 10)])), 2.5, "footway", null);
        var authorProfile = new TerrainRoadProfile([new(0, 1), new(10, 1)], 10, [], 1d / 12, "fixture", "author");
        var fixedFit = TerrainRoadProfile.SmoothNetwork(requests, baseline, [(authorRoad, authorProfile)], (_, _) => false);
        AssertThat(Math.Abs((double)(1) - (double)(fixedFit[a.Source].At(10).Y)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1) - (double)(fixedFit[c.Source].At(0).Y)) <= TestTolerance.Position).IsTrue();
        {
            WorldDataException? rejection = null;
            try { TerrainRoadProfile.SmoothNetwork(requests, baseline, [(authorRoad, authorProfile)], (x, _) => x == 0); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void AuthoredJunctionUsesExistingHeightToleranceWithoutMovingEitherProfile()
    {
        const double endpointHeight = 1.786;
        const double interiorHeight = 1.7859987939771866;
        var endRoad = new TerrainRoad(new("fixture:end-road"), new("fixture:end-road"),
            new(new([new(-10, 0), new(0, 0)])), 2.5, "footway", null);
        var throughRoad = new TerrainRoad(new("fixture:through-road"), new("fixture:through-road"),
            new(new([new(0, -10), new(0, 0), new(0, 10)])), 2.5, "footway", null);
        var automatic = new TerrainRoad(new("fixture:auto-road"), new("fixture:auto-road"),
            new(new([new(0, 0), new(10, 0)])), 2.5, "footway", null);
        TerrainAnchor[] endAnchors = [new(0, endpointHeight), new(10, endpointHeight)];
        TerrainAnchor[] throughAnchors = [new(0, interiorHeight - 0.1), new(20, interiorHeight + 0.1)];
        var endProfile = new TerrainRoadProfile(endAnchors, 10, [], 1d / 12, "end.terrain.json", "design:terrain:end-road");
        var throughProfile = new TerrainRoadProfile(throughAnchors, 20, [], 1d / 12, "through.terrain.json", "design:terrain:through-road");
        var fitted = TerrainRoadProfile.SmoothNetwork([new(automatic, [], 1d / 12)], new TerrainProgram([]),
            [(endRoad, endProfile), (throughRoad, throughProfile)], (_, _) => false);
        AssertThat(Math.Abs(endProfile.At(10).Y - endpointHeight) < TestTolerance.Position
            && Math.Abs(throughProfile.At(10).Y - interiorHeight) < TestTolerance.Position).OverrideFailureMessage("Junction tolerance moved or averaged authored profiles.").IsTrue();
        AssertThat(Math.Abs(fitted[automatic.Source].At(0).Y - interiorHeight) < TestTolerance.Position).OverrideFailureMessage("The existing last-authored lock value was replaced or averaged.").IsTrue();
        AssertThat(endAnchors[0].Y == endpointHeight && endAnchors[1].Y == endpointHeight
            && throughAnchors[0].Y == interiorHeight - 0.1 && throughAnchors[1].Y == interiorHeight + 0.1).OverrideFailureMessage("Junction matching mutated the original authored anchor values.").IsTrue();
        var incompatible = new TerrainRoadProfile([new(0, interiorHeight - 0.1 + 0.05), new(20, interiorHeight + 0.1 + 0.05)],
            20, [], 1d / 12, "through.terrain.json", "design:terrain:incompatible-road");
        {
            WorldDataException? rejection = null;
            try { TerrainRoadProfile.SmoothNetwork([], new TerrainProgram([]),
            [(endRoad, endProfile), (throughRoad, incompatible)], (_, _) => false); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

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
        var tip = TerrainCompiler.EndCapDistance(3, 0, Vector2.Zero, 1, 0, radius);
        var flank = TerrainCompiler.EndCapDistance(0, 3, Vector2.Zero, 1, 0, radius);
        var centreline = TerrainCompiler.EndCapDistance(1, 0, Vector2.Zero, 1, 0, radius);
        AssertThat(Math.Abs((double)(0) - (double)(tip.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-1) - (double)(tip.Dx)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(flank.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-1) - (double)(flank.Dz)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(2) - (double)(centreline.Height)) <= TestTolerance.Position).IsTrue();
        foreach (var point in new[] { new Vector2(0.01f, 2.99f), new Vector2(-0.01f, 2.99f), new Vector2(1, 2), new Vector2(-1, 2) })
        {
            var d = TerrainCompiler.EndCapDistance(point.X, point.Y, Vector2.Zero, 1, 0, radius);
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
                var contact = TerrainCompiler.CorridorContactAt(road, 0.75, point.X, point.Y);
                AssertThat(contact.Distance.Height <= 0).OverrideFailureMessage("Fixture must be inside the fixed source core/shoulder.").IsTrue();
                var expected = path.Project(point.X, point.Y);
                var actual = TerrainCompiler.CorridorOffsetAt(road, contact, point.X, point.Y);
                AssertThat(Math.Abs(actual.Height - expected.Offset) < TestTolerance.Position).OverrideFailureMessage("Exterior continuity repair changed fixed core/shoulder crossfall.").IsTrue();
            }

            double Top(double px, double pz)
            {
                var contact = TerrainCompiler.CorridorContactAt(road, 0, px, pz);
                var along = profile.SurfaceAt(path, px, pz, road.Width, path.Project(px, pz));
                return along.Height + 0.015 * TerrainCompiler.CorridorOffsetAt(road, contact, px, pz).Height;
            }
        }
    }

}
