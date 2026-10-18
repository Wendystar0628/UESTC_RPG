using Campus.Contracts;

namespace Campus.Gen.Water;

/// <summary>
/// Water colour names registered in game/style/palette/ext/water.json (ART-STYLE).
/// Columns are fixed once registered; water.gdshader reads the same columns by number.
/// </summary>
public static class WaterPalette
{
    public static readonly PaletteToken NearLit = new("water.near.lit");
    public static readonly PaletteToken NearShade = new("water.near.shade");
    public static readonly PaletteToken MidLit = new("water.mid.lit");
    public static readonly PaletteToken MidShade = new("water.mid.shade");
    public static readonly PaletteToken FarLit = new("water.far.lit");
    public static readonly PaletteToken FarShade = new("water.far.shade");
    public static readonly PaletteToken FoamLit = new("water.foam.lit");
    public static readonly PaletteToken FoamShade = new("water.foam.shade");

    /// <summary>
    /// The surface family written into the vertex colour byte. The shader itself (water.gdshader) is chosen by the
    /// kernel's whitelist shader material source (Create(ShaderId), being added by WP-00 / WP-04); this spec is the only
    /// place the water module names its material, so switching to that source touches one line.
    /// </summary>
    public static ToonSpec Surface { get; } = new(FarLit, FarShade)
    {
        Kind = ToonKind.Static,
        Line = LineClass.None,
        Wet = 0f,
    };
}
