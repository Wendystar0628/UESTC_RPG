using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Parts.Stubs;

namespace Campus.Kernel.Stubs;

public sealed record D0InteriorBinding(
    string ProviderId,
    IReadOnlyList<FeatureId> FeatureIds,
    IReadOnlyList<int> OpenFloors,
    int AuthoredEntranceCount);

public sealed record D0InteriorSetupResult(
    ContentOutput Content,
    IReadOnlyList<Collider> Colliders,
    IReadOnlyList<InteriorIndexEntry> Entries,
    IReadOnlyList<NavNode> NavNodes,
    IReadOnlyList<NavEdge> NavEdges,
    IReadOnlyList<NavPortal> NavPortals);

/// <summary>Pure CPU D0 assembly. The host reads bytes and calls this on a worker before mounting.</summary>
public static class D0InteriorSetup
{
    /// <summary>
    /// Pure CPU configuration only: parse the authoritative catalog and configure fresh map-owned fallback providers.
    /// Call once before publishing the map's worker context, before any provider Layout/Build/Collision/Nav call.
    /// The returned bindings, not every globally registered provider's mutable FeatureIds, define this map's coverage.
    /// </summary>
    public static IReadOnlyList<D0InteriorBinding> Configure(ReadOnlyMemory<byte> interiorsBytes,
        IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        using JsonDocument document = JsonDocument.Parse(interiorsBytes);
        if (document.RootElement.GetProperty("version").GetInt32() != 1) throw new JsonException("Unsupported interior catalog version.");
        var bindings = new List<D0InteriorBinding>();
        var configurations = new List<(BoxInteriorProviderStub Stub, FeatureId[] Features, int[] Floors,
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
                float width = entry.GetProperty("clearWidth").GetSingle();
                float yaw = entry.GetProperty("facingDeg").GetSingle();
                if (!float.IsFinite(width) || width <= 0 || !float.IsFinite(yaw))
                    throw new JsonException($"Interior {providerId} has invalid entrance dimensions.");
                list.Add(new(entry.GetProperty("id").GetString() ?? throw new JsonException("Missing entrance ID."),
                    Vector(entry.GetProperty("outsideLanding")), Vector(entry.GetProperty("insideLanding")),
                    width, entry.GetProperty("open").GetBoolean())
                { Center = Vector(entry.GetProperty("doorCenter")), YawDeg = yaw });
                authoredEntranceCount++;
            }
            if (provider is BoxInteriorProviderStub stub)
                configurations.Add((stub, features, floors, entranceLists.ToDictionary(pair => pair.Key,
                    pair => (IReadOnlyList<EntranceDecl>)Array.AsReadOnly(pair.Value.ToArray()))));
            bindings.Add(new(providerId, Array.AsReadOnly(features), Array.AsReadOnly(floors), authoredEntranceCount));
        }
        // Validate the entire catalog before mutating any fallback provider; no geometry is generated here.
        foreach (var configuration in configurations)
            configuration.Stub.Configure(configuration.Features, configuration.Floors, configuration.Entrances);
        return bindings.AsReadOnly();
    }

    /// <summary>Legacy convenience for fresh, unpublished map-owned providers. Shared/global registry instances must not be passed.</summary>
    public static D0InteriorSetupResult Build(byte[] interiorsBytes, BuildContext context,
        IReadOnlyDictionary<string, IInteriorProvider> providers) =>
        BuildCore(context, providers, Configure(interiorsBytes, providers));

    /// <summary>Builds from already configured map-owned instances; this method never calls Configure.</summary>
    public static D0InteriorSetupResult BuildConfigured(BuildContext context, IReadOnlyList<D0InteriorBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bindings);
        return BuildCore(context, context.InteriorProviders, bindings);
    }

    private static D0InteriorSetupResult BuildCore(BuildContext context,
        IReadOnlyDictionary<string, IInteriorProvider> providers, IReadOnlyList<D0InteriorBinding> bindings)
    {
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
                InteriorLayout layout = provider.Layout(context, feature);
                entries.Add(new(feature, layout.Storeys, layout.Entrances, provider.Walkable, providerId));
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
        return new(new(meshes, []), colliders, entries, nodes, edges, portals);
    }

    private static Vector3 Vector(JsonElement value)
    {
        if (value.GetArrayLength() != 3) throw new JsonException("A spatial coordinate must contain three values.");
        var values = value.EnumerateArray().Select(e => e.GetSingle()).ToArray();
        if (values.Any(v => !float.IsFinite(v))) throw new JsonException("A spatial coordinate must be finite.");
        return new(values[0], values[1], values[2]);
    }
}
