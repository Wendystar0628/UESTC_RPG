using System.Numerics;
using Campus.Contracts;
using NetTopologySuite.Geometries;

namespace Campus.Gen.Geometry;

/// <summary>Polygon centroids, boundary distances and exact intersections using NTS.</summary>
public static class PlanPolygons
{
    private static readonly GeometryFactory Factory = new();


    /// <summary>Area centroid; callers that allow a degenerate ring can retain the scalar-float vertex-mean fallback.</summary>
    public static Vector2 Centroid(IReadOnlyList<Vector2> ring, bool meanIfDegenerate = false)
    {
        Vector2? fallback = null;
        if (meanIfDegenerate)
        {
            float x = 0, z = 0;
            foreach (var point in ring) { x += point.X; z += point.Y; }
            fallback = new(x / ring.Count, z / ring.Count);
        }
        return Centroid(new PolygonRings(ring), fallback);
    }

    public static Vector2 Centroid(PolygonRings polygon, Vector2? degenerateFallback = null)
    {
        var area = Math.Abs(PlanRings.SignedArea(polygon.Outer)) - polygon.Holes.Sum(hole => Math.Abs(PlanRings.SignedArea(hole)));
        if (area < 0.000001)
            return degenerateFallback ?? throw new ArgumentException("Degenerate polygon.", nameof(polygon));
        var shape = Factory.CreatePolygon(Factory.CreateLinearRing(ClosedCoordinates(polygon.Outer)),
            polygon.Holes.Select(hole => Factory.CreateLinearRing(ClosedCoordinates(hole))).ToArray());
        if (shape.Area < 0.000001)
            return degenerateFallback ?? throw new ArgumentException("Degenerate polygon.", nameof(polygon));
        var center = shape.Centroid;
        return new((float)center.X, (float)center.Y);
    }

    /// <summary>Area-weighted centroid of independent filled rings; ring orientation does not turn a component into a hole.</summary>
    public static Vector2 Centroid(IEnumerable<IReadOnlyList<Vector2>> rings)
    {
        var components = rings.Select(ring => Factory.CreatePolygon(Factory.CreateLinearRing(ClosedCoordinates(ring)))).ToArray();
        var shape = Factory.CreateMultiPolygon(components);
        if (shape.IsEmpty || shape.Area < 0.000001) throw new ArgumentException("Independent rings need nonzero area.", nameof(rings));
        var center = shape.Centroid;
        return new((float)center.X, (float)center.Y);
    }

    public static float DistanceToEdges(PolygonRings polygon, Vector2 point)
    {
        var best = PlanRings.DistanceToRing(point, polygon.Outer);
        foreach (var hole in polygon.Holes) best = MathF.Min(best, PlanRings.DistanceToRing(point, hole));
        return best;
    }

    /// <summary>Distance using existing single-precision edge frames, without renormalizing their directions.</summary>
    public static float DistanceToEdges(Vector2 point, IReadOnlyList<Vector2> ring,
        IReadOnlyList<Vector2> directions, IReadOnlyList<float> lengths)
    {
        var best = float.MaxValue;
        for (var i = 0; i < ring.Count; i++)
        {
            var t = Math.Clamp(Vector2.Dot(point - ring[i], directions[i]), 0f, lengths[i]);
            best = MathF.Min(best, Vector2.Distance(point, ring[i] + directions[i] * t));
        }
        return best;
    }

    private static Coordinate[] ClosedCoordinates(IReadOnlyList<Vector2> ring)
    {
        var points = PlanRings.OpenRing(ring);
        return points.Append(points[0]).Select(point => new Coordinate(point.X, point.Y)).ToArray();
    }

    /// <summary>Intersect real plan areas with NTS, returning hole-free triangles for point-permission storage.</summary>
    public static IReadOnlyList<IReadOnlyList<Vector2>> IntersectionTriangles(PolygonRings a, PolygonRings b, IReadOnlyList<PolygonRings>? exclusions = null)
    {
        var intersection = Shape(a).Intersection(Shape(b));
        if (exclusions is not null)
            foreach (var excluded in exclusions) intersection = intersection.Difference(Shape(excluded));
        if (intersection.IsEmpty || intersection.Area == 0) return [];
        var triangles = NetTopologySuite.Triangulate.Polygon.PolygonTriangulator.Triangulate(intersection);
        var result = new List<IReadOnlyList<Vector2>>();
        for (var index = 0; index < triangles.NumGeometries; index++)
        {
            var polygon = (Polygon)triangles.GetGeometryN(index);
            result.Add(polygon.ExteriorRing.Coordinates.Take(3).Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray());
        }
        return result.AsReadOnly();
    }

    private static Polygon Shape(PolygonRings polygon) => Factory.CreatePolygon(Factory.CreateLinearRing(ClosedCoordinates(polygon.Outer)),
        polygon.Holes.Select(hole => Factory.CreateLinearRing(ClosedCoordinates(hole))).ToArray());

    /// <summary>Cold-build line/area intersection, measured along the original segment for wall subtraction.</summary>
    public static IReadOnlyList<(float Start, float End)> SegmentIntervals(Vector2 a, Vector2 b, IReadOnlyList<Vector2> ring)
    {
        var direction = Vector2.Normalize(b - a);
        var line = Factory.CreateLineString([new Coordinate(a.X, a.Y), new Coordinate(b.X, b.Y)]);
        var intersection = line.Intersection(Shape(new(ring)));
        var result = new List<(float Start, float End)>();
        for (var index = 0; index < intersection.NumGeometries; index++)
        {
            if (intersection.GetGeometryN(index) is not LineString segment || segment.NumPoints < 2) continue;
            var first = segment.GetCoordinateN(0); var last = segment.GetCoordinateN(segment.NumPoints - 1);
            var start = Vector2.Dot(new Vector2((float)first.X, (float)first.Y) - a, direction);
            var end = Vector2.Dot(new Vector2((float)last.X, (float)last.Y) - a, direction);
            if (end < start) (start, end) = (end, start);
            if (end > start) result.Add((start, end));
        }
        return result.OrderBy(interval => interval.Start).ThenBy(interval => interval.End).ToArray();
    }
}
