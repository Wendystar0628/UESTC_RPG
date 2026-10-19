using System.Numerics;
using Campus.Contracts;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Precision;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace Campus.Gen.World;

/// <summary>Planar union on a centimetre grid; only the final terrain rings return to Vector2.</summary>
internal static class TerrainUnion
{
    // World X/Z metres map to NTS X/Y doubles. A fixed 100 units/metre is sufficient for the demo.
    private static readonly PrecisionModel Precision = new(100);
    private static readonly GeometryFactory Factory = new(Precision);

    public static bool Covers(TerrainRegion region, TerrainRegion candidate) => GeometryOf(region).Covers(GeometryOf(candidate));

    public static bool CoversPath(TerrainRegion region, TerrainPath path, double from, double to)
    {
        var coordinates = new[] { from }.Concat(path.VertexStations.Where(station => station > from && station < to)).Append(to)
            .Select(station => { var p = path.AtDouble(station); return new Coordinate(p.X, p.Z); }).ToArray();
        return GeometryOf(region).Covers(GeometryPrecisionReducer.Reduce(Factory.CreateLineString(coordinates), Precision));
    }

    public static IReadOnlyList<(double From, double To)> CoveredPathIntervals(TerrainPath path, TerrainRegion? footprint,
        NetworkLine structure, double join)
    {
        var alignment = Factory.CreateLineString(structure.Line.Points.Select(point => new Coordinate(point.X, point.Y)).ToArray());
        // Prefer the registered physical deck. A line-only registration uses its real width
        // with flat ends; only the ordinary planar join tolerance extends those ends.
        var deck = footprint is not null ? GeometryOf(footprint)
            : structure.Width > 0 ? alignment.Buffer(structure.Width / 2d, EndCapStyle.Flat) : alignment;
        var cover = deck.Buffer(join);
        var intervals = new List<(double From, double To)>();
        for (var i = 1; i < path.Line.Points.Count; i++)
        {
            var a = path.Line.Points[i - 1]; var c = path.Line.Points[i];
            var line = Factory.CreateLineString([new(a.X, a.Y), new(c.X, c.Y)]);
            if (!line.EnvelopeInternal.Intersects(cover.EnvelopeInternal)) continue;
            var segment = new LineSegment(line.GetCoordinateN(0), line.GetCoordinateN(1));
            var length = path.VertexStations[i] - path.VertexStations[i - 1];
            foreach (var part in LinearComponentExtracter.GetLines(line.Intersection(cover)))
            {
                var stations = part.Coordinates.Select(point => path.VertexStations[i - 1]
                    + Math.Clamp(segment.SegmentFraction(point), 0, 1) * length).ToArray();
                if (stations.Length > 1) intervals.Add((stations.Min(), stations.Max()));
            }
        }
        // Merge actual covered pieces only. In particular, a bridge touching one endpoint
        // cannot exclude the rest of an approach, nor a second uncovered water crossing.
        var merged = new List<(double From, double To)>();
        foreach (var interval in intervals.OrderBy(interval => interval.From).ThenBy(interval => interval.To))
        {
            if (merged.Count > 0 && interval.From <= merged[^1].To)
                merged[^1] = (merged[^1].From, Math.Max(merged[^1].To, interval.To));
            else merged.Add(interval);
        }
        return merged;
    }

    private static NtsGeometry GeometryOf(TerrainRegion region) =>
        UnaryUnionNG.Union(region.Polygons.Select(ToGeometry).ToArray(), Factory, Precision);

    public static TerrainRegion Build(IEnumerable<TerrainRegion> regions)
    {
        var polygons = regions.SelectMany(region => region.Polygons).Select(ToGeometry)
            .Order(Comparer<NtsGeometry>.Create((a, b) => a.CompareTo(b))).ToArray();
        var union = UnaryUnionNG.Union(polygons, Factory, Precision);
        if (union.IsEmpty) throw new ArgumentException("Terrain union has no area at centimetre precision.");
        union.Normalize();
        return new(Polygons(union).Select(polygon => new PolygonRings(
            ToRing(polygon.ExteriorRing),
            Enumerable.Range(0, polygon.NumInteriorRings).Select(i => ToRing(polygon.GetInteriorRingN(i))).ToArray())));
    }

    private static NtsGeometry ToGeometry(PolygonRings polygon)
    {
        var geometry = Factory.CreatePolygon(ToRing(polygon.Outer), polygon.Holes.Select(ToRing).ToArray());
        // Apply the same grid even for a lone/disjoint polygon, which unary union can otherwise pass through.
        var reduced = GeometryPrecisionReducer.Reduce(geometry, Precision);
        reduced.Normalize();
        return reduced;
    }

    private static LinearRing ToRing(IReadOnlyList<Vector2> ring)
    {
        var coordinates = new Coordinate[ring.Count + 1];
        for (var i = 0; i < ring.Count; i++) coordinates[i] = new((double)ring[i].X, (double)ring[i].Y);
        coordinates[^1] = coordinates[0].Copy();
        return Factory.CreateLinearRing(coordinates);
    }

    private static IReadOnlyList<Vector2> ToRing(LineString ring) => ring.Coordinates.Take(ring.NumPoints - 1).Select(point =>
    {
        var x = (float)point.X; var z = (float)point.Y;
        if (!float.IsFinite(x) || !float.IsFinite(z))
            throw new ArgumentException("Terrain union node is not representable as a finite Vector2.");
        return new Vector2(x == 0 ? 0 : x, z == 0 ? 0 : z);
    }).ToArray();

    private static IEnumerable<Polygon> Polygons(NtsGeometry geometry)
    {
        if (geometry.IsEmpty) yield break;
        if (geometry is Polygon polygon) { yield return polygon; yield break; }
        if (geometry is not GeometryCollection collection)
            throw new ArgumentException("Terrain union did not produce polygonal geometry.");
        for (var i = 0; i < collection.NumGeometries; i++)
            foreach (var part in Polygons(collection.GetGeometryN(i))) yield return part;
    }
}
