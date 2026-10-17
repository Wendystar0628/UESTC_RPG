using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

/// <summary>
/// Mesh, terrain draping and the collision sampler split every 1 m cell along the same anti-diagonal (1,0)-(0,1), the
/// split Godot 4.7.2's Jolt height-field shape uses (WLD-TERRAIN 9.3). The reference values are native Jolt ray
/// measurements made on a non-coplanar fixture.
/// </summary>
[TestSuite]
public sealed class GroundDiagonalTests
{
    private const float MatchToleranceM = 0.002f;   // the native probe's own tolerance
    private static readonly TileKey NativeTile = new(-3, 2);

    // The probe's sample points (tile-local metres) and the heights the real Jolt body returned for them
    // (heightfield-native-measurements.json: x fastest, rows along +Z, anti-diagonal fits to 0.0000112 m, main diagonal 0.36 m off).
    private static readonly (float X, float Z, float JoltY)[] Native =
    [
        (8.2f, 12.3f, 1.7790079f), (8.75f, 12.2f, 1.7850084f), (8.7f, 12.8f, 2.7140021f), (8.25f, 12.8f, 1.8950167f),
        (17.2f, 4.3f, 2.0990086f), (17.7f, 4.8f, 2.1340046f), (4.2f, 47.3f, 3.8089948f), (4.7f, 47.8f, 3.8439937f),
        (53.2f, 22.3f, 3.7190118f), (53.7f, 22.8f, 3.7540064f), (0.125f, 0.375f, 1.0212536f), (63.75f, 63.2f, 5.5250053f),
    ];

    /// <summary>The probe's 65 x 65 cm field: unequal x and z gradients, and a 1.8 m bump on every odd-odd corner so that no cell is coplanar.</summary>
    private static short[] NativeField()
    {
        var heights = new short[65 * 65];
        for (var z = 0; z < 65; z++)
            for (var x = 0; x < 65; x++)
                heights[z * 65 + x] = (short)(100 + 2 * x + 5 * z + ((x & 1) == 1 && (z & 1) == 1 ? 180 : 0));
        return heights;
    }

    /// <summary>The old main-diagonal split, kept here only to prove the fixture tells the two apart.</summary>
    private static float MainDiagonal(short[] cm, float x, float z)
    {
        var i = Math.Clamp((int)MathF.Floor(x), 0, 63);
        var j = Math.Clamp((int)MathF.Floor(z), 0, 63);
        var u = x - i;
        var v = z - j;
        float H(int a, int b) => cm[(j + b) * 65 + i + a] * 0.01f;
        return u >= v ? H(0, 0) + u * (H(1, 0) - H(0, 0)) + v * (H(1, 1) - H(1, 0)) : H(0, 0) + v * (H(0, 1) - H(0, 0)) + u * (H(1, 1) - H(0, 1));
    }

    [TestCase]
    public void CollisionSamplerMatchesNativeJoltRays()
    {
        var cm = NativeField();
        var collider = new HeightFieldCollider(NativeTile, cm, SurfaceTag.Ground);
        foreach (var (x, z, jolt) in Native)
        {
            // World inputs formed as the probe forms them, so the local coordinates are the same floats.
            var worldX = NativeTile.X * GroundLod.TileSize + x;
            var worldZ = NativeTile.Z * GroundLod.TileSize + z;
            var sampled = GroundCollision.SampleHeight(collider, worldX, worldZ);
            Check.True(MathF.Abs(sampled - jolt) <= MatchToleranceM, $"({x}, {z}): sampler {sampled}, native Jolt {jolt}.");
            Check.True(MathF.Abs(MainDiagonal(cm, worldX - NativeTile.X * GroundLod.TileSize, worldZ - NativeTile.Z * GroundLod.TileSize) - jolt) > 0.2f,
                $"({x}, {z}): the fixture must tell the diagonals apart.");
        }
    }

    [TestCase]
    public void TerrainDrapingFollowsTheSameDiagonal()
    {
        var ctx = TestWorld.Context(TestWorld.Empty());
        var extent = GroundLod.Tile(NativeTile, LodLevel.L0, ctx.Map.DataRect);
        var grid = TerrainGrid.For(new FixtureTerrain(NativeTile, NativeField()), NativeTile, LodLevel.L0, extent);
        foreach (var (x, z, jolt) in Native)
        {
            var draped = grid.At(extent.X0 + x, extent.Z0 + z);
            Check.True(MathF.Abs(draped - jolt) <= MatchToleranceM, $"({x}, {z}): draped {draped}, native Jolt {jolt}.");
        }
    }

