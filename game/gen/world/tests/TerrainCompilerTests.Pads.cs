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
    public void PadPreFieldIsDiagnosticWhenTheFinalCorridorReplacesItsWholeSection()
    {
        var building = WorldFeatureFixture.Create("fixture:replaced-pad", "building", new PolygonGeometry(Box(-2, -2, 2, 2)), height: 8);
        var road = new NetworkLine(new("fixture:wide-pad-road"), new([new(-30, 0), new(30, 0)]), 30)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var pad = new TerrainPadOp("design:terrain:replaced-pad", "pre-field is not final acceptance", building.Id, 6, PadY: 1.3) { Zone = Zone };
        var corridor = new TerrainCorridorOp("design:terrain:replacing-road", "frozen final road constraint", road.Id,
            [new(0, 1.3), new(60, 1.3)], 0, 0.75) { Zone = Zone };
        var context = Context([building]) with { Network = new([road], [], [], [], []) };
        var terrain = TerrainCompiler.Compile(context, [Source([pad, corridor])]);
        AssertThat(Math.Abs((double)(1.3) - (double)(terrain.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1.3) - (double)(terrain.HeightAt(2, 0))) <= TestTolerance.Position).IsTrue();
        AssertThat(terrain.Diagnostics.Any(message => message.StartsWith("PRE_FIELD:", StringComparison.Ordinal))).OverrideFailureMessage("Pre-pad earthwork evidence disappeared instead of becoming a diagnostic.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PadCoreHeightCannotHideAnUnfinishedFiniteGradingBand()
    {
        var building = WorldFeatureFixture.Create("fixture:unfinished-pad", "building", new PolygonGeometry(Box(-2, -2, 2, 2)), height: 8);
        var pad = new TerrainPadOp("design:terrain:unfinished-pad", "must really return within six metres", building.Id, 6, PadY: 1) { Zone = Zone };
        // The core is exactly PadY and the old pre-field difference is below 1.2 m.
        // Its rounded 1:4 envelope nevertheless has not returned to zero at 6 m.
        {
            WorldDataException? rejection = null;
            try { Compile([pad], [building]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var accepted = Compile([pad with { PadY = 0.2 }], [building]);
        AssertThat(Math.Abs((double)(0.2) - (double)(accepted.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(accepted.HeightAt(9, 0))) <= TestTolerance.Position).IsTrue();
        // A named but distant structure does not cover this pad's actual transition.
        var remote = WorldFeatureFixture.Create("fixture:remote-edge", "edgeBand", new PolygonGeometry(Box(50, 50, 52, 52)));
        var unrelated = pad with { Edge = new(remote.Id.Value, TerrainEdgeKind.EdgeBand, 1, 2, 1) };
        {
            WorldDataException? rejection = null;
            try { Compile([unrelated], [building, remote]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var covering = WorldFeatureFixture.Create("fixture:covering-edge", "edgeBand", new PolygonGeometry(Box(-20, -20, 20, 20)));
        var covered = pad with { Edge = new(covering.Id.Value, TerrainEdgeKind.EdgeBand, 1, 2, 1) };
        AssertThat(Math.Abs((double)(1) - (double)(Compile([covered], [building, covering]).PadY(building.Id))) <= TestTolerance.Position).IsTrue();
        // Structural plan coverage may allow its actual steep transition, but never
        // an unfinished envelope that would jump at the finite grade-width cap.
        {
            WorldDataException? rejection = null;
            try { Compile([covered with { PadY = 4 }], [building, covering]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PadGradingChecksTheFullGradientAndAcceptsTheOneInFourBoundary()
    {
        var diagonalComponent = 0.25 * Math.Sqrt(0.5);
        var diagonalSquared = diagonalComponent * diagonalComponent + diagonalComponent * diagonalComponent;
        AssertThat(diagonalSquared > 1d / 16 && !TerrainCompiler.PadSquaredGradeExceedsComparison(diagonalSquared)).OverrideFailureMessage("A non-axis-aligned theoretical 1:4 gradient was rejected for its one-ULP squared-norm rounding.").IsTrue();
        AssertThat(TerrainCompiler.PadSquaredGradeExceedsComparison(0.250002 * 0.250002)
            && TerrainCompiler.PadSquaredGradeExceedsComparison(double.PositiveInfinity)
            && TerrainCompiler.PadSquaredGradeExceedsComparison(double.NaN)).OverrideFailureMessage("The shared comparison accepted a genuine excess or a non-finite two-dimensional grade.").IsTrue();
        var longBuilding = WorldFeatureFixture.Create("fixture:tangential-pad", "building", new PolygonGeometry(Box(-1, -30, 1, 30)), height: 8);
        // The narrow mound varies along the building's long sides, far from its ends.
        // Their outward-ray projection stays within 1:4 while the tangential component
        // leaves the final directly affected blend steeper than 1:4 in two dimensions.
        var mound = new TerrainMoundOp("design:terrain:tangential-mound", "full-gradient pad regression",
            new TerrainEllipse(Vector2.Zero, new(100, 2), 90), 0.6) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { Compile([mound], [longBuilding]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }

        var building = WorldFeatureFixture.Create("fixture:quarter-grade-pad", "building", new PolygonGeometry(Box(-2, -20, 2, 20)), height: 8);
        var plateau = new TerrainPlatformOp("design:terrain:quarter-grade-base", "exact binary boundary fixture",
            Box(-40, -40, 40, 40), 0.75, new("1:4", "1:4"), 2) { Zone = Zone };
        var pad = new TerrainPadOp("design:terrain:quarter-grade-pad", "one-in-four boundary is legal",
            building.Id, 6, PadY: 0, Apron: 0) { Zone = Zone };
        var terrain = Compile([plateau, pad], [building]);
        // Two metres beyond this straight edge the actual generated profile has
        // H=.25 and |gradient|=.25, then returns exactly to H=.75 within six metres.
        AssertThat(Math.Abs((double)(0.25) - (double)(terrain.HeightAt(4, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.25) - (double)(terrain.FactsAt(4, 0).Grade)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0.75) - (double)(terrain.HeightAt(8, 0))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PadCourtyardNeedsARealReturnAndConcaveOffsetCornersUseMiters()
    {
        WorldFeature Courtyard(float half) => WorldFeatureFixture.Create("fixture:courtyard-pad", "building", new PolygonGeometry(new PolygonRings(Box(-20, -20, 20, 20).Outer, [Box(-half, -half, half, half).Outer])), height: 8);
        var small = Courtyard(1);
        var pad = new TerrainPadOp("design:terrain:courtyard-pad", "actual hole-side grading", small.Id, 6, PadY: 0.4) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { Compile([pad], [small]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        // Near an inside corner an original footprint normal hits the other edge;
        // that is not proof that a sufficiently large courtyard has no grading space.
        // The corner's diagonal section exceeds 6 m, but its true distance offset
        // returns within 6 m just like the straight edges of the same courtyard.
        var large = Compile([pad], [Courtyard(8)]);
        AssertThat(Math.Abs((double)(0) - (double)(large.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.4) - (double)(large.PadY(small.Id))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void RealFourPartPadContinuesPastAnIncreasingNearestEdgeSwitch()
    {
        PolygonRings[] wings = [
            new([new(-685.45f,372.8f),new(-678.04f,378.16f),new(-673.77f,372.16f),new(-680.92f,366.79f),
                new(-677.65f,362.48f),new(-671.87f,366.8f),new(-662.43f,373.92f),new(-667.34f,380.67f),
                new(-635.25f,404.76f),new(-635.32f,406.55f),new(-622.72f,415.31f),new(-629.28f,432.33f),new(-693.93f,384.04f)]),
            new([new(-614.95f,393.89f),new(-664.64f,357.22f),new(-653.77f,342.81f),new(-587.11f,392.59f),
                new(-607.56f,448.56f),new(-629.28f,432.33f),new(-622.72f,415.31f)]),
            new([new(-660.78f,360.05f),new(-667.97f,369.8f),new(-671.87f,366.8f),new(-664.64f,357.22f)]),
            new([new(-680.92f,366.79f),new(-673.77f,372.16f),new(-678.04f,378.16f),new(-685.45f,372.8f)])];
        string[] sources = ["osm:relation:10807928", "osm:way:779496157", "osm:way:779496158", "osm:way:842746200"];
        var exposed = TerrainUnion.Build([new TerrainRegion(wings)]);
        const double x = -635.25, z = 404.760009765625;
        const double nx = 0.9992360522213667, nz = 0.039080838541203894;
        var trace = TerrainCompiler.TracePadRayBoundary(exposed, TerrainRegion.Expand(exposed.Bounds, 6.8), x, z, nx, nz, 6.8);
        AssertThat(trace.End > 4.42 && exposed.Distance(x + nx * trace.End, z + nz * trace.End).Height >= 3.88).OverrideFailureMessage("An increasing takeover by the adjacent slanted edge still truncates the legal grading band.").IsTrue();
        const double ux = -679.8299967447916, uz = 365.3533426920573;
        const double unx = -0.796664108638979, unz = -0.6044222845051802;
        var outward = TerrainCompiler.TracePadRayBoundary(exposed, TerrainRegion.Expand(exposed.Bounds, 6.8), ux, uz, unx, unz, 6.8);
        AssertThat(outward.End > 0.8 && exposed.Distance(ux + unx * outward.End, uz + unz * outward.End).Height > 0.8).OverrideFailureMessage("A parameter-only ULP interval was mistaken for the actual outward band's end.").IsTrue();
        var inward = TerrainCompiler.TracePadRayBoundary(exposed, TerrainRegion.Expand(exposed.Bounds, 6.8), ux, uz, -unx, -unz, 6.8);
        AssertThat(inward.End <= TestTolerance.Position).OverrideFailureMessage("A reversed boundary ray acquired an outward grading strip through the building core.").IsTrue();
        var building = new WorldFeature(new("qsh:relation:10807928"), FeatureSource.Osm, new("building"), new MultiPolygonGeometry(wings))
        {
            Height = 20,
            Parts = wings.Select((wing, i) => new WorldFeaturePart(sources[i], new PolygonGeometry(wing), 20, 5) { Props = Kind("building") }).ToArray()
        };
        var context = Context([building]) with
        {
            DataRect = new(new(-704, 336), new(-576, 464)), Boundary = Box(-1024, -1024, 1024, 1024),
            Zones = new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [Zone] = [Box(-1024, -1024, 1024, 1024)] }
        };
        var terrain = TerrainCompiler.Compile(context, []);
        AssertThat(Math.Abs((double)(0) - (double)(terrain.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(-630.82035f, 404.93326f))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PadRayStopsAtTheOpposingWallMaximumAndHandlesAnExactPlateau()
    {
        var courtyard = new TerrainRegion([new PolygonRings(Box(-20, -20, 20, 20).Outer, [Box(-4, -4, 4, 4).Outer])]);
        var trace = TerrainCompiler.TracePadRayBoundary(courtyard, TerrainRegion.Expand(courtyard.Bounds, 6.8), -4, 0, 1, 0, 6.8);
        AssertThat(Math.Abs(trace.End - 4) < TestTolerance.Position).OverrideFailureMessage("The ray borrowed grading space from the opposing courtyard wall.").IsTrue();
        // This ray first grows away from the left wall, then has an exact 2 m
        // plateau under the top/bottom walls, then turns towards the right wall.
        var slot = new TerrainRegion([new PolygonRings(Box(-20, -20, 20, 20).Outer, [Box(-8, -2, 8, 2).Outer])]);
        var plateau = TerrainCompiler.TracePadRayBoundary(slot, TerrainRegion.Expand(slot.Bounds, 6.8), -8, 0, 1, 0, 6.8);
        AssertThat(Math.Abs(plateau.End - 14) < TestTolerance.Position).OverrideFailureMessage("An exact plateau was mistaken for a return or a numerical derivative epsilon.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PadCounterfactualSkipsOnlyTheExactNodeAndKeepsLaterConstraintsFrozen()
    {
        var support = new MapRect(new(-10), new(10));
        var pad = new TerrainNode("fixture:pad", "fixture", Zone, TerrainGroup.Pad, 50, support,
            (before, _, _) => before with { Surface = TerrainDifferential.Constant(2) });
        var equalButDistinct = pad with { };
        var road = new TerrainNode("fixture:road", "fixture", Zone, TerrainGroup.Corridor, 50, support,
            (before, _, _) => before with { Surface = before.Surface * 0.5 });
        var program = new TerrainProgram([pad, road], (_, _) => TerrainDifferential.Constant(0.75));
        AssertThat(Math.Abs((double)(1) - (double)(program.Evaluate(0, 0).Surface.Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(program.EvaluateExcept(0, 0, pad).Surface.Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(1) - (double)(program.EvaluateExcept(0, 0, equalButDistinct).Surface.Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0.75) - (double)(program.EvaluateExcept(0, 0, pad).BoundaryWeight!.Value.Height)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AutoPadTouchesProtectedBandAndApronJoinsWithoutJump()
    {
        var building = WorldFeatureFixture.Create("fixture:building", "building", new PolygonGeometry(Box(19, 40, 32, 60)), height: 8);
        var context = Context([building]) with { Boundary = Box(0, 0, 200, 200), DataRect = new(new(0), new(200)) };
        var terrain = TerrainCompiler.Compile(context, []);
        AssertThat(Math.Abs((double)(0) - (double)(terrain.PadY(building.Id))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(19.5f, 50))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(18.5f, 50))) <= TestTolerance.Position).IsTrue();
        var inside = terrain.HeightAt(32.7999f, 50); var outside = terrain.HeightAt(32.8001f, 50);
        AssertThat(Math.Abs(inside - outside) < TestTolerance.Position).OverrideFailureMessage("Apron edge reset to PadY and made a vertical seam.").IsTrue();
    }

}
