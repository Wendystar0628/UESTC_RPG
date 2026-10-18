using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Furniture;

/// <summary>What an anchor's point lands on (<see cref="StreetSite.LandingAt"/>).</summary>
internal enum Landing : byte { Open, Carriageway, Water, Building }

/// <summary>
/// The world as the furniture placer sees it, indexed on a coarse grid: blocking outlines (buildings, water, courts, parking),
/// road centre lines with their widths, the walks a bench can line (footways, paths, pedestrian streets), pedestrian paving
/// (those walks and steps, and the paved road areas of the features), vehicle road areas, the planted tree rows along the
/// furnished roads, bridges, network trees and the things already placed. Pure and deterministic: every list is filled in
/// feature-ID order and grid lookups never depend on dictionary iteration order.
/// </summary>
internal sealed partial class StreetSite
{
    private const float Cell = 32f;
    private static readonly string[] Blocking = ["building", "water", "sport", "parking"];

    private readonly Dictionary<(int, int), List<Outline>> _outlines = [];
    private readonly Dictionary<(int, int), List<NetworkLine>> _roads = [];
    private readonly Dictionary<(int, int), List<NetworkLine>> _walks = [];
    private readonly Dictionary<(int, int), List<Paving>> _paving = [];
    private readonly Dictionary<(int, int), List<PolygonRings>> _lanes = [];
    private readonly Dictionary<(int, int), List<(NetworkLine Line, float Offset)>> _rows = [];
    private readonly Dictionary<(int, int), List<NetworkLine>> _bridges = [];
    private readonly Dictionary<(int, int), List<Vector2>> _trees = [];
    private readonly Dictionary<(int, int), List<(Vector2 P, float R)>> _occupied = [];
    private readonly List<PolygonRings> _parking = [];

    public StreetSite(IWorldModel world)
    {
        World = world;
        // The effective network (WP-08) publishes a design walk as a network line under its feature's ID and keeps the feature: the
        // line is the walk, so the feature's polyline stays out of the paving below and the walk is filed once, as the line it is.
        var published = world.Network.Roads.Select(r => r.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            foreach (var kind in Blocking)
                foreach (var rings in Polygons(feature, kind))
                {
                    var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                    var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
                    Index(_outlines, min, max, new Outline(kind, rings, min, max));
                    if (kind == "parking") _parking.Add(rings);
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
                            Index(_lanes, rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min), rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max), rings);
            }
        }
        foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
        {
            IndexLine(_roads, road);
            if (Strollable(road)) IndexLine(_walks, road);
            if (Pedestrian(RoadType(road.Props)) && road.Line.Points.Count >= 2)
                IndexPaving(new PolylineGeometry(road.Line), null, HalfWidth(road), road);
        }
        foreach (var bridge in world.Network.Bridges.OrderBy(r => r.Id.Value, StringComparer.Ordinal)) IndexLine(_bridges, bridge);
        foreach (var tree in world.Network.Trees.OrderBy(t => t.Id.Value, StringComparer.Ordinal))
        {
            var p = new Vector2(tree.Position.X, tree.Position.Z);
            Index(_trees, p, p, p);
        }
    }

    public IWorldModel World { get; }

    /// <summary>Half width of a network line's surface (the width WP-15 paves; never below a footpath's).</summary>
    public static float HalfWidth(NetworkLine line) => MathF.Max(line.Width, 2f) * 0.5f;

    /// <summary>Walks a bench may line and face (WP-15 paves them as walks or pedestrian plazas); steps and cycleways are not.</summary>
    public static bool Strollable(NetworkLine line) => RoadType(line.Props) is "footway" or "path" or "pedestrian";

    /// <summary>True for a pedestrian network line (a walk or steps), the lines a plaza may carry.</summary>
    public static bool IsPedestrian(NetworkLine line) => Pedestrian(RoadType(line.Props));

    /// <summary>Pedestrian paving types: the strollable walks and steps.</summary>
    private static bool Pedestrian(string? type) => type is "footway" or "path" or "pedestrian" or "steps";

    /// <summary>Road type as WP-15 reads it (GroundClassifier): <c>roadType</c>, else <c>highway</c>.</summary>
    private static string? RoadType(JsonElement? props)
    {
        if (props is not { ValueKind: JsonValueKind.Object } p) return null;
        foreach (var name in new[] { "roadType", "highway" })
            if (p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }
}
