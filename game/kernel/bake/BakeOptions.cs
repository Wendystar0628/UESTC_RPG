using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;
using Campus.Contracts.Startup;

namespace Campus.Kernel.Bake;

/// <summary>Resolved once on the main thread; workers never read engine settings or command-line state.</summary>
public sealed record BakeOptions(string CacheRoot, bool Live, bool Strict, bool Background,
    bool MapOnly, int LogicalProcessors, uint? SeedOverride)
{
    public const string Abi = "godot-4.7.2-tbin-19";
    public int MaximumWorkers => Math.Max(1, LogicalProcessors - 2);
    public int PlayingWorkers => Math.Max(1, LogicalProcessors / 4);

    public static BakeOptions FromApplication() => From(KernelServices.Get<BootRunOptions>().Bake);

    public static BakeOptions From(BakeBootOptions options)
    {
        var root = System.Environment.GetEnvironmentVariable("UESTC_BAKE_CACHE") ??
            Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "output", "cache", "bake");
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("UESTC_BAKE_CACHE must be an absolute path.");
        return new(Path.GetFullPath(root), options.Live, options.Strict, options.Background,
            options.MapOnly, System.Environment.ProcessorCount, options.Seed);
    }
}
