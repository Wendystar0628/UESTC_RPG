using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class EffectiveRoadNetworkTests
{
    private const string Campus = """
        {"meta":{"origin":[103,30],"crs":"EPSG:32648","bounds":[-20,-20,20,20],"entityPrefix":"campus-test"},
         "boundary":[[[-20,-20],[20,-20],[20,20],[-20,20]]],"entities":[],"features":[
           {"id":"osm:way:10","entityId":null,"kind":"road","height":0,"polygons":[[[[-10,-10],[-2,-10],[-2,-2],[-10,-2]]]]},
           {"id":"osm:way:20","entityId":null,"kind":"building","height":8,"polygons":[[[[2,2],[10,2],[10,10],[2,10]]]]}
         ]}
        """;
    private const string RoadId = "campus-test:source:way:10";
    private const string BuildingId = "campus-test:source:way:20";
    private static readonly Polyline DesignLine = new([new(-5, 4), new(3, 6), new(1, 8)]);

    [TestCase]
    public void SurvivingDesignLinesPublishExactlyOnceWithAuthoredIdentityOrderWidthAndProps()
    {
        var add = Design("design:line");
        var world = Load([add]).World;
        var line = world.Network.Roads.Single(road => road.Id == add.Id);
        Require(world.Network.Roads.Count == 2 && line.Line.Points.SequenceEqual(DesignLine.Points) && line.Width == 2.5f &&
            line.Props?.GetRawText() == add.Props?.GetRawText(), "Projected road changed identity, point order, width or props.");
        Require(world.Features.ContainsKey(add.Id), "Projection removed the real design Feature.");
        foreach (var type in new[] { "footway", "path", "pedestrian" })
        {
            var byHighway = Design("design:" + type, "{\"highway\":\"" + type + "\",\"width\":3}");
            Require(Load([byHighway]).World.Network.Roads.Single(road => road.Id == byHighway.Id).Width == 3, "highway classification was lost.");
        }
        var styled = Load([add, new StyleOp(add.Id, new("fixture"), null)]).World;
        Require(styled.Network.Roads.Count(road => road.Id == add.Id) == 1, "A same-file style operation lost the design line's origin.");
    }

    [TestCase]
    public void SameFileAddHideAndReaddPreservesLegacySemanticsButNeverLeaksADeadRoad()
    {
        var add = Design("design:line");
        var hidden = Load([add, new HideOp(add.Id, "remove current design line")]).World;
        Require(!hidden.Features.ContainsKey(add.Id) && hidden.Network.Roads.Count == 1, "Add-hide leaked a design road.");
        var replacement = add with { Geometry = new PolylineGeometry(new([new(8, 2), new(4, 1)])) };
        var readded = Load([add, new HideOp(add.Id, "same-file replacement"), replacement]).World;
        Require(readded.Network.Roads.Single(road => road.Id == add.Id).Line.Points.SequenceEqual(((PolylineGeometry)replacement.Geometry).Line.Points),
            "Same-file add-hide-readd changed the surviving line.");
    }

    [TestCase]
    public void ExactRoadAndLegacySourceAliasHideRemoveBothStacksAtomically()
    {
        foreach (var target in new[] { RoadId, "osm:way:10" })
        {
            var previous = Load([]).World;
            var after = Load([new HideOp(new(target), "hide exact road")]).World;
            Require(!after.Features.ContainsKey(new(RoadId)) && after.Network.Roads.Count == 0, "Road hide left one stack alive.");
            Require(previous.Features.ContainsKey(new(RoadId)) && previous.Network.Roads.Count == 1, "Reload mutated an existing snapshot.");
        }
        var building = Load([new HideOp(new(BuildingId), "ordinary non-road hide")]).World;
        Require(!building.Features.ContainsKey(new(BuildingId)) && building.Network.Roads.Count == 1, "Ordinary non-road hide changed roads.");
    }

    [TestCase]
    public void OnlyProvenNetworkOnlyRoadsAreDeletedAndOtherNetworkCategoriesSurvive()
    {
        var road = Line("campus-test:source:way:99");
        var water = Line("campus-test:source:way:80");
        var network = new WorldNetwork([road], [water], [Line("campus-test:source:way:81")], [Line("campus-test:source:way:82")], [new(new("tree"), Vector3.Zero)]);
        var after = Load([new HideOp(road.Id, "true network-only road")], network: network).World;
        Require(after.Network.Roads.Count == 0 && Canonical(after.Network.Waterways) == Canonical(network.Waterways) &&
            Canonical(after.Network.Bridges) == Canonical(network.Bridges) && Canonical(after.Network.Culverts) == Canonical(network.Culverts) &&
            Canonical(after.Network.Trees) == Canonical(network.Trees) && after.Features.Count == 2, "Network-only hide changed another stack or category.");
        Contains(Fails(() => Load([new HideOp(new("other:source:way:99"), "unverified prefix")],
            network: new([Line("other:source:way:99")], [], [], [], []))), "verify", "other:source:way:99");
    }

    [TestCase]
    public void NamedSourceAndMixedOrMultipleSourceEntitiesRejectBothRoadAndFeatureEntrypoints()
    {
        var named = JsonNode.Parse(Campus)!;
        named["features"]![0]!["entityId"] = "campus-test:way:10";
        named["entities"]!.AsArray().Add(JsonNode.Parse("""{"id":"campus-test:way:10","name":"named road","kind":"road"}"""));
        foreach (var target in new[] { RoadId, "campus-test:way:10", "osm:way:10" })
            Contains(Fails(() => Load([new HideOp(new(target), "ambiguous named road")], named.ToJsonString())),
                "Ambiguous", RoadId, "osm:way:10", "campus-test:way:10");
        foreach (var mixed in new[] { false, true })
        {
            var multi = JsonNode.Parse(Campus)!;
            multi["features"]![0]!["entityId"] = RoadId;
            multi["features"]![1]!["entityId"] = RoadId;
            if (!mixed) multi["features"]![1]!["kind"] = "road";
            multi["entities"]!.AsArray().Add(JsonNode.Parse("{\"id\":\"" + RoadId + "\",\"name\":\"group\",\"kind\":\"road\"}"));
            foreach (var target in new[] { RoadId, "osm:way:10" })
                Contains(Fails(() => Load([new HideOp(new(target), "ambiguous source membership")], multi.ToJsonString())),
                    "Ambiguous", "osm:way:10", "osm:way:20", RoadId);
        }
    }

    [TestCase]
    public void HidePartRejectsReferencedRoadSourcesWithoutChangingOrdinaryBuildingPartRemoval()
    {
        var campus = JsonNode.Parse(Campus)!;
        campus["features"]![0]!["entityId"] = "campus-test:mixed";
        campus["features"]![1]!["entityId"] = "campus-test:mixed";
        campus["entities"]!.AsArray().Add(JsonNode.Parse("""{"id":"campus-test:mixed","name":"mixed source fixture","kind":"poi"}"""));
        var previous = Load([], campus.ToJsonString()).World;
        var before = Canonical(previous.Features[new("campus-test:mixed")]);
        Contains(Fails(() => Load([new HidePartOp(new("campus-test:mixed"), "osm:way:10", "unsupported road source removal")], campus.ToJsonString())),
            "hidePart", RoadId, "osm:way:10", "campus-test:mixed");
        Require(Canonical(previous.Features[new("campus-test:mixed")]) == before && previous.Network.Roads.Count == 1,
            "Rejected road hidePart changed a previously published snapshot.");
        var ordinary = Load([new HidePartOp(new("campus-test:mixed"), "osm:way:20", "ordinary building source removal")], campus.ToJsonString()).World;
        Require(ordinary.Features[new("campus-test:mixed")].Parts.Single().SourceId == "osm:way:10" && ordinary.Network.Roads.Count == 1,
            "The road guard changed an ordinary building source removal.");
    }

    [TestCase]
    public void RoadBridgeWaterwayCulvertAndAreaAmbiguitiesRejectEveryRoadHideEntrypoint()
    {
        var road = Line(RoadId);
        foreach (var network in new[]
        {
            new WorldNetwork([road], [], [road], [], []), new([road], [road], [], [], []), new([road], [], [], [road], []),
            new([road with { Props = Props("{\"highway\":\"pedestrian\",\"area\":\"yes\"}") }], [], [], [], [])
        })
            foreach (var target in new[] { RoadId, "osm:way:10" })
                Contains(Fails(() => Load([new HideOp(new(target), "ambiguous multi-class or area")], network: network)),
                    "Ambiguous", RoadId, "osm:way:10");
    }

    [TestCase]
    public void CrossFileSameOwnerWritesAndAddHideReaddCannotBypassTheConflictLedger()
    {
        var add = Design("design:line");
        foreach (var first in new OverlayOp[][] { [add, new HideOp(add.Id, "first file hide")], [add] })
        {
            var files = Files(first);
            files["overlay/b.overlay.json"] = Overlay([add]);
            Contains(Fails(() => WorldModelLoader.Load(new("test"), files)), "Overlay conflict", "overlay/a.overlay.json", "overlay/b.overlay.json", add.Id.Value);
        }
        foreach (var targets in new[] { new[] { RoadId, "osm:way:10" }, new[] { "osm:way:10", RoadId }, new[] { "campus-test:source:way:99", "campus-test:source:way:99" } })
        {
            var network = new WorldNetwork([Line(RoadId), Line("campus-test:source:way:99")], [], [], [], []);
            var files = Files([new HideOp(new(targets[0]), "first writer")], network: network);
            files["overlay/b.overlay.json"] = Overlay([new HideOp(new(targets[1]), "same owner is not a claim")]);
            Contains(Fails(() => WorldModelLoader.Load(new("test"), files)), "Overlay conflict", "overlay/a.overlay.json", "overlay/b.overlay.json");
        }
    }

    [TestCase]
    public void PolygonAndMultiPolygonRoadsKeepRealHolesAndDoNotBecomeCenterlines()
    {
        var rings = new PolygonRings([new(-10, 1), new(-2, 1), new(-2, 9), new(-10, 9)], [[new(-8, 3), new(-8, 7), new(-4, 7), new(-4, 3)]]);
        var polygon = Design("design:area") with { Geometry = new PolygonGeometry(rings) };
        var multi = polygon with { Id = new("design:multi"), Geometry = new MultiPolygonGeometry([rings, new([new(10, -5), new(12, -5), new(12, -3), new(10, -3)])]) };
        var closed = Design("design:area-ring", "{\"roadType\":\"pedestrian\",\"width\":2.5,\"area\":\"yes\"}") with
        { Geometry = new PolylineGeometry(new([new(0, 0), new(2, 0), new(2, 2), new(0, 0)])) };
        var world = Load([polygon, multi, closed]).World;
        Require(world.Network.Roads.Count == 1, "An area or its boundary was fabricated as a centerline.");
        Require(Canonical(world.Features[polygon.Id].Geometry) == Canonical(polygon.Geometry) &&
            Canonical(world.Features[multi.Id].Geometry) == Canonical(multi.Geometry), "Real area rings or holes changed.");
    }

    [TestCase]
    public void InvalidWidthsPointsDuplicateIdsAndConflictingClassificationsFailBeforePublication()
    {
        foreach (var json in new[]
        {
            "{\"roadType\":\"footway\"}", "{\"roadType\":\"footway\",\"width\":0}", "{\"roadType\":\"footway\",\"width\":-1}",
            "{\"roadType\":\"footway\",\"width\":\"2.5\"}", "{\"roadType\":\"footway\",\"width\":1e99}",
            "{\"roadType\":\"footway\",\"highway\":\"path\",\"width\":2.5}", "{\"roadType\":3,\"width\":2.5}",
            "{\"highway\":\" \",\"width\":2.5}"
        }) Contains(Fails(() => Load([Design("design:invalid", json)])), "design:invalid");
        foreach (var points in new Vector2[][] { [new(1, 1)], [new(1, 1), new(1, 1)] })
        {
            var add = Design("design:invalid") with { Geometry = new PolylineGeometry(new(points)) };
            _ = Fails(() => Load([add]));
        }
        var nonfinite = Files([Design("design:nonfinite")]);
        var overlay = JsonNode.Parse(nonfinite["overlay/a.overlay.json"])!;
        overlay["ops"]![0]!["geometry"]!["line"]!["points"]![1]![0] = JsonValue.Create(1e99);
        nonfinite["overlay/a.overlay.json"] = overlay.ToJsonString();
        _ = Fails(() => WorldModelLoader.Load(new("test"), nonfinite));
        var duplicated = new WorldNetwork([Line(RoadId), Line(RoadId)], [], [], [], []);
        Contains(Fails(() => Load([], network: duplicated)), "duplicate road ID", RoadId);
        var collision = new WorldNetwork([Line("design:line")], [], [], [], []);
        Contains(Fails(() => Load([Design("design:line")], network: collision)), "duplicates", "design:line");
        var previous = Load([Design("design:valid")]).World;
        var before = Canonical(previous.Network);
        var failed = Files([new HideOp(new(RoadId), "private removal"), Design("design:bad", "{\"roadType\":\"path\",\"width\":0}")]);
        _ = Fails(() => WorldModelLoader.Load(new("test"), failed));
        Require(Canonical(previous.Network) == before && previous.Features.ContainsKey(new(RoadId)), "Failure changed a published snapshot.");
    }

    private static AddOp Design(string id, string props = "{\"roadType\":\"footway\",\"width\":2.5,\"sourceRoad\":\"original-kept\"}") =>
        new(new(id), new("road"), new PolylineGeometry(DesignLine), null, Props(props), "synthetic road regression");
    private static JsonElement Props(string json) { using var document = JsonDocument.Parse(json); return document.RootElement.Clone(); }
    private static NetworkLine Line(string id) => new(new(id), new([new(-10, -6), new(-2, -6)]), 2) { Props = Props("{\"highway\":\"footway\"}") };
    private static string Overlay(IReadOnlyList<OverlayOp> ops) => JsonSerializer.Serialize(new OverlayFile(new("test"), new("WP-08"), ops), ContractJson.Options);
    private static Dictionary<string, string> Files(IReadOnlyList<OverlayOp> ops, string campus = Campus, WorldNetwork? network = null) => new(StringComparer.Ordinal)
    { ["campus.json"] = campus, ["network.json"] = Canonical(network ?? new([Line(RoadId)], [], [], [], [])), ["overlay/a.overlay.json"] = Overlay(ops) };
    private static WorldModelLoadResult Load(IReadOnlyList<OverlayOp> ops, string campus = Campus, WorldNetwork? network = null) =>
        WorldModelLoader.Load(new("test"), Files(ops, campus, network));
    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
    private static WorldDataException Fails(Action action)
    { try { action(); } catch (WorldDataException error) { return error; } throw new InvalidOperationException("Expected world load failure."); }
    private static void Contains(WorldDataException error, params string[] parts) => Contains(error.Message, parts);
    private static void Contains(string error, params string[] parts)
    { foreach (var part in parts) Require(error.Contains(part, StringComparison.Ordinal), "Missing error identity: " + part + " in " + error); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
