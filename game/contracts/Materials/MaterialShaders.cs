namespace Campus.Contracts;

/// <summary>Delivered shader families. Numeric identities are stable; removed IDs are not reused.</summary>
public enum ShaderId : byte
{
    Sky = 0,
    Water = 1,
    ReflectFlat = 3,
    FxGlow = 4,
    Precip = 6,
    VegTrunk = 7,
    VegCard = 8,
    VegGrass = 10,
    DioramaFar = 13,
    UiPanel = 16,
    UiScreen = 17,
    MapBase = 18,
    VegTwigMass = 19,
    ToonStatic = 20,
    ToonGlass = 21,
    ToonSceneText = 22
}

/// <summary>Single shader file registry shared by build preparation and runtime material creation.</summary>
public static class MaterialShaders
{
    /// <summary>Project-relative resource path; the caller adds res:// or the repository's game/ prefix.</summary>
    public static string FileOf(ShaderId shader) => "style/shaders/" + (shader switch
    {
        ShaderId.Sky => "sky",
        ShaderId.Water => "water",
        ShaderId.ReflectFlat => "reflect_flat",
        ShaderId.FxGlow => "fx_glow",
        ShaderId.Precip => "precip",
        ShaderId.VegTrunk => "veg_trunk",
        ShaderId.VegCard => "veg_card",
        ShaderId.VegGrass => "veg_grass",
        ShaderId.DioramaFar => "diorama_far",
        ShaderId.UiPanel => "ui_panel",
        ShaderId.UiScreen => "ui_screen",
        ShaderId.MapBase => "map_base",
        ShaderId.VegTwigMass => "veg_twig_mass",
        ShaderId.ToonStatic => "toon_static",
        ShaderId.ToonGlass => "toon_glass",
        ShaderId.ToonSceneText => "toon_scene_text",
        _ => throw new ArgumentOutOfRangeException(nameof(shader), shader, "Shader ID is not registered.")
    }) + ".gdshader";

    public static ShaderId ShaderFor(ToonKind kind) => kind switch
    {
        ToonKind.Static => ShaderId.ToonStatic,
        ToonKind.Glass or ToonKind.GlassNear => ShaderId.ToonGlass,
        ToonKind.VegTrunk => ShaderId.VegTrunk,
        ToonKind.VegCard => ShaderId.VegCard,
        ToonKind.VegGrass => ShaderId.VegGrass,
        ToonKind.SceneText => ShaderId.ToonSceneText,
        ToonKind.VegTwigMass => ShaderId.VegTwigMass,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Toon kind is not registered.")
    };
}