    [TestCase]
    public void CellSplitIsTheAntiDiagonal()
    {
        // Corner heights 0, 1, 2, 4 at 00, 10, 01, 11: on the diagonal 10-01 the two triangles agree, off it they differ from the main-diagonal split.
        Check.Close(1.5, CellSplit.Lerp(0f, 1f, 2f, 4f, 0.5f, 0.5f), 1e-6, "diagonal midpoint is the mean of corners 10 and 01");
        Check.Close(1.0, CellSplit.Lerp(0f, 1f, 2f, 4f, 1f, 0f), 1e-6, "corner 10");
        Check.Close(2.0, CellSplit.Lerp(0f, 1f, 2f, 4f, 0f, 1f), 1e-6, "corner 01");
        Check.Close(4.0, CellSplit.Lerp(0f, 1f, 2f, 4f, 1f, 1f), 1e-6, "corner 11");
        Check.Close(0.25 * 1.0 + 0.25 * 2.0, CellSplit.Lerp(0f, 1f, 2f, 4f, 0.25f, 0.25f), 1e-6, "inside 00-10-01");
        Check.Close(4.0 - 0.25 * 3.0 - 0.25 * 2.0, CellSplit.Lerp(0f, 1f, 2f, 4f, 0.75f, 0.75f), 1e-6, "inside 10-11-01");
    }

    [TestCase]
    public void L0MeshTrianglesLieOnTheCollisionSurface()
    {
        // The designed terrain is the native fixture on tile (0, 0). Every top triangle of the L0 mesh (both of its
        // cell triangles and every edge midpoint) must sit on the surface the collision sampler describes: a mesh that
        // split cells along the other diagonal would cut across the creases by tenths of a metre.
        var tile = new TileKey(0, 0);
        var terrain = new FixtureTerrain(tile, NativeField());
        var world = TestWorld.Empty();
        world.Terrain = terrain;
        var ctx = TestWorld.Context(world);
        var section = GroundSection.Default;
        var module = new GroundModule(section);
        var support = GroundSupport.TileFor(ctx, tile, section);
        var collider = support.Colliders.OfType<HeightFieldCollider>().Single();
        Check.True(collider.HeightsCm.SequenceEqual(NativeField()), "The base HF must retain the exact FillTile centimetres.");
        var output = module.Build(ctx, tile, LodLevel.L0);
        var checkedTriangles = 0;
        var worst = 0f;
        foreach (var m in output.Meshes)
            for (var t = 0; t < m.Index.Length; t += 3)
            {
                var p = new Vector3[3];
                for (var k = 0; k < 3; k++)
                    p[k] = new(m.Position[3 * m.Index[t + k]], m.Position[3 * m.Index[t + k] + 1], m.Position[3 * m.Index[t + k] + 2]);
                if (p.Any(q => q.X % 64f == 0f || q.Z % 64f == 0f)) continue;   // border skirts hang below
                var cross = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
                if (cross.Length() < 1e-5f || MathF.Abs(cross.Y) < 0.5f * cross.Length()) continue;   // slivers and vertical faces
                var points = new[] { (p[0] + p[1] + p[2]) / 3f, (p[0] + p[1]) / 2f, (p[1] + p[2]) / 2f, (p[2] + p[0]) / 2f };
                foreach (var q in points)
                {
                    var surface = GroundCollision.SampleHeight(collider, q.X, q.Z);
                    worst = MathF.Max(worst, MathF.Abs(q.Y - section.GrassY - surface));
                    Check.Close(q.Y, support.SupportY(q.X, q.Z)!.Value, .011, "actual top support follows the L0 triangle");
                }
                checkedTriangles++;
            }
        Check.True(checkedTriangles > 500, $"Only {checkedTriangles} top triangles checked.");
        // Collision heights are whole centimetres, so allow one centimetre of rounding.
        Check.True(worst <= 0.011f, $"Mesh leaves the collision surface by {worst} m.");
    }

    [TestCase]
    public void CutCellsOfTheL0MeshLieOnTheDrapedSurface()
    {
        // Roads and kerbs cut the lawn into small pieces along curves (the junction fillet), where the mesher works on
        // single fine cells instead of whole blocks. Every lawn piece must still lie on the draped terrain surface,
        // so each fine cell has to be split along the same diagonal as the terrain grid.
        var tile = new TileKey(0, 0);
        var terrain = new FixtureTerrain(tile, NativeField());
        var world = TestWorld.Junction();
        world.Terrain = terrain;
        var ctx = TestWorld.Context(world);
        var section = GroundSection.Default;
        var grid = TerrainGrid.For(terrain, tile, LodLevel.L0, GroundLod.Tile(tile, LodLevel.L0, ctx.Map.DataRect));
        var output = new GroundModule(section).Build(ctx, tile, LodLevel.L0);
        var checkedTriangles = 0;
        var nearArc = 0;
        var worst = 0f;
        foreach (var m in output.Meshes)
            for (var t = 0; t < m.Index.Length; t += 3)
            {
                var p = new Vector3[3];
                for (var k = 0; k < 3; k++) p[k] = new(m.Position[3 * m.Index[t + k]], m.Position[3 * m.Index[t + k] + 1], m.Position[3 * m.Index[t + k] + 2]);
                if (m.Index.Skip(t).Take(3).Any(i => m.ColorIndices![i] is not (150 or 190 or 192))) continue;   // lawn only
                if (p.Any(q => q.X % 64f == 0f || q.Z % 64f == 0f)) continue;
                var cross = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
                if (cross.Length() < 1e-5f || MathF.Abs(cross.Y) < 0.5f * cross.Length()) continue;
                var points = new[] { (p[0] + p[1] + p[2]) / 3f, (p[0] + p[1]) / 2f, (p[1] + p[2]) / 2f, (p[2] + p[0]) / 2f };
                foreach (var q in points) worst = MathF.Max(worst, MathF.Abs(q.Y - section.GrassY - grid.At(q.X, q.Z)));
                checkedTriangles++;
                if (p.All(q => q.X > 3f && q.X < 8f && q.Z > 3f && q.Z < 8f)) nearArc++;
            }
        Check.True(checkedTriangles > 500, $"Only {checkedTriangles} lawn triangles checked.");
        Check.True(nearArc > 20, $"Only {nearArc} lawn triangles beside the junction fillet.");
        Check.True(worst <= 0.001f, $"Lawn leaves the draped terrain by {worst} m.");
    }

