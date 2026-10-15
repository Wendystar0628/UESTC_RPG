using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Walking collision for the ground (SYS-WALK 13): one 65 x 65 height field per 64 m cell, 1 m spacing, heights in cm,
/// sampled from the same lattice and the same surface rules as the visible L0 mesh; plus the 0.5 m footstep grid
/// (128 x 128 bytes, SYS-AUDIO 9.3). Row-major with x fastest: index = z * 65 + x (HeightMapShape3D map_data order).
/// Triangulation diagonal of each 1 m cell: (0, 0)-(1, 1), the same diagonal the L0 mesh uses (to be verified in P0).
/// </summary>
public static class GroundCollision
{
    public const int Samples = 65;
    public const int FootCells = 128;

    public static HeightFieldCollider Build(BuildContext ctx, TileKey tile, GroundSection? section = null, IGroundTerrain? terrain = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        section ??= GroundSection.Default;
        var layout = GroundLayout.Of(ctx.World);
        var extent = GroundLod.Tile(tile, LodLevel.L0, ctx.Map.DataRect);
        var grid = TerrainGrid.For(terrain ?? FlatTerrain.Instance, tile, LodLevel.L0, extent);
        var field = TileField.Build(layout, extent, section, grid, LawnNoise.Seed(ctx));
        var mesher = new GroundMesher(field, section, LodLevel.L0, layout, grid, new MeshSink());
        var step = field.N / (Samples - 1);          // lattice points per metre (4 at 0.25 m)
        var cm = new short[Samples * Samples];
        for (var j = 0; j < Samples; j++)
            for (var i = 0; i < Samples; i++)
                cm[j * Samples + i] = (short)MathF.Round(mesher.HeightAt(i * step, j * step) * 100f);
        var foot = new byte[FootCells * FootCells];
        var half = field.N / FootCells;               // lattice points per 0.5 m (2 at 0.25 m)
        for (var j = 0; j < FootCells; j++)
            for (var i = 0; i < FootCells; i++)
            {
                // Cell centre: (i + 0.5) * 0.5 m lies on the 0.25 m lattice.
                var surface = mesher.SurfaceAt(i * half + half / 2, j * half + half / 2);
                foot[j * FootCells + i] = (byte)GroundPalette.FootOf(surface);
            }
        return new HeightFieldCollider(tile, cm, SurfaceTag.Ground)
        {
            Foot = Contracts.Audio.FootMat.Grass,
            FootMatHalfMeter = foot
        };
    }

    /// <summary>Height of the collision surface at a world point, interpolated on the same diagonal split.</summary>
    public static float SampleHeight(HeightFieldCollider collider, float x, float z)
    {
        ArgumentNullException.ThrowIfNull(collider);
        var lx = x - collider.Tile.X * GroundLod.TileSize;
        var lz = z - collider.Tile.Z * GroundLod.TileSize;
        var i = Math.Clamp((int)MathF.Floor(lx), 0, Samples - 2);
        var j = Math.Clamp((int)MathF.Floor(lz), 0, Samples - 2);
        var fx = Math.Clamp(lx - i, 0f, 1f);
        var fz = Math.Clamp(lz - j, 0f, 1f);
        float H(int a, int b) => collider.HeightsCm[(j + b) * Samples + i + a] * 0.01f;
        // Diagonal (0,0)-(1,1): lower-right triangle when fx >= fz.
        return fx >= fz
            ? H(0, 0) + fx * (H(1, 0) - H(0, 0)) + fz * (H(1, 1) - H(1, 0))
            : H(0, 0) + fz * (H(0, 1) - H(0, 0)) + fx * (H(1, 1) - H(0, 1));
    }
}
