using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Parts.Stubs;

/// <summary>D0 only: one rectangular plan, an open entry and straight stairs per registered building.</summary>
public abstract class BoxInteriorProviderStub : IInteriorProvider, IInteriorCatalogConfigurable
{
    private FeatureId[] _features = [];
    private int[] _floors = [1];
    private IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> _entrances =
        new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>();
    public IReadOnlyList<FeatureId> FeatureIds => _features;
    public int Priority => -1000;
    public bool Walkable => true;

    /// <summary>Boot supplies the authoritative interiors.json fields; generators perform no file IO.</summary>
    public void Configure(IEnumerable<FeatureId> features, IEnumerable<int> openFloors,
        IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> entrances)
    {
        _features = features.Distinct().OrderBy(x => x.Value, StringComparer.Ordinal).ToArray();
        _floors = openFloors.Distinct().Order().ToArray();
        if (_features.Length == 0 || _floors.Length == 0) throw new ArgumentException("Interior stub requires features and open floors.");
        if (_floors.Any(f => f < short.MinValue || f > short.MaxValue)) throw new ArgumentOutOfRangeException(nameof(openFloors));
        _entrances = entrances;
    }

    public InteriorLayout Layout(BuildContext ctx, FeatureId id)
    {
        var plan = Plan(ctx, id);
        var storeys = _floors.Select(f => new StoreyInfo(f, plan.Y(f), 4, plan.Ring(-plan.Width / 2, 0, plan.Width / 2, plan.Depth))).ToArray();
        var zones = _floors.Select(f => new ZoneVolume(f, f, ZoneKind.Lobby, storeys.First(s => s.Index == f).Footprint,
            new ChannelId("interior.main"), StableHash.Hash32(ctx.WorldSeed, id.Value, "interior.light"), true)).ToArray();
        float halfDoor = Math.Clamp(plan.Entrances[0].ClearWidth / 2, 0.55f, plan.Width / 4);
        var portals = new List<PortalDecl> { new(_floors[0], -1, _floors[0],
            plan.P(-halfDoor, plan.Y(_floors[0]), 0), plan.P(halfDoor, plan.Y(_floors[0]), 0), 2.8f, PortalKind.Door) };
        for (int i = 1; i < _floors.Length; i++)
            portals.Add(new(_floors[i], _floors[i - 1], _floors[i], plan.P(plan.Width / 2 - 4, plan.Y(_floors[i]), 11.5f),
                plan.P(plan.Width / 2 - 1, plan.Y(_floors[i]), 11.5f), 3, PortalKind.StairOpening));
        var corners = storeys[0].Footprint;
        Vector3 min = new(corners.Min(p => p.X), storeys[0].FloorY - 0.25f, corners.Min(p => p.Y));
        Vector3 max = new(corners.Max(p => p.X), storeys[^1].FloorY + 4, corners.Max(p => p.Y));
        return new(id, storeys, zones, portals, plan.Entrances, [], _floors, [], new(min, max),
            StableHash.Hash32(ctx.WorldSeed, id.Value, "d0.interior.plan.v1"));
    }

