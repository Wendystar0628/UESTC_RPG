using System.Numerics;
using Campus.Contracts;
using NetTopologySuite.Geometries;

namespace Campus.Gen.Geometry;

public static partial class PlanGeometry
{
    private static readonly GeometryFactory Factory = new();

    /// <summary>Drop adjacent and closing duplicates, preserving the first point and traversal direction.</summary>
    public static Vector2[] DeduplicateRing(IReadOnlyList<Vector2> ring, float distanceSquared = 1e-8f)
    {
        var points = new List<Vector2>(ring.Count);
        foreach (var point in ring)
            if (points.Count == 0 || Vector2.DistanceSquared(points[^1], point) > distanceSquared) points.Add(point);
        while (points.Count > 1 && Vector2.DistanceSquared(points[0], points[^1]) <= distanceSquared) points.RemoveAt(points.Count - 1);
        return points.ToArray();
    }

    /// <summary>Clean short edges, collinear vertices and reversing spikes, then orient without rotating the start vertex.</summary>
    public static Vector2[]? CleanRing(IReadOnlyList<Vector2> input, bool outer = true, float minEdge = 0,
        float collinearTolerance = 0, float duplicateTolerance = 0, float minimumArea = 0, float reversalTolerance = 0,
        int maxPasses = int.MaxValue, float? duplicateDistanceSquared = null)
    {
        if (input.Any(point => !VectorMath.Finite(point))) return null;
        var points = DeduplicateRing(input, duplicateDistanceSquared ?? duplicateTolerance * duplicateTolerance).ToList();
        var passes = 0;
        for (var changed = true; changed && points.Count >= 3 && passes++ < maxPasses;)
        {
            changed = false;
            for (var i = 0; i < points.Count && points.Count >= 3; i++)
            {
                var a = points[(i + points.Count - 1) % points.Count];
                var p = points[i];
                var b = points[(i + 1) % points.Count];
                var chord = b - a;
                var chordLength = chord.Length();
                var deviation = chordLength > 1e-4f ? MathF.Abs(CrossFloat(chord, p - a)) / chordLength : 0;
                var reverses = Vector2.Dot(p - a, b - p) < 0 && deviation < reversalTolerance;
                if (deviation < collinearTolerance && !reverses && chordLength > 1e-4f || reverses)
                {
                    points.RemoveAt(i--);
                    changed = true;
                }
                else if (Vector2.Distance(p, b) < minEdge)
                {
                    var c = points[(i + 2) % points.Count];
                    var keepP = MathF.Abs(CrossFloat(c - p, a - p));
                    var keepB = MathF.Abs(CrossFloat(c - b, a - b));
                    points.RemoveAt(keepP >= keepB ? (i + 1) % points.Count : i);
                    i--;
                    changed = true;
                }
            }
        }
        if (points.Count < 3) return null;
        var area = (float)SignedArea(points);
        if (MathF.Abs(area) < minimumArea || area == 0) return null;
        if ((area > 0) != outer) points.Reverse();
        return points.ToArray();
    }

    public static Vector2 Centroid(PolygonRings polygon, Vector2? degenerateFallback = null)
    {
        var area = Math.Abs(SignedArea(polygon.Outer)) - polygon.Holes.Sum(hole => Math.Abs(SignedArea(hole)));
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
        var best = DistanceToRing(point, polygon.Outer);
        foreach (var hole in polygon.Holes) best = MathF.Min(best, DistanceToRing(point, hole));
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

    /// <summary>Existing positive-side crossing rule: zero lies on the nonpositive side; adjacent edges are excluded.</summary>
    public static bool SelfIntersects(IReadOnlyList<Vector2> ring)
    {
        for (var i = 0; i < ring.Count; i++)
            for (var j = i + 2; j < ring.Count; j++)
            {
                if (i == 0 && j == ring.Count - 1) continue;
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                var c = ring[j]; var d = ring[(j + 1) % ring.Count];
                if ((CrossFloat(b - a, c - a) > 0) != (CrossFloat(b - a, d - a) > 0)
                    && (CrossFloat(d - c, a - c) > 0) != (CrossFloat(d - c, b - c) > 0)) return true;
            }
        return false;
    }

    private static Coordinate[] ClosedCoordinates(IReadOnlyList<Vector2> ring)
    {
        var points = OpenRing(ring);
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

    public static float CrossFloat(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
