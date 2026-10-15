namespace Campus.Contracts.Quality;

public enum QualityPreset : byte { Smooth, Balanced, Fine, Ultra, Custom }       // 流畅、均衡、精致、极致、自定义
public enum DeviceTier : byte { MinSpec, DgpuMid, DgpuHigh }                     // 最低配置（核显）、独显中、独显高（ENG-BUDGET §2，D-169）
public enum ApplyClass : byte { Immediate, Amortized, UnderVeil, Restart }       // 即时、即时（分帧）、雾幕、重启（SYS-GRAPHICS §3），由轻到重
public enum KnobKind : byte { Floor, StyleDefault, Performance }                 // 风格底线（有最差值）、风格默认（可关）、性能项（SYS-GRAPHICS §2）

public enum QualityKnob : byte                      // 共 27 项，序号固定（QualityChange.Knobs 按序号置位）；--quality-set 用它的 snake_case
{
    DisplayMode, WindowSize, VSync, FrameCap, Vrr,
    RenderScale, FsrSharpness, AntiAliasing, Anisotropy,
    ShadowQuality, ShadowDistance, SmallCasters,
    GlowQuality, TiltShift, LensFlare, LightShafts,
    DetailScale, MeshLodThreshold,
    GeometricLeaves, FoliageDensity, GrassRadius,
    ReflectionInterval, ParticleDensity,
    Adaptive, GraphicsApi, GpuIndex,
    Vignette,                                    // 暗角（风格默认，SYS-GRAPHICS §2），追加在末尾
}

public sealed record KnobSpec(QualityKnob Knob, string OwnerWp, ApplyClass Apply, KnobKind Kind,
    float Min, float Max, float Step, float WorstAllowed, bool HigherIsBetter,
    bool InAdaptiveLadder, string LabelKey);        // 界面按它自动生成控件；枚举类的项按序号编码成 float

public sealed record QualityProfile(QualityPreset Preset,
    IReadOnlyDictionary<QualityKnob, float> Overrides, bool Adaptive, int Schema = 1);   // Overrides 只存与预设不同的项

/// <summary>An immutable, allocation-free view of effective and requested quality values.</summary>
public sealed class QualityView
{
    private static readonly int KnobCount = Enum.GetValues<QualityKnob>().Length;
    private readonly float[] _effective;
    private readonly float[] _requested;
    private readonly bool[] _locked;
    private readonly bool[] _reduced;

    public QualityView(long version, QualityPreset preset, DeviceTier tier, bool adaptive,
        IReadOnlyList<float> effective, IReadOnlyList<float>? requested = null,
        IReadOnlyList<bool>? locked = null, IReadOnlyList<bool>? reduced = null,
        int stepRate = 12)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);
        if (!Enum.IsDefined(preset)) throw new ArgumentOutOfRangeException(nameof(preset));
        if (!Enum.IsDefined(tier)) throw new ArgumentOutOfRangeException(nameof(tier));
        if (stepRate is not (12 or 15)) throw new ArgumentOutOfRangeException(nameof(stepRate));
        ArgumentNullException.ThrowIfNull(effective);
        _effective = Copy(effective, nameof(effective));
        _requested = Copy(requested ?? effective, nameof(requested));
        _locked = locked is null ? new bool[KnobCount] : Copy(locked, nameof(locked));
        _reduced = reduced is null ? new bool[KnobCount] : Copy(reduced, nameof(reduced));
        if (_effective.Any(value => !float.IsFinite(value)) || _requested.Any(value => !float.IsFinite(value)))
            throw new ArgumentException("Quality values must be finite.");
        Version = version;
        Preset = preset;
        Tier = tier;
        Adaptive = adaptive;
        StepRate = stepRate;
    }

    public long Version { get; }
    public QualityPreset Preset { get; }
    public DeviceTier Tier { get; }
    public bool Adaptive { get; }
    public int StepRate { get; }
    public float this[QualityKnob k] => _effective[Index(k)];
    public float Requested(QualityKnob k) => _requested[Index(k)];
    public bool Locked(QualityKnob k) => _locked[Index(k)];
    public bool Reduced(QualityKnob k) => _reduced[Index(k)];

    private static int Index(QualityKnob k)
    {
        var index = (int)k;
        if (index >= KnobCount) throw new ArgumentOutOfRangeException(nameof(k));
        return index;
    }

    private static T[] Copy<T>(IReadOnlyList<T> source, string parameter)
    {
        if (source.Count != KnobCount)
            throw new ArgumentException($"Expected {KnobCount} quality values in enum order.", parameter);
        var result = new T[KnobCount];
        for (var i = 0; i < KnobCount; i++) result[i] = source[i];
        return result;
    }
}

