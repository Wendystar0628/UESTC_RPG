namespace Campus.Systems.Vegetation;

/// <summary>
/// Distance triggers only: near levels and ready owner cells complete a bounded RenderFade.Seconds cross-fade even
/// when the camera stops. Owner cells enter at NearFadeStart and leave past NearFadeEnd with retirement hysteresis;
/// the common quality factor scales both. No distance-dependent Bayer progress remains in the tree path.
/// </summary>
public static class VegFade
{
    /// <summary>Initial trigger-band width, relative to each near LOD boundary; final presentation is reviewed separately.</summary>
    public const float LevelBandFraction = 0.1f;
    public const float NearFadeStart = 135f;
    public const float NearFadeEnd = 165f;

}
