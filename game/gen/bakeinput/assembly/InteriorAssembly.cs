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
    public IReadOnlyList<FeatureId> InteriorFeatures { get; } = Array.AsReadOnly(FeatureIds
        .Where(feature => InteriorOpenPart.IndoorFloorsFor(OpenParts, feature).Count > 0).ToArray());
    public IReadOnlyList<int> FloorsFor(FeatureId feature) => InteriorOpenPart.IndoorFloorsFor(OpenParts, feature);
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
            var actual = binding.InteriorFeatures.Count == 0 ? Array.Empty<FeatureId>() : providers[binding.ProviderId].FeatureIds;
            if (actual.Count != binding.InteriorFeatures.Count || actual.Distinct().Count() != actual.Count || !actual.ToHashSet().SetEquals(binding.InteriorFeatures))
                throw new InvalidDataException($"Interior provider {binding.ProviderId} is not bound to its catalog: expected {binding.InteriorFeatures.Count} features, actual {actual.Count}.");
            foreach (var part in binding.OpenParts)
            {
                if (!world.Features.TryGetValue(part.FeatureId, out var feature))
                    throw new InvalidDataException($"Interior catalog feature is absent from the loaded world: {part.FeatureId.Value}");
                if (InteriorOpenPart.IsIndoor(part.Kind) && !WorldFeatureKinds.Parts(feature, "building").Any(source => source.SourceId == part.SourceId))
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
        return prepared;
    }

    /// <summary>Cold-only capture from the same sites that own visible geometry and collision.</summary>
    public static IReadOnlyList<OutdoorAccessArea> CaptureOutdoorAccess(BuildContext context,
        IReadOnlyList<InteriorProviderBinding> bindings, IEnumerable<IOutdoorAccessSource> sources)
    {
        var requested = bindings.SelectMany(binding => binding.OpenParts)
            .Where(part => !InteriorOpenPart.IsIndoor(part.Kind)).ToDictionary(part => part.SourceId, StringComparer.Ordinal);
        if (requested.Count == 0) return [];
        var found = new Dictionary<string, OutdoorAccessArea>(StringComparer.Ordinal);
        foreach (var source in sources)
            foreach (var area in source.DescribeOutdoorAccess(context))
                if (!found.TryAdd(area.Id, area)) throw new InvalidDataException($"Duplicate outdoor access source: {area.Id}.");
        var result = new List<OutdoorAccessArea>();
        foreach (var pair in requested.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!found.TryGetValue(pair.Key, out var source) || source.Feature != pair.Value.FeatureId)
                throw new InvalidDataException($"Missing outdoor access source: {pair.Value.FeatureId.Value}/{pair.Key}.");
            ValidateOutdoor(source);
            result.Add(source with { Areas = Array.AsReadOnly(source.Areas.Select(area => area with
                { Footprint = Array.AsReadOnly(area.Footprint.ToArray()) }).ToArray()) });
        }
        return result.AsReadOnly();
    }

    public static IReadOnlyList<InteriorIndexEntry> IndexEntries(IReadOnlyList<InteriorLayout> layouts,
        IReadOnlyList<InteriorProviderBinding> bindings, IReadOnlyDictionary<string, IInteriorProvider> providers,
        IWorldModel world, IReadOnlyList<OutdoorAccessArea> outdoorAccess)
    {
        var byFeature = layouts.ToDictionary(layout => layout.Feature);
        var outside = outdoorAccess.ToDictionary(area => area.Id, StringComparer.Ordinal);
        var result = new List<InteriorIndexEntry>();
        foreach (var binding in bindings)
            foreach (var feature in binding.FeatureIds)
            {
                var floors = binding.FloorsFor(feature);
                InteriorLayout? layout = null;
                if (floors.Count > 0)
                {
                    if (!byFeature.Remove(feature, out layout) || layout.OpenStoreys.Count == 0 ||
                        !layout.OpenStoreys.ToHashSet().SetEquals(floors) || layout.Storeys.Count == 0 ||
                        layout.OpenStoreys.Any(floor => !layout.Storeys.Any(storey => storey.Index == floor)) ||
                        layout.Storeys.Any(area => !Valid(area.FloorY, area.Height, area.Footprint)))
                        throw new InvalidDataException($"Baked interior layout differs from catalog: {binding.ProviderId}/{feature.Value}.");
                }
                var areas = new List<InteriorOpenArea>(layout?.OpenAreas ?? []);
                if (floors.Any(floor => !areas.Any(area => area.Index == floor)) ||
                    areas.Any(area => !InteriorOpenPart.IsIndoor(area.Kind) || !floors.Contains(area.Index) || !Valid(area.FloorY, area.Height, area.Footprint)))
                    throw new InvalidDataException($"Baked interior has invalid frozen access areas: {binding.ProviderId}/{feature.Value}.");
                foreach (var part in binding.OpenParts.Where(part => part.FeatureId == feature && !InteriorOpenPart.IsIndoor(part.Kind)))
                {
                    if (!outside.Remove(part.SourceId, out var source) || source.Feature != feature)
                        throw new InvalidDataException($"Baked outdoor access differs from catalog: {feature.Value}/{part.SourceId}.");
                    ValidateOutdoor(source);
                    areas.AddRange(source.Areas.Select(area => new InteriorOpenArea(InteriorOpenPartKind.OutdoorHighPlace,
                        area.Index, area.FloorY, area.Height, area.Footprint)));
                }
                if (areas.Count == 0) throw new InvalidDataException($"Catalog feature has no frozen access areas: {feature.Value}.");
                if (layout is not null) ValidateElevators(layout, areas);
                var original = layout is null ? world.Interiors.Get(feature)
                    ?? throw new InvalidDataException($"Missing building index: {feature.Value}.") : null;
                result.Add(new(feature, layout is null ? original!.Storeys : layout.Storeys,
                    layout is null ? original!.Entrances : layout.Entrances,
                    layout is null || providers[binding.ProviderId].Walkable,
                    layout is null ? null : binding.ProviderId, floors, areas.AsReadOnly()));
            }
        if (byFeature.Count != 0 || outside.Count != 0) throw new InvalidDataException("Baked access includes unregistered areas.");
        return result.AsReadOnly();
    }

    private static bool Valid(float floorY, float height, IReadOnlyList<Vector2> footprint) =>
        float.IsFinite(floorY) && float.IsFinite(height) && height > 0 && footprint.Count >= 3 && footprint.All(VectorMath.Finite);

    private static void ValidateOutdoor(OutdoorAccessArea source)
    {
        if (source.Areas.Count == 0 || source.Areas.Any(area => area.Index < short.MinValue || area.Index > short.MaxValue ||
            !Valid(area.FloorY, area.Height, area.Footprint)))
            throw new InvalidDataException($"Invalid outdoor access source: {source.Id}.");
    }

    private static IReadOnlyList<InteriorOpenArea> OpenAreas(InteriorLayout layout, InteriorProviderBinding binding, IWorldModel world)
    {
        var result = new List<InteriorOpenArea>();
        var feature = world.Features[layout.Feature];
        foreach (var scope in binding.OpenParts.Where(part => part.FeatureId == layout.Feature && InteriorOpenPart.IsIndoor(part.Kind)))
        {
            var source = WorldFeatureKinds.Parts(feature, "building").SingleOrDefault(part => part.SourceId == scope.SourceId)
                ?? throw new InvalidDataException($"Interior catalog part is absent: {layout.Feature.Value}/{scope.SourceId}.");
            var footprints = WorldFeatureGeometry.PolygonsOf(source.Geometry).ToArray();
            foreach (var floor in scope.Floors)
            {
                foreach (var storey in layout.Storeys.Where(storey => storey.Index == floor))
                    foreach (var footprint in footprints)
                        foreach (var ring in PlanGeometry.IntersectionTriangles(new(storey.Footprint), footprint))
                            result.Add(new(InteriorOpenPartKind.IndoorFloor, storey.Index, storey.FloorY, storey.Height, ring));
            }
        }
        foreach (var floor in binding.FloorsFor(layout.Feature))
            if (!result.Any(area => area.Index == floor))
                throw new InvalidDataException($"Open catalog floor has no actual provider area: {layout.Feature.Value}/{floor}.");
        return result.AsReadOnly();
    }

    private static void ValidateElevators(InteriorLayout layout, IReadOnlyList<InteriorOpenArea> areas)
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
        if (document.RootElement.GetProperty("version").GetInt32() != 3) throw new JsonException("Unsupported interior catalog version.");
        _ = InteriorEntranceJson.ParseClosedEntrances(document.RootElement, terrain);
        var bindings = new List<InteriorProviderBinding>();
        var configurations = new List<(IInteriorCatalogConfigurable Provider, InteriorCatalogConfiguration Configuration)>();
        var claimed = new HashSet<FeatureId>();
        var providerIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement item in document.RootElement.GetProperty("interiors").EnumerateArray())
        {
            FeatureId[] features = item.GetProperty("featureIds").EnumerateArray()
                .Select(e => new FeatureId(e.GetString() ?? throw new JsonException("Missing feature ID.")))
                .OrderBy(feature => feature.Value, StringComparer.Ordinal).ToArray();
            var openParts = InteriorEntranceJson.ParseOpenParts(item);
            var indoorFeatures = features.Where(feature => InteriorOpenPart.IndoorFloorsFor(openParts, feature).Count > 0).ToArray();
            string providerId = indoorFeatures.Length == 0 ? "" : item.GetProperty("providerId").GetString()
                ?? throw new JsonException("Missing provider ID.");
            IInteriorProvider? provider = null;
            if (indoorFeatures.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(providerId) || !providerIds.Add(providerId)) throw new JsonException($"Empty or duplicate provider ID: {providerId}.");
                if (!providers.TryGetValue(providerId, out provider)) throw new InvalidOperationException($"Unregistered interior provider: {providerId}.");
            }
            if (features.Length == 0) throw new JsonException("Access catalog entry needs features.");
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
                configurations.Add((configurable, new(Array.AsReadOnly(indoorFeatures), openParts, entranceLists.ToDictionary(pair => pair.Key,
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
            var parts = InteriorEntranceJson.ParseOpenParts(item);
            if (!parts.Any(part => InteriorOpenPart.IsIndoor(part.Kind))) continue;
            var id = item.GetProperty("providerId").GetString() ?? throw new JsonException("Missing provider ID.");
            result.Add((id, RegistrationDiscovery.ResolveInterior(registrations, id)));
        }
        if (result.Select(r => r.Item1).Distinct(StringComparer.Ordinal).Count() != result.Count)
            throw new JsonException("Duplicate catalog interior provider ID.");
        return result.OrderBy(r => r.Item1, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Convenience for fresh, unpublished map-owned providers. Shared/global registry instances must not be passed.</summary>
    public static InteriorAssemblyResult Build(byte[] interiorsBytes, BuildContext context,
        IReadOnlyDictionary<string, IInteriorProvider> providers, IEnumerable<IOutdoorAccessSource>? outdoorSources = null) =>
        BuildCore(context, providers, Configure(interiorsBytes, providers, context.World.Terrain), interiorsBytes, outdoorSources ?? []);

    /// <summary>Builds from already configured map-owned instances; this method never calls Configure.</summary>
    public static InteriorAssemblyResult BuildConfigured(BuildContext context, IReadOnlyList<InteriorProviderBinding> bindings,
        ReadOnlyMemory<byte> interiorsBytes = default, IEnumerable<IOutdoorAccessSource>? outdoorSources = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bindings);
        return BuildCore(context, context.InteriorProviders, bindings, interiorsBytes, outdoorSources ?? []);
    }

    private static InteriorAssemblyResult BuildCore(BuildContext context,
        IReadOnlyDictionary<string, IInteriorProvider> providers, IReadOnlyList<InteriorProviderBinding> bindings,
        ReadOnlyMemory<byte> interiorsBytes, IEnumerable<IOutdoorAccessSource> outdoorSources)
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
        var layouts = new List<InteriorLayout>();
        var outdoor = CaptureOutdoorAccess(context, bindings, outdoorSources);
        var nodes = new List<NavNode>();
        var edges = new List<NavEdge>();
        var portals = new List<NavPortal>();
        foreach (var binding in bindings)
        {
            string providerId = binding.ProviderId;
            if (binding.InteriorFeatures.Count == 0) continue;
            var provider = providers[providerId];
            foreach (FeatureId feature in binding.InteriorFeatures)
            {
                InteriorLayout layout = ValidatedLayout(provider, binding, context, feature);
                layouts.Add(layout);
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
        var entries = IndexEntries(layouts, bindings, providers, context.World, outdoor).ToList();
        entries.AddRange(closedEntries);
        return new(new(meshes, []), colliders, entries, nodes, edges, portals);
    }

}
