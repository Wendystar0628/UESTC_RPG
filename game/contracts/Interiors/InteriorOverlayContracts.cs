using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

public sealed record InteriorOverlayFile(ZoneId Zone, WpId Owner, IReadOnlyList<InteriorOverlayOp> Ops)
{
    public MapId? Map { get; init; }      // 只用于自检；归属以文件所在目录为准
}
[JsonPolymorphic(TypeDiscriminatorPropertyName = "op")]
[JsonDerivedType(typeof(SealOp), "seal")]
[JsonDerivedType(typeof(InteriorConfigOp), "config")]
public abstract record InteriorOverlayOp;
public enum SealKind : byte { IronGate, FireDoor, GlassPartition, FoldingGate, RollerShutter, ShelfWall }
// 收口物：锁着的铁栅门、关着的防火门、玻璃隔断、伸缩门、卷帘门、书架墙端板（WLD-INTERIORS §2.0）
public sealed record SealOp(FeatureId Building, int Storey, Vector2 A, Vector2 B, SealKind Kind, string Reason) : InteriorOverlayOp;
// 封口：楼层、位置（平面上的一段）、收口物类型。碰撞记 WallCollider、SurfaceTag.DoorClosed，顶 ≥ 1.5 m（WLD-INTERIORS §10）；
// 平面记 PortalKind.Sealed；SYS-WALK §6 可行走覆盖层的 seal 区域引用同一份封口
public sealed record InteriorConfigOp(FeatureId Building, string Profile, JsonElement Config, string Reason) : InteriorOverlayOp;
// 配置型楼给 GenericInteriorProvider 的楼型配置：楼型、各层房间配比、开门比例、主痕迹的风格倾向（WLD-INTERIORS §14）
