using System.Numerics;
using Campus.Contracts;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;

namespace Campus.Gen.Geometry;

public readonly record struct PlanLineSample(Vector2 Point, Vector2 Tangent, double Station);
public readonly record struct PlanLineProjection(Vector2 Point, Vector2 Tangent, double Station, double Distance);

public static partial class PlanGeometry
{
    private static readonly GeometryFactory Factory = new();

    public static double Length(IReadOnlyList<Vector2> points, bool closed = false) => PlanLine(points, closed).Length;

    /// <summary>Distance in metres along an open line (clamped) or a closed ring (wrapped).</summary>
    public static PlanLineSample Sample(IReadOnlyList<Vector2> points, double station, bool closed = false)
    {
        var line = PlanLine(points, closed);
        return Sample(line, new LengthIndexedLine(line), station, closed);
    }

    public static PlanLineProjection Project(IReadOnlyList<Vector2> points, Vector2 point, bool closed = false)
    {
        if (!VectorMath.Finite(point)) throw new ArgumentException("Non-finite query point.", nameof(point));
        var line = PlanLine(points, closed);
        var index = new LengthIndexedLine(line);
        var station = index.Project(new Coordinate(point.X, point.Y));
        var sample = Sample(line, index, station, closed, preferIncoming: true);
        var dx = (double)point.X - sample.Point.X;
        var dz = (double)point.Y - sample.Point.Y;
        return new(sample.Point, sample.Tangent, sample.Station, Math.Sqrt(dx * dx + dz * dz));
    }

    /// <summary>Double-precision segment distance; promote endpoints before subtraction.</summary>
    public static double SegmentDistance(double px, double pz, Vector2 a, Vector2 b)
    {
        double ax = a.X, az = a.Y, dx = b.X - ax, dz = b.Y - az;
        var len2 = dx * dx + dz * dz;
        var t = len2 > 0d ? ((px - ax) * dx + (pz - az) * dz) / len2 : 0d;
        t = t < 0d ? 0d : t > 1d ? 1d : t;
        var ex = px - (ax + t * dx);
        var ez = pz - (az + t * dz);
        return Math.Sqrt(ex * ex + ez * ez);
    }

    /// <summary>Allocation-free float clearance query. Empty and single-point paths have no segments.</summary>
    public static float PolylineDistanceSquared(IReadOnlyList<Vector2> points, Vector2 point, bool closed = false)
    {
        var best = float.MaxValue;
        var count = closed ? points.Count : points.Count - 1;
        for (var i = 0; i < count; i++)
            best = MathF.Min(best, SegmentDistanceSquared(point, points[i], points[(i + 1) % points.Count]));
        return best;
    }

    /// <summary>Source paths retain direction and may have fewer than three points; invalid points are omitted.</summary>
    public static Vector2[] CleanPath(IEnumerable<Vector2> points, float distanceSquared = 1e-8f)
    {
        var result = new List<Vector2>();
        foreach (var point in points)
            if (VectorMath.Finite(point) && (result.Count == 0 || Vector2.DistanceSquared(result[^1], point) >= distanceSquared))
                result.Add(point);
        if (result.Count > 2 && Vector2.DistanceSquared(result[0], result[^1]) < distanceSquared) result.RemoveAt(result.Count - 1);
        return result.ToArray();
    }

    /// <summary>Single-precision line intersection, retaining the caller's parallel tolerance.</summary>
    public static Vector2? LineIntersection(Vector2 p, Vector2 u, Vector2 q, Vector2 v, float epsilon)
    {
        var den = u.X * v.Y - u.Y * v.X;
        if (MathF.Abs(den) < epsilon) return null;
        var t = ((q.X - p.X) * v.Y - (q.Y - p.Y) * v.X) / den;
        return p + u * t;
    }

    public static Vector2? SegmentIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float epsilon)
    {
        var r = b - a;
        var s = d - c;
        var den = r.X * s.Y - r.Y * s.X;
        if (MathF.Abs(den) < epsilon) return null;
        var t = ((c.X - a.X) * s.Y - (c.Y - a.Y) * s.X) / den;
        var u = ((c.X - a.X) * r.Y - (c.Y - a.Y) * r.X) / den;
        return t < 0 || t > 1 || u < 0 || u > 1 ? null : a + r * t;
    }

    private static LineString PlanLine(IReadOnlyList<Vector2> points, bool closed)
    {
        ArgumentNullException.ThrowIfNull(points);
        var vertices = new List<Coordinate>(points.Count + 1);
        foreach (var point in points)
        {
            if (!VectorMath.Finite(point)) throw new ArgumentException("Non-finite line coordinate.", nameof(points));
            if (vertices.Count == 0 || vertices[^1].X != point.X || vertices[^1].Y != point.Y)
                vertices.Add(new(point.X, point.Y));
        }
        if (vertices.Count < 2) throw new ArgumentException("A line needs two distinct points.", nameof(points));
        if (closed && !vertices[0].Equals2D(vertices[^1])) vertices.Add(vertices[0].Copy());
        return Factory.CreateLineString(vertices.ToArray());
    }

    private static PlanLineSample Sample(LineString line, LengthIndexedLine index, double station, bool closed, bool preferIncoming = false)
    {
        if (!double.IsFinite(station)) throw new ArgumentOutOfRangeException(nameof(station));
        var length = line.Length;
        station = closed ? ((station % length) + length) % length : Math.Clamp(station, 0d, length);
        var point = index.ExtractPoint(station);
        // The downstream segment owns a vertex; the final endpoint uses its incoming segment.
        double cursor = 0;
        var coordinates = line.Coordinates;
        for (var i = 1; i < coordinates.Length; i++)
        {
            var segmentLength = coordinates[i - 1].Distance(coordinates[i]);
            cursor += segmentLength;
            if ((preferIncoming ? station <= cursor : station < cursor) || i == coordinates.Length - 1)
            {
                var tangent = new Vector2((float)((coordinates[i].X - coordinates[i - 1].X) / segmentLength),
                    (float)((coordinates[i].Y - coordinates[i - 1].Y) / segmentLength));
                return new(new((float)point.X, (float)point.Y), tangent, station);
            }
        }
        throw new ArgumentException("A line needs nonzero length.");
    }
}
