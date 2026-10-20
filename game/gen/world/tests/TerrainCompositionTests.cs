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
public sealed class TerrainCompositionTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void ZeroBandTaperHasPrescribedValuesAndZeroEndpointSlopes()
    {
        var left = TerrainFieldGrading.ZeroBandWeight(new(20, 1, 0));
        var right = TerrainFieldGrading.ZeroBandWeight(new(30, 1, 0));
        var middle = TerrainFieldGrading.ZeroBandWeight(new(25, 1, 0));
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
}
