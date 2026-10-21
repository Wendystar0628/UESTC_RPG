using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Buildings.Rules;

/// <summary>
/// Construction pattern lines of the main wall (ART-STYLE-SHADING, WLD-RENOVATION, D-195): painted walls
/// are cut into 1.5 × 0.75 m panels, identity-tile buildings carry the tile grid. Both are lightness-only lines drawn by
/// toon_static from the wall's metre UV (U along the wall, V up from the plinth top); the shader fades them out per cell
/// pixel size, so there is no distance-banded colour block anywhere.
/// </summary>
public enum WallPattern : byte
{
    /// <summary>No pattern (every non-wall class).</summary>
    None = 0,
    /// <summary>Painted wall: 1.5 × 0.75 m panels, joints aligned.</summary>
    Paint,
    /// <summary>Facing tile: 240 × 60 mm running bond.</summary>
    Tile,
}

public static class WallPatterns
{
    /// <summary>game/style/ layers (game/style/shaders/SurfacePatternLayers.cs). Stable; never reused.</summary>
    public const ushort StackBondLayer = 1;
    public const ushort RunningBondLayer = 2;

    /// <summary>Painted panels: 1.5 m wide, 0.75 m high including a 10 mm joint (trial value).</summary>
    public static readonly SurfacePatternSpec Paint = new(StackBondLayer, new(1.5f, 0.75f), new(0.01f, 0.01f));

    /// <summary>Facing tile: 240 × 60 mm including a 5 mm joint, courses along the wall, vertical joints staggered by half a tile (trial values).</summary>
    public static readonly SurfacePatternSpec Tile = new(RunningBondLayer, new(0.24f, 0.06f), new(0.005f, 0.005f));

    /// <summary>The pattern of a wall colour family: the identity facing-tile families are tiled, every other main wall is painted.</summary>
    public static WallPattern Of(string wallFamily) => wallFamily == Families.TileCanteen ? WallPattern.Tile : WallPattern.Paint;

    public static SurfacePatternSpec? Spec(WallPattern pattern) => pattern switch
    {
        WallPattern.None => null,
        WallPattern.Paint => Paint,
        WallPattern.Tile => Tile,
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown wall pattern."),
    };

    /// <summary>UV2 of a patterned wall face: (gate, K). Gate 1 only in K1–K3; K4 and K5 stay plain (ART-FIDELITY).</summary>
    public static Vector2 Uv2Of(FidelityZone zone) => new(zone <= FidelityZone.K3 ? 1f : 0f, (float)zone);
}
