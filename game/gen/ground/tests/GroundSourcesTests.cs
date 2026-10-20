using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

/// <summary>Ground sources are read per WorldFeature part: kinds, props, polygons and holes of every source record.</summary>
[TestSuite]
public sealed class GroundSourcesTests
{
    private const string Building = """{"kind":"building"}""";

    [TestCase, TestCategory(TestReason.Contract)]
    public void FaceInputQueriesActualPositionAndKeepsAmbiguousSourcesUnresolved()
    {
        const string plaza = """{"kind":"road","highway":"pedestrian"}""";
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void WalkFaceInputRetainsTheOriginalPolylineWithoutInventingPhase()
    {
        Vector2[] points = [new(0, 20), new(10, 20), new(10, 30)];
        var feature = WorldFeatureFixture.Create("fixture:walk", "road", new PolylineGeometry(new(points)),
            """{"highway":"footway","width":2.5}""");
        var layout = GroundLayout.Of(TestWorld.Of([feature]));
        var context = new GroundSourceContext(layout.Near(new(0, 0), new(64, 64)), GroundSection.Default, 0f);
        var source = new GroundFaceInput(GroundSurface.Paver, GroundFaceRole.Top, context).SourcesAt(new(5, 20)).Single();
        AssertThat(source.Id == feature.Id && source.DirectionPath!.SequenceEqual(points) && ReferenceEquals(source.DirectionPath, source.Line)).OverrideFailureMessage("The batching input needs the source walk and ordered path, shared rather than copied for every face.").IsTrue();
        AssertThat(source.PartSourceId is null).OverrideFailureMessage("A network-independent feature must not invent a part identity.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PlazaTopAndKerbAreSeparateContentClassesWithoutFakeShaderDifferences()
    {
        var context = new GroundSourceContext([], GroundSection.Default, 0f);
        var top = new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Top, context);
        var face = new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Vertical, context);
        AssertThat(GroundPalette.MaterialOf(top.Surface, top.Role, GroundColor.Curb) == GroundMaterial.Plaza &&
            GroundPalette.MaterialOf(face.Surface, face.Role, GroundColor.Curb) == GroundMaterial.Curb).OverrideFailureMessage("Granite top and its no-pattern vertical faces must not lose their distinct content roles.").IsTrue();
        var sink = new GroundSurfaceSink();
        sink.Triangle(GroundColor.Curb, VertexFlag.NoShadow, new(0, 0, 0), new(1, 0, 0), new(0, 0, 1),
            Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, top);
        sink.Triangle(GroundColor.Curb, VertexFlag.NoShadow, new(0, 0, 0), new(1, 0, 0), new(0, 1, 0),
            Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, face);
        var meshes = sink.Finish();
        AssertThat(meshes.Count == 2 && meshes[0].Material == meshes[1].Material &&
            meshes.All(mesh => mesh.Material.Spec.SurfacePattern is null && mesh.ColorIndices!.All(id => id == GroundPalette.Id(GroundColor.Curb)))).OverrideFailureMessage("Content classes split before batching; colour/Fx and GPU material sharing stay unchanged until T03 opts in.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
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
        AssertThat(Math.Abs((double)(6.0) - (double)(layout.SignedDistance(SurfaceKind.Building, new(0f, 0f), 50f))) <= TestTolerance.Position).OverrideFailureMessage($"courtyard centre is outside, 6 m from the walls: expected {6.0}, got {layout.SignedDistance(SurfaceKind.Building, new(0f, 0f), 50f)}.").IsTrue();
        AssertThat(Math.Abs((double)(-7.0) - (double)(layout.SignedDistance(SurfaceKind.Building, new(-13f, 0f), 50f))) <= TestTolerance.Position).OverrideFailureMessage($"inside the block wall: expected {-7.0}, got {layout.SignedDistance(SurfaceKind.Building, new(-13f, 0f), 50f)}.").IsTrue();
        AssertThat(Math.Abs((double)(-5.0) - (double)(layout.SignedDistance(SurfaceKind.Building, new(35f, 0f), 50f))) <= TestTolerance.Position).OverrideFailureMessage($"inside the annex: expected {-5.0}, got {layout.SignedDistance(SurfaceKind.Building, new(35f, 0f), 50f)}.").IsTrue();
        AssertThat(Math.Abs((double)(5.0) - (double)(layout.SignedDistance(SurfaceKind.Building, new(25f, 0f), 50f))) <= TestTolerance.Position).OverrideFailureMessage($"gap between the parts stays open: expected {5.0}, got {layout.SignedDistance(SurfaceKind.Building, new(25f, 0f), 50f)}.").IsTrue();

        var masks = GroundMasks.Build(TestWorld.Context(world, 128f));
        byte Shelter(float x, float z) { var (px, pz) = masks.Pixel(new(x, z)); return masks.Read(MaskChannel.Shelter, px, pz); }
        AssertThat(Shelter(0.5f, 0.5f) == 255).OverrideFailureMessage("The courtyard is open to rain and snow.").IsTrue();
        AssertThat(Shelter(-13.5f, 0.5f) == 0).OverrideFailureMessage("Under the block is sheltered.").IsTrue();
        AssertThat(Shelter(35.5f, 0.5f) == 0).OverrideFailureMessage("Under the annex is sheltered (a part of a multi-part parent).").IsTrue();
        AssertThat(Shelter(25.5f, 0.5f) == 255).OverrideFailureMessage("The gap between the parts is exposed.").IsTrue();

        foreach (var lod in new[] { LodLevel.L0, LodLevel.L2 })
        {
            var tile = new GroundModule().Build(TestWorld.Context(world), new TileKey(0, 0), lod);
            var centres = new List<Vector2>();
            foreach (var mesh in tile.Meshes)
            {
                mesh.ValidateVertexData();
                for (var t = 0; t < mesh.Index.Length; t += 3)
                {
                    Vector2 P(int k) => new(mesh.Position[3 * mesh.Index[t + k]], mesh.Position[3 * mesh.Index[t + k] + 2]);
                    centres.Add((P(0) + P(1) + P(2)) / 3f);
                }
            }
            var deepest = centres.MinBy(p => layout.SignedDistance(SurfaceKind.Building, p, 50f));
            AssertThat(centres.All(p => layout.SignedDistance(SurfaceKind.Building, p, 50f) >= -TestTolerance.Layout))
                .OverrideFailureMessage($"{lod}: buildings replace the lawn beneath every part; deepest triangle centre {deepest}, distance {layout.SignedDistance(SurfaceKind.Building, deepest, 50f)} m.").IsTrue();
            AssertThat(centres.Any(p => p.X > 0f && p.X < 6f && p.Y > 0f && p.Y < 6f) &&
                centres.Any(p => p.X > 20f && p.X < 30f && p.Y > 0f && p.Y < 5f))
                .OverrideFailureMessage($"{lod}: courtyard lawn and the open gap between parts stay visible.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void MixedPoiParentIsClassifiedPartByPart()
    {
        // A poi entity (a park) that owns building parts, a car park, a track and a pool: the parent's kind says nothing
        // about its parts. Type properties come from each part's own record: the entity has no highway or sport.
        var parts = new[]
        {
            Part("osm:way:10", """{"kind":"poi"}""", Rect(-10f, -10f, 10f, 10f)),
            Part("osm:way:11", Building, Rect(-8f, -8f, -2f, -2f)),
            Part("osm:way:12", Building, Rect(2f, 2f, 8f, 8f), Rect(30f, 30f, 36f, 36f)),
            Part("osm:way:13", """{"kind":"parking"}""", Rect(12f, 0f, 20f, 6f)),
            Part("osm:way:14", """{"kind":"sport","sport":"running"}""", Rect(40f, 0f, 60f, 10f)),
            Part("osm:way:15", """{"kind":"sport","sport":"swimming"}""", Rect(40f, 20f, 50f, 30f)),
            Part("osm:way:16", """{"kind":"road","highway":"residential"}""", Rect(-30f, 40f, 30f, 46f)),
            Part("osm:way:17", """{"kind":"road","highway":"footway"}""", Rect(-30f, 50f, 30f, 52f)),
        };
        var world = TestWorld.Of([Entity("test:way:1", "poi", """{"id":"test:way:1","kind":"poi"}""", parts)]);

        var (shapes, _) = GroundSources.Read(world);
        var kinds = shapes.GroupBy(s => s.Kind).ToDictionary(g => g.Key, g => g.Count());
        AssertThat(shapes.Count == 8).OverrideFailureMessage($"Eight ground polygons, none from the poi part; got {shapes.Count}.").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Building) == 3).OverrideFailureMessage("Three building polygons (one part holds two).").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Parking) == 1).OverrideFailureMessage("Parking part.").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Track) == 1).OverrideFailureMessage("Running track part (sport read from the part).").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Water) == 1).OverrideFailureMessage("Swimming pool part (sport read from the part).").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Carriageway) == 1).OverrideFailureMessage("Residential road part (highway read from the part).").IsTrue();
        AssertThat(kinds.GetValueOrDefault(SurfaceKind.Walk) == 1).OverrideFailureMessage("Footway part.").IsTrue();

        var layout = GroundLayout.Of(world);
        AssertThat(layout.SignedDistance(SurfaceKind.Building, new(-5f, -5f), 20f) < 0f).OverrideFailureMessage("Building part keeps its footprint.").IsTrue();
        AssertThat(layout.SignedDistance(SurfaceKind.Building, new(33f, 33f), 20f) < 0f).OverrideFailureMessage("Second polygon of a part keeps its footprint.").IsTrue();
        AssertThat(layout.SignedDistance(SurfaceKind.Building, new(0f, 0f), 20f) > 0f).OverrideFailureMessage("The poi part itself is no footprint.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SingleSourceFeaturesReadTheSameWithOrWithoutParts()
    {
        foreach (var authored in new[] { TestWorld.Street(), TestWorld.Junction() })
        {
            // The same features, each wrapped as its only part with the same props: the old single-source path.
            var wrapped = TestWorld.Of(authored.Features.Values.Select(f => f with
            {
                Parts = [new WorldFeaturePart(f.Id.Value, f.Geometry, f.Height, f.Storeys) { Props = f.Props }]
            }), authored.Network);
            var a = GroundSources.Read(authored);
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
            var ground = new GroundModule().Build(TestWorld.Context(authored), new TileKey(0, 0), LodLevel.L0);
            var again = new GroundModule().Build(TestWorld.Context(wrapped), new TileKey(0, 0), LodLevel.L0);
            AssertThat(ground.Meshes.Count == again.Meshes.Count).OverrideFailureMessage("Bundle count differs.").IsTrue();
            for (var k = 0; k < ground.Meshes.Count; k++)
                AssertThat(ground.Meshes[k].Position.SequenceEqual(again.Meshes[k].Position) &&
                    ground.Meshes[k].Index.SequenceEqual(again.Meshes[k].Index)).OverrideFailureMessage($"Mesh {k} differs.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void UnsupportedGroundGeometryIsRejected()
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
        // The same failure through the public build path is not swallowed on the way.
        Exception? buildError = null;
        try { new GroundModule().Build(TestWorld.Context(withParts), new TileKey(0, 0), LodLevel.L0); }
        catch (Exception caught) { buildError = caught; }
        AssertThat(buildError).OverrideFailureMessage("Expected the unsupported geometry to be rejected.").IsNotNull();
        AssertThat(buildError!).IsInstanceOf<InvalidOperationException>();
        var withoutParts = TestWorld.Of([new WorldFeature(new FeatureId("test:way:8"), new DesignKind("road"), odd)
            { Props = Json("""{"highway":"service"}""") }]);
        Exception? plainError = null;
        try { GroundSources.Read(withoutParts); }
        catch (Exception caught) { plainError = caught; }
        AssertThat(plainError).OverrideFailureMessage("Expected the unsupported geometry to be rejected.").IsNotNull();
        AssertThat(plainError!).IsInstanceOf<InvalidOperationException>();
        // A kind that never touches the ground is not read at all, whatever its geometry.
        var green = TestWorld.Of([Entity("test:way:9", "green", """{"kind":"green"}""",
            new WorldFeaturePart("osm:way:90", odd, 0f, null) { Props = Json("""{"kind":"green"}""") })]);
        AssertThat(GroundSources.Read(green).Shapes.Count == 0).OverrideFailureMessage("Green parts leave the lawn untouched.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AnchorOnlyEntitiesHaveNoGroundExtent()
    {
        // Entities known only by their anchor (no footprint in the data) arrive as point geometry, with or without a
        // part record: nothing to shape, and nothing to reject.
        var anchor = new PointGeometry(new Vector3(10f, 0f, 10f));
        var world = TestWorld.Of(
        [
            WorldFeatureFixture.Create("test:way:20", "building", anchor, Building),
            WorldFeatureFixture.Create("test:way:21", "water", anchor),
            Entity("test:way:22", "poi", """{"kind":"poi"}""", new WorldFeaturePart("osm:way:220", anchor, null, null) { Props = Json(Building) }),
        ]);
        AssertThat(GroundSources.Read(world).Shapes.Count == 0).OverrideFailureMessage("Point geometry yields no ground shape.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    [TestCategory("Slow")]
    public void RealCampusIsReadPartByPart()
    {
        var world = RealMapFixture.Load().World;
        var shapes = GroundSources.Read(world).Shapes;
        var buildingParts = world.Features.Values.SelectMany(feature => feature.Parts
            .Where(part => (part.Props is { } props && props.TryGetProperty("kind", out var kind)
                ? kind.GetString() : feature.Kind.Value) == "building")
            .Select(part => (Feature: feature, Part: part))).ToArray();
        AssertThat(buildingParts.Length > 0).IsTrue();
        foreach (var (feature, part) in buildingParts)
        {
            var polygons = part.Geometry switch
            {
                PolygonGeometry polygon => new[] { polygon.Rings },
                MultiPolygonGeometry multi => multi.Polygons.ToArray(),
                _ => Array.Empty<PolygonRings>()
            };
            var footprints = shapes.Where(shape => shape.Id == feature.Id && shape.PartSourceId == part.SourceId).ToArray();
            AssertThat(footprints.Length == polygons.Length).OverrideFailureMessage($"Building part {part.SourceId} retains every footprint.").IsTrue();
            foreach (var polygon in polygons)
                AssertThat(footprints.Any(shape => shape.Kind == SurfaceKind.Building && shape.Rings is { } rings &&
                    rings[0].SequenceEqual(polygon.Outer) && rings.Length == polygon.Holes.Count + 1 &&
                    rings.Skip(1).Zip(polygon.Holes).All(pair => pair.First.SequenceEqual(pair.Second))))
                    .OverrideFailureMessage($"Building part {part.SourceId} retains its outline and holes.").IsTrue();
        }
    }

    // ---------------------------------------------------------------- helpers

    private sealed record Unsupported : WorldGeometry;

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
        WorldGeometry geometry = polygons.Length switch
        {
            0 => new PointGeometry(Vector3.Zero),
            1 => new PolygonGeometry(polygons[0]),
            _ => new MultiPolygonGeometry(polygons)
        };
        return WorldFeatureFixture.Create(id, kind, geometry, props,
            height: 10f, parts: parts.OrderBy(p => p.SourceId, StringComparer.Ordinal).ToArray());
    }
}
