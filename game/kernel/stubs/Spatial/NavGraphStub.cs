using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(INavGraph), IsStub = true)]
public sealed class NavGraphStub : INavGraph
{
    private IReadOnlyDictionary<NavNodeId, NavNode> _nodes = new Dictionary<NavNodeId, NavNode>();
    private IReadOnlyDictionary<NavNodeId, NavEdge[]> _edges = new Dictionary<NavNodeId, NavEdge[]>();
    private NavNode[] _ordered = [];
    private NavPortal[] _portals = [];

    public void Configure(IEnumerable<NavNode> nodes, IEnumerable<NavEdge> edges, IEnumerable<NavPortal>? portals = null)
    {
        var unique = new Dictionary<NavNodeId, NavNode>();
        foreach (NavNode node in nodes.OrderBy(n => n.Id.Value))
        {
            if (unique.TryGetValue(node.Id, out var old) && old != node) throw new ArgumentException("Conflicting navigation node IDs.", nameof(nodes));
            unique[node.Id] = node;
        }
        var adjacency = unique.Keys.ToDictionary(id => id, _ => new List<NavEdge>());
        foreach (NavEdge edge in edges.OrderBy(e => e.A.Value).ThenBy(e => e.B.Value))
        {
            if (!unique.ContainsKey(edge.A) || !unique.ContainsKey(edge.B)) throw new ArgumentException("Edge endpoint does not exist.", nameof(edges));
            if (edge.Length < 0 || !float.IsFinite(edge.Length)) throw new ArgumentOutOfRangeException(nameof(edges));
            adjacency[edge.A].Add(edge);
            if (!edge.Flags.HasFlag(NavFlags.OneWay)) adjacency[edge.B].Add(edge);
        }
        _ordered = unique.Values.OrderBy(n => n.Id.Value).ToArray();
        _nodes = unique;
        _edges = adjacency.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        _portals = (portals ?? []).OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
    }

    public NavNode Node(NavNodeId id) => _nodes.TryGetValue(id, out var node) ? node : throw new KeyNotFoundException(id.ToString());
    public IReadOnlyList<NavEdge> Edges(NavNodeId id) => _edges.TryGetValue(id, out var edges) ? edges : [];
    public NavNodeId? Nearest(Vector3 p, NavSpace? space = null, float maxDist = 20)
    {
        if (!float.IsFinite(maxDist) || maxDist < 0) throw new ArgumentOutOfRangeException(nameof(maxDist));
        float best = maxDist * maxDist;
        NavNodeId? found = null;
        foreach (var node in _ordered)
        {
            if (space is NavSpace wanted && node.Space != wanted) continue;
            float distance = Vector3.DistanceSquared(node.P, p);
            if (distance > best || (distance == best && found.HasValue)) continue;
            found = node.Id; best = distance;
        }
        return found;
    }

    public NavPath? FindPath(Vector3 from, Vector3 to, NavQuery q)
    {
        ArgumentNullException.ThrowIfNull(q);
        NavNodeId? start = Nearest(from), end = Nearest(to);
        if (start is null || end is null) return null;
        var distances = new Dictionary<NavNodeId, float> { [start.Value] = 0 };
        var lengths = new Dictionary<NavNodeId, float> { [start.Value] = 0 };
        var previous = new Dictionary<NavNodeId, NavNodeId>();
        var frontier = new PriorityQueue<NavNodeId, (float Cost, uint Id)>();
        frontier.Enqueue(start.Value, (0, start.Value.Value));
        while (frontier.TryDequeue(out var current, out var priority))
        {
            if (priority.Cost > distances[current]) continue;
            if (current == end.Value)
            {
                var path = new List<NavNodeId> { current };
                while (previous.TryGetValue(current, out var before)) { current = before; path.Add(current); }
                path.Reverse();
                float length = lengths[end.Value];
                return new(path, length, length / 3.6f);
            }
            foreach (NavEdge edge in Edges(current))
            {
                if ((edge.Flags & q.Avoid) != 0 || !Allowed(edge.Kind, q)) continue;
                NavNodeId next = edge.A == current ? edge.B : edge.A;
                float cost = distances[current] + edge.Length * ((edge.Flags & q.Prefer) != 0 ? 0.9f : 1);
                if (distances.TryGetValue(next, out float old) && cost >= old) continue;
                distances[next] = cost; lengths[next] = lengths[current] + edge.Length; previous[next] = current;
                frontier.Enqueue(next, (cost, next.Value));
            }
        }
        return null;
    }

    public IReadOnlyList<NavPortal> PortalsNear(Vector3 p, float r)
    {
        if (!float.IsFinite(r) || r < 0) throw new ArgumentOutOfRangeException(nameof(r));
        return _portals.Where(portal => Vector3.DistanceSquared(portal.DoorCenter, p) <= r * r).ToArray();
    }

    private static bool Allowed(NavEdgeKind kind, NavQuery q) => kind switch
    {
        NavEdgeKind.Wade => q.AllowWade,
        NavEdgeKind.Elevator => q.AllowElevator,
        NavEdgeKind.Jump => q.AllowJump,
        NavEdgeKind.Road or NavEdgeKind.Sidewalk or NavEdgeKind.Footway or NavEdgeKind.Path or NavEdgeKind.Plaza or
        NavEdgeKind.Steps or NavEdgeKind.Ramp or NavEdgeKind.Bridge or NavEdgeKind.Boardwalk or NavEdgeKind.Corridor or
        NavEdgeKind.Room or NavEdgeKind.Stair or NavEdgeKind.Connector or NavEdgeKind.Door or NavEdgeKind.Gate or NavEdgeKind.DesignPath => true,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
