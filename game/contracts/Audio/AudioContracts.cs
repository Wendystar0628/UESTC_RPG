using System;
using System.Collections.Generic;

namespace Campus.Contracts.Audio;

using System.Numerics;

// ===== 名字常量：G5 比对代码建出的总线树（同 ShaderGlobals 的做法） =====
public static class AudioBuses
{
    public const string Master = "Master", Music = "Music", Ui = "UI", World = "World", Out = "Out",
        AmbBed = "Amb.Bed", AmbWeather = "Amb.Weather", AmbFar = "Amb.Far", AmbTrace = "Amb.Trace", AmbEmit = "Amb.Emit",
        OutEcho = "Out.Echo", InBed = "In.Bed", InEmit = "In.Emit",
        NearDry = "Near.Dry", SlotAEr = "Slot.A.ER", SlotARev = "Slot.A.Rev", SlotBEr = "Slot.B.ER", SlotBRev = "Slot.B.Rev",
        Air = "Air";
    public const string ZonePrefix = "Zone.";       // Zone.<AcousticClass>.Rev，≤ 6 条（SYS-AUDIO）
}
public enum AudioGroup : byte { Master, Ambience, Foley, Ui, Music }      // 设置里的五组音量：总、环境、动作、界面、音乐

// ===== 设置：user://settings.json 的 audio 节，由统一设置读写方持久化 =====
public enum BackgroundBehavior : byte { Keep, Lower, Mute }
public enum SuddenSoundMode : byte { Normal, Softer, Off }                 // 雷、钟、孔雀
public sealed record AudioSettings(
    float Master = 0.8f, float Ambience = 1.0f, float Foley = 0.8f, float Ui = 0.6f, float Music = 0.7f,   // 滑杆值 0–1；脚步默认安静
    bool UiSounds = true, bool HoverSounds = false, bool QuietMode = false, bool Mono = false,
    SuddenSoundMode Sudden = SuddenSoundMode.Normal, BackgroundBehavior Background = BackgroundBehavior.Keep,
    string? OutputDevice = null);

// ===== 界面音与音乐 =====
public enum UiCue : byte
{
    Focus, Hover, Confirm, Back, Toggle, PageOpen, PageClose, SliderTick,
    TeleportConfirm, TeleportArrive, Saved, ShiftDayNight, Denied
}
public enum MusicCue : byte { Silence, Title, EnterNew, EnterContinue, ReturnTitle }   // 开场音乐做好之前全部按 Silence

// ===== 音频服务：总入口（53a，Kernel 服务） =====
public enum DuckReason : byte { Boot, Veil, PageOpen, Cinematic, MapTravel }
public readonly record struct AudioSnapshot(long Frame, int RealVoices, int Nodes3D, int VirtualEmitters,
    int OneShotsLast5s, float MasterPeakDb, int ResidentKb);
public interface IAudioService
{
    AudioSettings Settings { get; }
    event Action<AudioSettings>? SettingsChanged;            // 主线程；值变了才发
    void ApplySettings(AudioSettings next);                  // 音量 80 ms 斜坡；0.5 s 去抖写盘
    void PlayUi(UiCue cue);                                  // 当场 PlayStream；界面音效关时静默丢弃；80 ms 去重
    void SetMusic(MusicCue cue, float fadeSeconds = 1.5f);
    void Cine(string cue, float seconds, float db = 0f);     // 过场的 audio 轨：title.start / title.fadeIn / title.fadeOut / duck / amb.altitude
    void Duck(DuckReason reason, float db, float attackSeconds, float releaseSeconds);   // 多个原因取最深
    void ReleaseDuck(DuckReason reason);
    AudioSnapshot Snapshot { get; }                          // 调试面板与门禁
}

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

public readonly record struct AmbienceCue(SoundSetId Set, Vector3 Position, float DelaySec = 0f, float GainDb = 0f,
    TraceKind Trace = TraceKind.None, byte Priority = 1, uint Group = 0);   // Group：同一批（如一次偶发事件），可以整批取消
