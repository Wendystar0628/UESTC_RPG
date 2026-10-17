using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.World.Tests;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

/// <summary>
/// The ground lays centre lines from the effective road network only (WP-08 batch 4): an authored footway that is in the
/// feature table and in <c>Network.Roads</c> is paved once, a hidden one is gone, an area stays an area, and the markings
/// and kerbs follow the same lines.
/// </summary>
[TestSuite]
public sealed class GroundEffectiveRoadsTests
{
    private static readonly GroundSection Section = GroundSection.Default;

    [TestCase]
    public void AuthoredLineInFeaturesAndRoadsIsPavedOnce()
    {
        var points = new Vector2[] { new(0f, 20f), new(30f, 24f), new(60f, 20f) };
        var both = TestWorld.Of([Line("design:t-walk", points, 3f)], new WorldNetwork([Road("design:t-walk", points, 3f)], [], [], [], []));
        var (shapes, roads) = GroundSources.Read(both);
        Check.True(roads.Count == 1, $"One centre line, got {roads.Count}.");
        var mine = shapes.Where(s => s.Id.Value == "design:t-walk").ToArray();
        Check.True(mine.Length == 1, $"The line is one capsule chain, got {mine.Length} shapes.");
        Check.True(mine[0].Kind == SurfaceKind.Walk && mine[0].Line!.SequenceEqual(points) && mine[0].HalfWidth == 1.5f, "A 3 m footway along its authored points.");

        // The network alone gives the same shapes: the feature copy adds nothing.
        var networkOnly = GroundSources.Read(TestWorld.Of([], new WorldNetwork([Road("design:t-walk", points, 3f)], [], [], [], []))).Shapes;
        Check.True(networkOnly.Count == shapes.Count && networkOnly.Zip(shapes).All(p => p.First.Id == p.Second.Id && p.First.Kind == p.Second.Kind &&
            p.First.HalfWidth == p.Second.HalfWidth && p.First.Line!.SequenceEqual(p.Second.Line!)), "Features copy changed the shapes.");
    }

    [TestCase]
    public void HiddenAuthoredLineIsNotLaidAgain()
    {
        // A feature of a class the network owns, with no road under its ID, has been hidden: nothing is paved.
        foreach (var type in new[] { "footway", "path", "pedestrian" })
        {
            var stale = TestWorld.Of([Line("design:t-" + type, [new(0f, 0f), new(40f, 0f)], 3f, type)]);
            var (shapes, roads) = GroundSources.Read(stale);
            Check.True(shapes.Count == 0 && roads.Count == 0, $"A {type} line missing from the network was laid anyway.");
        }
        // A class the network does not take is still laid from its feature, and so is an original way.
        var service = TestWorld.Of([Line("design:t-service", [new(0f, 0f), new(40f, 0f)], 5f, "service")]);
        Check.True(GroundSources.Read(service).Shapes.Single().Kind == SurfaceKind.Carriageway, "A service line outside the network keeps its surface.");
        var original = Line("osm:way:9", [new(0f, 0f), new(40f, 0f)], 3f, "footway") with { Source = FeatureSource.Osm };
        Check.True(GroundSources.Read(TestWorld.Of([original])).Shapes.Count == 1, "An original way is not an authored line.");
    }

    [TestCase]
    public void HideThroughTheLoaderLeavesNoShape()
    {
        var added = GroundSources.Read(Load([Add("design:line")]).World);
        Check.True(added.Shapes.Count(s => s.Id.Value == "design:line") == 1 && added.Roads.Count(r => r.Id.Value == "design:line") == 1,
            "The authored line is paved once from the loaded world.");
        var hidden = GroundSources.Read(Load([Add("design:line"), new HideOp(new("design:line"), "remove the line")]).World);
        Check.True(hidden.Shapes.All(s => s.Id.Value != "design:line") && hidden.Roads.All(r => r.Id.Value != "design:line"),
            "A hidden authored line is not paved from anywhere.");
    }

