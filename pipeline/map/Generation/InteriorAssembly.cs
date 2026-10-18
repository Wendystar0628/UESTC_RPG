using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Pipeline.Map;

public sealed record InteriorProviderBinding(
    string ProviderId,
    IReadOnlyList<FeatureId> FeatureIds,
    IReadOnlyList<int> OpenFloors,
    int AuthoredEntranceCount);

public sealed record InteriorAssemblyResult(
    ContentOutput Content,
    IReadOnlyList<Collider> Colliders,
    IReadOnlyList<InteriorIndexEntry> Entries,
    IReadOnlyList<NavNode> NavNodes,
    IReadOnlyList<NavEdge> NavEdges,
    IReadOnlyList<NavPortal> NavPortals);

/// <summary>Pure CPU map interior assembly. The host reads bytes and calls this on a worker before mounting.</summary>
public static class InteriorAssembly
{
    /// <summary>
    /// Pure CPU configuration only: parse the authoritative catalog and configure fresh map-owned fallback providers.
    /// Call once before publishing the map's worker context, before any provider Layout/Build/Collision/Nav call.
    /// The returned bindings, not every globally registered provider's mutable FeatureIds, define this map's coverage.
    /// </summary>
    public static IReadOnlyList<InteriorProviderBinding> Configure(ReadOnlyMemory<byte> interiorsBytes,
        IReadOnlyDictionary<string, IInteriorProvider> providers) => ConfigureCore(interiorsBytes, providers, null);

    /// <summary>Resolve raw pad-relative entrances once against the explicitly supplied map terrain.</summary>
    public static IReadOnlyList<InteriorProviderBinding> Configure(ReadOnlyMemory<byte> interiorsBytes,
        IReadOnlyDictionary<string, IInteriorProvider> providers, ITerrain terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        return ConfigureCore(interiorsBytes, providers, terrain);
    }

    /// <summary>Configure and validate the complete catalog against the parsed map before publishing workers.</summary>
    public static IReadOnlyList<InteriorProviderBinding> ConfigureForWorld(ReadOnlyMemory<byte> interiorsBytes,
        IReadOnlyDictionary<string, IInteriorProvider> providers, IWorldModel world)
    {
        var bindings = Configure(interiorsBytes, providers, world.Terrain);
        foreach (var binding in bindings)
        {
            var actual = providers[binding.ProviderId].FeatureIds;
            if (actual.Count != binding.FeatureIds.Count || actual.Distinct().Count() != actual.Count || !actual.ToHashSet().SetEquals(binding.FeatureIds))
                throw new InvalidDataException($"Interior provider {binding.ProviderId} is not bound to its catalog: expected {binding.FeatureIds.Count} features, actual {actual.Count}.");
            foreach (var feature in binding.FeatureIds)
                if (!world.Features.ContainsKey(feature)) throw new InvalidDataException($"Interior catalog feature is absent from the loaded world: {feature.Value}");
        }
        return bindings;
    }

    public static InteriorLayout ValidatedLayout(IInteriorProvider provider, InteriorProviderBinding binding, BuildContext context, FeatureId feature)
    {
        var layout = provider.Layout(context, feature);
        if (layout.Feature != feature || layout.OpenStoreys.Count == 0 || layout.OpenStoreys.Distinct().Count() != layout.OpenStoreys.Count ||
            !layout.OpenStoreys.ToHashSet().SetEquals(binding.OpenFloors) ||
            layout.OpenStoreys.Any(floor => !layout.Storeys.Any(storey => storey.Index == floor)))
            throw new InvalidDataException($"Interior layout coverage differs from its catalog: {binding.ProviderId}/{feature.Value}.");
        return layout;
    }

