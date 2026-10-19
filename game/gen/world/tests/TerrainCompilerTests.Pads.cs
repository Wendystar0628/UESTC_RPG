using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Maps;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

public sealed partial class TerrainCompilerTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public void PolygonKioskAnchorsHaveGroundPadsWhilePostsAndRaisedDeckLevelsStaySeparate()
    {
        var kiosk = WorldFeatureFixture.Create("fixture:kiosk-anchor", "anchor", new PolygonGeometry(Box(-2, -2, 2, 2)),
            props: "{\"form\":\"kiosk\",\"groundY\":0}");
        var post = WorldFeatureFixture.Create("fixture:post-anchor", "anchor", new PolygonGeometry(Box(20, -1, 21, 1)),
            props: "{\"form\":\"post\",\"groundY\":0}");
        var deck = WorldFeatureFixture.Create("fixture:raised-kiosk-anchor", "anchor", new PolygonGeometry(Box(40, -2, 44, 2)),
            props: "{\"form\":\"kiosk\",\"groundY\":5}");
        var ground = new TerrainPlatformOp("design:terrain:kiosk-ground", "pre-pad ground", Box(-60, -60, 60, 60),
            0.5, new("1:4", "1:4"), 2) { Zone = Zone };
        var authored = new TerrainPadOp("design:terrain:kiosk-pad", "explicit kiosk pad", kiosk.Id, 6, PadY: 0.7) { Zone = Zone };
        foreach (var operations in new TerrainOp[][] { [ground], [ground, authored] })
        {
            var terrain = Compile(operations, [kiosk, post, deck]);
            var expected = operations.Length == 1 ? ground.Y : authored.PadY!.Value;
            AssertThat(Math.Abs(terrain.PadY(kiosk.Id) - expected) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs(terrain.HeightAt(0, 0) - terrain.PadY(kiosk.Id)) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs(terrain.PadY(deck.Id) - ground.Y) <= TestTolerance.Position).IsTrue();
            KeyNotFoundException? rejection = null;
            try { terrain.PadY(post.Id); } catch (KeyNotFoundException error) { rejection = error; }
            AssertThat(rejection is not null).OverrideFailureMessage("A small post acquired a structural terrain pad.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void BuildingCoreSurvivesEveryCorridorKindWhileAuthoredAnchorsStayFixed()
    {
        var building = WorldFeatureFixture.Create("fixture:final-pad", "building", new PolygonGeometry(Box(-2, -2, 2, 2)), height: 8);
        var road = new NetworkLine(new("fixture:pad-road"), new([new(-30, 0), new(30, 0)]), 2.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([building]) with { Network = new([road], [], [], [], []), DataRect = new(new(-40), new(40)) };
        const double roadY = 0.6;
        var full = new TerrainCorridorOp("design:terrain:pad-road", "core priority regression", road.Id,
            [new(0, roadY), new(60, roadY)], 0, 0.75) { Zone = Zone };
        var explicitPad = new TerrainPadOp("design:terrain:pad", "fixed building core", building.Id, 6, PadY: roadY) { Zone = Zone };
        TerrainOp[][] cases =
        [
            [explicitPad],
            [full],
            [full with { Id = "design:terrain:left-range", Anchors = [new(0, roadY), new(10, roadY)], Range = new(0, 10) },
             full with { Id = "design:terrain:right-range", Anchors = [new(50, roadY), new(60, roadY)], Range = new(50, 60) }]
        ];
        System.Numerics.Vector2[] points = [new(0, 0), new(2, 0), new(-2, 2)];
        for (var i = 0; i < cases.Length; i++)
        {
            var terrain = TerrainCompiler.Compile(context, [Source(cases[i])]);
            var padY = terrain.PadY(building.Id);
            AssertThat(Math.Abs(padY - (i == 0 ? roadY : 0)) <= TestTolerance.Position).IsTrue();
            foreach (var point in points)
                AssertThat(Math.Abs(terrain.HeightAt(point.X, point.Y) - padY) <= TestTolerance.Position)
                    .OverrideFailureMessage("A corridor or its shoulder changed the building core.").IsTrue();
            if (i > 0)
            {
                AssertThat(Math.Abs(terrain.HeightAt(-30, 0) - roadY) <= TestTolerance.Position).IsTrue();
                AssertThat(Math.Abs(terrain.HeightAt(30, 0) - roadY) <= TestTolerance.Position).IsTrue();
            }
            if (i == 1)
                AssertThat(Math.Abs(terrain.HeightAt(12, 0) - roadY) <= TestTolerance.Position)
                    .OverrideFailureMessage("Pad protection escaped its finite exterior grading band.").IsTrue();
        }
        var conflicting = full with { Anchors = [new(0, roadY), new(30, roadY), new(60, roadY)] };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(context, [Source([conflicting])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void MultipartPadSharedAndOverlappingWingEdgesAreNotExposedGradingBoundaries()
    {
        foreach (var wings in new[]
        {
            new[] { Box(-4, -4, 0, 4), Box(0, -4, 4, 4) },
            new[] { Box(-4, -4, 1, 4), Box(-1, -4, 4, 4) }
        })
        {
            var building = new WorldFeature(new("fixture:multipart-pad"), new("building"), new MultiPolygonGeometry(wings))
            {
                Height = 8,
                Parts = wings.Select((wing, index) => new WorldFeaturePart("fixture:wing:" + index,
                    new PolygonGeometry(wing), null, null) { Props = Kind("building") }).ToArray()
            };
            var terrain = TerrainCompiler.CompileAndValidate(Context([building]), []);
            AssertThat(Math.Abs((double)(0) - (double)(terrain.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(-2, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(2, 0))) <= TestTolerance.Position).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PadGradingRespectsTheOneInFourLimit()
    {
        var building = WorldFeatureFixture.Create("fixture:quarter-grade-pad", "building", new PolygonGeometry(Box(-2, -20, 2, 20)), height: 8);
        var plateau = new TerrainPlatformOp("design:terrain:quarter-grade-base", "one-in-four base",
            Box(-40, -40, 40, 40), 0.75, new("1:4", "1:4"), 2) { Zone = Zone };
        var pad = new TerrainPadOp("design:terrain:quarter-grade-pad", "one-in-four boundary is legal",
            building.Id, 6, PadY: 0, Apron: 0) { Zone = Zone };
        var terrain = Compile([plateau, pad], [building]);
        AssertThat(Math.Abs((double)(0.25) - (double)(terrain.HeightAt(4, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.25) - (double)(terrain.FactsAt(4, 0).Grade)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0.75) - (double)(terrain.HeightAt(8, 0))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AutoPadTouchesProtectedBandAndApronJoinsWithoutJump()
    {
        var building = WorldFeatureFixture.Create("fixture:building", "building", new PolygonGeometry(Box(19, 40, 32, 60)), height: 8);
        var context = Context([building]) with { Boundary = Box(0, 0, 200, 200), DataRect = new(new(0), new(200)) };
        var terrain = TerrainCompiler.Compile(context, []);
        AssertThat(Math.Abs((double)(0) - (double)(terrain.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(19.5f, 50))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(18.5f, 50))) <= TestTolerance.Position).IsTrue();
        var inside = terrain.HeightAt(32.79f, 50); var outside = terrain.HeightAt(32.81f, 50);
        AssertThat(Math.Abs(inside - outside) < TestTolerance.Position).OverrideFailureMessage("Apron edge reset to PadY and made a vertical seam.").IsTrue();
    }

}
