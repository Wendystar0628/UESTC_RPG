using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Vegetation;

namespace Campus.Gen.Vegetation;

/// <summary>Pure .NET bake of the existing grass selection envelope; no Godot or live quality state is involved.</summary>
internal static class GrassPoolIndexBuilder
{
    public static GrassPoolIndex Build(IReadOnlyList<Box3> bounds)
    {
        var cells = new Dictionary<(int X, int Z), (List<int> Items, Box3 Box)>();
        var order = new List<(int X, int Z)>();
        for (var i = 0; i < bounds.Count; i++)
        {
            var b = bounds[i];
            var key = KeyOf(new Vector2((b.Min.X + b.Max.X) * 0.5f, (b.Min.Z + b.Max.Z) * 0.5f));
            if (cells.TryGetValue(key, out var cell))
                cells[key] = (cell.Items, new Box3(Vector3.Min(cell.Box.Min, b.Min), Vector3.Max(cell.Box.Max, b.Max)));
            else { cells[key] = ([], b); order.Add(key); }
            cells[key].Items.Add(i);
        }
        var directory = new List<GrassPoolCell>(cells.Count);
        var members = new int[bounds.Count];
        var start = 0;
        foreach (var key in order)
        {
            var cell = cells[key];
            cell.Items.CopyTo(members, start);
            directory.Add(new(key.X, key.Z, start, cell.Items.Count, cell.Box));
            start += cell.Items.Count;
        }
        return GrassPoolIndex.TakeOwnership(directory, members, Math.Max(1, MostInReach(cells, order)));
    }

    private static (int X, int Z) KeyOf(Vector2 point) =>
        ((int)MathF.Floor(point.X / GrassPoolIndex.CellSize), (int)MathF.Floor(point.Y / GrassPoolIndex.CellSize));

    private static int InReach(Dictionary<(int X, int Z), (List<int> Items, Box3 Box)> cells, Vector2 at, float reach)
    {
        var r = (int)MathF.Ceiling(reach / GrassPoolIndex.CellSize);
        var c = KeyOf(at);
        var count = 0;
        for (var z = c.Z - r; z <= c.Z + r; z++)
            for (var x = c.X - r; x <= c.X + r; x++)
            {
                if (!cells.TryGetValue((x, z), out var cell)) continue;
                var dx = MathF.Max(0f, MathF.Max(x * GrassPoolIndex.CellSize - at.X, at.X - (x + 1) * GrassPoolIndex.CellSize));
                var dz = MathF.Max(0f, MathF.Max(z * GrassPoolIndex.CellSize - at.Y, at.Y - (z + 1) * GrassPoolIndex.CellSize));
                if (dx * dx + dz * dz <= reach * reach) count += cell.Items.Count;
            }
        return count;
    }

    // Every eye in a cell is within half its diagonal of the centre. Preserve the existing conservative 0.71 envelope.
    private static int MostInReach(Dictionary<(int X, int Z), (List<int> Items, Box3 Box)> cells,
        IReadOnlyList<(int X, int Z)> order)
    {
        var r = (int)MathF.Ceiling(GrassPoolIndex.Reach / GrassPoolIndex.CellSize) + 1;
        var best = 0;
        var seen = new HashSet<(int, int)>();
        foreach (var key in order)
            for (var z = key.Z - r; z <= key.Z + r; z++)
                for (var x = key.X - r; x <= key.X + r; x++)
                    if (seen.Add((x, z)))
                        best = Math.Max(best, InReach(cells,
                            new Vector2((x + 0.5f) * GrassPoolIndex.CellSize, (z + 0.5f) * GrassPoolIndex.CellSize),
                            GrassPoolIndex.Reach + GrassPoolIndex.CellSize * 0.71f));
        return best;
    }
}
