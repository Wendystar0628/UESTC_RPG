using Campus.Contracts;
using Campus.Kernel.Inspect;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Captures an automatic command-line run after readiness, then closes its production stage.</summary>
internal static class StageCapture
{
    public static async Task FinishRun(IStageGateHost host)
    {
        try
        {
            if (AppBoot.Options.Capture.Shots.Count > 0)
                await KernelServices.Get<GameCaptureNode>().RunSequenceAsync(host.Stage, host.Readiness, AppBoot.Options, host.SyncView, () =>
                {
                    host.CheckActive();
                    return host.Ready;
                }, host.Lifetime, pose =>
                {
                    if (!KernelServices.Get<ICameraPoseValidator>().IsLegal(pose, outsideBuffer: true))
                        throw new ArgumentException("Capture pose is outside the reachable space or inside its buffer.");
                });
            await host.Close();
        }
        catch (OperationCanceledException) when (host.Closing) { }
        catch (Exception error) { host.Fail(error); }
    }
}
