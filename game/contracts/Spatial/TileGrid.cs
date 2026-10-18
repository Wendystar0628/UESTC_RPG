using System.Numerics;

namespace Campus.Contracts;

/// <summary>World tile addressing. L0-L3 are regular grids; L4 is one whole-map tile.</summary>
public static class TileGrid
{
    public const int BaseEdge = 64;

    /// <summary>Nominal grid edge in metres. Whole-map L4 producers use the map rectangle instead of this edge.</summary>
    public static int Edge(LodLevel lod) => lod switch
    {
        LodLevel.L0 or LodLevel.L1 or LodLevel.L2 or LodLevel.L3 or LodLevel.L4 => BaseEdge << (int)lod,
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    public static TileKey At(Vector2 point, LodLevel lod = LodLevel.L0)
    {
        if (!VectorMath.Finite(point)) throw new ArgumentOutOfRangeException(nameof(point));
        if (lod == LodLevel.L4) return new(0, 0);
        int edge = Edge(lod);
        return new((int)MathF.Floor(point.X / edge), (int)MathF.Floor(point.Y / edge));
    }

    public static TileKey At(Vector3 point, LodLevel lod = LodLevel.L0) => At(new Vector2(point.X, point.Z), lod);
    public static Vector2 Origin(TileKey tile, LodLevel lod = LodLevel.L0)
    {
        double edge = Edge(lod);
        return new((float)(tile.X * edge), (float)(tile.Z * edge));
    }
}
