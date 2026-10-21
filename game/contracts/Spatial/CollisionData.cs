using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 观察模式与碰撞（照 SYS-WALK 冻结，只增不改）
public enum WalkClass { Walk, Interior, Wade, Building, Water, Forbidden, Outside, Steep }
public enum SurfaceTag { Ground, Stair, Barrier, Wall, Prop, Water, Invisible, Boundary, DoorClosed }
// Invisible：可行走遮罩边界的隐形墙（SYS-WALK）
// Boundary（WLD-OUTSKIRTS）：校界栏杆、关闭的校门与闸机、车道抬杆、河渠格栅、校门处的隐形墙；按 WalkRules.BoundarySlowdownDistance 缓停
// DoorClosed（WLD-INTERIORS）：关着的门所在的楼体墙段，以及开放楼里锁着的房门、封口的防火门
// 自动门（DoorLeafDecl）关好时的运行时门体用 Wall，Foot 按门扇材质（WoodDoor、Metal、Glass），WLD-INTERIORS

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
    public FootMat Foot { get; init; } = FootMat.Auto;    // 脚步与接触声的材质（SYS-AUDIO）；Auto 由表面类型和地面栅格推断
}
public sealed record WallCollider(Vector2 A, Vector2 B, float HalfT, float Y0, float Y1, SurfaceTag Tag) : Collider(Tag);
public sealed record RampCollider(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float Thick, SurfaceTag Tag) : Collider(Tag)  // 台阶坡面、桥面
{
    public StairSpec? Stair { get; init; }               // 楼梯的踏步规格，供脚步声（SYS-AUDIO）
    /// <summary>Explicit visible tread origin/frame for actual steps; ordinary ramps have no treads.</summary>
    public StairFrame? Treads { get; init; }
}
public sealed record PostCollider(Vector2 C, float R, float Y0, float Y1, SurfaceTag Tag) : Collider(Tag);     // 树干、灯杆
public sealed record BoxCollider(Matrix4x4 At, Vector3 Half, SurfaceTag Tag) : Collider(Tag);                // 长椅、花池
public sealed record SlabCollider(IReadOnlyList<Vector2> Ring, float Y, float Thick, SurfaceTag Tag) : Collider(Tag);  // 室内楼板
public sealed record HeightFieldCollider(TileKey Tile, short[] HeightsCm, SurfaceTag Tag) : Collider(Tag)      // 地面：65×65，1 m 间距（WLD-TERRAIN-APPLY）
{
    public byte[]? FootMatHalfMeter { get; init; }        // 128×128 的 FootMat 栅格（SYS-AUDIO）
    public IReadOnlyList<FootPatch>? Patches { get; init; }   // 井盖、箅子、排水沟这类点状特征
}
public sealed record SlopedWallCollider(Vector2 A, Vector2 B, float HalfT,
    float Y0a, float Y1a, float Y0b, float Y1b, SurfaceTag Tag) : Collider(Tag);
