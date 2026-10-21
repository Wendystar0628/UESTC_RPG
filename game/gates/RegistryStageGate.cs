using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Frame;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

// Command-line gate entries invoked by the production stage.
public static class RegistryStageGate
{
    /// <summary>--gate=registry: runs the declaration checks inside the engine. The stage owns shutdown. False when the gate is not requested.</summary>
    public static bool TryRun()
    {
        if (AppBoot.Options.Capture.Gate != "registry") return false;
        var report = RegistryGateRunner.Run(KernelServices.Services, ProjectSettings.GetSetting("campus/maps/index").AsString(),
            AppBoot.Options.Flow.Map?.Value ?? "", ReadData, [typeof(IAppExit),
                typeof(Camera3D), typeof(WorldEnvironment), typeof(Godot.Environment), typeof(CanvasLayer)], DirAccess.GetFilesAt);
        GD.Print("G5_REGISTRY_RESULT " + JsonSerializer.Serialize(report, ContractJson.Options));
        return true;
    }

    /// <summary>Boot phase markers the core save gates read from the log.</summary>
    public static void TraceBootPhase(string phase)
    {
        if (AppBoot.Options.Capture.Gate is "save-close" or "save-resume")
            GD.Print($"CORE_BOOT_PHASE {phase} {FrameClock.MonotonicMilliseconds:R}");
    }

    private static byte[] ReadData(string path)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing required data: {path}");
        return bytes;
    }
}
