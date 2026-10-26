using Campus.Contracts.AppFlow;
using Campus.Contracts.Startup;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Gates;

/// <summary>Runs normal player input through Godot; the stage owns and drains the complete sample task.</summary>
internal sealed class StartupSampleDriver(IStageGateHost host)
{
    private readonly StartupSampleInput _input = new(host);

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
            case StartupScenario.Immediate:
            case StartupScenario.Route: break;
            default: throw new InvalidOperationException("Unknown startup sampling scenario.");
        }
        host.CheckActive();
        if (host.Flow.State != AppState.Title)
            throw new InvalidOperationException("Startup input requires the real title menu.");
        await _input.Press(Key.Enter, false, "title-enter-press", "title-enter-release", _input.NextFrame);
        await _input.Wait(() => host.Flow.State != AppState.Title, "The normal title menu did not accept Enter.");
        if (scenario == StartupScenario.Route)
            await new StartupRouteDriver(host, _input).Run(AppBoot.Options.StartupRoute
                ?? throw new InvalidOperationException("The route startup scenario has no route options."));
    }
}
