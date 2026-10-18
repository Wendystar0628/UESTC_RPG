using System.Numerics;
using System;
using System.Collections.Generic;

namespace Campus.Contracts.Audio;

// ===== 环境声（53b） =====
public readonly record struct SoundSetId(string Value);          // 如 "amb.bird.dusk.roost"，登记在 game/data/audio/sets/*.json
public readonly record struct AmbienceProfileId(string Value);   // 室内档案，如 "interior.library.atrium"
public enum EmitterShape : byte { Point, Line, Area, Scatter }
[Flags]
public enum DayBand : ushort
{
    PreDawn = 1, Dawn = 2, Morning = 4, Midday = 8, Afternoon = 16,
    Dusk = 32, Evening = 64, Night = 128, LateNight = 256, All = 511
}
[Flags]
public enum WeatherMask : ushort
{
    Clear = 1, FewClouds = 2, Cloudy = 4, Overcast = 8, Fog = 16, LightRain = 32,
    Rain = 64, Thunderstorm = 128, AfterRain = 256, Snow = 512, AfterSnow = 1024, All = 2047
}
public enum TraceKind : byte { None, HumanTrace, VisibleAnimal }  // 要过画外规则或动物同步规则（SYS-AUDIO）

public sealed record AmbientEmitterDecl(
    string Id,                            // 以 "amb:" 开头，本图内唯一；G5 查重
    EmitterShape Shape, SoundSetId Set,
    IReadOnlyList<Vector3> Points,        // Point：1 个；Line：折线；Area：外环；Scatter：候选位点
    float RefDb, float AudibleRadius,
    SeasonMask Seasons = SeasonMask.All, DayBand Bands = DayBand.All, WeatherMask Weather = WeatherMask.All,
    TraceKind Trace = TraceKind.None, byte Priority = 4,
    float MinDistance = 0f,               // 远景声的距离下限（SYS-AUDIO）
    FeatureId? Feature = null,
    ChannelId? Activity = null,           // 人迹跟随的灯光通道（灯亮才有人迹）
    string Reason = "");                  // 手工点必须写理由

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AmbientEmitterAttribute(string id) : Attribute { public string Id { get; } = id; public string Owner { get; set; } = ""; }
public interface IAmbientEmitterSource { IEnumerable<AmbientEmitterDecl> Emit(BuildContext ctx); }   // gen 里，纯函数，在烘焙器里跑

// 部件自带的声音插座：IPartDef.Sounds() 默认方法在 §3 部件块
public sealed record SoundSocketDecl(string Id, SoundSetId Set, Matrix4x4 Local, float RefDb, float AudibleRadius,
    SeasonMask Seasons = SeasonMask.All, NightRule Night = NightRule.Any, WeatherMask Weather = WeatherMask.All, byte Priority = 2);

// ===== 空间声学（53c）：声学网格 + 房间与门户 + 射线细化 + 混响区，不上 Steam Audio =====
public enum AcousticClass : byte
{
    Open, Waterside, Street, Courtyard, Canopy, Underpass, Arcade,                      // 室外与半室外
    Corridor, Stair, SmallRoom, Room, Hall, Atrium, Gym, Pool, Canteen, Auditorium, Exhibit,  // 室内
    Underwater, Underground, FogSea,                                                    // 介质（SYS-VIEW）
}
public enum AbsorbClass : byte { Hard, Mixed, Soft, Foliage, Water, Snow }
public enum AcousticPortalKind : byte { OpenDoor, SlidingGlass, Curtain, Window, Archway, ElevatorDoor, SwingDoor }
public sealed record AcousticRoomDecl(string LocalId, AcousticClass Class, IReadOnlyList<Box3> Volumes,
    AbsorbClass Surfaces, AmbienceProfileId? Profile = null, float? Rt60 = null, float? SendDb = null);
public sealed record AcousticPortalDecl(string RoomA, string? RoomB /* null = 室外 */, Vector3 Center, Vector3 Normal,
    float Width, float Height, float Depth, AcousticPortalKind Kind, ChannelId? Openness = null /* 电梯门的开度通道 */,
    string? Door = null /* 自动门的门 ID（DoorLeafDecl.Id），开度读 IInteriorDoors.OpenOf，关好时门户馈送降到 −80 dB（SYS-AUDIO） */);
public sealed record AcousticsDecl(IReadOnlyList<AcousticRoomDecl> Rooms, IReadOnlyList<AcousticPortalDecl> Portals)
{ public static readonly AcousticsDecl Empty = new([], []); }
// IInteriorProvider.Acoustics(ctx, id) 默认方法在 §3 室内块；按层提供的室内照样实现：房间按层声明，Box3 带 y 区间

// ===== 观者声（53d）与它的输入 =====
public enum FootMat : byte
{
    Auto = 0, Asphalt, Concrete, StonePaving, BrickPaver, RubberTrack, Grass, Soil, Gravel,
    WoodDeck, MetalGrate, SteelPlate, MetalCover, RubberMat, Terrazzo, PolishedStone, Tile,
    Carpet, WoodFloor, Epoxy, SportWood, PoolDeck, Glass, Metal,
    Hedge, WoodDoor, Shutter,                              // 只作接触组
    Bark,                                                  // 树干的接触组（SYS-AUDIO），追加在末尾
}
public readonly record struct StairSpec(float RiserM, float TreadM, int Count);
public readonly record struct FootPatch(Vector2 Center, Vector2 HalfSize, float YawDeg, FootMat Mat, uint Flags);  // 井盖、箅子、排水沟
