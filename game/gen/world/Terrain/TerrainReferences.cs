using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

internal sealed record TerrainRoad(FeatureId Feature, FeatureId Source, TerrainPath Path, double Width, string Kind, TerrainRegion? Footprint);

internal sealed class TerrainReferences(TerrainCompileContext context)
{
    public TerrainRegion Area(FeatureId id, string kind, string file, string op)
    {
        if (!context.Features.TryGetValue(id, out var feature)) throw new WorldDataException(file, op, $"Missing {kind} entity '{id.Value}'.");
        var polygons = WorldFeatureKinds.Parts(feature, kind).SelectMany(p => WorldFeatureGeometry.PolygonsOf(p.Geometry)).ToArray();
        if (polygons.Length == 0) throw new WorldDataException(file, op, $"Entity '{id.Value}' has no actual {kind} polygon parts.");
        return new(polygons);
    }

    public TerrainRegion PadArea(FeatureId id, string file, string op)
    {
        if (!context.Features.TryGetValue(id, out var feature)) throw new WorldDataException(file, op, $"Missing pad entity '{id.Value}'.");
        var buildings = WorldFeatureKinds.Parts(feature, "building").SelectMany(p => WorldFeatureGeometry.PolygonsOf(p.Geometry)).ToArray();
        if (buildings.Length > 0) return new(buildings);
        if (feature.Kind.Value is not ("plaza" or "platform" or "pavilion" or "shelter" or "kiosk") && !IsArealRoad(id))
            throw new WorldDataException(file, op, $"Pad entity '{id.Value}' is neither an actual building nor an explicitly typed supported structure/plaza.");
        return new(WorldFeatureGeometry.PolygonsOf(feature.Geometry));
    }

    public TerrainRoad Road(FeatureId id, string file, string op)
    {
        if (IsArealRoad(id)) throw new WorldDataException(file, op, $"Road '{id.Value}' is areal paving, not an ordered highway centreline. A polygon perimeter is not a centreline.");
        var lines = ResolveLines(id, context.Network.Roads);
        if (lines.Length != 1)
            throw new WorldDataException(file, op, $"Road '{id.Value}' must resolve to one unambiguous ordered source centreline; found {lines.Length}.");
        var line = lines[0];
        if (!float.IsFinite(line.Width) || line.Width <= 0) throw new WorldDataException(file, op, $"Road '{id.Value}' has no actual positive width.");
        var kind = RoadKind(line);
        TerrainRegion? footprint = null;
        if (context.Features.TryGetValue(id, out var feature))
        {
            var area = WorldFeatureKinds.Parts(feature, "road").SelectMany(p => WorldFeatureGeometry.PolygonsOf(p.Geometry)).ToArray();
            if (area.Length > 0) footprint = new(area);
        }
        return new(id, line.Id, new(line.Line), line.Width, kind, footprint);
    }

    public bool IsArealRoad(FeatureId id)
    {
        var lines = ResolveLines(id, context.Network.Roads);
        if (lines.Length > 0 && lines.All(line => line.Props is { } props && props.TryGetProperty("area", out var area) && area.GetString() == "yes")) return true;
        if (lines.Length != 0 || !context.Features.TryGetValue(id, out var feature)) return false;
        var parts = WorldFeatureKinds.Parts(feature, "road").ToArray();
        return parts.Length > 0 && parts.All(part => part.SourceId.Split(':') is { Length: >= 3 } source && source[1] == "relation"
            && part.Geometry is PolygonGeometry or MultiPolygonGeometry);
    }

    public (TerrainPath Path, double Width) WaterLine(FeatureId id, string file, string op)
    {
        var lines = ResolveLines(id, context.Network.Waterways);
        if (lines.Length != 1 || lines[0].Width <= 0)
            throw new WorldDataException(file, op, $"Water '{id.Value}' needs an explicit line/width or one unambiguous real waterway source.");
        return (new(lines[0].Line), lines[0].Width);
    }

