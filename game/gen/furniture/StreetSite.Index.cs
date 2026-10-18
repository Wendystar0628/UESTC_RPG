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

    private void IndexLine(SpatialGrid<NetworkLine> grid, NetworkLine line)
    {
        if (line.Line.Points.Count < 2) return;
        var pad = new Vector2(HalfWidth(line) + 2f);
        var min = line.Line.Points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - pad;
        var max = line.Line.Points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + pad;
        grid.AddBox(min, max, line);
    }
}
