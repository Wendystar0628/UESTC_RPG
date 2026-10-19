using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Content;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Contracts.Vegetation;
using Campus.Kernel.Maps;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Vegetation;

/// <summary>
/// WP-38's seasonal globals (WLD-VEGETATION): the tree_species table that the veg_* shaders read for leaf amount, bloom,
/// bare twigs and the per-species season rows, uploaded on map load from the vegetation module's packed table; and the
/// snow cover, the maximum of the eased seasonal baseline and the weather process's SnowAdd. Weather owns the snow
/// residue and its world-time decay; this node only publishes the current combined amount.
/// </summary>
[KernelNode("systems.season-globals", Owner = "WP-38")]
public sealed partial class SeasonGlobalsNode : Node, IMapAware
{
    private static readonly StringName _shaderGlobalTreeSpecies = ShaderGlobals.TreeSpecies;
    private static readonly StringName _shaderGlobalSnow = ShaderGlobals.Snow;

    private const string ModuleId = "vegetation";
    /// <summary>Texels per species row (= SpeciesTable.Width and the veg_common fetches).</summary>
    private const int TableWidth = 8;
    /// <summary>Snow cover at the season anchors spring, summer, autumn, winter: winter without snowfall is 0.7 (WLD-VEGETATION).</summary>
    private static readonly float[] SnowAnchors = [0f, 0f, 0f, 0.7f];

    private IWorldState? _world;
    private ImageTexture? _table;
    private float _snow = float.NaN;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        _world = Services.Get<IWorldState>();
        WriteSnow();
    }

    public override void _Process(double delta) => WriteSnow();

    public void OnMapUnloading(MapId from) { }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        float[] data;
        try
        {
            var producer = Services.Get<IMapContentSource>().GetModule<IVegetationRuntimeSource>(ctx, ModuleId);
            data = producer.PrepareSpeciesTable(VegetationContext.Resolve(ctx, ModuleId, Services.Get<IBakeContextSource>())());
        }
        catch (Exception error)
        {
            GD.PushError($"Species table unavailable on {ctx.Descriptor.Id.Value}, trees keep a full crown: {error}");
            return;
        }
        var rows = data.Length / (TableWidth * 4);
        if (rows == 0 || rows * TableWidth * 4 != data.Length)
        {
            GD.PushError($"Species table has {data.Length} floats, not whole rows of {TableWidth} RGBA texels.");
            return;
        }
        var image = Image.CreateFromData(TableWidth, rows, false, Image.Format.Rgbaf, MemoryMarshal.AsBytes(data.AsSpan()));
        if (_table is null) _table = ImageTexture.CreateFromImage(image);
        else _table.SetImage(image);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalTreeSpecies, _table);
    }

    private void WriteSnow()
    {
        if (_world is null) return;
        var snow = Math.Clamp(Math.Max(SnowAt(_world.SeasonPos), _world.WeatherNow.SnowAdd), 0f, 1f);
        if (snow == _snow) return;
        _snow = snow;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalSnow, snow);
    }

    /// <summary>Snow cover at a season position 0–4: smootherstep between neighbouring anchors, winter wraps to spring (as veg_anchor).</summary>
    internal static float SnowAt(double season)
    {
        var x = season % 4.0;
        if (x < 0) x += 4.0;
        var i = (int)Math.Floor(x);
        var f = (float)(x - i);
        var t = f * f * f * (f * (f * 6f - 15f) + 10f);
        return SnowAnchors[i % 4] + (SnowAnchors[(i + 1) % 4] - SnowAnchors[i % 4]) * t;
    }
}
