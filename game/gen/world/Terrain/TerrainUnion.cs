using System.Numerics;
using Campus.Contracts;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Precision;

namespace Campus.Gen.World;

/// <summary>Planar union on a centimetre grid; only the final terrain rings return to Vector2.</summary>
internal static class TerrainUnion
{
    // World X/Z metres map to NTS X/Y doubles. A fixed 100 units/metre is sufficient for the demo.
    private static readonly PrecisionModel Precision = new(100);
    private static readonly GeometryFactory Factory = new(Precision);

    public static TerrainRegion Build(IEnumerable<TerrainRegion> regions)
    {
        var polygons = regions.SelectMany(region => region.Polygons).Select(ToGeometry)
            .Order(Comparer<Geometry>.Create((a, b) => a.CompareTo(b))).ToArray();
        var union = UnaryUnionNG.Union(polygons, Factory, Precision);
        if (union.IsEmpty) throw new ArgumentException("Terrain union has no area at centimetre precision.");
        union.Normalize();
        return new(Polygons(union).Select(polygon => new PolygonRings(
            ToRing(polygon.ExteriorRing),
            Enumerable.Range(0, polygon.NumInteriorRings).Select(i => ToRing(polygon.GetInteriorRingN(i))).ToArray())));
    }

    private static Geometry ToGeometry(PolygonRings polygon)
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

    private static IEnumerable<Polygon> Polygons(Geometry geometry)
    {
        if (geometry.IsEmpty) yield break;
        if (geometry is Polygon polygon) { yield return polygon; yield break; }
        if (geometry is not GeometryCollection collection)
            throw new ArgumentException("Terrain union did not produce polygonal geometry.");
        for (var i = 0; i < collection.NumGeometries; i++)
            foreach (var part in Polygons(collection.GetGeometryN(i))) yield return part;
    }
}
