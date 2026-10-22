using Campus.Contracts;
using Campus.Kernel.Diagnostics;
using Godot;
using Campus.Kernel.Registry;
using Campus.Contracts.Startup;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Boot;

public partial class Boot : Node
{
    public static BootRunOptions Options { get; private set; } = BootArguments.Parse([], true);
    internal static IReadOnlyList<FixedShot> FixedShots { get; private set; } = [];
    public static bool Failed { get; private set; }
    public override void _EnterTree()
    {
        DiagnosticLog.EnableGodotOutput();
        GetTree().AutoAcceptQuit = false;
        Failed = false;
        FixedShots = [];
        try
        {
            Options = BootArguments.Parse(OS.GetCmdlineUserArgs(), OS.IsDebugBuild(), out var fixedShots);
            FixedShots = fixedShots;
            KernelServices.Services.Add(Options);
        }
        catch (ArgumentException error) { Reject("Invalid or duplicate application argument:", error); return; }
        var profile = Path.GetFileName(OS.GetUserDataDir().TrimEnd('/', '\\'));
        GD.Print($"Storage profile: {profile}");
        if (BuildInfo.Kind == "dev" && !profile.StartsWith("UESTC_", StringComparison.Ordinal))
            Reject("Development build must use an isolated user directory.");
    }
    // Reject the command-line run with its original failure and a nonzero exit (ENG-GATES-SHOTS).
    private void Reject(string message, [FailureHandler("ENG-GATES-SHOTS")] Exception? error = null)
    {
        Failed = true;
        if (error is null) DiagnosticLog.Error("Boot", message);
        else DiagnosticLog.Error(message, error);
        GetTree().Quit(2);
    }
}
