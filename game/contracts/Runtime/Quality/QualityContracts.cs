namespace Campus.Contracts.Quality;

public enum QualityPreset : byte { Balanced, Fine, Ultra, Custom }              // 均衡、精致、极致、自定义
public enum ApplyClass : byte { Immediate, Amortized, UnderLoading, Restart }       // 即时、即时（分帧）、加载界面、重启（SYS-GRAPHICS），由轻到重
public enum KnobKind : byte { Floor, StyleDefault, Performance }                 // 风格底线（有最差值）、风格默认（可关）、性能项（SYS-GRAPHICS）

public enum QualityKnob : byte                      // 共 11 项，连续序号（QualityChange.Knobs 按序号置位）；--quality-set 用它的 snake_case
{
    DisplayMode, WindowSize, VSync, FrameCap,
    RenderScale, AntiAliasing,
    ShadowQuality, ShadowDistance, FoliageDensity,
    ReflectionInterval, ParticleDensity,
}

public sealed record KnobSpec(QualityKnob Knob, string OwnerWp, ApplyClass Apply, KnobKind Kind,
    float Min, float Max, float Step, float WorstAllowed, bool HigherIsBetter,
    string LabelKey)                               // 界面按它自动生成控件；枚举类的项按序号编码成 float
{
    /// <summary>When present, only these discrete values are selectable and valid.</summary>
    public IReadOnlyList<float>? Values { get; init; }
}

public sealed record QualityProfile(QualityPreset Preset,
    IReadOnlyDictionary<QualityKnob, float> Overrides, int Schema = 1);   // Overrides 只存与预设不同的项

/// <summary>An immutable, allocation-free view of effective and requested quality values.</summary>
public sealed class QualityView
{
    private static readonly int KnobCount = Enum.GetValues<QualityKnob>().Length;
    private readonly float[] _effective;
    private readonly float[] _requested;

    public QualityView(long version, QualityPreset preset,
        IReadOnlyList<float> effective, IReadOnlyList<float>? requested = null,
        int stepRate = WorldTime.DefaultStepRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);
        if (!Enum.IsDefined(preset)) throw new ArgumentOutOfRangeException(nameof(preset));
        if (!WorldTime.IsStepRate(stepRate)) throw new ArgumentOutOfRangeException(nameof(stepRate));
        ArgumentNullException.ThrowIfNull(effective);
        _effective = Copy(effective, nameof(effective));
        _requested = Copy(requested ?? effective, nameof(requested));
        if (_effective.Any(value => !float.IsFinite(value)) || _requested.Any(value => !float.IsFinite(value)))
            throw new ArgumentException("Quality values must be finite.");
        Version = version;
        Preset = preset;
        StepRate = stepRate;
    }

    public long Version { get; }
    public QualityPreset Preset { get; }
    public int StepRate { get; }
    public float this[QualityKnob k] => _effective[Index(k)];
    public float Requested(QualityKnob k) => _requested[Index(k)];

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
    Startup,        // 启动：settings.json > 默认值
    CommandLine,    // --quality、--quality-set：只在内存里生效、不写盘，优先于 settings.json（SYS-GRAPHICS）
    User,           // 设置页：写盘
    Gate,           // 门禁、基准、画面测试与"预设轮换"脚本：不写盘
}

public readonly record struct QualityChange(long Version, ulong Knobs, ApplyOrigin Origin, bool Loading)
{
    public bool Has(QualityKnob k) => (Knobs & (1UL << (int)k)) != 0;   // 按 QualityKnob 的序号置位，不分配
}   // Loading：本次在加载界面完全遮盖时生效；为 false 时负责方自己淡化，不得硬切

public readonly record struct KnobDelta(QualityKnob Knob, float From, float To, ApplyClass Apply);
public sealed record ApplyPlan(
    QualityProfile Next,
    IReadOnlyList<KnobDelta> Deltas,                // 只列有变化的项
    ApplyClass Heaviest,                            // 最重的一类：有 UnderLoading 时界面才亮"应用"，有 Restart 时显示"启"
    long VramBytes, long VramBudgetBytes,           // 按 SYS-GRAPHICS 的公式估的显存与 DXGI 给的预算；P2 之前为 0
    IReadOnlyList<QualityKnob> Rejected);           // 劣于最差值、超出范围或不合法的项；不为空时 Apply 整体拒绝，什么也不改

public enum ApplyStatus : byte { Applied, Deferred, Rejected }
public sealed record ApplyResult(
    ApplyStatus Status,                             // Deferred：SYS-GRAPHICS 第 5 步列出的过渡进行中（跳转、地图传送、模式切换、换季转场、回位、
                                                    // 切图、读档的遮挡期间、进场过场与返回标题），请求排队，
                                                    // 结束后执行并发 Changed；排队期间再 Apply，新请求替换旧请求
    QualityView Effective,
    bool RestartPending,                            // 有重启类改动：已写盘，下次启动生效（SYS-GRAPHICS 第 4 步）
    IReadOnlyList<QualityKnob> Rejected);

public interface IQualityService                    // Kernel 服务（WP-32）；界面与门禁的唯一入口
{
    QualityProfile Requested { get; }
    QualityView Effective { get; }                  // 实际生效值，读取无分配
    IReadOnlyList<KnobSpec> Knobs { get; }          // 单项目录（QualityCatalog）：画面页按它生成控件
    float PresetValue(QualityPreset p, QualityKnob k);   // 预设表 game/data/quality-presets.json 的取值
    event Action<QualityChange>? Changed;           // 主线程，值变才发
    ApplyPlan Plan(QualityProfile next);            // 界面据此显示"加载界面""重启"生效标签与显存估计（UI-VISUAL）
    Task<ApplyResult> Apply(QualityProfile next, ApplyOrigin origin);
}

public interface IQualityAware                      // 标了 [QualityResponsive] 的 [KernelNode] 实现
{
    // WP-32 在 FramePriority.Quality 那一步调用：启动时以全部项调用一次，之后只在 Owns 或 Reads 里的项变化时调用。
    // 即时类按节点 ID 顺序调用；加载界面类在加载界面完全遮盖时按 SYS-GRAPHICS 第 3 步的负责方顺序调用；
    // 分帧类由负责方自己分帧完成，每帧不超过 0.3 ms。
    void ApplyQuality(QualityView view, in QualityChange change);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class QualityResponsiveAttribute : Attribute   // 标在 Godot 侧包的 [KernelNode] 上
{
    public QualityKnob[] Owns { get; init; } = [];   // 我负责落到引擎上；G5 核对每项恰有一个负责方
    public QualityKnob[] Reads { get; init; } = [];  // 只读，或按它调整自己的对象、但不对这一项的效果负责
}
// ParticleDensity：WP-45 Owns（降水池），WP-38 Reads（飘落池按同一系数降；萤火虫不降，SYS-GRAPHICS）
// 着色器要的植被密度由 WP-32 合成全局参数 quality（vec4，ENG-PERF）
// 设置由 settings.json 的统一读写方持久化；各包只经 IQualityService 和 IQualityAware 接收画面设置（SYS-GRAPHICS）
