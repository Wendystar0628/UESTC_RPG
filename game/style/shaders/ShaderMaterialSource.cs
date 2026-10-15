using Campus.Contracts;
using Campus.Kernel.Materials;
using Godot;

namespace Campus.Style.Materials;

/// <summary>
/// 白名单里非卡通着色器的材质来源（contract-3；ENG-PERF §1.4）。每次返回一个新的 ShaderMaterial，
/// 调用方只写 uniform，不换着色器、不改 render_mode 和 #define。卡通系仍经 ToonSpec 走材质库。
/// 着色器文件归各自的包，这里只建材质。
/// </summary>
[KernelService(typeof(IShaderMaterialSource), Owner = "WP-04")]
public sealed class ShaderMaterialSource : IShaderMaterialSource
{
    private const string ShaderDirectory = "res://style/shaders/";
    private readonly Dictionary<ShaderId, Shader> _shaders = [];

    public ShaderMaterial Create(ShaderId shader) => new() { Shader = Load(shader) };

    /// <summary>着色器文件在不在（归属的包还没交付时为 false），供材质库在主线版里退到同类的已有着色器。</summary>
    public static bool Exists(ShaderId shader) => ResourceLoader.Exists(PathOf(shader), "Shader");

    public static string PathOf(ShaderId shader) => ShaderDirectory + shader switch
    {
        ShaderId.Sky => "sky",
        ShaderId.Water => "water",
        ShaderId.WaterClear => "water_clear",
        ShaderId.ReflectFlat => "reflect_flat",
        ShaderId.FxGlow => "fx_glow",
        ShaderId.FxFall => "fx_fall",
        ShaderId.Precip => "precip",
        ShaderId.VegTrunk => "veg_trunk",
        ShaderId.VegCard => "veg_card",
        ShaderId.VegImpostor => "veg_impostor",
        ShaderId.VegGrass => "veg_grass",
        ShaderId.VegLotus => "veg_lotus",
        ShaderId.DioramaSection => "diorama_section",
        ShaderId.DioramaFar => "diorama_far",
        ShaderId.MediumOverlay => "medium_overlay",
        ShaderId.CineCloud => "cine_cloud",
        ShaderId.UiPanel => "ui_panel",
        ShaderId.UiScreen => "ui_screen",
        ShaderId.MapBase => "map_base",
        _ => throw new ArgumentOutOfRangeException(nameof(shader), shader, "Shader ID is not registered in the material whitelist.")
    } + ".gdshader";

    private Shader Load(ShaderId shader)
    {
        if (_shaders.TryGetValue(shader, out var cached)) return cached;
        var path = PathOf(shader);
        if (!ResourceLoader.Exists(path, "Shader"))
            throw new FileNotFoundException($"Whitelisted shader '{shader}' is not available at '{path}'. Its owning work package must supply the shader.", path);
        var loaded = ResourceLoader.Load<Shader>(path)
            ?? throw new InvalidOperationException($"Failed to load whitelisted shader '{shader}' from '{path}'.");
        _shaders.Add(shader, loaded);
        return loaded;
    }
}
