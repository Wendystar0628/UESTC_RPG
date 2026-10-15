using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 观察模式与碰撞（照 SYS-WALK §13 冻结，只增不改）
public enum WalkClass { Walk, Interior, Wade, Building, Water, Forbidden, Outside, Steep }
public enum SurfaceTag { Ground, Stair, Barrier, Wall, Prop, Water, Invisible, Boundary, DoorClosed }
// Invisible：可行走遮罩边界的隐形墙（SYS-WALK §6）
// Boundary（WLD-OUTSKIRTS §7）：校界栏杆、关闭的校门与闸机、车道抬杆、河渠格栅、校门处的隐形墙；离它 0.6 m 内缓停，顶住 0.8 s 提示"校外暂未开放"
// DoorClosed（WLD-INTERIORS §4）：关着的门所在的楼体墙段，以及开放楼里锁着的房门、封口的防火门；顶住 0.8 s 提示"此处暂不开放"
// 自动门（DoorLeafDecl）关好时的运行时门体用 Wall，Foot 按门扇材质（WoodDoor、Metal、Glass），WLD-INTERIORS §4.1

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(WallCollider), "wall")]
[JsonDerivedType(typeof(RampCollider), "ramp")]
[JsonDerivedType(typeof(PostCollider), "post")]
[JsonDerivedType(typeof(BoxCollider), "box")]
[JsonDerivedType(typeof(SlabCollider), "slab")]
[JsonDerivedType(typeof(HeightFieldCollider), "heightField")]
[JsonDerivedType(typeof(SlopedWallCollider), "slopedWall")]
public abstract record Collider(SurfaceTag Tag)
{
    public FootMat Foot { get; init; } = FootMat.Auto;    // 脚步与接触声的材质（SYS-AUDIO §9.3）；Auto 由表面类型和地面栅格推断
}
public sealed record WallCollider(Vector2 A, Vector2 B, float HalfT, float Y0, float Y1, SurfaceTag Tag) : Collider(Tag);
public sealed record RampCollider(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float Thick, SurfaceTag Tag) : Collider(Tag)  // 台阶坡面、桥面
{
    public StairSpec? Stair { get; init; }               // 楼梯的踏步规格，供脚步声（SYS-AUDIO §9.3）
}
public sealed record PostCollider(Vector2 C, float R, float Y0, float Y1, SurfaceTag Tag) : Collider(Tag);     // 树干、灯杆
public sealed record BoxCollider(Matrix4x4 At, Vector3 Half, SurfaceTag Tag) : Collider(Tag);                // 长椅、花池
public sealed record SlabCollider(IReadOnlyList<Vector2> Ring, float Y, float Thick, SurfaceTag Tag) : Collider(Tag);  // 室内楼板
public sealed record HeightFieldCollider(TileKey Tile, short[] HeightsCm, SurfaceTag Tag) : Collider(Tag)      // 地面：65×65，1 m 间距（WP-15）
{
    public byte[]? FootMatHalfMeter { get; init; }        // 128×128 的 FootMat 栅格（SYS-AUDIO §9.3）
    public IReadOnlyList<FootPatch>? Patches { get; init; }   // 井盖、箅子、排水沟这类点状特征
}
public sealed record SlopedWallCollider(Vector2 A, Vector2 B, float HalfT,
    float Y0a, float Y1a, float Y0b, float Y1b, SurfaceTag Tag) : Collider(Tag);
// WLD-INTERIORS §10：斜顶薄墙。A 端底 Y0a、顶 Y1a，B 端底 Y0b、顶 Y1b；宽梯井（> 0.2 m）的栏杆，顶面取踏步前缘线以上 1.5 m
// 图元到 Godot 形状的映射归 WP-43（SYS-WALK §13）：Wall → 薄 BoxShape3D（楼体顶取地面以上约 300 m），
// Ramp → 斜放的 BoxShape3D 或 8 顶点 ConvexPolygonShape3D，Post → CylinderShape3D，Box → BoxShape3D，
// Slab → 凸分解，HeightField → HeightMapShape3D 65×65，SlopedWall → 8 顶点 ConvexPolygonShape3D
// 楼梯：斜面 26–34°、上限 40°，坡顶与上平台齐平、坡底埋进下层地面 ≥ 0.2 m，SYS-WALK §12 的登阶层必须开；梯井 ≤ 0.2 m 时整面做墙（WLD-INTERIORS §10）

public readonly record struct SweepHit(Vector3 Point, Vector3 Normal, float T, SurfaceTag Tag);
public readonly record struct SupportHit(float Y, SurfaceTag Tag);
public interface ICollisionWorld             // WP-43，Godot 侧实现
{
    bool Ready(Vector3 p, float r);          // ENG-PERF §5 第 5 条用它判断
    SweepHit? Sweep(Vector3 from, Vector3 to, float r, float h);
    SupportHit? SupportY(float x, float z, float yHint);
}

public enum ModeSwitchVia { Key, Ui, DoubleSpace }        // 没有触屏输入
public enum ModeSwitchResult { Ok, Relocated, Refused }
public interface IViewControl
{
    ViewMode Mode { get; }
    Task<ModeSwitchResult> SetMode(ViewMode m, ModeSwitchVia via);
}
// IContentModule.Collide、IPartDef.Collision 两个默认方法和 IInteriorProvider.CollisionStorey 分别在 §3 的内容模块块、部件块、室内块里，照 SYS-WALK §13
