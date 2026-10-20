namespace Campus.Contracts;

/// <summary>Godot visual layer masks registered by ENG-PERF. Names are masks, never one-based indices.</summary>
public static class RenderLayers
{
    public const uint BuildingsAndProps = 1u << 0;
    public const uint Ground = 1u << 1;
    public const uint SmallCasters = 1u << 2;
    public const uint Vegetation = 1u << 3;
    public const uint ShadowProxy = 1u << 4;
    public const uint GrassAndEffects = 1u << 5;
    public const uint Underground = 1u << 6;
    public const uint Interiors = 1u << 7;
    public const uint Water = 1u << 8;
    public const uint FarRing = 1u << 9;
    public const uint NonCasters = Ground | Vegetation | GrassAndEffects | Underground | Water | FarRing;
}
