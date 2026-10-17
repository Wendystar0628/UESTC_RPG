using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Position queries over registered regions and network widths. Physical support is resolved at runtime.</summary>
internal sealed class WorldWalkRegions
{
    private readonly PolygonRings _boundary;
    private readonly PolygonRings[] _forbidden;
    private readonly PolygonRings[] _wade;
    private readonly PolygonRings[] _water;
    private readonly PolygonRings[] _bridges;
    private readonly NetworkLine[] _waterways;
    private readonly NetworkLine[] _bridgeLines;
    private readonly Polyline[] _seals;

    public WorldWalkRegions(IEnumerable<WorldFeature> source, PolygonRings boundary, WorldNetwork network)
    {
        var features = source.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        _boundary = boundary;
        _forbidden = Areas("viewOnly").Concat(Areas("seal")).ToArray();
        _wade = Areas("wade");
        _water = Areas("water");
        _bridges = Areas("bridge-deck");
        _waterways = network.Waterways.ToArray();
        _seals = features.Where(feature => feature.Kind.Value == "seal" && feature.Geometry is PolylineGeometry)
            .Select(feature => ((PolylineGeometry)feature.Geometry).Line).ToArray();
        var replaced = new HashSet<FeatureId>();
        foreach (var feature in features)
        {
            if (!WorldFeatureKinds.Parts(feature, "bridge-deck").Any() ||
                feature.Props is not { ValueKind: JsonValueKind.Object } props) continue;
            if (props.TryGetProperty("axis", out var axis) && axis.ValueKind == JsonValueKind.String)
                replaced.Add(new(axis.GetString()!));
            if (props.TryGetProperty("walks", out var walks) && walks.ValueKind == JsonValueKind.Array)
                foreach (var walk in walks.EnumerateArray())
                    if (walk.ValueKind == JsonValueKind.String) replaced.Add(new(walk.GetString()!));
        }
        _bridgeLines = network.Bridges.Where(line => !replaced.Contains(line.Id)).ToArray();

        PolygonRings[] Areas(string kind) => features.SelectMany(feature => WorldFeatureKinds.Parts(feature, kind))
            .SelectMany(part => WorldModelLoader.Polygons(part.Geometry)).ToArray();
    }

    public WalkClass At(float x, float z)
    {
        var point = new Vector2(x, z);
        if (!PlanGeometry.Contains(_boundary, point)) return WalkClass.Outside;
        if (Contains(_forbidden, point)) return WalkClass.Forbidden;
        foreach (var line in _seals)
            if (Contains(line, .3f, point)) return WalkClass.Forbidden;
        if (Contains(_wade, point)) return WalkClass.Wade;
        if (Contains(_bridges, point) || Contains(_bridgeLines, point)) return WalkClass.Walk;
        return Contains(_water, point) || Contains(_waterways, point) ? WalkClass.Water : WalkClass.Walk;
    }

    private static bool Contains(PolygonRings[] regions, Vector2 point)
    {
        foreach (var polygon in regions)
            if (PlanGeometry.Contains(polygon, point)) return true;
        return false;
    }

    private static bool Contains(NetworkLine[] lines, Vector2 point)
    {
        foreach (var line in lines)
            if (Contains(line.Line, line.Width, point)) return true;
        return false;
    }

    private static bool Contains(Polyline line, float width, Vector2 point)
    {
        if (width <= 0) return false;
        var radiusSquared = width * width * .25f;
        for (var i = 1; i < line.Points.Count; i++)
            if (PlanGeometry.SegmentDistanceSquared(point, line.Points[i - 1], line.Points[i]) <= radiusSquared)
                return true;
        return false;
    }
}
