using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;
// WLD-INTERIORS：斜顶薄墙。A 端底 Y0a、顶 Y1a，B 端底 Y0b、顶 Y1b；宽梯井（> 0.2 m）的栏杆，顶面取踏步前缘线以上 1.5 m
// 图元到 Godot 形状的映射归 game/kernel/collision/（SYS-WALK）：Wall → 薄 BoxShape3D（楼体顶取地面以上约 300 m），
// Ramp → 斜放的 BoxShape3D 或 8 顶点 ConvexPolygonShape3D，Post → CylinderShape3D，Box → BoxShape3D，
// Slab → 凸分解，HeightField → HeightMapShape3D 65×65，SlopedWall → 8 顶点 ConvexPolygonShape3D
// 楼梯：斜面 26–34°、上限 40°，坡顶与上平台齐平、坡底埋进下层地面 ≥ 0.2 m，SYS-WALK 的登阶层必须开；梯井 ≤ 0.2 m 时整面做墙（WLD-INTERIORS）

public readonly record struct SweepHit(Vector3 Point, Vector3 Normal, float T, SurfaceTag Tag);
public readonly record struct SupportHit(float Y, SurfaceTag Tag);
/// <summary>Horizontal clearance from the walking cylinder to a mounted Boundary surface, with its inward unit normal.</summary>
public readonly record struct BoundaryHit(float Distance, Vector3 Inward);
public interface ICollisionWorld             // game/kernel/collision/，Godot 侧实现
{
    /// <summary>Terminal failure of the active map's collision preparation or mounting; never a readiness result.</summary>
    Exception? Failure { get; }
    bool Ready(Vector3 p, float r);          // ENG-PERF 第 5 条用它判断
    SweepHit? Sweep(Vector3 from, Vector3 to, float r, float h);
    SupportHit? SupportY(float x, float z, float yHint);
    /// <summary>Mounted support-surface ray; a hit selects a candidate, never establishes destination readiness.</summary>
    SweepHit? SupportRay(Vector3 from, Vector3 to);
    /// <summary>Mounted Boundary contacts within WalkRules.BoundarySlowdownDistance; call only after Ready.</summary>
    int NearbyBoundaries(Vector3 feet, float r, float h, Span<BoundaryHit> hits);
    /// <summary>Mounted analytical tread data near the support layer, or null when unavailable. Callers retain Ready/leg-reach checks.</summary>
    StairHit? StairAt(Vector3 p, float yHint) => null;
}

public enum ModeSwitchVia { Key, Ui, DoubleSpace }        // 没有触屏输入
public enum ModeSwitchResult { Ok, Relocated, Refused }
public interface IViewControl
{
    ViewMode Mode { get; }
    /// <summary>A completed walking entry or repair found no legal landing. Presentation owns its localized message.</summary>
    event Action? WalkEntryRefused;
    /// <summary>Walking entry completes after real destination admission; Refused leaves the prior mode and pose in place.</summary>
    Task<ModeSwitchResult> SetMode(ViewMode m, ModeSwitchVia via);
}
