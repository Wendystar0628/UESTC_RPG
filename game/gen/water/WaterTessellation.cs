using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Water;

/// <summary>Cuts water-ring boundary runs into trapezoids and fills adjacent interior grid cells.</summary>
internal static class WaterTessellation
{
    internal static void Tessellate(WaterBody body, List<WaterEdge> edges, float step, float sliver, WaterTopology builder)
    {
        var c0x = (int)MathF.Floor(body.Min.X / step);
        var c1x = (int)MathF.Floor(body.Max.X / step);
        var c0z = (int)MathF.Floor(body.Min.Y / step);
        var c1z = (int)MathF.Floor(body.Max.Y / step);
        var width = c1x - c0x + 1;
        var height = c1z - c0z + 1;
        var boundary = new bool[width * height];
        foreach (var e in edges)
        {
            var ax = (int)MathF.Floor(MathF.Min(e.A.X, e.B.X) / step) - c0x;
            var bx = (int)MathF.Floor(MathF.Max(e.A.X, e.B.X) / step) - c0x;
            var az = (int)MathF.Floor(e.ZMin / step) - c0z;
            var bz = (int)MathF.Floor(e.ZMax / step) - c0z;
            for (var cz = Math.Max(az - 1, 0); cz <= Math.Min(bz + 1, height - 1); cz++)
                for (var cx = Math.Max(ax - 1, 0); cx <= Math.Min(bx + 1, width - 1); cx++)
                {
                    var min = new Vector2((cx + c0x) * step, (cz + c0z) * step);
                    if (SegmentTouchesRect(e.A, e.B, min, min + new Vector2(step))) boundary[cz * width + cx] = true;
                }
        }

        // Extra vertices that boundary runs put on the walls of neighbouring inside cells, keyed by wall column.
        var wallRows = new Dictionary<(int Wall, int Row), List<float>>();
        for (var cz = 0; cz < height; cz++)
        {
            var z0 = (cz + c0z) * step;
            var z1 = z0 + step;
            var cx = 0;
            while (cx < width)
            {
                if (!boundary[cz * width + cx]) { cx++; continue; }
                var start = cx;
                while (cx < width && boundary[cz * width + cx]) cx++;
                var rows = TessellateRun(edges, (start + c0x) * step, (cx + c0x) * step, z0, z1, step, start + c0x, cx + c0x, builder);
                wallRows[(start + c0x, cz + c0z)] = rows;
                wallRows[(cx + c0x, cz + c0z)] = rows;
            }
        }

        for (var cz = 0; cz < height; cz++)
            for (var cx = 0; cx < width; cx++)
            {
                if (boundary[cz * width + cx]) continue;
                var x0 = (cx + c0x) * step;
                var z0 = (cz + c0z) * step;
                var centre = new Vector2(x0 + step * 0.5f, z0 + step * 0.5f);
                if (!body.Contains(centre)) continue;
                wallRows.TryGetValue((cx + c0x, cz + c0z), out var left);
                wallRows.TryGetValue((cx + c0x + 1, cz + c0z), out var right);
                InsideCell(x0, z0, step, left, right, builder);
            }
    }

    /// <summary>Cuts a horizontal run of shore cells into trapezoids; returns the rows it used (for neighbour walls).</summary>
    private static List<float> TessellateRun(List<WaterEdge> edges, float xa, float xb, float z0, float z1, float step,
        int firstWall, int endWall, WaterTopology builder)
    {
        // Rows: the cell borders and every ring vertex inside the run. Ring vertices lying on a row are pinned into
        // that row, so a vertex where two intervals meet (an island's tip) is shared by the bands above and below.
        var rows = new List<float> { z0, z1 };
        var pins = new List<Vector2>();
        foreach (var e in edges)
        {
            if (e.ZMax < z0 || e.ZMin > z1 || MathF.Max(e.A.X, e.B.X) < xa || MathF.Min(e.A.X, e.B.X) > xb) continue;
            Pin(e.A);
            Pin(e.B);
        }
        rows.Sort();
        var unique = new List<float>(rows.Count);
        foreach (var z in rows)
            if (unique.Count == 0 || z - unique[^1] > 1e-5f) unique.Add(z);
        unique[^1] = z1;
        var rowPins = new List<float>[unique.Count];
        for (var r = 0; r < unique.Count; r++) rowPins[r] = [];
        foreach (var v in pins)
        {
            var r = unique.BinarySearch(v.Y);
            if (r < 0) { r = ~r; if (r == unique.Count || (r > 0 && v.Y - unique[r - 1] < unique[r] - v.Y)) r--; }
            if (!rowPins[r].Contains(v.X)) rowPins[r].Add(v.X);
        }

        var active = new List<(float X, WaterEdge E)>();
        for (var r = 0; r + 1 < unique.Count; r++)
        {
            var zb = unique[r];
            var zt = unique[r + 1];
            var zm = (zb + zt) * 0.5f;
            active.Clear();
            foreach (var e in edges)
                if (e.Spans(zm)) active.Add((e.XAt(zm), e));
            active.Sort((a, b) => a.X.CompareTo(b.X));
            var inside = false;
            var i = 0;
            for (; i < active.Count && active[i].X < xa; i++) inside = !inside;
            WaterEdge? left = null;
            for (; i <= active.Count; i++)
            {
                var atEnd = i == active.Count || active[i].X >= xb;
                if (atEnd)
                {
                    if (inside) Trapezoid(left, null, xa, xb, zb, zt, rowPins[r], rowPins[r + 1], firstWall, endWall, step, builder);
                    break;
                }
                if (inside) Trapezoid(left, active[i].E, xa, xb, zb, zt, rowPins[r], rowPins[r + 1], firstWall, endWall, step, builder);
                else left = active[i].E;
                inside = !inside;
            }
        }
        return unique;

        void Pin(Vector2 v)
        {
            if (v.Y < z0 || v.Y > z1 || v.X < xa || v.X > xb) return;
            pins.Add(v);
            if (v.Y > z0 && v.Y < z1) rows.Add(v.Y);
        }
    }