    public bool HasWaterLine(FeatureId id) => ResolveLines(id, context.Network.Waterways).Length != 0;
    public IReadOnlyList<Vector2> WaterNodes(FeatureId id) => ResolveLines(id, context.Network.Waterways).SelectMany(line => line.Line.Points).ToArray();
    public IReadOnlyList<Vector2> WaterEndpoints(FeatureId id) => ResolveLines(id, context.Network.Waterways)
        .SelectMany(line => new[] { line.Line.Points[0], line.Line.Points[^1] }).ToArray();
    public bool IsNaturalStream(FeatureId id) => ResolveLines(id, context.Network.Waterways)
        .Any(line => line.Props is { } props && props.TryGetProperty("waterway", out var type) && type.GetString() == "stream");

    public bool IsChannelWater(WorldFeature feature)
    {
        if (HasWaterLine(feature.Id)) return true;
        foreach (var part in WorldFeatureKinds.Parts(feature, "water"))
        {
            if (part.Props is not { } props || !props.TryGetProperty("sourceTags", out var tags)) continue;
            if (tags.TryGetProperty("water", out var water) && water.GetString() is "river" or "stream" or "canal" or "drain" or "ditch") return true;
            if (tags.TryGetProperty("waterway", out var channel) && channel.GetString() is "river" or "stream" or "canal" or "drain" or "ditch") return true;
        }
        return false;
    }

    public bool IsUndergroundWater(FeatureId id)
    {
        var registered = context.Network.Culverts.Select(line => line.Id).ToHashSet();
        var declared = context.Network.Waterways.Where(line => line.Props is { } props && props.TryGetProperty("tunnel", out var tunnel) && tunnel.GetString() == "culvert"
            && props.TryGetProperty("layer", out var layer) && layer.ValueKind == System.Text.Json.JsonValueKind.String
            && int.TryParse(layer.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var depth) && depth < 0)
            .Select(line => line.Id).ToHashSet();
        var underground = declared.Where(registered.Contains).ToHashSet();
        if (!context.Features.TryGetValue(id, out var feature)) return false;
        var parts = WorldFeatureKinds.Parts(feature, "water").ToArray();
        if (feature.Parts.Count == 0)
        {
            if (declared.Contains(id) && !registered.Contains(id)) throw new WorldDataException("network.json", id.Value, "Underground water requires matching WorldNetwork.Culverts registration.");
            return underground.Contains(id);
        }
        if (parts.Any(part => declared.Contains(SourceNetworkId(part.SourceId)) && !registered.Contains(SourceNetworkId(part.SourceId))))
            throw new WorldDataException("network.json", id.Value, "Underground water requires matching WorldNetwork.Culverts registration.");
        var count = parts.Count(part => underground.Contains(SourceNetworkId(part.SourceId)));
        if (count != 0 && count != parts.Length)
            throw new WorldDataException("network.json", id.Value, "One water entity mixes registered underground culvert parts with surface parts; separate their explicit water identities before terrain compilation.");
        return count > 0;
    }

