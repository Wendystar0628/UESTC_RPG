using System.Numerics;

namespace Campus.Contracts;
public readonly record struct Flow(int Rate);   // 世界秒 / 真实秒；取值见 FlowRates，0 = 暂停
public enum FlowPriority : byte { Pause, Cinematic }
public interface IFlowLease : IDisposable
{
    // Updates only this lease; either command cancels its preceding destination.
    void SetRate(int rate);
    // Advance to the next occurrence, then hold this lease at zero until changed or disposed.
    void FlowTo(double clock, int rate);
}
[Flags] public enum WorldTransition : byte { None = 0, Time = 1, Season = 2, Weather = 4 }   // 几种过渡可以同时进行
public enum WeatherMode : byte { Auto, Manual }
public readonly record struct WeatherSel(WeatherMode Mode, WeatherKind Kind, float Intensity);
public readonly record struct WeatherSpan(double FromHours, double ToHours, WeatherKind Kind, float Intensity);   // 按 WorldHours 计

public interface IWorldState                // Kernel 的服务（WP-34），不单独做 Autoload
{
    long Rev { get; }
    long Instance { get; }                  // 世界实例号。启动后第一次得到世界状态为 1，此后每次 Load、LoadFile、NewGame 加 1；
                                            // 切图不变（SYS-SAVE、SYS-MAPS）。做平滑、带回差或保留历史的系统，看到它变了就直接落到新值
    double WorldHours { get; }              // 世界时间（h）：同一世界实例内单调递增，只增不减；暂停时不动
    double Clock { get; }                   // 美术钟点 0–24 h = (WorldHours + offset) mod 24（offset 见 §3"世界时间与天气的语义"）
    double PresetClock(ClockPreset preset, Season season);   // 只读查询该季美术预设钟点，不修改世界；晨=日出+40 min，午=日出日落中点，昏=日落-35 min，夜=21:00（SYS-TIME）
    double SeasonPos { get; }               // 旧网页版的 season：0–4，连续，循环；0 春 1 夏 2 秋 3 冬
    Season SeasonTarget { get; }
    Body Sun { get; }
    /// <summary>Queries the art-directed solar altitude without advancing or modifying the world.</summary>
    float SunAltDegAt(double clock, Season season) => throw new NotSupportedException("Solar forecast is not implemented by this world-state provider.");
    Body Moon { get; }
    float MoonPhase { get; }                // 默认 1 = 满月
    float Omega { get; }                    // 主光角速度 °/s；WP-03 据此决定量化转灯还是连续转动（ENG-PERF）
    Flow Flow { get; }
    Flow BaseFlow { get; }                  // 用户基础倍率；临时租约不改它，存档保存它
    int LastFlowRate { get; }               // 用户最近一次非零基础倍率，供暂停后恢复
    bool Scrubbing { get; }                 // 正在拖动时间环（UI-VISUAL）
    WorldTransition Transition { get; }     // 位标志：快进、换季转场、换天气的过渡
    WeatherSel Weather { get; }             // 生效的天气选择（换季替换之后的值）
    WeatherVec WeatherNow { get; }          // 本帧的天气向量
    IReadOnlyList<WeatherSpan> WeatherAhead { get; }   // 前瞻 24 h 的天气队列，供快进和预报行（UI-NAV、UI-VISUAL）
    uint WeatherSeed { get; }               // 本存档的天气基础种子（SYS-WEATHER；每个存档一个，SYS-SAVE）
    bool IsNative(WeatherKind k);           // 该天气在目标季节里有没有；界面据此把该格画成禁用态（SYS-WEATHER、UI-VISUAL）
    double AnimTime { get; }                // 实时秒，周期见 WorldTime.AnimLoopSeconds；用 delta 累加，与 Clock 无关
    double Step12 { get; }                  // floor(AnimTime·StepRate)/StepRate，"一拍二"；StepRate 与全局参数 step_rate 同源
                                            // （QualityView.StepRate：规则见 WorldTime.StepRate）
}

public enum ClockSetMode { Drag, Jump }
public interface IWorldControl              // 没有任何让世界时间倒退的方法；读档由 WP-34 自己的 world 存档段恢复，不经过 IWorldControl（SYS-SAVE）
{
    void SetClock(double hours, ClockSetMode mode = ClockSetMode.Jump);   // 跳到"下一次出现"的 hours（见 §3"世界时间与天气的语义"）
    void Advance(double hours);                                // 前推 hours 小时（> 0，否则抛 ArgumentOutOfRangeException）：句点键 +1 h、脚本"前推 24 小时"
    void SetSeason(Season s);                                  // 季节任选，一律正向转场（SYS-TIME）
    void SetFlow(int rate);                                    // 只改用户基础倍率；rate 经 FlowRates 校验
    // Highest priority wins; within one priority the last acquired lease wins. Release restores the next owner or BaseFlow.
    IFlowLease AcquireFlow(FlowPriority priority, int rate);
    void SetWeather(WeatherKind kind, float? intensity = null);   // 进入手动；不合季节的格不生效（界面先用 IsNative 判断并提示）
    void SetWeatherIntensity(float intensity);
    void SetWeatherAuto();
    void Apply(WorldPreset p);                                 // 应用一组状态，例如点位推荐（见 §3"世界时间与天气的语义"）
    void ResetDefault();                                       // "回到本图默认状态"：清水河上就是"回到落金初灯"（见 §3"世界时间与天气的语义"、SYS-MAPS）
    void Freeze(double clock, Season season, double anim);     // 只给测试构建（--clock、--season、--anim）：启动时设初值，WorldHours 从 clock 起算
}