    private static IReadOnlyList<InteriorProviderBinding> ConfigureCore(ReadOnlyMemory<byte> interiorsBytes,
        IReadOnlyDictionary<string, IInteriorProvider> providers, ITerrain? terrain)
    {
        ArgumentNullException.ThrowIfNull(providers);
        using JsonDocument document = JsonDocument.Parse(interiorsBytes);
        if (document.RootElement.GetProperty("version").GetInt32() != 1) throw new JsonException("Unsupported interior catalog version.");
        _ = InteriorEntranceJson.ParseClosedEntrances(document.RootElement, terrain);
        var bindings = new List<InteriorProviderBinding>();
        var configurations = new List<(IInteriorCatalogConfigurable Provider, FeatureId[] Features, int[] Floors,
            IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> Entrances)>();
        var claimed = new HashSet<FeatureId>();
        var providerIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement item in document.RootElement.GetProperty("interiors").EnumerateArray())
        {
            string providerId = item.GetProperty("providerId").GetString() ?? throw new JsonException("Missing provider ID.");
            if (string.IsNullOrWhiteSpace(providerId) || !providerIds.Add(providerId))
                throw new JsonException($"Empty or duplicate interior provider ID: {providerId}");
            if (!providers.TryGetValue(providerId, out IInteriorProvider? provider) || provider is null)
                throw new InvalidOperationException($"Unregistered interior provider: {providerId}");
            FeatureId[] features = item.GetProperty("featureIds").EnumerateArray()
                .Select(e => new FeatureId(e.GetString() ?? throw new JsonException("Missing feature ID.")))
                .OrderBy(feature => feature.Value, StringComparer.Ordinal).ToArray();
            int[] floors = item.GetProperty("openFloors").EnumerateArray().Select(e => e.GetInt32()).Order().ToArray();
            if (features.Length == 0 || floors.Length == 0)
                throw new JsonException($"Interior {providerId} needs features and open floors.");
            foreach (FeatureId feature in features)
                if (string.IsNullOrWhiteSpace(feature.Value) || !claimed.Add(feature))
                    throw new JsonException($"Empty or multiply claimed interior feature: {feature.Value}");
            if (floors.Distinct().Count() != floors.Length || floors.Any(floor => floor < short.MinValue || floor > short.MaxValue))
                throw new JsonException($"Interior {providerId} has duplicate or out-of-range open floors.");
            var entranceLists = new Dictionary<FeatureId, List<EntranceDecl>>();
            int authoredEntranceCount = 0;
            foreach (JsonElement entry in item.GetProperty("entrances").EnumerateArray())
            {
                var feature = new FeatureId(entry.GetProperty("featureId").GetString() ?? throw new JsonException("Missing entrance feature ID."));
                if (!features.Contains(feature)) throw new JsonException("Entrance belongs to a feature outside its interior.");
                if (!entranceLists.TryGetValue(feature, out var list)) entranceLists.Add(feature, list = []);
                list.Add(InteriorEntranceJson.Parse(entry, feature, terrain));
                authoredEntranceCount++;
            }
            if (provider is IInteriorCatalogConfigurable configurable)
                configurations.Add((configurable, features, floors, entranceLists.ToDictionary(pair => pair.Key,
                    pair => (IReadOnlyList<EntranceDecl>)Array.AsReadOnly(pair.Value.ToArray()))));
            bindings.Add(new(providerId, Array.AsReadOnly(features), Array.AsReadOnly(floors), authoredEntranceCount));
        }
        // Validate the entire catalog before mutating any fallback provider; no geometry is generated here.
        foreach (var configuration in configurations)
            configuration.Provider.Configure(configuration.Features, configuration.Floors, configuration.Entrances);
        return bindings.AsReadOnly();
    }

    /// <summary>Convenience for fresh, unpublished map-owned providers. Shared/global registry instances must not be passed.</summary>
    public static InteriorAssemblyResult Build(byte[] interiorsBytes, BuildContext context,
        IReadOnlyDictionary<string, IInteriorProvider> providers) =>
        BuildCore(context, providers, Configure(interiorsBytes, providers, context.World.Terrain), interiorsBytes);

    /// <summary>Builds from already configured map-owned instances; this method never calls Configure.</summary>
    public static InteriorAssemblyResult BuildConfigured(BuildContext context, IReadOnlyList<InteriorProviderBinding> bindings,
        ReadOnlyMemory<byte> interiorsBytes = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bindings);
        return BuildCore(context, context.InteriorProviders, bindings, interiorsBytes);
    }

    private static InteriorAssemblyResult BuildCore(BuildContext context,
        IReadOnlyDictionary<string, IInteriorProvider> providers, IReadOnlyList<InteriorProviderBinding> bindings,
        ReadOnlyMemory<byte> interiorsBytes)
    {
        // Reject the complete closed register before calling any provider or generating content.
        var closedEntries = new List<InteriorIndexEntry>();
        if (!interiorsBytes.IsEmpty)
        {
            using var document = JsonDocument.Parse(interiorsBytes);
            var closed = InteriorEntranceJson.ParseClosedEntrances(document.RootElement, context.World.Terrain);
            foreach (var pair in closed.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal))
            {
                if (!context.World.Features.TryGetValue(pair.Key, out var feature))
                    throw new JsonException($"Closed entrance '{pair.Value[0].Id}' names missing feature '{pair.Key.Value}'.");
                var storeys = BuildingStoreys(feature);
                if (storeys.Count == 0)
                    throw new JsonException($"Closed entrance '{pair.Value[0].Id}' feature '{pair.Key.Value}' has no building footprint.");
                if (bindings.Any(binding => binding.FeatureIds.Contains(pair.Key)))
                    throw new JsonException($"Closed entrance '{pair.Value[0].Id}' conflicts with configured interior '{pair.Key.Value}'.");
                var existing = context.World.Interiors.Get(pair.Key);
                if (existing is { Open: true } || existing?.ProviderId is not null)
                    throw new JsonException($"Closed entrance '{pair.Value[0].Id}' conflicts with existing open interior '{pair.Key.Value}'.");
                closedEntries.Add(new(pair.Key, existing?.Storeys ?? storeys, pair.Value, false, null, []));
            }
        }
        var meshes = new List<GeometryBundle>();
        var colliders = new List<Collider>();
        var entries = new List<InteriorIndexEntry>();
        var nodes = new List<NavNode>();
        var edges = new List<NavEdge>();
        var portals = new List<NavPortal>();
        foreach (var binding in bindings)
        {
            string providerId = binding.ProviderId;
            var provider = providers[providerId];
            foreach (FeatureId feature in binding.FeatureIds)
            {
                InteriorLayout layout = ValidatedLayout(provider, binding, context, feature);
                entries.Add(new(feature, layout.Storeys, layout.Entrances, provider.Walkable, providerId, layout.OpenStoreys));
                NavFloorGraph? entranceFloor = null;
                foreach (int floor in layout.OpenStoreys.Order())
                {
                    meshes.AddRange(provider.BuildStorey(context, feature, floor, InteriorDetail.Full).Meshes);
                    colliders.AddRange(provider.CollisionStorey(context, feature, floor));
                    NavFloorGraph graph = provider.Nav(context, feature, floor);
                    nodes.AddRange(graph.Nodes); edges.AddRange(graph.Edges);
                    entranceFloor ??= graph;
                }
                if (entranceFloor is null) continue;
                foreach (EntranceDecl entrance in layout.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal))
                {
                    NavNode? inside = entranceFloor.Nodes.Where(n => n.Space.Floor == entranceFloor.Floor)
                        .OrderBy(n => Vector3.DistanceSquared(n.P, entrance.Inside)).ThenBy(n => n.Id.Value).FirstOrDefault();
                    if (inside is null) continue;
                    var outsideId = new NavNodeId(StableHash.Hash32(context.WorldSeed, feature.Value, entrance.Id, "d0.outside"));
                    nodes.Add(new(outsideId, entrance.Outside, new(null, 0), entrance.Id));
                    edges.Add(new(outsideId, inside.Id, NavEdgeKind.Door, Vector3.Distance(entrance.Outside, inside.P),
                        entrance.ClearWidth, inside.P.Y - entrance.Outside.Y, NavFlags.None, "d0.entry"));
                    portals.Add(new($"{feature.Value}/{entrance.Id}", feature, outsideId, inside.Id,
                        entrance.Center, entrance.YawDeg, entrance.ClearWidth, DoorKind.OpenArch, DoorState.Open));
                }
            }
        }
        entries.AddRange(closedEntries);
        return new(new(meshes, []), colliders, entries, nodes, edges, portals);
    }

    private static IReadOnlyList<StoreyInfo> BuildingStoreys(WorldFeature feature)
    {
        var parts = feature.Parts.Count == 0
            ? new[] { new WorldFeaturePart("", feature.Geometry, feature.Height, feature.Storeys) }
            : feature.Parts;
        var storeys = new List<StoreyInfo>();
        foreach (var part in parts)
        {
            var kind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var declared)
                ? declared.GetString() : feature.Kind.Value;
            if (kind != "building") continue;
            IReadOnlyList<PolygonRings> polygons = part.Geometry switch
            {
                PolygonGeometry polygon => new[] { polygon.Rings },
                MultiPolygonGeometry multi => multi.Polygons,
                _ => []
            };
            var count = Math.Max(1, part.Storeys ?? (int)MathF.Ceiling((part.Height ?? 3.6f) / 3.6f));
            var height = (part.Height ?? count * 3.6f) / count;
            foreach (var polygon in polygons)
                for (var floor = 1; floor <= count; floor++)
                    storeys.Add(new(floor, (floor - 1) * height, height, polygon.Outer));
        }
        return storeys.AsReadOnly();
    }

}
