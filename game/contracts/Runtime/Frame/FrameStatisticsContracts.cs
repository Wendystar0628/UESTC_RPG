using Campus.Contracts.AppFlow;
using Godot;

namespace Campus.Contracts;

public enum FrameBoundary : byte { B0, B1, B2, B3, B4 }

[Flags]
public enum FramePhaseValidity : byte
{
    None = 0, Physics = 1, Process = 2, Late = 4, Draw = 8, Tail = 16,
    All = Physics | Process | Late | Draw | Tail
}

public readonly record struct FrameStageSnapshot(
    FrameBoundary Boundary, ulong CollectionFrame, double MonotonicMilliseconds);

public readonly record struct FramePhases(
    FramePhaseValidity Validity,
    double? PhysicsMilliseconds, double? ProcessMilliseconds,
    double? LateMilliseconds, double? DrawMilliseconds, double? TailMilliseconds,
    int PhysicsSteps);

public readonly record struct FramePipelineCounts(
    long Canvas, long Mesh, long Surface, long Draw, long Specialization);

public readonly record struct FrameRenderCounts(
    int VisibleDrawCalls, int VisiblePrimitives, int ShadowDrawCalls, int ShadowPrimitives);

public readonly record struct FrameGcSample(
    double PauseMilliseconds, int Gen0Collections, int Gen1Collections,
    int Gen2Collections, long AllocatedBytes, long ManagedHeapBytes);

public readonly record struct FrameJitSample(
    long CompiledMethods, long CompiledIlBytes, double CompilationMilliseconds);

public readonly record struct FrameContextSample(
    AppState State, AppActivity Activity, bool? WindowFocused,
    bool WindowCanDraw, bool TreePaused);

public readonly record struct ViewportFrameSample
{
    public ulong ViewportId { get; init; }
    public string Role { get; init; }
    // 所属采集行的绘制报告；没有报告与不能确认实际绘制均为 null。
    public bool? DrewThisFrame { get; init; }
    // true 只表示本次确认有新回读结果，不能由浮点值变化推出。
    public bool? ResultUpdated { get; init; }
    // 来源绘制轮次独立于采集 process 帧号；未证映射为 null。
    public ulong? SourceDrawFrame { get; init; }
    // 原始 CPU timestamp 与 Time.GetTicksUsec 同源的对应需首次实验确认。
    public ulong? SourceCpuBeginMicroseconds { get; init; }
    public ulong? SourceCpuEndMicroseconds { get; init; }
    // DECISION-342: the continuously drawn main viewport supplies approximate cost samples (±2 frames).
    // Its exact source/newness may remain unknown; on-demand viewports still require source confirmation.
    public double? CpuRenderMilliseconds { get; init; }
    public double? GpuRenderMilliseconds { get; init; }
}

public readonly record struct FrameSample
{
    public ulong CollectionFrame { get; init; }
    public double MonotonicMilliseconds { get; init; }
    public double? FrameMilliseconds { get; init; }
    public FramePhases Phases { get; init; }
    public ulong? MainCycles { get; init; }
    public ulong? DrawCycles { get; init; }
    public float? ObserverYawDegrees { get; init; }
    public float? CameraPitchDegrees { get; init; }
    public ViewportFrameSample MainViewport { get; init; }
    public double? MeasuredViewportGpuMilliseconds { get; init; }
    // 在 B4 读取本次绘制已写入的 setup，保留来源，不与视口回读假定同帧。
    public double? FrameSetupCpuMilliseconds { get; init; }
    public ulong? FrameSetupDrawFrame { get; init; }
    public FrameRenderCounts RenderCounts { get; init; }
    public FramePipelineCounts Pipelines { get; init; }
    public FrameGcSample Gc { get; init; }
    public FrameJitSample Jit { get; init; }
    public FrameContextSample Context { get; init; }
}

public readonly record struct FrameDisplayWindow
{
    public ulong Sequence { get; init; }
    public int FrameCount { get; init; }
    public double IntervalMilliseconds { get; init; }
    public double FramesPerSecond { get; init; }
    public double AverageFrameMilliseconds { get; init; }
    public double SlowestFrameMilliseconds { get; init; }
    public double? MainViewportCpuMilliseconds { get; init; }
    public double? MainViewportGpuMilliseconds { get; init; }
    public double? ProcessCpuFraction { get; init; }
    public long WorkingSetBytes { get; init; }
    public long EngineVideoMemoryBytes { get; init; }
}

public delegate void FrameSamplePublished(
    in FrameSample sample, ReadOnlySpan<ViewportFrameSample> viewports);

public interface IFrameStatistics
{
    // 以下两项首次发布前为空；仅主线程读取。
    FrameSample? Latest { get; }
    FrameDisplayWindow? DisplayWindow { get; }
    // 同 Latest 的各受测视口；借用内存仅在本次主线程调用内有效。
    ReadOnlySpan<ViewportFrameSample> Viewports { get; }
    // 唯一允许后台读取的成员：三个字段必须来自同一次发布；初始为空。
    FrameStageSnapshot? StageSnapshot { get; }
    // 主线程：先替换 Latest/Viewports/可能完成的显示窗，再每采集帧调用一次。
    event FrameSamplePublished? SamplePublished;
}

public interface IMeasuredViewportRegistry
{
    // 主线程登记一次；角色在登记期间不可变；同一 RID 不重复登记。
    // 所有者在释放 RID 前 Dispose；注销只结束测量，不释放视口本身。
    IDisposable Register(Rid viewport, string role);
    // collectionFrame 必须显式传 Engine.GetProcessFrames()；不能沿用前帧报告。
    // false 是本轮明确跳过；true 是所有者本轮安排绘制。主视口成本按 DECISION-342 近似对齐，
    // 其他视口仍须回读证据确认结果；不把主视口近似成本声明为精确来源。
    void ReportDraw(Rid viewport, ulong collectionFrame, bool drawn);
}
