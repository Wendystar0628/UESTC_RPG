using Campus.Kernel.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Godot;

namespace Campus.Kernel.Bake;

/// <summary>Map lifecycle only; generation and disk access stay on bake workers.</summary>
[KernelNode("bake")]
public partial class BakeRuntime : Node, IMapAware, IMapDataParticipant
{
    private readonly BakeService _service;
    private readonly BakeLightInventory _inventory;
    private BakeService.MapBuild? _map;
    public BakeRuntime(BakeService service, BakeLightInventory inventory)
    {
        _service = service;
        _inventory = inventory;
    }

    public async Task Prepare(IMapContext context)
    {
        _map = _service.Mount(context);
        var metadata = await _service.PrepareMapData(context);
        _inventory.Publish(metadata.Lighting);
    }
    public async Task UnloadAsync(MapId map)
    {
        var failures = new List<Exception>();
        try { await _service.DrainAsync(map); }
        catch (Exception error) { failures.Add(error); }
        try { _service.Unmount(map); }
        catch (Exception error) { failures.Add(error); }
        finally { _inventory.Clear(); _map = null; }
        if (failures.Count != 0) throw new AggregateException("Bake map unload failed.", failures);
    }

    public void OnMapLoaded(IMapContext context)
    {
        _map = _service.Mount(context);
        if (_map.Content.Metadata is null)
            throw new InvalidOperationException("Bake map preparation must complete before OnMapLoaded.");
        _service.Prebake(context);
    }
    public void OnMapUnloading(MapId from) { _ = _service.DrainAsync(from); _map = null; }

    public override void _Process(double delta)
    {
        while (_service.TryTakeDiagnostic(out var collected))
        {
            var diagnostic = collected!.Diagnostic;
            var message = $"[{collected.Owner}] map={collected.Map.Value} module={collected.Module} stage={collected.Stage} entity={diagnostic.Feature?.Value ?? "-"} {diagnostic.Code}: {diagnostic.Message}";
            switch (diagnostic.Level)
            {
                case DiagnosticLevel.Info: GD.Print(message); break;
                case DiagnosticLevel.Warning: DiagnosticLog.Warning("Bake", message); break;
                case DiagnosticLevel.Error: DiagnosticLog.Error("Bake", message); break;
                default: throw new InvalidDataException("Unknown build diagnostic level.");
            }
        }
    }
}
