namespace Campus.Contracts.Water;

/// <summary>Water colour names shared by generated surfaces and underwater presentation (ART-STYLE).</summary>
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
    /// Water surface material shared by all generated water bodies; palette token values stay fixed.
    /// The kernel selects the water shader through its material whitelist.
    /// Surface lighting and outline settings are authored here once.
    /// </summary>
    public static ToonSpec Surface { get; } = new(FarLit, FarShade)
    {
        Kind = ToonKind.Static,
        Line = LineClass.None,
        Wet = 0f,
    };

    public static readonly PaletteToken DeepLit = new("water.deep.lit");
    public static readonly PaletteToken DeepShade = new("water.deep.shade");
    public static readonly PaletteToken PoolTileLit = new("water.poolTile.lit");
    public static readonly PaletteToken PoolTileShade = new("water.poolTile.shade");
    public static readonly PaletteToken PoolLineLit = new("water.poolLine.lit");
    public static readonly PaletteToken PoolLineShade = new("water.poolLine.shade");
}
