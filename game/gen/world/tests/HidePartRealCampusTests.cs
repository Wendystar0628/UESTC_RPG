using System.Text.Json;
using Campus.Contracts;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

/// <summary>Read-only real-data regression candidate. Uses campus.json plus these operations only; no terrain-stage inputs.</summary>
[TestSuite]
public sealed class HidePartRealCampusTests
{
    [TestCase]
    public void ApprovedSourceRemovalsKeepParentIdentitiesAndIndependentResearchWings()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json")))
            directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Real campus fixture was not found.");
        var campusPath = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json");
        var files = new Dictionary<string, string>(StringComparer.Ordinal) { ["campus.json"] = File.ReadAllText(campusPath) };
        var before = WorldModelLoader.Load(new("qingshuihe"), files).World;
        var one = new FeatureId("qsh:way:691679140");
        var four = new FeatureId("qsh:relation:11665640");
        var wingA = new FeatureId("qsh:relation:11334255");
        var wingB = new FeatureId("qsh:relation:11665641");
        files["overlay/hide-part-review.d/0000-fixture.json"] = JsonSerializer.Serialize(new OverlayFile(new("review"), new("WP-14"),
        [
            new HidePartOp(one, "osm:way:691679138", "approved source-only removal fixture"),
            new HidePartOp(four, "osm:relation:11334254", "approved duplicate source removal fixture"),
            new HidePartOp(four, "osm:relation:11665642", "approved duplicate source removal fixture")
        ]), ContractJson.Options);
        var after = WorldModelLoader.Load(new("qingshuihe"), files).World;
        AssertThat(after.Features.Count == before.Features.Count).OverrideFailureMessage("Source-only removal deleted an entity identity.").IsTrue();
        AssertThat(after.Features.Values.Sum(feature => feature.Parts.Count) == before.Features.Values.Sum(feature => feature.Parts.Count) - 3).OverrideFailureMessage("Unexpected source parts were removed.").IsTrue();
        var remainingOne = after.Features[one];
        AssertThat(remainingOne.Parts.Single().SourceId == "osm:way:691679140" && remainingOne.Height == 3.9f && remainingOne.Storeys == 1).OverrideFailureMessage("Building 1 did not retain its one-storey source part.").IsTrue();
        var remainingFour = after.Features[four];
        AssertThat(remainingFour.Parts.Select(part => part.SourceId).SequenceEqual(
            new[] { "osm:relation:11665640", "osm:way:828726368", "osm:way:828726369" }, StringComparer.Ordinal)).OverrideFailureMessage("Research Building 4 lost or retained the wrong source pieces.").IsTrue();
        AssertThat(remainingFour.Height == 7.8f && remainingFour.Storeys == 2).OverrideFailureMessage("Research Building 4 retained the removed pieces' height.").IsTrue();
        foreach (var id in new[] { one, four })
        {
            var old = before.Features[id]; var current = after.Features[id];
            AssertThat(current.Name == old.Name && current.Zone == old.Zone && current.Family == old.Family &&
                current.Facade == old.Facade && current.Props?.GetRawText() == old.Props?.GetRawText()).OverrideFailureMessage("Parent properties changed.").IsTrue();
            foreach (var part in current.Parts)
                AssertThat(Canonical(part) == Canonical(old.Parts.Single(previous => previous.SourceId == part.SourceId))).OverrideFailureMessage("A surviving real source part changed.").IsTrue();
        }
        foreach (var id in new[] { wingA, wingB })
            AssertThat(Canonical(after.Features[id]) == Canonical(before.Features[id])).OverrideFailureMessage("An independent A/B entity or one of its parts changed.").IsTrue();
    }

    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
}
