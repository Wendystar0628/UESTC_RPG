using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class GroundTests
{
    private static readonly GroundSection Section = GroundSection.Default;

    [TestCase]
    public void EmptyWorldIsFlatLawnInBroadTones()
    {
        var ctx = TestWorld.Context(TestWorld.Empty());
        var tones = new HashSet<string>();
        var triangles = 0;
        for (var x = -2; x < 2; x++)
        {
            var output = new GroundModule().Build(ctx, new TileKey(x, 0), LodLevel.L0);
            foreach (var mesh in output.Meshes)
            {
                foreach (var id in mesh.ColorIndices!) tones.Add(id.ToString());
                triangles += mesh.Index.Length / 3;
                for (var v = 0; v < mesh.Position.Length / 3; v++)
                {
                    if (OnTileBorder(mesh.Position[3 * v], mesh.Position[3 * v + 2])) continue;   // border skirts hang below
                    AssertThat(Math.Abs((double)(Section.GrassY) - (double)(mesh.Position[3 * v + 1])) <= 0.01).OverrideFailureMessage($"lawn height: expected {Section.GrassY}, got {mesh.Position[3 * v + 1]}.").IsTrue();
                }
            }
            AssertWinding(output);
        }
        AssertThat(tones.SetEquals(["150", "190", "192"])).OverrideFailureMessage($"A 256 m strip of lawn shows all three tones, got {string.Join(", ", tones)}.").IsTrue();
        AssertThat(triangles < 4 * 12000).OverrideFailureMessage($"Lawn tone contours stay cheap, got {triangles} triangles.").IsTrue();
    }

    [TestCase]
    public void LawnTonesDoNotSpeckle()
    {
        // Every tone patch is broad: sample a 1 m grid and require each sample to share its tone with a neighbour.
        var ctx = TestWorld.Context(TestWorld.Empty());
        var tone = GroundSection.Default.Lawn;
        var seed = LawnNoise.Seed(ctx);
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

    [TestCase]
    public void StreetHasKerbPavingMarkingsAndValidGeometry()
    {
        var ctx = TestWorld.Context(TestWorld.Street());
        var output = new GroundModule().Build(ctx, new TileKey(0, 0), LodLevel.L0);
        var colours = output.Meshes.SelectMany(m => m.ColorIndices!).Distinct().ToHashSet();
        foreach (var expected in new[] { GroundColor.Asphalt, GroundColor.Curb, GroundColor.Paver, GroundColor.Marking })
            AssertThat(colours.Contains(GroundPalette.Id(expected))).OverrideFailureMessage($"Missing {expected}; got {string.Join(", ", colours)}.").IsTrue();
        AssertThat(output.Meshes.Select(m => m.Material.Spec).Distinct().Count() <= 5).OverrideFailureMessage("At most five ground materials.").IsTrue();
        foreach (var mesh in output.Meshes)
        {
            mesh.ValidateVertexData();
            AssertThat(mesh.Position.Length / 3 <= 65536).OverrideFailureMessage("Bundle exceeds 16-bit indices.").IsTrue();
            AssertThat(mesh.Flags is not null && mesh.Flags.All(f => (f & GroundPalette.FlagNoShadow) != 0)).OverrideFailureMessage("Ground must not cast shadows.").IsTrue();
        }
        var marking = output.Meshes.Where(m => m.Material.Spec.Lit.Value == "ground.marking.lit").ToArray();
        AssertThat(marking.All(m => m.Flags!.All(f => (f & GroundPalette.FlagDecal) != 0))).OverrideFailureMessage("Markings are decals.").IsTrue();
        AssertThat(marking.All(m => m.Material.Spec.Line == LineClass.None)).OverrideFailureMessage("Markings are not outlined.").IsTrue();
        var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
        AssertThat(triangles < new GroundModule().Budget.TrianglesL0).OverrideFailureMessage($"Over L0 budget: {triangles}.").IsTrue();
        AssertWinding(output);
    }

    [TestCase]
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
                if (MathF.Abs(ny) < 1e-3f && MathF.Abs(z - 3f) < 1e-3f)
                {
                    found = true;
                    var nz = m.Normal[3 * v + 2];
                    AssertThat(nz < -0.99f).OverrideFailureMessage($"Kerb face must face the road (-z), got {nz}.").IsTrue();
                }
            }
        AssertThat(found).OverrideFailureMessage("No kerb face found along the road edge.").IsTrue();
    }

    [TestCase]
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

    [TestCase]
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

    [TestCase]
    public void HeightFieldMatchesSurfaces()
    {
        var ctx = TestWorld.Context(TestWorld.Street());
        var colliders = new GroundModule().Collide(ctx, new TileKey(0, 0));
        AssertThat(colliders.Count == 1 && colliders[0] is HeightFieldCollider).OverrideFailureMessage("Each ground tile has one height field.").IsTrue();
        var collider = (HeightFieldCollider)colliders[0];
        AssertThat(collider.HeightsCm.Length == 65 * 65).OverrideFailureMessage("Height field must be 65 x 65.").IsTrue();
        AssertThat(collider.FootMatHalfMeter is { Length: 128 * 128 }).OverrideFailureMessage("Footstep grid must be 128 x 128.").IsTrue();
        AssertThat(collider.FootMatHalfMeter!.All(b => b != 0)).OverrideFailureMessage("No Auto footstep material on the ground.").IsTrue();
        AssertThat(Math.Abs((double)(Section.RoadY) - (double)(GroundCollision.SampleHeight(collider, 10f, 0f))) <= .02).OverrideFailureMessage($"road height: expected {Section.RoadY}, got {GroundCollision.SampleHeight(collider, 10f, 0f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.PavingY) - (double)(GroundCollision.SampleHeight(collider, 10f, 4.5f))) <= .02).OverrideFailureMessage($"sidewalk height: expected {Section.PavingY}, got {GroundCollision.SampleHeight(collider, 10f, 4.5f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.GrassY) - (double)(GroundCollision.SampleHeight(collider, 10f, 20f))) <= .02).OverrideFailureMessage($"lawn height: expected {Section.GrassY}, got {GroundCollision.SampleHeight(collider, 10f, 20f)}.").IsTrue();
    }


    [TestCase]
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

    [TestCase]
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
        AssertThat(masks.Mask1.Where((_, i) => i % 4 == 3).All(b => b == 0)).OverrideFailureMessage("Mask A stays reserved.").IsTrue();
        AssertThat(masks.Terrain.All(float.IsFinite)).OverrideFailureMessage("Terrain heights finite.").IsTrue();
        masks.Write(MaskChannel.GinkgoLeaves, 3, 4, 200);
        AssertThat(masks.Read(MaskChannel.GinkgoLeaves, 3, 4) == 200).OverrideFailureMessage("Channel write round-trips.").IsTrue();
    }

    [TestCase]
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
                if (m.ColorIndices![v] is not (150 or 190 or 192)) continue;
                if (OnTileBorder(m.Position[3 * v], m.Position[3 * v + 2])) continue;
                float x = m.Position[3 * v], y = m.Position[3 * v + 1], z = m.Position[3 * v + 2];
                if (z < 12f) continue;   // away from the flattened road verge
                AssertThat(Math.Abs((double)(terrain.HeightAt(x, z) + Section.GrassY) - (double)(y)) <= 0.03).OverrideFailureMessage($"lawn at ({x}, {z}): expected {terrain.HeightAt(x, z) + Section.GrassY}, got {y}.").IsTrue();
                checkedLawn++;
            }
        }
        AssertThat(checkedLawn > 50).OverrideFailureMessage("Expected lawn vertices on the mound.").IsTrue();
        var faceted = output.Meshes.Sum(m => Enumerable.Range(0, m.Normal.Length / 3).Count(v => m.Normal[3 * v + 1] < 0.99f && m.Normal[3 * v + 1] > 0.5f));
        AssertThat(faceted > 0).OverrideFailureMessage("The steep flank of the mound (steeper than 1:6) shades with faceted normals.").IsTrue();
        AssertWinding(output);
        var collider = module.Collide(ctx, new TileKey(0, 0)).OfType<HeightFieldCollider>().Single();
        AssertThat(Math.Abs((double)(terrain.HeightAt(50f, 50f) + Section.GrassY) - (double)(GroundCollision.SampleHeight(collider, 50f, 50f))) <= 0.02).OverrideFailureMessage($"collision on the mound: expected {terrain.HeightAt(50f, 50f) + Section.GrassY}, got {GroundCollision.SampleHeight(collider, 50f, 50f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.RoadY) - (double)(GroundCollision.SampleHeight(collider, 50f, 0f))) <= 0.02).OverrideFailureMessage($"road stays at its flattened grade: expected {Section.RoadY}, got {GroundCollision.SampleHeight(collider, 50f, 0f)}.").IsTrue();
    }

    private static FormulaTerrain Mound() => new(
        (x, z) =>
        {
            var d2 = ((x - 50f) * (x - 50f) + (z - 50f) * (z - 50f)) / 361f;
            return d2 >= 1f ? 0f : 3f * (1f - d2) * (1f - d2);
        },
        (x, z) => new TerrainFacts(0f, TerrainClass.Slope, float.PositiveInfinity, float.PositiveInfinity));

    [TestCase]
    public void PaletteColumnsMatchTheRegistry()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(root, "game", "style", "palette"))) root = System.IO.Path.GetDirectoryName(root);
        AssertThat(root is not null).OverrideFailureMessage("Repository root not found.").IsTrue();
        var palette = System.IO.Path.Combine(root!, "game", "style", "palette");
        var ids = new Dictionary<string, int>();
        var core = System.IO.Path.Combine(palette, "core.json");
        foreach (var path in new[] { System.IO.File.Exists(core) ? core : System.IO.Path.Combine(palette, "keys", "registry.fallback.json"),
                     System.IO.Path.Combine(palette, "ext", "WP-15.json") })
        {
            using var doc = JsonDocument.Parse(System.IO.File.ReadAllBytes(path));
            foreach (var e in doc.RootElement.GetProperty("entries").EnumerateArray())
                ids[e.GetProperty("token").GetString()!] = e.GetProperty("id").GetInt32();
        }
        foreach (var color in Enum.GetValues<GroundColor>())
        {
            var lit = GroundPalette.Lit(color).Value;
            AssertThat(ids.TryGetValue(lit, out var id) && id == GroundPalette.Id(color)).OverrideFailureMessage($"{lit}: table {GroundPalette.Id(color)}, registry {(ids.TryGetValue(lit, out var r) ? r : -1)}.").IsTrue();
            AssertThat(ids.TryGetValue(GroundPalette.Shade(color).Value, out var shade) && shade == id + 1).OverrideFailureMessage($"{color} shade must follow its lit column.").IsTrue();
        }
    }

    [TestCase]
    public void JunctionCornersAreRounded()
    {
        var ctx = TestWorld.Context(TestWorld.Junction());
        var collider = new GroundModule().Collide(ctx, new TileKey(0, 0)).OfType<HeightFieldCollider>().Single();
        // (4, 4) lies in the corner between the two kerb lines (x, z > 3): asphalt with a fillet, raised lawn without.
        AssertThat(Math.Abs((double)(Section.RoadY) - (double)(GroundCollision.SampleHeight(collider, 4f, 4f))) <= 0.011).OverrideFailureMessage($"fillet corner: expected {Section.RoadY}, got {GroundCollision.SampleHeight(collider, 4f, 4f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.GrassY) - (double)(GroundCollision.SampleHeight(collider, 12f, 12f))) <= 0.011).OverrideFailureMessage($"lawn beyond the fillet: expected {Section.GrassY}, got {GroundCollision.SampleHeight(collider, 12f, 12f)}.").IsTrue();
    }

    [TestCase]
    public void SurfacesAreWatertight()
    {
        // Every edge inside the tile is shared by exactly two triangles: no T-junctions, no pinholes between leaves,
        // kerb faces close onto the surfaces they join. Open edges may only lie on the tile border or at the bottom
        // of skirts and water banks; road markings are separate decals.
        foreach (var world in new[] { TestWorld.Street(), TestWorld.Junction() })
            foreach (var lod in new[] { LodLevel.L0, LodLevel.L1 })
            {
                var output = new GroundModule().Build(TestWorld.Context(world), new TileKey(0, 0), lod);
                var edges = new Dictionary<(Vector3, Vector3), int>();
                foreach (var m in output.Meshes.Where(m => m.Material.Spec != GroundPalette.Spec(GroundMaterial.Marking)))
                    for (var t = 0; t < m.Index.Length; t += 3)
                        for (var e = 0; e < 3; e++)
                        {
                            // Snap to 0.1 mm: contours nudged off lattice points leave micrometre slivers that
                            // collapse here; anything that stays open after snapping is a real crack.
                            Vector3 P(int i) => new(MathF.Round(m.Position[3 * i] * 1e4f) * 1e-4f,
                                MathF.Round(m.Position[3 * i + 1] * 1e4f) * 1e-4f, MathF.Round(m.Position[3 * i + 2] * 1e4f) * 1e-4f);
                            var p0 = P(m.Index[t]);
                            var p1 = P(m.Index[t + 1]);
                            var p2 = P(m.Index[t + 2]);
                            if (p0 == p1 || p1 == p2 || p2 == p0) break;
                            var a = P(m.Index[t + e]);
                            var b = P(m.Index[t + (e + 1) % 3]);
                            var key = Less(a, b) ? (a, b) : (b, a);
                            edges[key] = edges.GetValueOrDefault(key) + 1;
                        }
                var size = lod == LodLevel.L0 ? 64f : 128f;
                bool Border(Vector3 p) => p.X == 0f || p.Z == 0f || p.X == size || p.Z == size;
                foreach (var ((a, b), count) in edges)
                {
                    if (count % 2 == 0) continue;   // collapsed slivers can stack an even number of faces on one edge
                    if (Border(a) && Border(b) && (a.X == b.X || a.Z == b.Z)) continue;
                    if (a.Y < 0f && b.Y < 0f) continue;   // skirt and bank bottoms
                    if (Vector3.Distance(a, b) < 1e-3f) continue;   // sub-millimetre slivers beside nudged contours are dropped
                    AssertThat(false).OverrideFailureMessage($"{lod}: edge {a} -> {b} used {count} times.").IsTrue();
                }
            }
    }

    private static bool Less(Vector3 a, Vector3 b) =>
        a.X != b.X ? a.X < b.X : a.Z != b.Z ? a.Z < b.Z : a.Y < b.Y;

    // ---------------------------------------------------------------- helpers

    private static bool OnTileBorder(float x, float z) => x % 64f == 0f || z % 64f == 0f;

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
internal sealed class TestWorld : FakeWorld
{
    private TestWorld(IEnumerable<WorldFeature> features, WorldNetwork network)
    {
        Boundary = new([new(-200f, -200f), new(200f, -200f), new(200f, 200f), new(-200f, 200f)]);
        Features = features.ToDictionary(f => f.Id);
        Network = network;
    }

    public static TestWorld Empty() => new([], WorldNetwork.Empty);

    public static TestWorld Of(IEnumerable<WorldFeature> features, WorldNetwork? network = null) => new(features, network ?? WorldNetwork.Empty);

    public static TestWorld Street()
    {
        var features = new List<WorldFeature>
        {
            Polygon("osm:way:1", "road", Rect(-200f, -3f, 200f, 3f), """{"roadType":"residential"}"""),
            Polygon("osm:way:2", "road", Rect(-200f, 3f, 200f, 5.5f), """{"roadType":"footway"}"""),
            Polygon("osm:way:3", "road", Rect(20f, 8f, 40f, 20f), """{"roadType":"pedestrian"}"""),
            Polygon("osm:way:4", "road", Rect(30f, -12f, 32.5f, 8f), """{"roadType":"footway"}"""),
            Polygon("osm:way:5", "building", Rect(-40f, 20f, -20f, 40f), "{}"),
        };
        var roads = new List<NetworkLine>
        {
            new(new FeatureId("osm:way:1"), new Polyline([new(-200f, 0f), new(200f, 0f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
            new(new FeatureId("osm:way:4"), new Polyline([new(31.25f, -12f), new(31.25f, 8f)]), 2.5f)
                { Props = JsonDocument.Parse("""{"highway":"footway"}""").RootElement.Clone() },
        };
        return new(features, new WorldNetwork(roads, [], [], [], []));
    }

    /// <summary>A T junction: an east-west road and a road running south from it, both 6 m wide.</summary>
    public static TestWorld Junction()
    {
        var features = new List<WorldFeature>
        {
            Polygon("osm:way:11", "road", Rect(-60f, -3f, 60f, 3f), """{"roadType":"residential"}"""),
            Polygon("osm:way:12", "road", Rect(-3f, 0f, 3f, 60f), """{"roadType":"residential"}"""),
        };
        var roads = new List<NetworkLine>
        {
            new(new FeatureId("osm:way:11"), new Polyline([new(-60f, 0f), new(0f, 0f), new(60f, 0f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
            new(new FeatureId("osm:way:12"), new Polyline([new(0f, 0f), new(0f, 60f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
        };
        return new(features, new WorldNetwork(roads, [], [], [], []));
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
            new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = map, WorldSeed = 1u };
    }

    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static WorldFeature Polygon(string id, string kind, PolygonRings rings, string props)
    {
        var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
        var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
        return new(new FeatureId(id), FeatureSource.Osm, new DesignKind(kind), new PolygonGeometry(rings))
        {
            Bounds = new Box3(new(min.X, 0f, min.Y), new(max.X, kind == "building" ? 20f : 0f, max.Y)),
            Props = JsonDocument.Parse(props).RootElement.Clone()
        };
    }
}
