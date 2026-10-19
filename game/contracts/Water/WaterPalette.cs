namespace Campus.Contracts.Water;

/// <summary>Water colour names shared by generated surfaces and underwater presentation.</summary>
public static class WaterPalette
{
    public static readonly PaletteToken DeepLit = new("water.deep.lit");
    public static readonly PaletteToken DeepShade = new("water.deep.shade");
    public static readonly PaletteToken PoolTileLit = new("water.poolTile.lit");
    public static readonly PaletteToken PoolTileShade = new("water.poolTile.shade");
    public static readonly PaletteToken PoolLineLit = new("water.poolLine.lit");
    public static readonly PaletteToken PoolLineShade = new("water.poolLine.shade");
}
