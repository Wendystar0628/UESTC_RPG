using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Startup;
using Campus.Kernel.Frame;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Runs normal player input through Godot; the stage owns and drains the complete sample task.</summary>
internal sealed class StartupSampleDriver(IStageGateHost host)
{
    private readonly StartupSampleInput _input = new(host);
    private static readonly StringName ForwardAction = "move_forward";
    private const double IdleMilliseconds = 10_000;
    private const double ForwardHoldMilliseconds = 20_000;

    public async Task Run(StartupScenario scenario)
    {
        try { await RunScenario(scenario); }
        catch (OperationCanceledException error) when (!host.Lifetime.IsCancellationRequested || error.CancellationToken != host.Lifetime)
        { throw new InvalidOperationException("Startup input sampling was cancelled outside its owning lifetime.", error); }
    }

    private async Task RunScenario(StartupScenario scenario)
    {
        switch (scenario)
        {
            case StartupScenario.Title: return;
            case StartupScenario.Immediate:
            case StartupScenario.Playing: break;
            case StartupScenario.Route: break;
            case StartupScenario.Dwell:
                var boot = KernelServices.Get<IAppBootFlow>();
                var bake = KernelServices.Get<IBakeService>();
                var progress = (boot.MapReady.Status, boot.BackgroundReady.Status, bake.Progress);
                await _input.Wait(() =>
                {
                    if (boot.BackgroundReady.IsCompleted) boot.BackgroundReady.GetAwaiter().GetResult();
                    return boot.NewGamePreloadReady;
                }, "New-game preload stopped making progress.", () =>
                {
                    var next = (boot.MapReady.Status, boot.BackgroundReady.Status, bake.Progress);
                    var changed = next != progress;
                    progress = next;
                    return changed;
                });
                break;
            default: throw new InvalidOperationException("Unknown startup sampling scenario.");
        }
        host.CheckActive();
        if (host.Flow.State != AppState.Title)
            throw new InvalidOperationException("Startup input requires the real title menu.");
        await _input.Press(Key.Enter, false, "title-enter-press", "title-enter-release", _input.NextFrame);
        await _input.Wait(() => host.Flow.State != AppState.Title, "The normal title menu did not accept Enter.");
        if (scenario == StartupScenario.Playing) await SamplePlaying();
        else if (scenario == StartupScenario.Route)
            await new StartupRouteDriver(host, _input).Run(AppBoot.Options.StartupRoute
                ?? throw new InvalidOperationException("The route startup scenario has no route options."));
    }

    private async Task SamplePlaying()
    {
        var walk = KernelServices.Get<IWalkController>();
        var observer = KernelServices.Get<IObserverState>();
        var input = KernelServices.Get<IInputRouter>();
        await _input.WaitForPlayingWalk();
        var physicalKey = (Key)input.PhysicalKey("move_forward");
        GD.Print($"STARTUP_INPUT action=move_forward physicalKey={(int)physicalKey}");
        if (Input.IsActionPressed(ForwardAction))
            throw new InvalidOperationException("The startup sample inherited a held forward action.");
        StartupSampleInput.Trace("playing-idle-start", IdleMilliseconds);
        await HoldFor(IdleMilliseconds, observer, walk);
        StartupSampleInput.Trace("playing-idle-end");
        await _input.Press(physicalKey, true, "playing-forward-start", "playing-forward-end",
            () => HoldFor(ForwardHoldMilliseconds, observer, walk), ForwardHoldMilliseconds,
            () => _input.WaitForRelease(ForwardAction));
        StartupSampleInput.Trace("playing-release-observed");
    }

    private async Task HoldFor(double milliseconds, IObserverState observer, IWalkController walk)
    {
        var started = FrameClock.MonotonicMilliseconds;
        do
        {
            await _input.NextFrame();
            if (host.Flow.State != AppState.Playing || observer.Mode != ViewMode.Walk || !walk.Active || walk.ExitRequested)
                throw new InvalidOperationException("The startup sample left the active Playing Walk controller.");
        } while (FrameClock.MonotonicMilliseconds - started < milliseconds);
    }
}
