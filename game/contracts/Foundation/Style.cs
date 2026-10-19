using System.Numerics;

namespace Campus.Contracts;

[Flags]
public enum ToonFx
{
    None = 0, GlassStreak = 1, Glare = 2, AnisoBand = 4, Rim = 8,
    Mottle = 16, DetailTiling = 32, BevelLift = 64, Escalator = 128
}
public enum ToonNormal { Flat, Smooth, Spherical, AnalyticSphere, AnalyticCylinder }
public enum LineClass { Building, Tree, Fine, None, Figure }
public enum ShadeSteps : byte { Two, TwoHalf, Soft, Three }
// Append new kinds only. The material library selects its shader from Kind, not from Fx.
public enum ToonKind { Static, Glass, GlassNear, Cut, VegTrunk, VegCard, VegImpostor, VegGrass, Lotus, Figure, SceneText, VegTwigMass }

/// <summary>
/// Stable WP-04 layer and physical grid dimensions. Period includes the joint; Joint is the full
/// seam width centred on each axis boundary. Layer registration is checked by the resource consumer.
/// Direction and phase belong to the producer's continuous metre UVs, never to material identity.
/// </summary>
public readonly record struct SurfacePatternSpec(ushort Layer, Vector2 PeriodMeters, Vector2 JointMeters);

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
    /// <summary>Optional outdoor toon_static grid. Null retains all existing vertex-channel meanings.</summary>
    public SurfacePatternSpec? SurfacePattern { get; init; }

    /// <summary>Checks CPU profile shape; does not claim layer registration, GPU or bake support.</summary>
    public void ValidateSurfacePattern()
    {
        if (SurfacePattern is not { } pattern) return;
        if (Kind != ToonKind.Static)
            throw new ArgumentException("Surface patterns require the ordinary Static toon kind.", nameof(SurfacePattern));
        var period = pattern.PeriodMeters;
        var joint = pattern.JointMeters;
        if (!float.IsFinite(period.X) || !float.IsFinite(period.Y) || period.X <= 0f || period.Y <= 0f)
            throw new ArgumentException("Surface pattern periods must be finite positive metres, including joints.", nameof(SurfacePattern));
        if (!float.IsFinite(joint.X) || !float.IsFinite(joint.Y) || joint.X < 0f || joint.Y < 0f ||
            joint.X >= period.X || joint.Y >= period.Y)
            throw new ArgumentException("Surface pattern joints must be finite and satisfy 0 <= joint < period on each axis.", nameof(SurfacePattern));
    }
}
public readonly record struct ToonRef(ToonSpec Spec);
