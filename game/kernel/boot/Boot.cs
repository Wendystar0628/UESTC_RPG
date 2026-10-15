using Godot;
using Campus.Kernel.Registry;

namespace Campus.Kernel.Boot;

public partial class Boot : Node
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "gate", "map", "spawn", "pose", "clock", "season", "anim", "flow", "mode", "ui", "seed",
        "weather", "weather-intensity", "wet", "weather-seed", "weather-remaining", "weather-queue",
        "quality", "quality-set", "adaptive", "bench", "tier", "without", "inspect", "shots", "frames", "out",
        "gen", "bake", "bake-cache", "bake-bg", "bake-strict", "bake-verify", "title", "cine", "save", "slot",
        "load", "save-dir", "save-at", "audio", "audio-fixed", "ui-theme", "motion", "ui-scale", "ui-gallery",
        "ui-export-theme", "amb-solo", "amb-off", "amb-debug", "audio-probe", "audio-out", "preset"
    };
    public static IReadOnlyDictionary<string, string> Arguments { get; private set; } = new Dictionary<string, string>();
    public static bool Failed { get; private set; }
    public static string Value(string key, string fallback = "") => Arguments.TryGetValue(key, out var value) ? value : fallback;
    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal)) { Reject(arg); return; }
            var pair = arg[2..].Split('=', 2);
            if (!Allowed.Contains(pair[0]) || !arguments.TryAdd(pair[0], pair.Length == 2 ? pair[1] : "1"))
            { Reject(arg); return; }
        }
        Arguments = arguments;
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
