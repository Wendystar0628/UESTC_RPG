namespace Campus.Contracts;

[Flags]
public enum ToonFx
{
    None = 0, GlassStreak = 1, Glare = 2, AnisoBand = 4, Rim = 8,
    Mottle = 16, DetailTiling = 32, BevelLift = 64
}
public enum ToonNormal { Flat, Smooth, Spherical, AnalyticSphere, AnalyticCylinder }
public enum LineClass { Building, Tree, Fine, None, Figure }
public enum ShadeSteps : byte { Two, TwoHalf, Soft, Three }
// Append new kinds only. The material library selects its shader from Kind, not from Fx.
public enum ToonKind { Static, Glass, GlassNear, Cut, VegTrunk, VegCard, VegImpostor, VegGrass, Lotus, Figure }

/// <summary>
/// Lit and Shade are registered as a pair: Shade palette ID = Lit palette ID + 1.
/// The vertex colour byte and PlacedPart.Tint both identify the Lit entry; G5 checks pairing.
/// </summary>
public sealed record ToonSpec(PaletteToken Lit, PaletteToken Shade)
{
    public ToonKind Kind { get; init; } = ToonKind.Static;
    public PaletteToken? Deep { get; init; }
    // Only used by Two and Three. TwoHalf and Soft derive their band from Steps: 1 and 2.2 times fwidth.
    public float Band { get; init; } = 0.02f;
    public ToonNormal Normal { get; init; } = ToonNormal.Flat;
    public LineClass Line { get; init; } = LineClass.Building;
    public ToonFx Fx { get; init; }
    public PaletteToken? Backface { get; init; }
    public ChannelId? Emissive { get; init; }
    public ShadeSteps Steps { get; init; } = ShadeSteps.Two;
    public float Translucency { get; init; }
    // Wet-surface response in mat_table row 1, alpha: default 0.6, asphalt 1, glass 0.
    public float Wet { get; init; } = 0.6f;
}
public readonly record struct ToonRef(ToonSpec Spec);
