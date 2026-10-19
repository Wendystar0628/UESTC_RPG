using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Registry;

/// <summary>What the command-line test drivers need from the production composition root.</summary>
public interface IStageGateHost
{
    Node Stage { get; }
    IAppFlow Flow { get; }
    IMapRuntime MapRuntime { get; }
    ViewContentReadiness Readiness { get; }
    CancellationToken Lifetime { get; }
    bool Ready { get; }
    bool Closing { get; }
    void CheckActive();
    void SyncView();
    Task Close();
    void Fail(Exception error);
}

/// <summary>
/// Drives the production stage from the command line: screenshot runs (--shots, --frames)
/// and the startup/exit core lifecycle (--gate=save-close, save-resume). No second scene or service graph is created.
/// </summary>
public sealed partial class StageGateDriver(IStageGateHost host)
{
    private double? _closeAt;
    private bool _awaitingClose;
    private bool _finishing;
    private int _frames;

    /// <summary>The window-close request that the core lifecycle waits for starts the shutdown timer.</summary>
    public void OnCloseRequest()
    {
        if (!_awaitingClose) return;
        _awaitingClose = false;
        _closeAt = FrameClock.MonotonicMilliseconds;
    }

    public void TraceClose(string phase)
    {
        if (_closeAt is { } started)
            GD.Print($"CORE_CLOSE_PHASE {phase} {FrameClock.MonotonicMilliseconds - started:R}");
    }

    public void ReportShutdown()
    {
        if (_closeAt is { } closeAt)
            GD.Print($"CORE_SHUTDOWN_MS {FrameClock.MonotonicMilliseconds - closeAt:R}");
    }

    /// <summary>Called once startup reached realtime: starts the requested production command.</summary>
    public async Task OnRealtime()
    {
        var options = AppBoot.Options;
        if (options.TravelSequence.Count > 0)
        {
            _finishing = true;
            await RunTravelSequence(options.TravelSequence);
            await host.Close();
            return;
        }
        if (options.Capture.Gate is "save-close" or "save-resume")
        {
            await RunCoreLifecycle(options.Capture.Gate);
            return;
        }
        if (options.Capture.Gate is null && options.Capture.Shots.Count > 0 && options.Capture.FrameLimit is null) { _finishing = true; _ = FinishRun(); }
    }

    private async Task RunTravelSequence(IReadOnlyList<string> sequence)
    {
        if (!host.Flow.IsPlaying) throw new InvalidOperationException("Travel sequence requires Playing after the normal entry flow.");
        var atlas = Kernel.Get<IAtlasService>();
        var observer = Kernel.Get<IObserverState>();
        var walk = Kernel.Get<IWalkController>();
        RequireWalk();
        while (!walk.Grounded || !walk.CollisionReady || !walk.IsSettled)
            await NextFrame();
        var origin = observer.Feet;
        GD.Print(FormattableString.Invariant($"TRAVEL_SEQUENCE_START {sequence.Count} {FrameClock.MonotonicMilliseconds:R} mode={observer.Mode} feet_x={origin.X:R} feet_y={origin.Y:R} feet_z={origin.Z:R}"));
        for (var index = 0; index < sequence.Count; index++)
        {
            var id = new MarkerId(sequence[index]);
            if (atlas.Find(id)?.Teleport?.Stand is null)
                throw new InvalidOperationException("Travel sequence requires a standing destination: " + id.Value);
            if (!atlas.CanTravel(id, TravelStyle.Teleport, out var refusal))
                throw new InvalidOperationException($"Travel sequence cannot start {id.Value}: {refusal}.");
            var handle = atlas.Travel(id, TravelStyle.Teleport)
                ?? throw new InvalidOperationException("Travel sequence was refused: " + id.Value);
            if (await handle.Done != TransitResult.Arrived)
                throw new InvalidOperationException("Travel sequence did not arrive: " + id.Value);
            // Done precedes Atlas.Tick's disposal of the input lease. Here is returned only after that release.
            while (true)
            {
                host.CheckActive();
                RequireWalk();
                atlas.CanTravel(id, TravelStyle.Teleport, out refusal);
                if (refusal == TravelRefusal.Here && walk.Grounded && walk.CollisionReady && walk.IsSettled) break;
                if (refusal is not (TravelRefusal.Busy or TravelRefusal.Unavailable or TravelRefusal.Here))
                    throw new InvalidOperationException($"Travel sequence destination changed after arrival: {id.Value}, {refusal}.");
                await NextFrame();
            }
            GD.Print(FormattableString.Invariant($"TRAVEL_SEQUENCE_STEP_READY {index + 1} {id.Value} {FrameClock.MonotonicMilliseconds:R} mode={observer.Mode} grounded={walk.Grounded} collision={walk.CollisionReady}"));
        }
        GD.Print(FormattableString.Invariant($"TRAVEL_SEQUENCE_COMPLETE {sequence.Count} {FrameClock.MonotonicMilliseconds:R}"));

        void RequireWalk()
        {
            if (observer.Mode != ViewMode.Walk || !walk.Active || walk.ExitRequested)
                throw new InvalidOperationException("Travel sequence requires the active production walk controller.");
        }
        async Task NextFrame()
        {
            host.CheckActive();
            await host.Stage.ToSignal(host.Stage.GetTree(), SceneTree.SignalName.ProcessFrame);
            RequireWalk();
        }
    }

    /// <summary>--frames: ends the run after a fixed number of realtime frames.</summary>
    public void OnProcess()
    {
        if (!host.Ready || host.Closing || _finishing) return;
        _frames++;
        if (AppBoot.Options.Capture.Gate is null && AppBoot.Options.Capture.FrameLimit is { } limit && _frames >= limit)
        { _finishing = true; _ = FinishRun(); }
    }

    private async Task FinishRun()
    {
        try
        {
            if (AppBoot.Options.Capture.Shots.Count > 0)
                await CaptureSequence.RunAsync(host.Stage, host.Readiness, AppBoot.Options, host.SyncView, () =>
                {
                    host.CheckActive();
                    return host.Ready;
                }, host.Lifetime, pose =>
                {
                    if (!Kernel.Get<ICameraPoseValidator>().IsLegal(pose, outsideBuffer: true))
                        throw new ArgumentException("Capture pose is outside the reachable space or inside its buffer.");
                });
            await host.Close();
        }
        catch (OperationCanceledException) when (host.Closing) { }
        catch (Exception error) { host.Fail(error); }
    }
}
