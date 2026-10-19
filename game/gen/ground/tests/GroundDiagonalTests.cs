using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

/// <summary>
/// Mesh, terrain draping and the collision sampler split every 1 m cell along the same anti-diagonal (1,0)-(0,1), the
/// split Godot 4.7.2's Jolt height-field shape uses (WLD-TERRAIN 9.3). The reference values are native Jolt ray
/// measurements made on a non-coplanar fixture.
/// </summary>
[TestSuite]
public sealed class GroundDiagonalTests
{
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void CollisionSamplerMatchesNativeJoltRays()
    {
        var cm = NativeField();
        var collider = new HeightFieldCollider(NativeTile, cm, SurfaceTag.Ground);
        foreach (var (x, z, jolt) in Native)
        {
            // World inputs formed as the probe forms them, so the local coordinates are the same floats.
            var worldX = NativeTile.X * TileGrid.BaseEdge + x;
            var worldZ = NativeTile.Z * TileGrid.BaseEdge + z;
            var sampled = GroundCollision.SampleHeight(collider, worldX, worldZ);
            AssertThat(MathF.Abs(sampled - jolt) <= TestTolerance.Position).OverrideFailureMessage($"({x}, {z}): sampler {sampled}, native Jolt {jolt}.").IsTrue();
            AssertThat(MathF.Abs(MainDiagonal(cm, worldX - NativeTile.X * TileGrid.BaseEdge, worldZ - NativeTile.Z * TileGrid.BaseEdge) - jolt) > 0.2f).OverrideFailureMessage($"({x}, {z}): the fixture must tell the diagonals apart.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void TerrainDrapingFollowsTheSameDiagonal()
    {
        var ctx = TestWorld.Context(TestWorld.Empty());
        var extent = GroundLod.Tile(NativeTile, LodLevel.L0, ctx.Map.DataRect);
        var grid = TerrainGrid.For(FixtureTerrain(NativeTile, NativeField()), NativeTile, LodLevel.L0, extent);
        foreach (var (x, z, jolt) in Native)
        {
            var draped = grid.At(extent.X0 + x, extent.Z0 + z);
            AssertThat(MathF.Abs(draped - jolt) <= TestTolerance.Position).OverrideFailureMessage($"({x}, {z}): draped {draped}, native Jolt {jolt}.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CellSplitIsTheAntiDiagonal()
    {
        // Corner heights 0, 1, 2, 4 at 00, 10, 01, 11: on the diagonal 10-01 the two triangles agree, off it they differ from the main-diagonal split.
        AssertThat(Math.Abs((double)(1.5) - (double)(CellSplit.Lerp(0f, 1f, 2f, 4f, 0.5f, 0.5f))) <= TestTolerance.Position).OverrideFailureMessage($"diagonal midpoint is the mean of corners 10 and 01: expected {1.5}, got {CellSplit.Lerp(0f, 1f, 2f, 4f, 0.5f, 0.5f)}.").IsTrue();
        AssertThat(Math.Abs((double)(1.0) - (double)(CellSplit.Lerp(0f, 1f, 2f, 4f, 1f, 0f))) <= TestTolerance.Position).OverrideFailureMessage($"corner 10: expected {1.0}, got {CellSplit.Lerp(0f, 1f, 2f, 4f, 1f, 0f)}.").IsTrue();
        AssertThat(Math.Abs((double)(2.0) - (double)(CellSplit.Lerp(0f, 1f, 2f, 4f, 0f, 1f))) <= TestTolerance.Position).OverrideFailureMessage($"corner 01: expected {2.0}, got {CellSplit.Lerp(0f, 1f, 2f, 4f, 0f, 1f)}.").IsTrue();
        AssertThat(Math.Abs((double)(4.0) - (double)(CellSplit.Lerp(0f, 1f, 2f, 4f, 1f, 1f))) <= TestTolerance.Position).OverrideFailureMessage($"corner 11: expected {4.0}, got {CellSplit.Lerp(0f, 1f, 2f, 4f, 1f, 1f)}.").IsTrue();
        AssertThat(Math.Abs((double)(0.25 * 1.0 + 0.25 * 2.0) - (double)(CellSplit.Lerp(0f, 1f, 2f, 4f, 0.25f, 0.25f))) <= TestTolerance.Position).OverrideFailureMessage($"inside 00-10-01: expected {0.25 * 1.0 + 0.25 * 2.0}, got {CellSplit.Lerp(0f, 1f, 2f, 4f, 0.25f, 0.25f)}.").IsTrue();
        AssertThat(Math.Abs((double)(4.0 - 0.25 * 3.0 - 0.25 * 2.0) - (double)(CellSplit.Lerp(0f, 1f, 2f, 4f, 0.75f, 0.75f))) <= TestTolerance.Position).OverrideFailureMessage($"inside 10-11-01: expected {4.0 - 0.25 * 3.0 - 0.25 * 2.0}, got {CellSplit.Lerp(0f, 1f, 2f, 4f, 0.75f, 0.75f)}.").IsTrue();
    }



    [TestCase, TestCategory(TestReason.Regression)]
    public void CutCellsOfTheL0MeshLieOnTheDrapedSurface()
    {
        // Roads and kerbs cut the lawn into small pieces along curves (the junction fillet), where the mesher works on
        // single fine cells instead of whole blocks. Every lawn piece must still lie on the draped terrain surface,
        // so each fine cell has to be split along the same diagonal as the terrain grid.
        var tile = new TileKey(0, 0);
        var terrain = FixtureTerrain(tile, NativeField());
        var world = TestWorld.Junction();
        world.Terrain = terrain;
        var ctx = TestWorld.Context(world);
        var section = GroundSection.Default;
        var extent = GroundLod.Tile(tile, LodLevel.L0, ctx.Map.DataRect);
        var grid = TerrainGrid.For(terrain, tile, LodLevel.L0, extent);
        var field = TileField.Build(GroundLayout.Of(world), extent, section, grid);
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
                var lift = p.Select(LawnLift).ToArray();
                var points = new[]
                {
                    (Point: (p[0] + p[1] + p[2]) / 3f, Lift: (lift[0] + lift[1] + lift[2]) / 3f),
                    (Point: (p[0] + p[1]) / 2f, Lift: (lift[0] + lift[1]) / 2f),
                    (Point: (p[1] + p[2]) / 2f, Lift: (lift[1] + lift[2]) / 2f),
                    (Point: (p[2] + p[0]) / 2f, Lift: (lift[2] + lift[0]) / 2f)
                };
                foreach (var (q, surfaceLift) in points) worst = MathF.Max(worst, MathF.Abs(q.Y - surfaceLift - grid.At(q.X, q.Z)));
                checkedTriangles++;
                if (p.All(q => q.X > 3f && q.X < 8f && q.Z > 3f && q.Z < 8f)) nearArc++;
            }
        AssertThat(checkedTriangles > 500).OverrideFailureMessage($"Only {checkedTriangles} lawn triangles checked.").IsTrue();
        AssertThat(nearArc > 20).OverrideFailureMessage($"Only {nearArc} lawn triangles beside the junction fillet.").IsTrue();
        AssertThat(worst <= 0.01f).OverrideFailureMessage($"Lawn leaves the draped terrain by {worst} m.").IsTrue();

        // The lowered lawn rises to paving at its edge. Read the mesher's same distance field and diagonal;
        // interpolate those vertex lifts along the tested triangle before checking the underlying terrain.
        float LawnLift(Vector3 p)
        {
            var u = (p.X - field.X0) / field.Cell;
            var w = (p.Z - field.Z0) / field.Cell;
            var i = Math.Clamp((int)MathF.Floor(u), 0, field.N - 1);
            var j = Math.Clamp((int)MathF.Floor(w), 0, field.N - 1);
            var distance = CellSplit.Lerp(field.Get(i, j, TileField.LawnEdge), field.Get(i + 1, j, TileField.LawnEdge),
                field.Get(i, j + 1, TileField.LawnEdge), field.Get(i + 1, j + 1, TileField.LawnEdge), u - i, w - j);
            return section.GrassY + (section.PavingY - section.GrassY) *
                (1f - Math.Clamp(distance / section.LawnEdgeRise, 0f, 1f));
        }
    }

    /// <summary>A terrain whose only non-flat tile is <paramref name="tile"/> at level 0, filled from a ready 65 x 65 centimetre field.</summary>
    private static FormulaTerrain FixtureTerrain(TileKey tile, short[] cm) => new(
        (x, z) =>
        {
            var u = x - tile.X * TileGrid.BaseEdge;
            var w = z - tile.Z * TileGrid.BaseEdge;
            if (u < 0f || w < 0f || u > 64f || w > 64f) return 0f;
            var i = Math.Clamp((int)MathF.Floor(u), 0, 63);
            var j = Math.Clamp((int)MathF.Floor(w), 0, 63);
            float H(int a, int b) => cm[(j + b) * 65 + i + a] * 0.01f;
            return CellSplit.Lerp(H(0, 0), H(1, 0), H(0, 1), H(1, 1), u - i, w - j);
        },
        (x, z) => new TerrainFacts(0f, TerrainClass.Slope, float.PositiveInfinity, float.PositiveInfinity),
        other => other != tile,
        (other, level, target) =>
        {
            if (level == 0 && other == tile) cm.CopyTo(target);
            else target.Clear();
        });
}
