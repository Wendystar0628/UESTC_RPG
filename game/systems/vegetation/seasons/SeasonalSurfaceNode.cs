using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Content;
using Campus.Contracts.Maps;
using Campus.Contracts.Vegetation;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Vegetation;

/// <summary>Publishes the per-species render table and seasonal surface amounts. Seasonal anchors are
/// evaluated once by the colour script; weather owns snow accumulation and its world-time decay.</summary>
[KernelNode("systems.season-globals", Owner = "WP-38")]
public sealed partial class SeasonalSurfaceNode : Node, IMapAware
{
    private static readonly StringName _shaderGlobalTreeSpecies = ShaderGlobals.TreeSpecies;
    private static readonly StringName _shaderGlobalSnow = ShaderGlobals.Snow;
    private static readonly StringName _shaderGlobalGroundAmt = ShaderGlobals.GroundAmt;

    private const string ModuleId = "vegetation";
    /// <summary>Texels per species row (= SpeciesTable.Width and the veg_common fetches).</summary>
    private const int TableWidth = 8;

    private IWorldState? _world;
    private IColorScriptFrame? _script;
    private ImageTexture? _table;
    private float _snow = float.NaN;
    private Vector4 _groundAmt = new(float.NaN, 0f, 0f, 0f);

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        _world = Services.Get<IWorldState>();
        _script = Services.Get<IColorScriptFrame>();
        WriteSnow();
    }

    public override void _Process(double delta) => WriteSnow();

    public void OnMapUnloading(MapId from) { }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var producer = Services.Get<IMapContentSource>().GetModule<IVegetationRuntimeSource>(ctx, ModuleId);
        var data = producer.PrepareSpeciesTable(VegetationContext.Resolve(ctx, ModuleId, Services.Get<IBakeContextSource>()));
        var rows = data.Length / (TableWidth * 4);
        if (rows == 0 || rows * TableWidth * 4 != data.Length)
            throw new InvalidDataException($"Species table has {data.Length} floats, not whole rows of {TableWidth} RGBA texels.");
        var image = Image.CreateFromData(TableWidth, rows, false, Image.Format.Rgbaf, MemoryMarshal.AsBytes(data.AsSpan()));
        if (_table is null) _table = ImageTexture.CreateFromImage(image);
        else _table.SetImage(image);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalTreeSpecies, _table);
    }

    private void WriteSnow()
    {
        if (_world is null || _script is null) return;
        var surface = _script.Current.SeasonSurface;
        var groundSnow = Math.Clamp(Math.Max(surface.Z, _world.WeatherNow.SnowAdd), 0f, 1.5f);
        var snow = Math.Min(groundSnow, 1f);
        if (snow != _snow)
        {
            _snow = snow;
            RenderingServer.GlobalShaderParameterSet(_shaderGlobalSnow, snow);
        }
        // Crown snow caps at one; ground keeps the full 1.5 range, normalized for the global texture contract.
        var groundAmt = new Vector4(surface.X, surface.Y, groundSnow / 1.5f, 0f);
        if (groundAmt == _groundAmt) return;
        _groundAmt = groundAmt;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalGroundAmt, groundAmt);
    }
}
