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
public sealed class TerrainSourceInputsTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public void TerrainParseFailureUsesTheOriginalJsonPathAndException()
    {
        const string file = "world/rejected.terrain.json";
        foreach (var text in new[] { """{"zone":"fixture","ops":42}""", """{"zone":"fixture","ops":[""" })
        {
            WorldDataException? failure = null;
            try { WorldTerrainInputs.ReadSources(new("fixture"), new(StringComparer.Ordinal) { [file] = text }); }
            catch (WorldDataException error) { failure = error; }
            AssertThat(failure?.InnerException is JsonException original && failure.File == file &&
                failure.Entry == (original.Path ?? "root"))
                .OverrideFailureMessage("Terrain parsing must retain the original JSON exception and use its path without reparsing.").IsTrue();
        }
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
}
