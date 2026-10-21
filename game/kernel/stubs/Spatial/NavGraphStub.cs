using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs;

/// <summary>Explicit empty navigation placeholder until the main graph is delivered (ENG-ARCH).</summary>
[KernelService(typeof(INavGraph), IsStub = true)]
public sealed class NavGraphStub : INavGraph
{
    public NavNode Node(NavNodeId id) => throw new KeyNotFoundException(id.ToString());
    public IReadOnlyList<NavEdge> Edges(NavNodeId id) => [];

    public NavNodeId? Nearest(Vector3 p, NavSpace? space = null, float maxDist = 20)
    {
        if (!float.IsFinite(maxDist) || maxDist < 0) throw new ArgumentOutOfRangeException(nameof(maxDist));
        return null;
    }

    public NavPath? FindPath(Vector3 from, Vector3 to, NavQuery q)
    {
        ArgumentNullException.ThrowIfNull(q);
        return null;
    }

    public IReadOnlyList<NavPortal> PortalsNear(Vector3 p, float r)
    {
        if (!float.IsFinite(r) || r < 0) throw new ArgumentOutOfRangeException(nameof(r));
        return [];
    }
}
