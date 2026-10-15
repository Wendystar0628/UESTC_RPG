using System.Numerics;

namespace Campus.Contracts;

public enum Season { Spring, Summer, Autumn, Winter }
public enum ClockPreset { Morning, Noon, Dusk, Night }
public enum ViewMode { Fly, Walk }
// AltDeg：日月美术轨迹上的实际高度角；Sun 为美术太阳实际高度，不是 h_eff 或阴影钳制高度。
public readonly record struct Body(Vector3 Dir, float AltDeg, float AzDeg, float Intensity);
public readonly record struct Flow(int Rate);   // 世界秒 / 真实秒，Rate ∈ {0, 1, 10, 60, 360, 1440}，0 = 暂停；默认 1
[Flags] public enum WorldTransition : byte { None = 0, Time = 1, Season = 2, Weather = 4 }   // 几种过渡可以同时进行

// 天气（SYS-WEATHER §8）：界面的 8 格（SYS-WEATHER §1）对应下面 11 个内部状态
public enum WeatherKind : byte { Clear, FewClouds, Cloudy, Overcast, Fog, LightRain, Rain, Thunderstorm, AfterRain, Snow, AfterSnow }
public enum WeatherMode : byte { Auto, Manual }
public readonly record struct WeatherSel(WeatherMode Mode, WeatherKind Kind, float Intensity);
// Auto 时 Kind 是前向过程当前的状态；Intensity 0–1，对应界面的轻 / 中 / 重三档
public readonly record struct WeatherVec(       // 天气向量：每帧插值；所有天气表现只读它和它写出的全局参数
    float Cloud, float CloudType, float Cb,     // 云量、云型、积雨云量
    float Overcast, float Precip, float PrecipKind,   // 阴度、降水量、降水类型（0 雨 → 1 雪）
    float Wet, float Puddle,                    // 湿润、积水
    float Fog, float GroundFog,                 // 雾密度倍率（晴为 1）、贴地雾
    float Storm, float SnowAdd,                 // 风暴度、新雪
    float Rainbow, float Lightning);            // 彩虹、闪光
public readonly record struct WeatherSpan(double FromHours, double ToHours, WeatherKind Kind, float Intensity);   // 按 WorldHours 计
public sealed record WorldPreset(double Clock, Season Season, WeatherKind? Weather = null, float? Intensity = null);

public interface IWorldState                // Kernel 的服务（WP-34），不单独做 Autoload
{
    long Rev { get; }
    long Instance { get; }                  // 世界实例号。启动后第一次得到世界状态为 1，此后每次 Load、LoadFile、NewGame 加 1；
                                            // 切图不变（SYS-SAVE §5.2、SYS-MAPS §5）。做平滑、带回差或保留历史的系统，看到它变了就直接落到新值
    double WorldHours { get; }              // 世界时间（h）：同一世界实例内单调递增，只增不减；暂停时不动
    double Clock { get; }                   // 美术钟点 0–24 h = (WorldHours + offset) mod 24（offset 见 §3"世界时间与天气的语义"）
    double PresetClock(ClockPreset preset, Season season);   // 只读查询该季美术预设钟点，不修改世界；晨=日出+40 min，午=日出日落中点，昏=日落-35 min，夜=21:00（SYS-TIME §2）
    double SeasonPos { get; }               // 旧网页版的 season：0–4，连续，循环；0 春 1 夏 2 秋 3 冬
    Season SeasonTarget { get; }
    Body Sun { get; }
    Body Moon { get; }
    float MoonPhase { get; }                // 默认 1 = 满月
    float Omega { get; }                    // 主光角速度 °/s；WP-03 据此决定量化转灯还是连续转动（ENG-PERF §10.1）
    Flow Flow { get; }
    bool Scrubbing { get; }                 // 正在拖动时间环（UI-VISUAL §7.5）
    WorldTransition Transition { get; }     // 位标志：快进、换季转场、换天气的过渡
    ViewMode ViewMode { get; }
    WeatherSel Weather { get; }             // 生效的天气选择（换季替换之后的值）
    WeatherVec WeatherNow { get; }          // 本帧的天气向量
    IReadOnlyList<WeatherSpan> WeatherAhead { get; }   // 前瞻 24 h 的天气队列，供快进和预报行（UI-NAV §4.10、UI-VISUAL §7.6）
    uint WeatherSeed { get; }               // 本存档的天气基础种子（SYS-WEATHER §6.4；每个存档一个，SYS-SAVE §5.4）
    bool IsNative(WeatherKind k);           // 该天气在目标季节里有没有；界面据此把该格画成禁用态（SYS-WEATHER §1、UI-VISUAL §7.6）
    double AnimTime { get; }                // 实时秒 mod 480，用 delta 累加，与 Clock 无关；旧网页版的 t = AnimTime / 480
    double Step12 { get; }                  // floor(AnimTime·StepRate)/StepRate，"一拍二"；StepRate 与全局参数 step_rate 同源
                                            // （QualityView.StepRate：帧率上限 30 时 15，其余 12；截图与门禁一律 12）
}

