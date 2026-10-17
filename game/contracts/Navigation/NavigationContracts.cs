using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 导航图（照 WLD-INTERIORS，WP-44 烘焙，随启动集发；室内部分由开放提供者的 Nav() 给出；命名空间 Campus.Contracts）
public readonly record struct NavNodeId(uint Value);
public readonly record struct NavSpace(FeatureId? Building, short Floor);   // 室外 Building 为 null；存档记观者所在的空间（SYS-SAVE）
public enum NavEdgeKind : byte
{
    Road, Sidewalk, Footway, Path, Plaza, Steps, Ramp, Bridge, Boardwalk, Wade,
    Corridor, Room, Stair, Elevator, Connector, Door, Gate, Jump, DesignPath
}
[Flags]
public enum NavFlags : ushort
{
    None = 0, Main = 1, Covered = 2, Lit = 4, Indoor = 8, OneWay = 16, Locked = 32,
    Accessible = 64, Scenic = 128, NpcOnly = 256, PlayerOnly = 512
}
public enum DoorKind : byte { Swing, Sliding, Curtain, Turnstile, Gate, OpenArch }
public enum DoorState : byte { Open, AutoOpen, Locked }      // AutoOpen：会动的门（DoorLeafDecl）
public sealed record NavNode(NavNodeId Id, Vector3 P, NavSpace Space, string? Tag = null);
public sealed record NavEdge(NavNodeId A, NavNodeId B, NavEdgeKind Kind, float Length, float Width,
                             float Rise, NavFlags Flags, string Source);
public sealed record NavPortal(string Id, FeatureId Building, NavNodeId Outside, NavNodeId Inside,
                               Vector3 DoorCenter, float YawDeg, float ClearWidth, DoorKind Kind, DoorState State);
public sealed record NavFloorGraph(FeatureId Building, short Floor, IReadOnlyList<NavNode> Nodes, IReadOnlyList<NavEdge> Edges)
{ public static NavFloorGraph Empty(FeatureId b, int f) => new(b, (short)f, [], []); }
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
