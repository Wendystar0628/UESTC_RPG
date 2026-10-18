using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 门的运行时（Godot 侧，WP-07 的 InteriorDirector 实现，FramePriority.Medium 那一步推进，WLD-INTERIORS）
public enum DoorPhase : byte { Closed, Opening, Open, Closing }
public readonly record struct DoorEvent(FeatureId Building, string DoorId, DoorPhase Phase, Vector3 Position, ulong Tick);
public interface IInteriorDoors             // Kernel 服务；只读
{
    DoorPhase PhaseOf(FeatureId building, string doorId);
    float OpenOf(FeatureId building, string doorId);   // 门扇开度 0–1；声学门户的开度读同一个值（AcousticPortalDecl.Door，SYS-AUDIO）
    event Action<DoorEvent>? Changed;        // 阶段变化时发：WP-53d 放开门、关门、落锁声（进入 Opening、Closed 时；停在开位和回关途中无声），
                                             // WP-43 在 Closed 时启用、离开 Closed 时停用这扇门的运行时门体（StaticBody3D，只切 CollisionLayer，不增删形状）
}