    [TestCase]
    public void AreasStayAreasAndKeepTheirHoles()
    {
        var plaza = Rect(0f, 0f, 40f, 40f);
        var holed = new PolygonRings(plaza.Outer, [Rect(15f, 15f, 25f, 25f).Outer]);
        var second = new PolygonRings(Rect(60f, 0f, 80f, 20f).Outer, [Rect(66f, 6f, 74f, 14f).Outer]);
        var world = TestWorld.Of(
        [
            Area("design:t-plaza", new PolygonGeometry(holed), "pedestrian"),
            Area("design:t-plaza-pair", new MultiPolygonGeometry([holed with { Outer = Shift(holed.Outer, 100f), Holes = [Shift(holed.Holes[0], 100f)] }, second]), "pedestrian"),
        ]);
        var (shapes, roads) = GroundSources.Read(world);
        Check.True(roads.Count == 0, "An area is no centre line.");
        Check.True(shapes.Count == 3 && shapes.All(s => s.Kind == SurfaceKind.Plaza && s.Line is null && s.Rings!.Length == 2), "Three plaza polygons, each with its hole.");
        var layout = GroundLayout.Of(world);
        Check.True(layout.SignedDistance(SurfaceKind.Plaza, new(20f, 20f), 20f) > 0f, "The hole of a polygon is open ground.");
        Check.True(layout.SignedDistance(SurfaceKind.Plaza, new(5f, 20f), 20f) < 0f, "The body of a polygon is plaza.");
        Check.True(layout.SignedDistance(SurfaceKind.Plaza, new(120f, 20f), 20f) > 0f && layout.SignedDistance(SurfaceKind.Plaza, new(70f, 10f), 20f) > 0f,
            "The holes of a multi-polygon are open ground too.");

        // An original way is a polygon in the feature table and a centre line in the network under one ID: both are laid.
        var polygon = Area("osm:way:5", new PolygonGeometry(plaza), "footway") with { Source = FeatureSource.Osm };
        var both = TestWorld.Of([polygon], new WorldNetwork([Road("osm:way:5", [new(0f, 20f), new(40f, 20f)], 2.5f)], [], [], [], []));
        var laid = GroundSources.Read(both).Shapes;
        Check.True(laid.Count == 2 && laid.Count(s => s.Rings is not null) == 1 && laid.Count(s => s.Line is not null) == 1, "Polygon as polygon, centre line as capsule.");
    }

    [TestCase]
    public void AuthoredWalkJoinsAnExistingSidewalkAsOneSurfaceWithNoKerbAtTheJoin()
    {
        var walk = new Vector2[] { new(30f, 4.25f), new(30f, 40f) };
        var street = new List<WorldFeature>
        {
            Area("osm:way:1", new PolygonGeometry(Rect(-100f, -3f, 100f, 3f)), "residential") with { Source = FeatureSource.Osm },
            Area("osm:way:2", new PolygonGeometry(Rect(-100f, 3f, 100f, 5.5f)), "footway") with { Source = FeatureSource.Osm },
        };
        var network = new List<NetworkLine> { Road("osm:way:1", [new(-100f, 0f), new(100f, 0f)], 6f, "residential") };
        var bare = TestWorld.Of(street, new WorldNetwork(network, [], [], [], []));
        var withWalk = TestWorld.Of([.. street, Line("design:t-walk", walk, 2.5f)], new WorldNetwork([.. network, Road("design:t-walk", walk, 2.5f)], [], [], [], []));
        var networkOnly = TestWorld.Of(street, new WorldNetwork([.. network, Road("design:t-walk", walk, 2.5f)], [], [], [], []));

        // The walk and the sidewalk are one paved surface: no kerb, edging or step across the join, and none beside it.
        for (var z = 5.0f; z <= 30f; z += 0.5f)
            foreach (var x in new[] { 29.3f, 30f, 30.7f })
                Check.True(SurfaceAt(withWalk, x, z) == GroundSurface.Paver, $"({x}, {z}) is {SurfaceAt(withWalk, x, z)}, not plain paving.");
        Check.True(SurfaceAt(bare, 30f, 20f) != GroundSurface.Paver, "Without the walk that ground is lawn.");

        // The carriageway has no fillet or kerb of its own from a footway, and the footway's crossing sits where it meets the road.
        Check.True(RoadGraph.Fillets(GroundSources.Read(withWalk).Roads).Count == 0, "A footway never rounds a carriageway corner.");
        var crossing = new Vector2[] { new(50f, -20f), new(50f, 20f) };
        var crossed = TestWorld.Of(street, new WorldNetwork([.. network, Road("design:t-cross", crossing, 2.5f)], [], [], [], []));
        var marks = GroundLayout.Of(crossed).Markings(Section);
        Check.True(marks.Crossings.Count == 1 && Vector2.Distance(marks.Crossings[0], new(50f, 0f)) < 0.01f, "An authored footway across the road gets one zebra where it crosses.");
        Check.True(GroundLayout.Of(bare).Markings(Section).Crossings.Count == 0, "No walk, no crossing.");

        // Feature and network copies together build the same tile as the network alone: no second layer, no z conflict.
        var a = new GroundModule().Build(TestWorld.Context(withWalk), new TileKey(0, 0), LodLevel.L0);
        var b = new GroundModule().Build(TestWorld.Context(networkOnly), new TileKey(0, 0), LodLevel.L0);
        Check.True(a.Meshes.Count == b.Meshes.Count && a.Meshes.Count > 0, "Bundle count differs.");
        for (var k = 0; k < a.Meshes.Count; k++)
        {
            Check.True(a.Meshes[k].Position.SequenceEqual(b.Meshes[k].Position) && a.Meshes[k].Index.SequenceEqual(b.Meshes[k].Index), $"Mesh {k} differs.");
            a.Meshes[k].ValidateVertexData();
        }
    }

