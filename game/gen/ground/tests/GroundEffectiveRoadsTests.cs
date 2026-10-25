using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

/// <summary>
/// The ground lays centre lines from the effective road network only (game/gen/world/ batch 4): a footway that is in the
/// feature table and in <c>Network.Roads</c> is paved once, an area stays an area, and the markings
/// and kerbs follow the same lines.
/// </summary>
[TestSuite]
public sealed class GroundEffectiveRoadsTests
{
    private static readonly GroundSection Section = GroundSection.Default;

    [TestCase, TestCategory(TestReason.Contract)]
    public void AuthoredLineInFeaturesAndRoadsIsPavedOnce()
    {
        var points = new Vector2[] { new(0f, 20f), new(30f, 24f), new(60f, 20f) };
        var both = GroundTestSupport.Of([WorldFeatureFixture.Create("design:t-walk", "road", new PolylineGeometry(new Polyline(points)), RoadProps("footway", 3f))], new WorldNetwork([Road("design:t-walk", points, 3f)], [], [], [], []));
        var (shapes, roads) = GroundSources.Read(both);
        AssertThat(roads.Count == 1).OverrideFailureMessage($"One centre line, got {roads.Count}.").IsTrue();
        var mine = shapes.Where(s => s.Id.Value == "design:t-walk").ToArray();
        AssertThat(mine.Length == 1).OverrideFailureMessage($"The line is one capsule chain, got {mine.Length} shapes.").IsTrue();
        AssertThat(mine[0].Kind == SurfaceKind.Walk && mine[0].Line!.SequenceEqual(points) && mine[0].HalfWidth == 1.5f).OverrideFailureMessage("A 3 m footway along its authored points.").IsTrue();

        // The network alone gives the same shapes: the feature copy adds nothing.
        var networkOnly = GroundSources.Read(GroundTestSupport.Of([], new WorldNetwork([Road("design:t-walk", points, 3f)], [], [], [], []))).Shapes;
        AssertThat(networkOnly.Count == shapes.Count && networkOnly.Zip(shapes).All(p => p.First.Id == p.Second.Id && p.First.Kind == p.Second.Kind &&
            p.First.HalfWidth == p.Second.HalfWidth && p.First.Line!.SequenceEqual(p.Second.Line!))).OverrideFailureMessage("Features copy changed the shapes.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void LinesOutsideTheNetworkKeepTheirFeatureSurface()
    {
        foreach (var type in new[] { "footway", "path", "pedestrian", "service" })
        {
            var feature = WorldFeatureFixture.Create("fixture:" + type, "road",
                new PolylineGeometry(new Polyline([new(0f, 0f), new(40f, 0f)])), RoadProps(type, 3f));
            var (shapes, roads) = GroundSources.Read(GroundTestSupport.Of([feature]));
            AssertThat(shapes.Count == 1 && roads.Count == 0).OverrideFailureMessage("A feature line outside the network keeps its surface.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AreasStayAreasAndKeepTheirHoles()
    {
        var plaza = Rect(0f, 0f, 40f, 40f);
        var holed = new PolygonRings(plaza.Outer, [Rect(15f, 15f, 25f, 25f).Outer]);
        var second = new PolygonRings(Rect(60f, 0f, 80f, 20f).Outer, [Rect(66f, 6f, 74f, 14f).Outer]);
        var world = GroundTestSupport.Of(
        [
            WorldFeatureFixture.Create("design:t-plaza", "road", new PolygonGeometry(holed), RoadProps("pedestrian", null)),
            WorldFeatureFixture.Create("design:t-plaza-pair", "road", new MultiPolygonGeometry([holed with { Outer = Shift(holed.Outer, 100f), Holes = [Shift(holed.Holes[0], 100f)] }, second]), RoadProps("pedestrian", null)),
        ]);
        var (shapes, roads) = GroundSources.Read(world);
        AssertThat(roads.Count == 0).OverrideFailureMessage("An area is no centre line.").IsTrue();
        AssertThat(shapes.Count == 3 && shapes.All(s => s.Kind == SurfaceKind.Plaza && s.Line is null && s.Rings!.Length == 2)).OverrideFailureMessage("Three plaza polygons, each with its hole.").IsTrue();
        var layout = GroundLayout.Of(world);
        AssertThat(layout.SignedDistance(SurfaceKind.Plaza, new(20f, 20f), 20f) > 0f).OverrideFailureMessage("The hole of a polygon is open ground.").IsTrue();
        AssertThat(layout.SignedDistance(SurfaceKind.Plaza, new(5f, 20f), 20f) < 0f).OverrideFailureMessage("The body of a polygon is plaza.").IsTrue();
        AssertThat(layout.SignedDistance(SurfaceKind.Plaza, new(120f, 20f), 20f) > 0f && layout.SignedDistance(SurfaceKind.Plaza, new(70f, 10f), 20f) > 0f).OverrideFailureMessage("The holes of a multi-polygon are open ground too.").IsTrue();

        // An original way is a polygon in the feature table and a centre line in the network under one ID: both are laid.
        var polygon = WorldFeatureFixture.Create("osm:way:5", "road", new PolygonGeometry(plaza), RoadProps("footway", null));
        var both = GroundTestSupport.Of([polygon], new WorldNetwork([Road("osm:way:5", [new(0f, 20f), new(40f, 20f)], 2.5f)], [], [], [], []));
        var laid = GroundSources.Read(both).Shapes;
        AssertThat(laid.Count == 2 && laid.Count(s => s.Rings is not null) == 1 && laid.Count(s => s.Line is not null) == 1).OverrideFailureMessage("Polygon as polygon, centre line as capsule.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AuthoredWalkJoinsAnExistingSidewalkAsOneSurfaceWithNoKerbAtTheJoin()
    {
        var walk = new Vector2[] { new(30f, 4.25f), new(30f, 40f) };
        var street = new List<WorldFeature>
        {
            WorldFeatureFixture.Create("osm:way:1", "road", new PolygonGeometry(Rect(-100f, -3f, 100f, 3f)), RoadProps("residential", null)),
            WorldFeatureFixture.Create("osm:way:2", "road", new PolygonGeometry(Rect(-100f, 3f, 100f, 5.5f)), RoadProps("footway", null)),
        };
        var network = new List<NetworkLine> { Road("osm:way:1", [new(-100f, 0f), new(100f, 0f)], 6f, "residential") };
        var bare = GroundTestSupport.Of(street, new WorldNetwork(network, [], [], [], []));
        var bareContext = GroundTestSupport.Context(bare);
        var withWalk = GroundTestSupport.Of([.. street, WorldFeatureFixture.Create("design:t-walk", "road", new PolylineGeometry(new Polyline(walk)), RoadProps("footway", 2.5f))], new WorldNetwork([.. network, Road("design:t-walk", walk, 2.5f)], [], [], [], []));
        var withWalkContext = GroundTestSupport.Context(withWalk);
        var networkOnly = GroundTestSupport.Of(street, new WorldNetwork([.. network, Road("design:t-walk", walk, 2.5f)], [], [], [], []));

        // The walk and the sidewalk are one paved surface: no kerb, edging or step across the join, and none beside it.
        for (var z = 5.0f; z <= 30f; z += 0.5f)
            foreach (var x in new[] { 29.3f, 30f, 30.7f })
                AssertThat(GroundTestSupport.SurfaceAt(withWalkContext, x, z) == GroundSurface.Paver).OverrideFailureMessage($"({x}, {z}) is {GroundTestSupport.SurfaceAt(withWalkContext, x, z)}, not plain paving.").IsTrue();
        AssertThat(GroundTestSupport.SurfaceAt(bareContext, 30f, 20f) != GroundSurface.Paver).OverrideFailureMessage("Without the walk that ground is lawn.").IsTrue();

        // The carriageway has no fillet or kerb of its own from a footway, and the footway's crossing sits where it meets the road.
        AssertThat(RoadGraph.Fillets(GroundSources.Read(withWalk).Roads).Count == 0).OverrideFailureMessage("A footway never rounds a carriageway corner.").IsTrue();
        var crossing = new Vector2[] { new(50f, -20f), new(50f, 20f) };
        var crossed = GroundTestSupport.Of(street, new WorldNetwork([.. network, Road("design:t-cross", crossing, 2.5f)], [], [], [], []));
        var marks = GroundModule.AssembleMarkings(GroundTestSupport.Context(crossed), Section).Markings;
        AssertThat(marks.Crossings.Count == 1 && Vector2.Distance(marks.Crossings[0], new(50f, 0f)) < TestTolerance.Position).OverrideFailureMessage("An authored footway across the road gets one zebra where it crosses.").IsTrue();
        AssertThat(GroundModule.AssembleMarkings(bareContext, Section).Markings.Crossings.Count == 0).OverrideFailureMessage("No walk, no crossing.").IsTrue();

        // Feature and network copies together build the same tile as the network alone: no second layer, no z conflict.
        var a = new GroundModule().Build(GroundTestSupport.Context(withWalk), new TileKey(0, 0), LodLevel.L0);
        var b = new GroundModule().Build(GroundTestSupport.Context(networkOnly), new TileKey(0, 0), LodLevel.L0);
        AssertThat(a.Meshes.Count == b.Meshes.Count && a.Meshes.Count > 0).OverrideFailureMessage("Bundle count differs.").IsTrue();
        for (var k = 0; k < a.Meshes.Count; k++)
        {
            AssertThat(a.Meshes[k].Position.SequenceEqual(b.Meshes[k].Position) && a.Meshes[k].Index.SequenceEqual(b.Meshes[k].Index)).OverrideFailureMessage($"Mesh {k} differs.").IsTrue();
            a.Meshes[k].ValidateVertexData();
        }
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    [TestCategory("Slow")]
    public void RealCampusLaysEveryAuthoredLineOnceAndKeepsItsAreasAndHoles()
    {
        var world = RealMapFixture.Load().World;
        var (shapes, roads) = GroundSources.Read(world);
        // Only the classes game/gen/world/ projects into the network (footway, path, pedestrian); a design drive or service lane is laid from its own feature.
        var authored = world.Features.Values.Where(f => f.Kind.Value == "road" && f.Geometry is PolylineGeometry &&
            WorldProps.TryText(f.Props, "area") != "yes" &&
            GroundClassifier.RoadType(f.Props) is "footway" or "path" or "pedestrian").ToArray();
        AssertThat(authored.Length > 0).OverrideFailureMessage($"The campus has authored footway lines, got {authored.Length}.").IsTrue();
        foreach (var feature in authored)
        {
            var road = world.Network.Roads.SingleOrDefault(r => r.Id == feature.Id);
            AssertThat(road is not null).OverrideFailureMessage($"{feature.Id.Value} is missing from the effective network.").IsTrue();
            var laid = shapes.Where(s => s.Id == feature.Id).ToArray();
            AssertThat(laid.Length == 1 && laid[0].Kind == SurfaceKind.Walk && laid[0].Line is not null).OverrideFailureMessage($"{feature.Id.Value} must be laid once as a footway, got {laid.Length}.").IsTrue();
            AssertThat(laid[0].Line!.SequenceEqual(PlanLines.CleanPath(road!.Line.Points)) && laid[0].HalfWidth == road.Width * 0.5f).OverrideFailureMessage($"{feature.Id.Value} is not laid along its network line.").IsTrue();
        }
        // OSM area perimeters retain their network identity but never become extra paved capsule bands.
        var perimeters = world.Network.Roads.Where(r => WorldProps.TryText(r.Props, "area") == "yes").ToArray();
        AssertThat(perimeters.Length > 0).IsTrue();
        foreach (var perimeter in perimeters)
            AssertThat(roads.All(r => r.Id != perimeter.Id) && shapes.All(s => s.Id != perimeter.Id || s.Line is null))
                .OverrideFailureMessage($"{perimeter.Id} is an area perimeter, not a paved centre line.").IsTrue();
        // No centre line is laid twice anywhere: every network road has exactly one capsule chain, and no other line shape shares its ID.
        var chains = shapes.Where(s => s.Line is not null && s.Kind is SurfaceKind.Carriageway or SurfaceKind.Walk or SurfaceKind.Plaza && !s.Id.Value.Contains(":footprint:", StringComparison.Ordinal)).ToArray();
        AssertThat(chains.GroupBy(s => s.Id).All(g => g.Count() == 1)).OverrideFailureMessage("A road ID is laid as more than one centre line.").IsTrue();
        // An authored line of a class the network does not take (a design drive) is laid from its own feature, once.
        var outside = world.Features.Values.Count(f => f.Kind.Value == "road" && f.Geometry is PolylineGeometry &&
            world.Network.Roads.All(r => r.Id != f.Id));
        AssertThat(chains.Length == roads.Count + outside).OverrideFailureMessage($"{roads.Count} network roads and {outside} lines outside it give {chains.Length} line shapes.").IsTrue();
        // Authored pedestrian areas keep their data identity and polygon geometry.
        var areas = world.Features.Values.Where(feature => feature.Kind.Value == "road" && GroundClassifier.RoadType(feature.Props) == "pedestrian" &&
            feature.Geometry is PolygonGeometry or MultiPolygonGeometry).ToArray();
        AssertThat(areas.Length > 0).IsTrue();
        foreach (var area in areas)
            AssertThat(shapes.Any(shape => shape.Id == area.Id && shape.Kind == SurfaceKind.Plaza &&
                shape.Rings is not null && shape.Line is null)).OverrideFailureMessage($"{area.Id} stays a plaza polygon.").IsTrue();
        AssertThat(shapes.Any(shape => shape.Kind == SurfaceKind.Turf && shape.Rings is { Length: > 1 }))
            .OverrideFailureMessage("The athletics ground keeps its infield hole.").IsTrue();
    }

    // ---------------------------------------------------------------- helpers

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static string RoadProps(string type, float? width) =>
        width is { } w ? $$"""{"highway":"{{type}}","width":{{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}""" : $$"""{"highway":"{{type}}"}""";

    private static NetworkLine Road(string id, Vector2[] points, float width, string type = "footway") =>
        new(new FeatureId(id), new Polyline(points), width) { Props = Json($$"""{"highway":"{{type}}"}""") };

    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static IReadOnlyList<Vector2> Shift(IReadOnlyList<Vector2> ring, float dx) => ring.Select(p => new Vector2(p.X + dx, p.Y)).ToArray();

}
