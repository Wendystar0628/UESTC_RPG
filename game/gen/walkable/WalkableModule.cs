using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Walkable;

[ContentModule("walkable", Owner = "WP-44")]
public sealed class WalkableModule : IContentModule
{
    public IReadOnlyList<Claim> Claims => [];
    public ModuleBudget Budget { get; } = new(0, 0, 0);
    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod) => ContentOutput.Empty;

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        if (ctx.World.Walk is not { } walk) return [];
        var tiles = new SortedSet<TileKey>(Comparer<TileKey>.Create((a, b) => a.Z != b.Z ? a.Z.CompareTo(b.Z) : a.X.CompareTo(b.X)));
        foreach (var wall in walk.Walls)
        {
            var min = Vector2.Min(wall.A, wall.B);
            var max = Vector2.Max(wall.A, wall.B);
            for (var z = (int)MathF.Floor(min.Y / 64f); z <= (int)MathF.Floor(max.Y / 64f); z++)
                for (var x = (int)MathF.Floor(min.X / 64f); x <= (int)MathF.Floor(max.X / 64f); x++)
                {
                    var tile = new TileKey(x, z);
                    if (Clip(wall, tile) is not null) tiles.Add(tile);
                }
        }
        return tiles;
    }

    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        if (ctx.World.Walk is not { } walk) return [];
        var result = new List<Collider>();
        foreach (var wall in walk.Walls)
            if (Clip(wall, tile) is { } clipped) result.Add(clipped);
        return result;
    }

    private static WallCollider? Clip(WallCollider wall, TileKey tile)
    {
        var min = new Vector2(tile.X * 64f, tile.Z * 64f);
        var max = min + new Vector2(64f);
        var direction = wall.B - wall.A;
        var from = 0f;
        var to = 1f;
        if (!Axis(wall.A.X, direction.X, min.X, max.X) || !Axis(wall.A.Y, direction.Y, min.Y, max.Y) || to <= from)
            return null;
        return wall with { A = wall.A + direction * from, B = wall.A + direction * to };

        bool Axis(float origin, float delta, float low, float high)
        {
            // A boundary-aligned segment belongs to the tile on its positive side only.
            if (Math.Abs(delta) < .000001f) return origin >= low && origin < high;
            var t0 = (low - origin) / delta;
            var t1 = (high - origin) / delta;
            if (t0 > t1) (t0, t1) = (t1, t0);
            from = Math.Max(from, t0);
            to = Math.Min(to, t1);
            return to > from;
        }
    }
}