    public IReadOnlyList<Collider> CollisionStorey(BuildContext ctx, FeatureId id, int storey)
    {
        var p = Plan(ctx, id);
        if (Array.IndexOf(_floors, storey) < 0) return [];
        var result = new List<Collider>();
        float y = p.Y(storey), left = -p.Width / 2, right = p.Width / 2;
        float stairLeft = right - 4, stairRight = right - 1;
        bool incoming = Array.IndexOf(_floors, storey) > 0;
        void Slab(float x0, float z0, float x1, float z1) => result.Add(new SlabCollider(p.Ring(x0, z0, x1, z1), y, 0.25f, SurfaceTag.Ground) { Foot = FootMat.Concrete });
        if (incoming)
        {
            Slab(left, 0, stairLeft, p.Depth);
            Slab(stairRight, 0, right, p.Depth);
            Slab(stairLeft, 0, stairRight, 3);
            Slab(stairLeft, 11.5f, stairRight, p.Depth);
        }
        else Slab(left, 0, right, p.Depth);
        void Wall(float x0, float z0, float x1, float z1, float bottom, float top) =>
            result.Add(new WallCollider(p.Xz(x0, z0), p.Xz(x1, z1), 0.12f, bottom, top, SurfaceTag.Wall) { Foot = FootMat.Concrete });
        Wall(left, 0, left, p.Depth, y, y + 4);
        Wall(right, p.Depth, right, 0, y, y + 4);
        Wall(left, p.Depth, right, p.Depth, y, y + 4);
        float halfDoor = Math.Clamp(p.Entrances[0].ClearWidth / 2, 0.55f, p.Width / 4);
        if (storey == _floors[0])
        {
            Wall(left, 0, -halfDoor, 0, y, y + 4);
            Wall(halfDoor, 0, right, 0, y, y + 4);
            Wall(-halfDoor, 0, halfDoor, 0, y + 2.8f, y + 4);
        }
        else
        {
            Wall(left, 0, right, 0, y, y + 4);
            Wall(stairLeft, 3, stairLeft, 11.5f, y, y + 1.5f);
            Wall(stairRight, 3, stairRight, 11.5f, y, y + 1.5f);
        }
        int next = Array.IndexOf(_floors, storey) + 1;
        if (next < _floors.Length)
        {
            float top = p.Y(_floors[next]);
            const float run = 8.5f;
            if (top - y > 5) throw new InvalidOperationException("D0 straight stair requires consecutive open storeys.");
            result.Add(new RampCollider(p.P(stairLeft, y - 0.2f, 3), p.P(stairRight, y - 0.2f, 3),
                p.P(stairRight, top, 3 + run), p.P(stairLeft, top, 3 + run), 0.2f, SurfaceTag.Stair)
            { Foot = FootMat.Concrete, Stair = new((top - y) / 24, run / 24, 24) });
        }
        if (next == _floors.Length)
            result.Add(new SlabCollider(p.Ring(left, 0, right, p.Depth), y + 4, 0.25f, SurfaceTag.Ground) { Foot = FootMat.Concrete });
        return result;
    }

