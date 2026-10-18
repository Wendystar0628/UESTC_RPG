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
        if (AppBoot.Value("gate") != "registry") return false;
        var report = Run(Kernel.Services, ProjectSettings.GetSetting("campus/maps/index").AsString(),
            AppBoot.Value("map"), ReadData, [typeof(IRuleset), typeof(IRulesetActivation), typeof(PaletteCatalog), typeof(IAppExit),
                typeof(Camera3D), typeof(WorldEnvironment), typeof(Godot.Environment), typeof(CanvasLayer)]);
        GD.Print("G5_REGISTRY_RESULT " + JsonSerializer.Serialize(report, ContractJson.Options));
        stage.GetTree().Quit();
        return true;
    }

    /// <summary>Boot phase markers the core save gates read from the log.</summary>
    public static void TraceBootPhase(string phase)
    {
        if (AppBoot.Value("gate") is "save-close" or "save-resume")
            GD.Print($"CORE_BOOT_PHASE {phase} {FrameClock.MonotonicMilliseconds:R}");
    }

    /// <summary>--save-at: frame at which the save service writes one test save.</summary>
    public static int? SaveAtFrame(IReadOnlyDictionary<string, string> arguments) =>
        arguments.TryGetValue("save-at", out var value) ? int.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : null;

    /// <summary>Screenshot runs freeze the animation clock, except named cinematics and benchmarks.</summary>
    public static bool FreezesAnimation =>
        AppBoot.Arguments.ContainsKey("shots") && !AppBoot.Arguments.ContainsKey("cine") && !AppBoot.Arguments.ContainsKey("bench");

    private static byte[] ReadData(string path)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing required data: {path}");
        return bytes;
    }
}