/// <summary>The single settled endpoint shared by every section in one Save-frame capture.</summary>
public readonly record struct WorldSaveMoment(double WorldHours, double ClockOffset, Season Season, int FlowRate);

/// <summary>Commits the restored world once all World-phase save sections have succeeded.</summary>
public interface IWorldRestore
{
    // Get once per capture on the main Save frame, after all control requests for that frame.
    // WorldHours/ClockOffset/Season are the settled target; FlowRate is BaseFlow, never a temporary lease rate.
    // No time advancement, weather Step, random draws, or live-state mutation is permitted.
    WorldSaveMoment GetSaveMoment() => throw new NotSupportedException("This world provider does not support a shared save moment.");

    // Fault latch, not rollback: stop world progression and reject controls/capture while faulted.
    // Abort must run synchronously in the same Save frame as the failing restore callback.
    // It never increments Instance. Only a fresh valid Restore + CompleteRestore clears the latch.
    void AbortRestore() => throw new NotSupportedException("This world provider does not support restore fault isolation.");
    bool RestoreFaulted => false;

    // WP-50 在 FramePriority.Save 内、maps/world/weather/各图天气段全部恢复成功后调用一次，
    // 然后才执行地图 Travel 及 Content/Pose 阶段；新游戏同序，CompleteResume 不调用。
    // world.Restore 只恢复本段并清瞬态，不增加 Instance，也不调用天气 Reset/Quiet/Step。
    // 此处增加 Instance/Rev 并落定日月、季节读数；天气由自己的 Restore 立即求值，不再推进。
    // World 阶段失败不调用；此提交之后的阶段失败不回滚到旧实例。
    void CompleteRestore();
}

// 天气过程：WP-45 实现，注册为 Kernel 服务；只有 WorldState（WP-34）和 WP-45a 自己的存档段调用它
// （Reset 由新游戏调用，见 WeatherBoot），其他代码一律读 IWorldState 和天气类全局参数。有了这一层，WP-34 和 WP-45 可以并行开发
public readonly record struct WeatherTick(
    double WorldHours,
    double TargetHours,                      // 大于 WorldHours 表示正在快进（跳转、拨时间环、Advance、FlowTo、ResetDefault、Apply）
    double ClockOffset, Season Season, float SunAltDeg, int FlowRate, double RealDt);
public sealed record WeatherProcessState(    // 全局天气设定的落定快照；自动天气由种子、地图与世界时间求值，不存生成状态。
    MapId Map, WeatherSel Selection,
    double? PinFromHours, double? PinToHours, WeatherKind? PinKind, float? PinIntensity,   // 钉住窗口、
    double QuietUntilHours, float Wet, float Puddle,                // 保持晴的期限、湿润与积水余量、
    double LeftAtHours)                      // 快照时刻；湿润、积水与新雪余量的衰减起点。
{
    public Season Season { get; init; } = Season.Autumn;
    public double ClockOffset { get; init; }
    public float SnowAdd { get; init; }                // 保存时的新雪余量，与 Wet/Puddle 同属落定状态。
    public float? WetOverride { get; init; }           // --wet 的固定读数覆盖；自然推进保留，人工 Select/Reseason/Quiet/Pin 清除，无衰减计时锚点。
    // 不存可重建的 WeatherNow；自然天气直接按世界时间求值。
    // 手动、换季、显式快进的视觉过渡 Capture 取终点，Restore 直接落定，不重播过渡。
}
public interface IWeatherProcess
{
    /// <summary>Parses and validates new-world command-line choices before any world state changes.</summary>
    WeatherBoot ParseBootOptions(IReadOnlyDictionary<string, string> arguments, uint seed, Season season);
    /// <summary>Maps a map's default weather to a kind available in the requested season.</summary>
    WeatherKind MapToSeason(WeatherKind kind, Season season);
    void Reset(WeatherBoot b);               // 只在新游戏时调用
    void Step(in WeatherTick t);             // 每帧在钟点、季节推进之后调用：求本帧向量和未来 24 h 预报
    WeatherSel Selection { get; }
    WeatherVec Now { get; }
    IReadOnlyList<WeatherSpan> Ahead { get; }
    uint Seed { get; }
    bool Transitioning { get; }              // 换天气的过渡进行中（WorldTransition.Weather）
    bool IsNative(WeatherKind k, Season s);
    void Select(WeatherSel sel);             // SetWeather、SetWeatherIntensity、SetWeatherAuto 转发到这里
    void Reseason(double worldHours, Season to, double clockOffset);   // 换季：映射手动与钉住的选择，后续按目标季节求值（SYS-WEATHER）
    void Quiet(double worldHours, double untilHours);                  // 保持晴到 untilHours：新游戏与 ResetDefault（本图 quietUntilDusk 为真时）
    void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours);   // 钉住：Apply
    void Enter(MapId map, string profile, double worldHours);          // 绑定当前图的天气求值器，不补算、不保存按图过程。
    WeatherProcessState Capture();                                  // 存档：给出落定值，过渡中取终点；在 FramePriority.Save 调用。
    // Pure settled snapshot for the shared world endpoint. Work on a copy:
    // never Step/Advance/Reseason the live process, consume its random stream, or change Now/Ahead.
    WeatherProcessState CaptureAt(in WorldSaveMoment moment) =>
        throw new NotSupportedException("This weather provider does not support a specified save moment.");
    void Restore(WeatherProcessState s);                              // 读档：在遮挡下直接落到存档的状态，不做过渡、不调 Reset
}

