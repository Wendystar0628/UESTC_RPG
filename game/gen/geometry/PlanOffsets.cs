using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Plan offsets and edge normals, retaining each algorithm's precision and corner rules.</summary>
public static class PlanOffsets
{

    /// <summary>
    /// Mitred offset with one output vertex per input vertex, positive to the right of the path (dz, −dx): a corner moves along
    /// (n0 + n1) / (1 + n0·n1) of its edges' unit normals, held to <paramref name="limit"/> at sharp corners; an open line's ends
    /// move along their own edge's normal.
    /// </summary>
    public static Vector2[] MitreOffset(IReadOnlyList<Vector2> points, float distance, bool closed, float limit = float.PositiveInfinity)
    {
        ArgumentNullException.ThrowIfNull(points);
        var n = points.Count;
        var output = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            Vector2 m;
            if (!closed && i == 0) m = RightNormal(points[0], points[1]);
            else if (!closed && i == n - 1) m = RightNormal(points[n - 2], points[n - 1]);
            else
            {
                Vector2 n0 = RightNormal(points[(i + n - 1) % n], points[i]), n1 = RightNormal(points[i], points[(i + 1) % n]);
                m = (n0 + n1) / (1f + n0.X * n1.X + n0.Y * n1.Y);
                var length = m.Length();
                if (length > limit) m *= limit / length;
            }
            output[i] = points[i] + m * distance;
        }
        return output;
    }


    private static Vector2 RightNormal(Vector2 a, Vector2 b)
    {
        var v = new Vector2(b.Y - a.Y, a.X - b.X);
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
        return length > 1e-9f ? v / length : throw new ArgumentException("An offset edge must have nonzero length.");
    }


    /// <summary>
    /// Closed-ring inset with one vertex per corner and scalar double normals/intersections. Positive distance moves left;
    /// zero-length edges fail. Unlike <see cref="MitreOffset"/>, no substituted normal or miter limit is applied.
    /// </summary>
    public static Vector2[] MitreInset(IReadOnlyList<Vector2> ring, float distance)
    {
        var inset = new Vector2[ring.Count];
        for (var i = 0; i < ring.Count; i++)
        {
            var p = ring[(i + ring.Count - 1) % ring.Count]; var c = ring[i]; var n = ring[(i + 1) % ring.Count];
            var previous = RightUnitNormal(p, c); var next = RightUnitNormal(c, n);
            var divisor = 1 + (double)previous.X * next.X + (double)previous.Y * next.Y;
            inset[i] = new((float)(c.X - distance * (previous.X + next.X) / divisor), (float)(c.Y - distance * (previous.Y + next.Y) / divisor));
        }
        return inset;
    }


    /// <summary>Right-hand normal normalized in scalar double precision; nonfinite and zero-length edges fail.</summary>
    public static Vector2 RightUnitNormal(Vector2 a, Vector2 b)
    {
        float x = b.Y - a.Y, y = a.X - b.X;
        if (!float.IsFinite(x) || !float.IsFinite(y)) throw new ArgumentException("Coordinates must be finite.", nameof(a));
        var length = Math.Sqrt((double)x * x + (double)y * y);
        if (length <= 1e-12) throw new ArgumentException("A direction or edge must be nonzero.");
        return new((float)(x / length), (float)(y / length));
    }
}
