using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Scalar, millimetre-quantized plan operations shared by the facade and room generators.</summary>
public static partial class PlanGeometry
{
    internal const float Tolerance = 0.002f;

    internal static float Quantize(float value)
    {
        if (!float.IsFinite(value) || MathF.Abs(value) > 100000f)
            throw new ArgumentOutOfRangeException(nameof(value), "Plan coordinates must be finite and within 100 km.");
        float result = (float)(Math.Round((double)value * 1000d, MidpointRounding.ToEven) / 1000d);
        return result == 0f ? 0f : result;
    }

    internal static Vector2 Quantize(Vector2 p) => new(Quantize(p.X), Quantize(p.Y));
    internal static Vector3 Quantize(Vector3 p) => new(Quantize(p.X), Quantize(p.Y), Quantize(p.Z));
    internal static double Cross(Vector2 a, Vector2 b) => (double)a.X * b.Y - (double)a.Y * b.X;
    internal static double Dot(Vector2 a, Vector2 b) => (double)a.X * b.X + (double)a.Y * b.Y;
    internal static Vector2 Unit(Vector2 p)
    {
        float length = VectorMath.Length(p);
        if (length < 0.001f) throw new ArgumentException("A plan direction cannot have zero length.");
        return new(p.X / length, p.Y / length);
    }

    internal static Vector2 Lerp(Vector2 a, Vector2 b, double t) => Quantize(new Vector2(
        (float)(a.X + ((double)b.X - a.X) * t), (float)(a.Y + ((double)b.Y - a.Y) * t)));

    internal static string Mm(float value) => checked((int)Math.Round((double)value * 1000d)).ToString(CultureInfo.InvariantCulture);
    internal static string Key(Vector2 p) => Mm(p.X) + "," + Mm(p.Y);

    internal static Vector2[] CanonicalRing(IReadOnlyList<Vector2> source, bool counterclockwise)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Count > 4096) throw new ArgumentException("A mainline plan ring may contain at most 4096 vertices.", nameof(source));
        var points = new List<Vector2>(source.Count);
        foreach (Vector2 input in source)
        {
            Vector2 p = Quantize(input);
            if (points.Count == 0 || p != points[^1]) points.Add(p);
        }
        if (points.Count > 1 && points[0] == points[^1]) points.RemoveAt(points.Count - 1);
        bool changed = true;
        while (changed && points.Count >= 3)
        {
            changed = false;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 before = points[(i + points.Count - 1) % points.Count];
                Vector2 current = points[i];
                Vector2 after = points[(i + 1) % points.Count];
                // Only remove a point on a forward edge; a backtracking spike is invalid.
                if (Math.Abs(Cross(current - before, after - current)) <= 0.0000001d &&
                    Dot(current - before, after - current) > 0d)
                {
                    points.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }
        if (points.Count < 3) throw new ArgumentException("A footprint needs at least three distinct non-collinear points.", nameof(source));
        double area = SignedArea(points);
        if (Math.Abs(area) < 0.01d) throw new ArgumentException("A footprint has negligible area.", nameof(source));
        for (int i = 0; i < points.Count; i++)
            for (int j = i + 1; j < points.Count; j++)
            {
                if (j == i + 1 || i == 0 && j == points.Count - 1) continue;
                if (SegmentsTouch(points[i], points[(i + 1) % points.Count], points[j], points[(j + 1) % points.Count]))
                    throw new ArgumentException("Footprint edges intersect or touch away from adjacent corners.", nameof(source));
            }
        if ((area > 0d) != counterclockwise) points.Reverse();
        int first = 0;
        for (int i = 1; i < points.Count; i++)
            if (Compare(points[i], points[first]) < 0) first = i;
        return Enumerable.Range(0, points.Count).Select(i => points[(first + i) % points.Count]).ToArray();
    }

    internal static int Compare(Vector2 a, Vector2 b)
    {
        int x = a.X.CompareTo(b.X);
        return x != 0 ? x : a.Y.CompareTo(b.Y);
    }

    internal static bool OnSegment(Vector2 p, Vector2 a, Vector2 b, float tolerance = Tolerance)
    {
        Vector2 d = b - a;
        float length = VectorMath.Length(d);
        if (length < 0.001f) return VectorMath.Length(p - a) <= tolerance;
        return Math.Abs(Cross(d, p - a)) <= length * tolerance &&
            Dot(p - a, d) >= -length * tolerance && Dot(p - b, d) <= length * tolerance;
    }

    internal static bool Contains(IReadOnlyList<Vector2> ring, Vector2 p, bool includeBoundary = true)
    {
        bool inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            Vector2 a = ring[j], b = ring[i];
            if (OnSegment(p, a, b)) return includeBoundary;
            if ((a.Y > p.Y) != (b.Y > p.Y) &&
                p.X < ((double)b.X - a.X) * (p.Y - a.Y) / ((double)b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    internal static bool SegmentsTouch(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        double abC = Cross(b - a, c - a), abD = Cross(b - a, d - a);
        double cdA = Cross(d - c, a - c), cdB = Cross(d - c, b - c);
        if ((abC > 0d) != (abD > 0d) && (cdA > 0d) != (cdB > 0d)) return true;
        return OnSegment(c, a, b) || OnSegment(d, a, b) || OnSegment(a, c, d) || OnSegment(b, c, d);
    }
}
