using System.Numerics;

namespace Campus.Gen.Zones.West.Framework;

/// <summary>
/// An open plan polyline (world x, z) measured by arc length, for things laid along a route (a boardwalk, its rails and
/// posts). Scalar arithmetic only. "Right" is the sweep kernel's lateral positive: (t.z, −t.x) for a unit tangent t.
/// </summary>
public sealed class Route
{
    private readonly float[] _s;

    public Route(IReadOnlyList<Vector2> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2) throw new ArgumentException("A route needs at least two points.", nameof(points));
        Points = [.. points];
        _s = new float[Points.Length];
        for (var i = 1; i < Points.Length; i++)
        {
            var d = Vector2.Distance(Points[i - 1], Points[i]);
            if (!(d > 1e-4f)) throw new ArgumentException("A route cannot repeat a point.", nameof(points));
            _s[i] = _s[i - 1] + d;
        }
    }

    public Vector2[] Points { get; }

    public float Length => _s[^1];

    /// <summary>Two Chaikin corner-cutting passes per <paramref name="passes"/>, keeping both end points.</summary>
    public static Vector2[] Smooth(IReadOnlyList<Vector2> points, int passes)
    {
        var p = points.ToArray();
        for (var k = 0; k < passes; k++)
        {
            var q = new List<Vector2> { p[0] };
            for (var i = 0; i + 1 < p.Length; i++)
            {
                Vector2 a = p[i], b = p[i + 1];
                if (i > 0) q.Add(a * 0.75f + b * 0.25f);
                if (i + 2 < p.Length) q.Add(a * 0.25f + b * 0.75f);
            }
            q.Add(p[^1]);
            p = [.. q];
        }
        return p;
    }

    /// <summary>Point and unit tangent at arc length s (clamped to the route).</summary>
    public (Vector2 P, Vector2 T) At(float s)
    {
        s = Math.Clamp(s, 0f, Length);
        var i = 1;
        while (i < _s.Length - 1 && _s[i] < s) i++;
        Vector2 a = Points[i - 1], b = Points[i];
        var t = (s - _s[i - 1]) / (_s[i] - _s[i - 1]);
        var d = b - a;
        var l = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
        return (a + d * t, d / l);
    }

    public static Vector2 Right(Vector2 t) => new(t.Y, -t.X);

    /// <summary>Arc length of the route point nearest to <paramref name="p"/>, and its signed lateral offset (right positive).</summary>
    public (float S, float Lateral) Project(Vector2 p)
    {
        float best = float.MaxValue, bestS = 0, bestLat = 0;
        for (var i = 1; i < Points.Length; i++)
        {
            Vector2 a = Points[i - 1], b = Points[i], d = b - a;
            var l2 = d.X * d.X + d.Y * d.Y;
            var t = Math.Clamp(((p.X - a.X) * d.X + (p.Y - a.Y) * d.Y) / l2, 0f, 1f);
            var q = a + d * t;
            var dist = Vector2.Distance(p, q);
            if (dist < best)
            {
                best = dist;
                bestS = _s[i - 1] + t * (_s[i] - _s[i - 1]);
                var r = Right(d / MathF.Sqrt(l2));
                bestLat = (p.X - q.X) * r.X + (p.Y - q.Y) * r.Y;
            }
        }
        return (bestS, bestLat);
    }

    /// <summary>The part of the route between arc lengths s0 and s1, with interior vertices kept.</summary>
    public Vector2[] Sub(float s0, float s1)
    {
        s0 = Math.Clamp(s0, 0f, Length); s1 = Math.Clamp(s1, 0f, Length);
        var list = new List<Vector2> { At(s0).P };
        for (var i = 1; i < Points.Length - 1; i++)
            if (_s[i] > s0 + 0.01f && _s[i] < s1 - 0.01f) list.Add(Points[i]);
        list.Add(At(s1).P);
        return [.. list];
    }

    /// <summary>A polyline offset to the right by <paramref name="lateral"/> with exact miters (as the sweep kernel builds them).</summary>
    public static Vector2[] Offset(IReadOnlyList<Vector2> line, float lateral)
    {
        var n = line.Count;
        var rights = new Vector2[n - 1];
        for (var i = 0; i + 1 < n; i++)
        {
            var d = line[i + 1] - line[i];
            rights[i] = Right(d / MathF.Sqrt(d.X * d.X + d.Y * d.Y));
        }
        var result = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            Vector2 m;
            if (i == 0) m = rights[0];
            else if (i == n - 1) m = rights[^1];
            else
            {
                Vector2 a = rights[i - 1], b = rights[i];
                m = (a + b) / (1f + a.X * b.X + a.Y * b.Y);
            }
            result[i] = line[i] + m * lateral;
        }
        return result;
    }
}