    public ContentOutput BuildStorey(BuildContext ctx, FeatureId id, int storey, InteriorDetail detail)
    {
        _ = detail switch
        {
            InteriorDetail.Proxy or InteriorDetail.Shell or InteriorDetail.Full => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(detail))
        };
        var builder = new StubSpatialMeshBuilder();
        foreach (Collider collider in CollisionStorey(ctx, id, storey))
        {
            if (collider is RampCollider ramp) builder.AddSteps(ramp);
            else builder.Add(collider);
        }
        return new([builder.Build(ctx.Toon(new(new("wall.lit"), new("wall.shade"))))], []);
    }

    /// <summary>0.5 m plan occupancy; excludes the upper-floor stair well.</summary>
    public InteriorPlanGridStub PlanGrid(BuildContext ctx, FeatureId id, int storey)
    {
        var p = Plan(ctx, id);
        int width = (int)(p.Width * 2), depth = (int)(p.Depth * 2);
        var cells = new bool[width * depth];
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                float px = -p.Width / 2 + (x + 0.5f) * 0.5f, pz = (z + 0.5f) * 0.5f;
                cells[z * width + x] = Array.IndexOf(_floors, storey) >= 0 &&
                    !(storey != _floors[0] && px > p.Width / 2 - 4 && px < p.Width / 2 - 1 && pz > 3 && pz < 11.5f);
            }
        return new(p.P(-p.Width / 2, p.Y(storey), 0), p.Right, p.Inward, width, depth, 0.5f, cells);
    }

    public NavFloorGraph Nav(BuildContext ctx, FeatureId id, int storey)
    {
        var p = Plan(ctx, id);
        int index = Array.IndexOf(_floors, storey);
        if (index < 0) return NavFloorGraph.Empty(id, storey);
        float y = p.Y(storey), stairX = p.Width / 2 - 2.5f, sideX = p.Width / 2 - 5;
        NavNodeId NodeId(int floor, string role) => new(StableHash.Hash32(ctx.WorldSeed, id.Value, floor.ToString(System.Globalization.CultureInfo.InvariantCulture), role));
        NavNode Node(string role, float x, float z) => new(NodeId(storey, role), p.P(x, y, z), new(id, (short)storey), role);
        var nodes = new List<NavNode> { Node("entry", 0, 2), Node("center", 0, p.Depth / 2),
            Node("lower-approach", sideX, 2), Node("lower", stairX, 3 + 8.5f * 0.2f / 4.2f),
            Node("upper-approach", sideX, 12), Node("upper", stairX, 11.5f) };
        var edges = new List<NavEdge>();
        void Connect(NavNode a, NavNode b, NavEdgeKind kind) => edges.Add(new(a.Id, b.Id, kind,
            Vector3.Distance(a.P, b.P), 1.5f, b.P.Y - a.P.Y, NavFlags.Indoor, "d0.interior"));
        Connect(nodes[0], nodes[1], NavEdgeKind.Corridor);
        Connect(nodes[0], nodes[2], NavEdgeKind.Corridor);
        Connect(nodes[2], nodes[3], NavEdgeKind.Corridor);
        Connect(nodes[1], nodes[4], NavEdgeKind.Corridor);
        Connect(nodes[4], nodes[5], NavEdgeKind.Corridor);
        if (index + 1 < _floors.Length)
        {
            int next = _floors[index + 1];
            var upper = new NavNode(NodeId(next, "upper"), p.P(stairX, p.Y(next), 11.5f), new(id, (short)next), "upper");
            nodes.Add(upper);
            Connect(nodes[3], upper, NavEdgeKind.Stair);
        }
        return new(id, (short)storey, nodes, edges);
    }

    private PlanData Plan(BuildContext ctx, FeatureId id)
    {
        if (!_features.Contains(id)) throw new ArgumentException("Feature is not assigned to this provider.", nameof(id));
        if (!ctx.World.Features.TryGetValue(id, out var feature)) throw new KeyNotFoundException(id.Value);
        float width = Math.Clamp(feature.Bounds.Max.X - feature.Bounds.Min.X, 24, 48);
        float depth = Math.Clamp(feature.Bounds.Max.Z - feature.Bounds.Min.Z, 24, 48);
        width = (int)(width * 2) / 2f; depth = (int)(depth * 2) / 2f;
        IReadOnlyList<EntranceDecl> entrances = _entrances.GetValueOrDefault(id) ?? [];
        if (entrances.Count == 0)
        {
            var center = new Vector3((feature.Bounds.Min.X + feature.Bounds.Max.X) / 2, feature.Bounds.Min.Y, feature.Bounds.Min.Z);
            entrances = [new(id.Value + "/stub-entry", center - new Vector3(0, 0, 2.5f), center + new Vector3(0, 0, 3), 2.4f, true)
                { Center = center, YawDeg = 0 }];
        }
        float angle = entrances[0].YawDeg * (float.Pi / 180f);
        return new(width, depth, entrances[0].Center, new(DetMath.Cos(angle), 0, DetMath.Sin(angle)),
            new(-DetMath.Sin(angle), 0, DetMath.Cos(angle)), _floors[0], entrances);
    }

    private sealed record PlanData(float Width, float Depth, Vector3 Origin, Vector3 Right, Vector3 Inward,
        int FirstFloor, IReadOnlyList<EntranceDecl> Entrances)
    {
        public float Y(int floor) => Origin.Y + (floor - FirstFloor) * 4;
        public Vector3 P(float x, float y, float z) => Origin + Right * x + Inward * z + Vector3.UnitY * (y - Origin.Y);
        public Vector2 Xz(float x, float z) { var p = P(x, Origin.Y, z); return new(p.X, p.Z); }
        public IReadOnlyList<Vector2> Ring(float x0, float z0, float x1, float z1) => [Xz(x0, z0), Xz(x1, z0), Xz(x1, z1), Xz(x0, z1)];
    }
}

public sealed record InteriorPlanGridStub(Vector3 Origin, Vector3 Right, Vector3 Inward,
    int Width, int Depth, float CellSize, bool[] Walkable);
