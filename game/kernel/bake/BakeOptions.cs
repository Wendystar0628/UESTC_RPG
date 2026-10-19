using Godot;
using Campus.Kernel.Registry;
using Campus.Contracts.Startup;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Bake;

/// <summary>Resolved once on the main thread; workers never read engine settings or command-line state.</summary>
public sealed record BakeOptions(string CacheRoot, bool Live, bool Strict, bool Background,
    bool MapOnly, int LogicalProcessors, uint? SeedOverride)
{
    public const string Abi = "godot-4.7.2-tbin-11";
    public string? PackagedCacheRoot { get; init; }
    public int MaximumWorkers => Math.Max(1, LogicalProcessors - 2);
    public int PlayingWorkers => Math.Max(1, LogicalProcessors / 4);

    public static BakeOptions FromApplication() => From(AppBoot.Options.Bake);

    public static BakeOptions From(BakeBootOptions options)
    {
        var development = string.Equals(BuildInfo.Kind, "dev", StringComparison.Ordinal);
        var root = options.CacheDirectory;
        if (string.IsNullOrEmpty(root))
            root = development
                ? System.Environment.GetEnvironmentVariable("UESTC_BAKE_CACHE") ??
                    Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "output", "cache", "bake")
                : ProjectSettings.GlobalizePath("user://cache/bake/");
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("--bake-cache must be an absolute path.");
        return new(Path.GetFullPath(root), options.Live, options.Strict, options.Background,
            options.MapOnly, System.Environment.ProcessorCount, options.Seed)
        {
            PackagedCacheRoot = development ? null : Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "cache", "bake")
        };
    }
}
