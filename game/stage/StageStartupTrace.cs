using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Gates;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Campus.Kernel.Registry;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Stage;

/// <summary>Reads startup readiness and publishes phase and process-control observations.</summary>
internal sealed class StageStartupTrace
{
    private bool _preloadReadyTraced;

    internal static void TracePhase(string phase)
    {
        RegistryStageGate.TraceBootPhase(phase);
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE {phase} {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()}"));
    }

    internal static void TraceFlowPhase(ServiceRegistry registry, string phase)
    {
        var boot = registry.Get<IAppBootFlow>();
        var suffix = phase == "new-game-enter" ? StartupReadiness(boot) : "";
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE {phase} {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()}{suffix}"));
    }

    private static void TraceEnvironment()
    {
        var windowHandle = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        GD.Print(FormattableString.Invariant($"STARTUP_ENVIRONMENT {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()} windowHandle={windowHandle}"));
    }

    internal static void TraceTitleInputReady(IAppBootFlow boot)
    {
        TraceEnvironment();
        var time = FrameClock.MonotonicMilliseconds;
        var frame = Engine.GetProcessFrames();
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE title-input-ready {time:R} frame={frame}{StartupReadiness(boot)}"));
        KernelServices.Get<PerformanceRecordingService>().AppPhase("title-input-ready", frame, time);
    }

    internal static void TraceInputReady()
    {
        TraceEnvironment();
        var time = FrameClock.MonotonicMilliseconds;
        var frame = Engine.GetProcessFrames();
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE input-ready {time:R} frame={frame} mode={KernelServices.Get<ICameraRig>().Mode}"));
        KernelServices.Get<PerformanceRecordingService>().AppPhase("input-ready", frame, time);
    }

    internal void TracePreloadReady(IAppBootFlow boot)
    {
        if (_preloadReadyTraced || !boot.NewGamePreloadReady) return;
        _preloadReadyTraced = true;
        var time = FrameClock.MonotonicMilliseconds;
        var frame = Engine.GetProcessFrames();
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE preload-ready {time:R} frame={frame}{StartupReadiness(boot)}"));
        KernelServices.Get<PerformanceRecordingService>().AppPhase("preload-ready", frame, time);
    }
    private static string StartupReadiness(IAppBootFlow boot) =>
        $" mapReady={boot.MapReady.IsCompletedSuccessfully} preloadReady={boot.NewGamePreloadReady}";
}
