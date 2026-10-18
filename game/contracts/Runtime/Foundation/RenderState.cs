using System.Numerics;

namespace Campus.Contracts;

/// <summary>The body represented by the scene's single key light.</summary>
public enum KeyBody : byte { Sun, Moon }

/// <summary>
/// WP-03 publishes the values written during the current frame's key-light step.
/// Consumers copy these values on the main thread after that step, before queuing render-thread work.
/// Do not derive replacements from the unquantized world bodies or read shader globals back.
/// </summary>
public interface IKeyLightState
{
    /// <summary>World-space unit direction towards the key body, after the light's quantization and elevation clamp.</summary>
    Vector3 KeyDir { get; }
    float KeyStrength { get; }
    float ShadowStrength { get; }
    /// <summary>Retreat-line height in metres.</summary>
    float RetreatY { get; }
    /// <summary>World-space skylight direction, using the key light's quantized azimuth.</summary>
    Vector3 OvercastDir { get; }
    KeyBody Body { get; }
}

/// <summary>
/// WP-45b publishes the same values it writes to weather_fog during the current frame's motion step.
/// Consumers copy on the main thread after that step, before queuing render-thread work.
/// </summary>
public interface IWeatherRenderState
{
    /// <summary>Fog-density multiplier, ground-fog top height in metres, ground-fog density, distant rain curtain.</summary>
    Vector4 WeatherFog { get; }
}

/// <summary>
/// WP-07 publishes the same values it writes during the current frame's medium step.
/// Consumers copy on the main thread after that step, before queuing render-thread work.
/// </summary>
public interface IMediumRenderState
{
    /// <summary>Source medium, target medium, blend and visibility, matching the medium shader global.</summary>
    Vector4 Medium { get; }
    /// <summary>Linear RGB medium colour as seen by the shader; the source-colour conversion is already applied.</summary>
    Vector3 MediumColor { get; }
}