    public IEnumerable<TerrainRoad> AutomaticRoads()
    {
        foreach (var line in context.Network.Roads.OrderBy(line => line.Id.Value, StringComparer.Ordinal))
        {
            if (line.Props is not { ValueKind: System.Text.Json.JsonValueKind.Object } props) continue;
            var kind = RoadKind(line);
            if (string.IsNullOrWhiteSpace(kind)) continue;
            if (props.TryGetProperty("area", out var area) && area.GetString() == "yes") continue;
            if (line.Width <= 0) throw new WorldDataException("network.json", line.Id.Value, "A line-shaped highway requires its actual positive width.");
            var parts = context.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal)
                .SelectMany(feature => WorldFeatureKinds.Parts(feature, "road").Where(part => SourceNetworkId(part.SourceId) == line.Id)).ToArray();
            var polygons = parts.SelectMany(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)).ToArray();
            yield return new(line.Id, line.Id, new(line.Line), line.Width, kind, polygons.Length > 0 ? new(polygons) : null);
        }
    }

    private static string RoadKind(NetworkLine line)
    {
        if (line.Props is not { ValueKind: System.Text.Json.JsonValueKind.Object } props) return "";
        if (props.TryGetProperty("roadType", out var roadType)) return roadType.GetString() ?? "";
        return props.TryGetProperty("highway", out var highway) ? highway.GetString() ?? "" : "";
    }

    public WorldFeature Structure(FeatureId id, string file, string op, params string[] allowedKinds)
    {
        if (!context.Features.TryGetValue(id, out var feature)) throw new WorldDataException(file, op, $"Missing structural entity '{id.Value}'.");
        var parts = feature.Parts.Where(part => allowedKinds.Contains(
            part.Props is { } props && props.TryGetProperty("kind", out var kind) ? kind.GetString() : feature.Kind.Value, StringComparer.Ordinal)).ToArray();
        if (feature.Parts.Count > 0 ? parts.Length == 0 : !allowedKinds.Contains(feature.Kind.Value, StringComparer.Ordinal))
            throw new WorldDataException(file, op, $"Structural entity '{id.Value}' does not have the required declared kind ({string.Join(",", allowedKinds)}).");
        if (feature.Parts.Count > 0) feature = feature with { Parts = parts };
        if (!StructuralGeometry(feature).Any(geometry => WorldFeatureGeometry.PolygonsOf(geometry).Any() || geometry is PolylineGeometry))
            throw new WorldDataException(file, op, $"Structural entity '{id.Value}' has no footprint or alignment; a label alone is not structural evidence.");
        return feature;
    }

    private NetworkLine[] ResolveLines(FeatureId id, IReadOnlyList<NetworkLine> source)
    {
        var direct = source.Where(line => line.Id == id).ToArray();
        if (direct.Length != 0) return direct;
        if (!context.Features.TryGetValue(id, out var feature)) return [];
        var ids = feature.Parts.Select(part => SourceNetworkId(part.SourceId)).ToHashSet();
        return source.Where(line => ids.Contains(line.Id)).OrderBy(line => line.Id.Value, StringComparer.Ordinal).ToArray();
    }

    private FeatureId SourceNetworkId(string source)
    {
        var colon = source.IndexOf(':');
        if (colon < 0 || string.IsNullOrWhiteSpace(context.EntityPrefix)) return default;
        return new(OsmFeatureIds.FromPart(context.EntityPrefix, source).Source);
    }

    public static double Grade(string ratio) => 1 / double.Parse(ratio.AsSpan(2), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    public static bool NearStructure(WorldFeature feature, Vector2 point, double margin)
    {
        foreach (var geometry in StructuralGeometry(feature))
        {
            foreach (var polygon in WorldFeatureGeometry.PolygonsOf(geometry))
                if (TerrainGeometry.PolygonDistance(point.X, point.Y, polygon).Height <= margin) return true;
            if (geometry is PolylineGeometry line && TerrainGeometry.ProjectToLine(point.X, point.Y, line.Line).Distance <= margin) return true;
        }
        return false;
    }

    public static bool TouchesRegion(WorldFeature feature, TerrainRegion region, double margin)
    {
        foreach (var polygon in region.Polygons)
            foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                foreach (var p in ring) if (NearStructure(feature, p, margin)) return true;
        foreach (var geometry in StructuralGeometry(feature))
        {
            foreach (var polygon in WorldFeatureGeometry.PolygonsOf(geometry))
            {
                if (polygon.Outer.Any(p => region.Distance(p.X, p.Y).Height <= margin)) return true;
                if (new TerrainRegion([polygon]).InteriorOverlaps(region)) return true;
            }
            if (geometry is not PolylineGeometry line) continue;
            if (line.Line.Points.Any(p => region.Distance(p.X, p.Y).Height <= margin)) return true;
            var path = new TerrainPath(line.Line);
            foreach (var polygon in region.Polygons)
                foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                    if (path.Intersections(new(new(ring.Append(ring[0]).ToArray()))).Count > 0) return true;
        }
        return false;
    }

    private static IEnumerable<OverlayGeometry> StructuralGeometry(WorldFeature feature) => feature.Parts.Count > 0
        ? feature.Parts.Select(part => part.Geometry) : [feature.Geometry];
}
