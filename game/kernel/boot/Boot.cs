using Godot;
using Campus.Kernel.Registry;
using Campus.Contracts.Startup;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Boot;

public partial class Boot : Node
{
    public static BootRunOptions Options { get; private set; } = BootArguments.Parse([], true);
    internal static IReadOnlyList<FixedShot> FixedShots { get; private set; } = [];
    public static bool Failed { get; private set; }
    public override void _EnterTree()
    {
        GetTree().AutoAcceptQuit = false;
        Failed = false;
        FixedShots = [];
        try
        {
            Options = BootArguments.Parse(OS.GetCmdlineUserArgs(), OS.IsDebugBuild(), out var fixedShots);
            FixedShots = fixedShots;
            Services.Services.Add(Options);
        }
        catch (ArgumentException error) { Reject(error.Message); return; }
        var profile = Path.GetFileName(OS.GetUserDataDir().TrimEnd('/', '\\'));
        GD.Print($"Storage profile: {profile}");
        if (BuildInfo.Kind == "dev" && !profile.StartsWith("UESTC_", StringComparison.Ordinal))
            Reject("Development build must use an isolated user directory.");
    }
    private void Reject(string argument)
    {
        Failed = true;
        GD.PushError($"Invalid or duplicate application argument: {argument}");
        GetTree().Quit(2);
    }
}
