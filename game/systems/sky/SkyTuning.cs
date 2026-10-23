namespace Campus.Systems.Sky;

/// <summary>
/// C#-side tunables of the sky, kept in one place for review (WF-FLOW). Shader-side tunables are the
/// constant block in game/style/shaders/sky_params.gdshaderinc; map-dependent values live in the map's sky.json.
/// </summary>
public static class SkyTuning
{
    /// <summary>Drift is wrapped at this period (m); every noise tile in the shader divides it, so the wrap is seamless.</summary>
    public const double DriftWrapMeters = 96000.0;

    /// <summary>g(anim_time): a slow closed loop, so clouds still breathe while the clock is paused (ART-SKYLIGHT).</summary>
    public const double AnimLoopRadiusMeters = 150.0;

    /// <summary>Sky-looking cover → cloud-plane coverage: plane = cover^gamma (0.1 → 0.03, 0.32 → 0.18, 1 → 1).</summary>
    public const float PlaneCoverageGamma = 1.5f;

    /// <summary>Stars fade with weather fog: density multiplier from 1 (clear) to this value hides them.</summary>
    public const float StarsFogHide = 2.5f;

    /// <summary>Material parameters are re-sent only when they move by more than this, to keep the radiance pass idle.</summary>
    public const float UniformEpsilon = 1e-4f;
}