public enum ClockSetMode { Drag, Jump }
public interface IWorldControl              // 没有任何让世界时间倒退的方法；读档由 WP-34 自己的 world 存档段恢复，不经过 IWorldControl（SYS-SAVE §10）
{
    void SetClock(double hours, ClockSetMode mode = ClockSetMode.Jump);   // 跳到"下一次出现"的 hours（见 §3"世界时间与天气的语义"）
    void Advance(double hours);                                // 前推 hours 小时（> 0，否则抛 ArgumentOutOfRangeException）：句点键 +1 h、脚本"前推 24 小时"
    void FlowTo(double clock, int rate);                       // SYS-TITLE §6、§7.4：以 rate 正向流逝，到下一次出现的 clock 正好停住（流逝变为 0）；
                                                               // clock 取 [0, 24)，"下一次出现"与 SetClock 同一语义；rate 必须是 Flow 的取值之一，否则抛 ArgumentOutOfRangeException；
                                                               // 停点由 WorldState 在推进那一步里夹住，不越过一帧；SetFlow 或再次 FlowTo 取消上一次未到点的 FlowTo。
                                                               // 新游戏过场用它（60×，简短 360×，关与跳过 1440×），终态与播放速度无关
    void SetSeason(Season s);                                  // 季节任选，一律正向转场（SYS-TIME §5）
    void SetFlow(int rate);                                    // rate 必须是 Flow 的取值之一，否则抛 ArgumentOutOfRangeException
    void SetWeather(WeatherKind kind, float? intensity = null);   // 进入手动；不合季节的格不生效（界面先用 IsNative 判断并提示）
    void SetWeatherIntensity(float intensity);
    void SetWeatherAuto();
    void Apply(WorldPreset p);                                 // 应用一组状态，例如点位推荐（见 §3"世界时间与天气的语义"）
    void ResetDefault();                                       // "回到本图默认状态"：清水河上就是"回到落金初灯"（见 §3"世界时间与天气的语义"、SYS-MAPS §5）
    void Freeze(double clock, Season season, double anim);     // 只给测试构建（--clock、--season、--anim）：启动时设初值，WorldHours 从 clock 起算
}

// 天气过程：WP-45 实现，注册为 Kernel 服务；只有 WorldState（WP-34）和 WP-45a 自己的存档段调用它
// （Reset 由新游戏调用，见 WeatherBoot），其他代码一律读 IWorldState 和天气类全局参数。有了这一层，WP-34 和 WP-45 可以并行开发
public readonly record struct WeatherQueueItem(WeatherKind Kind, double Minutes);
public sealed record WeatherBoot(            // Boot 解析 --weather* 后交给新游戏；新游戏在第一帧之前调 Reset（SYS-WEATHER §8、SYS-SAVE §5.5）
    uint Seed,                               // --weather-seed；没给时由 ISaveService.NewGame 取一次熵（§2 BannedApiAnalyzers 的唯一例外）。
                                             // 读档时不调 Reset，天气由 WP-45a 的 weather、map:<id>/weather 两个存档段恢复（SYS-SAVE §5.4）
    WeatherKind? Kind = null, float? Intensity = null, float? Wet = null,   // --weather（auto 为 null）、--weather-intensity、--wet
    double? RemainingMinutes = null,         // --weather-remaining
    IReadOnlyList<WeatherQueueItem>? Queue = null);   // --weather-queue
