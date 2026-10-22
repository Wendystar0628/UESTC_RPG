using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Gates;
using Campus.Kernel.Frame;
using Campus.Kernel.Registry;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Stage;

/// <summary>Reads startup readiness and prints phase and optional renderer timing observations.</summary>
internal sealed class StageStartupTrace
{
    private bool _preloadReadyTraced;
    private readonly bool _profiling = System.Environment.GetEnvironmentVariable("UESTC_STARTUP_PROFILE") == "1";
    private IAppFlow? _profileFlow;
    private IObserverState? _profileObserver;
    private IInputRouter? _profileInput;
    private static readonly StringName ForwardAction = "move_forward";

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

    internal void TraceFrame(FrameStatistics? statistics)
    {
        if (_profiling && statistics is not null)
        {
            var frame = statistics.Current;
            _profileFlow ??= KernelServices.Get<IAppFlow>();
            var state = _profileFlow.State;
            var movement = "";
            if (state == AppState.Playing)
            {
                _profileObserver ??= KernelServices.Get<IObserverState>();
                if (_profileInput is null)
                {
                    _profileInput = KernelServices.Get<IInputRouter>();
                    GD.Print($"STARTUP_INPUT action=move_forward physicalKey={_profileInput.PhysicalKey("move_forward")}");
                }
                var feet = _profileObserver.Feet;
                movement = FormattableString.Invariant($" mode={_profileObserver.Mode} feetX={feet.X:R} feetY={feet.Y:R} feetZ={feet.Z:R} yawDeg={_profileObserver.YawDeg:R} forwardPressed={Input.IsActionPressed(ForwardAction)} interfaceMode={_profileInput.InterfaceMode}");
            }
            GD.Print(FormattableString.Invariant($"STARTUP_FRAME {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()} rendererCpuMs={frame.CpuRenderMilliseconds:R} rendererGpuMs={frame.GpuRenderMilliseconds:R} flow={state} pipelineSurface={frame.Pipelines.Surface:R} pipelineDraw={frame.Pipelines.Draw:R} pipelineCanvas={frame.Pipelines.Canvas:R} gcPauseMs={frame.GcPauseMilliseconds:R}{movement}"));
        }
    }

    internal static void TraceTitleInputReady(IAppBootFlow boot)
    {
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE title-input-ready {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()}{StartupReadiness(boot)}"));
    }

    internal static void TraceInputReady()
    {
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE input-ready {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()} mode={KernelServices.Get<ICameraRig>().Mode}"));
    }

    internal void TracePreloadReady(IAppBootFlow boot)
    {
        if (_preloadReadyTraced || !boot.NewGamePreloadReady) return;
        _preloadReadyTraced = true;
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE preload-ready {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()}{StartupReadiness(boot)}"));
    }
    private static string StartupReadiness(IAppBootFlow boot) =>
        $" mapReady={boot.MapReady.IsCompletedSuccessfully} preloadReady={boot.NewGamePreloadReady}";
}
