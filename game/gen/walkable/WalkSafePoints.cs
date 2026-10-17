using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Walkable;

internal static class WalkSafePoints
{
    internal static short[] Build(WalkRaster runtime, WalkRaster fine, PolygonRings boundary,
        ITerrain terrain, IWalkSupportSource exact, Vector3 quantizationOrigin, IReadOnlyList<PolygonRings> exclusions)
    {
        var width = runtime.Width / 4;
        var height = runtime.Height / 4;
        var count = checked(width * height);
        var source = new int[count];
        Array.Fill(source, -1);
        var coordinates = new short[checked(count * 3)];
        Array.Fill(coordinates, short.MinValue);
        var queue = new Queue<int>();
        var obstacles = exclusions.Select(polygon => (Polygon: polygon, Bounds: WalkGeometry.Bounds(polygon.Outer))).ToArray();

        // One conservative ground-level candidate per 8 m cell, nearest its centre.
        // Enumeration and ties are fixed in Z then X order; no hash iteration influences output.
        for (var gz = 0; gz < height; gz++)
            for (var gx = 0; gx < width; gx++)
            {
                var cell = gz * width + gx;
                var target = runtime.Origin + new Vector2(gx * 8f + 4f, gz * 8f + 4f);
                var best = float.PositiveInfinity;
                Vector3? chosen = null;
                for (var z = gz * 4; z < gz * 4 + 4; z++)
                    for (var x = gx * 4; x < gx * 4 + 4; x++)
                    {
                        if (runtime.At(x, z) != WalkClass.Walk) continue;
                        var point = runtime.Center(x, z);
                        var distance = Vector2.DistanceSquared(point, target);
                        if (distance >= best || !IsSafe(point, fine, boundary, terrain, exact, out var y) || NearObstacle(point)) continue;
                        best = distance;
                        chosen = new Vector3(point.X, y, point.Y);
                    }
                if (chosen is not { } safe) continue;
                Write(coordinates, cell, safe, quantizationOrigin);
                source[cell] = cell;
                queue.Enqueue(cell);
            }

        // Multi-source breadth-first propagation stores a valid nearby candidate even in blocked cells.
        // The runtime query still measures Euclidean distance and searches neighbours for maxDist.
        while (queue.TryDequeue(out var current))
        {
            var x = current % width;
            var z = current / width;
            if (z > 0) Visit(current - width);
            if (x > 0) Visit(current - 1);
            if (x + 1 < width) Visit(current + 1);
            if (z + 1 < height) Visit(current + width);

            void Visit(int next)
            {
                if (source[next] >= 0) return;
                source[next] = source[current];
                Array.Copy(coordinates, source[current] * 3, coordinates, next * 3, 3);
                queue.Enqueue(next);
            }
        }
        return coordinates;

        bool NearObstacle(Vector2 point)
        {
            foreach (var obstacle in obstacles)
            {
                if (point.X < obstacle.Bounds.Min.X - .6f || point.X > obstacle.Bounds.Max.X + .6f ||
                    point.Y < obstacle.Bounds.Min.Y - .6f || point.Y > obstacle.Bounds.Max.Y + .6f) continue;
                if (WalkGeometry.Contains(obstacle.Polygon, point) || WalkGeometry.EdgeDistanceSquared(obstacle.Polygon, point) <= .36f)
                    return true;
            }
            return false;
        }
    }

    private static bool IsSafe(Vector2 point, WalkRaster fine, PolygonRings boundary, ITerrain terrain, IWalkSupportSource exact, out float y)
    {
        var support = exact.SupportY(point.X, point.Y, terrain.HeightAt(point.X, point.Y));
        y = support ?? float.NaN;
        if (!float.IsFinite(y) || WalkGeometry.EdgeDistanceSquared(boundary, point) < 1f) return false;
        var fx = (int)MathF.Floor((point.X - fine.Origin.X) / fine.Step);
        var fz = (int)MathF.Floor((point.Y - fine.Origin.Y) / fine.Step);
        // The complete surrounding 2 x 2 m square must be dry ground. This exceeds the 0.6 m edge margin.
        for (var z = fz - 1; z <= fz; z++)
            for (var x = fx - 1; x <= fx; x++)
                if (fine.At(x, z) != WalkClass.Walk) return false;
        foreach (var offset in new[] { new Vector2(-.6f, 0f), new Vector2(.6f, 0f), new Vector2(0f, -.6f), new Vector2(0f, .6f) })
        {
            var neighbour = point + offset;
            var height = exact.SupportY(neighbour.X, neighbour.Y, y);
            if (height is null || !float.IsFinite(height.Value) || MathF.Abs(height.Value - y) > .35f) return false;
        }
        return terrain.FactsAt(point.X, point.Y).Grade < .36397023f; // tan(20 degrees).
    }

    private static void Write(short[] data, int cell, Vector3 point, Vector3 origin)
    {
        var relative = (point - origin) * 4f;
        data[cell * 3] = Quantize(MathF.Round(relative.X));
        data[cell * 3 + 1] = Quantize(MathF.Ceiling(relative.Y));
        data[cell * 3 + 2] = Quantize(MathF.Round(relative.Z));
    }

    private static short Quantize(float value)
    {
        if (!float.IsFinite(value) || value < short.MinValue + 1 || value > short.MaxValue)
            throw new InvalidOperationException("Safe point exceeds signed 16-bit quarter-metre encoding; adjust the map quantization origin.");
        return (short)value;
    }
}
