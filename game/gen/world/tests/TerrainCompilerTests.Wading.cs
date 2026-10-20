using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

public sealed partial class TerrainCompilerTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void SoftWaterIncludesAllPartsAndLeavesIslandHolesDry()
    {
        var island = new PolygonRings(Box(-20, -20, 20, 20).Outer, [Box(-5, -5, 5, 5).Outer]);
        var water = new WorldFeature(new("fixture:water"), new("poi"), new MultiPolygonGeometry([island, Box(40, -10, 60, 10)]))
        {
            Parts = [new("a", new PolygonGeometry(island), null, null) { Props = Kind("water") },
                new("b", new PolygonGeometry(Box(40, -10, 60, 10)), null, null) { Props = Kind("water") }]
        };
        var lake = new TerrainLakeOp("design:terrain:lake", "fixture", water.Id, -0.5, new(TerrainBankKind.Soft, "1:5"), new(1.5), []) { Zone = Zone };
        var terrain = Compile([lake], [water]);
        AssertThat(terrain.Waters.Single().Polygons.Count == water.Parts.Count
            && terrain.Waters.Single().Polygons[0].Holes.Count == island.Holes.Count).IsTrue();
        AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.WaterY(water.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(terrain.WaterAt(0, 0) is null && terrain.WaterAt(50, 0) is not null).OverrideFailureMessage("Water union lost holes or a real part.").IsTrue();
        AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.HeightAt(-20, 0))) <= TestTolerance.Position).IsTrue();
        var nearShore = terrain.HeightAt(-19.75f, 0);
        AssertThat(nearShore < terrain.WaterY(water.Id)!.Value && terrain.WaterAt(-19.75f, 0) is not null
            && Math.Abs(nearShore - RenderedHeight(terrain, -19.75f, 0)) <= TestTolerance.Position)
            .OverrideFailureMessage("The first wet grid cell lost its submerged bed or disagrees with rendering.").IsTrue();
        AssertThat(terrain.HeightAt(0, 0) > -0.5).OverrideFailureMessage("Island became submerged water bed.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void MixedWaterKeepsEveryChannelsLocalWadeAndRejectsIslandCoverage()
    {
        var lakeWater = WorldFeatureFixture.Create("fixture:a-lake", "water", new PolygonGeometry(Box(-10, -10, 10, 10)));
        var leftWater = WorldFeatureFixture.Create("fixture:b-channel", "water", new PolygonGeometry(Box(-4, -25, 4, 0)));
        var rightWater = WorldFeatureFixture.Create("fixture:c-channel", "water", new PolygonGeometry(Box(-4, 0, 4, 25)));
        var lake = new TerrainLakeOp("design:terrain:mixed-lake", "mixed local registrations", lakeWater.Id, -0.5,
            new(TerrainBankKind.Soft, "1:5"), new(1.5), []) { Zone = Zone };
        var left = new TerrainChannelOp("design:terrain:left-channel", "mixed local registrations", leftWater.Id, 1.2,
            new(TerrainBankKind.Soft, "1:5"), lake.WaterY, [], new([new(0, -25), new(0, 0)]), 8)
            { Zone = Zone, Wade = [Box(-2, -20, 2, -15), Box(-2, -5, 2, -3)] };
        var right = left with { Id = "design:terrain:right-channel", Water = rightWater.Id,
            Line = new([new(0, 0), new(0, 25)]), Wade = [Box(-2, 15, 2, 20)] };
        var terrain = Compile([lake, left, right], [lakeWater, leftWater, rightWater]);
        var withoutRight = Compile([lake, left, right with { Wade = [] }], [lakeWater, leftWater, rightWater]);
        AssertThat(terrain.WaterAt(0, -18) is { Wade: true } && terrain.WaterAt(0, 18) is { Wade: true }
            && terrain.WaterAt(0, -4) is { Wade: true } && terrain.WaterAt(0, 0) is { Wade: false }).IsTrue();
        AssertThat(terrain.HeightAt(0, 18) > withoutRight.HeightAt(0, 18)
            && terrain.Waters.Sum(body => body.WadeAreas.Count) == left.Wade.Count + right.Wade.Count).IsTrue();

        var island = new PolygonRings(Box(-10, -10, 10, 10).Outer, [Box(-1, -1, 1, 1).Outer]);
        var waterWithHole = leftWater with { Geometry = new PolygonGeometry(island) };
        var invalid = left with { Line = new([new(-10, 0), new(10, 0)]), Width = 20, Wade = [Box(-3, -2, 3, 2)] };
        WorldDataException? rejection = null;
        try { Compile([invalid], [waterWithHole]); } catch (WorldDataException error) { rejection = error; }
        AssertThat(rejection is not null).OverrideFailureMessage("Wade outer vertices hid coverage of an island hole.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void WetChannelResolvesARealWaterSegmentAndTerraceUsesItsDeclaredAxis()
    {
        var water = WorldFeatureFixture.Create("fixture:water", "water", new PolygonGeometry(Box(-20, -3, 20, 3)));
        var channel = new TerrainChannelOp("design:terrain:wet-channel", "synthetic real-id fixture", water.Id, 1.2,
            new(TerrainBankKind.Hard, "1:4"), -1.2, [], new([new(-20, 0), new(20, 0)]), 6) { Zone = Zone };
        var terrain = Compile([channel], [water]); AssertThat(Math.Abs((double)(-1.2) - (double)(terrain.WaterY(water.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-2.25) - (double)(terrain.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(0, 3))) <= TestTolerance.Position).IsTrue();
        var wading = channel with { Wade = [Box(-2, -1, 2, 1)] };
        var shallow = Compile([wading], [water]);
        AssertThat(shallow.HeightAt(0, 0) > terrain.HeightAt(0, 0)
            && shallow.WaterAt(0, 0) is { Wade: true } && shallow.WaterAt(10, 0) is { Wade: false }).IsTrue();
        AssertThat(shallow.Waters.Single().WadeAreas.Count == wading.Wade.Count
            && shallow.Waters.Single().Water == water.Id).IsTrue();
        {
            WorldDataException? rejection = null;
            try { Compile([channel]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var terrace = new TerrainTerraceOp("design:terrain:terrace", "synthetic terrace", new([new(-10, 0), new(10, 0)]),
            new(0, 1), 0, 2, 0.5, 2, []) { Zone = Zone };
        AssertThat(Math.Abs((double)(0.4) - (double)(Compile([terrace]).HeightAt(0, 2))) <= TestTolerance.Position).IsTrue();
        var basin = new TerrainBasinOp("design:terrain:basin", "synthetic basin", new TerrainCircle(Vector2.Zero, 12), 1) { Zone = Zone };
        AssertThat(Math.Abs((double)(-1) - (double)(Compile([basin]).HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RegisteredUndergroundCulvertPreservesSurfaceAndLogicalWaterIdentity()
    {
        var shape = new PolygonGeometry(Box(-10, -2, 10, 2));
        var water = new WorldFeature(new("fixture:source:way:44"), new("water"), shape)
        {
            Parts = [new("osm:way:44", shape, null, null) { Props = Kind("water") }]
        };
        var line = new NetworkLine(water.Id, new([new(-10, 0), new(10, 0)]), 4)
            { Props = JsonSerializer.SerializeToElement(new { waterway = "drain", tunnel = "culvert", layer = "-1" }) };
        var context = Context([water]) with { Network = new([], [line], [], [line], []) };
        AssertThrown(() => TerrainCompiler.Compile(context, [])).IsInstanceOf<WorldDataException>();
        var lake = WorldFeatureFixture.Create("fixture:pipe-lake", "water", new PolygonGeometry(Box(-15, -5, -5, 5)));
        var connected = TerrainCompiler.Compile(Context([water, lake]) with { Network = context.Network }, []);
        AssertThat(Math.Abs((double)(-0.5) - (double)(connected.WaterY(water.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(connected.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
        var drainShape = new PolygonGeometry(Box(8, -2, 30, 2));
        var drain = new WorldFeature(new("fixture:source:way:45"), new("water"), drainShape)
            { Parts = [new("osm:way:45", drainShape, null, null) { Props = Kind("water") }] };
        var drainLine = new NetworkLine(drain.Id, new([new(10, 0), new(30, 0)]), 4)
            { Props = JsonSerializer.SerializeToElement(new { waterway = "drain" }) };
        var through = TerrainCompiler.Compile(Context([water, lake, drain]) with { Network = new([], [line, drainLine], [], [line], []) }, []);
        AssertThat(Math.Abs((double)(-0.5) - (double)(through.WaterY(drain.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-0.5) - (double)(through.WaterY(water.Id)!.Value)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(through.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(context with { Network = new([], [line], [], [], []) }, []); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

}
