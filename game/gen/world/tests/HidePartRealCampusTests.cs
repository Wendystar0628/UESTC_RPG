using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

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
        files["overlay/hide-part-review.overlay.json"] = JsonSerializer.Serialize(new OverlayFile(new("review"), new("WP-14"),
        [
            new HidePartOp(one, "osm:way:691679138", "approved source-only removal fixture"),
            new HidePartOp(four, "osm:relation:11334254", "approved duplicate source removal fixture"),
            new HidePartOp(four, "osm:relation:11665642", "approved duplicate source removal fixture")
        ]), ContractJson.Options);
        var after = WorldModelLoader.Load(new("qingshuihe"), files).World;
        Require(after.Features.Count == before.Features.Count, "Source-only removal deleted an entity identity.");
        Require(after.Features.Values.Sum(feature => feature.Parts.Count) == before.Features.Values.Sum(feature => feature.Parts.Count) - 3,
            "Unexpected source parts were removed.");
        var remainingOne = after.Features[one];
        Require(remainingOne.Parts.Single().SourceId == "osm:way:691679140" && remainingOne.Height == 3.9f && remainingOne.Storeys == 1,
            "Building 1 did not retain its one-storey source part.");
        var remainingFour = after.Features[four];
        Require(remainingFour.Parts.Select(part => part.SourceId).SequenceEqual(
            new[] { "osm:relation:11665640", "osm:way:828726368", "osm:way:828726369" }, StringComparer.Ordinal),
            "Research Building 4 lost or retained the wrong source pieces.");
        Require(remainingFour.Height == 7.8f && remainingFour.Storeys == 2, "Research Building 4 retained the removed pieces' height.");
        foreach (var id in new[] { one, four })
        {
            var old = before.Features[id]; var current = after.Features[id];
            Require(current.Name == old.Name && current.Zone == old.Zone && current.Family == old.Family &&
                current.Facade == old.Facade && current.Props?.GetRawText() == old.Props?.GetRawText(), "Parent properties changed.");
            foreach (var part in current.Parts)
                Require(Canonical(part) == Canonical(old.Parts.Single(previous => previous.SourceId == part.SourceId)), "A surviving real source part changed.");
        }
        foreach (var id in new[] { wingA, wingB })
            Require(Canonical(after.Features[id]) == Canonical(before.Features[id]), "An independent A/B entity or one of its parts changed.");
    }

    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
