using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Ground;

/// <summary>
/// Walking collision for the ground (SYS-WALK 13): one 65 x 65 height field per 64 m cell, 1 m spacing, heights in cm,
/// read directly from ITerrain.FillTile on the same L0 base lattice; plus the 0.5 m footstep grid
/// (128 x 128 bytes, SYS-AUDIO 9.3). Row-major with x fastest: index = z * 65 + x (HeightMapShape3D map_data order).
/// Triangulation diagonal of each 1 m cell: the anti-diagonal (1, 0)-(0, 1) (<see cref="CellSplit"/>), as measured on Godot
/// 4.7.2's Jolt height-field shape and the same diagonal the L0 mesh uses.
/// </summary>
public static class GroundCollision
{
    public const int Samples = 65;
    public const int FootCells = 128;

    internal static HeightFieldCollider BuildBase(TileKey tile, ITerrain terrain, GroundMesher mesher, int cells)
    {
        var cm = new short[Samples * Samples];
        terrain.FillTile(tile, 0, cm);
        var foot = new byte[FootCells * FootCells];
        var half = cells / FootCells;               // lattice points per 0.5 m (2 at 0.25 m)
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
        return WalkSupportSurfaces.SampleHeight(collider, x, z);
    }
}