public readonly record struct WeatherTick(
    double WorldHours,
    double TargetHours,                      // 大于 WorldHours 表示正在快进（跳转、拨时间环、Advance、FlowTo、ResetDefault、Apply）
    double ClockOffset, Season Season, float SunAltDeg, int FlowRate, double RealDt);
public sealed record WeatherProcessState(    // 一张图的天气过程状态（落定值）；字段由 WP-45a 在 v1 里定，按 SYS-SAVE §3.5 只读、不含 Godot 类型，
    MapId Map, uint Epoch, int Segment,      // 至少包括：图、epoch、片段号、
    WeatherSel Selection,                    // 自动或手动的选择、
    IReadOnlyList<WeatherQueueItem> Queue, double QueueFromHours,   // 前瞻队列、
    double? PinFromHours, double? PinToHours, WeatherKind? PinKind, float? PinIntensity,   // 钉住窗口、
    double QuietUntilHours, float Wet, float Puddle,                // 保持晴的期限、湿润与积水余量、
    double LeftAtHours);                     // 离开这张图时的世界时间（切图时冻结，SYS-MAPS §5）
public interface IWeatherProcess
{
    void Reset(WeatherBoot b);               // 只在新游戏时调用
    void Step(in WeatherTick t);             // 每帧在钟点、季节推进之后调用：补前瞻队列，求本帧的天气向量
    WeatherSel Selection { get; }
    WeatherVec Now { get; }
    IReadOnlyList<WeatherSpan> Ahead { get; }
    uint Seed { get; }
    bool Transitioning { get; }              // 换天气的过渡进行中（WorldTransition.Weather）
    bool IsNative(WeatherKind k, Season s);
    void Select(WeatherSel sel);             // SetWeather、SetWeatherIntensity、SetWeatherAuto 转发到这里
    void Reseason(double worldHours, Season to, double clockOffset);   // 换季：当前相就地映射，之后的队列丢弃、前向重排（SYS-WEATHER §6.5）
    void Quiet(double worldHours, double untilHours);                  // 保持晴到 untilHours：新游戏与 ResetDefault（本图 quietUntilDusk 为真时）
    void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours);   // 钉住：Apply
    // 按图的过程与存档：
    void Enter(MapId map, string profile, double worldHours);          // 进图：恢复或新建本图的过程，补算到 worldHours；离开超过 72 h 则 epoch + 1 从现在重排（SYS-MAPS §5）
    void Leave(MapId map, double worldHours);                          // 离图：冻结在离开那一刻
    WeatherProcessState Capture(MapId map);                            // 存档：给出落定值，过渡中取终点（SYS-SAVE §4.2）；在 FramePriority.Save 那一步调用
    void Restore(MapId map, WeatherProcessState s);                    // 读档：在遮挡下直接落到存档的状态，不做过渡、不调 Reset
}

