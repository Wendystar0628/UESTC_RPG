using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Bake;

/// <summary>Map lifecycle only; generation and disk access stay on bake workers.</summary>
public partial class BakeRuntime : Node, IMapAware, IMapDataParticipant
{
    private readonly BakeService _service;
    private readonly BakeLightInventory _inventory;
    private BakeService.MapBuild? _map;
    private bool _finished;
    public BakeRuntime(IBakeService service, ILightInventory inventory)
    {
        _service = service as BakeService ?? throw new InvalidOperationException("Bake runtime requires the registered bake service.");
        _inventory = inventory as BakeLightInventory ?? throw new InvalidOperationException("Bake runtime requires the registered light inventory.");
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
        if (_map.Metadata is null)
            throw new InvalidOperationException("Bake map preparation must complete before OnMapLoaded.");
        _finished = false;
        _service.Prebake(context);
    }
    public void OnMapUnloading(MapId from) => _map = null;

    public override void _Process(double delta)
    {
        while (_service.TryTakeError(out var error)) GD.PushError("[WP-02a] " + error);
        if (_finished || _map is null) return;
        var progress = _service.Progress;
        if (AppBoot.Value("bake-strict") == "1" && progress.Stage.StartsWith("生成失败", StringComparison.Ordinal))
        {
            _finished = true; GD.PushError("WP-02a strict bake failure."); GetTree().Quit(2); return;
        }
    }
    public override void _ExitTree() => _service.Dispose();
}
