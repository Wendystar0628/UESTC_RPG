using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Frame;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Registry;

// Command-line gate entries shared by the production hosts (stage, save node, world-state node).
public static partial class RegistryGateRunner
{
    /// <summary>--gate=registry: runs the declaration checks inside the engine and quits. False when the gate is not requested.</summary>
    public static bool TryRun(Node stage)
    {
        if (AppBoot.Options.Capture.Gate != "registry") return false;
        var report = Run(Kernel.Services, ProjectSettings.GetSetting("campus/maps/index").AsString(),
            AppBoot.Options.Flow.Map?.Value ?? "", ReadData, [typeof(IRuleset), typeof(IRulesetActivation), typeof(PaletteCatalog), typeof(IAppExit),
                typeof(Camera3D), typeof(WorldEnvironment), typeof(Godot.Environment), typeof(CanvasLayer)], DirAccess.GetFilesAt);
        GD.Print("G5_REGISTRY_RESULT " + JsonSerializer.Serialize(report, ContractJson.Options));
        stage.GetTree().Quit();
        return true;
    }

    /// <summary>Boot phase markers the core save gates read from the log.</summary>
    public static void TraceBootPhase(string phase)
    {
        if (AppBoot.Options.Capture.Gate is "save-close" or "save-resume")
            GD.Print($"CORE_BOOT_PHASE {phase} {FrameClock.MonotonicMilliseconds:R}");
    }

    /// <summary>Screenshot runs freeze the animation clock, except named cinematics and benchmarks.</summary>
    public static bool FreezesAnimation => AppBoot.Options.FreezeAnimation;

    private static byte[] ReadData(string path)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing required data: {path}");
        return bytes;
    }
}
