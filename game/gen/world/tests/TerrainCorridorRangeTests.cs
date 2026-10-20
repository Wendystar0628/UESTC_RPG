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
public sealed class TerrainCorridorRangeTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public void CorridorWaterEdgesShareOneCompositionAndKeepFixedAnchors()
    {
        var water = WorldFeatureFixture.Create("fixture:edge-water", "water", new PolygonGeometry(Box(0, -20, 20, 20)));
        var original = Compile([], [water]);
        foreach (var center in new[] { -1f, -0.5f })
        {
            var geometry = new PolygonGeometry(Box(center - 1, -15, center + 1, 15));
            var road = new WorldFeature(new("fixture:edge-road"), new("road"), geometry)
                { Parts = [new("osm:way:77", geometry, null, null) { Props = Kind("road") }] };
            var line = new NetworkLine(new("fixture:source:way:77"), new([new(center, -15), new(center, 15)]), 2)
                { Props = JsonSerializer.SerializeToElement(new { highway = "service" }) };
            var context = Context([water, road]) with { Network = new([line], [], [], [], []), DataRect = new(new(-30), new(30)) };
            var roadY = original.HeightAt(center, 0);
            var corridor = new TerrainCorridorOp("design:terrain:water-road", "shared water-edge composition", line.Id,
                [new(0, roadY), new(30, roadY)], 0, Defaults.CorridorShoulder) { Zone = Zone };
            TerrainOp[][] cases =
            [
                [], [corridor], [corridor with { Range = new(0, 30) }],
                [corridor with { Id = "design:terrain:water-left", Range = new(0, 10), Anchors = [new(0, roadY), new(10, roadY)] },
                 corridor with { Id = "design:terrain:water-right", Range = new(20, 30), Anchors = [new(20, roadY), new(30, roadY)] }]
            ];
            foreach (var operations in cases)
                foreach (var terrain in new[] { TerrainCompiler.Compile(context, [Source(operations)]), TerrainCompiler.CompileAndValidate(context, [Source(operations)]) })
                {
                    foreach (var x in new[] { 0f, 0.25f, 1f, 10f })
                        AssertThat(Math.Abs(terrain.HeightAt(x, 0) - original.HeightAt(x, 0)) <= TestTolerance.Position)
                            .OverrideFailureMessage("A corridor edge filled the shoreline or registered bed.").IsTrue();
                    foreach (var anchor in operations.OfType<TerrainCorridorOp>().SelectMany(op => op.Anchors))
                    {
                        var z = (float)(line.Line.Points[0].Y + anchor.S);
                        // The flat authored road core keeps its fixed datum at a dry metre node.
                        // Its half-metre centre is sampled from that node and the fixed shore node.
                        AssertThat(Math.Abs(terrain.HeightAt(MathF.Floor(center), z) - anchor.Y) <= TestTolerance.Position)
                            .OverrideFailureMessage("The road core moved its fixed elevation.").IsTrue();
                        AssertThat(Math.Abs(terrain.HeightAt(center, z) - RenderedHeight(terrain, center, z)) <= TestTolerance.Position)
                            .OverrideFailureMessage("The road centre differs from its rendered metre lattice.").IsTrue();
                    }
                    AssertThat(terrain.WaterY(water.Id) == original.WaterY(water.Id) && Math.Abs(terrain.HeightAt(-0.01f, 0) - terrain.HeightAt(0, 0)) <= TestTolerance.Position).IsTrue();
                    AssertThat(terrain.FactsAt(-0.5f, 0).Grade <= 1f / 3 + TestTolerance.Position).IsTrue();
                }
            WorldDataException? fixedConflict = null;
            try { TerrainCompiler.Compile(context, [Source([corridor with { Anchors = [new(0, 0), new(30, 0)] }])]); }
            catch (WorldDataException error) { fixedConflict = error; }
            AssertThat(fixedConflict is not null).OverrideFailureMessage("Water-edge composition silently moved an authored fixed anchor.").IsTrue();
            var crossing = line with { Line = new([new(-5, 0), new(5, 0)]) }; WorldDataException? rejection = null;
            var crossingContext = context with { Network = new([crossing], [], [crossing with { Id = new("fixture:partial-bridge"), Line = new([new(-5, 0), new(2, 0)]) }], [], []) };
            var waterBaseline = TerrainCompiler.Compile(crossingContext with { Network = crossingContext.Network with { Roads = [] } }, []);
            AssertThat(Math.Abs(TerrainCompiler.Compile(crossingContext, []).HeightAt(4, 0) - waterBaseline.HeightAt(4, 0)) <= TestTolerance.Position).IsTrue();
            try { TerrainCompiler.CompileAndValidate(crossingContext, []); }
            catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).OverrideFailureMessage("A source-centreline water crossing acquired an unregistered bridge.").IsTrue();
        }
        var approach = new NetworkLine(new("fixture:bridge-approach"), new([new(19.92f, 0), new(25, 0)]), 2)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var bridge = approach with { Id = new("fixture:bridge-end"), Line = new([new(-5, 0), approach.Line.Points[0]]) };
        var deck = WorldFeatureFixture.Create(bridge.Id.Value, "road", new PolygonGeometry(Box(-5, -1, 19.92f, 1)));
        var joined = TerrainCompiler.Compile(Context([water, deck]) with { Network = new([approach], [], [bridge], [], []) }, []);
        AssertThat(joined.WaterY(water.Id) == original.WaterY(water.Id) && float.IsFinite(joined.HeightAt(20, 0))).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract), TestCategory(TestTag.Determinism)]
    public void CorridorRangesKeepAbsoluteAnchorsAndSolveAutomaticGapsTogether()
    {
        var road = new NetworkLine(new("fixture:range-road"), new([new(-20, 0), new(20, 0)]), 2.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([]) with { Network = new([road], [], [], [], []) };
        var first = new TerrainCorridorOp("design:terrain:first-range", "one source profile", road.Id,
            [new(5, 0.5), new(15, 0.5)], 0, Defaults.CorridorShoulder) { Zone = Zone, Range = new(5, 15) };
        var second = first with { Id = "design:terrain:second-range", Range = new(15, 25), Anchors = [new(15, 0.5), new(25, 0.5)] };
        var terrain = TerrainCompiler.Compile(context, [Source([first, second])]);
        var reversed = TerrainCompiler.Compile(context, [Source([second, first])]);
        foreach (var anchor in first.Anchors.Concat(second.Anchors))
            AssertThat(Math.Abs(terrain.HeightAt((float)(anchor.S - 20), 0) - anchor.Y) <= TestTolerance.Position).IsTrue();
        var a = new short[TerrainSampling.SampleCount(0)]; var c = new short[a.Length];
        terrain.FillTile(default, 0, a); reversed.FillTile(default, 0, c);
        AssertThat(a.SequenceEqual(c)).OverrideFailureMessage("Range input order changed the source-road profile.").IsTrue();
        AssertThat(terrain.HeightAt(-20, 0) < first.Anchors[0].Y
            && terrain.HeightAt(-20, 0) > 0).OverrideFailureMessage("The automatic source gap was omitted or rebased to the authored fragment.").IsTrue();
        var full = first with { Range = new(0, 40), Anchors = [new(0, 0.5), new(40, 0.5)] };
        AssertThat(Math.Abs(TerrainCompiler.Compile(context, [Source([full])]).HeightAt(0, 0) - full.Anchors[0].Y)
            <= TestTolerance.Position).IsTrue();
        WorldDataException? rejection = null;
        try { TerrainCompiler.Compile(context, [Source([first, second with { Range = new(10, 25), Anchors = [new(10, 0.5), new(25, 0.5)] }])]); }
        catch (WorldDataException error) { rejection = error; }
        AssertThat(rejection is not null).IsTrue();
    }
}