// 帧顺序用 Node.ProcessPriority（值越小越先执行，与节点在树里的位置无关；对应旧网页版的 FramePhase / onPhase）
// 共 17 步。任何两步都不得同值（同值时 Godot 按场景树顺序执行，就说不清先后，SYS-SAVE §11）。
// Stream（−500）只做选级与请求，StreamMount（900）做挂载与碰撞入树。
// ENG-PERF §5–§6 的帧内顺序、SYS-TIME §1 的每帧顺序表、SYS-MAPS §9 和 SYS-AUDIO §20 的表与本表逐行一致（同样的 17 个值、同样的工作包）
public static class FramePriority
{
    public const int Map = -1300;           // WP-46：推进切图状态机；在雾幕全黑的帧里分发 IMapAware；Mount 时写 map_rect；
                                            // Current、Context 在这一步换，同一帧里所有系统看到同一张图（SYS-MAPS §9）
    public const int Quality = -1200;       // WP-32：即时类设置与自动调节生效、分发 IQualityAware、写全局参数 quality；
                                            // 先于相机，同一帧里所有系统看到同一版本的 QualityView（SYS-GRAPHICS §8）
    public const int Camera = -1100;        // WP-06（含可达空间求值、写 edge_fog）、WP-36、WP-42；WP-49a 的 CinematicPlayer（SYS-TITLE §8.3）
    public const int WorldState = -1000;    // WP-34：推进世界时间、钟点和季节（含 FlowTo 的停点），再调 IWeatherProcess.Step 求天气向量（WP-45）
    public const int KeyLight = -900;       // WP-03：含按阴度改投影（SYS-WEATHER §7）、室内抬高阴影方向的最低高度（ENG-PERF §10.1）
    public const int Palette = -800;        // WP-37、WP-04：含天气修正量
    public const int Channels = -700;       // WP-03 通道求值、WP-39 时刻表：按有效太阳高度 h_eff 求值（SYS-WEATHER §7）
    public const int Motion = -650;         // WP-26 风场（含天气系数）、一拍二、偶发事件、钟面指针与钟室钟锤（SYS-MOTION §9.5）；WP-45 降水数量与闪电
    public const int SeasonFx = -640;       // WP-38 季节氛围
    public const int Medium = -600;         // WP-07：介质判定，写介质全局参数与 interior_light，更新 IMediumState；室内运行时（按层显示、软停信号，WLD-INTERIORS §6）；
                                            // 自动门的状态机：读本帧行走物理之后的观者位置，推进推开与闭门器，阶段变化时发 IInteriorDoors.Changed（WLD-INTERIORS §4.1）
    public const int Acoustics = -590;      // WP-53c：本帧听者处的声学与路由，含门户开度（自动门读 IInteriorDoors，SYS-AUDIO §20）
    public const int Stream = -500;         // WP-02 流式选级与请求、WP-43 碰撞格请求；切图期间（Unload 到 Mount）不发新请求
    public const int Systems = 0;           // 其余系统（含 WP-48c 的 IAtlasService：10 Hz 的区域查询与追踪到达检查）
    public const int Audio = 50;            // WP-53a、53b、53d：仲裁、权重、排程、写播放器音量（SYS-AUDIO §20）
    public const int Hud = 100;             // WP-35：HudModel、MotionHub、UiThemeClock（写 ui_night）；WP-48b 的小地图与大地图
    public const int StreamMount = 900;     // WP-02 挂载（每帧 ≤ 1.0 ms）、WP-43 碰撞入树（预算内分帧）；新挂上的室内房间与门户下一帧生效
    public const int Save = 1000;           // WP-50：每帧最后一步，拍快照；读档时在这一步恢复，下一帧所有系统同时看到新世界（SYS-SAVE §11）
}
// 行走的物理在 _PhysicsProcess 里按固定 120 Hz 子步推进（旧网页版的 'physics' 阶段，SYS-WALK §12）；观者的脚步等事件在那里当场发声，不等 Audio 那一步（SYS-AUDIO §20）。
// 自动门的开关门声在 Medium 那一步门的阶段变化时当场发出；门体碰撞的启停从下一个物理步起生效（WLD-INTERIORS §4.1）

public enum ChannelKind { Scalar, Vec2, PerInstance }
public enum ChannelRate { Continuous, Step12 }   // Step12：按 IWorldState.Step12 步进，即 floor(AnimTime·StepRate)/StepRate，StepRate 同 step_rate
public sealed record ChannelDecl(
    ChannelId Id, ChannelKind Kind, float Min, float Max, ChannelRate Rate,
    string? Source = null);                 // 由灯光时刻表或世界状态驱动，例如 "window.dorm"、"lamp.street"（SYS-TIME §6）

// 随季节、昼夜显隐的物体（雪人、校庆道旗、毕业行李箱等）：几何照常生成，由内核按掩码显隐，不重建格子
[Flags] public enum SeasonMask : byte { Spring = 1, Summer = 2, Autumn = 4, Winter = 8, All = 15 }
public enum NightRule { Any, Only, Never }
public readonly record struct VariantTag(SeasonMask Seasons = SeasonMask.All, NightRule Night = NightRule.Any);

public readonly record struct WindSample(float Gust, float Phase);
public interface IWindField                 // WP-26
{
    WindSample Sample(float x, float z, double animTime);   // CPU 侧：植被近池重建、粒子槽位
}
// 着色器侧是 res://style/shaders/motion_wind.gdshaderinc，与 Sample 同一公式；G2 比对两边的输出