    /// <summary>
    /// Triangulates one band of an interval between two rows. Each row carries its corners, every inner cell wall
    /// and every pinned ring vertex inside it; the two rows are zipped together, which is valid for any trapezoid.
    /// </summary>
    private static void Trapezoid(WaterEdge? left, WaterEdge? right, float xa, float xb, float zb, float zt,
        List<float> pinsBottom, List<float> pinsTop, int firstWall, int endWall, float step, WaterTopology builder)
    {
        var bottom = Row(left?.XAt(zb) ?? xa, right?.XAt(zb) ?? xb, pinsBottom);
        var top = Row(left?.XAt(zt) ?? xa, right?.XAt(zt) ?? xb, pinsTop);
        var b = new int[bottom.Count];
        var t = new int[top.Count];
        for (var k = 0; k < bottom.Count; k++) b[k] = builder.Vertex(bottom[k], zb);
        for (var k = 0; k < top.Count; k++) t[k] = builder.Vertex(top[k], zt);
        int i = 0, j = 0;
        while (i < b.Length - 1 || j < t.Length - 1)
        {
            if (j == t.Length - 1 || (i < b.Length - 1 && bottom[i + 1] <= top[j + 1]))
            {
                builder.Triangle(b[i], b[i + 1], t[j]);
                i++;
            }
            else
            {
                builder.Triangle(b[i], t[j + 1], t[j]);
                j++;
            }
        }

        List<float> Row(float l, float r, List<float> pins)
        {
            const float eps = 1e-3f;
            var xs = new List<float> { l };
            for (var w = firstWall + 1; w < endWall; w++)
            {
                var g = w * step;
                if (g > l + eps && g < r - eps) xs.Add(g);
            }
            foreach (var x in pins)
                if (x > l + eps && x < r - eps) xs.Add(x);
            xs.Sort();
            var row = new List<float>(xs.Count + 1);
            foreach (var x in xs)
                if (row.Count == 0 || x - row[^1] > eps) row.Add(x);
            if (r - row[^1] > eps) row.Add(r);
            return row;
        }
    }

    private static void InsideCell(float x0, float z0, float step, List<float>? left, List<float>? right, WaterTopology builder)
    {
        var x1 = x0 + step;
        var z1 = z0 + step;
        var extraLeft = Inner(left, z0, z1);
        var extraRight = Inner(right, z0, z1);
        if (extraLeft.Count == 0 && extraRight.Count == 0)
        {
            builder.Quad(x0, x1, x1, x0, z0, z1);
            return;
        }
        // Fan from the centre over the cell outline, so wall vertices shared with shore runs leave no T-junction.
        var ring = new List<Vector2> { new(x0, z0), new(x1, z0) };
        foreach (var z in extraRight) ring.Add(new(x1, z));
        ring.Add(new(x1, z1));
        ring.Add(new(x0, z1));
        for (var i = extraLeft.Count - 1; i >= 0; i--) ring.Add(new(x0, extraLeft[i]));
        var c = builder.Vertex(x0 + step * 0.5f, z0 + step * 0.5f);
        var outline = ring.Select(v => builder.Vertex(v.X, v.Y)).ToArray();
        for (var i = 0; i < outline.Length; i++) builder.Triangle(c, outline[i], outline[(i + 1) % outline.Length]);
    }

    private static List<float> Inner(List<float>? rows, float z0, float z1)
    {
        var result = new List<float>();
        if (rows is null) return result;
        foreach (var z in rows)
            if (z > z0 + 1e-5f && z < z1 - 1e-5f) result.Add(z);
        return result;
    }

    private static bool SegmentTouchesRect(Vector2 a, Vector2 b, Vector2 min, Vector2 max)
    {
        // Liang-Barsky on the closed rectangle.
        float t0 = 0f, t1 = 1f;
        var d = b - a;
        return Clip(-d.X, a.X - min.X) && Clip(d.X, max.X - a.X) && Clip(-d.Y, a.Y - min.Y) && Clip(d.Y, max.Y - a.Y);

        bool Clip(float q, float r)
        {
            if (q == 0f) return r >= 0f;
            var t = r / q;
            if (q < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
            else { if (t < t0) return false; if (t < t1) t1 = t; }
            return true;
        }
    }


}
