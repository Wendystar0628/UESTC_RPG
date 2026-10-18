using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

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
    public void FacadeFootprintsUseTheSameRingConversionAndKeepPhysicalGeometry()
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(Campus)!;
        var first = root["features"]![0]!;
        first["facadePolygons"] = System.Text.Json.Nodes.JsonNode.Parse("""[[[[-10,-10],[-2,-10],[-2,-2],[-10,-2],[-10,-10]]]]""");
        first["polygons"] = System.Text.Json.Nodes.JsonNode.Parse("""[[[[-10,-10],[-6,-10.02],[-2,-10],[-2,-2],[-10,-2],[-10,-10]]]]""");
        var world = WorldModelLoader.Load(new("test"), new Dictionary<string, string> { ["campus.json"] = root.ToJsonString() }).World;
        var feature = world.Features[new("fixture:relation:1")];
        var part = feature.Parts[0];
        AssertThat(((PolygonGeometry)part.Geometry).Rings.Outer.Count == 5).OverrideFailureMessage("Physical source detail was simplified.").IsTrue();
        var facade = ((PolygonGeometry)WorldFeatureGeometry.FacadeGeometryOf(part)).Rings;
        AssertThat(facade.Outer.SequenceEqual(new Vector2[] { new(-10, -10), new(-2, -10), new(-2, -2), new(-10, -2) })).OverrideFailureMessage("Facade coordinate order or conversion changed.").IsTrue();
        AssertThat(ReferenceEquals(WorldFeatureGeometry.FacadeGeometryOf(feature.Parts[1]), feature.Parts[1].Geometry)).OverrideFailureMessage("Missing facade did not fall back to current physical geometry.").IsTrue();
        AssertThat(WorldFeatureGeometry.FacadePolygonsOf(feature).SelectMany(p => p.Outer).SequenceEqual(facade.Outer.Concat(((PolygonGeometry)feature.Parts[1].Geometry).Rings.Outer))).OverrideFailureMessage("Current part order changed.").IsTrue();
    }

    [TestCase]
    public void FacadeFootprintsRejectPresentEmptyMalformedAndMismatchedValues()
    {
        foreach (var value in new[] { "null", "{}", "[]", "[[]]", "[[[[0,0],[1,1],[2,2]]]]", "[[[[0],[1,0],[0,1]]]]", "[[[[0,0],[1,0],[0,1]]],[[[2,2],[3,2],[2,3]]]]", "[[[[0,0],[4,0],[0,4]],[[1,1],[2,1],[1,2]]]]" })
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Campus)!;
            root["features"]![0]!["facadePolygons"] = System.Text.Json.Nodes.JsonNode.Parse(value);
            try
            {
                WorldModelLoader.Load(new("test"), new Dictionary<string, string> { ["campus.json"] = root.ToJsonString() });
                throw new InvalidOperationException("Malformed facade footprint accepted: " + value);
            }
            catch (WorldDataException error)
            { AssertThat(error.File == "campus.json" && error.Entry == "osm:way:10.facadePolygons").OverrideFailureMessage("Lost source facade error context.").IsTrue(); }
        }
    }

    [TestCase]
    public void FacadeFootprintsFollowOverlayPartLifecycle()
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(Campus)!;
        foreach (var raw in root["features"]!.AsArray()) raw!["facadePolygons"] = raw["polygons"]!.DeepClone();
        WorldFeature Apply(string ops, string id = "fixture:relation:1") => WorldModelLoader.Load(new("test"), new Dictionary<string, string>
        {
            ["campus.json"] = root.ToJsonString(),
            ["overlay/test.d/0000-fixture.json"] = "{\"zone\":\"test\",\"owner\":\"WP-08\",\"ops\":[" + ops + "]}"
        }).World.Features[new(id)];
        var height = Apply("""{"op":"height","target":"fixture:relation:1","height":20,"reason":"test"}""");
        AssertThat(height.Parts.All(p => p.FacadeGeometry is not null && p.Height == 20)).OverrideFailureMessage("Height discarded facade geometry.").IsTrue();
        var style = Apply("""{"op":"style","target":"fixture:relation:1","family":"test"}""");
        AssertThat(style.Parts.All(p => p.FacadeGeometry is not null)).OverrideFailureMessage("Style discarded facade geometry.").IsTrue();
        var hidden = Apply("""{"op":"hidePart","target":"fixture:relation:1","sourceId":"osm:way:10","reason":"test"}""");
        AssertThat(hidden.Parts.Count == 1 && hidden.Parts[0].SourceId == "osm:way:11" && ReferenceEquals(WorldFeatureGeometry.FacadePolygonsOf(hidden)[0], ((PolygonGeometry)hidden.Parts[0].FacadeGeometry!).Rings)).OverrideFailureMessage("Hidden part survived in facade aggregation.").IsTrue();
        var reshaped = Apply("""{"op":"reshape","target":"fixture:relation:1","rings":{"outer":[[0,0],[3,0],[3,2],[0,2]],"holes":[]},"reason":"test"}""");
        AssertThat(reshaped.Parts.Count == 0 && ReferenceEquals(WorldFeatureGeometry.FacadePolygonsOf(reshaped)[0], ((PolygonGeometry)reshaped.Geometry).Rings)).OverrideFailureMessage("Reshape resurrected stale source facade geometry.").IsTrue();
        var added = Apply("""{"op":"add","id":"design:test","kind":"building","geometry":{"type":"polygon","rings":{"outer":[[0,0],[3,0],[3,2],[0,2]],"holes":[]}},"reason":"test"}""", "design:test");
        AssertThat(added.Parts.Count == 0 && ReferenceEquals(WorldFeatureGeometry.FacadePolygonsOf(added)[0], ((PolygonGeometry)added.Geometry).Rings)).OverrideFailureMessage("Add ignored current authored geometry.").IsTrue();
    }

    [TestCase]
    public void FacadeAggregationPreservesPolygonHoleAndPartOrderWithoutNormalization()
    {
        var first = new PolygonRings([new(3, 3), new(3, 0), new(0, 0), new(0, 3)], [new Vector2[] { new(1, 1), new(2, 1), new(1, 2) }]);
        var second = new PolygonRings([new(6, 6), new(6, 4), new(4, 4), new(4, 6)]);
        var physical = new PointGeometry(Vector3.Zero);
        var part = new WorldFeaturePart("second-source", physical, null, null) { FacadeGeometry = new MultiPolygonGeometry([first, second]) };
        var feature = new WorldFeature(new("fixture"), FeatureSource.Osm, new("building"), physical)
        { Parts = [part, new("first-source", new PolygonGeometry(second), null, null)] };
        var polygons = WorldFeatureGeometry.FacadePolygonsOf(feature);
        AssertThat(polygons.Count == 3 && ReferenceEquals(polygons[0], first) && ReferenceEquals(polygons[1], second) && ReferenceEquals(polygons[2], second)).OverrideFailureMessage("Facade aggregation reordered or rebuilt rings.").IsTrue();
        AssertThat(PlanGeometry.SignedArea(polygons[0].Outer) < 0 && ReferenceEquals(polygons[0].Holes, first.Holes)).OverrideFailureMessage("Facade helper normalized input direction or holes.").IsTrue();
    }

    [TestCase]
    public void MultiWingEntitiesKeepTheirOwnHeightAndStableIdentity()
    {
        var result = Load();
        var feature = result.World.Features[new("fixture:relation:1")];
        AssertThat(feature.Parts.Count == 2 && feature.Parts[0].Height == 8 && feature.Parts[1].Height == 16).OverrideFailureMessage("Wing heights were flattened.").IsTrue();
        AssertThat(feature.Geometry is MultiPolygonGeometry { Polygons.Count: 2 }).OverrideFailureMessage("Lost a footprint.").IsTrue();
        AssertThat(result.World.Spatial.MediumAt(new(-5, 10, -5)).Kind == MediumKind.Air).OverrideFailureMessage("Low wing became as tall as the high wing.").IsTrue();
        AssertThat(result.World.Spatial.MediumAt(new(5, 10, 5)).Kind == MediumKind.Interior).OverrideFailureMessage("High wing missing.").IsTrue();
        AssertThat(result.Center == Vector2.Zero).OverrideFailureMessage("Boundary centroid changed.").IsTrue();
    }

    [TestCase]
    public void HolesRemainDryAndBoundaryQueriesAreExact()
    {
        var spatial = Load().World.Spatial;
        AssertThat(spatial.WalkClassAt(-6, 6) == WalkClass.Walk).OverrideFailureMessage("Water polygon hole filled.").IsTrue();
        AssertThat(spatial.WalkClassAt(-9, 3) == WalkClass.Water).OverrideFailureMessage("Water missing.").IsTrue();
        AssertThat(spatial.MediumAt(new(-9, -1, 3)).Kind == MediumKind.Underwater).OverrideFailureMessage("Underwater classification missing.").IsTrue();
        AssertThat(spatial.WalkClassAt(21, 0) == WalkClass.Outside).OverrideFailureMessage("Exterior became walkable.").IsTrue();
        AssertThat(spatial.WalkClassAt(-5, -5) == WalkClass.Walk).OverrideFailureMessage("A building projection blocked its physical openings.").IsTrue();
    }

    [TestCase]
    public void OverlayFailureNeverMutatesThePreviousSnapshot()
    {
        var previous = Load();
        const string invalid = """{"zone":"test","owner":"WP-08","ops":[{"op":"hide","target":"fixture:relation:1","reason":"test"},{"op":"hide","target":"missing","reason":"test"}]}""";
        try { Load(invalid); throw new InvalidOperationException("Invalid overlay accepted."); }
        catch (WorldDataException e) { AssertThat(e.File == "overlay/test.d/0000-fixture.json").OverrideFailureMessage("Lost error filename.").IsTrue(); }
        AssertThat(previous.World.Features.ContainsKey(new("fixture:relation:1"))).OverrideFailureMessage("Previous immutable snapshot changed.").IsTrue();
        var hidden = Load("""{"zone":"test","owner":"WP-08","ops":[{"op":"hide","target":"fixture:relation:1","reason":"test"}]}""");
        AssertThat(!hidden.World.Features.ContainsKey(new("fixture:relation:1"))).OverrideFailureMessage("Hide did not remove entity and its parts.").IsTrue();
    }

    [TestCase]
    public void RingNormalizationKeepsOrientationAndRejectsDegenerateInput()
    {
        var ring = PlanGeometry.OpenRing([new(0, 0), new(0, 2), new(2, 2), new(2, 0), new(0, 0)]);
        AssertThat(ring.Count == 4 && PlanGeometry.SignedArea(ring) == 4).OverrideFailureMessage("Ring normalization failed.").IsTrue();
        AssertThat(PlanGeometry.PointInRing(new(0, 1), ring)).OverrideFailureMessage("Boundary point excluded.").IsTrue();
        try { PlanGeometry.OpenRing([new(0, 0), new(1, 1), new(2, 2)]); throw new InvalidOperationException("Degenerate ring accepted."); }
        catch (ArgumentException) { }
    }

    [TestCase]
    public void RealMigratedCampusLoadsEverySourcePartOnce()
    {
        var files = RealMapFixture.ReadTextFiles().Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var campus = files["campus.json"];
        var interiors = files["interiors.json"];
        var networkJson = files["network.json"];
        var result = RealMapFixture.LoadPrepared(files, options: new() { ClaimedBy = new Dictionary<FeatureId, WpId>() });
        using var original = JsonDocument.Parse(campus);
        foreach (var entity in original.RootElement.GetProperty("entities").EnumerateArray())
            AssertThat(result.World.Features.ContainsKey(new(entity.GetProperty("id").GetString()!))).OverrideFailureMessage("Lost an existing entity identity.").IsTrue();
        var sourceParts = result.World.Features.Values.SelectMany(f => f.Parts).ToArray();
        var sourceCount = original.RootElement.GetProperty("features").GetArrayLength();
        AssertThat(sourceParts.Length == sourceCount && sourceParts.Select(p => p.SourceId).Distinct(StringComparer.Ordinal).Count() == sourceCount).OverrideFailureMessage("Source geometry was lost or duplicated.").IsTrue();
        using var network = JsonDocument.Parse(networkJson);
        int Authored(string name) => network.RootElement.GetProperty(name).GetArrayLength();
        AssertThat(result.World.Network.Bridges.Count == Authored("bridges") && result.World.Network.Culverts.Count == Authored("culverts") &&
            result.World.Network.Trees.Count == Authored("trees")).OverrideFailureMessage("Network snapshot incomplete.").IsTrue();
        using var authoredInteriors = JsonDocument.Parse(interiors);
        var expectedEntrances = authoredInteriors.RootElement.GetProperty("interiors").EnumerateArray()
            .SelectMany(interior => interior.GetProperty("entrances").EnumerateArray())
            .Concat(authoredInteriors.RootElement.TryGetProperty("closedEntrances", out var closed)
                ? closed.EnumerateArray() : Enumerable.Empty<JsonElement>())
            .Select(entrance => entrance.GetProperty("id").GetString()!)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var actualEntrances = result.World.Interiors.EntrancesNear(Vector3.Zero, 3000)
            .Select(entrance => entrance.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        AssertThat(actualEntrances.Distinct(StringComparer.Ordinal).Count() == actualEntrances.Length &&
            actualEntrances.SequenceEqual(expectedEntrances, StringComparer.Ordinal)).OverrideFailureMessage("Authored interior entrances were lost or duplicated.").IsTrue();
        foreach (var raw in authoredInteriors.RootElement.GetProperty("closedEntrances").EnumerateArray())
        {
            var entry = result.World.Interiors.Get(new(raw.GetProperty("featureId").GetString()!))!;
            var door = entry.Entrances.Single(entrance => entrance.Id == raw.GetProperty("id").GetString());
            AssertThat(!door.Open && door.Center == raw.GetProperty("doorCenter").Deserialize<Vector3>(ContractJson.Options) &&
                door.Outside == raw.GetProperty("outsideLanding").Deserialize<Vector3>(ContractJson.Options) &&
                door.Inside == raw.GetProperty("insideLanding").Deserialize<Vector3>(ContractJson.Options) &&
                door.YawDeg == raw.GetProperty("facingDeg").GetSingle() && door.ClearWidth == raw.GetProperty("clearWidth").GetSingle()).OverrideFailureMessage("An authored closed entrance changed its position, dimensions or availability.").IsTrue();
        }
        AssertThat(result.Geo.Epsg == 32648 && result.Geo.OriginLon > 103 && result.Center.X > 0 && result.Center.Y > 0).OverrideFailureMessage("Map coordinate frame is wrong.").IsTrue();
    }

    [TestCase]
    public void ClosedRegisterPreservesBuildingStoreysAndAddsOnlyQueryableClosedDoors()
    {
        var baseline = Load().World.Interiors.Get(new("fixture:relation:1"))!;
        var world = LoadClosed().World;
        var entry = world.Interiors.Get(new("fixture:relation:1"))!;
        AssertThat(!entry.Open && entry.ProviderId is null && entry.Storeys.Count == baseline.Storeys.Count &&
            entry.Storeys.Zip(baseline.Storeys).All(pair => pair.First.Index == pair.Second.Index &&
                pair.First.FloorY == pair.Second.FloorY && pair.First.Height == pair.Second.Height &&
                pair.First.Footprint.SequenceEqual(pair.Second.Footprint))).OverrideFailureMessage("Closed registration changed building availability or storeys.").IsTrue();
        var entrance = entry.Entrances.Single();
        AssertThat(!entrance.Open && entrance.Center == new Vector3(-5, .15f, -5) &&
            world.Interiors.EntrancesNear(entrance.Center, .1f).Single() == entrance).OverrideFailureMessage("Closed entrance was lost from spatial lookup.").IsTrue();
        AssertThat(Load().World.Interiors.EntrancesNear(Vector3.Zero, 100).Count == 0).OverrideFailureMessage("Missing closed register no longer defaults empty.").IsTrue();
    }

    [TestCase]
    public void ClosedRegisterRejectsInvalidClaimsWithCatalogAndDoorDiagnostics()
    {
        foreach (var record in new[]
        {
            ClosedDoor.Replace("\"open\":false", "\"open\":true", StringComparison.Ordinal),
            ClosedDoor.Replace("fixture:relation:1", "missing", StringComparison.Ordinal),
            ClosedDoor.Replace("fixture:relation:1", "fixture:source:way:20", StringComparison.Ordinal),
            ClosedDoor + "," + ClosedDoor,
            ClosedDoor.Replace("\"open\":false", "\"open\":false,\"heightRef\":\"pad\"", StringComparison.Ordinal)
        })
        {
            try { LoadClosed(record); throw new InvalidOperationException("Invalid closed entrance accepted."); }
            catch (WorldDataException error)
            { AssertThat(error.File == "interiors.json" && error.Entry == "entry.closed").OverrideFailureMessage("Closed error lost its catalog or door ID.").IsTrue(); }
        }
        const string open = """{"featureIds":["fixture:relation:1"],"providerId":"fixture.provider","entrances":[]}""";
        try { LoadClosed(open: open); throw new InvalidOperationException("Open/closed feature overlap accepted."); }
        catch (WorldDataException error) { AssertThat(error.File == "interiors.json" && error.Entry == "entry.closed").OverrideFailureMessage("Lost overlap diagnostic.").IsTrue(); }
        const string otherOpen = """{"featureIds":["other"],"providerId":"fixture.provider","entrances":[{"id":"entry.closed","featureId":"other"}]}""";
        try { LoadClosed(open: otherOpen); throw new InvalidOperationException("Cross-register duplicate ID accepted."); }
        catch (WorldDataException error) { AssertThat(error.File == "interiors.json" && error.Entry == "entry.closed").OverrideFailureMessage("Lost duplicate diagnostic.").IsTrue(); }
        foreach (var malformed in new[] { "null", "42", "{}" })
        {
            try { LoadClosed(malformed); throw new InvalidOperationException("Malformed closed record accepted."); }
            catch (WorldDataException error)
            { AssertThat(error.File == "interiors.json" && error.Entry == "closedEntrances[0]").OverrideFailureMessage("Malformed closed record lost its catalog array path.").IsTrue(); }
        }
    }

    private const string ClosedDoor = """
        {"id":"entry.closed","featureId":"fixture:relation:1","doorCenter":[-5,0.15,-5],
         "outsideLanding":[-5,0.15,-6],"insideLanding":[-5,0.15,-4],"facingDeg":0,"clearWidth":2.4,"open":false}
        """;

    private static WorldModelLoadResult LoadClosed(string record = ClosedDoor, string open = "") =>
        WorldModelLoader.Load(new("test"), new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["interiors.json"] = "{\"version\":1,\"interiors\":[" + open + "],\"closedEntrances\":[" + record + "]}"
        });

    [TestCase]
    public void ArchitectureReviewMixedParentsKeepBuildingPartsAndOpenParkDistinct()
    {
        var result = RealMapFixture.LoadPrepared(RealMapFixture.ReadTextFiles().Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal), options: new() { ClaimedBy = new Dictionary<FeatureId, WpId>() });
        var samples = new[]
        {
            (Id: new FeatureId("qsh:way:682336066"), Point: new Vector3(-672.712f, 1, -106.672f)),
            (Id: new FeatureId("qsh:way:883187548"), Point: new Vector3(375.455f, 1, 80.05f))
        };
        foreach (var sample in samples)
        {
            var feature = result.World.Features[sample.Id];
            var part = WorldFeatureKinds.Parts(feature, "building").Single(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)
                .Any(polygon => PlanGeometry.Contains(polygon, new(sample.Point.X, sample.Point.Z))));
            var height = part.Height ?? feature.Height ?? 0;
            var medium = result.World.Spatial.MediumAt(sample.Point);
            AssertThat(medium.Kind == MediumKind.Interior && medium.Feature == sample.Id).OverrideFailureMessage($"Building part {sample.Id.Value} was classified as {medium.Kind}.").IsTrue();
            AssertThat(result.World.Spatial.WalkClassAt(sample.Point.X, sample.Point.Z) == WalkClass.Walk).OverrideFailureMessage("A building projection blocked its physical openings.").IsTrue();
            AssertThat(result.World.Interiors.Get(sample.Id) is not null).OverrideFailureMessage("Mixed parent lost its interior index.").IsTrue();
            AssertThat(result.World.Spatial.MediumAt(sample.Point with { Y = height + 1 }).Kind == MediumKind.Air).OverrideFailureMessage("Part height was ignored.").IsTrue();
        }
        var outdoors = new Vector3(-619.9118f, 1, -102.2f);
        AssertThat(result.World.Spatial.MediumAt(outdoors).Kind == MediumKind.Air).OverrideFailureMessage("Entire parent park became a building volume.").IsTrue();
        AssertThat(result.World.Spatial.WalkClassAt(outdoors.X, outdoors.Z) == WalkClass.Walk).OverrideFailureMessage("Open park became a building obstacle.").IsTrue();
        foreach (var id in samples.Select(sample => sample.Id))
        {
            var feature = result.World.Features[id];
            var buildingFootprints = feature.Parts.Where(part => part.Props?.GetProperty("kind").GetString() == "building")
                .SelectMany(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)).Select(polygon => polygon.Outer).ToArray();
            AssertThat(buildingFootprints.Length > 0).OverrideFailureMessage($"The regression fixture {id} no longer has building parts.").IsTrue();
            var storeys = result.World.Interiors.Get(feature.Id)!.Storeys;
            AssertThat(buildingFootprints.All(ring => storeys.Any(storey => storey.Footprint.SequenceEqual(ring)))).OverrideFailureMessage("A building part is missing from the interior index.").IsTrue();
            AssertThat(storeys.All(storey => buildingFootprints.Any(ring => storey.Footprint.SequenceEqual(ring)))).OverrideFailureMessage("Interior index contains the parent park footprint.").IsTrue();
        }
    }

    [TestCase]
    public void ArchitectureReviewOverlayAliasesCannotSilentlyOverwriteAnotherFile()
    {
        var previous = Load();
        var files = new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["overlay/a.d/0000-fixture.json"] = """{"zone":"test","owner":"WP-14","ops":[{"op":"height","target":"fixture:relation:1","height":20,"reason":"test"}]}""",
            ["overlay/b.d/0000-fixture.json"] = """{"zone":"test","owner":"WP-15","ops":[{"op":"height","target":"osm:way:10","height":30,"reason":"test"}]}"""
        };
        try { WorldModelLoader.Load(new("test"), files); throw new InvalidOperationException("Conflicting overlays silently overwrote the same resolved entity."); }
        catch (WorldDataException) { }
        AssertThat(previous.World.Features[new("fixture:relation:1")].Height == 16).OverrideFailureMessage("Failed reload mutated the previous snapshot.").IsTrue();
    }

    [TestCase]
    public void ArchitectureReviewOverlayClaimsMustBeExplicit()
    {
        var files = new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["overlay/a.d/0000-fixture.json"] = """{"zone":"test","owner":"WP-14","ops":[{"op":"height","target":"fixture:relation:1","height":20,"reason":"test"}]}""",
            ["overlay/b.d/0000-fixture.json"] = """{"zone":"test","owner":"WP-14","ops":[{"op":"height","target":"osm:way:10","height":30,"reason":"test"}]}"""
        };
        try { WorldModelLoader.Load(new("test"), files); throw new InvalidOperationException("Matching owner strings were treated as a claim."); }
        catch (WorldDataException) { }
        var claimedBy = new Dictionary<FeatureId, WpId> { [new("fixture:relation:1")] = new("WP-14") };
        var result = WorldModelLoader.Load(new("test"), files, new() { ClaimedBy = claimedBy });
        AssertThat(result.World.Features[new("fixture:relation:1")].Height == 30).OverrideFailureMessage("Explicit registered claim did not allow the overlay exception.").IsTrue();
        claimedBy[new("fixture:relation:1")] = new("WP-99");
        try { WorldModelLoader.Load(new("test"), files, new() { ClaimedBy = claimedBy }); throw new InvalidOperationException("An unrelated owner's claim bypassed a conflict."); }
        catch (WorldDataException) { }
        files["overlay/a.d/0000-fixture.json"] = """{"zone":"test","owner":"WP-14","ops":[{"op":"hide","target":"fixture:relation:1","reason":"test"}]}""";
        try { WorldModelLoader.Load(new("test"), files); throw new InvalidOperationException("Hide conflict was accepted."); }
        catch (WorldDataException) { }
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
        AssertThat(result.Bounds == new Box3(new(-45, -40, -45), new(45, 1500, 45))).OverrideFailureMessage($"Authored roam bounds ignored: {result.Bounds}.").IsTrue();
        var real = RealMapFixture.LoadPrepared(RealMapFixture.ReadTextFiles().Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal), options: new() { ClaimedBy = new Dictionary<FeatureId, WpId>() });
        var roam = RealMapFixture.Entry().Descriptor.Roam;
        AssertThat(real.Bounds.Min.Y == roam.FloorY && real.Bounds.Max.Y == roam.CeilY).OverrideFailureMessage("Real campus vertical roam bounds were replaced with building heights.").IsTrue();
        AssertThat(Math.Abs(real.Bounds.Min.X - (real.Boundary.Outer.Min(p => p.X) - roam.FallbackMargin)) < 0.01f).OverrideFailureMessage("Real campus fallback margin missing.").IsTrue();
    }

    [TestCase]
    public void SyntheticBeatAnchorsLoadOverRealCampusWithoutBecomingBuildings()
    {
        // contract-25 / MAP-QSH-CORE 8.6: synthetic regression inputs, not an authored campus walk layout.
        var files = RealMapFixture.ReadTextFiles().Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        files["overlay/core-beat-fixture.d/0000-fixture.json"] = """
            {"zone":"core","owner":"WP-16","map":"qingshuihe","ops":[
              {"op":"add","id":"walk:beat-fixture-point","kind":"beat","geometry":{"type":"point","position":[-100,0,168]},"reason":"Regression fixture: time-square walking beat; not an authored map placement."},
              {"op":"add","id":"walk:beat-fixture-area","kind":"beat","geometry":{"type":"polygon","rings":{"outer":[[-331,94],[-329,94],[-329,96],[-331,96]],"holes":[]}},"reason":"Regression fixture: polygon beat area over real campus data."},
              {"op":"add","id":"walk:beat-fixture-multi","kind":"beat","geometry":{"type":"multiPolygon","polygons":[{"outer":[[-110,160],[-108,160],[-108,162],[-110,162]],"holes":[]},{"outer":[[-106,160],[-104,160],[-104,162],[-106,162]],"holes":[]}]},"reason":"Regression fixture: multi-area beat anchor, not building massing."}
            ]}
            """;
        var result = RealMapFixture.LoadPrepared(files, options: new() { ClaimedBy = new Dictionary<FeatureId, WpId>() });
        foreach (var id in new[] { "walk:beat-fixture-point", "walk:beat-fixture-area", "walk:beat-fixture-multi" })
        {
            var anchor = result.World.Features[new(id)];
            AssertThat(anchor.Kind.Value == "beat" && anchor.Height is null && anchor.Parts.Count == 0).OverrideFailureMessage("Beat anchor acquired building semantics.").IsTrue();
            AssertThat(anchor.Source == FeatureSource.Design && anchor.Zone == new ZoneId("core")).OverrideFailureMessage("Beat anchor lost overlay provenance.").IsTrue();
            AssertThat(result.World.Interiors.Get(anchor.Id) is null).OverrideFailureMessage("Beat anchor created an interior.").IsTrue();
        }
        AssertThat(result.World.Features[new("walk:beat-fixture-point")].Geometry is PointGeometry).OverrideFailureMessage("Point anchor shape changed.").IsTrue();
        AssertThat(result.World.Features[new("walk:beat-fixture-area")].Geometry is PolygonGeometry).OverrideFailureMessage("Area anchor shape changed.").IsTrue();
        AssertThat(result.World.Features[new("walk:beat-fixture-multi")].Geometry is MultiPolygonGeometry { Polygons.Count: 2 }).OverrideFailureMessage("Multi-area anchor lost a part.").IsTrue();
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
                AssertThat(error.File == "overlay/test.d/0000-fixture.json").OverrideFailureMessage("Invalid walk add lost a precise validation error.").IsTrue();
            }
        }
    }

    [TestCase]
    public void SplitOverlaysKeepOperationOrderAndDerivedQueries()
    {
        var files = new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["overlay/test.d/02-detail.json"] = Overlay([new StyleOp(new("fixture:relation:1"), new("fixture-style"), null),
                new HideOp(new("fixture:source:way:20"), "fixture removal"),
                new AddOp(new("design:split-fixture"), new("building"), new PointGeometry(new(15, 0, 15)), 4, null, "fixture addition")]),
            ["overlay/test.d/01-shape.json"] = Overlay([new HeightOp(new("fixture:relation:1"), 24, 6, "fixture height"),
                new HeightOp(new("fixture:relation:1"), null, 3, "fixture storeys")])
        };
        var world = WorldModelLoader.Load(new("test"), files).World;
        AssertThat(world.Features[new("fixture:relation:1")].Height == 12).OverrideFailureMessage("Operations within a shard were reordered.").IsTrue();
        AssertThat(world.Features[new("fixture:relation:1")].Family?.Value == "fixture-style").OverrideFailureMessage("Later shard style did not apply.").IsTrue();
        AssertThat(!world.Features.ContainsKey(new("fixture:source:way:20")) && world.Features.ContainsKey(new("design:split-fixture"))).OverrideFailureMessage("Shard hide/add did not apply.").IsTrue();
        AssertThat(world.Spatial.MediumAt(new(5, 10, 5)).Kind == MediumKind.Interior).OverrideFailureMessage("Shard height did not enter the spatial query.").IsTrue();
    }

    [TestCase]
    public void SplitOverlaysUseOrdinalGroupNamesAndFilenames()
    {
        var files = new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["overlay/a.d/a.json"] = Overlay([new HeightOp(new("fixture:relation:1"), 40, null, "last ordinal shard")]),
            ["overlay/a.more.d/01.json"] = Overlay([new HeightOp(new("fixture:relation:1"), 19, null, "last ordinal group")]),
            ["overlay/a.d/B.json"] = Overlay([new HeightOp(new("fixture:relation:1"), 30, null, "first ordinal shard")])
        };
        var options = new WorldLoadOptions { ClaimedBy = new Dictionary<FeatureId, WpId> { [new("fixture:relation:1")] = new("WP-08") } };
        AssertThat(WorldModelLoader.Load(new("test"), files, options).World.Features[new("fixture:relation:1")].Height == 19).OverrideFailureMessage("Group names are not Ordinal.").IsTrue();
        files.Remove("overlay/a.more.d/01.json");
        AssertThat(WorldModelLoader.Load(new("test"), files).World.Features[new("fixture:relation:1")].Height == 40).OverrideFailureMessage("Shard filenames are not Ordinal.").IsTrue();
    }

    [TestCase]
    public void SplitOverlaysKeepInteriorBetweenEarlierAndLaterGroups()
    {
        var interior = new InteriorOverlayFile(new("test"), new("WP-08"),
            [new SealOp(new("design:before-interior"), 0, Vector2.Zero, Vector2.One, SealKind.FireDoor, "earlier group"),
                new SealOp(new("fixture:relation:1"), 0, Vector2.Zero, Vector2.One, SealKind.FireDoor, "later group")]);
        var files = new Dictionary<string, string>
        {
            ["campus.json"] = Campus,
            ["overlay/north.d/01.json"] = Overlay([new HideOp(new("fixture:relation:1"), "after interior")]),
            ["overlay/interior/test.interior.json"] = JsonSerializer.Serialize(interior, ContractJson.Options),
            ["overlay/alpha.d/01.json"] = Overlay([new AddOp(new("design:before-interior"), new("building"),
                new PointGeometry(new(15, 0, 15)), 4, null, "before interior")])
        };
        var world = (WorldModelSnapshot)WorldModelLoader.Load(new("test"), files).World;
        AssertThat(world.InteriorOverlays.Single().Ops.Count == 2).OverrideFailureMessage("Interior references must resolve between the alpha add and north hide.").IsTrue();
        AssertThat(world.Features.ContainsKey(new("design:before-interior")) && !world.Features.ContainsKey(new("fixture:relation:1"))).OverrideFailureMessage("Groups around the interior did not apply.").IsTrue();
    }

    [TestCase]
    public void FeatureOverlaysRejectRetiredInputsEvenWhenEmpty()
    {
        foreach (var path in new[] { "overlay/test.overlay.json", "overlay/another/test.overlay.json", "overlay/test.d/nested/03.overlay.json" })
            foreach (var operations in new OverlayOp[][] { [], [new HeightOp(new("fixture:relation:1"), 20, null, "retired")] })
            {
                var files = new Dictionary<string, string> { ["campus.json"] = Campus, [path] = Overlay(operations) };
                var error = OverlayError(files);
                AssertThat(error.File == path && error.Entry == "path").OverrideFailureMessage("Retired input was ignored or lost its path.").IsTrue();
            }
    }

    [TestCase]
    public void SplitOverlaysKeepRealMapOperationAndConflictErrorSources()
    {
        var files = new Dictionary<string, string> { ["campus.json"] = Campus, ["overlay/test.d/01.json"] = "{" };
        AssertThat(OverlayError(files).File == "overlay/test.d/01.json").OverrideFailureMessage("Malformed shard lost its filename.").IsTrue();
        files["overlay/test.d/01.json"] = Overlay([], map: "another-map");
        var mapError = OverlayError(files);
        AssertThat(mapError.File == "overlay/test.d/01.json" && mapError.Entry == "map").OverrideFailureMessage("Shard map validation lost its source.").IsTrue();
        files["overlay/test.d/01.json"] = Overlay([new HeightOp(new("fixture:relation:1"), -1, null, "invalid")]);
        var operationError = OverlayError(files);
        AssertThat(operationError.File == "overlay/test.d/01.json" && operationError.Entry == "ops[0]").OverrideFailureMessage("Shard operation validation lost its source.").IsTrue();
        files["overlay/test.d/01.json"] = Overlay([new HeightOp(new("fixture:relation:1"), 20, null, "first")], "WP-14");
        files["overlay/test.d/02.json"] = Overlay([new HeightOp(new("osm:way:10"), 30, null, "second")], "WP-15");
        var conflict = OverlayError(files);
        AssertThat(conflict.File == "overlay/test.d/02.json").IsTrue();
        var options = new WorldLoadOptions { ClaimedBy = new Dictionary<FeatureId, WpId> { [new("fixture:relation:1")] = new("WP-14") } };
        AssertThat(WorldModelLoader.Load(new("test"), files, options).World.Features[new("fixture:relation:1")].Height == 30).OverrideFailureMessage("Different shard owners did not respect a real claim.").IsTrue();
        files.Remove("overlay/test.d/02.json");
        files["overlay/other.d/01.json"] = Overlay([new HeightOp(new("osm:way:10"), 30, null, "same owner across groups")], "WP-14");
        _ = OverlayError(files);

    }

    [TestCase]
    public void SplitOverlaysReadOnlyDirectJsonChildrenAndLeaveDedicatedFormatsAlone()
    {
        var files = new Dictionary<string, string> { ["campus.json"] = Campus, ["overlay/test.d/01.json"] = Overlay([]) };
        foreach (var path in new[] { "overlay/test.d/nested/02.json", "overlay/test.d/04.txt",
            "overlay/test.d/05.JSON", "overlay/terrain.d/01.json", "overlay/interior.d/01.json", "overlay/outskirts.d/01.json",
            "overlay/walkable.d/01.json", "overlay/fidelity.d/01.json", "overlay/terrain/test.d/01.json", "overlay/interior/test.d/01.json",
            "overlay/terrain/test.overlay.json", "overlay/outskirts.overlay.json", "overlay/walkable.overlay.json", "overlay/fidelity.overlay.json" })
            files[path] = "not a feature overlay";
        files["overlay/interior/test.interior.json"] = """{"zone":"test","owner":"WP-08","ops":[]}""";
        var world = WorldModelLoader.Load(new("test"), files).World;
        AssertThat(world.Features.Count == Load().World.Features.Count && ((WorldModelSnapshot)world).InteriorOverlays.Count == 1).OverrideFailureMessage("Feature splitting consumed a dedicated format or nested/non-JSON file.").IsTrue();
    }

    private static string Overlay(IReadOnlyList<OverlayOp> operations, string owner = "WP-08", string map = "test") =>
        JsonSerializer.Serialize(new OverlayFile(new("test"), new(owner), operations) { Map = new(map) }, ContractJson.Options);

    private static WorldDataException OverlayError(IReadOnlyDictionary<string, string> files)
    {
        try { WorldModelLoader.Load(new("test"), files); }
        catch (WorldDataException error) { return error; }
        throw new InvalidOperationException("Invalid feature overlay was accepted.");
    }

    private static WorldModelLoadResult Load(string? overlay = null)
    {
        var files = new Dictionary<string, string> { ["campus.json"] = Campus };
        if (overlay is not null) files["overlay/test.d/0000-fixture.json"] = overlay;
        return WorldModelLoader.Load(new("test"), files);
    }
}
