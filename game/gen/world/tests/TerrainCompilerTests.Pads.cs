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
    public void PadBoundaryValidationUsesTheFinalCorridorSurface()
    {
        var building = WorldFeatureFixture.Create("fixture:final-pad", "building", new PolygonGeometry(Box(-2, -8, 2, 8)), height: 8);
        var road = new NetworkLine(new("fixture:pad-road"), new([new(-30, 0), new(30, 0)]), 2.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([building]) with { Network = new([road], [], [], [], []) };
        TerrainCorridorOp Corridor(double y) => new("design:terrain:pad-road", "final-ground regression", road.Id,
            [new(0, y), new(60, y)], 0, 0.75) { Zone = Zone };
        // All original building corners remain clear of the road. The final mid-edge sample
        // must still see a later corridor crossing; the earlier pre-pad field was flat zero.
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(context, [Source([Corridor(1.3)])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var narrow = new TerrainPadOp("design:terrain:narrow-pad", "grade width is still binding", building.Id, 1, PadY: 0) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(context, [Source([narrow, Corridor(0.3)])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var accepted = TerrainCompiler.Compile(context, [Source([Corridor(0.2)])]);
        AssertThat(Math.Abs((double)(0) - (double)(accepted.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.2) - (double)(accepted.HeightAt(2, 0))) <= TestTolerance.Position).IsTrue();
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
            var building = new WorldFeature(new("fixture:multipart-pad"), FeatureSource.Design, new("building"), new MultiPolygonGeometry(wings))
            {
                Height = 8,
                Parts = wings.Select((wing, index) => new WorldFeaturePart("fixture:wing:" + index,
                    new PolygonGeometry(wing), null, null) { Props = Kind("building") }).ToArray()
            };
            var terrain = Compile([], [building]);
            AssertThat(Math.Abs((double)(0) - (double)(terrain.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(-2, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(2, 0))) <= TestTolerance.Position).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PadAndLaterCorridorUseTheFinalSurface()
    {
        var building = WorldFeatureFixture.Create("fixture:replaced-pad", "building", new PolygonGeometry(Box(-2, -2, 2, 2)), height: 8);
        var road = new NetworkLine(new("fixture:wide-pad-road"), new([new(-30, 0), new(30, 0)]), 30)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var pad = new TerrainPadOp("design:terrain:replaced-pad", "authored building level", building.Id, 6, PadY: 1.3) { Zone = Zone };
        var corridor = new TerrainCorridorOp("design:terrain:replacing-road", "final road constraint", road.Id,
            [new(0, 1.3), new(60, 1.3)], 0, 0.75) { Zone = Zone };
        var context = Context([building]) with { Network = new([road], [], [], [], []) };
        var terrain = TerrainCompiler.Compile(context, [Source([pad, corridor])]);
        AssertThat(Math.Abs((double)(1.3) - (double)(terrain.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1.3) - (double)(terrain.HeightAt(2, 0))) <= TestTolerance.Position).IsTrue();
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