public readonly record struct AmbienceSnapshot(long Frame, int ActiveVoices, int OneShotsLast5s, float BedWeightSum,
    float Birds, float Insects, float Frogs, float HumanTrace, float Rain, float Wind, float Water, float Far);
public interface IAmbienceService
{
    AmbienceSnapshot Now { get; }
    void Cue(in AmbienceCue cue);                          // 与画面同步的一次性声：偶发事件本身无声，它引起的抖落、积雨、旗动有声（SYS-AUDIO）
    void CancelCues(uint group);                           // 偶发事件被转场打断时
    int AddDynamic(in AmbientEmitterDecl decl);            // 会动的发声器（动物），返回句柄
    void MoveDynamic(int handle, Vector3 position);
    void RemoveDynamic(int handle);
}

// ===== 空间声学（53c）：声学网格 + 房间与门户 + 射线细化 + 混响区，不上 Steam Audio =====
public enum AcousticClass : byte
{
    Open, Waterside, Street, Courtyard, Canopy, Underpass, Arcade,                      // 室外与半室外
    Corridor, Stair, SmallRoom, Room, Hall, Atrium, Gym, Pool, Canteen, Auditorium, Exhibit,  // 室内
    Underwater, Underground, FogSea,                                                    // 介质（SYS-VIEW）
}
public enum AbsorbClass : byte { Hard, Mixed, Soft, Foliage, Water, Snow }
public enum PathKind : byte { Direct, Obstructed, Occluded, Portal, Transmission, None }
public enum AcousticPortalKind : byte { OpenDoor, SlidingGlass, Curtain, Window, Archway, ElevatorDoor, SwingDoor }
// 与室内的 PortalKind（WLD-INTERIORS）区分，不同名；SwingDoor 给会动的门
public readonly record struct AcousticEnv(                // 值类型，查询不分配
    AcousticClass Class, float Indoor, float SkyOpen, float Enclosure,
    float WallDistM, float CeilingM, float MeanFreePathM, float VolumeM3,
    float Absorption, float HfRatio, float Rt60S, float ErDelayMs, float ErLevelDb, float FlutterMs,
    float NoiseFloorDb, ushort RoomId, FeatureId? Building);
public readonly record struct SendTap(int BusIndex, float GainDb);
public readonly record struct ListenerRouting(int DryBus, SendTap ErA, SendTap RevA, SendTap ErB, SendTap RevB,
    float Enclosure, long Rev);                            // 不交叉时 B 两路为 −80 dB；BusIndex 由 53a 启动时建表，不在发声时查字符串
public readonly record struct OcclusionResult(float DirectDb, float CutoffHz, float FilterDb, PathKind Path, Vector3 ApparentPos);
public readonly record struct AcousticPortal(ushort Id, ushort RoomA, ushort RoomB, Vector3 Center, Vector3 Normal,
    float Width, float Height, float Depth, AcousticPortalKind Kind, float Openness);
public interface IAcousticEnv                              // Kernel 服务，主线程，FramePriority.Acoustics 那一步
{
    AcousticEnv At(Vector3 p);                             // 网格 + 房间 + 覆盖层 + 介质 + 天气修正；不含射线细化
    AcousticEnv Listener { get; }                          // 本帧听者处：At() + 射线细化 + 平滑；每帧一次
    ListenerRouting Routing { get; }                       // 观者声每次发声取一份
    long Rev { get; }                                      // Listener 或 Routing 变化时 +1
    OcclusionResult Occlusion(Vector3 source, ushort sourceRoom);   // 相对当前听者
    ushort RoomAt(Vector3 p);                              // 0 = 室外
    AmbienceProfileId? ProfileOf(ushort room);
    ReadOnlySpan<AcousticPortal> PortalsOf(ushort room);
}
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

public readonly record struct GroundCover(float Wet, float Puddle, float Snow, float SnowFresh,
    float Leaves, float LeafWet, float Petals, float Shelter);
public interface IGroundCover { GroundCover Sample(float x, float z); }    // WP-38；与着色器同一套阈值函数，G2 比对两边