    [TestCase]
    public void LawnToneFieldFollowsTheSameDiagonal()
    {
        // The tone field is sampled on a coarse lattice and linear on its triangles; the mesher clips blocks that are
        // affine on the anti-diagonal halves, so the fine values must come from the same split. The noise is gentle
        // (the two splits differ by a few millionths at the sampled points), so the comparison is exact to float rounding.
        var tile = new TileKey(0, 0);
        var ctx = TestWorld.Context(TestWorld.Empty());
        var extent = GroundLod.Tile(tile, LodLevel.L0, ctx.Map.DataRect);
        var grid = TerrainGrid.For(FlatTerrain.Default, tile, LodLevel.L0, extent);
        var field = TileField.Build(GroundLayout.Of(ctx.World), extent, GroundSection.Default, grid, LawnNoise.Seed(ctx), GroundLod.ToneCell(LodLevel.L0));
        var m = field.ToneMacro;
        Check.True(m >= 4, $"Tone macro cell of {m} fine cells is too small to test.");
        var spread = 0f;
        for (var cj = 0; cj < 3; cj++)
            for (var ci = 0; ci < 3; ci++)
            {
                float T(int a, int b) => field.Get((ci + a) * m, (cj + b) * m, TileField.Tone);
                foreach (var (a, b) in new[] { (1, 1), (m - 1, 1), (1, m - 1), (m / 2, m / 2 - 1), (m - 1, m - 1), (m / 2 + 1, m / 2 + 1) })
                {
                    float u = (float)a / m, w = (float)b / m;
                    var anti = u + w <= 1f
                        ? T(0, 0) + u * (T(1, 0) - T(0, 0)) + w * (T(0, 1) - T(0, 0))
                        : T(1, 1) + (1f - w) * (T(1, 0) - T(1, 1)) + (1f - u) * (T(0, 1) - T(1, 1));
                    var main = u >= w
                        ? T(0, 0) + u * (T(1, 0) - T(0, 0)) + w * (T(1, 1) - T(1, 0))
                        : T(0, 0) + w * (T(0, 1) - T(0, 0)) + u * (T(1, 1) - T(0, 1));
                    var actual = field.Get(ci * m + a, cj * m + b, TileField.Tone);
                    Check.Close(anti, actual, 2e-7, $"tone at cell ({ci}, {cj}) offset ({a}, {b})");
                    spread = MathF.Max(spread, MathF.Abs(main - anti));
                }
            }
        Check.True(spread > 1e-6f, $"The sampled tone values must tell the two diagonals apart; they differ by at most {spread}.");
    }

    /// <summary>A terrain whose only non-flat tile is <paramref name="tile"/> at level 0, filled from a ready 65 x 65 centimetre field.</summary>
    private sealed class FixtureTerrain(TileKey tile, short[] cm) : ITerrain
    {
        public float PadY(FeatureId feature) => 0f;
        public float? WaterY(FeatureId water) => null;
        public TerrainFacts FactsAt(float x, float z) => new(0f, TerrainClass.Slope, float.PositiveInfinity, float.PositiveInfinity);
        public bool IsFlat(TileKey other) => other != tile;

        public float HeightAt(float x, float z)
        {
            var u = x - tile.X * GroundLod.TileSize;
            var w = z - tile.Z * GroundLod.TileSize;
            if (u < 0f || w < 0f || u > 64f || w > 64f) return 0f;
            var i = Math.Clamp((int)MathF.Floor(u), 0, 63);
            var j = Math.Clamp((int)MathF.Floor(w), 0, 63);
            float H(int a, int b) => cm[(j + b) * 65 + i + a] * 0.01f;
            return CellSplit.Lerp(H(0, 0), H(1, 0), H(0, 1), H(1, 1), u - i, w - j);
        }

        public void FillTile(TileKey other, int level, Span<short> target)
        {
            if (level == 0 && other == tile) cm.CopyTo(target);
            else target.Clear();
        }
    }
}
