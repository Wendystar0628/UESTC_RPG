using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.Sites;

/// <summary>Plan envelopes from authored sources only; padding covers a site's actual overhangs and height-read footprint.</summary>
public static class SiteRanges
{
    public static MapRect Point(Vector2 anchor, float padding = 0f) =>
        new(anchor - new Vector2(padding), anchor + new Vector2(padding));

    public static MapRect Bounds(IEnumerable<Vector2> points, float padding = 0f)
    {
        using var source = points.GetEnumerator();
        if (!source.MoveNext()) throw new ArgumentException("A site range needs at least one source point.", nameof(points));
        var min = source.Current;
        var max = min;
        while (source.MoveNext())
        {
            min = Vector2.Min(min, source.Current);
            max = Vector2.Max(max, source.Current);
        }
        return new(min - new Vector2(padding), max + new Vector2(padding));
    }

    public static MapRect Bounds(WorldFeature feature, float padding = 0f) =>
        Bounds(Points(feature.Geometry).Concat(feature.Parts.SelectMany(part =>
            Points(part.Geometry).Concat(part.FacadeGeometry is { } facade ? Points(facade) : []))), padding);

    private static IEnumerable<Vector2> Points(WorldGeometry geometry) => geometry switch
    {
        PointGeometry point => [new(point.Position.X, point.Position.Z)],
        PolylineGeometry line => line.Line.Points,
        PolygonGeometry polygon => polygon.Rings.Outer,
        MultiPolygonGeometry multi => multi.Polygons.SelectMany(polygon => polygon.Outer),
        _ => throw new ArgumentOutOfRangeException(nameof(geometry)),
    };
}