public enum FootSide : byte { Left, Right, Both }
public readonly record struct FootfallInfo(ulong Tick, FootSide Side, Vector3 Pos, float SpeedH, Vector3 Normal,
    ulong ColliderId, int ShapeIndex, bool OnStair, float Slope);
public readonly record struct ContactInfo(ulong Tick, ulong ColliderId, int ShapeIndex, Vector3 Pos, Vector3 Normal,
    float NormalSpeed, float TangentSpeed, bool Pushing, SurfaceTag Tag);
public interface IWalkTelemetry                            // WP-42；全部在 _PhysicsProcess 里发出
{
    event Action<FootfallInfo> Footfall;
    event Action<ulong, float> Jumped;                     // tick, 起跳速度
    event Action<ulong, float, Vector3> Landed;            // tick, 撞击速度, 位置（SYS-WALK 第 9 条的扩展）
    event Action<ulong, float> StepUp;                     // 高差
    event Action<ulong, float> StepDown;
    event Action<ulong, float> Skid;                       // Δv
    event Action<ContactInfo> WallContact;                 // 每个物理步里有墙接触就发
    event Action<ulong, float> WaterEnter;                 // 水深
    event Action<ulong> WaterExit;
    event Action<ulong> Sink;
    event Action<ulong, float> TurnInPlace;                // 角度
    event Action<ulong> ModeTouchdown;
    event Action<ulong> ModeTakeoff;
    event Action<ulong> Teleport;
    event Action<ulong, float> ElevatorDoor;               // 主楼电梯门的开度 0–1
    float StepPhase { get; }                               // 0–1，晃动和脚步共用
}

public enum FoleyKind : byte
{
    Footfall, Scuff, Jump, Land, StepUp, StepDown, Skid, Bump, Brush,
    WadeStep, WadeEnter, WadeExit, Sink, LeafKick,
    Curtain, LockedDoor, Gate, Railing, Hedge, BoundaryTouch,
    ElevatorDoorClose, ElevatorDoorOpen, ElevatorRun,
    Takeoff, Touchdown,
    DoorOpen, DoorClose, AutoDoorOpen, AutoDoorClose,      // DoorOpen、DoorClose 给会动的门（WLD-INTERIORS，IInteriorDoors.Changed：
                                                           // 进入 Opening 时 DoorOpen，进入 Closed 时 DoorClose，SYS-AUDIO）；Auto* 预留
}
public readonly record struct GroundSample(FootMat Mat, FootMat Mat2, float Mat2Weight, bool Stair,
    GroundCover Cover, float WaterDepth, uint LocationHash);
public readonly record struct FoleyEvent(
    FoleyKind Kind,
    Vector3 Pos,                 // 世界坐标；观者自身的事件取落脚点
    float Intensity,             // 0–1：速度、撞击速度、Δv 等归一后的力度
    GroundSample Ground,         // 地面事件用；交互事件可以为 default
    FootSide Side = FootSide.Both,
    FootMat Contact = FootMat.Auto,   // 碰撞类事件：碰到的东西的材质
    uint SourceId = 0,           // 交互对象的稳定 ID（冷却、复用）
    ulong Tick = 0);             // 物理步序号（事件日志与确定性）
public enum FoleyLoop : byte { WadeDrag, WallBrush, HedgeBrush, ElevatorCab, FlyAir }   // FlyAir：飞行风声，实时合成
public readonly record struct FoleyStats(int ActivePlayers, int Voices, int Dropped, long Events);
public interface IFoleyService                             // Kernel 服务（53d）
{
    void Play(in FoleyEvent e);                                  // 主线程，物理步或帧里都可以
    void SetLoop(FoleyLoop loop, float level01, float param01);  // 每帧调；level 为 0 时自动淡出停播
    FoleyStats Stats { get; }
    IReadOnlyList<FoleyEvent> RecentLog { get; }                 // 只在门禁构建里记录
}
// 闸机和门不出电子提示音，单车不响铃；3D 声源在播上限 32（最低配置档默认 24），不用 AudioStreamRandomizer，用自己的洗牌选择器（SYS-AUDIO）
