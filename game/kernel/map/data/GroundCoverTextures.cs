using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Godot;

namespace Campus.Kernel.Maps;

/// <summary>Retains this map's baked CPU ground masks and uploads them on the engine thread.</summary>
[KernelNode("maps.ground-cover")]
public sealed partial class GroundCoverTextures(IBakeContextSource builds)
    : Node, IMapDataParticipant, IMapAware
{
    private static readonly StringName MaskGlobal = ShaderGlobals.GroundMask;
    private static readonly StringName Mask2Global = ShaderGlobals.GroundMask2;
    private GroundCoverData? _data;
    private ImageTexture? _mask, _mask2;

    public Task Prepare(IMapContext context)
    {
        _data = builds.GetPreparedGroundCover(context);
        return Task.CompletedTask;
    }

    public void OnMapLoaded(IMapContext context)
    {
        var data = _data ?? throw new InvalidOperationException("Ground cover must be prepared before map load.");
        using var image = Image.CreateFromData(data.Grid.Width, data.Grid.Height, false, Image.Format.Rgba8, data.Mask1);
        using var image2 = Image.CreateFromData(data.Grid.Width, data.Grid.Height, false, Image.Format.Rgba8, data.Mask2);
        _mask = ImageTexture.CreateFromImage(image);
        _mask2 = ImageTexture.CreateFromImage(image2);
        RenderingServer.GlobalShaderParameterSet(MaskGlobal, _mask);
        RenderingServer.GlobalShaderParameterSet(Mask2Global, _mask2);
    }

    public void OnMapUnloading(MapId from) => Release();
    public Task UnloadAsync(MapId from) { _data = null; return Task.CompletedTask; }
    public override void _ExitTree() { Release(); _data = null; }

    private void Release()
    {
        RenderingServer.GlobalShaderParameterSet(MaskGlobal, default(Variant));
        RenderingServer.GlobalShaderParameterSet(Mask2Global, default(Variant));
        _mask?.Dispose(); _mask = null;
        _mask2?.Dispose(); _mask2 = null;
    }
}
