using Campus.Contracts;
using Campus.Contracts.Water;

namespace Campus.Systems.Water;

/// <summary>
/// Underwater look (SYS-VIEW, WLD-WATER): WP-07's medium service tints the frame and runs the shared fog
/// with these values; this package only supplies colour and visibility. Colours are palette names, so they follow
/// the time of day and season like the water surface.
/// </summary>
[MediumPresenter("water.underwater", Owner = "WP-24")]
public sealed class UnderwaterPresenter : IMediumPresenter
{
    /// <summary>Visibility just under the surface and at depth (m); SYS-VIEW asks for 5–10 m.</summary>
    public const float VisibilityShallow = 10f;
    public const float VisibilityDeep = 5f;
    public const float DepthForDeep = 3f;
    public const float TintStrength = 0.55f;
    public const float Saturation = 0.9f;
    public const float ShadowStrength = 0.35f;

    private static readonly PostOverride[] Steps = BuildSteps();

    public IReadOnlyList<MediumKind> Kinds { get; } = [MediumKind.Underwater];

    public void Enter(IRuntimeContext rt, in CameraMedium m) { }

    public PostOverride Update(IRuntimeContext rt, in CameraMedium m, float blend)
    {
        // Eight precomputed depth steps: no allocation per frame.
        var k = Math.Clamp(m.Depth / DepthForDeep, 0f, 1f);
        return Steps[(int)MathF.Round(k * (Steps.Length - 1))];
    }

    public void Exit(IRuntimeContext rt) { }

    private static PostOverride[] BuildSteps()
    {
        var steps = new PostOverride[8];
        for (var i = 0; i < steps.Length; i++)
        {
            var k = i / (float)(steps.Length - 1);
            steps[i] = new PostOverride(
                Saturation: Saturation,
                FogColor: WaterPalette.DeepShade,
                TintColor: WaterPalette.DeepLit,
                VisibilityM: VisibilityShallow + (VisibilityDeep - VisibilityShallow) * k,
                TintStrength: TintStrength,
                ShadowStrength: ShadowStrength);
        }
        return steps;
    }
}
