using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

/// <summary>Ground sources are read per WorldFeature part: kinds, props, polygons and holes of every source record.</summary>
[TestSuite]
public sealed class GroundSourcesTests
{
    private const string Building = """{"kind":"building"}""";

    [TestCase]
    public void FaceInputQueriesActualPositionAndKeepsAmbiguousSourcesUnresolved()
    {
        const string plaza = """{"kind":"road","roadType":"pedestrian"}""";
        var world = TestWorld.Of([
            Entity("fixture:plaza-a", "road", plaza, Part("source:a", plaza, Rect(0, 0, 10, 10))),
            Entity("fixture:plaza-b", "road", plaza, Part("source:b", plaza, Rect(8, 0, 18, 10))),
            Entity("fixture:plaza-far", "road", plaza, Part("source:far", plaza, Rect(30, 0, 40, 10)))]);
        var layout = GroundLayout.Of(world);
        var context = new GroundSourceContext(layout.Near(new(0, 0), new(64, 64)), GroundSection.Default, 0f);
        var input = new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Top, context);
        var a = input.SourcesAt(new(5, 5)).Single();
        AssertThat(a.Id.Value == "fixture:plaza-a" && a.PartSourceId == "source:a" && a.DirectionPath is null).OverrideFailureMessage("The face must retain its real feature/part, without guessing an area paving axis.").IsTrue();
        AssertThat(input.SourcesAt(new(9, 5)).Select(shape => shape.Id.Value).SequenceEqual(["fixture:plaza-a", "fixture:plaza-b"])).OverrideFailureMessage("Overlapping areas must remain multiple candidates, not select the first tile source.").IsTrue();
        AssertThat(!input.SourcesAt(new(25, 5)).Any()).OverrideFailureMessage("A gap must not inherit a distant source from the same tile.").IsTrue();
        AssertThat(input.SourcesAt(new(35, 5)).Single().PartSourceId == "source:far").OverrideFailureMessage("Queries must use the current face position.").IsTrue();
    }

    [TestCase]
    public void WalkFaceInputRetainsTheOriginalPolylineWithoutInventingPhase()
    {
        Vector2[] points = [new(0, 20), new(10, 20), new(10, 30)];
        var feature = new WorldFeature(new("fixture:walk"), FeatureSource.Osm, new("road"), new PolylineGeometry(new(points)))
        { Props = Json("""{"roadType":"footway","width":2.5}""") };
        var layout = GroundLayout.Of(TestWorld.Of([feature]));
        var context = new GroundSourceContext(layout.Near(new(0, 0), new(64, 64)), GroundSection.Default, 0f);
        var source = new GroundFaceInput(GroundSurface.Paver, GroundFaceRole.Top, context).SourcesAt(new(5, 20)).Single();
        AssertThat(source.Id == feature.Id && source.DirectionPath!.SequenceEqual(points) && ReferenceEquals(source.DirectionPath, source.Line)).OverrideFailureMessage("The batching input needs the source walk and ordered path, shared rather than copied for every face.").IsTrue();
        AssertThat(source.PartSourceId is null).OverrideFailureMessage("A network-independent feature must not invent a part identity.").IsTrue();
    }

    [TestCase]
    public void PlazaTopAndKerbAreSeparateContentClassesWithoutFakeShaderDifferences()
    {
        var context = new GroundSourceContext([], GroundSection.Default, 0f);
        var top = new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Top, context);
        var face = new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Vertical, context);
        AssertThat(GroundPalette.MaterialOf(top.Surface, top.Role, GroundColor.Curb) == GroundMaterial.Plaza &&
            GroundPalette.MaterialOf(face.Surface, face.Role, GroundColor.Curb) == GroundMaterial.Curb).OverrideFailureMessage("Granite top and its no-pattern vertical faces must not lose their distinct content roles.").IsTrue();
        var sink = new MeshSink();
        sink.Triangle(GroundColor.Curb, GroundPalette.FlagNoShadow, new(0, 0, 0), new(1, 0, 0), new(0, 0, 1),
            Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, top);
        sink.Triangle(GroundColor.Curb, GroundPalette.FlagNoShadow, new(0, 0, 0), new(1, 0, 0), new(0, 1, 0),
            Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, face);
        var meshes = sink.Finish();
        AssertThat(meshes.Count == 2 && meshes[0].Material == meshes[1].Material &&
            meshes.All(mesh => mesh.Material.Spec.SurfacePattern is null && mesh.ColorIndices!.All(id => id == GroundPalette.Id(GroundColor.Curb)))).OverrideFailureMessage("Content classes split before batching; colour/Fx and GPU material sharing stay unchanged until T03 opts in.").IsTrue();
    }

    [TestCase]
    public void MultiPartParentKeepsEveryPolygonAndItsHoles()
    {
        // One entity, two source parts: a block around a courtyard and a separate annex. The parent's aggregate geometry
        // is a multi-polygon, which the old reader rejected.
        var block = Part("osm:way:1", Building, WithHole(Rect(-20f, -20f, 20f, 20f), Rect(-6f, -6f, 6f, 6f)));
        var annex = Part("osm:way:2", Building, Rect(30f, -5f, 40f, 5f));
        var world = TestWorld.Of([Entity("test:relation:1", "building", """{"id":"test:relation:1","kind":"building"}""", block, annex)]);

        var (shapes, _) = GroundSources.Read(world);
        AssertThat(shapes.Count == 2 && shapes.All(s => s.Kind == SurfaceKind.Building)).OverrideFailureMessage($"Expected two building footprints, got {shapes.Count}.").IsTrue();
        AssertThat(shapes.Select(s => s.Rings!.Length).Order().SequenceEqual([1, 2])).OverrideFailureMessage("The courtyard must stay a hole of its own footprint.").IsTrue();

        var layout = GroundLayout.Of(world);
        AssertThat(Math.Abs((double)(6.0) - (double)(layout.SignedDistance(SurfaceKind.Building, new(0f, 0f), 50f))) <= 0.01).OverrideFailureMessage($"courtyard centre is outside, 6 m from the walls: expected {6.0}, got {layout.SignedDistance(SurfaceKind.Building, new(0f, 0f), 50f)}.").IsTrue();
        AssertThat(Math.Abs((double)(-7.0) - (double)(layout.SignedDistance(SurfaceKind.Building, new(-13f, 0f), 50f))) <= 0.01).OverrideFailureMessage($"inside the block wall: expected {-7.0}, got {layout.SignedDistance(SurfaceKind.Building, new(-13f, 0f), 50f)}.").IsTrue();
        AssertThat(Math.Abs((double)(-5.0) - (double)(layout.SignedDistance(SurfaceKind.Building, new(35f, 0f), 50f))) <= 0.01).OverrideFailureMessage($"inside the annex: expected {-5.0}, got {layout.SignedDistance(SurfaceKind.Building, new(35f, 0f), 50f)}.").IsTrue();
        AssertThat(Math.Abs((double)(5.0) - (double)(layout.SignedDistance(SurfaceKind.Building, new(25f, 0f), 50f))) <= 0.01).OverrideFailureMessage($"gap between the parts stays open: expected {5.0}, got {layout.SignedDistance(SurfaceKind.Building, new(25f, 0f), 50f)}.").IsTrue();

        var masks = GroundMasks.Build(TestWorld.Context(world, 128f));
        byte Shelter(float x, float z) { var (px, pz) = masks.Pixel(new(x, z)); return masks.Read(MaskChannel.Shelter, px, pz); }
        AssertThat(Shelter(0.5f, 0.5f) == 255).OverrideFailureMessage("The courtyard is open to rain and snow.").IsTrue();
        AssertThat(Shelter(-13.5f, 0.5f) == 0).OverrideFailureMessage("Under the block is sheltered.").IsTrue();
        AssertThat(Shelter(35.5f, 0.5f) == 0).OverrideFailureMessage("Under the annex is sheltered (a part of a multi-part parent).").IsTrue();
        AssertThat(Shelter(25.5f, 0.5f) == 255).OverrideFailureMessage("The gap between the parts is exposed.").IsTrue();

        var tile = new GroundModule().Build(TestWorld.Context(world), new TileKey(0, 0), LodLevel.L0);
        AssertThat(tile.Meshes.Count > 0).OverrideFailureMessage("A world with a multi-part building builds ground.").IsTrue();
        foreach (var mesh in tile.Meshes) mesh.ValidateVertexData();
    }

    [TestCase]
    public void MixedPoiParentIsClassifiedPartByPart()
    {
        // A poi entity (a park) that owns building parts, a car park, a track and a pool: the parent's kind says nothing
        // about its parts. Type properties come from each part's own record: the entity has no roadType or sport.
        var parts = new[]
        {
            Part("osm:way:10", """{"kind":"poi"}""", Rect(-10f, -10f, 10f, 10f)),
            Part("osm:way:11", Building, Rect(-8f, -8f, -2f, -2f)),
            Part("osm:way:12", Building, Rect(2f, 2f, 8f, 8f), Rect(30f, 30f, 36f, 36f)),
            Part("osm:way:13", """{"kind":"parking"}""", Rect(12f, 0f, 20f, 6f)),
            Part("osm:way:14", """{"kind":"sport","sport":"running"}""", Rect(40f, 0f, 60f, 10f)),
            Part("osm:way:15", """{"kind":"sport","sport":"swimming"}""", Rect(40f, 20f, 50f, 30f)),
            Part("osm:way:16", """{"kind":"road","roadType":"residential"}""", Rect(-30f, 40f, 30f, 46f)),
            Part("osm:way:17", """{"kind":"road","roadType":"footway"}""", Rect(-30f, 50f, 30f, 52f)),
        };
        var world = TestWorld.Of([Entity("test:way:1", "poi", """{"id":"test:way:1","kind":"poi"}""", parts)]);

        var (shapes, _) = GroundSources.Read(world);
        var kinds = shapes.GroupBy(s => s.Kind).ToDictionary(g => g.Key, g => g.Count());
        AssertThat(shapes.Count == 8).OverrideFailureMessage($"Eight ground polygons, none from the poi part; got {shapes.Count}.").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Building) == 3).OverrideFailureMessage("Three building polygons (one part holds two).").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Parking) == 1).OverrideFailureMessage("Parking part.").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Track) == 1).OverrideFailureMessage("Running track part (sport read from the part).").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Water) == 1).OverrideFailureMessage("Swimming pool part (sport read from the part).").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Carriageway) == 1).OverrideFailureMessage("Residential road part (roadType read from the part).").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Walk) == 1).OverrideFailureMessage("Footway part.").IsTrue();

        var layout = GroundLayout.Of(world);
        AssertThat(layout.SignedDistance(SurfaceKind.Building, new(-5f, -5f), 20f) < 0f).OverrideFailureMessage("Building part keeps its footprint.").IsTrue();
        AssertThat(layout.SignedDistance(SurfaceKind.Building, new(33f, 33f), 20f) < 0f).OverrideFailureMessage("Second polygon of a part keeps its footprint.").IsTrue();
        AssertThat(layout.SignedDistance(SurfaceKind.Building, new(0f, 0f), 20f) > 0f).OverrideFailureMessage("The poi part itself is no footprint.").IsTrue();
    }

    [TestCase]
    public void SingleSourceFeaturesReadTheSameWithOrWithoutParts()
    {
        foreach (var legacy in new[] { TestWorld.Street(), TestWorld.Junction() })
        {
            // The same features, each wrapped as its only part with the same props: the old single-source path.
            var wrapped = TestWorld.Of(legacy.Features.Values.Select(f => f with
            {
                Parts = [new WorldFeaturePart(f.Id.Value, f.Geometry, f.Height, f.Storeys) { Props = f.Props }]
            }), legacy.Network);
            var a = GroundSources.Read(legacy);
            var b = GroundSources.Read(wrapped);
            AssertThat(a.Shapes.Count == b.Shapes.Count && a.Roads.Count == b.Roads.Count).OverrideFailureMessage("Shape or road count differs.").IsTrue();
            for (var k = 0; k < a.Shapes.Count; k++)
            {
                var (x, y) = (a.Shapes[k], b.Shapes[k]);
                AssertThat(x.Kind == y.Kind && x.Id == y.Id && x.Min == y.Min && x.Max == y.Max && x.HalfWidth == y.HalfWidth).OverrideFailureMessage($"Shape {k} differs.").IsTrue();
                AssertThat((x.Line is null && y.Line is null) || x.Line!.SequenceEqual(y.Line!)).OverrideFailureMessage($"Line {k} differs.").IsTrue();
                AssertThat((x.Rings is null && y.Rings is null) ||
                    (x.Rings!.Length == y.Rings!.Length && x.Rings.Zip(y.Rings).All(r => r.First.SequenceEqual(r.Second)))).OverrideFailureMessage($"Rings {k} differ.").IsTrue();
            }
            var ground = new GroundModule().Build(TestWorld.Context(legacy), new TileKey(0, 0), LodLevel.L0);
            var again = new GroundModule().Build(TestWorld.Context(wrapped), new TileKey(0, 0), LodLevel.L0);
            AssertThat(ground.Meshes.Count == again.Meshes.Count).OverrideFailureMessage("Bundle count differs.").IsTrue();
            for (var k = 0; k < ground.Meshes.Count; k++)
                AssertThat(ground.Meshes[k].Position.SequenceEqual(again.Meshes[k].Position) &&
                    ground.Meshes[k].Index.SequenceEqual(again.Meshes[k].Index)).OverrideFailureMessage($"Mesh {k} differs.").IsTrue();
        }
    }

    [TestCase]
    public void UnsupportedGeometryNamesTheEntityAndPart()
    {
        var odd = new Unsupported();
        var withParts = TestWorld.Of([Entity("test:relation:7", "building", Building,
            Part("osm:way:70", Building, Rect(0f, 0f, 4f, 4f)),
            new WorldFeaturePart("osm:way:71", odd, 5f, 1) { Props = Json(Building) })]);
        Exception? sourceError = null;
        try { GroundSources.Read(withParts); }
        catch (Exception caught) { sourceError = caught; }
        AssertThat(sourceError).OverrideFailureMessage("Expected the unsupported geometry to be rejected.").IsNotNull();
        AssertThat(sourceError!).IsInstanceOf<InvalidOperationException>();
        var message = sourceError!.Message;
        AssertThat(message.Contains("test:relation:7", StringComparison.Ordinal) && message.Contains("osm:way:71", StringComparison.Ordinal) &&
            message.Contains(nameof(Unsupported), StringComparison.Ordinal)).OverrideFailureMessage($"Error must name entity, part and geometry: {message}").IsTrue();
        AssertThat(!message.Contains("osm:way:70", StringComparison.Ordinal)).OverrideFailureMessage("The readable part is not blamed.").IsTrue();

        // The same failure through the public build path is not swallowed on the way.
        Exception? buildError = null;
        try { new GroundModule().Build(TestWorld.Context(withParts), new TileKey(0, 0), LodLevel.L0); }
        catch (Exception caught) { buildError = caught; }
        AssertThat(buildError).OverrideFailureMessage("Expected the unsupported geometry to be rejected.").IsNotNull();
        AssertThat(buildError!).IsInstanceOf<InvalidOperationException>();
        AssertThat(buildError!.Message.Contains("osm:way:71", StringComparison.Ordinal))
            .OverrideFailureMessage("The tile build must surface the entity and part.").IsTrue();

        var withoutParts = TestWorld.Of([new WorldFeature(new FeatureId("test:way:8"), FeatureSource.Osm, new DesignKind("road"), odd)
            { Props = Json("""{"roadType":"service"}""") }]);
        Exception? plainError = null;
        try { GroundSources.Read(withoutParts); }
        catch (Exception caught) { plainError = caught; }
        AssertThat(plainError).OverrideFailureMessage("Expected the unsupported geometry to be rejected.").IsNotNull();
        AssertThat(plainError!).IsInstanceOf<InvalidOperationException>();
        var plain = plainError!.Message;
        AssertThat(plain.Contains("test:way:8", StringComparison.Ordinal) && plain.Contains(nameof(Unsupported), StringComparison.Ordinal)).OverrideFailureMessage($"Error must name the entity: {plain}").IsTrue();

        // A kind that never touches the ground is not read at all, whatever its geometry.
        var green = TestWorld.Of([Entity("test:way:9", "green", """{"kind":"green"}""",
            new WorldFeaturePart("osm:way:90", odd, 0f, null) { Props = Json("""{"kind":"green"}""") })]);
        AssertThat(GroundSources.Read(green).Shapes.Count == 0).OverrideFailureMessage("Green parts leave the lawn untouched.").IsTrue();
    }

    [TestCase]
    public void AnchorOnlyEntitiesHaveNoGroundExtent()
    {
        // Entities known only by their anchor (no footprint in the data) arrive as point geometry, with or without a
        // part record: nothing to shape, and nothing to reject.
        var anchor = new PointGeometry(new Vector3(10f, 0f, 10f));
        var world = TestWorld.Of(
        [
            new WorldFeature(new FeatureId("test:way:20"), FeatureSource.Osm, new DesignKind("building"), anchor) { Props = Json(Building) },
            new WorldFeature(new FeatureId("test:way:21"), FeatureSource.Osm, new DesignKind("water"), anchor),
            Entity("test:way:22", "poi", """{"kind":"poi"}""", new WorldFeaturePart("osm:way:220", anchor, null, null) { Props = Json(Building) }),
        ]);
        AssertThat(GroundSources.Read(world).Shapes.Count == 0).OverrideFailureMessage("Point geometry yields no ground shape.").IsTrue();
    }

    [TestCase]
    public void RealCampusIsReadPartByPart()
    {
        var files = RealFiles();
        var world = WorldModelLoader.Load(new("qingshuihe"), files).World;
        var (shapes, _) = GroundSources.Read(world);   // used to throw on the first multi-part building

        var raw = RawPolygons(files["campus.json"]);
        var polygonShapes = shapes.Where(s => s.Rings is not null && !s.Id.Value.StartsWith("fillet:", StringComparison.Ordinal)).ToArray();
        var ground = new HashSet<string> { "road", "parking", "water", "building", "sport" };
        // A stadium or sports-centre outline without a sport tag only encloses other features and has no ground of its own.
        AssertThat(polygonShapes.Length == raw.Count(p => ground.Contains(p.Kind) && !p.Outline)).OverrideFailureMessage("Every ground polygon of every part gives exactly one shape.").IsTrue();
        AssertThat(raw.Count(p => p.Outline) == 2).OverrideFailureMessage("The campus has one stadium outline and one sports-centre outline.").IsTrue();

        int Shapes(SurfaceKind kind) => polygonShapes.Count(s => s.Kind == kind);
        int Raw(Func<RawPolygon, bool> where) => raw.Count(where);
        AssertThat(Shapes(SurfaceKind.Building) == Raw(p => p.Kind == "building")).OverrideFailureMessage("Building footprints.").IsTrue();
        AssertThat(polygonShapes.Where(s => s.Kind == SurfaceKind.Building).Sum(s => s.Rings!.Length) == raw.Where(p => p.Kind == "building").Sum(p => p.Rings.Length)).OverrideFailureMessage("Every building hole is kept as a ring of its footprint.").IsTrue();
        AssertThat(Shapes(SurfaceKind.Parking) == Raw(p => p.Kind == "parking")).OverrideFailureMessage("Car parks.").IsTrue();
        AssertThat(Shapes(SurfaceKind.Track) == Raw(p => p.Kind == "sport" && p.Sport == "running")).OverrideFailureMessage("Running tracks come from the part's own sport.").IsTrue();
        AssertThat(Shapes(SurfaceKind.Turf) == Raw(p => p.Kind == "sport" && !p.Outline && p.Sport is "soccer" or null)).OverrideFailureMessage("Football pitches, and pitches that name no sport, are turf (the campus has one: the ring-shaped athletics field).").IsTrue();
        AssertThat(Shapes(SurfaceKind.Court) == Raw(p => p.Kind == "sport" && p.Sport is not (null or "running" or "soccer" or "swimming"))).OverrideFailureMessage("Courts.").IsTrue();
        AssertThat(Shapes(SurfaceKind.Water) == Raw(p => p.Kind == "water" || (p.Kind == "sport" && p.Sport == "swimming"))).OverrideFailureMessage("Lakes and pools.").IsTrue();

        // Cross-check the whole campus against the raw polygons with an independent even-odd test: parts, polygons and holes.
        var layout = GroundLayout.Of(world);
        Compare(layout, SurfaceKind.Building, raw.Where(p => p.Kind == "building").ToArray());
        Compare(layout, SurfaceKind.Parking, raw.Where(p => p.Kind == "parking").ToArray());
        Compare(layout, SurfaceKind.Water, raw.Where(p => p.Kind == "water" || (p.Kind == "sport" && p.Sport == "swimming")).ToArray());

        // Mixed parents of the architecture review: poi entities owning building parts and an open park part.
        foreach (var id in new[] { "qsh:way:682336066", "qsh:way:883187548" })
        {
            var feature = world.Features[new(id)];
            AssertThat(feature.Kind.Value == "poi" && feature.Parts.Count > 1).OverrideFailureMessage($"{id} is no longer a mixed parent.").IsTrue();
        }
        AssertThat(layout.SignedDistance(SurfaceKind.Building, new(-672.712f, -106.672f), 30f) < 0f).OverrideFailureMessage("A building part of a poi parent keeps its footprint.").IsTrue();
        AssertThat(layout.SignedDistance(SurfaceKind.Building, new(-619.9118f, -102.2f), 30f) > 0f).OverrideFailureMessage("The park part of the same parent is no footprint.").IsTrue();
    }

    /// <summary>Samples every polygon's neighbourhood and checks the layout's inside/outside against raw even-odd.</summary>
    private static void Compare(GroundLayout layout, SurfaceKind kind, RawPolygon[] polygons)
    {
        var sampled = 0;
        foreach (var polygon in polygons)
        {
            var (min, max) = polygon.Bounds;
            for (var z = min.Y - 3f; z <= max.Y + 3f; z += 2f)
                for (var x = min.X - 3f; x <= max.X + 3f; x += 2f)
                {
                    var distance = layout.SignedDistance(kind, new(x, z), 4f);
                    if (MathF.Abs(distance) < 0.02f) continue;   // on a boundary: either answer is right
                    var inside = polygons.Any(p => p.Contains(x, z));
                    sampled++;
                    AssertThat(inside == distance < 0f).OverrideFailureMessage($"{kind} at ({x}, {z}): layout says {(distance < 0f ? "inside" : "outside")}, source polygons say {(inside ? "inside" : "outside")}.").IsTrue();
                }
        }
        AssertThat(sampled > 1000).OverrideFailureMessage($"Sampled only {sampled} points for {kind}.").IsTrue();
    }

    // ---------------------------------------------------------------- helpers

    private sealed record Unsupported : OverlayGeometry;

    private sealed record RawPolygon(string Kind, string? Sport, bool Outline, Vector2[][] Rings)
    {
        public (Vector2 Min, Vector2 Max) Bounds { get; } = Extent(Rings[0]);

        private static (Vector2, Vector2) Extent(Vector2[] outer) =>
            (outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min), outer.Aggregate(new Vector2(float.MinValue), Vector2.Max));

        /// <summary>Even-odd over all rings in double precision: a hole is outside, independent of the generator's code.</summary>
        public bool Contains(double x, double z)
        {
            if (x < Bounds.Min.X || x > Bounds.Max.X || z < Bounds.Min.Y || z > Bounds.Max.Y) return false;
            var inside = false;
            foreach (var ring in Rings)
                for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                {
                    var (a, b) = (ring[j], ring[i]);
                    if ((a.Y > z) != (b.Y > z) && x < (double)(b.X - a.X) * (z - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
                }
            return inside;
        }
    }

    private static List<RawPolygon> RawPolygons(string campusJson)
    {
        var result = new List<RawPolygon>();
        using var document = JsonDocument.Parse(campusJson);
        // The OSM leisure tag of every entity: a part's own record does not repeat it.
        var leisure = document.RootElement.GetProperty("entities").EnumerateArray()
            .Where(e => e.TryGetProperty("sourceTags", out var tags) && tags.TryGetProperty("leisure", out _))
            .ToDictionary(e => e.GetProperty("id").GetString()!, e => e.GetProperty("sourceTags").GetProperty("leisure").GetString()!);
        foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
        {
            var kind = feature.GetProperty("kind").GetString()!;
            var sport = feature.TryGetProperty("sport", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
            var owner = feature.TryGetProperty("entityId", out var entityId) && entityId.ValueKind == JsonValueKind.String ? entityId.GetString() : null;
            var outline = kind == "sport" && sport is null && owner is not null && leisure.GetValueOrDefault(owner) is "stadium" or "sports_centre";
            foreach (var polygon in feature.GetProperty("polygons").EnumerateArray())
                result.Add(new(kind, sport, outline, polygon.EnumerateArray()
                    .Select(ring => ring.EnumerateArray().Select(p => new Vector2(p[0].GetSingle(), p[1].GetSingle())).ToArray()).ToArray()));
        }
        return result;
    }

    private static Dictionary<string, string> RealFiles()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        var data = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
        return new[] { "campus.json", "network.json", "interiors.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
    }

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static PolygonRings WithHole(PolygonRings outer, PolygonRings hole) => new(outer.Outer, [hole.Outer]);

    private static WorldFeaturePart Part(string sourceId, string props, params PolygonRings[] polygons) =>
        new(sourceId, polygons.Length == 1 ? new PolygonGeometry(polygons[0]) : new MultiPolygonGeometry(polygons), 10f, 3) { Props = Json(props) };

    /// <summary>An entity the way the world model loader builds one: parts sorted by source ID, aggregate multi-polygon geometry.</summary>
    private static WorldFeature Entity(string id, string kind, string props, params WorldFeaturePart[] parts)
    {
        var polygons = parts.SelectMany(p => p.Geometry switch
        {
            PolygonGeometry polygon => new[] { polygon.Rings },
            MultiPolygonGeometry multi => multi.Polygons.ToArray(),
            _ => Array.Empty<PolygonRings>()
        }).ToArray();
        OverlayGeometry geometry = polygons.Length switch
        {
            0 => new PointGeometry(Vector3.Zero),
            1 => new PolygonGeometry(polygons[0]),
            _ => new MultiPolygonGeometry(polygons)
        };
        var outline = polygons.SelectMany(r => r.Outer).DefaultIfEmpty().ToArray();
        var min = outline.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
        var max = outline.Aggregate(new Vector2(float.MinValue), Vector2.Max);
        return new(new FeatureId(id), FeatureSource.Osm, new DesignKind(kind), geometry)
        {
            Parts = parts.OrderBy(p => p.SourceId, StringComparer.Ordinal).ToArray(),
            Bounds = new Box3(new(min.X, 0f, min.Y), new(max.X, 10f, max.Y)),
            Props = Json(props)
        };
    }
}