    [TestCase]
    public void RealCampusLaysEveryAuthoredLineOnceAndKeepsItsAreasAndHoles()
    {
        var world = EffectiveRoadNetworkRealCampusTests.LoadPrepared(EffectiveRoadNetworkRealCampusTests.ReadFiles()).World;
        var (shapes, roads) = GroundSources.Read(world);
        // Only the classes WP-08 projects into the network (footway, path, pedestrian); a design drive or service lane is laid from its own feature.
        var authored = world.Features.Values.Where(f => f.Source == FeatureSource.Design && f.Kind.Value == "road" && f.Geometry is PolylineGeometry &&
            GroundClassifier.RoadType(f.Props) is "footway" or "path" or "pedestrian").ToArray();
        Check.True(authored.Length >= 7, $"The campus has at least seven authored footway lines, got {authored.Length}.");
        foreach (var feature in authored)
        {
            var road = world.Network.Roads.SingleOrDefault(r => r.Id == feature.Id);
            Check.True(road is not null, $"{feature.Id.Value} is missing from the effective network.");
            var laid = shapes.Where(s => s.Id == feature.Id).ToArray();
            Check.True(laid.Length == 1 && laid[0].Kind == SurfaceKind.Walk && laid[0].Line is not null, $"{feature.Id.Value} must be laid once as a footway, got {laid.Length}.");
            Check.True(laid[0].Line!.SequenceEqual(GroundSources.Clean(road!.Line.Points)) && laid[0].HalfWidth == road.Width * 0.5f, $"{feature.Id.Value} is not laid along its network line.");
        }
        // No centre line is laid twice anywhere: every network road has exactly one capsule chain, and no other line shape shares its ID.
        var chains = shapes.Where(s => s.Line is not null && s.Kind is SurfaceKind.Carriageway or SurfaceKind.Walk or SurfaceKind.Plaza && !s.Id.Value.Contains(":footprint:", StringComparison.Ordinal)).ToArray();
        Check.True(chains.GroupBy(s => s.Id).All(g => g.Count() == 1), "A road ID is laid as more than one centre line.");
        // An authored line of a class the network does not take (a design drive) is laid from its own feature, once.
        var outside = world.Features.Values.Count(f => f.Source == FeatureSource.Design && f.Kind.Value == "road" && f.Geometry is PolylineGeometry &&
            world.Network.Roads.All(r => r.Id != f.Id));
        Check.True(chains.Length == roads.Count + outside, $"{roads.Count} network roads and {outside} lines outside it give {chains.Length} line shapes.");
        // The authored pedestrian areas stay polygons.
        foreach (var id in new[] { "design:south-clock-forecourt", "design:south-a01-plaza" })
        {
            var area = shapes.Single(s => s.Id.Value == id);
            Check.True(area.Kind == SurfaceKind.Plaza && area.Rings is not null && area.Line is null, $"{id} must stay a plaza polygon.");
        }
        // The ring-shaped athletics relation names no sport: turf, with its infield hole, and no court.
        var field = shapes.Where(s => s.Id.Value == "osm:relation:11583700" || s.Id.Value == "qsh:relation:11583700").ToArray();
        Check.True(field.Length == 1 && field[0].Kind == SurfaceKind.Turf && field[0].Rings is { Length: 2 }, "The athletics field is one turf ring with a hole.");
    }

