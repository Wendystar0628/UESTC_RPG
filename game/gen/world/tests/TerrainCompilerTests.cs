using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class TerrainCompilerTests
{
    private static readonly ZoneId Zone = new("fixture");
    private static readonly TerrainAutoSettings Defaults = new(0.2, 1.5, 0.25, 1.2, 0.75);

    [TestCase]
    public void ZeroBandTaperHasPrescribedValuesAndZeroEndpointSlopes()
    {
        var left = TerrainCompiler.ZeroBandWeight(new(20, 1, 0));
        var right = TerrainCompiler.ZeroBandWeight(new(30, 1, 0));
        var middle = TerrainCompiler.ZeroBandWeight(new(25, 1, 0));
        Equal(0, left.Height); Equal(0, left.Dx); Equal(1, right.Height); Equal(0, right.Dx);
        Equal(0.5, middle.Height); Equal(0.15, middle.Dx);
        // Independent endpoint limits: cubic Hermite has O(epsilon^2) value error
        // and O(epsilon) slope error. Test both sides, not a second polynomial implementation.
        const double e = 0.0001;
        Require(TerrainCompiler.ZeroBandWeight(new(20 - e, 1, 0)) == left, "Protected side is not constant.");
        Require(TerrainCompiler.ZeroBandWeight(new(30 + e, 1, 0)) == right, "Interior side is not constant.");
        var nearLeft = TerrainCompiler.ZeroBandWeight(new(20 + e, 1, 0));
        var nearRight = TerrainCompiler.ZeroBandWeight(new(30 - e, 1, 0));
        Require(nearLeft.Height / e < 0.01 && (1 - nearRight.Height) / e < 0.01, "Taper has a height corner.");
        Require(Math.Abs(nearLeft.Dx) < 0.01 && Math.Abs(nearRight.Dx) < 0.01, "Taper endpoint derivative is not continuous.");
    }

    [TestCase]
    public void RegionalPriorityAndOrdinalIdHaveNoHiddenTypeOrder()
    {
        TerrainOp mound = new TerrainMoundOp("design:terrain:a-mound", "fixture", new TerrainCircle(Vector2.Zero, 20), 1, Priority: 300) { Zone = Zone };
        TerrainOp platform = new TerrainPlatformOp("design:terrain:z-platform", "fixture", Box(-4, -4, 4, 4), 2, new("1:4", "1:4"), 2, 300) { Zone = Zone };
        var first = Compile([mound, platform]); var second = Compile([platform, mound]);
        Equal(2, first.HeightAt(0, 0));
        var a = new short[4225]; var b = new short[4225]; first.FillTile(default, 0, a); second.FillTile(default, 0, b);
        Require(a.SequenceEqual(b), "Input enumeration changed terrain bytes.");
        var later = mound with { Id = "design:terrain:zz-mound" };
        Equal(3, Compile([later, platform]).HeightAt(0, 0));
    }

    [TestCase]
    public void PlatformHoleAndTwoPointSlopeKeepTheirGeometry()
    {
        var outer = Box(-30, -30, 30, 30);
        var ring = new PolygonRings(outer.Outer, [Box(-15, -15, 15, 15).Outer]);
        var platform = new TerrainPlatformOp("design:terrain:platform", "fixture", ring, 1, new("1:4", "1:4"), 2) { Zone = Zone };
        var terrain = Compile([platform]); Equal(0, terrain.HeightAt(0, 0)); Equal(1, terrain.HeightAt(20, 0));
        var slope = new TerrainSlopeOp("design:terrain:slope", "fixture", new PolygonGeometry(Box(-15, -5, 15, 5)), 0, 1, 0,
            new("1:4", "1:4"), 2, new(-10, 0), new(10, 0)) { Zone = Zone };
        var ramp = Compile([slope]); Equal(0, ramp.HeightAt(-12, 0)); Equal(0.5, ramp.HeightAt(0, 0)); Equal(1, ramp.HeightAt(12, 0));
        Equal(0.05, ramp.FactsAt(0, 0).Grade);
    }

    [TestCase]
    public void FixedRoadAnchorsSurviveSmoothingAndInfeasibleGradeFails()
    {
        var profile = new TerrainRoadProfile([new(0, 0), new(20, 0.5), new(40, 0)], 40, [], 0.05, "fixture", "road");
        Equal(0, profile.At(0).Y); Equal(0.5, profile.At(20).Y); Equal(0, profile.At(40).Y);
        Equal(0, profile.At(20).Grade);
        Reject(() => new TerrainRoadProfile([new(0, 0), new(10, 2)], 10, [], 0.05, "fixture", "road"), "Anchors were not moved");
        Reject(() => new TerrainRoadProfile([new(0, 0), new(9, 0)], 10, [], 0.05, "fixture", "road"), "actual centreline length");
        var roundedEnd = new TerrainRoadProfile([new(0, 0), new(55.5, 1)], 55.49793264276604, [], 0.05, "fixture", "rounded-end");
        Equal(1, roundedEnd.At(roundedEnd.Length).Y);
        Equal(roundedEnd.Length, TerrainRoadProfile.ResolveStation(55.5, 55.5, roundedEnd.Length, "fixture", "steps"));
        Reject(() => TerrainRoadProfile.ResolveStation(55.499, 55.5, roundedEnd.Length, "fixture", "steps"), "only the declared terminal anchor");
    }

    [TestCase]
    public void ExplicitCorridorRangeIsUnsupportedEvenForTheFullValidSource()
    {
        var road = new NetworkLine(new("fixture:range-road"), new([new(-10, 0), new(10, 0)]), 2.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([]) with { Network = new([road], [], [], [], []) };
        var full = new TerrainCorridorOp("design:terrain:range-road", "explicit interval capability boundary", road.Id,
            [new(0, 0.5), new(20, 0.5)], 0, 0.75) { Zone = Zone };
        foreach (var range in new[] { new TerrainStationRange(5, 15), new TerrainStationRange(0, 20) })
        {
            var ranged = full with { Range = range, Anchors = [new(range.S0, 0.5), new(range.S1, 0.5)] };
            var source = Source([ranged]);
            try
            {
                TerrainCompiler.Compile(context, [source]);
                throw new InvalidOperationException("An explicit Range was silently treated as a full-source corridor.");
            }
            catch (WorldDataException error)
            {
                Require(error.File == source.File && error.Entry == ranged.Id
                    && error.Message.Contains("Unsupported corridor Range", StringComparison.Ordinal),
                    "Range did not fail explicitly at its own source entry: " + error.Message);
            }
        }
        var unchanged = TerrainCompiler.Compile(context, [Source([full with { Range = null }])]);
        Equal(0.5, unchanged.HeightAt(0, 0));
    }

    [TestCase]
    public void AutomaticCorridorUsesNonRoadPlateauAndExplicitAnchorsOverrideIt()
    {
        var road = new NetworkLine(new("fixture:plateau-road"), new([new(-30, 0), new(30, 0)]), 2.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var plateau = new TerrainPlatformOp("design:terrain:road-plateau", "terrain-following road regression",
            Box(-80, -80, 80, 80), 4, new("1:4", "1:4"), 2) { Zone = Zone };
        var context = Context([]) with { Network = new([road], [], [], [], []) };
        var automatic = TerrainCompiler.Compile(context, [Source([plateau])]);
        Equal(4, automatic.HeightAt(0, 0)); Equal(4, automatic.HeightAt(-30, 0)); Equal(4, automatic.HeightAt(30, 0));
        var explicitRoad = new TerrainCorridorOp("design:terrain:authored-road", "explicit anchors win", road.Id,
            [new(0, 5), new(60, 5)], 0, 0.75) { Zone = Zone };
        var authored = TerrainCompiler.Compile(context, [Source([plateau, explicitRoad])]);
        Equal(5, authored.HeightAt(0, 0)); Equal(5, authored.HeightAt(-30, 0));
    }

    [TestCase]
    public void AutomaticTerrainConsumesEffectiveDesignRoadTypeAndLegacyHighway()
    {
        foreach (var (kind, legacy) in new[] { ("footway", false), ("path", false), ("pedestrian", false), ("footway", true) })
        {
            var road = new NetworkLine(new("design:road:effective-" + kind), new([new(-10, 0), new(10, 0)]), 2.5f)
            {
                Props = legacy ? JsonSerializer.SerializeToElement(new { highway = kind })
                    : JsonSerializer.SerializeToElement(new { roadType = kind })
            };
            var context = Context([]) with { Network = new([road], [], [], [], []) };
            var references = new TerrainReferences(context);
            Require(references.Road(road.Id, "fixture", "effective-road").Kind == kind
                && references.AutomaticRoads().Single().Kind == kind, "Explicit and automatic road classification disagree.");
            var terrain = TerrainCompiler.Compile(context, []);
            Require(!terrain.IsFlat(default), "An effective WorldNetwork road was ignored by automatic terrain.");
            Equal(0.0075, terrain.HeightAt(0, 0.5f)); Equal(-0.0075, terrain.HeightAt(0, -0.5f));
        }
    }

    [TestCase]
    public void AutomaticNetworkFitMovesSoftSamplesAndKeepsSharedAuthorNodes()
    {
        var a = new TerrainRoad(new("fixture:left-road"), new("fixture:left-road"), new(new([new(-10, 0), new(0, 0)])), 2.5, "footway", null);
        var c = new TerrainRoad(new("fixture:right-road"), new("fixture:right-road"), new(new([new(0, 0), new(10, 0)])), 2.5, "footway", null);
        var baseline = new TerrainProgram([new("base", "fixture", Zone, TerrainGroup.Regional, 0, new(new(-20), new(20)),
            (previous, x, _) => previous with { Surface = new(x * 0.2, 0.2, 0) })]);
        TerrainRoadProfile.Request[] requests = [new(a, [], 1d / 12), new(c, [], 1d / 12)];
        var fitted = TerrainRoadProfile.SmoothNetwork(requests, baseline, [], (_, _) => false);
        var reverse = TerrainRoadProfile.SmoothNetwork(requests.Reverse().ToArray(), baseline, [], (_, _) => false);
        Equal(0, fitted[a.Source].At(10).Y); Equal(0, fitted[c.Source].At(0).Y);
        Equal(-0.3, fitted[a.Source].At(0).Y); Equal(0.3, fitted[c.Source].At(10).Y);
        for (var s = 0d; s <= 10; s += 0.25)
        {
            Equal(fitted[a.Source].At(s).Y, reverse[a.Source].At(s).Y);
            Require(Math.Abs(fitted[a.Source].At(s).Grade) <= 0.031, "Default network smoothing broke the shared-node grade bound.");
        }
        var authorRoad = new TerrainRoad(new("fixture:author-road"), new("fixture:author-road"), new(new([new(0, 0), new(0, 10)])), 2.5, "footway", null);
        var authorProfile = new TerrainRoadProfile([new(0, 1), new(10, 1)], 10, [], 1d / 12, "fixture", "author");
        var fixedFit = TerrainRoadProfile.SmoothNetwork(requests, baseline, [(authorRoad, authorProfile)], (_, _) => false);
        Equal(1, fixedFit[a.Source].At(10).Y); Equal(1, fixedFit[c.Source].At(0).Y);
        Reject(() => TerrainRoadProfile.SmoothNetwork(requests, baseline, [(authorRoad, authorProfile)], (x, _) => x == 0), "protected zero band");
    }

    [TestCase]
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
        Require(Math.Abs(endProfile.At(10).Y - endpointHeight) < 0.01
            && Math.Abs(throughProfile.At(10).Y - interiorHeight) < 0.01,
            "Junction tolerance moved or averaged authored profiles.");
        Require(Math.Abs(fitted[automatic.Source].At(0).Y - interiorHeight) < 0.01,
            "The existing last-authored lock value was replaced or averaged.");
        Require(endAnchors[0].Y == endpointHeight && endAnchors[1].Y == endpointHeight
            && throughAnchors[0].Y == interiorHeight - 0.1 && throughAnchors[1].Y == interiorHeight + 0.1,
            "Junction matching mutated the original authored anchor values.");
        var incompatible = new TerrainRoadProfile([new(0, interiorHeight - 0.1 + 0.05), new(20, interiorHeight + 0.1 + 0.05)],
            20, [], 1d / 12, "through.terrain.json", "design:terrain:incompatible-road");
        Reject(() => TerrainRoadProfile.SmoothNetwork([], new TerrainProgram([]),
            [(endRoad, endProfile), (throughRoad, incompatible)], (_, _) => false), "shared source node");
    }

    [TestCase]
    public void PadBoundaryValidationUsesTheFinalCorridorSurface()
    {
        var building = Feature("fixture:final-pad", "building", Box(-2, -8, 2, 8));
        var road = new NetworkLine(new("fixture:pad-road"), new([new(-30, 0), new(30, 0)]), 2.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([building]) with { Network = new([road], [], [], [], []) };
        TerrainCorridorOp Corridor(double y) => new("design:terrain:pad-road", "final-ground regression", road.Id,
            [new(0, y), new(60, y)], 0, 0.75) { Zone = Zone };
        // All original building corners remain clear of the road. The final mid-edge sample
        // must still see a later corridor crossing; the earlier pre-pad field was flat zero.
        Reject(() => TerrainCompiler.Compile(context, [Source([Corridor(1.3)])]), "after all corridors");
        var narrow = new TerrainPadOp("design:terrain:narrow-pad", "grade width is still binding", building.Id, 1, PadY: 0) { Zone = Zone };
        Reject(() => TerrainCompiler.Compile(context, [Source([narrow, Corridor(0.3)])]), "after all corridors");
        var accepted = TerrainCompiler.Compile(context, [Source([Corridor(0.2)])]);
        Equal(0, accepted.PadY(building.Id)); Equal(0.2, accepted.HeightAt(2, 0));
    }

    [TestCase]
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
            Equal(0, terrain.PadY(building.Id)); Equal(0, terrain.HeightAt(0, 0));
            Equal(0, terrain.HeightAt(-2, 0)); Equal(0, terrain.HeightAt(2, 0));
        }
    }

    [TestCase]
    public void PadPreFieldIsDiagnosticWhenTheFinalCorridorReplacesItsWholeSection()
    {
        var building = Feature("fixture:replaced-pad", "building", Box(-2, -2, 2, 2));
        var road = new NetworkLine(new("fixture:wide-pad-road"), new([new(-30, 0), new(30, 0)]), 30)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var pad = new TerrainPadOp("design:terrain:replaced-pad", "pre-field is not final acceptance", building.Id, 6, PadY: 1.3) { Zone = Zone };
        var corridor = new TerrainCorridorOp("design:terrain:replacing-road", "frozen final road constraint", road.Id,
            [new(0, 1.3), new(60, 1.3)], 0, 0.75) { Zone = Zone };
        var context = Context([building]) with { Network = new([road], [], [], [], []) };
        var terrain = TerrainCompiler.Compile(context, [Source([pad, corridor])]);
        Equal(1.3, terrain.PadY(building.Id)); Equal(1.3, terrain.HeightAt(2, 0));
        Require(terrain.Diagnostics.Any(message => message.StartsWith("PRE_FIELD:", StringComparison.Ordinal)),
            "Pre-pad earthwork evidence disappeared instead of becoming a diagnostic.");
    }

    [TestCase]
    public void PadCoreHeightCannotHideAnUnfinishedFiniteGradingBand()
    {
        var building = Feature("fixture:unfinished-pad", "building", Box(-2, -2, 2, 2));
        var pad = new TerrainPadOp("design:terrain:unfinished-pad", "must really return within six metres", building.Id, 6, PadY: 1) { Zone = Zone };
        // The core is exactly PadY and the old pre-field difference is below 1.2 m.
        // Its rounded 1:4 envelope nevertheless has not returned to zero at 6 m.
        Reject(() => Compile([pad], [building]), "Pad final grading after all corridors");
        var accepted = Compile([pad with { PadY = 0.2 }], [building]);
        Equal(0.2, accepted.PadY(building.Id)); Equal(0, accepted.HeightAt(9, 0));
        // A named but distant structure does not cover this pad's actual transition.
        var remote = Feature("fixture:remote-edge", "edgeBand", Box(50, 50, 52, 52));
        var unrelated = pad with { Edge = new(remote.Id.Value, TerrainEdgeKind.EdgeBand, 1, 2, 1) };
        Reject(() => Compile([unrelated], [building, remote]), "final grading");
        var covering = Feature("fixture:covering-edge", "edgeBand", Box(-20, -20, 20, 20));
        var covered = pad with { Edge = new(covering.Id.Value, TerrainEdgeKind.EdgeBand, 1, 2, 1) };
        Equal(1, Compile([covered], [building, covering]).PadY(building.Id));
        // Structural plan coverage may allow its actual steep transition, but never
        // an unfinished envelope that would jump at the finite grade-width cap.
        Reject(() => Compile([covered with { PadY = 4 }], [building, covering]),
            "Pad final grading after all corridors");
    }

    [TestCase]
    public void PadGradingChecksTheFullGradientAndAcceptsTheOneInFourBoundary()
    {
        var diagonalComponent = 0.25 * Math.Sqrt(0.5);
        var diagonalSquared = diagonalComponent * diagonalComponent + diagonalComponent * diagonalComponent;
        Require(diagonalSquared > 1d / 16 && !TerrainCompiler.PadSquaredGradeExceedsComparison(diagonalSquared),
            "A non-axis-aligned theoretical 1:4 gradient was rejected for its one-ULP squared-norm rounding.");
        Require(TerrainCompiler.PadSquaredGradeExceedsComparison(0.250002 * 0.250002)
            && TerrainCompiler.PadSquaredGradeExceedsComparison(double.PositiveInfinity)
            && TerrainCompiler.PadSquaredGradeExceedsComparison(double.NaN),
            "The shared comparison accepted a genuine excess or a non-finite two-dimensional grade.");
        var longBuilding = Feature("fixture:tangential-pad", "building", Box(-1, -30, 1, 30));
        // The narrow mound varies along the building's long sides, far from its ends.
        // Their outward-ray projection stays within 1:4 while the tangential component
        // leaves the final directly affected blend steeper than 1:4 in two dimensions.
        var mound = new TerrainMoundOp("design:terrain:tangential-mound", "full-gradient pad regression",
            new TerrainEllipse(Vector2.Zero, new(100, 2), 90), 0.6) { Zone = Zone };
        Reject(() => Compile([mound], [longBuilding]), "two-dimensional final grade");

        var building = Feature("fixture:quarter-grade-pad", "building", Box(-2, -20, 2, 20));
        var plateau = new TerrainPlatformOp("design:terrain:quarter-grade-base", "exact binary boundary fixture",
            Box(-40, -40, 40, 40), 0.75, new("1:4", "1:4"), 2) { Zone = Zone };
        var pad = new TerrainPadOp("design:terrain:quarter-grade-pad", "one-in-four boundary is legal",
            building.Id, 6, PadY: 0, Apron: 0) { Zone = Zone };
        var terrain = Compile([plateau, pad], [building]);
        // Two metres beyond this straight edge the actual generated profile has
        // H=.25 and |gradient|=.25, then returns exactly to H=.75 within six metres.
        Equal(0.25, terrain.HeightAt(4, 0)); Equal(0.25, terrain.FactsAt(4, 0).Grade);
        Equal(0.75, terrain.HeightAt(8, 0));
    }

    [TestCase]
    public void PadCourtyardNeedsARealReturnAndConcaveOffsetCornersUseMiters()
    {
        WorldFeature Courtyard(float half) => Feature("fixture:courtyard-pad", "building",
            new PolygonRings(Box(-20, -20, 20, 20).Outer, [Box(-half, -half, half, half).Outer]));
        var small = Courtyard(1);
        var pad = new TerrainPadOp("design:terrain:courtyard-pad", "actual hole-side grading", small.Id, 6, PadY: 0.4) { Zone = Zone };
        Reject(() => Compile([pad], [small]), "does not reach the same final ground without this pad");
        // Near an inside corner an original footprint normal hits the other edge;
        // that is not proof that a sufficiently large courtyard has no grading space.
        // The corner's diagonal section exceeds 6 m, but its true distance offset
        // returns within 6 m just like the straight edges of the same courtyard.
        var large = Compile([pad], [Courtyard(8)]);
        Equal(0, large.HeightAt(0, 0)); Equal(0.4, large.PadY(small.Id));
    }

    [TestCase]
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
        Require(trace.End > 4.42 && exposed.Distance(x + nx * trace.End, z + nz * trace.End).Height >= 3.88,
            "An increasing takeover by the adjacent slanted edge still truncates the legal grading band.");
        const double ux = -679.8299967447916, uz = 365.3533426920573;
        const double unx = -0.796664108638979, unz = -0.6044222845051802;
        var outward = TerrainCompiler.TracePadRayBoundary(exposed, TerrainRegion.Expand(exposed.Bounds, 6.8), ux, uz, unx, unz, 6.8);
        Require(outward.End > 0.8 && exposed.Distance(ux + unx * outward.End, uz + unz * outward.End).Height > 0.8,
            "A parameter-only ULP interval was mistaken for the actual outward band's end.");
        var inward = TerrainCompiler.TracePadRayBoundary(exposed, TerrainRegion.Expand(exposed.Bounds, 6.8), ux, uz, -unx, -unz, 6.8);
        Require(inward.End <= 0.01,
            "A reversed boundary ray acquired an outward grading strip through the building core.");
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
        Equal(0, terrain.PadY(building.Id)); Equal(0, terrain.HeightAt(-630.82035f, 404.93326f));
    }

    [TestCase]
    public void PadRayStopsAtTheOpposingWallMaximumAndHandlesAnExactPlateau()
    {
        var courtyard = new TerrainRegion([new PolygonRings(Box(-20, -20, 20, 20).Outer, [Box(-4, -4, 4, 4).Outer])]);
        var trace = TerrainCompiler.TracePadRayBoundary(courtyard, TerrainRegion.Expand(courtyard.Bounds, 6.8), -4, 0, 1, 0, 6.8);
        Require(Math.Abs(trace.End - 4) < 0.01, "The ray borrowed grading space from the opposing courtyard wall.");
        // This ray first grows away from the left wall, then has an exact 2 m
        // plateau under the top/bottom walls, then turns towards the right wall.
        var slot = new TerrainRegion([new PolygonRings(Box(-20, -20, 20, 20).Outer, [Box(-8, -2, 8, 2).Outer])]);
        var plateau = TerrainCompiler.TracePadRayBoundary(slot, TerrainRegion.Expand(slot.Bounds, 6.8), -8, 0, 1, 0, 6.8);
        Require(Math.Abs(plateau.End - 14) < 0.01, "An exact plateau was mistaken for a return or a numerical derivative epsilon.");
    }

    [TestCase]
    public void PadCounterfactualSkipsOnlyTheExactNodeAndKeepsLaterConstraintsFrozen()
    {
        var support = new MapRect(new(-10), new(10));
        var pad = new TerrainNode("fixture:pad", "fixture", Zone, TerrainGroup.Pad, 50, support,
            (before, _, _) => before with { Surface = TerrainDifferential.Constant(2) });
        var equalButDistinct = pad with { };
        var road = new TerrainNode("fixture:road", "fixture", Zone, TerrainGroup.Corridor, 50, support,
            (before, _, _) => before with { Surface = before.Surface * 0.5 });
        var program = new TerrainProgram([pad, road], (_, _) => TerrainDifferential.Constant(0.75));
        Equal(1, program.Evaluate(0, 0).Surface.Height);
        Equal(0, program.EvaluateExcept(0, 0, pad).Surface.Height);
        Equal(1, program.EvaluateExcept(0, 0, equalButDistinct).Surface.Height);
        Equal(0.75, program.EvaluateExcept(0, 0, pad).BoundaryWeight!.Value.Height);
    }

    [TestCase]
    public void RoadCrossfallOffsetIsContinuousAcrossAnInternalCorner()
    {
        var path = new TerrainPath(new([new(0, 10), new(0, 0), new(-10, 0)]));
        const double e = 0.00001;
        var a = path.Project(-e, -1); var c = path.Project(e, -1);
        Equal(1, a.Offset); Require(Math.Abs(a.Offset - c.Offset) < 0.01, "Crossfall jumped at an internal segment's clamped endpoint.");
        Equal(Math.Sqrt(2), path.Project(1, -1).Offset);
        var straight = new TerrainPath(new([new(0, 0), new(0, 10)]));
        Equal(0, straight.Project(0, -2).Offset);
    }

    [TestCase]
    public void RoadLongitudinalSurfaceIsContinuousAcrossTheInnerCorner()
    {
        var path = new TerrainPath(new([new(0, 10), new(0, 0), new(-10, 0)]));
        var profile = new TerrainRoadProfile([new(0, 0), new(20, 0.2)], 20, [], 1d / 12, "fixture", "corner");
        TerrainDifferential Sample(double x, double z) => profile.SurfaceAt(path, x, z, 2.5, path.Project(x, z));
        const double e = 0.00001;
        Require(Math.Abs(Sample(-1 - e, 1).Height - Sample(-1 + e, 1).Height) < 0.01,
            "Longitudinal height selected discontinuous nearest-segment station values at the inner bend.");
        Equal(0.1, Sample(-1, 1).Height); Equal(0.09, Sample(0, 1).Height); Equal(0.11, Sample(-1, 0).Height);
        Equal(0, Sample(0, 10).Height); Equal(0.2, Sample(-10, 0).Height);
    }

    [TestCase]
    public void StructuralRoadEndpointRetainsItsExactSourceStation()
    {
        var path = new TerrainPath(new([new(-465.7f, 66.61f), new(-436.57f, 111.36f)]));
        var end = path.Line.Points[^1];
        Equal(path.Length, path.Project(end.X, end.Y).Station);
        var profile = new TerrainRoadProfile([new(0, 0), new(path.Length, 0)], path.Length,
            [(0, path.Length - 0.00000000000001)], 1d / 12, "fixture", "bridge");
        Require(profile.Excluded(path.Length), "Floating-point endpoint residue invented a ground-road sliver after a complete bridge.");
        Require(!profile.Excluded(path.Length + 0.01), "Numerical endpoint tolerance expanded into geometric coverage.");
    }

    [TestCase]
    public void RealSegmentIntersectionRejectsNearbyParallelProjection()
    {
        var line = new TerrainPath(new([new(-20, 0), new(20, 0)]));
        var parallel = new TerrainPath(new([new(-20, 0.5f), new(20, 0.5f)]));
        Require(line.Intersections(parallel).Count == 0, "Close projections manufactured a crossing.");
        var cross = line.Intersections(new(new([new(3, -10), new(3, 10)])));
        Require(cross.Count == 1, "Actual crossing missing."); Equal(3, cross[0].X); Equal(23, cross[0].Station); Equal(10, cross[0].OtherStation);
    }

    [TestCase]
    public void EndCapDistanceHasBoundedSlopeAtBothFlanksAndObliqueRoadClearance()
    {
        const double radius = 3;
        var tip = TerrainCompiler.EndCapDistance(3, 0, Vector2.Zero, 1, 0, radius);
        var flank = TerrainCompiler.EndCapDistance(0, 3, Vector2.Zero, 1, 0, radius);
        var centreline = TerrainCompiler.EndCapDistance(1, 0, Vector2.Zero, 1, 0, radius);
        Equal(0, tip.Height); Equal(-1, tip.Dx); Equal(0, flank.Height); Equal(-1, flank.Dz); Equal(2, centreline.Height);
        foreach (var point in new[] { new Vector2(0.00001f, 2.99999f), new Vector2(-0.00001f, 2.99999f), new Vector2(1, 2), new Vector2(-1, 2) })
        {
            var d = TerrainCompiler.EndCapDistance(point.X, point.Y, Vector2.Zero, 1, 0, radius);
            Require(double.IsFinite(d.Dx) && double.IsFinite(d.Dz) && d.Dx * d.Dx + d.Dz * d.Dz <= 1.01,
                "End-cap distance has an unbounded flank derivative.");
        }
        // A 4 m-wide north/south road occupies x >= -2 on its near side. A radius-3
        // cap centred at x=-6 has exactly 1 m clearance, independently of channel yaw.
        var center = new Vector2(-6, 0); var road = new TerrainPath(new([new(0, -100), new(0, 100)]));
        Equal(1, road.Project(center.X, center.Y).Distance - radius - 2);
        var incorrectlyPlacedCenter = new Vector2(-3, 0);
        Require(road.Project(incorrectlyPlacedCenter.X, 0).Distance - radius - 2 < 0, "Fixture failed to expose centre-versus-cap clearance.");
    }

    [TestCase]
    public void AutoPadTouchesProtectedBandAndApronJoinsWithoutJump()
    {
        var building = Feature("fixture:building", "building", Box(19, 40, 32, 60));
        var context = Context([building]) with { Boundary = Box(0, 0, 200, 200), DataRect = new(new(0), new(200)) };
        var terrain = TerrainCompiler.Compile(context, []);
        Equal(0, terrain.PadY(building.Id)); Equal(0, terrain.HeightAt(19.5f, 50));
        Equal(0, terrain.HeightAt(18.5f, 50));
        var inside = terrain.HeightAt(32.7999f, 50); var outside = terrain.HeightAt(32.8001f, 50);
        Require(Math.Abs(inside - outside) < 0.01, "Apron edge reset to PadY and made a vertical seam.");
    }

    [TestCase]
    public void SoftWaterIncludesAllPartsAndLeavesIslandHolesDry()
    {
        var island = new PolygonRings(Box(-20, -20, 20, 20).Outer, [Box(-5, -5, 5, 5).Outer]);
        var water = new WorldFeature(new("fixture:water"), FeatureSource.Design, new("poi"), new MultiPolygonGeometry([island, Box(40, -10, 60, 10)]))
        {
            Parts = [new("a", new PolygonGeometry(island), null, null) { Props = Kind("water") },
                new("b", new PolygonGeometry(Box(40, -10, 60, 10)), null, null) { Props = Kind("water") }]
        };
        var lake = new TerrainLakeOp("design:terrain:lake", "fixture", water.Id, -0.5, new(TerrainBankKind.Soft, "1:5"), new(1.5), []) { Zone = Zone };
        var terrain = Compile([lake], [water]);
        Equal(-0.5, terrain.WaterY(water.Id)!.Value); Require(terrain.WaterAt(0, 0) is null && terrain.WaterAt(50, 0) is not null, "Water union lost holes or a real part.");
        Equal(-0.5, terrain.HeightAt(-20, 0)); Equal(-0.5875, terrain.HeightAt(-19.75f, 0));
        Require(terrain.HeightAt(0, 0) > -0.5, "Island became submerged water bed.");
    }

    [TestCase]
    public void MissingZonesAreVisibleAndNamedUnsupportedStepsFail()
    {
        var mound = new TerrainMoundOp("design:terrain:mound", "fixture", new TerrainCircle(Vector2.Zero, 12), 1) { Zone = Zone };
        var terrain = TerrainCompiler.Compile(Context([]) with { Zones = null, Defaults = null }, [Source([mound])]);
        Require(!terrain.ZonesValidated && terrain.Diagnostics.Any(s => s.StartsWith("NOT_VERIFIED", StringComparison.Ordinal)), "Missing boundaries passed validation.");
        var water = Feature("fixture:water", "water", Box(-20, -20, 20, 20));
        var steps = new TerrainLakeOp("design:terrain:steps", "fixture", water.Id, -0.5, new(TerrainBankKind.Steps, "1:6"), new(1.5), []) { Zone = Zone };
        Reject(() => Compile([steps], [water]), "real stair nosing profile");
    }

    [TestCase]
    public void IndexedBoundaryRetainsMillimetreContainmentAndExactEdgeDistance()
    {
        var rings = new PolygonRings([new(-20, -20), new(-5, -20), new(-5, -10), new(5, -10), new(5, -20),
            new(20, -20), new(20, 20), new(5, 20), new(5, 10), new(-5, 10), new(-5, 20), new(-20, 20)], [Box(-4, -4, 4, 4).Outer]);
        var indexed = new TerrainRegion([rings]);
        rings = indexed.Polygons[0]; // Both kernels use the same canonical hole winding and source-edge tie order.
        for (var z = -23.25; z <= 23; z += 1.5)
            for (var x = -23.25; x <= 23; x += 1.5)
            {
                var reference = TerrainGeometry.PolygonDistance(x, z, rings); var actual = indexed.Distance(x, z);
                Equal(reference.Height, actual.Height); Equal(reference.Dx, actual.Dx); Equal(reference.Dz, actual.Dz);
                Require(indexed.Contains(x, z) == TerrainMath.Contains(x, z, rings), "Edge BVH changed exact containment.");
            }
    }

    [TestCase]
    public void ProjectedSlantedBankHasZeroTangentialDistanceDerivative()
    {
        var region = new TerrainRegion([new PolygonRings([new(-436.73f, 40.12f), new(-432.11f, 58.69f),
            new(-449.31f, 61.93f), new(-453.93f, 43.36f)], [])]);
        for (var i = 1; i < 100; i++)
        {
            var hit = region.NearestBoundary(-431.8, 41 + i * 0.17);
            var atBank = region.Distance(hit.X, hit.Z);
            Equal(0, atBank.Height);
            Equal(0, atBank.Dx * hit.Tx + atBank.Dz * hit.Tz);
            if (hit.Tx != 0 || hit.Tz != 0) Equal(1, atBank.Dx * atBank.Dx + atBank.Dz * atBank.Dz);
        }
    }

    [TestCase]
    public void SignedDistanceKeepsRealRoadGeometryAcrossAMillimetreContainmentSwitch()
    {
        // Full float32 source polygon of way 572770118. Keep the public millimetre
        // containment result while distance uses the actual side of its source edge.
        var region = new TerrainRegion([new PolygonRings([
            new(106.33f, 268.17f), new(111.45f, 271.39f), new(110.12f, 273.5f), new(104.54f, 269.99f),
            new(102.25f, 266.52f), new(100.98f, 260.21f), new(99.6f, 258.74f), new(97.02f, 257.4f),
            new(95.5f, 257.13f), new(92.33f, 257.69f), new(86.45f, 259.89f), new(82.57f, 259.91f),
            new(80.57f, 257.62f), new(79.96f, 254.82f), new(80.7f, 250.17f), new(81.01f, 245.77f),
            new(79.51f, 241.44f), new(76.48f, 237.13f), new(69.24f, 234.11f), new(63.03f, 229.46f),
            new(55.85f, 229.08f), new(53.38f, 229.84f), new(46.44f, 230.71f), new(46.14f, 234.09f),
            new(44.67f, 236.39f), new(42.57f, 235.04f), new(43.7f, 233.26f), new(44.13f, 228.49f),
            new(52.85f, 227.39f), new(55.53f, 226.56f), new(63.92f, 227.01f), new(70.49f, 231.92f),
            new(78.11f, 235.1f), new(81.76f, 240.29f), new(83.54f, 245.44f), new(83.19f, 250.45f),
            new(82.5f, 254.75f), new(82.88f, 256.46f), new(83.7f, 257.41f), new(85.99f, 257.4f),
            new(91.67f, 255.27f), new(95.49f, 254.59f), new(97.83f, 255), new(101.13f, 256.72f),
            new(103.3f, 259.02f), new(104.61f, 265.55f)])]);
        const double ax = 46.01933059234094, az = 234.27950000792043;
        const double bx = 46.01933059241544, bz = 234.27949999311863;
        Require(!region.Contains(ax, az) && region.Contains(bx, bz), "Public millimetre topology changed.");
        var a = region.Distance(ax, az); var b = region.Distance(bx, bz);
        Require(a.Height > 0 && b.Height > 0, "A millimetre rounding threshold still changes the real distance sign.");
        var separation = Math.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
        Require(Math.Abs(b.Height - a.Height) <= separation + 0.01, "Signed distance is not 1-Lipschitz across the old jump.");
    }

    [TestCase]
    public void GeometricDistanceHandlesHolesConcaveVerticesAndExactBoundaryHits()
    {
        var region = new TerrainRegion([new PolygonRings([new(-8, -8), new(8, -8), new(8, 0),
            new(0, 0), new(0, 8), new(-8, 8)], [Box(-6, -6, -2, -2).Outer])]);
        var ring = new TerrainRingIndex(region.Polygons[0].Outer);
        Require(ring.ContainsGeometry(-4, 0) && !ring.ContainsGeometry(9, 0), "A ray through polygon vertices changed real containment.");
        Equal(2, region.Distance(-4, -4).Height); Equal(-Math.Sqrt(2), region.Distance(-1, -1).Height);
        foreach (var point in new[] { new Vector2(8, -4), Vector2.Zero, new Vector2(-2, -4), new Vector2(-2, -2) })
            Require(BitConverter.DoubleToInt64Bits(region.Distance(point.X, point.Y).Height) == 0,
                "Exact edge/vertex distance is not canonical positive zero.");
        foreach (var epsilon in new[] { 0.05, 0.5 })
        {
            Require(Math.Abs(region.Distance(8 + epsilon, -4).Height - epsilon) < 0.01
                && Math.Abs(region.Distance(8 - epsilon, -4).Height + epsilon) < 0.01,
                "Outer edge distance does not tend to zero from its actual two sides.");
            Require(Math.Abs(region.Distance(-2 - epsilon, -4).Height - epsilon) < 0.01
                && Math.Abs(region.Distance(-2 + epsilon, -4).Height + epsilon) < 0.01,
                "Clockwise hole distance has the wrong real sign.");
            Require(Math.Abs(region.Distance(-epsilon, -epsilon).Height + Math.Sqrt(2) * epsilon) < 0.01
                && Math.Abs(region.Distance(epsilon, epsilon).Height - epsilon) < 0.01,
                "The nearest concave vertex has a discontinuous distance sign.");
        }
        var rect = new MapRect(new(-64), new(128));
        var field = new TerrainHeightField(rect, (x, z) => new(region.Distance(x, z) * 0.01, TerrainClass.Slope), [rect]);
        var left = new short[TerrainSampling.SampleCount(0)]; var right = new short[left.Length]; var repeated = new short[left.Length];
        field.FillTile(new(-1, 0), 0, left); field.FillTile(new(0, 0), 0, right); field.FillTile(new(0, 0), 0, repeated);
        Require(right.SequenceEqual(repeated), "Repeated geometric-distance tile samples differ.");
        for (var z = 0; z < TerrainSampling.TileSamples; z++)
            Require(left[z * 65 + 64] == right[z * 65], "Adjacent tiles disagree at the same geometric-distance world point.");
    }

    [TestCase]
    public void DrySwaleStaysDryAndOnlyARealNamedCrossingIsAccepted()
    {
        var road = new NetworkLine(new("fixture:road"), new([new(0, -30), new(0, 30)]), 4)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([]) with { Network = new([road], [], [], [], []) };
        var swale = new TerrainChannelOp("design:terrain:swale", "synthetic crossing", null, 0.4,
            new(TerrainBankKind.Swale) { Inner = "1:4" }, null, [], new([new(-20, 0), new(20, 0)]), 1,
            Crossings: [new TerrainUnderpassCrossing(road.Id, Vector2.Zero)]) { Zone = Zone, Dry = true };
        var terrain = TerrainCompiler.Compile(context, [Source([swale])]);
        Equal(-0.4, terrain.HeightAt(-10, 0)); Equal(0, terrain.HeightAt(0, 0));
        Require(terrain.WaterY(new(swale.Id)) is null && terrain.WaterAt(-10, 0) is null, "Dry swale fabricated a water entity.");
        var invalid = swale with { Crossings = [new TerrainUnderpassCrossing(road.Id, new(0.9f, 0.9f))] };
        Reject(() => TerrainCompiler.Compile(context, [Source([invalid])]), "real centreline intersection within 1 m");
        var ending = swale with { Crossings = [new TerrainEndBeforeCrossing(road.Id)] };
        var terminal = TerrainCompiler.Compile(context, [Source([ending])]);
        var control = TerrainCompiler.Compile(context, []);
        Equal(control.HeightAt(-3, 0), terminal.HeightAt(-3, 0));
        Equal(control.HeightAt(-2.99f, 0), terminal.HeightAt(-2.99f, 0));
    }

    [TestCase]
    public void ZeroBandAndAuthoritativeZoneViolationsFailWithoutChangingAuthorData()
    {
        var mound = new TerrainMoundOp("design:terrain:edge-mound", "negative fixture", new TerrainCircle(new(40, 0), 12), 1) { Zone = Zone };
        Reject(() => TerrainCompiler.Compile(Context([]) with { Boundary = Box(-50, -50, 50, 50) }, [Source([mound])]), "zero band");
        var platform = new TerrainPlatformOp("design:terrain:zone-platform", "negative fixture", Box(-4, -4, 4, 4), 2,
            new("1:4", "1:4"), 2) { Zone = Zone };
        var context = Context([]) with { Zones = new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [Zone] = [Box(-6, -6, 6, 6)] } };
        Reject(() => TerrainCompiler.Compile(context, [Source([platform])]), "leaves authoritative zone");
        Equal(2, platform.Y);
    }

    [TestCase]
    public void ArealHighwayIsNotAContourMasqueradingAsACorridor()
    {
        var road = new NetworkLine(new("fixture:road"), new([new(-10, -10), new(10, -10), new(10, 10), new(-10, 10), new(-10, -10)]), 5)
            { Props = JsonSerializer.SerializeToElement(new { highway = "pedestrian", area = "yes" }) };
        var context = Context([]) with { Network = new([road], [], [], [], []) };
        Require(TerrainCompiler.Compile(context, []).IsFlat(default), "Areal paving generated an automatic perimeter road.");
        var corridor = new TerrainCorridorOp("design:terrain:invalid-road", "negative fixture", road.Id, [new(0, 0), new(80, 0)], 0, 0.75) { Zone = Zone };
        Reject(() => TerrainCompiler.Compile(context, [Source([corridor])]), "polygon perimeter is not a centreline");
    }

    [TestCase]
    public void WetChannelResolvesARealWaterSegmentAndTerraceUsesItsDeclaredAxis()
    {
        var water = Feature("fixture:water", "water", Box(-20, -3, 20, 3));
        var channel = new TerrainChannelOp("design:terrain:wet-channel", "synthetic real-id fixture", water.Id, 1.2,
            new(TerrainBankKind.Hard, "1:4"), -1.2, [], new([new(-20, 0), new(20, 0)]), 6) { Zone = Zone };
        var terrain = Compile([channel], [water]); Equal(-1.2, terrain.WaterY(water.Id)!.Value); Equal(-2.25, terrain.HeightAt(0, 0));
        Equal(0, terrain.HeightAt(0, 3));
        var wading = channel with { Wade = [Box(-2, -1, 2, 1)] };
        var wadingSource = Source([wading]);
        try
        {
            TerrainCompiler.Compile(Context([water]), [wadingSource]);
            throw new InvalidOperationException("Channel Wade was silently ignored.");
        }
        catch (WorldDataException error)
        {
            Require(error.File == wadingSource.File && error.Entry == wading.Id
                && error.Message.Contains("Unsupported channel Wade", StringComparison.Ordinal), error.Message);
        }
        Reject(() => Compile([channel]), "Missing water entity");
        var terrace = new TerrainTerraceOp("design:terrain:terrace", "synthetic terrace", new([new(-10, 0), new(10, 0)]),
            new(0, 1), 0, 2, 0.5, 2, []) { Zone = Zone };
        Equal(0.4, Compile([terrace]).HeightAt(0, 2));
        var basin = new TerrainBasinOp("design:terrain:basin", "synthetic basin", new TerrainCircle(Vector2.Zero, 12), 1) { Zone = Zone };
        Equal(-1, Compile([basin]).HeightAt(0, 0));
    }

    [TestCase]
    public void SourceRiverAreaUsesHardShoreWithoutInventingItsMissingCentreline()
    {
        var polygon = Box(35, -10, 45, 10);
        var river = Feature("fixture:river-area", "water", polygon) with
        {
            Parts = [new("osm:way:1", new PolygonGeometry(polygon), null, null)
                { Props = JsonSerializer.SerializeToElement(new { kind = "water", sourceTags = new { water = "river" } }) }]
        };
        var context = Context([river]) with { Boundary = Box(-50, -50, 50, 50) };
        var terrain = TerrainCompiler.Compile(context, []);
        Equal(-1.2, terrain.WaterY(river.Id)!.Value); Equal(0, terrain.HeightAt(46, 0));
        Require(terrain.HeightAt(40, 0) < -1.2, "Actual water=river was treated as a soft lake.");
        var explicitChannel = new TerrainChannelOp("design:terrain:missing-line", "negative fixture", river.Id, 1.2,
            new(TerrainBankKind.Hard, "1:4"), -1.2, []) { Zone = Zone };
        Reject(() => TerrainCompiler.Compile(context, [Source([explicitChannel])]), "explicit line/width or one unambiguous real waterway source");
    }

    [TestCase]
    public void AutomaticWaterUsesActualConstantShoreTopAndRequiresExplicitLevelForUnevenShore()
    {
        var water = Feature("fixture:raised-lake", "water", Box(-20, -20, 20, 20));
        var plateau = new TerrainPlatformOp("design:terrain:raised-lake-base", "fixture", Box(-40, -40, 40, 40), 3,
            new("1:4", "1:4"), 2) { Zone = Zone };
        var terrain = Compile([plateau], [water]); Equal(2.5, terrain.WaterY(water.Id)!.Value);
        var uneven = new TerrainSlopeOp("design:terrain:uneven-lake-base", "negative fixture", new PolygonGeometry(Box(-40, -40, 40, 40)),
            0, 1, 0, new("1:4", "1:4"), 2, new(-40, 0), new(40, 0)) { Zone = Zone };
        Reject(() => Compile([uneven], [water]), "authored constant waterY is required");
    }

    [TestCase]
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
            foreach (var epsilon in new[] { 0.0001, 0.000001, 0.00000001, 0.0000000001 })
            {
                var a = Top(x - tx * epsilon, z - tz * epsilon);
                var c = Top(x + tx * epsilon, z + tz * epsilon);
                Require(Math.Abs(c - a) < 0.01,
                    $"Source-end height jumps at epsilon={epsilon:R}: {a:R}->{c:R}, reversed={reversed}.");
            }
            var actualA = Top(43.68212918421214, 238.0730637711107);
            var actualB = Top(43.6821291989299, 238.0730637714087);
            Require(Math.Abs(actualB - actualA) < 0.01, "The measured real-shore source-end jump remains.");
            // The source core and its actual 0.75 m shoulder keep planar crossfall,
            // including the part of the shoulder past the source-end plane.
            foreach (var point in new[] { new Vector2(43.6f, 235.8f), new Vector2(44, 235.8f) })
            {
                var contact = TerrainCompiler.CorridorContactAt(road, 0.75, point.X, point.Y);
                Require(contact.Distance.Height <= 0, "Fixture must be inside the fixed source core/shoulder.");
                var expected = path.Project(point.X, point.Y);
                var actual = TerrainCompiler.CorridorOffsetAt(road, contact, point.X, point.Y);
                Require(Math.Abs(actual.Height - expected.Offset) < 0.01,
                    "Exterior continuity repair changed fixed core/shoulder crossfall.");
            }

            double Top(double px, double pz)
            {
                var contact = TerrainCompiler.CorridorContactAt(road, 0, px, pz);
                var along = profile.SurfaceAt(path, px, pz, road.Width, path.Project(px, pz));
                return along.Height + 0.015 * TerrainCompiler.CorridorOffsetAt(road, contact, px, pz).Height;
            }
        }
    }

    [TestCase]
    public void RectangularBridgeApproachKeepsItsActualEndAndShoulder()
    {
        var road = new TerrainRoad(new("fixture:approach"), new("fixture:source:way:1"),
            new(new([new(-20, 0), new(0, 0)])), 2, "footway", new([Box(-20, -1, 0, 1)]));
        var end = TerrainCompiler.CorridorContactAt(road, 0.75, 2, 0);
        Equal(1.25, end.Distance.Height); Equal(0.75, end.X); Equal(0, end.Z);
        var side = TerrainCompiler.CorridorContactAt(road, 0.75, -10, 3);
        Equal(1.25, side.Distance.Height); Equal(-10, side.X); Equal(1.75, side.Z);
        var corner = TerrainCompiler.CorridorContactAt(road, 0.75, 2, 2);
        Equal(Math.Sqrt(5) - 0.75, corner.Distance.Height);
        var fallback = TerrainCompiler.CorridorContactAt(road with { Footprint = null }, 0.75, 2, 0);
        Equal(0.25, fallback.Distance.Height);
        Require(end.Distance.Height > fallback.Distance.Height, "Real rectangular port was inflated into a line capsule.");
    }

    [TestCase]
    public void AutomaticRoadWaterWeightHasExactBoundariesAndNeverReappearsUnderWater()
    {
        var waterEdge = TerrainCompiler.WaterSideWeight(new(0, 1, 0), new(4, -1, 0));
        var roadEdge = TerrainCompiler.WaterSideWeight(new(4, 1, 0), new(0, -1, 0));
        Equal(0, waterEdge.Height); Equal(0, waterEdge.Dx); Equal(1, roadEdge.Height); Equal(0, roadEdge.Dx);
        Equal(0, TerrainCompiler.WaterSideWeight(new(-2, 1, 0), new(6, -1, 0)).Height);
        var midpoint = TerrainCompiler.WaterSideWeight(new(2, 1, 0), new(2, -1, 0));
        Equal(0.5, midpoint.Height); Equal(0.375, midpoint.Dx);
        try { TerrainCompiler.WaterSideWeight(new(0, 1, 0), new(0, -1, 0)); throw new InvalidOperationException("Coincident fixed boundaries were silently accepted."); }
        catch (ArgumentException) { }
    }

    [TestCase]
    public void BridgeBanksFollowActualShoreArclengthAndHaveTwoMetreContinuousEnds()
    {
        var region = new TerrainRegion([Box(-10, -10, 10, 10)]);
        var bridge = new NetworkLine(new("fixture:bridge"), new([new(-20, 0), new(20, 0)]), 2);
        var banks = new TerrainBridgeBanks(region, [bridge]);
        Require(banks.Contacts.Count == 2, "Bridge/shore crossings were lost or invented.");
        Equal(4, banks.NearestContact(-10, 4).AlongDistance);
        Equal(1, banks.HardWeight(-10, 5).Height); Equal(0, banks.HardWeight(-10, 5).Dz);
        Equal(0.5, banks.HardWeight(-10, 6).Height); Equal(-0.75, banks.HardWeight(-10, 6).Dz);
        Equal(0, banks.HardWeight(-10, 7).Height); Equal(0, banks.HardWeight(-10, 7).Dz);
        var unrelated = new NetworkLine(new("fixture:nearby"), new([new(-11, -20), new(-11, 20)]), 2);
        Require(new TerrainBridgeBanks(region, [unrelated]).Contacts.Count == 0, "Nearby nonintersecting bridge was guessed into a bank segment.");
        var water = Feature("fixture:lake", "water", region.Polygons[0]);
        var context = Context([water]) with { Network = new([], [], [bridge], [], []) };
        var terrain = TerrainCompiler.Compile(context, []);
        Equal(-0.5, terrain.WaterY(water.Id)!.Value); Equal(0, terrain.HeightAt(-10, 4));
        Equal(-0.25, terrain.HeightAt(-10, 6)); Equal(-0.5, terrain.HeightAt(-10, 7));
    }

    [TestCase]
    public void CompiledBankQueryExportsFrozenGeometryAndRejectsInvalidStations()
    {
        var water = Feature("fixture:bank-water", "water", Box(-10, -10, 10, 10));
        var bridge = new NetworkLine(new("fixture:bank-bridge"), new([new(-20, 0), new(20, 0)]), 2);
        var context = Context([water]) with { Network = new([], [], [bridge], [], []) };
        ITerrainBankSource source = TerrainCompiler.Compile(context, []);
        Require(source.Banks.Count == 2, "Resolved bridge banks were not exported once per actual crossing.");
        Require(source.Banks.Select(span => span.Id).SequenceEqual(source.Banks.Select(span => span.Id).OrderBy(id => id, StringComparer.Ordinal)), "Bank IDs are not in stable ordinal order.");
        var first = source.Banks[0];
        Require(first.Waters.SequenceEqual(new[] { water.Id }) && first.Bridges.SequenceEqual(new[] { bridge.Id }) && first.Roads.Count == 0,
            "Exported provenance does not identify the actual water/bridge.");
        Equal(2, first.TransitionLength); Equal(0, source.BankTopY(first.Id, first.CoreFrom));
        var retained = source.BankTopY(first.Id, (first.CoreFrom + first.CoreTo) / 2);
        var plateau = new TerrainPlatformOp("design:terrain:bank-plateau", "new snapshot", Box(-40, -40, 40, 40), 3, new("1:4", "1:4"), 2) { Zone = Zone };
        ITerrainBankSource changed = TerrainCompiler.Compile(context, [Source([plateau])]);
        Equal(3, changed.BankTopY(changed.Banks[0].Id, changed.Banks[0].CoreFrom));
        Equal(retained, source.BankTopY(first.Id, (first.CoreFrom + first.CoreTo) / 2));
        foreach (var station in new[] { double.NaN, double.PositiveInfinity, -0.01, first.Length + 0.01 })
        {
            try { source.BankTopY(first.Id, station); throw new InvalidOperationException("Invalid station was silently clamped."); }
            catch (ArgumentOutOfRangeException) { }
        }
        try { source.BankTopY(" missing ", 0); throw new InvalidOperationException("Malformed ID accepted."); } catch (ArgumentException) { }
        try { source.BankTopY("missing", 0); throw new InvalidOperationException("Unknown ID substituted terrain height."); } catch (KeyNotFoundException) { }
        var hard = new TerrainLakeOp("design:terrain:whole-bank", "whole ring", water.Id, -0.5, new(TerrainBankKind.Hard, "1:4"), new(1.5), []) { Zone = Zone };
        ITerrainBankSource closed = Compile([hard], [water]); var ring = closed.Banks.Single();
        Require(ring.Shore.Points[0] == ring.Shore.Points[^1] && ring.TransitionLength == 0 && ring.CoreTo == ring.Length, "Whole Hard ring lost its final station contract.");
        Equal(closed.BankTopY(ring.Id, 0), closed.BankTopY(ring.Id, ring.Length));
    }

    [TestCase]
    public void FootwayConflictIsOneContinuousHardSpanWithThreeMetreExtensions()
    {
        var water = new TerrainRegion([Box(-10, -10, 10, 10)]);
        var road = new TerrainRegion([Box(-12, -3, -11, 3)]);
        var banks = new TerrainBridgeBanks(water, []);
        banks.ResolveFootways([new(new("fixture:shore-path"), TerrainRegion.Expand(road.Bounds, 5),
            road.Polygons[0].Outer, (x, z) => (road.Distance(x, z), TerrainDifferential.Constant(0)))],
            (_, _) => -0.5);
        Require(banks.RoadSpans.Count == 1, "One geometric conflict was split into sample-sized walls.");
        var span = banks.RoadSpans[0];
        var expectedEnd = 3 + Math.Sqrt(1.5 * 1.5 - 1) + 3;
        Require(Math.Abs(span.To - span.From - 2 * expectedEnd) < 0.01, "The continuous conflict root or 3 m extension changed.");
        Equal(1, banks.HardWeight(-10, 0).Height);
        Require(Math.Abs(banks.HardWeight(-10, expectedEnd + 1).Height - 0.5) < 0.01, "Hard/Soft transition is not the fixed 2 m Hermite join.");
        Equal(0, banks.HardWeight(-10, expectedEnd + 2.01).Height);
        Equal(0, banks.ProfileAt(-10, 0).Top!.Value.Height);
        Require(span.Roads.Count == 1 && span.Roads[0].Road == new FeatureId("fixture:shore-path"), "Resolved bank lost its actual road identity.");
    }

    [TestCase]
    public void RegisteredUndergroundCulvertPreservesSurfaceAndLogicalWaterIdentity()
    {
        var shape = new PolygonGeometry(Box(-10, -2, 10, 2));
        var water = new WorldFeature(new("fixture:source:way:44"), FeatureSource.Osm, new("water"), shape)
        {
            Parts = [new("osm:way:44", shape, null, null) { Props = Kind("water") }]
        };
        var line = new NetworkLine(water.Id, new([new(-10, 0), new(10, 0)]), 4)
            { Props = JsonSerializer.SerializeToElement(new { waterway = "drain", tunnel = "culvert", layer = "-1" }) };
        var context = Context([water]) with { Network = new([], [line], [], [line], []) };
        var terrain = TerrainCompiler.Compile(context, []);
        Equal(0, terrain.HeightAt(0, 0)); Equal(-1.2, terrain.WaterY(water.Id)!.Value);
        Require(terrain.WaterAt(0, 0) is null && !terrain.IsSurfaceWater(water.Id), "Buried water was exposed as surface water.");
        var plateau = new TerrainPlatformOp("design:terrain:over-pipe", "raised land preserves buried metadata",
            Box(-40, -40, 40, 40), 3, new("1:4", "1:4"), 2) { Zone = Zone };
        var raised = TerrainCompiler.Compile(context, [Source([plateau])]);
        Equal(3, raised.HeightAt(0, 0)); Equal(-1.2, raised.WaterY(water.Id)!.Value);
        var lake = Feature("fixture:pipe-lake", "water", Box(-15, -5, -5, 5));
        var connected = TerrainCompiler.Compile(Context([water, lake]) with { Network = context.Network }, []);
        Equal(-0.5, connected.WaterY(water.Id)!.Value); Equal(0, connected.HeightAt(0, 0));
        var drainShape = new PolygonGeometry(Box(8, -2, 30, 2));
        var drain = new WorldFeature(new("fixture:source:way:45"), FeatureSource.Osm, new("water"), drainShape)
            { Parts = [new("osm:way:45", drainShape, null, null) { Props = Kind("water") }] };
        var drainLine = new NetworkLine(drain.Id, new([new(10, 0), new(30, 0)]), 4)
            { Props = JsonSerializer.SerializeToElement(new { waterway = "drain" }) };
        var through = TerrainCompiler.Compile(Context([water, lake, drain]) with { Network = new([], [line, drainLine], [], [line], []) }, []);
        Equal(-0.5, through.WaterY(drain.Id)!.Value); Equal(-0.5, through.WaterY(water.Id)!.Value);
        Equal(0, through.HeightAt(0, 0));
        Reject(() => TerrainCompiler.Compile(context with { Network = new([], [line], [], [], []) }, []), "WorldNetwork.Culverts registration");
    }

    [TestCase]
    public void AutomaticRoadFollowsSoftBankAndPreservesItsWaterBoundary()
    {
        var water = Feature("fixture:lake", "water", Box(0, -20, 20, 20));
        CompiledTerrain Build(float center, float width)
        {
            var roadGeometry = new PolygonGeometry(Box(center - width / 2, -15, center + width / 2, 15));
            var road = new WorldFeature(new("fixture:road"), FeatureSource.Osm, new("road"), roadGeometry)
                { Parts = [new("osm:way:8", roadGeometry, null, null) { Props = Kind("road") }] };
            var line = new NetworkLine(new("fixture:source:way:8"), new([new(center, -15), new(center, 15)]), width)
                { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
            return TerrainCompiler.Compile(Context([water, road]) with { Network = new([line], [], [], [], []) }, []);
        }
        var feasible = Build(-4, 2); Equal(-0.5, feasible.HeightAt(0, 0)); Equal(0, feasible.HeightAt(-4, 0));
        Require(feasible.FactsAt(-0.5f, 0).Grade <= 1f / 3 + 0.01, "Narrowed side exceeded its true transverse grade.");
        // Default base samples are now soft terrain-following targets, not two hidden Y=0
        // anchors. This narrow path can follow the dry soft bank without moving its waterline.
        var narrow = Build(-0.2f, 0.1f);
        Equal(-0.5, narrow.HeightAt(0, 0)); Equal(-0.46, narrow.HeightAt(-0.2f, 0));
    }

    [TestCase]
    public void WaterUnionRemovesInternalEdgesAndPreservesRemainingHoles()
    {
        var union = TerrainUnion.Build([new([Box(-2, -10, 1, 10)]), new([Box(-1, -10, 2, 10)])]);
        Equal(-2, union.Distance(0, 0).Height);
        var touching = TerrainUnion.Build([new([Box(-2, -2, 0, 2)]), new([Box(0, -2, 2, 2)])]);
        Equal(-2, touching.Distance(0, 0).Height);
        var island = new TerrainRegion([new PolygonRings(Box(-10, -10, 10, 10).Outer, [Box(-3, -3, 3, 3).Outer])]);
        var filled = TerrainUnion.Build([island, new([Box(-1, -5, 1, 5)])]);
        Require(filled.Contains(0, 0) && !filled.Contains(2, 0) && filled.Polygons.Sum(p => p.Holes.Count) == 2,
            "Water union filled an unrelated island hole or retained an internal boundary.");
        var a = new TerrainRegion([Box(-2.00037f, -10, 1, 10)]);
        var c = new TerrainRegion([Box(-1, -10, 2.00037f, 10)]);
        var fractional = TerrainUnion.Build([a, c]);
        Equal(-2, fractional.Distance(0, 0).Height);
        var expected = Nodes(fractional);
        Require(expected.SequenceEqual(Nodes(TerrainUnion.Build([a, c]))) && expected.SequenceEqual(Nodes(TerrainUnion.Build([c, a]))),
            "Centimetre-grid union geometry changes between repeated builds or input order.");
        static IEnumerable<Vector2> Nodes(TerrainRegion region) => region.Polygons
            .SelectMany(p => new[] { p.Outer }.Concat(p.Holes)).SelectMany(ring => ring);
    }

    [TestCase]
    public void ShoreEnvelopeKeepsFixedBoundaryValuesAndCapsTheFullLandGradient()
    {
        var envelope = new TerrainShoreEnvelope(new([Box(-1, -1, 1, 1)]), (x, _) => 0.2 * x, "fixture", "shore-envelope");
        Equal(-0.2, envelope.Constrain(TerrainDifferential.Constant(0), -1, 0).Height);
        Equal(0.2, envelope.Constrain(TerrainDifferential.Constant(0), 1, 0).Height);
        var sample = envelope.Constrain(TerrainDifferential.Constant(0), -1.2, 0.2);
        Equal(-0.1, sample.Height); Equal(0.5, Math.Sqrt(sample.Dx * sample.Dx + sample.Dz * sample.Dz));
        const double e = 0.00001;
        var finite = (envelope.Constrain(TerrainDifferential.Constant(0), -1.2 + e, 0.2).Height
            - envelope.Constrain(TerrainDifferential.Constant(0), -1.2 - e, 0.2).Height) / (2 * e);
        Equal(finite, sample.Dx);
    }

    [TestCase]
    public void WaterJoinKeepsTwoMetreShoreArclengthWhileInteriorWidthChanges()
    {
        var lake = new TerrainRegion([Box(-10, -10, 10, 10)]);
        var channel = new TerrainRegion([Box(-4, -25, 4, 0)]);
        var join = new TerrainWaterJoin(TerrainUnion.Build([lake, channel]), lake);
        foreach (var width in new[] { 2d, 6d, 12d })
        {
            Equal(1, join.Weight(-4, -10, width).Height);
            Equal(0.5, join.Weight(-4, -11, width).Height);
            Equal(0, join.Weight(-4, -12, width).Height);
            Equal(0, join.Weight(-4, -13, width).Height);
            var a = join.Weight(-4.00001, -11, width); var c = join.Weight(-3.99999, -11, width);
            Require(Math.Abs(a.Height - c.Height) < 0.01, "The fixed exposed shore and interior join are discontinuous.");
        }
        Equal(1, join.Weight(0, 0, 4).Height);
        Equal(0, join.Weight(0, -22, 4).Height);
    }

    [TestCase]
    public void ConnectedNaturalStreamUsesLakeLevelWithoutRenamingOrChangingItsDepth()
    {
        var lake = Feature("fixture:lake", "water", Box(-10, -10, 10, 10));
        var shape = new PolygonGeometry(Box(-4, -25, 4, 0));
        var stream = new WorldFeature(new("fixture:stream"), FeatureSource.Osm, new("water"), shape)
            { Parts = [new("osm:way:9", shape, null, null) { Props = Kind("water") }] };
        var line = new NetworkLine(new("fixture:source:way:9"), new([new(0, -25), new(0, 0)]), 8)
            { Props = JsonSerializer.SerializeToElement(new { waterway = "stream" }) };
        var terrain = TerrainCompiler.Compile(Context([lake, stream]) with { Network = new([], [line], [], [], []) }, []);
        Equal(-0.5, terrain.WaterY(lake.Id)!.Value); Equal(-0.5, terrain.WaterY(stream.Id)!.Value);
        Require(terrain.HeightAt(0, -10) < -0.5, "Internal lake/stream boundary became a fake shore.");
        Equal(-1.7, terrain.HeightAt(0, -18));
        Equal(Compile([], [lake]).HeightAt(0, 0), terrain.HeightAt(0, 0));
        var canal = line with { Props = JsonSerializer.SerializeToElement(new { waterway = "canal" }) };
        var fixedCanal = new TerrainChannelOp("design:terrain:fixed-canal", "explicit incompatible level", stream.Id,
            1.2, new(TerrainBankKind.Hard, "1:4"), -1.2, []) { Zone = Zone };
        Reject(() => TerrainCompiler.Compile(Context([lake, stream]) with { Network = new([], [canal], [], [], []) }, [Source([fixedCanal])]),
            "incompatible authored/lake levels");
    }

    [TestCase]
    public void WaterUnionCannotHideAuthoredZoneExtentBehindAnAutomaticMember()
    {
        var automatic = Feature("fixture:a-auto-lake", "water", Box(-15, -8, 1, 8));
        var authored = Feature("fixture:z-authored-lake", "water", Box(-1, -8, 15, 8));
        var op = new TerrainLakeOp("design:terrain:authored-water", "zone regression", authored.Id, -0.5,
            new(TerrainBankKind.Soft, "1:5"), new(1.5), []) { Zone = Zone };
        var context = Context([automatic, authored]) with
        {
            Zones = new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [Zone] = [Box(-7, -20, 7, 20)] }
        };
        Reject(() => TerrainCompiler.Compile(context, [Source([op])]), "leaves authoritative zone");
        var accepted = Compile([op], [automatic, authored]);
        Require(accepted.ZonesValidated, "A valid author extent lost its zone validation after union.");
    }

    [TestCase]
    public void FreeboardRaisesBankTopWithoutRaisingSoftWaterlineOrLoweringWater()
    {
        var water = Feature("fixture:raised-water", "water", Box(-10, -10, 10, 10));
        var lake = new TerrainLakeOp("design:terrain:raised-water", "freeboard fixture", water.Id, 1,
            new(TerrainBankKind.Soft, "1:5"), new(1.5), []) { Zone = Zone };
        var terrain = Compile([lake], [water]);
        Equal(1, terrain.WaterY(water.Id)!.Value); Equal(1, terrain.HeightAt(10, 0));
        Require(terrain.HeightAt(12.5f, 0) >= 1.3f - 0.01f, "Landward bank top lacks the required freeboard.");
        Require(Math.Abs(terrain.HeightAt(10.0001f, 0) - terrain.HeightAt(9.9999f, 0)) < 0.01,
            "Freeboard introduced a jump at the soft waterline.");
    }

    [TestCase]
    public void SlopeBudgetedQuadraticJoinPreservesEndpointsAndParameterContinuity()
    {
        foreach (var peak in new[] { 1.1, 1.2, 1.5 })
        {
            Equal(0, TerrainCompiler.LimitedEase(0, peak).Value); Equal(0, TerrainCompiler.LimitedEase(0, peak).Derivative);
            Equal(1, TerrainCompiler.LimitedEase(1, peak).Value); Equal(0, TerrainCompiler.LimitedEase(1, peak).Derivative);
            Equal(0.5, TerrainCompiler.LimitedEase(0.5, peak).Value);
            for (var i = 0; i <= 100; i++) Require(TerrainCompiler.LimitedEase(i / 100d, peak).Derivative <= peak + 0.01,
                "Join exceeded its analytically allocated derivative budget.");
        }
        Require(Math.Abs(TerrainCompiler.LimitedEase(0.25, 1.5).Value - TerrainCompiler.LimitedEase(0.25, 1.5 - 0.00000001).Value) < 0.01,
            "Switching interpolation families at the peak cap created a height jump.");
    }

    [TestCase]
    public void SameSpanRoadContributorsStayContinuousAcrossTheirEqualDistanceLine()
    {
        var first = BankContributor("fixture:bank-a", (_, z) => (new(2 + z * 0.05, 0, 0.05), TerrainDifferential.Constant(1)));
        var second = BankContributor("fixture:bank-b", (_, z) => (new(2 - z * 0.05, 0, -0.05), TerrainDifferential.Constant(2)));
        var banks = BanksForContributors(first, second);
        const double epsilon = 0.0001;
        var left = banks.ProfileAt(-10, -epsilon).Top!.Value;
        var right = banks.ProfileAt(-10, epsilon).Top!.Value;
        var center = banks.ProfileAt(-10, 0).Top!.Value;
        Require(Math.Abs(right.Height - left.Height) < 0.01, "The same-span contributor bisector still has a height jump.");
        var finiteDifference = (right.Height - left.Height) / (2 * epsilon);
        Require(Math.Abs(center.Dz - finiteDifference) < 0.01,
            "Reported bank derivative omits the changing contributor weights.");
        Require(Math.Abs(center.Dz) > 0.02, "Constant source tops lost the nonzero distance-weight derivative.");
        Equal(0, center.Dx);
    }

    [TestCase]
    public void SameSpanRoadContributorInputOrderDoesNotChangeItsSurface()
    {
        var first = BankContributor("fixture:bank-a", (_, z) => (new(2 + z * 0.05, 0, 0.05), new(1 + z * 0.02, 0, 0.02)));
        var second = BankContributor("fixture:bank-b", (_, z) => (new(2 - z * 0.05, 0, -0.05), new(2 - z * 0.03, 0, -0.03)));
        var forward = BanksForContributors(first, second); var reverse = BanksForContributors(second, first);
        foreach (var z in new[] { -0.2, 0, 0.2 })
        {
            var a = forward.ProfileAt(-10, z); var b = reverse.ProfileAt(-10, z);
            Require(a == b, "Reversing source roads changed the composed bank value or derivatives.");
        }
    }

    [TestCase]
    public void AZeroDistanceRoadCoreKeepsItsExactTopAndDerivative()
    {
        var expected = new TerrainDifferential(0.75, 0, 0.125);
        foreach (var distance in new[] { 0d, -0.25 })
        {
            var core = BankContributor("fixture:bank-a", (_, _) => (TerrainDifferential.Constant(distance), expected));
            var other = BankContributor("fixture:bank-b", (_, _) => (TerrainDifferential.Constant(2), TerrainDifferential.Constant(2)));
            var actual = BanksForContributors(other, core).ProfileAt(-10, 0).Top!.Value;
            Require(actual == expected, "A zero/negative-distance fixed core was averaged into another road.");
        }
    }

    [TestCase]
    public void CoincidentRoadCoresWithDifferentHeightsAreRejected()
    {
        var first = BankContributor("fixture:bank-a", (_, _) => (TerrainDifferential.Constant(0), TerrainDifferential.Constant(0.75)));
        var second = BankContributor("fixture:bank-b", (_, _) => (TerrainDifferential.Constant(0), TerrainDifferential.Constant(0.7502)));
        var banks = BanksForContributors(second, first);
        Reject(() => banks.ProfileAt(-10, 0), "Coincident road cores have incompatible bank tops");
    }

    [TestCase]
    public void NonFiniteRoadBankContactsAreRejectedBeforeComposition()
    {
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var invalidDistance = BankContributor("fixture:bad-distance", (_, _) => (new(value, 0, 0), TerrainDifferential.Constant(0)));
            var invalidDerivative = BankContributor("fixture:bad-distance-derivative", (_, _) => (new(1, value, 0), TerrainDifferential.Constant(0)));
            var invalidTop = BankContributor("fixture:bad-top", (_, _) => (TerrainDifferential.Constant(1), new(0, 0, value)));
            Reject(() => BanksForContributors(invalidDistance), "Road bank contact is non-finite");
            Reject(() => BanksForContributors(invalidDerivative), "Road bank contact is non-finite");
            Reject(() => BanksForContributors(invalidTop), "Road bank contact is non-finite");
        }
    }

    private static TerrainBridgeBanks.RoadContact BankContributor(string id,
        Func<double, double, (TerrainDifferential Distance, TerrainDifferential Top)> evaluate) =>
        new(new(id), new(new(-12), new(12)), [], (x, z) => evaluate(x, z));

    private static TerrainBridgeBanks BanksForContributors(params TerrainBridgeBanks.RoadContact[] contributors)
    {
        var banks = new TerrainBridgeBanks(new([Box(-10, -10, 10, 10)]), []);
        banks.ResolveFootways(contributors, (_, _) => -100);
        Require(banks.RoadSpans.Count == 1 && banks.RoadSpans[0].Roads.Count == contributors.Length,
            "Synthetic crossing contributors did not share one Hard span.");
        return banks;
    }

    [TestCase]
    public void FreeboardNetworkBoundsKeepFrozenSoftTargetsSharedNodesAndInputOrder()
    {
        var a = new TerrainRoad(new("fixture:floor-a"), new("fixture:floor-a"), new(new([new(-20, 0), new(0, 0)])), 2.5, "footway", null);
        var c = new TerrainRoad(new("fixture:floor-c"), new("fixture:floor-c"), new(new([new(0, 0), new(20, 0)])), 2.5, "footway", null);
        TerrainRoadProfile.Request[] requests = [new(a, [], 1d / 12), new(c, [], 1d / 12)];
        var evaluations = 0;
        var baseline = new TerrainProgram([new("base", "fixture", Zone, TerrainGroup.Regional, 0, new(new(-30), new(30)),
            (previous, _, _) => { evaluations++; return previous with { Surface = TerrainDifferential.Constant(-0.5) }; })]);
        var fit = new TerrainRoadProfile.NetworkFit(requests, baseline, [], (_, _) => false);
        fit.Solve(); var sampledOnce = evaluations;
        foreach (var sample in fit.Samples) fit.Require(sample.Source, sample.Index, new(-0.18, "fixture:water"));
        var forward = fit.Solve(); fit.Solve();
        Require(evaluations == sampledOnce, "A freeboard re-solve sampled a changed soft field instead of retaining its original objective.");
        var reverseFit = new TerrainRoadProfile.NetworkFit(requests.Reverse().ToArray(), baseline, [], (_, _) => false);
        foreach (var sample in reverseFit.Samples.Reverse()) reverseFit.Require(sample.Source, sample.Index, new(-0.18, "fixture:water"));
        var reverse = reverseFit.Solve();
        Equal(-0.18, forward[a.Source].At(a.Path.Length).Y); Equal(-0.18, forward[c.Source].At(0).Y);
        Require(forward[a.Source].At(20).Y == forward[c.Source].At(0).Y, "Shared freeboard node acquired two values.");
        foreach (var road in new[] { a, c })
            for (var station = 0d; station <= road.Path.Length; station += 0.025)
            {
                Require(Math.Abs(forward[road.Source].At(station).Y - reverse[road.Source].At(station).Y) < 0.000000000001,
                    "Input order changed the constrained profile.");
                Require(Math.Abs(forward[road.Source].At(station).Grade) <= 0.031, "Freeboard changed the junction grade ceiling.");
            }
        Require(!fit.Require(a.Source, 0, new(-0.2, "fixture:lower-water")), "A lower candidate weakened a cumulative node bound.");
    }

    [TestCase]
    public void AutomaticRoundingAndSurfaceAtStayAboveTheContinuousSupportFloor()
    {
        var road = new TerrainRoad(new("fixture:rounded-floor"), new("fixture:rounded-floor"),
            new(new([new(0, 15), new(0, 0), new(-15, 0)])), 2.5, "footway", null);
        var baseline = new TerrainProgram([new("base", "fixture", Zone, TerrainGroup.Regional, 0, new(new(-30), new(30)),
            (previous, x, z) => previous with { Surface = new(-0.6 + 0.01 * x + 0.02 * z, 0.01, 0.02) })]);
        var fit = new TerrainRoadProfile.NetworkFit([new(road, [], 1d / 12)], baseline, [], (_, _) => false);
        var initial = fit.Solve()[road.Source]; const double floor = -0.18;
        var support = TerrainCompiler.AutomaticFreeboardSupport(road, initial, (-1d, 0.875d), (-1d, 1.125d));
        Require(support.Count >= 3 && support.Count < fit.Samples.Count, "The existing bend support was missed or expanded to the entire road.");
        foreach (var index in support) fit.Require(road.Source, index, new(floor, "fixture:water"));
        var constrained = fit.Solve()[road.Source];
        for (var part = 0; part <= 1000; part++)
        {
            var z = 0.875 + 0.25 * part / 1000;
            var value = constrained.SurfaceAt(road.Path, -1, z, road.Width, road.Path.Project(-1, z));
            Require(value.Height >= floor - 0.01, "Continuous SurfaceAt fell below its constrained contributing samples.");
        }
        foreach (var sample in fit.Samples) fit.Require(sample.Source, sample.Index, new(floor, "fixture:water"));
        constrained = fit.Solve()[road.Source];
        for (var station = 0d; station <= road.Path.Length; station += 0.005)
            Require(constrained.At(station).Y >= floor - 0.01, "Existing three-node automatic rounding undershot its sample lower bound.");
    }

    [TestCase]
    public void ConservativeFreeboardNeighbourIsUnsupportedWithoutMovingTheAuthor()
    {
        var automatic = new TerrainRoad(new("fixture:conservative-auto"), new("fixture:conservative-auto"),
            new(new([new(0, 0), new(20, 0)])), 2.5, "footway", null);
        var authored = new TerrainRoad(new("fixture:conservative-author"), new("fixture:conservative-author"),
            new(new([new(0, 0), new(0, -20)])), 2.5, "footway", null);
        var authorProfile = new TerrainRoadProfile([new(0, -0.25), new(20, -0.25)], 20, [], 1d / 12, "owner.terrain.json", "design:terrain:good-author");
        var fit = new TerrainRoadProfile.NetworkFit([new(automatic, [], 1d / 12)], new TerrainProgram([]), [(authored, authorProfile)], (_, _) => false);
        fit.Solve();
        try { fit.Require(automatic.Source, 0, new(-0.18, "fixture:distant-water")); throw new InvalidOperationException("Conservative neighbour was silently moved."); }
        catch (WorldDataException error)
        {
            Require((string?)error.Data["TerrainConstraintKind"] == "unsupported" && error.Message.Contains("owner.terrain.json", StringComparison.Ordinal)
                && error.Message.Contains("not a demonstrated authored-content conflict", StringComparison.Ordinal), "A conservative neighbour was incorrectly blamed on author data.");
        }
        Equal(-0.25, authorProfile.At(0).Y); Equal(-0.25, fit.Profiles[automatic.Source].At(0).Y);
    }

    [TestCase]
    public void AuthoredAnchorAtRealHardShoreHasAnExactContentConflict()
    {
        var water = Feature("fixture:anchor-water", "water", Box(0, -10, 20, 10));
        var line = new NetworkLine(new("fixture:anchor-road"), new([new(0, -15), new(0, 0), new(0, 15)]), 0.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var corridor = new TerrainCorridorOp("design:terrain:bad-shore-anchor", "real fixed anchor conflict", line.Id,
            [new(0, -0.3), new(15, -0.3), new(30, -0.3)], 0, 0.75) { Zone = Zone };
        try
        {
            _ = TerrainCompiler.Compile(Context([water]) with { Network = new([line], [], [], [], []) }, [Source([corridor])]);
            throw new InvalidOperationException("A real constrained hard anchor was accepted.");
        }
        catch (WorldDataException error)
        {
            Require((string?)error.Data["TerrainConstraintKind"] == "content-conflict" && error.Entry == corridor.Id
                && error.Message.Contains("world=(0,0)", StringComparison.Ordinal), "The exact shoreline anchor conflict lost its classification/location.");
        }
    }

    [TestCase]
    public void AutomaticLowSoftRoadClosesRealHardBankFreeboardWithoutMovingWaterOrXZ()
    {
        var water = Feature("fixture:low-soft-water", "water", Box(0, -10, 20, 10));
        var points = new Vector2[] { new(-0.5f, -20), new(-0.5f, 20) };
        var line = new NetworkLine(new("fixture:low-soft-road"), new(points), 0.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([water]) with { Network = new([line], [], [], [], []) };
        var terrain = TerrainCompiler.Compile(context, []);
        Equal(-0.5, terrain.WaterY(water.Id)!.Value);
        Require(line.Line.Points.SequenceEqual(points), "Freeboard solving changed the real road XZ.");
        Require(terrain.Banks.Count > 0, "The actual road/soft-bank conflict was not resolved as Hard geometry.");
        var waterY = terrain.WaterY(water.Id)!.Value; var requiredTop = waterY + 0.3;
        foreach (var span in terrain.Banks)
        {
            // The exported shore includes Soft joins; freeboard applies to the Hard core.
            Require(terrain.BankTopY(span.Id, span.CoreFrom) >= requiredTop - 0.01
                && terrain.BankTopY(span.Id, span.CoreTo) >= requiredTop - 0.01,
                "A final Hard core endpoint retained the low soft target.");
            for (var station = span.CoreFrom; station <= span.CoreTo; station += 0.025)
                Require(terrain.BankTopY(span.Id, station) >= requiredTop - 0.01,
                    "Final exported continuous Hard bank retained the low soft target.");
            if (span.TransitionLength == 0) continue;
            // This fixture has no overlapping banks: both outer join ends are the Soft
            // shoreline water level, while the inner ends meet the Hard top continuously.
            Equal(waterY, terrain.BankTopY(span.Id, 0)); Equal(waterY, terrain.BankTopY(span.Id, span.Length));
            const double epsilon = 0.0001;
            Require(Math.Abs(terrain.BankTopY(span.Id, epsilon) - terrain.BankTopY(span.Id, 0)) < 0.01
                && Math.Abs(terrain.BankTopY(span.Id, span.Length - epsilon) - terrain.BankTopY(span.Id, span.Length)) < 0.01,
                "A Soft transition outer endpoint does not meet its fixed waterline continuously.");
            foreach (var join in new[] { span.CoreFrom, span.CoreTo })
                Require(Math.Abs(terrain.BankTopY(span.Id, join - epsilon) - terrain.BankTopY(span.Id, join + epsilon)) < 0.01,
                    "A Soft transition inner endpoint does not meet its Hard core continuously.");
        }
        Require(terrain.Diagnostics.Any(value => value.StartsWith("Road freeboard initial own deficit:", StringComparison.Ordinal))
            && terrain.Diagnostics.Any(value => value.StartsWith("Road freeboard sample final:", StringComparison.Ordinal))
            && terrain.Diagnostics.Any(value => value.Contains("influenceBBox=", StringComparison.Ordinal))
            && terrain.Diagnostics.Any(value => value.Contains("closure complete", StringComparison.Ordinal)), "Reviewable closure diagnostics are incomplete.");
    }

    [TestCase]
    public void FootwayPlanningDoesNotPolluteTheSoftBaseAndCommitsOnlyTheLastRealPlan()
    {
        var banks = new TerrainBridgeBanks(new([Box(-10, -10, 10, 10)]), []);
        var road = BankContributor("fixture:plan-road", (_, _) => (TerrainDifferential.Constant(1), TerrainDifferential.Constant(0)));
        var initial = banks.PlanFootways([road], (_, _) => -0.5);
        Require(initial.Count == 1 && banks.RoadSpans.Count == 0 && banks.ProfileAt(-10, 0).Top is null,
            "Pure planning mutated the WaterNode-owned bank state.");
        var final = banks.PlanFootways([road], (_, _) => 0);
        Require(final.Count == 0 && banks.RoadSpans.Count == 0, "A disappearing real conflict survived as cumulative geometry.");
        banks.CommitFootways(final);
        Require(banks.RoadSpans.Count == 0, "The initial stale Hard geometry was committed.");
        try { banks.CommitFootways(initial); throw new InvalidOperationException("Banks were mutable after snapshot finalization."); }
        catch (InvalidOperationException error) { Require(error.Message.Contains("committed once", StringComparison.Ordinal), error.Message); }
    }

    [TestCase]
    public void CoreCoverageBoundIncludesAnInteriorFarthestPoint()
    {
        // The boundary follows a square loop. Every footprint vertex is on the source path,
        // while the real core interior reaches five metres from it.
        var road = new TerrainRoad(new("fixture:wide-core"), new("fixture:wide-core"),
            new(new([new(-5, -5), new(5, -5), new(5, 5), new(-5, 5), new(-5, -5)])), 2.5, "footway", new([Box(-5, -5, 5, 5)]));
        var bound = TerrainCompiler.RoadCorePathRadius(road);
        Require(bound >= 5 && bound < 5.4, "Core radius was inferred from vertices or introduced an unrelated art margin.");
    }

    private static CompiledTerrain Compile(IReadOnlyList<TerrainOp> operations, IReadOnlyList<WorldFeature>? features = null) =>
        TerrainCompiler.Compile(Context(features ?? []), [Source(operations)]);
    private static TerrainSource Source(IReadOnlyList<TerrainOp> operations) => new("overlay/terrain/fixture.terrain.json", new(Zone, new("WP-08"), operations));
    private static TerrainCompileContext Context(IReadOnlyList<WorldFeature> features) => new(new("fixture"), new(new(-128), new(128)),
        Box(-512, -512, 512, 512), features.ToDictionary(f => f.Id), WorldNetwork.Empty, "fixture", Defaults,
        new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [Zone] = [Box(-512, -512, 512, 512)] });
    private static PolygonRings Box(float x0, float z0, float x1, float z1) => new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);
    private static WorldFeature Feature(string id, string kind, PolygonRings rings) => new(new(id), FeatureSource.Design, new(kind), new PolygonGeometry(rings))
        { Height = kind == "building" ? 8 : null, Bounds = WorldModelLoader.BoundsOf(new PolygonGeometry(rings), kind == "building" ? 8 : 0) };
    private static JsonElement Kind(string kind) => JsonSerializer.SerializeToElement(new { kind });
    private static void Equal(double expected, double actual) => Require(Math.Abs(expected - actual) <= 0.001, $"Expected {expected:R}, got {actual:R}.");
    private static void Reject(Action action, string message)
    { try { action(); throw new InvalidOperationException("Expected rejection: " + message); } catch (WorldDataException e) { Require(e.Message.Contains(message, StringComparison.Ordinal), e.Message); } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
