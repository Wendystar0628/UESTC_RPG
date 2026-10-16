using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class HidePartOverlayTests
{
    private static readonly FeatureId Main = new("fixture:entity:main");
    private static readonly FeatureId Other = new("fixture:entity:other");
    private const string Campus = """
        {"meta":{"origin":[103,30],"crs":"EPSG:32648","bounds":[-50,-50,50,50],"entityPrefix":"fixture"},
         "boundary":[[[-50,-50],[50,-50],[50,50],[-50,50]]],
         "entities":[{"id":"fixture:entity:main","name":"Keep name","kind":"building","sentinel":{"keep":true}},
                     {"id":"fixture:entity:other","name":"Other","kind":"building"}],
         "features":[
           {"id":"osm:way:10","entityId":"fixture:entity:main","kind":"building","height":12,"levels":3,"family":"initial","sentinel":"removed",
            "polygons":[[[[-20,-20],[-10,-20],[-10,-10],[-20,-10]]]]},
           {"id":"osm:way:20","entityId":"fixture:entity:main","kind":"building","height":7.8,"levels":2,"family":"part-family","sentinel":"retained-multi",
            "polygons":[[[[0,0],[8,0],[8,8],[0,8]],[[2,2],[2,4],[4,4],[4,2]]],[[[10,0],[12,0],[12,2],[10,2]]]]},
           {"id":"osm:way:30","entityId":"fixture:entity:main","kind":"building","height":3.9,"levels":1,"sentinel":"retained-single",
            "polygons":[[[[20,0],[22,0],[22,3],[20,3]]]]},
           {"id":"osm:way:40","entityId":"fixture:entity:other","kind":"building","height":9,"levels":3,
            "polygons":[[[[30,0],[34,0],[34,4],[30,4]]]]}
         ]}
        """;
    private const string Zones = """{"zones":[{"id":"zone-keep","features":["fixture:entity:main"]}]}""";

    [TestCase]
    public void HidePartHasAnExplicitPolymorphicWireOperation()
    {
        var json = Overlay(new HidePartOp(Main, "osm:way:10", "remove one source"));
        using var document = JsonDocument.Parse(json);
        var operation = document.RootElement.GetProperty("ops")[0];
        Require(operation.EnumerateObject().First().Name == "op" && operation.GetProperty("op").GetString() == "hidePart",
            "hidePart lost its explicit first-field discriminator.");
        var roundTrip = JsonSerializer.Deserialize<OverlayFile>(json, ContractJson.Options)!;
        Require(roundTrip.Ops[0] is HidePartOp { SourceId: "osm:way:10" } hide && hide.Target == Main,
            "hidePart did not round-trip as its own operation.");
    }

    [TestCase]
    public void RemovalPreservesEntityAndPartPropertiesAndReaggregatesHolesAndHeight()
    {
        var style = new StyleOp(Main, new("styled-family"), new FacadePreset("styled-facade"));
        var before = Load([style]).World.Features[Main];
        var after = Load([style, new HidePartOp(Main, "osm:way:10", "remove duplicate source")]).World.Features[Main];
        Require(after.Id == before.Id && after.Name == before.Name && after.Zone == before.Zone &&
            after.Kind == before.Kind && after.Family == before.Family && after.Facade == before.Facade &&
            after.Props?.GetRawText() == before.Props?.GetRawText(), "Entity metadata changed during part removal.");
        Require(after.Source == FeatureSource.OsmOverlay && after.Parts.Count == 2,
            "Part removal lost the entity or its overlay provenance.");
        foreach (var part in after.Parts)
            Require(Canonical(part) == Canonical(before.Parts.Single(old => old.SourceId == part.SourceId)),
                "A retained part property changed.");
        Require(after.Height == 7.8f && after.Storeys == 2 &&
            after.Bounds == new Box3(new(0, 0, 0), new(22, 7.8f, 8)), "Aggregated height/storeys/bounds were not rebuilt from retained parts.");
        Require(after.Geometry is MultiPolygonGeometry { Polygons.Count: 3 }, "Retained multi-polygon pieces were lost.");
        var polygon = ((MultiPolygonGeometry)after.Geometry).Polygons[0];
        Require(polygon.Holes.Count == 1 && !PlanGeometry.Contains(polygon, new(3, 3)), "A retained courtyard hole was filled.");
    }

    [TestCase]
    public void RemovedAliasCannotDeleteSurvivorsAndOtherAliasesRemainUsable()
    {
        var hide = new HidePartOp(Main, "osm:way:10", "remove one part");
        var error = Fails(() => Load([hide, new HideOp(new("osm:way:10"), "old source alias must be gone")]));
        Require(error.Message.Contains("osm:way:10", StringComparison.Ordinal), "Removed alias failure lost its source identity.");
        var result = Load([hide, new StyleOp(new("osm:way:20"), new("retained-alias"), null),
            new HeightOp(new("osm:way:40"), 17, null, "unrelated alias remains")]);
        Require(result.World.Features[Main].Parts.Count == 2 && result.World.Features[Main].Family == new BuildingFamily("retained-alias"),
            "A retained source alias stopped resolving to its entity.");
        Require(result.World.Features[Other].Height == 17, "An unrelated entity/source alias was changed or removed.");
    }

    [TestCase]
    public void InvalidTargetsMissingRepeatedOrFinalPartsAndReasonsFailAtomically()
    {
        var previous = Load([]).World.Features[Main];
        var stable = Canonical(previous);
        OverlayOp[][] invalid =
        [
            [new HidePartOp(new("osm:way:10"), "osm:way:10", "target aliases forbidden")],
            [new HidePartOp(new("fixture:missing"), "osm:way:10", "unknown entity")],
            [new HidePartOp(Main, "osm:way:999", "unknown source")],
            [new HidePartOp(Main, "10", "short source forbidden")],
            [new HidePartOp(Main, " osm:way:10", "source normalization forbidden")],
            [new HidePartOp(Main, "osm:way:10", " \t")],
            [new HidePartOp(Main, "osm:way:10", "first"), new HidePartOp(Main, "osm:way:10", "repeated")],
            [new HidePartOp(Main, "osm:way:10", "first"), new HidePartOp(Main, "osm:way:20", "second"),
                new HidePartOp(Main, "osm:way:30", "last part must use entity hide")]
        ];
        foreach (var operations in invalid)
        {
            var error = Fails(() => Load(operations));
            Require(error.File == "overlay/test.overlay.json", "hidePart failure lost its file location.");
            Require(Canonical(previous) == stable, "A failed reload changed the previously published entity.");
        }
        // Loader already rejects duplicate canonical sources before overlay dispatch; the new operation also counts exact matches defensively.
        var duplicate = Campus.Replace("\"osm:way:30\"", "\"osm:way:20\"", StringComparison.Ordinal);
        _ = Fails(() => Load([new HidePartOp(Main, "osm:way:20", "ambiguous source")], duplicate));
    }

    [TestCase]
    public void DifferentPartsStillShareTheExistingWholeEntityConflictRule()
    {
        var files = BaseFiles();
        files["overlay/a.overlay.json"] = Overlay([new HidePartOp(Main, "osm:way:10", "first part")], "WP-14");
        files["overlay/b.overlay.json"] = Overlay([new HidePartOp(Main, "osm:way:20", "second part")], "WP-15");
        var error = Fails(() => WorldModelLoader.Load(new("fixture"), files));
        Require(error.Message.Contains("overlay/a.overlay.json", StringComparison.Ordinal) &&
            error.Message.Contains("overlay/b.overlay.json", StringComparison.Ordinal) && error.Message.Contains(Main.Value, StringComparison.Ordinal),
            "hidePart bypassed entity-level conflict provenance.");
        files["overlay/b.overlay.json"] = Overlay([new HidePartOp(Main, "osm:way:20", "same owner is not a claim")], "WP-14");
        _ = Fails(() => WorldModelLoader.Load(new("fixture"), files));
        var claims = new Dictionary<FeatureId, WpId> { [Main] = new("WP-14") };
        var allowed = WorldModelLoader.Load(new("fixture"), files, new() { ClaimedBy = claims }).World.Features[Main];
        Require(allowed.Parts.Single().SourceId == "osm:way:30", "The existing explicit-claim exception changed.");
        claims[Main] = new("WP-99");
        _ = Fails(() => WorldModelLoader.Load(new("fixture"), files, new() { ClaimedBy = claims }));
    }

    [TestCase]
    public void LegacyHideStillRemovesTheWholeEntityIncludingViaItsExistingAlias()
    {
        foreach (var target in new[] { Main, new FeatureId("osm:way:10") })
        {
            var result = Load([new HideOp(target, "explicit whole-entity removal")]);
            Require(!result.World.Features.ContainsKey(Main) && result.World.Features.ContainsKey(Other), "Legacy hide semantics changed.");
        }
    }

    private static WorldModelLoadResult Load(IReadOnlyList<OverlayOp> operations, string campus = Campus)
    {
        var files = BaseFiles(campus);
        if (operations.Count != 0) files["overlay/test.overlay.json"] = Overlay(operations, "WP-14");
        return WorldModelLoader.Load(new("fixture"), files);
    }
    private static Dictionary<string, string> BaseFiles(string campus = Campus) => new(StringComparer.Ordinal)
    { ["campus.json"] = campus, ["zones.json"] = Zones };
    private static string Overlay(params OverlayOp[] operations) => Overlay(operations, "WP-14");
    private static string Overlay(IReadOnlyList<OverlayOp> operations, string owner) =>
        JsonSerializer.Serialize(new OverlayFile(new("fixture-zone"), new(owner), operations), ContractJson.Options);
    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
    private static WorldDataException Fails(Action action)
    {
        try { action(); }
        catch (WorldDataException error) { return error; }
        throw new InvalidOperationException("Expected an invalid or conflicting overlay to fail.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
