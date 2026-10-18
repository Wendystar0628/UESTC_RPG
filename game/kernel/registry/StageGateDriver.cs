using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
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
    string Phase { set; }
    void CheckActive();
    void SyncView();
    Task Close();
    void Fail(Exception error);
}

/// <summary>
/// Drives the production stage from the command line: named cinematics (--cine), screenshot runs (--shots, --frames)
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

    /// <summary>--cine: plays one named cinematic after the boot reveal.</summary>
    public async Task RunNamedCinematic()
    {
        var options = AppBoot.Options;
        if (!options.Has("cine")) return;
        host.Phase = "named-cinematic";
        var saves = Kernel.Get<ISaveService>();
        var flow = host.Flow;
        switch (options.Value("cine"))
        {
            case "enter_new":
            case "enter_new_short":
                SlotId? selected = options.Has("save") && options.Value("save") != "off"
                    ? new SlotId("s" + options.Value("save"))
                    : options.Has("slot") ? new SlotId("s" + options.Value("slot").TrimStart('s')) : null;
                var slot = selected ?? saves.Slots.FirstOrDefault(info => info.Health == SlotHealth.Empty)?.Slot
                    ?? throw new InvalidOperationException("Named new-game playback requires an empty or explicitly selected slot.");
                if (await flow.Enter(new(EnterKind.NewGame, slot)) != EnterResult.Entered)
                    throw new InvalidOperationException("Named new-game playback failed.");
                break;
            case "enter_continue":
                if (await flow.Enter(new(EnterKind.Continue, saves.Active ?? saves.Continue)) != EnterResult.Entered)
                    throw new InvalidOperationException("Named continue playback requires a readable slot.");
                break;
            case "leave":
                await flow.ReturnToTitle();
                if (flow.State != AppState.Title) throw new InvalidOperationException("Named return-to-title playback failed.");
                break;
            default: throw new ArgumentException("Unknown named cinematic: " + options.Value("cine"));
        }
        host.CheckActive();
    }

    /// <summary>Called once startup reached realtime: starts the lifecycle gate or a frameless screenshot run.</summary>
    public async Task OnRealtime()
    {
        var options = AppBoot.Options;
        if (options.Value("gate") is "save-close" or "save-resume")
        {
            await RunCoreLifecycle(options.Value("gate"));
            return;
        }
        if (!options.Has("gate") && options.Has("shots") && !options.Has("frames")) { _finishing = true; _ = FinishRun(); }
    }

    /// <summary>--frames: ends the run after a fixed number of realtime frames.</summary>
    public void OnProcess()
    {
        if (!host.Ready || host.Closing || _finishing) return;
        _frames++;
        if (!AppBoot.Options.Has("gate") && int.TryParse(AppBoot.Value("frames"), NumberStyles.None, CultureInfo.InvariantCulture, out var limit) && _frames >= limit)
        { _finishing = true; _ = FinishRun(); }
    }

    private async Task FinishRun()
    {
        try
        {
            if (AppBoot.Options.Has("shots"))
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
