using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class WorldModelTests
{
    private const string Campus = """
        {"meta":{"origin":[103,30],"crs":"EPSG:32648","bounds":[-20,-20,20,20],"entityPrefix":"fixture"},
         "boundary":[[[-20,-20],[20,-20],[20,20],[-20,20],[-20,-20]]],
         "entities":[{"id":"fixture:relation:1","name":"Building","kind":"building"}],
         "features":[
           {"id":"osm:way:10","entityId":"fixture:relation:1","kind":"building","height":8,"levels":2,
            "polygons":[[[[-10,-10],[-2,-10],[-2,-2],[-10,-2],[-10,-10]]]]},
           {"id":"osm:way:11","entityId":"fixture:relation:1","kind":"building","height":16,"levels":4,
            "polygons":[[[[2,2],[10,2],[10,10],[2,10],[2,2]]]]},
           {"id":"osm:way:20","entityId":null,"kind":"water","height":0,
            "polygons":[[[[-10,2],[-2,2],[-2,10],[-10,10],[-10,2]],[[-8,4],[-4,4],[-4,8],[-8,8],[-8,4]]]]}
         ]}
        """;

    [TestCase]
    public void MultiWingEntitiesKeepTheirOwnHeightAndStableIdentity()
    {
        var result = Load();
        var feature = result.World.Features[new("fixture:relation:1")];
        Require(feature.Parts.Count == 2 && feature.Parts[0].Height == 8 && feature.Parts[1].Height == 16, "Wing heights were flattened.");
        Require(feature.Geometry is MultiPolygonGeometry { Polygons.Count: 2 }, "Lost a footprint.");
        Require(result.World.Spatial.MediumAt(new(-5, 10, -5)).Kind == MediumKind.Air, "Low wing became as tall as the high wing.");
        Require(result.World.Spatial.MediumAt(new(5, 10, 5)).Kind == MediumKind.Interior, "High wing missing.");
        Require(result.Center == Vector2.Zero, "Boundary centroid changed.");
    }

    [TestCase]
    public void HolesRemainDryAndBoundaryQueriesAreExact()
    {
        var spatial = Load().World.Spatial;
        Require(spatial.WalkClassAt(-6, 6) == WalkClass.Walk, "Water polygon hole filled.");
        Require(spatial.WalkClassAt(-9, 3) == WalkClass.Water, "Water missing.");
        Require(spatial.MediumAt(new(-9, -1, 3)).Kind == MediumKind.Underwater, "Underwater classification missing.");
        Require(spatial.WalkClassAt(21, 0) == WalkClass.Outside, "Exterior became walkable.");
        Require(spatial.SupportY(-5, -5, 0) is null, "Building became supported ground.");
    }

    [TestCase]
    public void OverlayFailureNeverMutatesThePreviousSnapshot()
    {
        var previous = Load();
        const string invalid = """{"zone":"test","owner":"WP-08","ops":[{"op":"hide","target":"fixture:relation:1","reason":"test"},{"op":"hide","target":"missing","reason":"test"}]}""";
        try { Load(invalid); throw new InvalidOperationException("Invalid overlay accepted."); }
        catch (WorldDataException e) { Require(e.File == "overlay/test.overlay.json", "Lost error filename."); }
        Require(previous.World.Features.ContainsKey(new("fixture:relation:1")), "Previous immutable snapshot changed.");
        var hidden = Load("""{"zone":"test","owner":"WP-08","ops":[{"op":"hide","target":"fixture:relation:1","reason":"test"}]}""");
        Require(!hidden.World.Features.ContainsKey(new("fixture:relation:1")), "Hide did not remove entity and its parts.");
    }

    [TestCase]
    public void RingNormalizationKeepsOrientationAndRejectsDegenerateInput()
    {
        var ring = PlanGeometry.OpenRing([new(0, 0), new(0, 2), new(2, 2), new(2, 0), new(0, 0)]);
        Require(ring.Count == 4 && PlanGeometry.SignedArea(ring) == 4, "Ring normalization failed.");
        Require(PlanGeometry.PointInRing(new(0, 1), ring), "Boundary point excluded.");
        try { PlanGeometry.OpenRing([new(0, 0), new(1, 1), new(2, 2)]); throw new InvalidOperationException("Degenerate ring accepted."); }
        catch (ArgumentException) { }
    }

    [TestCase]
    public void RealMigratedCampusLoadsEverySourcePartOnce()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        var data = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
        var campus = File.ReadAllText(Path.Combine(data, "campus.json"));
        var interiors = File.ReadAllText(Path.Combine(data, "interiors.json"));
        var result = WorldModelLoader.Load(new("qingshuihe"), new Dictionary<string, string>
        {
            ["campus.json"] = campus,
            ["network.json"] = File.ReadAllText(Path.Combine(data, "network.json")),
            ["interiors.json"] = interiors
        });
        using var original = JsonDocument.Parse(campus);
        foreach (var entity in original.RootElement.GetProperty("entities").EnumerateArray())
            Require(result.World.Features.ContainsKey(new(entity.GetProperty("id").GetString()!)), "Lost an existing entity identity.");
        var sourceParts = result.World.Features.Values.SelectMany(f => f.Parts).ToArray();
        Require(sourceParts.Length == 1261 && sourceParts.Select(p => p.SourceId).Distinct(StringComparer.Ordinal).Count() == 1261, "Source geometry was lost or duplicated.");
        Require(result.World.Network.Bridges.Count == 77 && result.World.Network.Culverts.Count == 15 && result.World.Network.Trees.Count == 146, "Network snapshot incomplete.");
        using var authoredInteriors = JsonDocument.Parse(interiors);
        var expectedEntrances = authoredInteriors.RootElement.GetProperty("interiors").EnumerateArray()
            .SelectMany(interior => interior.GetProperty("entrances").EnumerateArray())
            .Select(entrance => entrance.GetProperty("id").GetString()!)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var actualEntrances = result.World.Interiors.EntrancesNear(Vector3.Zero, 3000)
            .Select(entrance => entrance.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Require(actualEntrances.Distinct(StringComparer.Ordinal).Count() == actualEntrances.Length &&
            actualEntrances.SequenceEqual(expectedEntrances, StringComparer.Ordinal), "Authored interior entrances were lost or duplicated.");
        Require(result.Geo.Epsg == 32648 && result.Geo.OriginLon > 103 && result.Center.X > 0 && result.Center.Y > 0, "Map coordinate frame is wrong.");
    }

    [TestCase]
    public void ResidentWalkDataUsesExactDistanceAndRestoresGroundHeight()
    {
        var coordinates = Enumerable.Repeat(SafePointGrid.InvalidCoordinate, 12).ToArray();
        coordinates[0] = 4; coordinates[1] = 1; coordinates[2] = 4;
        var data = new WalkBakeData(1, new("test"), new string('a', 64),
            new(new(-16, -16), 8, 8, new byte[64], new byte[64], new float[64]),
            new(new(-16, -16), 2, 2, new(-16, 0, -16), coordinates), []);
        var result = WorldModelLoader.Load(new("test"), new Dictionary<string, string> { ["campus.json"] = Campus }, new() { Walk = data });
        Require(result.World.Spatial.NearestSafe(new(-15, 2, -15), 1) is null, "maxDist was ignored.");
        Require(result.World.Spatial.NearestSafe(new(-15, 2, -15), 3) == new Vector3(-15, 0, -15), "Quantized safety clearance did not resolve onto the support surface.");
        Require(result.World.Spatial.WalkClassAt(1, 1) == WalkClass.Outside, "Resident grid extent was ignored.");
    }

    [TestCase]
    public void ArchitectureReviewMixedParentsKeepBuildingPartsAndOpenParkDistinct()
    {
        var result = WorldModelLoader.Load(new("qingshuihe"), RealFiles());
        foreach (var sample in new[]
        {
            (Id: new FeatureId("qsh:way:682336066"), Point: new Vector3(-672.712f, 1, -106.672f), Height: 15.6f),
            (Id: new FeatureId("qsh:way:883187548"), Point: new Vector3(375.455f, 1, 80.05f), Height: 58.5f)
        })
        {
            var medium = result.World.Spatial.MediumAt(sample.Point);
            Require(medium.Kind == MediumKind.Interior && medium.Feature == sample.Id, $"Building part {sample.Id.Value} was classified as {medium.Kind}.");
            Require(result.World.Spatial.WalkClassAt(sample.Point.X, sample.Point.Z) == WalkClass.Building, "Building part became walkable.");
            Require(result.World.Interiors.Get(sample.Id) is not null, "Mixed parent lost its interior index.");
            Require(result.World.Spatial.MediumAt(sample.Point with { Y = sample.Height + 1 }).Kind == MediumKind.Air, "Part height was ignored.");
        }
        var outdoors = new Vector3(-619.9118f, 1, -102.2f);
        Require(result.World.Spatial.MediumAt(outdoors).Kind == MediumKind.Air, "Entire parent park became a building volume.");
        Require(result.World.Spatial.WalkClassAt(outdoors.X, outdoors.Z) == WalkClass.Walk, "Open park became a building obstacle.");
        var buildingParts = new[] { "qsh:way:682336066", "qsh:way:883187548" }
            .SelectMany(id => result.World.Features[new(id)].Parts.Where(part => part.Props?.GetProperty("kind").GetString() == "building")).ToArray();
        Require(buildingParts.Length == 10, "The regression fixture no longer contains the ten reviewed building parts.");
        foreach (var id in new[] { "qsh:way:682336066", "qsh:way:883187548" })
        {
            var feature = result.World.Features[new(id)];
            var buildingFootprints = feature.Parts.Where(part => part.Props?.GetProperty("kind").GetString() == "building")
                .SelectMany(part => part.Geometry is PolygonGeometry polygon ? [polygon.Rings] : ((MultiPolygonGeometry)part.Geometry).Polygons).Select(polygon => polygon.Outer).ToArray();
            var storeys = result.World.Interiors.Get(feature.Id)!.Storeys;
            Require(buildingFootprints.All(ring => storeys.Any(storey => storey.Footprint.SequenceEqual(ring))), "A building part is missing from the interior index.");
            Require(storeys.All(storey => buildingFootprints.Any(ring => storey.Footprint.SequenceEqual(ring))), "Interior index contains the parent park footprint.");
        }
    }

    [TestCase]
    public void ArchitectureReviewOverlayAliasesCannotSilentlyOverwriteAnotherFile()
    {
        var previous = Load();
        var files = new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["overlay/a.overlay.json"] = """{"zone":"test","owner":"WP-14","ops":[{"op":"height","target":"fixture:relation:1","height":20,"reason":"test"}]}""",
            ["overlay/b.overlay.json"] = """{"zone":"test","owner":"WP-15","ops":[{"op":"height","target":"osm:way:10","height":30,"reason":"test"}]}"""
        };
        try { WorldModelLoader.Load(new("test"), files); throw new InvalidOperationException("Conflicting overlays silently overwrote the same resolved entity."); }
        catch (WorldDataException error)
        {
            Require(error.Message.Contains("overlay/a.overlay.json", StringComparison.Ordinal) && error.Message.Contains("overlay/b.overlay.json", StringComparison.Ordinal), "Conflict omitted one of its source files.");
        }
        Require(previous.World.Features[new("fixture:relation:1")].Height == 16, "Failed reload mutated the previous snapshot.");
    }

    [TestCase]
    public void ArchitectureReviewOverlayClaimsMustBeExplicit()
    {
        var files = new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["overlay/a.overlay.json"] = """{"zone":"test","owner":"WP-14","ops":[{"op":"height","target":"fixture:relation:1","height":20,"reason":"test"}]}""",
            ["overlay/b.overlay.json"] = """{"zone":"test","owner":"WP-14","ops":[{"op":"height","target":"osm:way:10","height":30,"reason":"test"}]}"""
        };
        try { WorldModelLoader.Load(new("test"), files); throw new InvalidOperationException("Matching owner strings were treated as a claim."); }
        catch (WorldDataException) { }
        var claimedBy = new Dictionary<FeatureId, WpId> { [new("fixture:relation:1")] = new("WP-14") };
        var result = WorldModelLoader.Load(new("test"), files, new() { ClaimedBy = claimedBy });
        Require(result.World.Features[new("fixture:relation:1")].Height == 30, "Explicit registered claim did not allow the overlay exception.");
        claimedBy[new("fixture:relation:1")] = new("WP-99");
        try { WorldModelLoader.Load(new("test"), files, new() { ClaimedBy = claimedBy }); throw new InvalidOperationException("An unrelated owner's claim bypassed a conflict."); }
        catch (WorldDataException) { }
        files["overlay/a.overlay.json"] = """{"zone":"test","owner":"WP-14","ops":[{"op":"hide","target":"fixture:relation:1","reason":"test"}]}""";
        try { WorldModelLoader.Load(new("test"), files); throw new InvalidOperationException("Hide conflict was accepted."); }
        catch (WorldDataException error)
        {
            Require(error.Message.Contains("overlay/a.overlay.json", StringComparison.Ordinal) && error.Message.Contains("overlay/b.overlay.json", StringComparison.Ordinal), "Hide lost its conflict provenance.");
        }
    }

    [TestCase]
    public void ArchitectureReviewBoundsUseAuthoredRoamLimits()
    {
        var files = new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["map.json"] = """{"id":"test","roam":{"heightFactor":0.9,"heightStart":30,"maxRadius":3000,"floorY":-40,"ceilY":1500,"buffer":30,"softFloorClearance":40,"fallbackMargin":25}}"""
        };
        var result = WorldModelLoader.Load(new("test"), files);
        Require(result.Bounds == new Box3(new(-45, -40, -45), new(45, 1500, 45)), $"Authored roam bounds ignored: {result.Bounds}.");
        var real = WorldModelLoader.Load(new("qingshuihe"), RealFiles());
        Require(real.Bounds.Min.Y == -28 && real.Bounds.Max.Y == 2000, "Real campus vertical roam bounds were replaced with building heights.");
        Require(Math.Abs(real.Bounds.Min.X - (real.Boundary.Outer.Min(p => p.X) - 150)) < 0.001f, "Real campus fallback margin missing.");
    }

    [TestCase]
    public void SyntheticBeatAnchorsLoadOverRealCampusWithoutBecomingBuildings()
    {
        // contract-25 / MAP-QSH-CORE 8.6: synthetic regression inputs, not an authored campus walk layout.
        var files = RealFiles();
        files["overlay/core-beat-fixture.overlay.json"] = """
            {"zone":"core","owner":"WP-16","map":"qingshuihe","ops":[
              {"op":"add","id":"walk:beat-fixture-point","kind":"beat","geometry":{"type":"point","position":[-100,0,168]},"reason":"Regression fixture: time-square walking beat; not an authored map placement."},
              {"op":"add","id":"walk:beat-fixture-area","kind":"beat","geometry":{"type":"polygon","rings":{"outer":[[-331,94],[-329,94],[-329,96],[-331,96]],"holes":[]}},"reason":"Regression fixture: polygon beat area over real campus data."},
              {"op":"add","id":"walk:beat-fixture-multi","kind":"beat","geometry":{"type":"multiPolygon","polygons":[{"outer":[[-110,160],[-108,160],[-108,162],[-110,162]],"holes":[]},{"outer":[[-106,160],[-104,160],[-104,162],[-106,162]],"holes":[]}]},"reason":"Regression fixture: multi-area beat anchor, not building massing."}
            ]}
            """;
        var result = WorldModelLoader.Load(new("qingshuihe"), files);
        foreach (var id in new[] { "walk:beat-fixture-point", "walk:beat-fixture-area", "walk:beat-fixture-multi" })
        {
            var anchor = result.World.Features[new(id)];
            Require(anchor.Kind.Value == "beat" && anchor.Height is null && anchor.Parts.Count == 0, "Beat anchor acquired building semantics.");
            Require(anchor.Source == FeatureSource.Design && anchor.Zone == new ZoneId("core"), "Beat anchor lost overlay provenance.");
            Require(result.World.Interiors.Get(anchor.Id) is null, "Beat anchor created an interior.");
        }
        Require(result.World.Features[new("walk:beat-fixture-point")].Geometry is PointGeometry, "Point anchor shape changed.");
        Require(result.World.Features[new("walk:beat-fixture-area")].Geometry is PolygonGeometry, "Area anchor shape changed.");
        Require(result.World.Features[new("walk:beat-fixture-multi")].Geometry is MultiPolygonGeometry { Polygons.Count: 2 }, "Multi-area anchor lost a part.");
    }

    [TestCase]
    public void WalkPrefixRejectsNonBeatGeometryAndVolume()
    {
        var point = new PointGeometry(Vector3.Zero);
        var area = new PolygonGeometry(new([new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)]));
        var line = new PolylineGeometry(new([new(0, 0), new(1, 0)]));
        var invalid = new AddOp[]
        {
            new(new("walk:invalid-building"), new("building"), area, 8, null, "negative fixture"),
            new(new("walk:invalid-safe"), new("safe"), area, null, null, "negative fixture"),
            new(new("walk:invalid-anchor-alias"), new("anchor"), point, null, null, "negative fixture"),
            new(new("walk:invalid-line"), new("beat"), line, null, null, "negative fixture"),
            new(new("walk:invalid-volume"), new("beat"), point, 2, null, "negative fixture"),
            new(new("walk:"), new("beat"), point, null, null, "negative fixture")
        };
        foreach (var op in invalid)
        {
            var overlay = JsonSerializer.Serialize(new OverlayFile(new("test"), new("WP-16"), [op]), ContractJson.Options);
            try { Load(overlay); throw new InvalidOperationException($"Invalid walk add '{op.Id.Value}' was accepted."); }
            catch (WorldDataException error)
            {
                Require(error.File == "overlay/test.overlay.json" && error.Message.Contains("walk: beat anchor", StringComparison.Ordinal), "Invalid walk add lost a precise validation error.");
            }
        }
    }

    private static Dictionary<string, string> RealFiles()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        var data = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
        return new[] { "campus.json", "network.json", "interiors.json", "map.json" }.ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
    }

    private static WorldModelLoadResult Load(string? overlay = null)
    {
        var files = new Dictionary<string, string> { ["campus.json"] = Campus };
        if (overlay is not null) files["overlay/test.overlay.json"] = overlay;
        return WorldModelLoader.Load(new("test"), files);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
