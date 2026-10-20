using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;
public sealed record NavQuery(NavFlags Avoid = NavFlags.Locked | NavFlags.NpcOnly, NavFlags Prefer = NavFlags.None,
                              bool AllowWade = false, bool AllowElevator = true, bool AllowJump = false);
public sealed record NavPath(IReadOnlyList<NavNodeId> Nodes, float Length, float Seconds);
public interface INavGraph                  // 小地图的路线预览（SYS-ATLAS）、G19、以后的 RPG；不用 NavigationServer3D 和 AStar3D 作主图
{
    NavNode Node(NavNodeId id);
    IReadOnlyList<NavEdge> Edges(NavNodeId id);
    NavNodeId? Nearest(Vector3 p, NavSpace? space = null, float maxDist = 20f);
    NavPath? FindPath(Vector3 from, Vector3 to, NavQuery q);         // A*，可在线程池里跑
    IReadOnlyList<NavPortal> PortalsNear(Vector3 p, float r);
}
