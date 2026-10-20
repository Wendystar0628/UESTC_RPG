using System.Numerics;

namespace Campus.Contracts;

/// <summary>
/// WP-37 publishes one immutable CPU snapshot after color-script evaluation. Consumers copy Current once
/// per frame; none of these values may be obtained by reading GPU shader parameters back.
/// Values include season/weather corrections, before a consumer applies quality, lightning or medium effects.
/// </summary>
public interface IColorScriptFrame
{
    ColorScriptSnapshot Current { get; }
}

/// <param name="Rev">Increases when the published snapshot changes, including a new world instance.</param>
/// <param name="LineDarkness">line_darken multiplier: approximately 0.12–0.15 by day and 0.58–0.70 at night;
/// the night range keeps lines 0.06–0.10 darker than shadow. A smaller multiplier gives a darker line.</param>
/// <param name="Glow">GlowBloom, the full-frame soft-glow amount; not GlowIntensity.</param>
/// <param name="Vignette">Vignette strength k, normally 0.10 by day and 0.12 at night.</param>
/// <param name="Exposure">Exposure offset in EV; zero is neutral, deep-night correction is +0.7 EV.</param>
/// <param name="Stars">Color-script star visibility in [0,1], before WP-40 applies cloud/moon occlusion.</param>
/// <param name="CloudCover">Cloud coverage in [0,1], with the weather replacement already applied.</param>
/// <param name="CdlSlope">Per-channel ASC CDL slope.</param>
/// <param name="CdlOffset">Per-channel ASC CDL offset (black lift).</param>
/// <param name="CdlPower">Per-channel ASC CDL power.</param>
/// <param name="FogLow">fog_low: top height in metres, density, lake-fog strength, reserved zero.</param>
/// <param name="CdlSaturation">Fourth ASC CDL parameter; one is neutral.</param>
/// <param name="GlowHdrThreshold">High-light glow threshold, at least 1.0 and above registered lit colours.</param>
/// <param name="GlowHdrScale">High-light glow scale, normally 0.3.</param>
/// <param name="GlowIntensity">Overall glow intensity, including the seasonal multiplier.</param>
/// <param name="VignetteTint">RGB multiplier used by the vignette formula, not a display color token.</param>
/// <param name="Fog">fog: density, haze-band elevation above the horizon in degrees (not metres),
/// fog start distance and silhouette distance in metres.
/// Weather fog is applied separately through weather_fog, not multiplied into this vector a second time.</param>
public readonly record struct ColorScriptSnapshot(
    long Rev,
    float LineDarkness,
    float Glow,
    float Vignette,
    float Exposure,
    float Stars,
    float CloudCover,
    Vector3 CdlSlope,
    Vector3 CdlOffset,
    Vector3 CdlPower,
    Vector4 FogLow,
    float CdlSaturation,
    float GlowHdrThreshold,
    float GlowHdrScale,
    float GlowIntensity,
    Vector3 VignetteTint,
    Vector4 Fog)
{
    /// <summary>Evaluated seasonal gold-leaf and mixed-leaf/petal amounts, and baseline snow, before weather accumulation.</summary>
    public Vector3 SeasonSurface { get; init; }

    /// <summary>Streetlamp light-pool gain, written by WP-37 and read by WP-39/WP-03: 1.5 on winter nights, otherwise 1.</summary>
    public float LampPoolGain { get; init; } = 1f;

    /// <summary>Linear RGB horizon fog colour towards the key light, including season/weather evaluation.</summary>
    public Vector3 FogSun { get; init; }

    /// <summary>Linear RGB horizon fog colour away from the key light, including season/weather evaluation.</summary>
    public Vector3 FogAnti { get; init; }
}
