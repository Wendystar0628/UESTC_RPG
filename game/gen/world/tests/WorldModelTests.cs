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
    private const string Geo = """
        {"origin":[103,30],"crs":"EPSG:32648","bounds":[-20,-20,20,20],"entityPrefix":"fixture",
         "boundary":{"outer":[[-20,-20],[20,-20],[20,20],[-20,20]],"holes":[]}}
        """;
    private const string Features = """
        {"features":[
          {"id":"fixture:relation:1","name":"Building","kind":"building","parts":[
            {"id":"osm:way:10","height":8,"storeys":2,
             "geometry":{"type":"polygon","rings":{"outer":[[-10,-10],[-2,-10],[-2,-2],[-10,-2]],"holes":[]}}},
            {"id":"osm:way:11","height":16,"storeys":4,
             "geometry":{"type":"polygon","rings":{"outer":[[2,2],[10,2],[10,10],[2,10]],"holes":[]}}}]},
          {"id":"fixture:source:way:20","kind":"water",
           "geometry":{"type":"polygon","rings":{"outer":[[-10,2],[-2,2],[-2,10],[-10,10]],"holes":[[[-8,4],[-4,4],[-4,8],[-8,8]]]}}}
        ],"trees":[]}
        """;

    [TestCase, TestCategory(TestReason.Contract)]
    public void ExplicitPointAndWidthlessLinePrecisionSurvivesFreezingWithoutPromotingGround()
    {
        var path = new Polyline([new(-10, 1), new(10, 1)]);
        WorldFeature[] features =
        [
            new(new("fixture:point"), new("bridge-deck"), new PointGeometry(new(-7, 0, -7)))
                { Props = JsonSerializer.SerializeToElement(new { fidelity = "K1", axis = "fixture:wide", walks = new[] { "fixture:wide" } }) },
            new(new("fixture:line"), new("road"), new PolylineGeometry(path))
                { Props = JsonSerializer.SerializeToElement(new { fidelity = "K1" }) },
            new(new("fixture:area"), new("plaza"), new PolygonGeometry(new PolygonRings([new(12, 12), new(18, 12), new(18, 18), new(12, 18)])))
                { Props = JsonSerializer.SerializeToElement(new { fidelity = "K2" }) }
        ];
        var world = new Campus.Gen.Tests.Support.FakeWorld
        {
            Boundary = new([new(-20, -20), new(20, -20), new(20, 20), new(-20, 20)]),
            Features = features.ToDictionary(feature => feature.Id),
            Network = new([new(new("fixture:line"), path) { Props = features[1].Props },
                new(new("fixture:wide"), new([new(-10, -7), new(10, -7)]), 4)], [], [], [], [])
        };
        var frozen = WorldFidelityCompiler.Compile(world);
        var restored = JsonSerializer.Deserialize<FidelityFrozenData>(JsonSerializer.Serialize(frozen, ContractJson.Options), ContractJson.Options)!;
        var fidelity = new WorldFidelityModel(restored);
        AssertThat(fidelity.Declared(features[0].Id) == FidelityZone.K1 && fidelity.Declared(features[1].Id) == FidelityZone.K1
            && fidelity.Declared(features[2].Id) == FidelityZone.K2 && fidelity.Declared(new("fixture:wide")) is null)
            .OverrideFailureMessage("Explicit feature marks were lost or inferred from bridge references.").IsTrue();
        AssertThat(fidelity.GroundAt(1, 1) == FidelityZone.K3 && fidelity.GroundAt(-7, -7) == FidelityZone.K3
            && fidelity.GroundAt(15, 15) == FidelityZone.K2)
            .OverrideFailureMessage("A point/widthless line promoted ground, or a marked polygon stopped promoting its area.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ExplicitWideRoadPrecisionRemainsConfinedToItsAuthoredWidth()
    {
        var path = new Polyline([new(-10, 1), new(10, 1)]);
        var feature = new WorldFeature(new("fixture:road"), new("road"), new PolylineGeometry(path))
            { Props = JsonSerializer.SerializeToElement(new { fidelity = "K1" }) };
        var world = new Campus.Gen.Tests.Support.FakeWorld
        {
            Boundary = new([new(-20, -20), new(20, -20), new(20, 20), new(-20, 20)]),
            Features = new Dictionary<FeatureId, WorldFeature> { [feature.Id] = feature },
            Network = new([new(feature.Id, path, 4) { Props = feature.Props }], [], [], [], [])
        };
        var fidelity = new WorldFidelityModel(WorldFidelityCompiler.Compile(world));
        AssertThat(fidelity.Declared(feature.Id) == FidelityZone.K1 && fidelity.GroundAt(1, 1) == FidelityZone.K1
            && fidelity.GroundAt(1, 5) == FidelityZone.K3)
            .OverrideFailureMessage("A wide road lost its explicit precision or promoted ground outside its authored width.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FacadeFootprintsUseTheSameRingConversionAndKeepPhysicalGeometry()
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(Features)!;
        var first = root["features"]![0]!["parts"]![0]!;
        first["facadePolygons"] = System.Text.Json.Nodes.JsonNode.Parse("""[{"outer":[[-10,-10],[-2,-10],[-2,-2],[-10,-2]],"holes":[]}]""");
        first["geometry"]!["rings"]!["outer"] = System.Text.Json.Nodes.JsonNode.Parse("""[[-10,-10],[-6,-10.02],[-2,-10],[-2,-2],[-10,-2]]""");
        var world = WorldModelLoader.Load(new("test"), Files(root.ToJsonString())).World;
        var feature = world.Features[new("fixture:relation:1")];
        var part = feature.Parts[0];
        AssertThat(((PolygonGeometry)part.Geometry).Rings.Outer.Count == 5).OverrideFailureMessage("Physical source detail was simplified.").IsTrue();
        var facade = ((PolygonGeometry)WorldFeatureGeometry.FacadeGeometryOf(part)).Rings;
        AssertThat(facade.Outer.SequenceEqual(new Vector2[] { new(-10, -10), new(-2, -10), new(-2, -2), new(-10, -2) })).OverrideFailureMessage("Facade coordinate order or conversion changed.").IsTrue();
        AssertThat(ReferenceEquals(WorldFeatureGeometry.FacadeGeometryOf(feature.Parts[1]), feature.Parts[1].Geometry)).OverrideFailureMessage("Missing facade did not fall back to current physical geometry.").IsTrue();
        AssertThat(WorldFeatureGeometry.FacadePolygonsOf(feature).SelectMany(p => p.Outer).SequenceEqual(facade.Outer.Concat(((PolygonGeometry)feature.Parts[1].Geometry).Rings.Outer))).OverrideFailureMessage("Current part order changed.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FacadeFootprintsRejectPresentEmptyMalformedAndMismatchedValues()
    {
        foreach (var value in new[] { "null", "{}", "[]", "[[]]", "[[[[0,0],[1,1],[2,2]]]]", "[[[[0],[1,0],[0,1]]]]", "[[[[0,0],[1,0],[0,1]]],[[[2,2],[3,2],[2,3]]]]", "[[[[0,0],[4,0],[0,4]],[[1,1],[2,1],[1,2]]]]" })
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Features)!;
            root["features"]![0]!["parts"]![0]!["facadePolygons"] = System.Text.Json.Nodes.JsonNode.Parse(value);
            try
            {
                WorldModelLoader.Load(new("test"), Files(root.ToJsonString()));
                throw new InvalidOperationException("Malformed facade footprint accepted: " + value);
            }
            catch (WorldDataException error)
            { AssertThat(error.File == "world/test.json").OverrideFailureMessage("Lost source facade error context.").IsTrue(); }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FacadeAggregationPreservesPolygonHoleAndPartOrderWithoutNormalization()
    {
        var first = new PolygonRings([new(3, 3), new(3, 0), new(0, 0), new(0, 3)], [new Vector2[] { new(1, 1), new(2, 1), new(1, 2) }]);
        var second = new PolygonRings([new(6, 6), new(6, 4), new(4, 4), new(4, 6)]);
        var physical = new PointGeometry(Vector3.Zero);
        var part = new WorldFeaturePart("second-source", physical, null, null) { FacadeGeometry = new MultiPolygonGeometry([first, second]) };
        var feature = new WorldFeature(new("fixture"), new("building"), physical)
        { Parts = [part, new("first-source", new PolygonGeometry(second), null, null)] };
        var polygons = WorldFeatureGeometry.FacadePolygonsOf(feature);
        AssertThat(polygons.Count == 3 && ReferenceEquals(polygons[0], first) && ReferenceEquals(polygons[1], second) && ReferenceEquals(polygons[2], second)).OverrideFailureMessage("Facade aggregation reordered or rebuilt rings.").IsTrue();
        AssertThat(PlanGeometry.SignedArea(polygons[0].Outer) < 0 && ReferenceEquals(polygons[0].Holes, first.Holes)).OverrideFailureMessage("Facade helper normalized input direction or holes.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void HolesRemainDryAndBoundaryQueriesAreExact()
    {
        var spatial = Load().World.Spatial;
        AssertThat(spatial.WalkClassAt(-6, 6) == WalkClass.Walk).OverrideFailureMessage("Water polygon hole filled.").IsTrue();
        AssertThat(spatial.WalkClassAt(-9, 3) == WalkClass.Water).OverrideFailureMessage("Water missing.").IsTrue();
        AssertThat(spatial.MediumAt(new(-9, -1, 3)).Kind == MediumKind.Underwater).OverrideFailureMessage("Underwater classification missing.").IsTrue();
        AssertThat(spatial.WalkClassAt(21, 0) == WalkClass.Outside).OverrideFailureMessage("Exterior became walkable.").IsTrue();
        AssertThat(spatial.WalkClassAt(-5, -5) == WalkClass.Walk).OverrideFailureMessage("A building projection blocked its physical openings.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RingNormalizationKeepsOrientationAndRejectsDegenerateInput()
    {
        var ring = PlanGeometry.OpenRing([new(0, 0), new(0, 2), new(2, 2), new(2, 0), new(0, 0)]);
        AssertThat(ring.Count == 4 && PlanGeometry.SignedArea(ring) == 4).OverrideFailureMessage("Ring normalization failed.").IsTrue();
        AssertThat(PlanGeometry.PointInRing(new(0, 1), ring)).OverrideFailureMessage("Boundary point excluded.").IsTrue();
        try { PlanGeometry.OpenRing([new(0, 0), new(1, 1), new(2, 2)]); throw new InvalidOperationException("Degenerate ring accepted."); }
        catch (ArgumentException) { }
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    [TestCategory("Slow")]
    public void RealMigratedCampusLoadsEverySourcePartOnce()
    {
        var files = RealMapFixture.ReadTextFiles();
        var result = RealMapFixture.Load();
        var records = files.Where(file => file.Key.StartsWith("world/", StringComparison.Ordinal) && file.Key != "world/geo.json" && !file.Key.EndsWith(".terrain.json", StringComparison.Ordinal))
            .SelectMany(file =>
            {
                using var document = JsonDocument.Parse(file.Value);
                return document.RootElement.GetProperty("features").EnumerateArray().Select(record => record.Clone()).ToArray();
            }).ToArray();
        AssertThat(records.Select(record => record.GetProperty("id").GetString()).Order(StringComparer.Ordinal)
            .SequenceEqual(result.World.Features.Keys.Select(id => id.Value).Order(StringComparer.Ordinal)))
            .OverrideFailureMessage("World records were lost or duplicated.").IsTrue();
        var expectedParts = records.SelectMany(record => record.TryGetProperty("parts", out var parts) ? parts.EnumerateArray() : Enumerable.Empty<JsonElement>())
            .Select(part => part.GetProperty("id").GetString()).Order(StringComparer.Ordinal);
        AssertThat(expectedParts.SequenceEqual(result.World.Features.Values.SelectMany(feature => feature.Parts).Select(part => part.SourceId).Order(StringComparer.Ordinal)))
            .OverrideFailureMessage("World parts were lost or duplicated.").IsTrue();
        var interiors = files["interiors.json"];
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
        foreach (var interior in authoredInteriors.RootElement.GetProperty("interiors").EnumerateArray())
        foreach (var feature in interior.GetProperty("featureIds").EnumerateArray())
            AssertThat(result.World.Interiors.Get(new(feature.GetString()!))!.OpenStoreys.SequenceEqual(
                interior.GetProperty("openFloors").EnumerateArray().Select(floor => floor.GetInt32()).Order())).OverrideFailureMessage("The interior index lost the authoritative open floor range.").IsTrue();
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

    [TestCase, TestCategory(TestReason.Contract)]
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

    [TestCase, TestCategory(TestReason.Contract)]
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
            ["world/geo.json"] = Geo,
            ["world/test.json"] = Features,
            ["interiors.json"] = "{\"version\":1,\"interiors\":[" + open + "],\"closedEntrances\":[" + record + "]}"
        });

    [TestCase, TestCategory(TestReason.Regression)]
    [TestCategory("Slow")]
    public void ArchitectureReviewMixedParentsKeepBuildingPartsAndOpenParkDistinct()
    {
        var result = RealMapFixture.Load();
        var samples = new[]
        {
            (Id: result.World.Features.Values.Single(feature => feature.Name == "电子科技大学立人托幼公园").Id, Point: new Vector3(-672.712f, 1, -106.672f)),
            (Id: new FeatureId(global::Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastIds.Wenke.Entity), Point: new Vector3(375.455f, 1, 80.05f))
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

    [TestCase, TestCategory(TestReason.Regression)]
    [TestCategory("Slow")]
    public void ArchitectureReviewBoundsUseAuthoredRoamLimits()
    {
        var files = new Dictionary<string, string>
        {
            ["world/geo.json"] = Geo,
            ["world/test.json"] = Features,
            ["map.json"] = """{"id":"test","roam":{"heightFactor":0.9,"heightStart":30,"maxRadius":3000,"floorY":-40,"ceilY":1500,"buffer":30,"softFloorClearance":40,"fallbackMargin":25}}"""
        };
        var result = WorldModelLoader.Load(new("test"), files);
        AssertThat(result.Bounds == new Box3(new(-45, -40, -45), new(45, 1500, 45))).OverrideFailureMessage($"Authored roam bounds ignored: {result.Bounds}.").IsTrue();
        var real = RealMapFixture.Load();
        var roam = RealMapFixture.Entry().Descriptor.Roam;
        AssertThat(real.Bounds.Min.Y == roam.FloorY && real.Bounds.Max.Y == roam.CeilY).OverrideFailureMessage("Real campus vertical roam bounds were replaced with building heights.").IsTrue();
        AssertThat(Math.Abs(real.Bounds.Min.X - (real.Boundary.Outer.Min(p => p.X) - roam.FallbackMargin)) < TestTolerance.Position).OverrideFailureMessage("Real campus fallback margin missing.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RetiredFeatureOverlayFilesAreRejectedWithoutChangingPublishedWorld()
    {
        var previous = Load().World;
        var files = Files();
        files["overlay/test.d/0000-fixture.json"] = "{\"ops\":[]}";
        AssertThrown(() => WorldModelLoader.Load(new("test"), files)).IsInstanceOf<WorldDataException>();
        AssertThat(previous.Features.Keys.SequenceEqual(Load().World.Features.Keys)).IsTrue();
    }

    private static Dictionary<string, string> Files(string features = Features) => new()
    {
        ["world/geo.json"] = Geo,
        ["world/test.json"] = features
    };

    private static WorldModelLoadResult Load() => WorldModelLoader.Load(new("test"), Files());
}
