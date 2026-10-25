using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Gates;

/// <summary>What the command-line test drivers need from the production composition root.</summary>
public interface IStageGateHost
{
    Node Stage { get; }
    IAppFlow Flow { get; }
    IMapRuntime MapRuntime { get; }
    ViewContentReadiness Readiness { get; }
    GameCaptureNode.CaptureDestination? StartupCapture { get; }
    CancellationToken Lifetime { get; }
    bool Ready { get; }
    bool Closing { get; }
    void CheckActive();
    void SyncView();
    Task Close();
    void Fail([FailureHandler("SYS-TITLE")] Exception error);
}

/// <summary>
/// Drives the production stage from the command line: screenshot runs (--shots, --frames)
/// and the startup/exit core lifecycle (--gate=save-close, save-resume). No second scene or service graph is created.
/// </summary>
public sealed class StageGateDriver(IStageGateHost host)
{
    private double? _closeAt;
    private bool _awaitingClose;
    private bool _finishing;
    private int _frames;
    private Task? _startupSampleTask;
    private bool _startupSampleFailureReported;

    /// <summary>Starts a held task at the real menu-input boundary without blocking background preparation.</summary>
    public void OnTitleInputReady()
    {
        if (_startupSampleTask is not null || AppBoot.Options.StartupScenario is not { } scenario) return;
        _startupSampleTask = new StartupSampleDriver(host).Run(scenario);
    }

    /// <summary>The host cancels Lifetime first, then drains before releasing any production input services.</summary>
    public async Task DrainStartupSample()
    {
        if (_startupSampleTask is null) return;
        try { await _startupSampleTask; }
        catch (OperationCanceledException error) when (host.Lifetime.IsCancellationRequested &&
            error.CancellationToken == host.Lifetime && _startupSampleTask.IsCanceled) { }
    }

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

    /// <summary>Starts the title-entry core command after the production title startup completes.</summary>
    public Task OnTitleReady() => AppBoot.Options.Capture.Gate == "save-close"
        ? RunCore("save-close") : Task.CompletedTask;

    /// <summary>Called once Playing reached realtime: starts the requested production command.</summary>
    public async Task OnRealtime()
    {
        if (_finishing) return;
        var options = AppBoot.Options;
        if (options.StartupScenario == Campus.Contracts.Startup.StartupScenario.Route)
        {
            _finishing = true;
            try { await (_startupSampleTask ?? throw new InvalidOperationException("Route reached Playing without its menu input task.")); }
            catch { _startupSampleFailureReported = true; throw; }
            await CloseRun();
            return;
        }
        if (options.TravelSequence.Count > 0)
        {
            _finishing = true;
            var travel = new PerfTravelDriver(host);
            if (options.PerfCollection is { } travelCollection)
            {
                var collector = new PerfCollectionDriver(host, travelCollection) { TravelSegments = travel.Segments };
                await collector.Run(() => travel.Run(options.TravelSequence, travelCollection.TravelPrefetch));
            }
            else if (await travel.Run(options.TravelSequence, false) is { } failure) throw new InvalidOperationException(failure);
            await CloseRun();
            return;
        }
        if (options.PerfCollection is { } collection)
        {
            _finishing = true;
            await new PerfCollectionDriver(host, collection).Run(verifyFixedStart: true);
            await CloseRun();
            return;
        }
        if (options.Capture.Gate is "save-close" or "save-resume")
        {
            await RunCore(options.Capture.Gate);
            return;
        }
        if (options.Capture.Gate is null && options.Capture.Shots.Count > 0 && options.Capture.FrameLimit is null) { _finishing = true; _ = StageCapture.FinishRun(host); }
    }

    private async Task CloseRun()
    {
        if (AppBoot.Options.PerfCollection is null) { await host.Close(); return; }
        GD.Print(FormattableString.Invariant($"PERF_COLLECTION_CLOSE_BEGIN {FrameClock.MonotonicMilliseconds:R}"));
        await host.Close();
        GD.Print(FormattableString.Invariant($"PERF_COLLECTION_CLOSE_END {FrameClock.MonotonicMilliseconds:R}"));
    }

    private async Task RunCore(string gate)
    {
        if (_finishing) return;
        _finishing = true;
        var window = await new CoreLifecycleDriver(host).Run(gate);
        _awaitingClose = true;
        GD.Print("CORE_READY_CLOSE " + window);
    }

    /// <summary>--frames: ends the run after a fixed number of realtime frames.</summary>
    public void OnProcess()
    {
        if (!_startupSampleFailureReported && _startupSampleTask is { IsFaulted: true })
        {
            _startupSampleFailureReported = true;
            host.Fail(_startupSampleTask.Exception!);
        }
        if (!host.Ready || host.Closing || _finishing) return;
        _frames++;
        if (AppBoot.Options.Capture.Gate is null && AppBoot.Options.Capture.FrameLimit is { } limit && _frames >= limit)
        { _finishing = true; _ = StageCapture.FinishRun(host); }
    }
}
