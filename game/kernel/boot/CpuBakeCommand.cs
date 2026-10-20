using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Startup;
using Campus.Kernel.Bake;
using Campus.Kernel.Frame;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using Godot;

namespace Campus.Kernel.Boot;

/// <summary>Loads frozen map inputs and runs CPU baking without the interactive service graph.</summary>
[KernelService(typeof(IBakeCommand), Owner = "WP-01")]
public sealed class CpuBakeCommand : IBakeCommand
{
    private readonly GlobalBuildInputs _globalInputs;

    public CpuBakeCommand(GlobalBuildInputs globalInputs) => _globalInputs = globalInputs;

    public async Task<int> Run(BootContext context)
    {
        var options = BakeOptions.From(context.Run.Bake);
        var started = FrameClock.MonotonicMilliseconds;
        var exitCode = 2;
        try
        {
            using var terrain = new TerrainCacheService(options);
            using var bake = new BakeService(options, terrain, _globalInputs.Snapshot);
            var source = new MapDataSource();
            var catalog = await source.ReadCatalog();
            var requested = context.Run.Flow.Map ?? catalog.Default;
            var maps = requested.Value == "*" ? catalog.Maps : catalog.Maps.Where(map => map.Id == requested).ToArray();
            if (maps.Count == 0) throw new ArgumentException($"Unknown bake map: {requested.Value}.");
            var pipelines = requested.Value == "*" && !options.MapOnly ? new BakePipelineCatalog() : null;
            var loader = new WorldModelLoaderAdapter(terrain);
            foreach (var descriptor in maps)
            {
                if (!catalog.TryGet(descriptor.Id, out var entry)) throw new InvalidDataException("Map catalogue entry is missing.");
                var input = source.ReadLoadInput(entry);
                var claims = source.ReadClaims(descriptor.Id);
                var loaded = await Task.Run(() => MapWorldLoading.LoadPrepared(loader, input, claims));
                var map = MapWorldLoading.CreateContext(input.Entry, loaded, input.SourceInputs);
                try
                {
                    var metadata = await bake.PrepareMapData(map);
                    await bake.BakeOffline(map, pipelines);
                    if (metadata.Lighting.WindowNumbering is { } numbering)
                    {
                        var path = ProjectSettings.GlobalizePath(map.DataRoot + WindowNumberingRegistry.FileName);
                        var bytes = JsonSerializer.SerializeToUtf8Bytes(WindowNumberingRegistry.FromSnapshot(numbering), ContractJson.Options);
                        if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) BakeCacheStore.AtomicWrite(path, bytes);
                    }
                    while (bake.TryTakeDiagnostic(out var collected))
                    {
                        var diagnostic = collected!.Diagnostic;
                        GD.Print($"BAKE_DIAGNOSTIC map={collected.Map.Value} module={collected.Module} level={diagnostic.Level} {diagnostic.Code}: {diagnostic.Message}");
                    }
                    GD.Print($"BAKE_MAP_COMPLETE map={descriptor.Id.Value}");
                }
                finally { bake.Unmount(map.Descriptor.Id); }
            }
            pipelines?.Write(ProjectSettings.GlobalizePath("res://"));
            exitCode = 0;
        }
        catch (Exception error) { GD.PushError(error.ToString()); }
        finally
        {
            var files = Directory.Exists(options.CacheRoot)
                ? Directory.EnumerateFiles(options.CacheRoot, "*.tbin", SearchOption.AllDirectories).LongCount() : 0;
            GD.Print(FormattableString.Invariant($"PREBAKE_COMPLETE exit={exitCode} files={files} elapsed_seconds={(FrameClock.MonotonicMilliseconds - started) / 1000:0.000}"));
        }
        return exitCode;
    }
}
