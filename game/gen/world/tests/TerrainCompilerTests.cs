using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Maps;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed partial class TerrainCompilerTests
{

    private static readonly ZoneId Zone = new("fixture");
    private static readonly TerrainAutoSettings Defaults = new(0.2, 1.5, 0.25, 1.2, 0.75);

    [TestCase, TestCategory(TestReason.Contract)]
    public void ZeroBandTaperHasPrescribedValuesAndZeroEndpointSlopes()
    {
        var left = TerrainCompiler.ZeroBandWeight(new(20, 1, 0));
        var right = TerrainCompiler.ZeroBandWeight(new(30, 1, 0));
        var middle = TerrainCompiler.ZeroBandWeight(new(25, 1, 0));
        AssertThat(Math.Abs((double)(0) - (double)(left.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(left.Dx)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1) - (double)(right.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(right.Dx)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0.5) - (double)(middle.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.15) - (double)(middle.Dx)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void RegionalPriorityAndOrdinalIdHaveNoHiddenTypeOrder()
    {
        TerrainOp mound = new TerrainMoundOp("design:terrain:a-mound", "fixture", new TerrainCircle(Vector2.Zero, 20), 1, Priority: 300) { Zone = Zone };
        TerrainOp platform = new TerrainPlatformOp("design:terrain:z-platform", "fixture", Box(-4, -4, 4, 4), 2, new("1:4", "1:4"), 2, 300) { Zone = Zone };
        var first = Compile([mound, platform]); var second = Compile([platform, mound]);
        AssertThat(Math.Abs((double)(2) - (double)(first.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
        var a = new short[4225]; var b = new short[4225]; first.FillTile(default, 0, a); second.FillTile(default, 0, b);
        AssertThat(a.SequenceEqual(b)).OverrideFailureMessage("Input enumeration changed terrain bytes.").IsTrue();
        var later = mound with { Id = "design:terrain:zz-mound" };
        AssertThat(Math.Abs((double)(3) - (double)(Compile([later, platform]).HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PlatformHoleAndTwoPointSlopeKeepTheirGeometry()
    {
        var outer = Box(-30, -30, 30, 30);
        var ring = new PolygonRings(outer.Outer, [Box(-15, -15, 15, 15).Outer]);
        var platform = new TerrainPlatformOp("design:terrain:platform", "fixture", ring, 1, new("1:4", "1:4"), 2) { Zone = Zone };
        var terrain = Compile([platform]); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1) - (double)(terrain.HeightAt(20, 0))) <= TestTolerance.Position).IsTrue();
        var slope = new TerrainSlopeOp("design:terrain:slope", "fixture", new PolygonGeometry(Box(-15, -5, 15, 5)), 0, 1, 0,
            new("1:4", "1:4"), 2, new(-10, 0), new(10, 0)) { Zone = Zone };
        var ramp = Compile([slope]); AssertThat(Math.Abs((double)(0) - (double)(ramp.HeightAt(-12, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.5) - (double)(ramp.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1) - (double)(ramp.HeightAt(12, 0))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0.05) - (double)(ramp.FactsAt(0, 0).Grade)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void MissingZonesAreVisibleAndNamedUnsupportedStepsFail()
    {
        var mound = new TerrainMoundOp("design:terrain:mound", "fixture", new TerrainCircle(Vector2.Zero, 12), 1) { Zone = Zone };
        var terrain = TerrainCompiler.Compile(Context([]) with { Zones = null, Defaults = null }, [Source([mound])]);
        AssertThat(!terrain.ZonesValidated && terrain.Diagnostics.Any(s => s.StartsWith("NOT_VERIFIED", StringComparison.Ordinal))).OverrideFailureMessage("Missing boundaries passed validation.").IsTrue();
        var withZones = TerrainCompiler.Compile(Context([]) with { Defaults = null }, [Source([mound])]);
        AssertThat(!withZones.ZonesValidated && withZones.Diagnostics.Any(s => s.StartsWith("NOT_VERIFIED", StringComparison.Ordinal))).OverrideFailureMessage("Runtime construction claimed offline influence acceptance.").IsTrue();
        var water = WorldFeatureFixture.Create("fixture:water", "water", new PolygonGeometry(Box(-20, -20, 20, 20)));
        var steps = new TerrainLakeOp("design:terrain:steps", "fixture", water.Id, -0.5, new(TerrainBankKind.Steps, "1:6"), new(1.5), []) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { Compile([steps], [water]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
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
                AssertThat(Math.Abs((double)(reference.Height) - (double)(actual.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(reference.Dx) - (double)(actual.Dx)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(reference.Dz) - (double)(actual.Dz)) <= TestTolerance.Position).IsTrue();
                AssertThat(indexed.Contains(x, z) == TerrainMath.Contains(x, z, rings)).OverrideFailureMessage("Edge BVH changed exact containment.").IsTrue();
            }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void GeometricDistanceHandlesHolesConcaveVerticesAndExactBoundaryHits()
    {
        var region = new TerrainRegion([new PolygonRings([new(-8, -8), new(8, -8), new(8, 0),
            new(0, 0), new(0, 8), new(-8, 8)], [Box(-6, -6, -2, -2).Outer])]);
        var ring = new TerrainRingIndex(region.Polygons[0].Outer);
        AssertThat(ring.ContainsGeometry(-4, 0) && !ring.ContainsGeometry(9, 0)).OverrideFailureMessage("A ray through polygon vertices changed real containment.").IsTrue();
        AssertThat(Math.Abs((double)(2) - (double)(region.Distance(-4, -4).Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-Math.Sqrt(2)) - (double)(region.Distance(-1, -1).Height)) <= TestTolerance.Position).IsTrue();
        foreach (var point in new[] { new Vector2(8, -4), Vector2.Zero, new Vector2(-2, -4), new Vector2(-2, -2) })
            AssertThat(BitConverter.DoubleToInt64Bits(region.Distance(point.X, point.Y).Height) == 0).OverrideFailureMessage("Exact edge/vertex distance is not canonical positive zero.").IsTrue();
        foreach (var epsilon in new[] { 0.05, 0.5 })
        {
            AssertThat(Math.Abs(region.Distance(8 + epsilon, -4).Height - epsilon) < TestTolerance.Position
                && Math.Abs(region.Distance(8 - epsilon, -4).Height + epsilon) < TestTolerance.Position).OverrideFailureMessage("Outer edge distance does not tend to zero from its actual two sides.").IsTrue();
            AssertThat(Math.Abs(region.Distance(-2 - epsilon, -4).Height - epsilon) < TestTolerance.Position
                && Math.Abs(region.Distance(-2 + epsilon, -4).Height + epsilon) < TestTolerance.Position).OverrideFailureMessage("Clockwise hole distance has the wrong real sign.").IsTrue();
            AssertThat(Math.Abs(region.Distance(-epsilon, -epsilon).Height + Math.Sqrt(2) * epsilon) < TestTolerance.Position
                && Math.Abs(region.Distance(epsilon, epsilon).Height - epsilon) < TestTolerance.Position).OverrideFailureMessage("The nearest concave vertex has a discontinuous distance sign.").IsTrue();
        }
        var rect = new MapRect(new(-64), new(128));
        var field = new TerrainHeightField(new("fixture", []), rect, (x, z) => new(region.Distance(x, z) * 0.01, TerrainClass.Slope), [rect]);
        var left = new short[TerrainSampling.SampleCount(0)]; var right = new short[left.Length]; var repeated = new short[left.Length];
        field.FillTile(new(-1, 0), 0, left); field.FillTile(new(0, 0), 0, right); field.FillTile(new(0, 0), 0, repeated);
        AssertThat(right.SequenceEqual(repeated)).OverrideFailureMessage("Repeated geometric-distance tile samples differ.").IsTrue();
        for (var z = 0; z < TerrainSampling.TileSamples; z++)
            AssertThat(left[z * 65 + 64] == right[z * 65]).OverrideFailureMessage("Adjacent tiles disagree at the same geometric-distance world point.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ZeroBandAndAuthoritativeZoneViolationsFailWithoutChangingAuthorData()
    {
        var edgePlatform = new TerrainPlatformOp("design:terrain:edge-platform", "fixed core enters protected band",
            Box(38, -4, 42, 4), 1, new("1:4", "1:4"), 2) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.CompileAndValidate(Context([]) with { Boundary = Box(-50, -50, 50, 50) }, [Source([edgePlatform])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var platform = new TerrainPlatformOp("design:terrain:zone-platform", "negative fixture", Box(-4, -4, 4, 4), 2,
            new("1:4", "1:4"), 2) { Zone = Zone };
        var context = Context([]) with { Zones = new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [Zone] = [Box(-6, -6, 6, 6)] } };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.CompileAndValidate(context, [Source([platform])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        AssertThat(Math.Abs((double)(2) - (double)(platform.Y)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void ProtectedZeroBandTrimsComposedEarthworksWhileKeepingFixedCores()
    {
        var building = WorldFeatureFixture.Create("fixture:zero-band-pad", "building", new PolygonGeometry(Box(21, -3, 23, 3)), height: 8);
        var road = new NetworkLine(new("fixture:zero-band-road"), new([new(25, -15), new(25, 15)]), 2.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([building]) with
        {
            Boundary = Box(-50, -50, 50, 50), DataRect = new(new(-50), new(50)), Network = new([road], [], [], [], [])
        };
        var platform = new TerrainPlatformOp("design:terrain:zero-band-platform", "long falloff reaches protected band",
            Box(10, -10, 18, 10), 1, new("1:10", "1:10"), 2) { Zone = Zone };
        var pad = new TerrainPadOp("design:terrain:zero-band-pad", "fixed pad outside protected band", building.Id, 6, PadY: 0.5) { Zone = Zone };
        var corridor = new TerrainCorridorOp("design:terrain:zero-band-road", "road earthworks reach protected band",
            road.Id, [new(0, 0.4), new(30, 0.4)], 0, 0.75) { Zone = Zone };
        var terrain = TerrainCompiler.CompileAndValidate(context, [Source([platform, pad, corridor])]);
        foreach (var x in new[] { 30f, 31f })
            AssertThat(Math.Abs(terrain.HeightAt(x, 0)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs(terrain.HeightAt(12, 0) - platform.Y) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs(terrain.HeightAt(22, 0) - terrain.PadY(building.Id)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs(terrain.PadY(building.Id) - pad.PadY!.Value) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs(terrain.HeightAt(25, 15) - corridor.Anchors[^1].Y) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RectangularBridgeApproachKeepsItsActualEndAndShoulder()
    {
        var road = new TerrainRoad(new("fixture:approach"), new("fixture:source:way:1"),
            new(new([new(-20, 0), new(0, 0)])), 2, "footway", new([Box(-20, -1, 0, 1)]));
        var end = TerrainCompiler.CorridorContactAt(road, 0.75, 2, 0);
        AssertThat(Math.Abs((double)(1.25) - (double)(end.Distance.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.75) - (double)(end.X)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(end.Z)) <= TestTolerance.Position).IsTrue();
        var side = TerrainCompiler.CorridorContactAt(road, 0.75, -10, 3);
        AssertThat(Math.Abs((double)(1.25) - (double)(side.Distance.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-10) - (double)(side.X)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1.75) - (double)(side.Z)) <= TestTolerance.Position).IsTrue();
        var corner = TerrainCompiler.CorridorContactAt(road, 0.75, 2, 2);
        AssertThat(Math.Abs((double)(Math.Sqrt(5) - 0.75) - (double)(corner.Distance.Height)) <= TestTolerance.Position).IsTrue();
        var fallback = TerrainCompiler.CorridorContactAt(road with { Footprint = null }, 0.75, 2, 0);
        AssertThat(Math.Abs((double)(0.25) - (double)(fallback.Distance.Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(end.Distance.Height > fallback.Distance.Height).OverrideFailureMessage("Real rectangular port was inflated into a line capsule.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ConnectedNaturalStreamUsesLakeLevelWithoutRenamingOrChangingItsDepth()
    {
        var lake = WorldFeatureFixture.Create("fixture:lake", "water", new PolygonGeometry(Box(-10, -10, 10, 10)));
        var shape = new PolygonGeometry(Box(-4, -25, 4, 0));
        var stream = new WorldFeature(new("fixture:stream"), new("water"), shape)
            { Parts = [new("osm:way:9", shape, null, null) { Props = Kind("water") }] };
        var line = new NetworkLine(new("fixture:source:way:9"), new([new(0, -25), new(0, 0)]), 8)
            { Props = JsonSerializer.SerializeToElement(new { waterway = "stream" }) };
        var terrain = TerrainCompiler.Compile(Context([lake, stream]) with { Network = new([], [line], [], [], []) }, []);
        AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.WaterY(lake.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.WaterY(stream.Id)!.Value)) <= TestTolerance.Position).IsTrue();
        AssertThat(terrain.HeightAt(0, -10) < -0.5).OverrideFailureMessage("Internal lake/stream boundary became a fake shore.").IsTrue();
        AssertThat(Math.Abs((double)(-1.7) - (double)(terrain.HeightAt(0, -18))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(Compile([], [lake]).HeightAt(0, 0)) - (double)(terrain.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
        var canal = line with { Props = JsonSerializer.SerializeToElement(new { waterway = "canal" }) };
        var fixedCanal = new TerrainChannelOp("design:terrain:fixed-canal", "explicit incompatible level", stream.Id,
            1.2, new(TerrainBankKind.Hard, "1:4"), -1.2, []) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(Context([lake, stream]) with { Network = new([], [canal], [], [], []) }, [Source([fixedCanal])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CoreCoverageBoundIncludesAnInteriorFarthestPoint()
    {
        // The boundary follows a square loop. Every footprint vertex is on the source path,
        // while the real core interior reaches five metres from it.
        var road = new TerrainRoad(new("fixture:wide-core"), new("fixture:wide-core"),
            new(new([new(-5, -5), new(5, -5), new(5, 5), new(-5, 5), new(-5, -5)])), 2.5, "footway", new([Box(-5, -5, 5, 5)]));
        var bound = TerrainCompiler.RoadCorePathRadius(road);
        AssertThat(bound >= 5 && bound < 5.4).OverrideFailureMessage("Core radius was inferred from vertices or introduced an unrelated art margin.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void SourcePreparationKeepsWholePadUpstreamInputsAndIgnoresExplanations()
    {
        var building = new WorldFeature(new("fixture:long-building"), new("building"), new PolygonGeometry(Box(0, 0, 256, 40)))
        { Bounds = new(new(0, 0, 0), new(256, 10, 40)) };
        var context = Context([building]);
        var upstream = new TerrainMoundOp("design:terrain:upstream", "before", new TerrainCircle(new(220, 20), 20), 1) { Zone = Zone };
        var remote = upstream with { Id = "design:terrain:remote", Shape = new TerrainCircle(new(1500, 20), 20) };
        // The source preparer can run directly, before Construct/Compile creates an evaluator or pad median.
        var before = Prepare(upstream, remote);
        var explanation = Prepare(upstream with { Reason = "after" }, remote);
        var changed = Prepare(upstream with { Height = 2 }, remote);
        var independent = Prepare(upstream, remote with { Height = 2 });
        AssertThat(before == explanation && before == independent && before != changed)
            .OverrideFailureMessage("Source stamps lost a remote dependency of the complete pad footprint, or included explanations/independent terrain.").IsTrue();
        var resized = context with { Features = new Dictionary<FeatureId, WorldFeature>
        { [building.Id] = building with { Geometry = new PolygonGeometry(Box(0, 0, 180, 40)), Bounds = new(new(0, 0, 0), new(180, 10, 40)) } } };
        AssertThat(TerrainSourceInputs.Prepare(resized, [Source([upstream, remote])]).Cells.Single(cell => cell.Cell == default).Stamp != before)
            .OverrideFailureMessage("The automatic pad target's source footprint was omitted.").IsTrue();

        string Prepare(TerrainOp local, TerrainOp distant) => TerrainSourceInputs.Prepare(context, [Source([local, distant])])
            .Cells.Single(cell => cell.Cell == default).Stamp;
    }

    private static CompiledTerrain Compile(IReadOnlyList<TerrainOp> operations, IReadOnlyList<WorldFeature>? features = null) =>
        TerrainCompiler.Compile(Context(features ?? []), [Source(operations)]);
    private static TerrainSource Source(IReadOnlyList<TerrainOp> operations) => new("world/fixture.terrain.json", new(Zone, new("WP-08"), operations));
    private static TerrainCompileContext Context(IReadOnlyList<WorldFeature> features) => new(new("fixture"), new(new(-128), new(128)),
        Box(-512, -512, 512, 512), features.ToDictionary(f => f.Id), WorldNetwork.Empty, "fixture", Defaults,
        new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [Zone] = [Box(-512, -512, 512, 512)] });
    private static PolygonRings Box(float x0, float z0, float x1, float z1) => new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);
    private static JsonElement Kind(string kind) => JsonSerializer.SerializeToElement(new { kind });
}
