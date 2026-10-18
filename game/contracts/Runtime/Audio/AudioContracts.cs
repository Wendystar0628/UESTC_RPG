using System.Numerics;
using Campus.Contracts.Settings;
using System;
using System.Collections.Generic;

namespace Campus.Contracts.Audio;

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
    string? OutputDevice = null)
{
    public static AudioSettings Default { get; } = new();
}

/// <summary>Audio settings data shared by persistence, normalization and settings controls.</summary>
public static class AudioSettingDefinitions
{
    public const string Section = "audio";
    public static readonly SettingRange VolumeRange = new(0, 1, .05);
    public static readonly SettingDefinition<float> Master = new("master", AudioSettings.Default.Master, "sound.master", VolumeRange);
    public static readonly SettingDefinition<float> Ambience = new("ambience", AudioSettings.Default.Ambience, "sound.ambience", VolumeRange);
    public static readonly SettingDefinition<float> Foley = new("foley", AudioSettings.Default.Foley, "sound.foley", VolumeRange);
    public static readonly SettingDefinition<float> Ui = new("ui", AudioSettings.Default.Ui, "sound.ui", VolumeRange);
    public static readonly SettingDefinition<float> Music = new("music", AudioSettings.Default.Music, "sound.music", VolumeRange);
    public static readonly SettingDefinition<bool> UiSounds = new("ui_sounds", AudioSettings.Default.UiSounds, "sound.ui_sounds");
    public static readonly SettingDefinition<bool> HoverSounds = new("hover_sounds", AudioSettings.Default.HoverSounds, "sound.hover");
    public static readonly SettingDefinition<bool> QuietMode = new("quiet_mode", AudioSettings.Default.QuietMode, "sound.quiet");
    public static readonly SettingDefinition<bool> Mono = new("mono", AudioSettings.Default.Mono, "sound.mono");
    public static readonly SettingDefinition<int> Sudden = new("sudden", (int)AudioSettings.Default.Sudden, "sound.sudden",
        new((int)SuddenSoundMode.Normal, (int)SuddenSoundMode.Off, 1), Options:
    [
        new((int)SuddenSoundMode.Normal, "sound.sudden.normal"),
        new((int)SuddenSoundMode.Softer, "sound.sudden.softer"),
        new((int)SuddenSoundMode.Off, "ui.common.off")
    ]);
    public static readonly SettingDefinition<int> Background = new("background", (int)AudioSettings.Default.Background, "sound.background",
        new((int)BackgroundBehavior.Keep, (int)BackgroundBehavior.Mute, 1), Options:
    [
        new((int)BackgroundBehavior.Keep, "sound.background.keep"),
        new((int)BackgroundBehavior.Lower, "sound.background.lower"),
        new((int)BackgroundBehavior.Mute, "sound.background.mute")
    ]);
    public static readonly SettingDefinition<string?> OutputDevice = new("output_device", AudioSettings.Default.OutputDevice, "sound.output_device");
}

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
public enum PathKind : byte { Direct, Obstructed, Occluded, Portal, Transmission, None }
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
