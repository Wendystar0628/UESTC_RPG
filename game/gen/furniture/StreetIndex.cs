using System.Text.Json;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using static Campus.Gen.Furniture.StreetNetwork;

namespace Campus.Gen.Furniture;

/// <summary>Indexes immutable world outlines, roads, paving, bridges and trees in stable ID order.</summary>
internal sealed class StreetIndex
{
    internal const float Cell = 32f;
    private static readonly string[] Blocking = ["building", "water", "sport", "parking"];

    public SpatialGrid<Outline> Outlines { get; } = new(Cell);
    public SpatialGrid<NetworkLine> Roads { get; } = new(Cell);
    public SpatialGrid<NetworkLine> Walks { get; } = new(Cell);
    public SpatialGrid<Paving> Surfaces { get; } = new(Cell);
    public SpatialGrid<PolygonRings> Lanes { get; } = new(Cell);
    public SpatialGrid<NetworkLine> Bridges { get; } = new(Cell);
    public SpatialGrid<Vector2> Trees { get; } = new(Cell);
    public List<PolygonRings> Parking { get; } = [];

    public StreetIndex(IWorldModel world)
    {
        // The effective network (WP-08) publishes a design walk as a network line under its feature's ID and keeps the feature: the
        // line is the walk, so the feature's polyline stays out of the paving below and the walk is filed once, as the line it is.
        var published = world.Network.Roads.Select(r => r.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            foreach (var kind in Blocking)
                foreach (var rings in WorldFeatureGeometry.FacadePolygonsOf(feature, kind))
                {
                    var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                    var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
                    Outlines.AddBox(min, max, new Outline(kind, rings, min, max));
                    if (kind == "parking") Parking.Add(rings);
                }
            foreach (var (props, geometry) in RoadSources(feature))
            {
                var type = RoadType(props);
                if (Pedestrian(type))
                {
                    if (geometry is not PolylineGeometry || !published.Contains(feature.Id.Value)) IndexPaving(geometry, props);
                }
                else if (CarriagewayType(type))
                    foreach (var rings in geometry switch { PolygonGeometry polygon => [polygon.Rings], MultiPolygonGeometry multi => multi.Polygons, _ => [] })
                        if (rings.Outer.Count >= 3)
                            Lanes.AddBox(rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min), rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max), rings);
            }
        }
        foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
        {
            IndexLine(Roads, road);
            if (Strollable(road)) IndexLine(Walks, road);
            if (Pedestrian(RoadType(road.Props)) && road.Line.Points.Count >= 2)
                IndexPaving(new PolylineGeometry(road.Line), null, HalfWidth(road), road);
        }
        foreach (var bridge in world.Network.Bridges.OrderBy(r => r.Id.Value, StringComparer.Ordinal)) IndexLine(Bridges, bridge);
        foreach (var tree in world.Network.Trees.OrderBy(t => t.Id.Value, StringComparer.Ordinal))
        {
            var p = new Vector2(tree.Position.X, tree.Position.Z);
            Trees.AddBox(p, p, p);
        }
    }

    internal sealed record Outline(string Kind, PolygonRings Rings, Vector2 Min, Vector2 Max);

    /// <summary>A pedestrian paving surface: a polygon, or a centre line with a half width (<c>Road</c>: the network walk it is, when it is one).</summary>
    internal sealed record Paving(PolygonRings? Rings, IReadOnlyList<Vector2>? Line, float Half, NetworkLine? Road = null);

    internal static float Edge(PolygonRings rings, Vector2 p) =>
        rings.Holes.Aggregate(PlanGeometry.DistanceToRing(p, rings.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h)));

    /// <summary>Road areas of a feature, part by part (a part's own kind and props win over its feature's, as WP-15 reads them).</summary>
    private static IEnumerable<(JsonElement? Props, WorldGeometry Geometry)> RoadSources(WorldFeature feature)
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

    private void IndexPaving(WorldGeometry geometry, JsonElement? props, float half = 0f, NetworkLine? road = null)
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
                Surfaces.AddBox(points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - pad,
                    points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + pad, new Paving(null, points, half, road));
                break;
        }
    }

    private void IndexPaving(PolygonRings rings)
    {
        if (rings.Outer.Count < 3) return;
        Surfaces.AddBox(rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min), rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max),
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
