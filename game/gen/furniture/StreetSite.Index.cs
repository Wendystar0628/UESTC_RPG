using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Furniture;

internal sealed partial class StreetSite
{
    private sealed record Outline(string Kind, PolygonRings Rings, Vector2 Min, Vector2 Max);

    /// <summary>A pedestrian paving surface: a polygon, or a centre line with a half width (<c>Road</c>: the network walk it is, when it is one).</summary>
    private sealed record Paving(PolygonRings? Rings, IReadOnlyList<Vector2>? Line, float Half, NetworkLine? Road = null);

    private static float Edge(PolygonRings rings, Vector2 p) =>
        rings.Holes.Aggregate(PlanGeometry.DistanceToRing(p, rings.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h)));

    /// <summary>The nearest point to <paramref name="p"/> on a polyline (or ring) with the unit direction of the segment it lies on.</summary>
    private static (Vector2 Point, Vector2 Tangent) NearestOn(Vector2 p, IReadOnlyList<Vector2> line, bool closed = false)
    {
        var best = line[0];
        var tangent = Vector2.UnitX;
        var bestD = float.MaxValue;
        var count = closed ? line.Count + 1 : line.Count;
        for (var i = 1; i < count; i++)
        {
            var a = line[i - 1]; var ab = line[i % line.Count] - a;
            var len2 = ab.LengthSquared();
            var q = len2 < 1e-8f ? a : a + ab * Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
            var d = Vector2.DistanceSquared(p, q);
            if (d >= bestD) continue;
            bestD = d;
            best = q;
            if (len2 >= 1e-8f) tangent = ab / MathF.Sqrt(len2);
        }
        return (best, tangent);
    }

    /// <summary>Road areas of a feature, part by part (a part's own kind and props win over its feature's, as WP-15 reads them).</summary>
    private static IEnumerable<(JsonElement? Props, OverlayGeometry Geometry)> RoadSources(WorldFeature feature)
    {
        if (feature.Parts.Count == 0)
        {
            if (feature.Kind.Value == "road") yield return (feature.Props, feature.Geometry);
            yield break;
        }
        foreach (var part in feature.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
        {
            var props = part.Props ?? feature.Props;
            var kind = part.Props is { ValueKind: JsonValueKind.Object } own && own.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String
                ? k.GetString() : feature.Kind.Value;
            if (kind == "road") yield return (props, part.Geometry);
        }
    }

    private void IndexPaving(OverlayGeometry geometry, JsonElement? props, float half = 0f, NetworkLine? road = null)
    {
        switch (geometry)
        {
            case PolygonGeometry polygon:
                IndexPaving(polygon.Rings);
                break;
            case MultiPolygonGeometry multi:
                foreach (var rings in multi.Polygons) IndexPaving(rings);
                break;
            case PolylineGeometry { Line.Points: { Count: >= 2 } points }:
                if (half <= 0f)
                    half = (props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number
                        ? MathF.Max(w.GetSingle(), 2f) : 2.5f) * 0.5f;
                var pad = new Vector2(half);
                _paving.AddBox(points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - pad,
                    points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + pad, new Paving(null, points, half, road));
                break;
        }
    }

    private void IndexPaving(PolygonRings rings)
    {
        if (rings.Outer.Count < 3) return;
        _paving.AddBox(rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min), rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max),
            new Paving(rings, null, 0f));
    }

    private static float LineDistanceSquared(Vector2 p, IReadOnlyList<Vector2> line)
    {
        var best = float.MaxValue;
        for (var i = 1; i < line.Count; i++) best = MathF.Min(best, PlanGeometry.SegmentDistanceSquared(p, line[i - 1], line[i]));
        return best;
    }

    /// <summary>The polygons of a feature of one kind (a part's own kind wins over its feature's), as the world model groups them.</summary>
    private static IEnumerable<PolygonRings> Polygons(WorldFeature feature, string kind)
    {
        IEnumerable<OverlayGeometry> geometries;
        if (feature.Parts.Count == 0)
            geometries = feature.Kind.Value == kind ? [feature.Geometry] : [];
        else
            geometries = feature.Parts.Where(part =>
                (part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var declared) ? declared.GetString() : feature.Kind.Value) == kind)
                .Select(WorldFeatureGeometry.FacadeGeometryOf);
        foreach (var geometry in geometries)
        {
            IEnumerable<PolygonRings> polygons = geometry switch
            {
                PolygonGeometry polygon => [polygon.Rings],
                MultiPolygonGeometry multi => multi.Polygons,
                _ => [],
            };
            foreach (var rings in polygons)
                if (rings.Outer.Count >= 3) yield return rings;
        }
    }

    private void IndexLine(SpatialGrid<NetworkLine> grid, NetworkLine line)
    {
        if (line.Line.Points.Count < 2) return;
        var pad = new Vector2(HalfWidth(line) + 2f);
        var min = line.Line.Points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - pad;
        var max = line.Line.Points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + pad;
        grid.AddBox(min, max, line);
    }
}
