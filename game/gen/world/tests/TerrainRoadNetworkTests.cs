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
public sealed class TerrainRoadNetworkTests
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
        var conflicting = new TerrainRoadProfile([new(0, interiorHeight - 0.1 + 0.05), new(20, interiorHeight + 0.1 + 0.05)],
            20, [], 1d / 12, "through.terrain.json", "design:terrain:incompatible-road");
        {
            WorldDataException? rejection = null;
            try { TerrainRoadProfile.SmoothNetwork([], new TerrainProgram([]),
            [(endRoad, endProfile), (throughRoad, conflicting)], (_, _) => false); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }
}
