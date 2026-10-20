using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class GroundTests
{
    private static readonly GroundSection Section = GroundSection.Default;

    [TestCase, TestCategory(TestReason.Contract)]
    public void EmptyWorldIsFlatLawnInBroadTones()
    {
        var ctx = TestWorld.Context(TestWorld.Empty());
        var tones = new HashSet<ushort>();
        var triangles = 0;
        for (var x = -2; x < 2; x++)
        {
            var output = new GroundModule().Build(ctx, new TileKey(x, 0), LodLevel.L0);
            foreach (var mesh in output.Meshes)
            {
                foreach (var id in mesh.ColorIndices!) tones.Add(id);
                triangles += mesh.Index.Length / 3;
                for (var v = 0; v < mesh.Position.Length / 3; v++)
                {
                    if (OnTileBorder(mesh.Position[3 * v], mesh.Position[3 * v + 2])) continue;   // border skirts hang below
                    AssertThat(Math.Abs((double)(Section.GrassY) - (double)(mesh.Position[3 * v + 1])) <= TestTolerance.Position).OverrideFailureMessage($"lawn height: expected {Section.GrassY}, got {mesh.Position[3 * v + 1]}.").IsTrue();
                }
            }
            AssertWinding(output);
        }
        AssertThat(tones.SetEquals(new[] { GroundColor.Grass, GroundColor.GrassLight, GroundColor.GrassDark }.Select(GroundPalette.Id))).OverrideFailureMessage($"A 256 m strip of lawn shows all three tones, got {string.Join(", ", tones)}.").IsTrue();
        AssertThat(triangles < 4 * 12000).OverrideFailureMessage($"Lawn tone contours stay cheap, got {triangles} triangles.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void LawnTonesDoNotSpeckle()
    {
        // Every tone patch is broad: sample a 1 m grid and require each sample to share its tone with a neighbour.
        var ctx = TestWorld.Context(TestWorld.Empty());
        var tone = GroundSection.Default.Lawn;
        var seed = LawnNoise.Seed(ctx.WorldSeed);
        int Tone(float x, float z) { var n = LawnNoise.At(x, z, seed, tone); return n < tone.Low ? 0 : n < tone.High ? 1 : 2; }
        var isolated = 0;
        for (var z = 0; z < 128; z++)
            for (var x = 0; x < 128; x++)
            {
                var t = Tone(x, z);
                if (Tone(x + 1, z) != t && Tone(x - 1, z) != t && Tone(x, z + 1) != t && Tone(x, z - 1) != t) isolated++;
            }
        AssertThat(isolated == 0).OverrideFailureMessage($"{isolated} isolated tone samples.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void StreetHasKerbPavingMarkingsAndValidGeometry()
    {
        var ctx = TestWorld.Context(TestWorld.Street());
        var output = new GroundModule().Build(ctx, new TileKey(0, 0), LodLevel.L0);
        // GeometryBundle uses Material.Spec.Lit when no per-vertex colour array is supplied (scene lettering).
        var colours = output.Meshes.SelectMany(m => m.ColorIndices is { } ids ? ids :
            new[] { ctx.Catalog.Resolve(m.Material.Spec.Lit).Value }).Distinct().ToHashSet();
        foreach (var expected in new[] { GroundColor.Asphalt, GroundColor.Curb, GroundColor.Paver, GroundColor.Marking })
            AssertThat(colours.Contains(GroundPalette.Id(expected))).OverrideFailureMessage($"Missing {expected}; got {string.Join(", ", colours)}.").IsTrue();
        // Asphalt, kerb, plain paving, patterned walk pavers, lawn, markings and the shared scene lettering.
        AssertThat(output.Meshes.Select(m => m.Material.Spec).Distinct().Count() <= 7).OverrideFailureMessage("At most seven ground materials including lettering.").IsTrue();
        foreach (var mesh in output.Meshes)
        {
            mesh.ValidateVertexData();
            AssertThat(mesh.Position.Length / 3 <= 65536).OverrideFailureMessage("Bundle exceeds 16-bit indices.").IsTrue();
            AssertThat(mesh.Flags is not null && mesh.Flags.All(f => (f & VertexFlag.NoShadow) != 0)).OverrideFailureMessage("Ground must not cast shadows.").IsTrue();
        }
        var marking = output.Meshes.Where(m => m.Material.Spec.Lit.Value == "ground.marking.lit").ToArray();
        AssertThat(marking.All(m => m.Flags!.All(f => (f & VertexFlag.Decal) != 0))).OverrideFailureMessage("Markings are decals.").IsTrue();
        AssertThat(marking.All(m => m.Material.Spec.Line == LineClass.None)).OverrideFailureMessage("Markings are not outlined.").IsTrue();
        AssertWinding(output);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void KerbFaceSpansRoadToChamfer()
    {
        var ctx = TestWorld.Context(TestWorld.Street());
        var output = new GroundModule().Build(ctx, new TileKey(0, 0), LodLevel.L0);
        // The kerb on the south side of the road: z = 3 (road edge). Face vertices with horizontal normals there.
        var kerb = output.Meshes.Where(m => m.Material.Spec == GroundPalette.Spec(GroundMaterial.Curb)).ToArray();
        var found = false;
        foreach (var m in kerb)
            for (var v = 0; v < m.Position.Length / 3; v++)
            {
                var ny = m.Normal[3 * v + 1];
                var z = m.Position[3 * v + 2];
                if (MathF.Abs(ny) < 0.02f && MathF.Abs(z - 3f) < TestTolerance.Position)
                {
                    found = true;
                    var nz = m.Normal[3 * v + 2];
                    AssertThat(nz < -0.99f).OverrideFailureMessage($"Kerb face must face the road (-z), got {nz}.").IsTrue();
                }
            }
        AssertThat(found).OverrideFailureMessage("No kerb face found along the road edge.").IsTrue();
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void BuildsAreDeterministic()
    {
        var a = new GroundModule().Build(TestWorld.Context(TestWorld.Street()), new TileKey(0, 0), LodLevel.L0);
        var b = new GroundModule().Build(TestWorld.Context(TestWorld.Street()), new TileKey(0, 0), LodLevel.L0);
        AssertThat(a.Meshes.Count == b.Meshes.Count).OverrideFailureMessage("Bundle count differs.").IsTrue();
        for (var k = 0; k < a.Meshes.Count; k++)
        {
            AssertThat(a.Meshes[k].Position.SequenceEqual(b.Meshes[k].Position)).OverrideFailureMessage("Positions differ.").IsTrue();
            AssertThat(a.Meshes[k].Normal.SequenceEqual(b.Meshes[k].Normal)).OverrideFailureMessage("Normals differ.").IsTrue();
            AssertThat(a.Meshes[k].Index.SequenceEqual(b.Meshes[k].Index)).OverrideFailureMessage("Indices differ.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void TileSeamsShareVertices()
    {
        var ctx = TestWorld.Context(TestWorld.Street());
        var left = new GroundModule().Build(ctx, new TileKey(-1, 0), LodLevel.L0);
        var right = new GroundModule().Build(ctx, new TileKey(0, 0), LodLevel.L0);
        HashSet<(float, float, float)> Seam(ContentOutput o) =>
            o.Meshes.Where(m => m.Material.Spec != GroundPalette.Spec(GroundMaterial.Marking))
                .SelectMany(m => Enumerable.Range(0, m.Position.Length / 3).Select(v => (m.Position[3 * v], m.Position[3 * v + 1], m.Position[3 * v + 2])))
                .Where(p => p.Item1 == 0f).ToHashSet();
        var a = Seam(left);
        var b = Seam(right);
        // Every contour crossing the seam is present on both sides.
        var crossings = a.Where(p => p.Item3 % 0.25f != 0f).ToArray();
        AssertThat(crossings.Length > 0).OverrideFailureMessage("Expected contour vertices on the seam.").IsTrue();
        foreach (var p in crossings)
            AssertThat(b.Contains(p)).OverrideFailureMessage($"Seam vertex {p} missing on the right tile. Near: " +
                string.Join(" ", b.Where(q => MathF.Abs(q.Item3 - p.Item3) < 0.02f).Select(q => $"({q.Item1:R},{q.Item2:R},{q.Item3:R})")) +
                " left near: " + string.Join(" ", a.Where(q => MathF.Abs(q.Item3 - p.Item3) < 0.02f).Select(q => $"({q.Item1:R},{q.Item2:R},{q.Item3:R})"))).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void HeightFieldMatchesSurfaces()
    {
        var ctx = TestWorld.Context(TestWorld.Street());
        var colliders = new GroundModule().Collide(ctx, new TileKey(0, 0));
        AssertThat(colliders.Count == 1 && colliders[0] is HeightFieldCollider).OverrideFailureMessage("Each ground tile has one height field.").IsTrue();
        var collider = (HeightFieldCollider)colliders[0];
        AssertThat(collider.HeightsCm.Length == GroundCollision.Samples * GroundCollision.Samples).OverrideFailureMessage("Height field must be 65 x 65.").IsTrue();
        AssertThat(collider.FootMatHalfMeter?.Length == GroundCollision.FootCells * GroundCollision.FootCells).OverrideFailureMessage("Footstep grid must be 128 x 128.").IsTrue();
        AssertThat(collider.FootMatHalfMeter!.All(b => b != 0)).OverrideFailureMessage("No Auto footstep material on the ground.").IsTrue();
        AssertThat(Math.Abs((double)(Section.RoadY) - (double)(GroundCollision.SampleHeight(collider, 10f, 0f))) <= TestTolerance.Position).OverrideFailureMessage($"road height: expected {Section.RoadY}, got {GroundCollision.SampleHeight(collider, 10f, 0f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.PavingY) - (double)(GroundCollision.SampleHeight(collider, 10f, 4.5f))) <= TestTolerance.Position).OverrideFailureMessage($"sidewalk height: expected {Section.PavingY}, got {GroundCollision.SampleHeight(collider, 10f, 4.5f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.GrassY) - (double)(GroundCollision.SampleHeight(collider, 10f, 20f))) <= TestTolerance.Position).OverrideFailureMessage($"lawn height: expected {Section.GrassY}, got {GroundCollision.SampleHeight(collider, 10f, 20f)}.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    public void AllLevelsBuild()
    {
        var ctx = TestWorld.Context(TestWorld.Street());
        foreach (var lod in new[] { LodLevel.L1, LodLevel.L2, LodLevel.L3 })
        {
            var output = new GroundModule().Build(ctx, new TileKey(0, 0), lod);
            AssertThat(output.Meshes.Count > 0).OverrideFailureMessage($"{lod} produced nothing.").IsTrue();
            foreach (var m in output.Meshes) m.ValidateVertexData();
            AssertWinding(output);
        }
        var whole = new GroundModule().Build(ctx, new TileKey(0, 0), LodLevel.L4);
        foreach (var m in whole.Meshes) m.ValidateVertexData();
        AssertThat(whole.Meshes.Sum(m => m.Index.Length / 3) < 20000).OverrideFailureMessage("L4 is a light whole-map surface.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void MasksFollowSurfaces()
    {
        var ctx = TestWorld.Context(TestWorld.Street(), 128f);
        var masks = GroundMasks.Build(ctx, Section);
        AssertThat(masks.Width == 256 && masks.Height == 256).OverrideFailureMessage($"Mask size {masks.Width} x {masks.Height}.").IsTrue();
        AssertThat(masks.Mask1.Length == 256 * 256 * 4 && masks.Mask2.Length == masks.Mask1.Length).OverrideFailureMessage("RGBA8 buffers.").IsTrue();
        byte Snow(float x, float z) { var (px, pz) = masks.Pixel(new(x, z)); return masks.Read(MaskChannel.Snow, px, pz); }
        byte Shelter(float x, float z) { var (px, pz) = masks.Pixel(new(x, z)); return masks.Read(MaskChannel.Shelter, px, pz); }
        AssertThat(Snow(10.2f, 0.2f) == 51).OverrideFailureMessage("Carriageway snow potential 0.2.").IsTrue();
        AssertThat(Snow(10.2f, 4.7f) == 128).OverrideFailureMessage("Sidewalk snow potential 0.5.").IsTrue();
        AssertThat(Snow(10.2f, 30.2f) == 255).OverrideFailureMessage("Lawn snow potential 1.").IsTrue();
        AssertThat(Shelter(-30.5f, 30.5f) == 0 && Snow(-30.5f, 30.5f) == 0).OverrideFailureMessage("Under a building is sheltered and snowless.").IsTrue();
        AssertThat(Shelter(10.2f, 30.2f) == 255).OverrideFailureMessage("Open lawn is exposed.").IsTrue();
        masks.Write(MaskChannel.GinkgoLeaves, 3, 4, 200);
        AssertThat(masks.Read(MaskChannel.GinkgoLeaves, 3, 4) == 200).OverrideFailureMessage("Channel write round-trips.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void GroundFollowsDesignedTerrain()
    {
        var terrain = Mound();
        var world = TestWorld.Street();
        world.Terrain = terrain;
        var ctx = TestWorld.Context(world);
        var module = new GroundModule(Section);
        var output = module.Build(ctx, new TileKey(0, 0), LodLevel.L0);
        var checkedLawn = 0;
        foreach (var m in output.Meshes)
        {
            m.ValidateVertexData();
            for (var v = 0; v < m.Position.Length / 3; v++)
            {
                var color = m.ColorIndices is { } ids ? ids[v] : ctx.Catalog.Resolve(m.Material.Spec.Lit).Value;
                if (color != GroundPalette.Id(GroundColor.Grass) &&
                    color != GroundPalette.Id(GroundColor.GrassLight) &&
                    color != GroundPalette.Id(GroundColor.GrassDark)) continue;
                if (OnTileBorder(m.Position[3 * v], m.Position[3 * v + 2])) continue;
                float x = m.Position[3 * v], y = m.Position[3 * v + 1], z = m.Position[3 * v + 2];
                if (z < 12f) continue;   // away from the flattened road verge
                AssertThat(Math.Abs((double)(terrain.HeightAt(x, z) + Section.GrassY) - (double)(y)) <= TestTolerance.Layout).OverrideFailureMessage($"lawn at ({x}, {z}): expected {terrain.HeightAt(x, z) + Section.GrassY}, got {y}.").IsTrue();
                checkedLawn++;
            }
        }
        AssertThat(checkedLawn > 50).OverrideFailureMessage("Expected lawn vertices on the mound.").IsTrue();
        var faceted = output.Meshes.Sum(m => Enumerable.Range(0, m.Normal.Length / 3).Count(v => m.Normal[3 * v + 1] < 0.99f && m.Normal[3 * v + 1] > 0.5f));
        AssertThat(faceted > 0).OverrideFailureMessage("The steep flank of the mound (steeper than 1:6) shades with faceted normals.").IsTrue();
        AssertWinding(output);
        var collider = module.Collide(ctx, new TileKey(0, 0)).OfType<HeightFieldCollider>().Single();
        AssertThat(Math.Abs((double)(terrain.HeightAt(50f, 50f) + Section.GrassY) - (double)(GroundCollision.SampleHeight(collider, 50f, 50f))) <= TestTolerance.Position).OverrideFailureMessage($"collision on the mound: expected {terrain.HeightAt(50f, 50f) + Section.GrassY}, got {GroundCollision.SampleHeight(collider, 50f, 50f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.RoadY) - (double)(GroundCollision.SampleHeight(collider, 50f, 0f))) <= TestTolerance.Position).OverrideFailureMessage($"road stays at its flattened grade: expected {Section.RoadY}, got {GroundCollision.SampleHeight(collider, 50f, 0f)}.").IsTrue();
    }

    private static FormulaTerrain Mound() => new(
        (x, z) =>
        {
            var d2 = ((x - 50f) * (x - 50f) + (z - 50f) * (z - 50f)) / 361f;
            return d2 >= 1f ? 0f : 3f * (1f - d2) * (1f - d2);
        },
        (x, z) => new TerrainFacts(0f, TerrainClass.Slope, float.PositiveInfinity, float.PositiveInfinity));

    [TestCase, TestCategory(TestReason.Contract)]
    public void PaletteColumnsMatchTheRegistry()
    {
        var palette = RepositoryFixture.FilePath("game", "style", "palette");
        var ids = new Dictionary<string, int>();
        var core = System.IO.Path.Combine(palette, "core.json");
        foreach (var path in new[] { core,
                     System.IO.Path.Combine(palette, "ext", "ground.json") })
        {
            using var doc = JsonDocument.Parse(System.IO.File.ReadAllBytes(path));
            foreach (var e in doc.RootElement.GetProperty("entries").EnumerateArray())
                ids[e.GetProperty("token").GetString()!] = e.GetProperty("index").GetInt32();
        }
        foreach (var color in Enum.GetValues<GroundColor>())
        {
            var lit = GroundPalette.Lit(color).Value;
            AssertThat(ids.TryGetValue(lit, out var id) && id == GroundPalette.Id(color)).OverrideFailureMessage($"{lit}: table {GroundPalette.Id(color)}, registry {(ids.TryGetValue(lit, out var r) ? r : -1)}.").IsTrue();
            AssertThat(ids.TryGetValue(GroundPalette.Shade(color).Value, out var shade) && shade == id + 1).OverrideFailureMessage($"{color} shade must follow its lit column.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void JunctionCornersAreRounded()
    {
        var ctx = TestWorld.Context(TestWorld.Junction());
        var collider = new GroundModule().Collide(ctx, new TileKey(0, 0)).OfType<HeightFieldCollider>().Single();
        // (4, 4) lies in the corner between the two kerb lines (x, z > 3): asphalt with a fillet, raised lawn without.
        AssertThat(Math.Abs((double)(Section.RoadY) - (double)(GroundCollision.SampleHeight(collider, 4f, 4f))) <= TestTolerance.Position).OverrideFailureMessage($"fillet corner: expected {Section.RoadY}, got {GroundCollision.SampleHeight(collider, 4f, 4f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.GrassY) - (double)(GroundCollision.SampleHeight(collider, 12f, 12f))) <= TestTolerance.Position).OverrideFailureMessage($"lawn beyond the fillet: expected {Section.GrassY}, got {GroundCollision.SampleHeight(collider, 12f, 12f)}.").IsTrue();
    }

    // ---------------------------------------------------------------- helpers

    private static bool OnTileBorder(float x, float z) => x % TileGrid.BaseEdge == 0f || z % TileGrid.BaseEdge == 0f;

    /// <summary>Every top triangle faces up, every face triangle faces horizontally (clockwise front faces).</summary>
    private static void AssertWinding(ContentOutput output)
    {
        foreach (var m in output.Meshes)
            for (var t = 0; t < m.Index.Length; t += 3)
            {
                Vector3 P(int i) => new(m.Position[3 * m.Index[t + i]], m.Position[3 * m.Index[t + i] + 1], m.Position[3 * m.Index[t + i] + 2]);
                Vector3 N(int i) => new(m.Normal[3 * m.Index[t + i]], m.Normal[3 * m.Index[t + i] + 1], m.Normal[3 * m.Index[t + i] + 2]);
                var cross = Vector3.Cross(P(1) - P(0), P(2) - P(0));
                if (cross.Length() < 2e-6f) continue;   // sub-millimetre slivers next to nudged contours carry no orientation
                var n = N(0) + N(1) + N(2);
                if (MathF.Abs(Vector3.Dot(Vector3.Normalize(cross), Vector3.Normalize(n))) < 0.1f) continue;   // border skirts shade as level ground
                AssertThat(Vector3.Dot(cross, n) < 0f).OverrideFailureMessage($"Triangle {t / 3} in {m.Material.Spec.Lit.Value} is back-facing: {P(0)} {P(1)} {P(2)} n {N(0)} {N(1)} {N(2)}.").IsTrue();
            }
    }
}

/// <summary>Small synthetic worlds: an east-west street with kerbs, a sidewalk, a plaza and a crossing footpath.</summary>
internal static class TestWorld
{

    public static FakeWorld Empty() => Of([]);

    /// <summary>Final ground surface at a point, classified on a 0.25 m lattice of the L0 tile that holds it (one tile field per world and tile, shared by the probes).</summary>
    public static GroundSurface SurfaceAt(BuildContext context, float x, float z)
    {
        var key = TileGrid.At(new Vector2(x, z));
        var tile = new GroundTile(key.X * TileGrid.BaseEdge, key.Z * TileGrid.BaseEdge, TileGrid.BaseEdge, TileGrid.BaseEdge / 256f);
        var mesher = context.Memo((typeof(TestWorld), key), () =>
        {
            var layout = GroundLayout.Of(context);
            var grid = TerrainGrid.For(FlatTerrain.Default, key, LodLevel.L0, tile);
            var field = TileField.Build(layout, tile, GroundSection.Default, grid);
            return new GroundMesher(field, GroundSection.Default, LodLevel.L0, layout, grid, new GroundSurfaceSink());
        });
        return mesher.SurfaceAt((int)MathF.Round((x - tile.X0) / tile.Cell), (int)MathF.Round((z - tile.Z0) / tile.Cell));
    }

    public static FakeWorld Of(IEnumerable<WorldFeature> features, WorldNetwork? network = null) => new()
    {
        Boundary = new([new(-200f, -200f), new(200f, -200f), new(200f, 200f), new(-200f, 200f)]),
        Features = features.ToDictionary(f => f.Id),
        Network = network ?? WorldNetwork.Empty,
    };

    public static FakeWorld Street()
    {
        var features = new List<WorldFeature>
        {
            WorldFeatureFixture.Create("osm:way:1", "road", new PolygonGeometry(Rect(-200f, -3f, 200f, 3f)), """{"highway":"residential"}"""),
            WorldFeatureFixture.Create("osm:way:2", "road", new PolygonGeometry(Rect(-200f, 3f, 200f, 5.5f)), """{"highway":"footway"}"""),
            WorldFeatureFixture.Create("osm:way:3", "road", new PolygonGeometry(Rect(20f, 8f, 40f, 20f)), """{"highway":"pedestrian"}"""),
            WorldFeatureFixture.Create("osm:way:4", "road", new PolygonGeometry(Rect(30f, -12f, 32.5f, 8f)), """{"highway":"footway"}"""),
            WorldFeatureFixture.Create("osm:way:5", "building", new PolygonGeometry(Rect(-40f, 20f, -20f, 40f)), "{}", height: 20f),
        };
        var roads = new List<NetworkLine>
        {
            new(new FeatureId("osm:way:1"), new Polyline([new(-200f, 0f), new(200f, 0f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
            new(new FeatureId("osm:way:4"), new Polyline([new(31.25f, -12f), new(31.25f, 8f)]), 2.5f)
                { Props = JsonDocument.Parse("""{"highway":"footway"}""").RootElement.Clone() },
        };
        return Of(features, new WorldNetwork(roads, [], [], [], []));
    }

    /// <summary>A T junction: an east-west road and a road running south from it, both 6 m wide.</summary>
    public static FakeWorld Junction()
    {
        var features = new List<WorldFeature>
        {
            WorldFeatureFixture.Create("osm:way:11", "road", new PolygonGeometry(Rect(-60f, -3f, 60f, 3f)), """{"highway":"residential"}"""),
            WorldFeatureFixture.Create("osm:way:12", "road", new PolygonGeometry(Rect(-3f, 0f, 3f, 60f)), """{"highway":"residential"}"""),
        };
        var roads = new List<NetworkLine>
        {
            new(new FeatureId("osm:way:11"), new Polyline([new(-60f, 0f), new(0f, 0f), new(60f, 0f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
            new(new FeatureId("osm:way:12"), new Polyline([new(0f, 0f), new(0f, 60f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
        };
        return Of(features, new WorldNetwork(roads, [], [], [], []));
    }

    public static BuildContext Context(IWorldModel world, float half = 1280f)
    {
        var rect = new MapRect(new(-half, -half), new(half, half));
        var map = new MapDescriptor(new MapId("test"), new LocalizedName("测试", "Test"), MapKind.Outdoor, 1, 1u,
            new GeoFrame(0d, 0d, 32648, new(-200f, -200f), new(200f, 200f)), Vector2.Zero,
            new Box3(new(-half, -28f, -half), new(half, 2000f, half)), rect,
            new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f),
            new TileKey(-20, -20), new TileKey(19, 19), "P00", [],
            new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
            new MapEnvironment("sky.json", "backdrop.json", []), []);
        return new BuildContext {
            CodeHash = "fixture", WindowNumbering = new((map).Id, new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = map,
            WorldSeed = 1u, SourceInputs = MaterialInputs.Value.Entries, GlobalInputs = GlobalInputs.Value };
    }

    private static readonly Lazy<BuildInputSnapshot> GlobalInputs = new(() => BuildInputSnapshot.Freeze(
        new Dictionary<string, ReadOnlyMemory<byte>>
        {
            [SceneTextInputs.AtlasKey] = System.IO.File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "scene-text", "atlas.json"))
        }, "fixture"));

    private static readonly Lazy<BuildInputSnapshot> MaterialInputs = new(() => BuildInputSnapshot.Freeze(
        new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [SurfaceMaterialCatalog.SourcePath] = System.IO.File.ReadAllBytes(
                System.IO.Path.Combine(RealMapFixture.DataDirectory(), SurfaceMaterialCatalog.SourcePath))
        }, "fixture"));

    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

}
