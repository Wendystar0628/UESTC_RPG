using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Walking collision for the ground (SYS-WALK 13): one 65 x 65 height field per 64 m cell, 1 m spacing, heights in cm,
/// sampled from the terrain and ground cross-section; plus the 0.5 m footstep grid
/// (128 x 128 bytes, SYS-AUDIO 9.3). Row-major with x fastest: index = z * 65 + x (HeightMapShape3D map_data order).
/// Triangulation diagonal of each 1 m cell: the anti-diagonal (1, 0)-(0, 1) (<see cref="CellSplit"/>), as measured on Godot
/// 4.7.2's Jolt height-field shape and the same diagonal the L0 mesh uses.
/// </summary>
public static class GroundCollision
{
    public const int Samples = 65;
    public const int FootCells = 128;

    internal static HeightFieldCollider Build(BuildContext context, TileKey tile, GroundSection section)
    {
        var layout = GroundLayout.Of(context.World);
        var extent = GroundLod.Tile(tile, LodLevel.L0, context.Map.DataRect);
        var terrain = TerrainGrid.For(context.World.Terrain, tile, LodLevel.L0, extent);
        var field = TileField.Build(layout, extent, section, terrain);
        var mesher = new GroundMesher(field, section, LodLevel.L0, layout, terrain, new MeshSink());
        var cm = new short[Samples * Samples];
        var metre = field.N / (Samples - 1);
        for (var z = 0; z < Samples; z++)
            for (var x = 0; x < Samples; x++)
                cm[z * Samples + x] = checked((short)MathF.Round(mesher.HeightAt(x * metre, z * metre) * 100));
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

    /// <summary>Height of the collision surface at a world point, interpolated on the same (anti-)diagonal split.</summary>
    public static float SampleHeight(HeightFieldCollider collider, float x, float z)
    {
        ArgumentNullException.ThrowIfNull(collider);
        var u = x - collider.Tile.X * TileGrid.BaseEdge;
        var w = z - collider.Tile.Z * TileGrid.BaseEdge;
        if (u < 0 || w < 0 || u > TileGrid.BaseEdge || w > TileGrid.BaseEdge) return float.NaN;
        var i = Math.Clamp((int)MathF.Floor(u), 0, Samples - 2);
        var j = Math.Clamp((int)MathF.Floor(w), 0, Samples - 2);
        float H(int a, int b) => collider.HeightsCm[(j + b) * Samples + i + a] * .01f;
        return CellSplit.Lerp(H(0, 0), H(1, 0), H(0, 1), H(1, 1), u - i, w - j);
    }
}
