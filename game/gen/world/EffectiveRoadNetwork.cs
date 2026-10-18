using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>One load's original road provenance and private overlay edits, published only after all operations succeed.</summary>
internal sealed class EffectiveRoadNetwork
{
    private sealed record Source(string Id, string Entity, string Kind);
    internal sealed record Removal(string Road, string? Entity);
    private readonly WorldNetwork _original;
    private readonly Dictionary<string, NetworkLine> _roads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Source> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Source?> _roadSources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _entityRoads;
    private readonly IReadOnlyDictionary<string, WorldFeature> _originalFeatures;
    private readonly HashSet<string> _removed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _designAdds = new(StringComparer.Ordinal);
    private readonly string? _prefix;

    public EffectiveRoadNetwork(WorldNetwork network, string? prefix, IReadOnlyDictionary<string, WorldFeature> features)
    {
        _original = network;
        _prefix = prefix is { Length: > 0 } && prefix == prefix.Trim() && !prefix.Contains(':') ? prefix : null;
        _originalFeatures = new Dictionary<string, WorldFeature>(features, StringComparer.Ordinal);
        foreach (var feature in features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
            foreach (var part in feature.Parts)
                _sources.Add(part.SourceId, new(part.SourceId, feature.Id.Value, PartKind(part, feature)));
        foreach (var road in network.Roads)
        {
            if (string.IsNullOrWhiteSpace(road.Id.Value) || !_roads.TryAdd(road.Id.Value, road))
                throw new WorldDataException("network.json", road.Id.Value, "Empty or duplicate road ID.");
            var source = SourceId(road.Id.Value);
            _roadSources.Add(road.Id.Value, source is not null ? _sources.GetValueOrDefault(source) : null);
        }
        _entityRoads = _roadSources.Where(pair => pair.Value is not null).GroupBy(pair => pair.Value!.Entity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Key).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
    }

    public bool IsOriginalRoad(string id) => _roads.ContainsKey(id);
    public void RecordAdd(FeatureId id) => _designAdds.Add(id.Value);

    public void RequireNonRoadPartRemoval(string entity, string source, string file)
    {
        var roads = _roadSources.Where(pair => pair.Value?.Id == source).Select(pair => pair.Key).Order(StringComparer.Ordinal).ToArray();
        if (roads.Length != 0)
            throw new WorldDataException(file, entity, $"hidePart cannot remove an original network road source in this policy: " +
                $"network=[{string.Join(",", roads)}], source='{source}', entity='{entity}'.");
    }

    // pipeline/network.py exports ways as <meta.entityPrefix>:source:way:<OSM integer>.
    // Source lookup uses the exact osm:way:<integer> record actually present in campus.json.
    // A failed export-key parse never proves that a road has no campus source.
    private string? SourceId(string networkId)
    {
        return _prefix is not null && OsmFeatureIds.TryWayFromSource(_prefix, networkId, out var ids) ? ids.Part : null;
    }

    public Removal PlanRoadHide(string id, IReadOnlyDictionary<string, WorldFeature> features, string file)
    {
        var road = _roads[id];
        var sourceId = SourceId(id);
        var binding = _roadSources[id];
        string Identity() => $"network='{id}', source='{sourceId ?? "(unverified export identity)"}', entity='{binding?.Entity ?? "(none)"}'";
        if (sourceId is null)
            throw new WorldDataException(file, id, $"Road hide cannot verify meta.entityPrefix and the exported source identity: {Identity()}.");
        var categories = new List<string>();
        if (_original.Waterways.Any(line => line.Id.Value == id)) categories.Add("waterways");
        if (_original.Bridges.Any(line => line.Id.Value == id)) categories.Add("bridges");
        if (_original.Culverts.Any(line => line.Id.Value == id)) categories.Add("culverts");
        if (categories.Count != 0)
            throw new WorldDataException(file, id, $"Ambiguous road hide across roads/{string.Join('/', categories)}: {Identity()}.");
        if (Area(road.Props))
            throw new WorldDataException(file, id, $"Ambiguous area=yes road hide requires an explicit area policy: {Identity()}.");
        if (binding is null)
        {
            if (_originalFeatures.ContainsKey(id) || features.ContainsKey(id))
                throw new WorldDataException(file, id, $"Road ID collides with a feature without a matching campus source: {Identity()}.");
            return new(id, null);
        }
        var original = _originalFeatures[binding.Entity];
        if (binding.Entity != id || binding.Kind != "road" || original.Kind.Value != "road" || original.Parts.Count != 1 ||
            original.Parts[0].SourceId != sourceId || _entityRoads[binding.Entity].Length != 1)
            throw new WorldDataException(file, id, $"Ambiguous named, mixed or multi-source road hide: {Identity()}; " +
                $"entityKind='{original.Kind.Value}', sourceKind='{binding.Kind}', sources=[{string.Join(",", original.Parts.Select(part => part.SourceId))}].");
        if (features.TryGetValue(binding.Entity, out var current) && (current.Kind.Value != "road" || current.Parts.Count != 1 || current.Parts[0].SourceId != sourceId))
            throw new WorldDataException(file, id, $"Road source membership changed before hide: {Identity()}.");
        return new(id, binding.Entity);
    }

    public Removal? PlanFeatureHide(string entity, IReadOnlyDictionary<string, WorldFeature> features, string file)
    {
        if (_roads.ContainsKey(entity)) return PlanRoadHide(entity, features, file);
        if (!_entityRoads.TryGetValue(entity, out var roads)) return null;
        // Multiple road sources in an entity are rejected by the same provenance check as an exact network entry.
        return PlanRoadHide(roads[0], features, file);
    }

    public void ApplyHide(Removal removal, SortedDictionary<string, WorldFeature> features, string file, string target)
    {
        // All identity and conflict checks run first. Neither stack is published during this private mutation.
        if (_removed.Contains(removal.Road) || removal.Entity is { } entity && !features.ContainsKey(entity))
            throw new WorldDataException(file, target, $"Road target was already hidden: network='{removal.Road}', entity='{removal.Entity ?? "(none)"}'.");
        if (removal.Entity is { } key) features.Remove(key);
        _removed.Add(removal.Road);
    }

    public WorldNetwork Publish(IReadOnlyDictionary<string, WorldFeature> features)
    {
        var roads = _original.Roads.Where(road => !_removed.Contains(road.Id.Value)).ToList();
        var ids = roads.Select(road => road.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var feature in features.Values.Where(feature => (feature.Source == FeatureSource.Design || _designAdds.Contains(feature.Id.Value)) && feature.Kind.Value == "road")
            .OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PolylineGeometry line) continue;
            var roadType = Classification(feature.Props, feature.Id.Value);
            if (roadType is not ("footway" or "path" or "pedestrian")) continue;
            if (Area(feature.Props)) continue; // A retained area boundary is not a new centerline.
            if (feature.Props is not { ValueKind: JsonValueKind.Object } props || !props.TryGetProperty("width", out var widthValue) ||
                widthValue.ValueKind != JsonValueKind.Number || !widthValue.TryGetSingle(out var width) || !float.IsFinite(width) || width <= 0)
                throw new WorldDataException("overlay", feature.Id.Value, "Design road width must be a finite positive number.");
            if (line.Line.Points.Count < 2 || line.Line.Points.Any(point => !float.IsFinite(point.X) || !float.IsFinite(point.Y)) ||
                line.Line.Points.All(point => point == line.Line.Points[0]))
                throw new WorldDataException("overlay", feature.Id.Value, "Design road points must be finite and have non-zero line length.");
            if (!ids.Add(feature.Id.Value))
                throw new WorldDataException("overlay", feature.Id.Value, "Design road ID duplicates a final network road.");
            roads.Add(new(feature.Id, line.Line, width) { Props = feature.Props });
        }
        return new(roads.AsReadOnly(), _original.Waterways, _original.Bridges, _original.Culverts, _original.Trees);
    }

    private static string PartKind(WorldFeaturePart part, WorldFeature parent) =>
        part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.String
            ? kind.GetString()! : parent.Kind.Value;

    private static bool Area(JsonElement? props) => props is { ValueKind: JsonValueKind.Object } value &&
        value.TryGetProperty("area", out var area) && area.ValueKind == JsonValueKind.String && area.GetString() == "yes";

    private static string? Classification(JsonElement? props, string id)
    {
        if (props is null) return null;
        if (props.Value.ValueKind != JsonValueKind.Object) throw new WorldDataException("overlay", id, "Design road props must be an object.");
        string? Read(string key)
        {
            if (!props.Value.TryGetProperty(key, out var value)) return null;
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()) || value.GetString() != value.GetString()!.Trim())
                throw new WorldDataException("overlay", id, $"Design road {key} must be a nonempty classification string.");
            return value.GetString();
        }
        var roadType = Read("roadType");
        var highway = Read("highway");
        if (roadType is not null && highway is not null && roadType != highway)
            throw new WorldDataException("overlay", id, $"Conflicting roadType='{roadType}' and highway='{highway}'.");
        return roadType ?? highway;
    }
}