public enum ApplyOrigin : byte
{
    Startup,        // 启动：graphics.cfg > 自动推荐 > 默认值
    CommandLine,    // --quality、--quality-set、--adaptive、--tier：只在内存里生效、不写盘，优先于 graphics.cfg（SYS-GRAPHICS §6）
    User,           // 设置页：写盘；改过的项锁定
    Gate,           // 门禁、基准、画面测试与"预设轮换"脚本：不写盘、不锁定
    Revert,         // 15 s 内没有 ConfirmDisplay()，自动还原显示类设置
    Adaptive,       // 自动调节；只出现在 QualityChange 里，Apply 不接受
}

public readonly record struct QualityChange(long Version, ulong Knobs, ApplyOrigin Origin, bool Veiled)
{
    public bool Has(QualityKnob k) => (Knobs & (1UL << (int)k)) != 0;   // 按 QualityKnob 的序号置位，不分配
}   // Veiled：本次在雾幕全黑时生效；为 false 时负责方自己淡化，不得硬切

public readonly record struct KnobDelta(QualityKnob Knob, float From, float To, ApplyClass Apply);
public sealed record ApplyPlan(
    QualityProfile Next,
    IReadOnlyList<KnobDelta> Deltas,                // 只列有变化的项
    ApplyClass Heaviest,                            // 最重的一类：有 UnderVeil 时界面才亮"应用"，有 Restart 时显示"启"
    bool NeedsConfirm,                              // 含显示模式、窗口尺寸、渲染比例：生效后要在 15 s 内确认（SYS-GRAPHICS §4 第 6 步）
    long VramBytes, long VramBudgetBytes,           // 按 SYS-GRAPHICS §5 的公式估的显存与 DXGI 给的预算；P2 之前为 0
    IReadOnlyList<QualityKnob> Rejected);           // 劣于最差值、超出范围或不合法的项；不为空时 Apply 整体拒绝，什么也不改

public enum ApplyStatus : byte { Applied, Deferred, Rejected }
public sealed record ApplyResult(
    ApplyStatus Status,                             // Deferred：SYS-GRAPHICS §4 第 5 步列出的过渡进行中（跳转、地图传送、模式切换、换季转场、回位、
                                                    // 切图、读档的遮挡期间、进场过场与返回标题），请求排队，
                                                    // 结束后执行并发 Changed；排队期间再 Apply，新请求替换旧请求
    QualityView Effective,
    bool AwaitingConfirm,                           // 显示类改动已生效，等 ConfirmDisplay()
    bool RestartPending,                            // 有重启类改动：已写盘，下次启动生效（SYS-GRAPHICS §4 第 4 步）
    IReadOnlyList<QualityKnob> Rejected);

public interface IQualityService                    // Kernel 服务（WP-32）；界面与门禁的唯一入口
{
    DeviceTier Tier { get; }
    QualityPreset Recommended { get; }
    QualityProfile Requested { get; }
    QualityView Effective { get; }                  // 实际生效值，读取无分配
    IReadOnlyList<KnobSpec> Knobs { get; }          // 单项目录（QualityCatalog）：画面页按它生成控件
    float PresetValue(QualityPreset p, QualityKnob k);   // 预设表 game/data/quality-presets.json 的取值
    event Action<QualityChange>? Changed;           // 主线程，值变才发
    ApplyPlan Plan(QualityProfile next);            // 界面据此显示"雾幕""重启"生效标签与显存估计（UI-VISUAL §8.5）
    Task<ApplyResult> Apply(QualityProfile next, ApplyOrigin origin);   // origin 为 Adaptive 时抛 ArgumentException
    void ConfirmDisplay();                          // 15 s 确认
}

public interface IQualityAware                      // 标了 [QualityResponsive] 的 [KernelNode] 实现
{
    // WP-32 在 FramePriority.Quality 那一步调用：启动时以全部项调用一次，之后只在 Owns 或 Reads 里的项变化时调用。
    // 即时类按节点 ID 顺序调用；雾幕类在雾幕全黑时按 SYS-GRAPHICS §4 第 3 步的负责方顺序调用；
    // 分帧类（细节距离）由负责方自己分帧完成，每帧不超过 0.3 ms；自动调节的每级降档在 0.5–1 s 内淡化完成（G13）
    void ApplyQuality(QualityView view, in QualityChange change);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class QualityResponsiveAttribute : Attribute   // 标在 Godot 侧包的 [KernelNode] 上
{
    public QualityKnob[] Owns { get; init; } = [];   // 我负责落到引擎上；G5 核对每项恰有一个负责方
    public QualityKnob[] Reads { get; init; } = [];  // 只读，或按它调整自己的对象、但不对这一项的效果负责
}
// ParticleDensity：WP-45 Owns（降水池），WP-38 Reads（飘落池按同一系数降；萤火虫不降，SYS-GRAPHICS §3）
// 着色器要的值（细节系数、植被密度、草半径）由 WP-32 合成全局参数 quality（vec4，ENG-PERF §1.5）
// 任何包都不读 user://graphics.cfg，只经 IQualityService 和 IQualityAware 接收设置（SYS-GRAPHICS §8）
