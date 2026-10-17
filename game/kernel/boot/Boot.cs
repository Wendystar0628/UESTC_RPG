using Godot;
using Campus.Kernel.Registry;

namespace Campus.Kernel.Boot;

public partial class Boot : Node
{
    public static BootArguments Options { get; private set; } = BootArguments.Parse([], true);
    // Consumers distinguish explicit inputs from the effective defaults in Options.
    public static IReadOnlyDictionary<string, string> Arguments { get; private set; } = new Dictionary<string, string>();
    public static bool Failed { get; private set; }
    public static string Value(string key, string fallback = "") => Arguments.TryGetValue(key, out var value) ? value : fallback;
    public override void _EnterTree()
    {
        GetTree().AutoAcceptQuit = false;
        Failed = false;
        try
        {
            Options = BootArguments.Parse(OS.GetCmdlineUserArgs(), OS.IsDebugBuild());
            Arguments = Options.ExplicitArguments.ToDictionary(key => key, key => Options.Value(key), StringComparer.Ordinal);
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
