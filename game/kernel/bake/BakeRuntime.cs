using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;
using Campus.Contracts.Startup;

namespace Campus.Kernel.Bake;

/// <summary>Map lifecycle only; generation and disk access stay on bake workers.</summary>
[KernelNode("bake")]
public partial class BakeRuntime : Node, IMapAware, IMapDataParticipant
{
    private readonly BakeService _service;
    private readonly BakeLightInventory _inventory;
    private BakeService.MapBuild? _map;
    private bool _finished;
    private bool _strict;
    public override void _Ready() => _strict = KernelServices.Get<BootRunOptions>().Bake.Strict;
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
    public void Unload(MapId map) { _service.Unmount(map); _inventory.Clear(); _map = null; }

    public void OnMapLoaded(IMapContext context)
    {
        _map = _service.Mount(context);
        if (_map.Content.Metadata is null)
            throw new InvalidOperationException("Bake map preparation must complete before OnMapLoaded.");
        _finished = false;
        _service.Prebake(context);
    }
    public void OnMapUnloading(MapId from) => _map = null;

    public override void _Process(double delta)
    {
        while (_service.TryTakeError(out var error)) GD.PushError("[WP-02a] " + error);
        while (_service.TryTakeDiagnostic(out var collected))
        {
            var diagnostic = collected!.Diagnostic;
            var message = $"[{collected.Owner}] map={collected.Map.Value} module={collected.Module} stage={collected.Stage} entity={diagnostic.Feature?.Value ?? "-"} {diagnostic.Code}: {diagnostic.Message}";
            switch (diagnostic.Level)
            {
                case DiagnosticLevel.Info: GD.Print(message); break;
                case DiagnosticLevel.Warning: GD.PushWarning(message); break;
                case DiagnosticLevel.Error: GD.PushError(message); break;
                default: throw new InvalidDataException("Unknown build diagnostic level.");
            }
        }
        if (_finished || _map is null) return;
        var progress = _service.Progress;
        if (_strict && progress.Stage.StartsWith("生成失败", StringComparison.Ordinal))
        {
            _finished = true; GD.PushError("WP-02a strict bake failure."); GetTree().Quit(2); return;
        }
    }
}
