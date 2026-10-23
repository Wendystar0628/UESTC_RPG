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
        Task? capture = null;
        try
        {
            if (AppBoot.Options.Capture.Shots.Count > 0)
            {
                capture = KernelServices.Get<GameCaptureNode>().RunSequenceAsync(host.Stage, host.Readiness, AppBoot.Options, host.StartupCapture, host.SyncView, () =>
                {
                    host.CheckActive();
                    return host.Ready;
                }, host.Lifetime, pose =>
                {
                    if (!KernelServices.Get<ICameraPoseValidator>().IsLegal(pose, outsideBuffer: true))
                        throw new ArgumentException("Capture pose is outside the reachable space or inside its buffer.");
                });
                await capture;
            }
            await host.Close();
        }
        // The capture service links its token; the cancelled child task still belongs to this cancelled stage.
        catch (OperationCanceledException error) when (host.Closing && host.Lifetime.IsCancellationRequested &&
            (error.CancellationToken == host.Lifetime || capture?.IsCanceled == true)) { }
        catch (Exception error) { host.Fail(error); }
    }
}
