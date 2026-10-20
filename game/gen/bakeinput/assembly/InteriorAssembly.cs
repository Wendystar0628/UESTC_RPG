using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.Geometry;

namespace Campus.Gen.BakeInput;

public sealed record InteriorProviderBinding(
    string ProviderId,
    IReadOnlyList<FeatureId> FeatureIds,
    IReadOnlyList<InteriorOpenPart> OpenParts,
    int AuthoredEntranceCount)
{
    public IReadOnlyList<int> OpenFloors { get; } = Array.AsReadOnly(OpenParts.SelectMany(part => part.Floors).Distinct().Order().ToArray());
    public IReadOnlyList<int> FloorsFor(FeatureId feature) => Array.AsReadOnly(OpenParts.Where(part => part.FeatureId == feature)
        .SelectMany(part => part.Floors).Distinct().Order().ToArray());
}

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
            foreach (var part in binding.OpenParts)
            {
                if (!world.Features.TryGetValue(part.FeatureId, out var feature))
                    throw new InvalidDataException($"Interior catalog feature is absent from the loaded world: {part.FeatureId.Value}");
                if (!WorldFeatureKinds.Parts(feature, "building").Any(source => source.SourceId == part.SourceId))
                    throw new InvalidDataException($"Interior catalog part is absent from the loaded building: {part.FeatureId.Value}/{part.SourceId}.");
            }
        }
        return bindings;
    }

    public static InteriorLayout ValidatedLayout(IInteriorProvider provider, InteriorProviderBinding binding, BuildContext context, FeatureId feature)
    {
        var layout = provider.Layout(context, feature);
        if (layout.Feature != feature || layout.OpenStoreys.Count == 0 || layout.OpenStoreys.Distinct().Count() != layout.OpenStoreys.Count ||
            !layout.OpenStoreys.ToHashSet().SetEquals(binding.FloorsFor(feature)) ||
            layout.OpenStoreys.Any(floor => !layout.Storeys.Any(storey => storey.Index == floor)))
            throw new InvalidDataException($"Interior layout coverage differs from its catalog: {binding.ProviderId}/{feature.Value}.");
        foreach (var storey in layout.Storeys)
            if (!float.IsFinite(storey.FloorY) || !float.IsFinite(storey.Height) || storey.Height <= 0)
                throw new InvalidDataException($"Interior provider returned invalid floor elevation/height: {binding.ProviderId}/{feature.Value}.");
        if (context.World.Interiors.Get(feature) is { Storeys.Count: > 0 } indexed)
        {
            var expected = indexed.Storeys.OrderBy(s => s.Index).ThenBy(s => s.FloorY).ToArray();
            var actual = layout.Storeys.OrderBy(s => s.Index).ThenBy(s => s.FloorY).ToArray();
            if (actual.Length != expected.Length || actual.Zip(expected).Any(pair => pair.First.Index != pair.Second.Index ||
                pair.First.FloorY != pair.Second.FloorY || pair.First.Height != pair.Second.Height))
                throw new InvalidDataException($"Interior layout changed its baked FloorY/Height: {binding.ProviderId}/{feature.Value}.");
        }
        var prepared = layout with { OpenAreas = OpenAreas(layout, binding, context.World) };
        ValidateElevators(prepared, prepared.OpenAreas);
        return prepared;
    }

    public static IReadOnlyList<InteriorIndexEntry> IndexEntries(IReadOnlyList<InteriorLayout> layouts,
        IReadOnlyList<InteriorProviderBinding> bindings, IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        var byFeature = layouts.ToDictionary(layout => layout.Feature);
        var result = new List<InteriorIndexEntry>();
        foreach (var binding in bindings)
            foreach (var feature in binding.FeatureIds)
            {
                if (!byFeature.Remove(feature, out var layout) || layout.OpenStoreys.Count == 0 ||
                    !layout.OpenStoreys.ToHashSet().SetEquals(binding.FloorsFor(feature)) || layout.Storeys.Count == 0 ||
                    layout.OpenStoreys.Any(floor => !layout.Storeys.Any(s => s.Index == floor)) ||
                    layout.Storeys.Any(s => !float.IsFinite(s.FloorY) || !float.IsFinite(s.Height) || s.Height <= 0))
                    throw new InvalidDataException($"Baked interior layout differs from catalog: {binding.ProviderId}/{feature.Value}.");
                var areas = layout.OpenAreas;
                if (areas.Count == 0 || binding.FloorsFor(feature).Any(floor => !areas.Any(area => area.Index == floor)) ||
                    areas.Any(area => !layout.OpenStoreys.Contains(area.Index) || area.Footprint.Count < 3 ||
                        !float.IsFinite(area.FloorY) || !float.IsFinite(area.Height) || area.Height <= 0 || area.Footprint.Any(point => !VectorMath.Finite(point))))
                    throw new InvalidDataException($"Baked interior has no valid frozen access areas: {binding.ProviderId}/{feature.Value}.");
                ValidateElevators(layout, areas);
                result.Add(new(feature, layout.Storeys, layout.Entrances, providers[binding.ProviderId].Walkable,
                    binding.ProviderId, layout.OpenStoreys, areas));
            }
        if (byFeature.Count != 0) throw new InvalidDataException("Baked interior layouts include unregistered features.");
        return result.AsReadOnly();
    }

    private static IReadOnlyList<StoreyInfo> OpenAreas(InteriorLayout layout, InteriorProviderBinding binding, IWorldModel world)
    {
        var result = new List<StoreyInfo>();
        var feature = world.Features[layout.Feature];
        foreach (var scope in binding.OpenParts.Where(part => part.FeatureId == layout.Feature))
        {
            var source = WorldFeatureKinds.Parts(feature, "building").SingleOrDefault(part => part.SourceId == scope.SourceId)
                ?? throw new InvalidDataException($"Interior catalog part is absent: {layout.Feature.Value}/{scope.SourceId}.");
            var footprints = WorldFeatureGeometry.PolygonsOf(source.Geometry).ToArray();
            foreach (var floor in scope.Floors)
            {
                foreach (var storey in layout.Storeys.Where(storey => storey.Index == floor))
                    foreach (var footprint in footprints)
                        foreach (var ring in PlanGeometry.IntersectionTriangles(new(storey.Footprint), footprint))
                            result.Add(storey with { Footprint = ring });
            }
        }
        foreach (var floor in binding.FloorsFor(layout.Feature))
            if (!result.Any(area => area.Index == floor))
                throw new InvalidDataException($"Open catalog floor has no actual provider area: {layout.Feature.Value}/{floor}.");
        return result.AsReadOnly();
    }

    private static void ValidateElevators(InteriorLayout layout, IReadOnlyList<StoreyInfo> areas)
    {
        foreach (var elevator in layout.Elevators)
        {
            Stop(elevator.Id + ".a", elevator.StoreyA, elevator.CabA);
            Stop(elevator.Id + ".b", elevator.StoreyB, elevator.CabB);
        }
        return;
        void Stop(string id, int floor, Box3 cab)
        {
            var point = cab.Center;
            if (!VectorMath.Finite(cab.Min) || !VectorMath.Finite(cab.Max) ||
                !areas.Any(area => area.Index == floor && point.Y >= area.FloorY && point.Y < area.FloorY + area.Height &&
                    PlanGeometry.PointInRing(new(point.X, point.Z), area.Footprint)))
                throw new InvalidDataException($"Elevator stop is outside the catalog's open area: {layout.Feature.Value}/{id}/{floor}.");
        }
    }

    private static IReadOnlyList<InteriorProviderBinding> ConfigureCore(ReadOnlyMemory<byte> interiorsBytes,
        IReadOnlyDictionary<string, IInteriorProvider> providers, ITerrain? terrain)
    {
        ArgumentNullException.ThrowIfNull(providers);
        using JsonDocument document = JsonDocument.Parse(interiorsBytes);
        if (document.RootElement.GetProperty("version").GetInt32() != 2) throw new JsonException("Unsupported interior catalog version.");
        _ = InteriorEntranceJson.ParseClosedEntrances(document.RootElement, terrain);
        var bindings = new List<InteriorProviderBinding>();
        var configurations = new List<(IInteriorCatalogConfigurable Provider, InteriorCatalogConfiguration Configuration)>();
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
            var openParts = InteriorEntranceJson.ParseOpenParts(item);
            int[] floors = openParts.SelectMany(part => part.Floors).Distinct().Order().ToArray();
            if (features.Length == 0 || floors.Length == 0)
                throw new JsonException($"Interior {providerId} needs features and open floors.");
            foreach (FeatureId feature in features)
                if (string.IsNullOrWhiteSpace(feature.Value) || !claimed.Add(feature))
                    throw new JsonException($"Empty or multiply claimed interior feature: {feature.Value}");
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
                configurations.Add((configurable, new(features, openParts, entranceLists.ToDictionary(pair => pair.Key,
                    pair => (IReadOnlyList<EntranceDecl>)Array.AsReadOnly(pair.Value.ToArray())),
                    item.TryGetProperty("buildingType", out var buildingType) ? buildingType.Clone() : null,
                    item.TryGetProperty("seals", out var seals)
                        ? JsonSerializer.Deserialize<SealOp[]>(seals.GetRawText(), ContractJson.Options) ?? throw new JsonException("Null catalog seals.") : [])));
            bindings.Add(new(providerId, Array.AsReadOnly(features), openParts, authoredEntranceCount));
        }
        // Validate the entire catalog before mutating any fallback provider; no geometry is generated here.
        foreach (var configuration in configurations)
            configuration.Provider.Configure(configuration.Configuration);
        return bindings.AsReadOnly();
    }

    public static IReadOnlyList<(string ProviderId, Registration Registration)> ResolveRegistrations(ReadOnlyMemory<byte> catalog,
        IReadOnlyList<Registration> registrations)
    {
        using var document = JsonDocument.Parse(catalog);
        var result = new List<(string, Registration)>();
        foreach (var item in document.RootElement.GetProperty("interiors").EnumerateArray())
        {
            var id = item.GetProperty("providerId").GetString() ?? throw new JsonException("Missing provider ID.");
            result.Add((id, RegistrationDiscovery.ResolveInterior(registrations, id)));
        }
        if (result.Select(r => r.Item1).Distinct(StringComparer.Ordinal).Count() != result.Count)
            throw new JsonException("Duplicate catalog interior provider ID.");
        return result.OrderBy(r => r.Item1, StringComparer.Ordinal).ToArray();
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
                var existing = context.World.Interiors.Get(pair.Key)
                    ?? throw new JsonException($"Closed entrance feature '{pair.Key.Value}' has no building footprint.");
                if (bindings.Any(binding => binding.FeatureIds.Contains(pair.Key)))
                    throw new JsonException($"Closed entrance '{pair.Value[0].Id}' conflicts with configured interior '{pair.Key.Value}'.");
                if (existing is { Open: true } || existing.ProviderId is not null)
                    throw new JsonException($"Closed entrance '{pair.Value[0].Id}' conflicts with existing open interior '{pair.Key.Value}'.");
                closedEntries.Add(new(pair.Key, existing.Storeys, pair.Value, false, null, [], []));
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
                entries.Add(new(feature, layout.Storeys, layout.Entrances, provider.Walkable, providerId, layout.OpenStoreys,
                    layout.OpenAreas));
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

}
