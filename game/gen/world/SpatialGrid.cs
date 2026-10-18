using System.Numerics;

namespace Campus.Gen.World;

/// <summary>
/// Items filed in square plan cells (world X/Z) for neighbourhood lookups. Lookups read cells in a fixed order and each cell
/// in filing order, never in dictionary iteration order, so callers that stop at the first hit stay deterministic.
/// </summary>
public sealed class SpatialGrid<T>(float cell)
{
    private readonly Dictionary<(int, int), List<T>> _cells = [];

    /// <summary>Files <paramref name="item"/> in every cell its box, grown by half a cell on each side, touches.</summary>
    public void AddBox(Vector2 min, Vector2 max, T item)
    {
        var (x0, z0) = Key(min - new Vector2(cell * 0.5f));
        var (x1, z1) = Key(max + new Vector2(cell * 0.5f));
        for (var x = x0; x <= x1; x++)
            for (var z = z0; z <= z1; z++)
                File((x, z), item);
    }

    /// <summary>Files <paramref name="item"/> in the one cell holding <paramref name="p"/>.</summary>
    public void AddPoint(Vector2 p, T item) => File(Key(p), item);

    /// <summary>The items filed in the cell holding <paramref name="p"/>, in filing order.</summary>
    public IReadOnlyList<T> In(Vector2 p) => _cells.TryGetValue(Key(p), out var list) ? list : [];

    /// <summary>True when an item filed in the 3 × 3 cells around <paramref name="p"/> matches.</summary>
    public bool AnyAround(Vector2 p, Func<T, bool> match)
    {
        var (cx, cz) = Key(p);
        for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
                if (_cells.TryGetValue((cx + dx, cz + dz), out var list) && list.Any(match))
                    return true;
        return false;
    }

    /// <summary>The cells around <paramref name="p"/> that hold items, X outer and Z inner, 3 × 3.</summary>
    internal IEnumerable<List<T>> CellsAround(Vector2 p)
    {
        var (cx, cz) = Key(p);
        for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
                if (_cells.TryGetValue((cx + dx, cz + dz), out var list))
                    yield return list;
    }

    private (int, int) Key(Vector2 p) => ((int)MathF.Floor(p.X / cell), (int)MathF.Floor(p.Y / cell));

    private void File((int, int) key, T item)
    {
        if (!_cells.TryGetValue(key, out var list)) _cells.Add(key, list = []);
        list.Add(item);
    }
}

public static class SpatialGridExtensions
{
    /// <summary>Everything filed in the 3 × 3 cells around <paramref name="p"/>, each item once (by reference), in cell then filing order.</summary>
    public static List<T> AllAround<T>(this SpatialGrid<T> grid, Vector2 p) where T : class
    {
        var seen = new HashSet<T>(ReferenceEqualityComparer.Instance);
        var result = new List<T>();
        foreach (var list in grid.CellsAround(p))
            foreach (var item in list)
                if (seen.Add(item)) result.Add(item);
        return result;
    }
}
