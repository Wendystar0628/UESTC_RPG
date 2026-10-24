using Campus.Contracts;
using System.Runtime;
using Godot;

namespace Campus.Kernel.Frame;

/// <summary>Single producer of frame facts. Viewport timings never estimate gameplay CPU time.</summary>
public sealed class FrameStatistics : IFrameStatistics, IMeasuredViewportRegistry, IDisposable
{
    private readonly Rid _mainViewport;
    private readonly MeasuredViewports _viewports;
    private readonly FramePhaseAccumulator _phases;
    private readonly FrameDisplayAccumulator _display = new();
    private readonly object _snapshotGate = new();
    private FrameStageSnapshot? _snapshot;
    private IDisposable? _mainRegistration;
    private TimeSpan _gcPause = GC.GetTotalPauseDuration();
    private int _gen0 = GC.CollectionCount(0), _gen1 = GC.CollectionCount(1), _gen2 = GC.CollectionCount(2);
    private long _allocated = GC.GetTotalAllocatedBytes(false);
    private double? _pendingSetup, _completedSetup;
    private ulong? _pendingSetupFrame, _completedSetupFrame;
    private ulong? _publishedFrame;
    private TimeSpan _processCpu;
    private double _processCpuTime;

    public FrameStatistics(Viewport viewport)
    {
        _mainViewport = viewport.GetViewportRid();
        _viewports = new(_mainViewport);
        // Safe is the native single render-thread model required by ENG-PERF-DATA.
        _phases = new(ProjectSettings.GetSettingWithOverride("rendering/driver/threads/thread_model").AsInt32() == 1);
    }

    public FrameSample? Latest { get; private set; }
    public FrameDisplayWindow? DisplayWindow { get; private set; }
    public ReadOnlySpan<ViewportFrameSample> Viewports => _viewports.Samples;
    public FrameStageSnapshot? StageSnapshot { get { lock (_snapshotGate) return _snapshot; } }
    public event FrameSamplePublished? SamplePublished;

    public void Enable()
    {
        if (_mainRegistration is not null) return;
        _mainRegistration = Register(_mainViewport, "main");
        _processCpu = System.Environment.CpuUsage.TotalTime;
        _processCpuTime = FrameClock.MonotonicMilliseconds;
    }

    public IDisposable Register(Rid viewport, string role) => _viewports.Register(viewport, role);
    public void ReportDraw(Rid viewport, ulong collectionFrame, bool drawn) => _viewports.ReportDraw(viewport, collectionFrame, drawn);

    internal void Physics(ulong frame, double now)
    {
        PublishBoundary(FrameBoundary.B0, frame, now);
        _phases.Physics(now);
    }

    internal void Begin(ulong frame, double now)
    {
        var cycles = ThreadCycles.Read();
        PublishBoundary(FrameBoundary.B1, frame, now);
        _phases.Begin(now, cycles);
        _completedSetup = _pendingSetup;
        _completedSetupFrame = _pendingSetupFrame;
        _pendingSetup = null;
        _pendingSetupFrame = null;
    }

    internal void PreDraw(ulong frame, double now)
    {
        var cycles = ThreadCycles.Read();
        PublishBoundary(FrameBoundary.B3, frame, now);
        _phases.PreDraw(now, cycles);
    }

    internal void PostDraw(ulong frame, double now)
    {
        var cycles = ThreadCycles.Read();
        PublishBoundary(FrameBoundary.B4, frame, now);
        _phases.PostDraw(now, cycles);
        _pendingSetup = RenderingServer.GetFrameSetupTimeCpu();
        _pendingSetupFrame = (ulong)Engine.GetFramesDrawn();
    }

    internal void Sample(ulong frame, double now, in FrameContextSample context, float yaw, float pitch)
    {
        if (_publishedFrame == frame) return;
        PublishBoundary(FrameBoundary.B2, frame, now);
        _phases.ProcessEnd(now);
        _viewports.ReportDraw(_mainViewport, frame, context.WindowCanDraw && RenderingServer.IsRenderLoopEnabled());
        _viewports.Sample(frame);
        var pause = GC.GetTotalPauseDuration();
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        var allocated = GC.GetTotalAllocatedBytes(false);
        var sample = new FrameSample
        {
            CollectionFrame = frame, MonotonicMilliseconds = now, FrameMilliseconds = _phases.FrameMilliseconds,
            Phases = _phases.Phases, MainCycles = _phases.MainCycles, DrawCycles = _phases.DrawCycles,
            ObserverYawDegrees = yaw, CameraPitchDegrees = pitch, Context = context,
            MainViewport = _viewports.MainSample, MeasuredViewportGpuMilliseconds = null,
            FrameSetupCpuMilliseconds = _completedSetup, FrameSetupDrawFrame = _completedSetupFrame,
            RenderCounts = new(RenderInfo(RenderingServer.ViewportRenderInfoType.Visible, RenderingServer.ViewportRenderInfo.DrawCallsInFrame),
                RenderInfo(RenderingServer.ViewportRenderInfoType.Visible, RenderingServer.ViewportRenderInfo.PrimitivesInFrame),
                RenderInfo(RenderingServer.ViewportRenderInfoType.Shadow, RenderingServer.ViewportRenderInfo.DrawCallsInFrame),
                RenderInfo(RenderingServer.ViewportRenderInfoType.Shadow, RenderingServer.ViewportRenderInfo.PrimitivesInFrame)),
            Pipelines = PipelineCounts.Read(),
            Gc = new((pause - _gcPause).TotalMilliseconds, gen0 - _gen0, gen1 - _gen1, gen2 - _gen2,
                allocated - _allocated, GC.GetTotalMemory(false)),
            Jit = new(JitInfo.GetCompiledMethodCount(), JitInfo.GetCompiledILBytes(), JitInfo.GetCompilationTime().TotalMilliseconds)
        };
        _gcPause = pause;
        _gen0 = gen0; _gen1 = gen1; _gen2 = gen2; _allocated = allocated;
        Latest = sample;
        _publishedFrame = frame;
        if (_display.Add(in sample, out var window)) CompleteDisplayWindow(window, now);
        SamplePublished?.Invoke(in sample, Viewports);
    }

    private int RenderInfo(RenderingServer.ViewportRenderInfoType type, RenderingServer.ViewportRenderInfo info) =>
        RenderingServer.ViewportGetRenderInfo(_mainViewport, type, info);

    private void CompleteDisplayWindow(FrameDisplayWindow window, double now)
    {
        var cpu = System.Environment.CpuUsage.TotalTime;
        var elapsed = now - _processCpuTime;
        DisplayWindow = window with
        {
            ProcessCpuFraction = elapsed > 0 ? Math.Clamp((cpu - _processCpu).TotalMilliseconds / elapsed / System.Environment.ProcessorCount, 0, 1) : null,
            WorkingSetBytes = System.Environment.WorkingSet,
            EngineVideoMemoryBytes = (long)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed)
        };
        _processCpu = cpu;
        _processCpuTime = now;
    }

    private void PublishBoundary(FrameBoundary boundary, ulong frame, double now)
    {
        lock (_snapshotGate) _snapshot = new(boundary, frame, now);
    }

    public void Dispose() => _viewports.Dispose();
}
