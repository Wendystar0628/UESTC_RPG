namespace Campus.Gen.Vegetation;

/// <summary>
/// Which pools a tree is drawn in by its distance (WLD-VEGETATION, ENG-PERF; max-011 23c2c). The near pool's L1
/// and the far stand-in (<see cref="MotherModels.FarStandIn"/>) cross-fade with complementary 4×4 Bayer dither over
/// <see cref="NearFadeStart"/>–<see cref="NearFadeEnd"/> (veg_card and veg_trunk VEG_POOL_FADE), so a tree in that band
/// is in both pools. Until the crown clouds (L3, merged into the tile geometry) exist, the far stand-in is drawn at every
/// distance (<see cref="CloudReady"/>); then it fades into them over <see cref="CloudFadeStart"/>–<see cref="CloudFadeEnd"/>.
/// Distances run from the main camera to the point half a tree height above the trunk foot and scale with the detail
/// factor (quality.x, WLD-VEGETATION). The runtime adds its own hysteresis on top; the shaders discard whatever lies
/// outside a pool's share, so a late re-bucket never shows.
/// </summary>
public static class VegFade
{
    public const float NearFadeStart = 135f;
    public const float NearFadeEnd = 165f;

    /// <summary>False while there are no crown clouds: the far stand-in never fades out (veg_* VEG_CLOUD_READY).</summary>
    public const bool CloudReady = false;

    public const float CloudFadeStart = 360f;
    public const float CloudFadeEnd = 440f;

    /// <summary>The pools a tree at this distance must be in: the near pool (L1 at this range) and/or the far pool.</summary>
    public static (bool Near, bool Far) Pools(float distance, float detail = 1f)
    {
        var q = MathF.Max(detail, 0.5f);
        var near = distance < NearFadeEnd * q;
        var far = distance > NearFadeStart * q && (!CloudReady || distance < CloudFadeEnd * q);
        return (near, far);
    }

    /// <summary>Share of a tree's pixels the far pool draws (the rest are the near pool's): 0 inside the band, 1 past it.</summary>
    public static float FarShare(float distance, float detail = 1f)
    {
        var q = MathF.Max(detail, 0.5f);
        var t = Math.Clamp((distance - NearFadeStart * q) / ((NearFadeEnd - NearFadeStart) * q), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
