namespace Campus.Systems.Vegetation;

/// <summary>
/// Distance triggers only: near levels and ready owner cells complete a bounded RenderFade.Seconds cross-fade even
/// when the camera stops. Owner cells enter at NearFadeStart and leave past NearFadeEnd with retirement hysteresis;
/// the common quality factor scales both. No distance-dependent Bayer progress remains in the tree path.
/// Order per boundary is shrink, then fade (GAME-194): veg_card has shrunk every card the coarser level lacks by the
/// upgrade point (trigger ÷ <see cref="VegetationBuckets.Hysteresis"/>), and L1's cards before NearFadeStart, so each
/// bounded fade only swaps mass detail and wood.
/// </summary>
public static class VegFade
{
    /// <summary>A near level steps down at (1 − this) of its nominal boundary; veg_card places its card shrink band below it.</summary>
    public const float LevelBandFraction = 0.1f;
    public const float NearFadeStart = 135f;
    public const float NearFadeEnd = 165f;
}