// 帧顺序用 Node.ProcessPriority（值越小越先执行，与节点在树里的位置无关；对应旧网页版的 FramePhase / onPhase）
// 共 18 步。任何两步都不得同值（同值时 Godot 按场景树顺序执行，就说不清先后，SYS-SAVE）。
// Stream（−500）只做选级与请求，StreamMount（900）做挂载与碰撞入树。
// ENG-PERF 的帧内顺序、SYS-TIME 的每帧顺序表、SYS-MAPS 和 SYS-AUDIO 的表与本表逐行一致（同样的 18 个值、同样的工作包）
public static class FramePriority
{
    public const int Map = -1300;           // WP-46：推进切图状态机；在雾幕全黑的帧里分发 IMapAware；Mount 时写 map_rect；
                                            // Current、Context 在这一步换，同一帧里所有系统看到同一张图（SYS-MAPS）
    public const int Quality = -1200;       // WP-32：即时类设置与自动调节生效、分发 IQualityAware、写全局参数 quality；
                                            // 先于相机，同一帧里所有系统看到同一版本的 QualityView（SYS-GRAPHICS）
    public const int Camera = -1100;        // WP-06（含可达空间求值、写 edge_fog）、WP-36、WP-42；WP-49a 的 CinematicPlayer（SYS-TITLE）
    public const int WorldState = -1000;    // WP-34：推进世界时间、钟点和季节（含 FlowTo 的停点），再调 IWeatherProcess.Step 求天气向量（WP-45）
    public const int KeyLight = -900;       // WP-03：含按阴度改投影（SYS-WEATHER）、室内抬高阴影方向的最低高度（ENG-PERF）
    public const int Palette = -800;        // WP-37、WP-04：含天气修正量
    public const int Channels = -700;       // WP-03 通道求值、WP-39 时刻表：按有效太阳高度 h_eff 求值（SYS-WEATHER）
    public const int Motion = -650;         // WP-26 风场（含天气系数）、一拍二、偶发事件、钟面指针与钟室钟锤（SYS-MOTION）；WP-45 降水数量与闪电
    public const int SeasonFx = -640;       // WP-38 季节氛围
    public const int Medium = -600;         // WP-07：介质判定，写介质全局参数与 interior_light，更新 IMediumState；室内运行时（按层显示、软停信号，WLD-INTERIORS）；
                                            // 自动门的状态机：读本帧行走物理之后的观者位置，推进推开与闭门器，阶段变化时发 IInteriorDoors.Changed（WLD-INTERIORS）
    public const int Acoustics = -590;      // WP-53c：本帧听者处的声学与路由，含门户开度（自动门读 IInteriorDoors，SYS-AUDIO）
    public const int Stream = -500;         // WP-02 流式选级与请求、WP-43 碰撞格请求；切图期间（Unload 到 Mount）不发新请求
    public const int Systems = 0;           // 其余系统（含 WP-48c 的 IAtlasService：10 Hz 的区域查询与追踪到达检查）
    public const int Animate = 20;         // WP-54a: animation/IK after Systems; never feeds back into physics or the earlier camera.
    public const int Audio = 50;            // WP-53a、53b、53d：仲裁、权重、排程、写播放器音量（SYS-AUDIO）
    public const int Hud = 100;             // WP-35：HudModel、MotionHub、UiThemeClock（写 ui_night）；WP-48b 的小地图与大地图
    public const int StreamMount = 900;     // WP-02 挂载（每帧 ≤ 1.0 ms）、WP-43 碰撞入树（预算内分帧）；新挂上的室内房间与门户下一帧生效
    public const int Save = 1000;           // WP-50：每帧最后一步，拍快照；读档时在这一步恢复，下一帧所有系统同时看到新世界（SYS-SAVE）
}

public readonly record struct WindSample(float Gust, float Phase);
public interface IWindField                 // WP-26
{
    /// <summary>Normalized horizontal direction of air travel in world X/Z coordinates; zero means calm.</summary>
    Vector2 Direction => Vector2.Zero;
    WindSample Sample(float x, float z, double animTime);   // CPU 侧：植被近池重建、粒子槽位
}
