using Campus.Kernel.Diagnostics;
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
[KernelService(typeof(IBakeCommand))]
public sealed class CpuBakeCommand : IBakeCommand
{
    private readonly GlobalBuildInputs _globalInputs;

    private readonly BuildCodeIndex _codeFingerprints;
    private readonly IRequiredResources _resources;

    public CpuBakeCommand(GlobalBuildInputs globalInputs, BuildCodeIndex codeFingerprints, IRequiredResources resources)
    { _globalInputs = globalInputs; _codeFingerprints = codeFingerprints; _resources = resources; }

    public async Task<int> Run(BootContext context)
    {
        var options = BakeOptions.From(context.Run.Bake);
        var started = FrameClock.MonotonicMilliseconds;
        var exitCode = 2;
        TerrainCacheService? terrain = null;
        BakeService? bake = null;
        var failures = new List<Exception>();
        try
        {
            terrain = new TerrainCacheService(options, _codeFingerprints);
            bake = new BakeService(options, terrain, _globalInputs.Snapshot);
            var source = new MapDataSource(_resources);
            var catalog = await source.ReadCatalog();
            var requested = context.Run.Flow.Map ?? catalog.Default;
            var maps = context.Run.AllMaps ? catalog.Maps : catalog.Maps.Where(map => map.Id == requested).ToArray();
            if (maps.Count == 0) throw new ArgumentException($"Unknown bake map: {requested.Value}.");
            var pipelines = context.Run.AllMaps && !options.MapOnly ? new BakePipelineCatalog() : null;
            var loader = new WorldModelLoaderAdapter(terrain);
            foreach (var descriptor in maps)
            {
                if (!catalog.TryGet(descriptor.Id, out var entry)) throw new InvalidDataException("Map catalogue entry is missing.");
                var input = await source.ReadLoadInputAsync(entry);
                var claims = source.ReadClaims(descriptor.Id);
                var loaded = await Task.Run(() => MapWorldLoading.LoadPrepared(loader, input, claims));
                var map = MapWorldLoading.CreateContext(input.Entry, loaded, input.SourceInputs);
                var mapFailures = new List<Exception>();
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
                }
                catch (Exception error) { mapFailures.Add(error); }
                finally
                {
                    try { await bake.DrainAsync(map.Descriptor.Id); }
                    catch (Exception error) { mapFailures.Add(error); }
                    try { bake.Unmount(map.Descriptor.Id); }
                    catch (Exception error) { mapFailures.Add(error); }
                }
                if (mapFailures.Count != 0) throw new AggregateException("CPU map bake failed.", mapFailures);
                GD.Print($"BAKE_MAP_COMPLETE map={descriptor.Id.Value}");
            }
            pipelines?.Write(ProjectSettings.GlobalizePath("res://"));
            exitCode = 0;
        }
        catch (Exception error) { failures.Add(error); }
        finally
        {
            if (bake is not null)
                try { await bake.DisposeAsync(); }
                catch (Exception error) { failures.Add(error); }
            if (terrain is not null)
                try { await terrain.DisposeAsync(); }
                catch (Exception error) { failures.Add(error); }
            long files = 0;
            try
            {
                files = Directory.Exists(options.CacheRoot)
                    ? Directory.EnumerateFiles(options.CacheRoot, "*.tbin", SearchOption.AllDirectories).LongCount() : 0;
            }
            catch (Exception error) { failures.Add(error); }
            if (failures.Count != 0) exitCode = FailedCommand(new AggregateException("CPU bake command failed.", failures));
            GD.Print(FormattableString.Invariant($"PREBAKE_COMPLETE exit={exitCode} files={files} elapsed_seconds={(FrameClock.MonotonicMilliseconds - started) / 1000:0.000}"));
        }
        return exitCode;
    }

    private static int FailedCommand([FailureHandler("ENG-ARCH")] Exception error)
    {
        DiagnosticLog.Error("CPU bake command failed.", error);
        return 2;
    }
}
