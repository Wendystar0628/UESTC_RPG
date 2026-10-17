namespace Campus.Systems.Sky;

/// <summary>
/// C#-side tunables of the sky, kept in one place for review (WF-FLOW). Shader-side tunables are the
/// constant block at the top of game/style/shaders/sky.gdshader; map-dependent values live in the map's sky.json.
/// </summary>
public static class SkyTuning
{
    // cloud_noise 的团块频率（每个平铺多少格）。着色器里一个平铺 = 24 km。
    public const int NoiseBodyFreq = 14;      // 云体：格约 1.7 km
    public const int NoiseDetailFreq = 24;    // 圆团块：着色器按 ×1.5（格约 0.67 km）与 ×4（格约 0.25 km）两个尺度取

    /// <summary>Drift is wrapped at this period (m); every noise tile in the shader divides it, so the wrap is seamless.</summary>
    public const double DriftWrapMeters = 96000.0;

    /// <summary>g(anim_time): a slow closed loop, so clouds still breathe while the clock is paused (ART-SKYLIGHT).</summary>
    public const double AnimLoopRadiusMeters = 150.0;
    public const double AnimLoopSeconds = 480.0;

    /// <summary>Sky-looking cover → cloud-plane coverage: plane = cover^gamma (0.1 → 0.03, 0.32 → 0.18, 1 → 1).</summary>
    public const float PlaneCoverageGamma = 1.5f;

    /// <summary>Stars fade with weather fog: density multiplier from 1 (clear) to this value hides them.</summary>
    public const float StarsFogHide = 2.5f;

    /// <summary>Material parameters are re-sent only when they move by more than this, to keep the radiance pass idle.</summary>
    public const float UniformEpsilon = 1e-4f;
}
