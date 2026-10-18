using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Request vocabulary shared by baking and streaming. Coordinates are indices at the requested level.</summary>
public static class BakeRequestKeys
{
    public static BakeKey Tile(MapId map, string module, TileKey tile, LodLevel level) =>
        new(map, module, FormattableString.Invariant($"L{(int)level}/{tile.X}/{tile.Z}"));

    public static BakeKey Collision(MapId map, string module, TileKey tile) =>
        new(map, module, FormattableString.Invariant($"C/{tile.X}/{tile.Z}"));

    public static (TileKey Tile, LodLevel Level, bool Collision) Parse(BakeKey key)
    {
        var pieces = key.Detail.Split('/');
        if (pieces.Length != 3 || !int.TryParse(pieces[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(pieces[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z) ||
            x is < -512 or > 511 || z is < -512 or > 511)
            throw new ArgumentException($"Invalid tile request: {key.Detail}", nameof(key));
        if (pieces[0] == "C") return (new(x, z), LodLevel.L0, true);
        if (pieces[0].Length != 2 || pieces[0][0] != 'L' || pieces[0][1] is < '0' or > '4')
            throw new ArgumentException($"Invalid tile level: {key.Detail}", nameof(key));
        var level = (LodLevel)(pieces[0][1] - '0');
        if (level == LodLevel.L4 && (x != 0 || z != 0))
            throw new ArgumentException("L4 is the whole map and uses L4/0/0.", nameof(key));
        return (new(x, z), level, false);
    }

    public static int TileId(BakeKey key)
    {
        var request = Parse(key);
        return ((request.Collision ? 7 : (int)request.Level) << 20) |
            ((request.Tile.X + 512) << 10) | (request.Tile.Z + 512);
    }

    public static TileKey Parent(TileKey baseTile, LodLevel level)
    {
        var scale = TileGrid.Edge(level) / TileGrid.BaseEdge;
        return new((int)Math.Floor((double)baseTile.X / scale), (int)Math.Floor((double)baseTile.Z / scale));
    }
}
