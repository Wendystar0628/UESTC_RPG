using Godot;
using Campus.Kernel.Registry;
using System.Globalization;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Bake;

/// <summary>Resolved once on the main thread; workers never read engine settings or command-line state.</summary>
public sealed record BakeOptions(string CacheRoot, bool Live, bool Strict, bool Background,
    bool AllowUnlisted, bool Reference, bool Verify, bool MapOnly, int LogicalProcessors, uint? SeedOverride)
{
    // v5 invalidates geometry packed against map-wide height limits instead of actual surface heights.
    // Binary TileBundle payload remains version 2; old geometry/cache manifests must not be reused.
    public const string Abi = "godot-4.7.2-tbin-5";
    public int MaximumWorkers => Math.Max(1, LogicalProcessors - 2);
    public int PlayingWorkers => Math.Max(1, LogicalProcessors / 4);

    public static BakeOptions FromApplication() => FromArguments(AppBoot.Arguments);

    public static BakeOptions FromArguments(IReadOnlyDictionary<string, string> arguments)
    {
        string Value(string key, string fallback = "") => arguments.GetValueOrDefault(key, fallback);
        bool Flag(string name, bool fallback) => Value(name, fallback ? "1" : "0") switch
        {
            "0" => false,
            "1" => true,
            var value => throw new ArgumentException($"--{name} must be 0 or 1, got '{value}'.")
        };
        var root = Value("bake-cache");
        if (root.Length == 0)
            root = BuildInfo.Kind == "dev"
                ? System.Environment.GetEnvironmentVariable("UESTC_BAKE_CACHE") ?? @"E:\Project\UESTC_RPG.cache\bake"
                : ProjectSettings.GlobalizePath("user://cache/bake/");
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("--bake-cache must be an absolute path.");
        return new(Path.GetFullPath(root), Value("gen") == "live",
            Flag("bake-strict", false), Flag("bake-bg", true),
            BuildInfo.Kind == "dev" || arguments.ContainsKey("seed"),
            Value("bake") == "all", arguments.ContainsKey("bake-verify"),
            Value("bake") == "map", System.Environment.ProcessorCount,
            arguments.TryGetValue("seed", out var seed) ? uint.Parse(seed, CultureInfo.InvariantCulture) : null);
    }
}
