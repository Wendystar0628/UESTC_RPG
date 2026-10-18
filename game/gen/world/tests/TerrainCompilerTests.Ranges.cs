using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

public sealed partial class TerrainCompilerTests
{
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
