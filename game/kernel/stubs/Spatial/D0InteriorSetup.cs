using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Parts.Stubs;

namespace Campus.Kernel.Stubs;

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
    public static D0InteriorSetupResult Build(byte[] interiorsBytes, BuildContext context,
        IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        using JsonDocument document = JsonDocument.Parse(interiorsBytes);
        if (document.RootElement.GetProperty("version").GetInt32() != 1) throw new JsonException("Unsupported interior catalog version.");
        var meshes = new List<GeometryBundle>();
        var colliders = new List<Collider>();
        var entries = new List<InteriorIndexEntry>();
        var nodes = new List<NavNode>();
        var edges = new List<NavEdge>();
        var portals = new List<NavPortal>();
        var claimed = new HashSet<FeatureId>();
        foreach (JsonElement item in document.RootElement.GetProperty("interiors").EnumerateArray())
        {
            string providerId = item.GetProperty("providerId").GetString() ?? throw new JsonException("Missing provider ID.");
            if (!providers.TryGetValue(providerId, out IInteriorProvider? provider)) throw new InvalidOperationException($"Unregistered interior provider: {providerId}");
            FeatureId[] features = item.GetProperty("featureIds").EnumerateArray().Select(e => new FeatureId(e.GetString() ?? throw new JsonException("Missing feature ID."))).ToArray();
            int[] floors = item.GetProperty("openFloors").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            var entranceLists = new Dictionary<FeatureId, List<EntranceDecl>>();
            foreach (JsonElement entry in item.GetProperty("entrances").EnumerateArray())
            {
                var feature = new FeatureId(entry.GetProperty("featureId").GetString() ?? throw new JsonException("Missing entrance feature ID."));
                if (!features.Contains(feature)) throw new JsonException("Entrance belongs to a feature outside its interior.");
                if (!entranceLists.TryGetValue(feature, out var list)) entranceLists.Add(feature, list = []);
                list.Add(new(entry.GetProperty("id").GetString() ?? throw new JsonException("Missing entrance ID."),
                    Vector(entry.GetProperty("outsideLanding")), Vector(entry.GetProperty("insideLanding")),
                    entry.GetProperty("clearWidth").GetSingle(), entry.GetProperty("open").GetBoolean())
                { Center = Vector(entry.GetProperty("doorCenter")), YawDeg = entry.GetProperty("facingDeg").GetSingle() });
            }
            if (provider is BoxInteriorProviderStub stub)
                stub.Configure(features, floors, entranceLists.ToDictionary(p => p.Key, p => (IReadOnlyList<EntranceDecl>)p.Value));

            foreach (FeatureId feature in features.OrderBy(f => f.Value, StringComparer.Ordinal))
            {
                if (!claimed.Add(feature)) throw new JsonException($"More than one interior provider claims {feature.Value}.");
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
