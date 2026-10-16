using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.Walkable;

internal sealed class WalkRaster
{
    internal Vector2 Origin { get; }
    internal int Width { get; }
    internal int Height { get; }
    internal float Step { get; }
    internal byte[] Classes { get; }

    internal WalkRaster(MapRect rect, float step)
    {
        Origin = rect.Min;
        Step = step;
        Width = checked((int)((rect.Max.X - rect.Min.X) / step));
        Height = checked((int)((rect.Max.Y - rect.Min.Y) / step));
        Classes = new byte[checked(Width * Height)];
        Array.Fill(Classes, (byte)WalkClass.Outside);
    }

    internal Vector2 Center(int x, int z) => Origin + new Vector2((x + .5f) * Step, (z + .5f) * Step);
    internal WalkClass At(int x, int z) => x < 0 || z < 0 || x >= Width || z >= Height
        ? WalkClass.Outside : (WalkClass)Classes[z * Width + x];

    internal void Polygon(PolygonRings polygon, WalkClass value, bool campus = false)
    {
        var (min, max) = WalkGeometry.Bounds(polygon.Outer);
        var z0 = Math.Clamp((int)MathF.Ceiling((min.Y - Origin.Y) / Step - .5f), 0, Height - 1);
        var z1 = Math.Clamp((int)MathF.Floor((max.Y - Origin.Y) / Step - .5f), 0, Height - 1);
        var crossings = new List<float>();
        for (var z = z0; z <= z1; z++)
        {
            crossings.Clear();
            var rowY = Origin.Y + (z + .5f) * Step;
            Cross(polygon.Outer, rowY);
            foreach (var hole in polygon.Holes) Cross(hole, rowY);
            crossings.Sort();
            // Even/odd spans retain holes and avoid testing every pixel against every campus edge.
            for (var c = 0; c + 1 < crossings.Count; c += 2)
            {
                var first = Math.Max(0, (int)MathF.Ceiling((crossings[c] - Origin.X) / Step - .5f));
                var last = Math.Min(Width - 1, (int)MathF.Ceiling((crossings[c + 1] - Origin.X) / Step - .5f) - 1);
                for (var x = first; x <= last; x++)
                {
                    var i = z * Width + x;
                    if (campus || Classes[i] != (byte)WalkClass.Outside) Classes[i] = (byte)value;
                }
            }
        }

        void Cross(IReadOnlyList<Vector2> ring, float rowY)
        {
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            {
                var a = ring[j];
                var b = ring[i];
                if ((a.Y > rowY) != (b.Y > rowY))
                    crossings.Add(a.X + (b.X - a.X) * (rowY - a.Y) / (b.Y - a.Y));
            }
        }
    }

    internal void Line(Polyline line, float width, WalkClass value)
    {
        if (line.Points.Count < 2 || !float.IsFinite(width) || width <= 0f) return;
        var radius = width * .5f;
        for (var i = 1; i < line.Points.Count; i++)
        {
            var a = line.Points[i - 1];
            var b = line.Points[i];
            Visit(Vector2.Min(a, b) - new Vector2(radius), Vector2.Max(a, b) + new Vector2(radius), (x, z, point) =>
            {
                var index = z * Width + x;
                if (Classes[index] != (byte)WalkClass.Outside && WalkGeometry.SegmentDistanceSquared(point, a, b) <= radius * radius)
                    Classes[index] = (byte)value;
            });
        }
    }

    internal void Visit(Vector2 min, Vector2 max, Action<int, int, Vector2> action)
    {
        var x0 = Math.Clamp((int)MathF.Floor((min.X - Origin.X) / Step), 0, Width - 1);
        var z0 = Math.Clamp((int)MathF.Floor((min.Y - Origin.Y) / Step), 0, Height - 1);
        var x1 = Math.Clamp((int)MathF.Floor((max.X - Origin.X) / Step), 0, Width - 1);
        var z1 = Math.Clamp((int)MathF.Floor((max.Y - Origin.Y) / Step), 0, Height - 1);
        for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++) action(x, z, Center(x, z));
    }

    internal void MarkNearBoundary(PolygonRings boundary, byte[] flags)
    {
        // A cell whose corner can touch the 3 m band is included, avoiding a false negative at its edge.
        var reach = 3f + Step * .707107f;
        MarkRing(boundary.Outer);
        foreach (var hole in boundary.Holes) MarkRing(hole);
        void MarkRing(IReadOnlyList<Vector2> points)
        {
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
            {
                var a = points[j];
                var b = points[i];
                Visit(Vector2.Min(a, b) - new Vector2(reach), Vector2.Max(a, b) + new Vector2(reach), (x, z, point) =>
                {
                    if (WalkGeometry.SegmentDistanceSquared(point, a, b) <= reach * reach)
                        flags[z * Width + x] |= 1;
                });
            }
        }
    }
}
