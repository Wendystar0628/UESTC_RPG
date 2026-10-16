using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

public sealed record WorldLoadOptions
{
    public IInteriorIndex? Interiors { get; init; }
    public IOutskirtsModel? Outskirts { get; init; }
    public IFidelityModel? Fidelity { get; init; }
    public WalkBakeData? Walk { get; init; }
    /// <summary>Explicit registered feature claims, supplied by the map adapter; an overlay Owner is not a claim.</summary>
    public IReadOnlyDictionary<FeatureId, WpId>? ClaimedBy { get; init; }
}

public sealed class WorldDataException(string file, string entry, string message, Exception? inner = null)
    : Exception($"{file} [{entry}]: {message}", inner)
{
    public string File { get; } = file;
    public string Entry { get; } = entry;
}

/// <summary>Pure parser. The kernel supplies only the active map's relative filenames and bytes.</summary>
public static class WorldModelLoader
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    public static WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, WorldLoadOptions? options = null) =>
        Load(map, files.ToDictionary(p => p.Key, p => Decode(p.Key, p.Value), StringComparer.Ordinal), options);

    private static string Decode(string file, ReadOnlyMemory<byte> bytes)
    { try { return Utf8.GetString(bytes.Span).TrimStart('\uFEFF'); } catch (DecoderFallbackException error) { throw new WorldDataException(file, "encoding", "Invalid UTF-8.", error); } }

    public static WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, string> files, WorldLoadOptions? options = null)
    {
        try { return LoadCore(map, files, options); }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException)
        { throw new WorldDataException("campus.json", "root", error.Message, error); }
    }

    private static WorldModelLoadResult LoadCore(MapId map, IReadOnlyDictionary<string, string> files, WorldLoadOptions? options)
    {
        options ??= new();
        if (options.Walk is { } walk)
        {
            try { walk.Validate(); }
            catch (ArgumentException error) { throw new WorldDataException("walk.bake.json", "root", error.Message, error); }
            if (walk.Map != map) throw new WorldDataException("walk.bake.json", "map", "Walk data belongs to another map.");
        }
        var input = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in files.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var key = pair.Key.Replace('\\', '/');
            if (key.StartsWith('/') || key.Contains(':') || key.Split('/').Any(s => s is ".." or "." or ""))
                throw new WorldDataException(pair.Key, "path", "Expected a relative path within the current map.");
            if (!input.TryAdd(key, pair.Value)) throw new WorldDataException(key, "path", "Duplicate normalized path.");
        }
        if (!input.TryGetValue("campus.json", out var campusText)) throw new WorldDataException("campus.json", "root", "Required map data is missing.");
        using var document = Parse("campus.json", campusText);
        var root = document.RootElement;
        var meta = root.GetProperty("meta");
        var origin = meta.GetProperty("origin");
        var limits = meta.GetProperty("bounds");
        var geo = new GeoFrame(origin[0].GetDouble(), origin[1].GetDouble(),
            int.Parse(Text(meta, "crs").Split(':')[^1], CultureInfo.InvariantCulture),
            new(Finite(limits[0]), Finite(limits[1])), new(Finite(limits[2]), Finite(limits[3])));
        if (geo.BoundaryMin.X >= geo.BoundaryMax.X || geo.BoundaryMin.Y >= geo.BoundaryMax.Y)
            throw new WorldDataException("campus.json", "meta.bounds", "Invalid bounds.");
        var boundary = Rings(root.GetProperty("boundary"));
        var entities = root.GetProperty("entities").EnumerateArray()
            .ToDictionary(e => Text(e, "id"), e => e, StringComparer.Ordinal);
        var features = new SortedDictionary<string, WorldFeature>(StringComparer.Ordinal);
        var aliases = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var prefix = OptionalText(meta, "entityPrefix");
        var sourceIds = new HashSet<string>(StringComparer.Ordinal);
        var groups = new SortedDictionary<string, List<(JsonElement Raw, WorldFeaturePart Part)>>(StringComparer.Ordinal);
        foreach (var raw in root.GetProperty("features").EnumerateArray())
        {
            var sourceId = Text(raw, "id");
            if (!sourceIds.Add(sourceId)) throw new WorldDataException("campus.json", sourceId, "Duplicate source geometry.");
            var entityId = OptionalText(raw, "entityId");
            var id = entityId ?? (prefix is null ? throw new WorldDataException("campus.json", "meta.entityPrefix", "Anonymous geometry needs a map entity prefix.") : prefix + ":source" + sourceId[sourceId.IndexOf(':')..]);
            if (entityId is null && entities.ContainsKey(id)) throw new WorldDataException("campus.json", sourceId, "Anonymous feature ID collides with an authored entity.");
            var polygons = raw.GetProperty("polygons").EnumerateArray().Select(Rings).ToArray();
            if (polygons.Length == 0) throw new WorldDataException("campus.json", sourceId, "Missing footprint.");
            var height = OptionalFloat(raw, "height");
            var storeys = OptionalFloat(raw, "levels");
            var part = new WorldFeaturePart(sourceId, Geometry(polygons), height,
                storeys is > 0 ? (int)MathF.Ceiling(storeys.Value) : null)
            { FacadeGeometry = ReadFacadeGeometry(raw, sourceId, polygons), Props = raw.Clone() };
            if (!groups.TryGetValue(id, out var group)) groups[id] = group = [];
            group.Add((raw, part));
            Alias(aliases, sourceId, id);
            Alias(aliases, id, id);
        }
        foreach (var pair in groups)
        {
            var parts = pair.Value.OrderBy(p => p.Part.SourceId, StringComparer.Ordinal).Select(p => p.Part).ToArray();
            var raw = pair.Value[0].Raw;
            entities.TryGetValue(pair.Key, out var entity);
            var geometry = Geometry(parts.SelectMany(p => Polygons(p.Geometry)).ToArray());
            var height = parts.Max(p => p.Height);
            var kind = entity.ValueKind == JsonValueKind.Object ? Text(entity, "kind") : Text(raw, "kind");
            features.Add(pair.Key, new(new(pair.Key), FeatureSource.Osm, new(kind), geometry)
            {
                Name = entity.ValueKind == JsonValueKind.Object ? OptionalText(entity, "name") : null,
                Height = height,
                Storeys = parts.Max(p => p.Storeys),
                Family = OptionalText(raw, "family") is { } family ? new(family) : null,
                Bounds = BoundsOf(geometry, height ?? 0),
                Parts = parts,
                Props = entity.ValueKind == JsonValueKind.Object ? entity.Clone() : raw.Clone()
            });
        }
        foreach (var pair in entities.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (features.ContainsKey(pair.Key)) continue;
            var anchor = pair.Value.GetProperty("anchor");
            var point = new Vector3(Finite(anchor[0]), Finite(anchor[1]), Finite(anchor[2]));
            var geometry = new PointGeometry(point);
            features.Add(pair.Key, new(new(pair.Key), FeatureSource.Osm, new(Text(pair.Value, "kind")), geometry)
            { Name = OptionalText(pair.Value, "name"), Bounds = new(point, point), Props = pair.Value.Clone() });
            Alias(aliases, pair.Key, pair.Key);
        }
        ApplyZones(input, features, aliases);
        var originalNetwork = input.TryGetValue("network.json", out var networkText) ? Deserialize<WorldNetwork>("network.json", networkText) : WorldNetwork.Empty;
        ValidateNetwork(originalNetwork);
        var roads = new EffectiveRoadNetwork(originalNetwork, prefix, features);
        var overlayWrites = new Dictionary<string, List<OverlayWrite>>(StringComparer.Ordinal);
        var interiorOverlays = new List<InteriorOverlayFile>();
        var splitGroups = input.Keys.Select(FeatureOverlayShardGroup).OfType<string>().ToHashSet(StringComparer.Ordinal);
        foreach (var pair in input.Where(p => p.Key.StartsWith("overlay/", StringComparison.Ordinal))
            .OrderBy(p => FeatureOverlayShardGroup(p.Key) ?? p.Key, StringComparer.Ordinal)
            .ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            if (pair.Key.StartsWith("overlay/interior/", StringComparison.Ordinal) && pair.Key.EndsWith(".interior.json", StringComparison.Ordinal))
            {
                var overlay = Deserialize<InteriorOverlayFile>(pair.Key, pair.Value);
                CheckMap(map, overlay.Map, pair.Key);
                foreach (var op in overlay.Ops)
                {
                    var id = op switch { SealOp seal => seal.Building, InteriorConfigOp config => config.Building, _ => throw new WorldDataException(pair.Key, "op", "Unknown interior operation.") };
                    Resolve(features, aliases, id, pair.Key);
                }
                interiorOverlays.Add(overlay);
            }
            else if (FeatureOverlayShardGroup(pair.Key) is not null || IsLegacyFeatureOverlay(pair.Key))
            {
                var overlay = Deserialize<OverlayFile>(pair.Key, pair.Value);
                CheckMap(map, overlay.Map, pair.Key);
                if (splitGroups.Contains(pair.Key) && overlay.Ops.Count != 0)
                    throw new WorldDataException(pair.Key, "ops", $"A non-empty legacy overlay cannot coexist with split overlay directory '{pair.Key[..^".overlay.json".Length]}.d/'.");
                ApplyOverlay(pair.Key, overlay, features, aliases, overlayWrites, options.ClaimedBy, roads);
            }
        }
        var network = roads.Publish(features);
        var frozen = new ReadOnlyDictionary<FeatureId, WorldFeature>(features.Values.ToDictionary(f => f.Id));
        var interiors = options.Interiors ?? new WorldInteriorIndex(frozen.Values, input.GetValueOrDefault("interiors.json"));
        // The outskirts producer owns its document format; do not silently discard a supplied layer.
        if (input.ContainsKey("outskirts.json") && options.Outskirts is null)
            throw new WorldDataException("outskirts.json", "root", "A parsed outskirts model is required until the producer's file contract is available.");
        var outskirts = options.Outskirts;
        var world = new WorldModelSnapshot(frozen, boundary, network, interiors, outskirts, options.Fidelity, interiorOverlays.AsReadOnly(), options.Walk);
        var limitRing = outskirts?.CutLine is { Count: >= 3 } cutLine ? cutLine : boundary.Outer;
        var roam = ReadRoam(map, input);
        var margin = outskirts is null ? roam?.FallbackMargin ?? 0 : 0;
        var bounds = new Box3(new(limitRing.Min(p => p.X) - margin, roam?.FloorY ?? 0, limitRing.Min(p => p.Y) - margin),
            new(limitRing.Max(p => p.X) + margin, roam?.CeilY ?? Math.Max(1, features.Count > 0 ? features.Values.Max(f => f.Bounds.Max.Y) : 0), limitRing.Max(p => p.Y) + margin));
        return new(world, geo, PlanGeometry.Centroid(boundary.Outer), bounds,
            new((int)MathF.Floor(bounds.Min.X / 64), (int)MathF.Floor(bounds.Min.Z / 64)),
            new((int)MathF.Floor(bounds.Max.X / 64), (int)MathF.Floor(bounds.Max.Z / 64)))
        { Diagnostics = ["Terrain input is not attached: GroundY currently falls back to the source relative plane. Designed slopes, platforms and sunken areas remain incomplete."] };
    }

    private static void ApplyZones(SortedDictionary<string, string> input, SortedDictionary<string, WorldFeature> features, Dictionary<string, List<string>> aliases)
    {
        if (!input.TryGetValue("zones.json", out var text)) return;
        using var document = Parse("zones.json", text);
        if (!document.RootElement.TryGetProperty("zones", out var zones)) return;
        foreach (var zone in zones.EnumerateArray())
        {
            var id = new ZoneId(Text(zone, "id"));
            if (!zone.TryGetProperty("features", out var members)) continue;
            foreach (var member in members.EnumerateArray())
                foreach (var key in Resolve(features, aliases, new(member.GetString()!), "zones.json")) features[key] = features[key] with { Zone = id };
        }
    }

    private sealed record OverlayWrite(string File, WpId Owner, string Target);

    // Only root-level feature groups opt into splitting. Dedicated overlay formats keep their own loaders.
    private static bool IsDedicatedOverlayGroup(string group) =>
        group is "interior" or "terrain" or "outskirts" or "walkable" or "fidelity";

    private static string? FeatureOverlayShardGroup(string file)
    {
        if (!file.StartsWith("overlay/", StringComparison.Ordinal)) return null;
        var relative = file["overlay/".Length..];
        var separator = relative.IndexOf('/');
        if (separator < 3 || !relative[..separator].EndsWith(".d", StringComparison.Ordinal)) return null;
        var group = relative[..(separator - 2)];
        var child = relative[(separator + 1)..];
        return !IsDedicatedOverlayGroup(group) && child.EndsWith(".json", StringComparison.Ordinal) && !child.Contains('/')
            ? "overlay/" + group + ".overlay.json" : null;
    }

    private static bool IsLegacyFeatureOverlay(string file)
    {
        if (!file.EndsWith(".overlay.json", StringComparison.Ordinal)) return false;
        var segments = file["overlay/".Length..].Split('/');
        if (segments.Take(segments.Length - 1).Any(segment => segment.EndsWith(".d", StringComparison.Ordinal))) return false;
        var group = segments[0];
        if (segments.Length == 1) group = group[..^".overlay.json".Length];
        return !IsDedicatedOverlayGroup(group);
    }

    private static void RegisterWrite(string entity, string target, string file, WpId owner,
        Dictionary<string, List<OverlayWrite>> writes, IReadOnlyDictionary<FeatureId, WpId>? claimedBy)
    {
        if (!writes.TryGetValue(entity, out var previous)) writes[entity] = previous = [];
        var splitGroup = FeatureOverlayShardGroup(file);
        foreach (var write in previous)
        {
            if (write.File == file) continue;
            // Same-owner shards retain the legacy logical file's sequential writes, without losing real error paths.
            if (splitGroup is not null && splitGroup == FeatureOverlayShardGroup(write.File) && owner == write.Owner) continue;
            var explicitlyClaimed = claimedBy is not null && claimedBy.TryGetValue(new(entity), out var claimant)
                && (claimant == owner || claimant == write.Owner);
            if (!explicitlyClaimed)
                throw new WorldDataException(file, target,
                    $"Overlay conflict for resolved entity '{entity}': '{write.File}' (owner '{write.Owner.Value}', target '{write.Target}') and '{file}' (owner '{owner.Value}', target '{target}'); neither overlay has an explicit registered claim.");
        }
        if (!previous.Any(write => write.File == file)) previous.Add(new(file, owner, target));
    }

    private static RoamLimits? ReadRoam(MapId map, SortedDictionary<string, string> input)
    {
        if (!input.TryGetValue("map.json", out var text)) return null;
        using var document = Parse("map.json", text);
        var root = document.RootElement;
        if (OptionalText(root, "id") is { } id && id != map.Value) throw new WorldDataException("map.json", "id", "Map descriptor belongs to another map.");
        if (!root.TryGetProperty("roam", out var element)) throw new WorldDataException("map.json", "roam", "Required roam limits are missing.");
        var roam = Deserialize<RoamLimits>("map.json", element.GetRawText());
        if (!float.IsFinite(roam.FallbackMargin) || roam.FallbackMargin < 0 || !float.IsFinite(roam.FloorY)
            || !float.IsFinite(roam.CeilY) || roam.FloorY >= roam.CeilY)
            throw new WorldDataException("map.json", "roam", "Invalid fallback margin or vertical limits.");
        return roam;
    }

    private static void ApplyOverlay(string file, OverlayFile overlay, SortedDictionary<string, WorldFeature> features,
        Dictionary<string, List<string>> aliases, Dictionary<string, List<OverlayWrite>> writes, IReadOnlyDictionary<FeatureId, WpId>? claimedBy,
        EffectiveRoadNetwork roads)
    {
        for (var i = 0; i < overlay.Ops.Count; i++)
        {
            try
            {
                var op = overlay.Ops[i];
                if (op is HideOp roadHide && roads.IsOriginalRoad(roadHide.Target.Value))
                {
                    RequireReason(roadHide.Reason);
                    var removal = roads.PlanRoadHide(roadHide.Target.Value, features, file);
                    RegisterWrite(removal.Road, roadHide.Target.Value, file, overlay.Owner, writes, claimedBy);
                    if (removal.Entity is { } entity && entity != removal.Road)
                        RegisterWrite(entity, roadHide.Target.Value, file, overlay.Owner, writes, claimedBy);
                    roads.ApplyHide(removal, features, file, roadHide.Target.Value);
                    continue;
                }
                if (op is HidePartOp hidePart)
                {
                    RequireReason(hidePart.Reason);
                    // Unlike legacy hide, this operation accepts only the exact current entity key, never a source alias.
                    var entityId = hidePart.Target.Value;
                    if (string.IsNullOrWhiteSpace(entityId) || !features.TryGetValue(entityId, out var current))
                        throw new ArgumentException($"hidePart target must be an exact existing entity ID: '{entityId}'.");
                    if (string.IsNullOrWhiteSpace(hidePart.SourceId) || hidePart.SourceId != hidePart.SourceId.Trim())
                        throw new ArgumentException("hidePart sourceId must be the exact canonical source ID of one current part.");
                    var matches = current.Parts.Count(part => string.Equals(part.SourceId, hidePart.SourceId, StringComparison.Ordinal));
                    if (matches != 1)
                        throw new ArgumentException($"hidePart sourceId '{hidePart.SourceId}' must match exactly one part of '{entityId}'; found {matches}.");
                    roads.RequireNonRoadPartRemoval(entityId, hidePart.SourceId, file);
                    if (current.Parts.Count == 1)
                        throw new ArgumentException("hidePart cannot remove the final part; use hide to remove the entity explicitly.");
                    var remaining = current.Parts.Where(part => !string.Equals(part.SourceId, hidePart.SourceId, StringComparison.Ordinal)).ToArray();
                    // Match LoadCore's aggregation, preserving the remaining part records, polygon rings and holes.
                    var geometry = Geometry(remaining.SelectMany(part => Polygons(part.Geometry)).ToArray());
                    var height = remaining.Max(part => part.Height);
                    var bounds = BoundsOf(geometry, height ?? 0);
                    RegisterWrite(entityId, entityId, file, overlay.Owner, writes, claimedBy);
                    features[entityId] = current with
                    {
                        Parts = Array.AsReadOnly(remaining),
                        Geometry = geometry,
                        Height = height,
                        Storeys = remaining.Max(part => part.Storeys),
                        Bounds = bounds,
                        Source = FeatureSource.OsmOverlay
                    };
                    if (aliases.TryGetValue(hidePart.SourceId, out var sourceTargets))
                    {
                        sourceTargets.RemoveAll(mappedEntity => string.Equals(mappedEntity, entityId, StringComparison.Ordinal));
                        if (sourceTargets.Count == 0) aliases.Remove(hidePart.SourceId);
                    }
                    continue;
                }
                if (op is AddOp add)
                {
                    RequireReason(add.Reason);
                    var designId = add.Id.Value.StartsWith("design:", StringComparison.Ordinal);
                    var walkBeat = add.Id.Value.StartsWith("walk:", StringComparison.Ordinal) && add.Id.Value.Length > 5
                        && add.Kind.Value == "beat" && add.Height is null
                        && add.Geometry is PointGeometry or PolygonGeometry or MultiPolygonGeometry;
                    if (!designId && !walkBeat)
                        throw new ArgumentException("Added features require a design: identifier, or a walk: beat anchor with point/area geometry and no height.");
                    RegisterWrite(add.Id.Value, add.Id.Value, file, overlay.Owner, writes, claimedBy);
                    var geometry = Normalize(add.Geometry);
                    var feature = new WorldFeature(add.Id, FeatureSource.Design, add.Kind, geometry)
                    { Height = add.Height, Zone = overlay.Zone, Props = add.Props?.Clone(), Bounds = BoundsOf(geometry, add.Height ?? 0) };
                    if (!features.TryAdd(add.Id.Value, feature)) throw new ArgumentException("Added ID already exists.");
                    Alias(aliases, add.Id.Value, add.Id.Value);
                    roads.RecordAdd(add.Id);
                    continue;
                }
                var target = op switch
                {
                    ReshapeOp reshape => reshape.Target,
                    HeightOp height => height.Target,
                    StyleOp style => style.Target,
                    HideOp hide => hide.Target,
                    _ => throw new ArgumentException("Unknown overlay operation.")
                };
                // Keep aliases after a hide so a later file reports the two conflicting writers, not only a missing target.
                var keys = aliases.TryGetValue(target.Value, out var resolved) ? resolved : Resolve(features, aliases, target, file);
                if (op is ReshapeOp && keys.Count != 1) throw new ArgumentException("Reshape must target one source feature, not a multi-wing entity.");
                foreach (var key in keys)
                {
                    RegisterWrite(key, target.Value, file, overlay.Owner, writes, claimedBy);
                    if (!features.TryGetValue(key, out var current)) throw new WorldDataException(file, target.Value, "Target was already hidden.");
                    switch (op)
                    {
                        case HideOp hide:
                            RequireReason(hide.Reason);
                            if (roads.PlanFeatureHide(key, features, file) is { } removal)
                            {
                                if (removal.Road != key) RegisterWrite(removal.Road, target.Value, file, overlay.Owner, writes, claimedBy);
                                roads.ApplyHide(removal, features, file, target.Value);
                            }
                            else features.Remove(key);
                            break;
                        case HeightOp height:
                            RequireReason(height.Reason);
                            if (height.Height is <= 0 || height.Storeys is <= 0) throw new ArgumentException("Height and storeys must be positive.");
                            var newHeight = height.Height ?? (height.Storeys is { } count ? count * (current.Height / current.Storeys ?? 3.6f) : current.Height);
                            features[key] = current with { Height = newHeight, Storeys = height.Storeys ?? current.Storeys, Parts = current.Parts.Select(p => p with { Height = height.Height ?? (height.Storeys is { } floors ? floors * (p.Height / p.Storeys ?? 3.6f) : p.Height), Storeys = height.Storeys ?? p.Storeys }).ToArray(), Source = FeatureSource.OsmOverlay, Bounds = BoundsOf(current.Geometry, newHeight ?? 0) };
                            break;
                        case StyleOp style:
                            features[key] = current with { Family = style.Family, Facade = style.Facade, Source = FeatureSource.OsmOverlay }; break;
                        case ReshapeOp reshape:
                            RequireReason(reshape.Reason);
                            var geometry = Normalize(new PolygonGeometry(reshape.Rings));
                            features[key] = current with { Geometry = geometry, Parts = [], Source = FeatureSource.OsmOverlay, Bounds = BoundsOf(geometry, current.Height ?? 0) }; break;
                        default: throw new ArgumentException("Unknown overlay operation.");
                    }
                }
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException)
            { throw new WorldDataException(file, $"ops[{i}]", error.Message, error); }
        }
    }

    private static void RequireReason(string reason) { if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("An authored change requires a reason."); }
    private static void CheckMap(MapId map, MapId? declared, string file)
    { if (declared is { } id && id != map) throw new WorldDataException(file, "map", "Overlay belongs to a different map."); }
    private static void Alias(Dictionary<string, List<string>> aliases, string key, string value)
    { if (!aliases.TryGetValue(key, out var list)) aliases[key] = list = []; if (!list.Contains(value, StringComparer.Ordinal)) list.Add(value); }
    private static IReadOnlyList<string> Resolve(SortedDictionary<string, WorldFeature> features, Dictionary<string, List<string>> aliases, FeatureId id, string file)
    {
        if (features.ContainsKey(id.Value)) return [id.Value];
        if (aliases.TryGetValue(id.Value, out var values))
        {
            var active = values.Where(features.ContainsKey).ToArray();
            if (active.Length > 0) return active;
        }
        throw new WorldDataException(file, id.Value, "Target feature does not exist (or was hidden).");
    }
    private static OverlayGeometry? ReadFacadeGeometry(JsonElement raw, string sourceId, IReadOnlyList<PolygonRings> physical)
    {
        if (!raw.TryGetProperty("facadePolygons", out var value)) return null;
        try
        {
            var polygons = value.EnumerateArray().Select(Rings).ToArray();
            if (polygons.Length == 0) throw new ArgumentException("Empty facade footprint.");
            if (polygons.Length != physical.Count || polygons.Where((polygon, i) => polygon.Holes.Count != physical[i].Holes.Count).Any())
                throw new ArgumentException("Facade polygons and holes must correspond to the physical footprint.");
            return Geometry(polygons);
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or IndexOutOfRangeException)
        { throw new WorldDataException("campus.json", sourceId + ".facadePolygons", error.Message, error); }
    }

    internal static OverlayGeometry Geometry(IReadOnlyList<PolygonRings> polygons) => polygons.Count == 1 ? new PolygonGeometry(polygons[0]) : new MultiPolygonGeometry(polygons);
    internal static OverlayGeometry Normalize(OverlayGeometry geometry) => geometry switch
    {
        PolygonGeometry polygon => new PolygonGeometry(Normalize(polygon.Rings)),
        MultiPolygonGeometry multi => Geometry(multi.Polygons.Select(Normalize).ToArray()),
        PolylineGeometry line when line.Line.Points.Count >= 2 => line,
        PointGeometry point when float.IsFinite(point.Position.X) && float.IsFinite(point.Position.Y) && float.IsFinite(point.Position.Z) => point,
        _ => throw new ArgumentException("Invalid geometry.")
    };
    private static PolygonRings Normalize(PolygonRings rings) => new(PlanGeometry.OpenRing(rings.Outer), rings.Holes.Select(h => PlanGeometry.OpenRing(h, false)).ToArray());
    internal static IEnumerable<PolygonRings> Polygons(OverlayGeometry geometry) => geometry switch
    { PolygonGeometry polygon => [polygon.Rings], MultiPolygonGeometry multi => multi.Polygons, PolylineGeometry or PointGeometry => [], _ => throw new ArgumentException("Unknown geometry.") };
    internal static Box3 BoundsOf(OverlayGeometry geometry, float height)
    {
        if (!float.IsFinite(height) || height < 0) throw new ArgumentException("Invalid feature height.");
        if (geometry is PointGeometry point) return new(point.Position, point.Position);
        var points = geometry is PolylineGeometry line ? line.Line.Points : Polygons(geometry).SelectMany(p => p.Outer).ToArray();
        if (points.Count == 0) throw new ArgumentException("Empty feature geometry.");
        return new(new(points.Min(p => p.X), 0, points.Min(p => p.Y)), new(points.Max(p => p.X), height, points.Max(p => p.Y)));
    }
    internal static PolygonRings Rings(JsonElement array)
    {
        var rings = array.EnumerateArray().Select((ring, index) => PlanGeometry.OpenRing(ring.EnumerateArray().Select(Vector), index == 0)).ToArray();
        if (rings.Length == 0) throw new ArgumentException("Empty polygon.");
        return new(rings[0], rings.Skip(1).ToArray());
    }
    internal static Vector2 Vector(JsonElement e) => new(Finite(e[0]), Finite(e[1]));
    internal static float Finite(JsonElement e) { var n = e.GetSingle(); return float.IsFinite(n) ? n : throw new JsonException("Non-finite number."); }
    internal static string Text(JsonElement e, string key) => e.GetProperty(key).GetString() ?? throw new JsonException($"Missing {key}.");
    internal static string? OptionalText(JsonElement e, string key) => e.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;
    internal static float? OptionalFloat(JsonElement e, string key) => e.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? Finite(value) : null;
    internal static JsonDocument Parse(string file, string text)
    { try { return JsonDocument.Parse(text); } catch (JsonException error) { throw new WorldDataException(file, error.Path ?? "root", error.Message, error); } }
    internal static T Deserialize<T>(string file, string text)
    { try { return JsonSerializer.Deserialize<T>(text, ContractJson.Options) ?? throw new JsonException("Null document."); } catch (JsonException error) { throw new WorldDataException(file, error.Path ?? "root", error.Message, error); } }
    private static void ValidateNetwork(WorldNetwork network)
    {
        foreach (var line in network.Roads.Concat(network.Waterways).Concat(network.Bridges).Concat(network.Culverts))
            if (line.Line.Points.Count < 2 || line.Width < 0 || !float.IsFinite(line.Width) || line.Line.Points.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y)))
                throw new WorldDataException("network.json", line.Id.Value, "Invalid centerline or width.");
        foreach (var tree in network.Trees)
            if (!float.IsFinite(tree.Position.X) || !float.IsFinite(tree.Position.Y) || !float.IsFinite(tree.Position.Z)) throw new WorldDataException("network.json", tree.Id.Value, "Invalid tree coordinate.");
    }
}