    // ---------------------------------------------------------------- helpers

    private static GroundSurface SurfaceAt(IWorldModel world, float x, float z)
    {
        var layout = GroundLayout.Of(world);
        var tx = (int)MathF.Floor(x / GroundLod.TileSize);
        var tz = (int)MathF.Floor(z / GroundLod.TileSize);
        var tile = new GroundTile(tx * GroundLod.TileSize, tz * GroundLod.TileSize, GroundLod.TileSize, GroundLod.TileSize / 256f);
        var grid = TerrainGrid.For(FlatTerrain.Instance, new TileKey(tx, tz), LodLevel.L0, tile);
        var field = TileField.Build(layout, tile, Section, grid);
        var mesher = new GroundMesher(field, Section, LodLevel.L0, layout, grid, new MeshSink());
        return mesher.SurfaceAt((int)MathF.Round((x - tile.X0) / tile.Cell), (int)MathF.Round((z - tile.Z0) / tile.Cell));
    }

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static string RoadProps(string type, float? width) =>
        width is { } w ? $$"""{"roadType":"{{type}}","width":{{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}""" : $$"""{"roadType":"{{type}}"}""";

    /// <summary>An authored (design) road drawn as a line, as the loader keeps it in the feature table.</summary>
    private static WorldFeature Line(string id, Vector2[] points, float width, string type = "footway") =>
        new(new FeatureId(id), FeatureSource.Design, new DesignKind("road"), new PolylineGeometry(new Polyline(points)))
        {
            Props = Json(RoadProps(type, width)),
            Bounds = new Box3(new(points.Min(p => p.X), 0f, points.Min(p => p.Y)), new(points.Max(p => p.X), 0f, points.Max(p => p.Y)))
        };

    /// <summary>A road area: a polygon or multi-polygon feature with a road type.</summary>
    private static WorldFeature Area(string id, OverlayGeometry geometry, string type) =>
        new(new FeatureId(id), FeatureSource.Design, new DesignKind("road"), geometry) { Props = Json(RoadProps(type, null)) };

    private static NetworkLine Road(string id, Vector2[] points, float width, string type = "footway") =>
        new(new FeatureId(id), new Polyline(points), width) { Props = Json($$"""{"highway":"{{type}}"}""") };

    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static IReadOnlyList<Vector2> Shift(IReadOnlyList<Vector2> ring, float dx) => ring.Select(p => new Vector2(p.X + dx, p.Y)).ToArray();

    // The loader fixture of the road tests: one original road polygon, a network with its centre line, and overlay operations.
    private const string Campus = """
        {"meta":{"origin":[103,30],"crs":"EPSG:32648","bounds":[-20,-20,20,20],"entityPrefix":"campus-test"},
         "boundary":[[[-20,-20],[20,-20],[20,20],[-20,20]]],"entities":[],"features":[
           {"id":"osm:way:10","entityId":null,"kind":"road","height":0,"polygons":[[[[-10,-10],[-2,-10],[-2,-2],[-10,-2]]]]}
         ]}
        """;

    private static AddOp Add(string id) => new(new(id), new("road"), new PolylineGeometry(new Polyline([new(-5f, 4f), new(3f, 6f), new(1f, 8f)])), null,
        Json("""{"roadType":"footway","width":2.5}"""), "ground regression");

    private static WorldModelLoadResult Load(IReadOnlyList<OverlayOp> ops)
    {
        var line = new NetworkLine(new("campus-test:source:way:10"), new Polyline([new(-10f, -6f), new(-2f, -6f)]), 2f) { Props = Json("""{"highway":"footway"}""") };
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["campus.json"] = Campus,
            ["network.json"] = JsonSerializer.Serialize(new WorldNetwork([line], [], [], [], []), ContractJson.Options),
            ["overlay/a.d/0000-fixture.json"] = JsonSerializer.Serialize(new OverlayFile(new("test"), new("WP-08"), ops), ContractJson.Options)
        };
        return WorldModelLoader.Load(new("test"), files);
    }
}
