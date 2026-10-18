using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Lighting;

/// <summary>Per-frame readouts the key light needs; all angles are art altitudes in degrees.</summary>
public readonly record struct KeyLightInput(
    Vector3 SunDir, float SunAltDeg,
    Vector3 MoonDir, float MoonAltDeg,
    float Overcast, float Precip, float PrecipKind)
{
    public static KeyLightInput From(IWorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var weather = world.WeatherNow;
        return new(world.Sun.Dir, world.Sun.AltDeg, world.Moon.Dir, world.Moon.AltDeg,
            weather.Overcast, weather.Precip, weather.PrecipKind);
    }
}

/// <summary>
/// Values written by the key-light step. <see cref="LightDir"/> points towards the body and is the
/// shadow direction (altitude clamped to 4 degrees); <see cref="ShadowPass"/> false means
/// ShadowOpacity 0 and an empty caster mask (ENG-PERF 10.1, SYS-WEATHER 7).
/// </summary>
public readonly record struct KeyLightFrame(
    KeyBody Body, Vector3 LightDir, Vector3 OvercastDir,
    float KeyStrength, float ShadowStrength, float RetreatY,
    bool ShadowPass, bool Switched);

/// <summary>
/// Pure key-light state machine: sun/moon hand-off inside the shadowless blue hour, key strength,
/// shadow strength with the weather multiplier, the idle shadow pass and the retreat line.
/// Direction quantisation is a post-mainline detail; the light currently follows the body continuously.
/// </summary>
public sealed class KeyLightModel
{
    /// <summary>Shadow direction never drops below this altitude (ART-SKYLIGHT 2.1).</summary>
    public const float MinShadowAltDeg = 4f;
    /// <summary>Sun altitude at which the key light swaps bodies; inside the −2°…−7° blue hour.</summary>
    public const float HandoffSunAltDeg = -4.5f;
    public const float MoonShadowMax = 0.45f;
    /// <summary>Below this product of shadow strength and key strength the shadow pass idles.</summary>
    public const float IdleShadowThreshold = 0.01f;
    public const float OvercastElevationDeg = 55f;
    public const float RetreatLow = -10f;
    public const float RetreatHigh = 80f;

    private bool _fresh = true;
    private KeyBody _body;
    private bool _passOn;

    public void Reset() => _fresh = true;

    public KeyLightFrame Step(in KeyLightInput input)
    {
        var fresh = _fresh;
        _fresh = false;
        var body = input.SunAltDeg < HandoffSunAltDeg ? KeyBody.Moon : KeyBody.Sun;
        var switched = !fresh && body != _body;
        _body = body;

        var (dir, alt) = body switch
        {
            KeyBody.Sun => (input.SunDir, input.SunAltDeg),
            KeyBody.Moon => (input.MoonDir, input.MoonAltDeg),
            _ => throw new InvalidOperationException($"Unknown key body {body}."),
        };
        var lightDir = ClampAltitude(dir, MinShadowAltDeg);
        var overcastDir = WithElevation(lightDir, OvercastElevationDeg);
        var key = KeyStrength(body, input.SunAltDeg, input.MoonAltDeg);
        var shadow = BaseShadowStrength(body, input.SunAltDeg, input.MoonAltDeg)
            * WeatherShadowFactor(input.Overcast)
            * (body == KeyBody.Moon ? RainNightFactor(input.Precip, input.PrecipKind) : 1f);
        var wantPass = !switched && shadow * key >= IdleShadowThreshold;

        float written;
        if (!wantPass)
        {
            // Idle pass, or the hand-off frame: shadow_strength must be 0 in the frame the body changes.
            _passOn = false;
            written = 0f;
        }
        else if (!_passOn && !fresh)
        {
            // Restore mask and opacity one frame early; the strength fades in from the next frame.
            _passOn = true;
            written = 0f;
        }
        else
        {
            _passOn = true;
            written = shadow;
        }

        var retreat = body == KeyBody.Sun ? RetreatLine(input.SunAltDeg) : RetreatLow;
        return new(body, lightDir, overcastDir, key, written, retreat, _passOn, switched);
    }

    /// <summary>Sun: smoothstep(−2°, 4°, h) (0.26 at h = 0). Moon: fades in with the moon shadow; the keyframe intensity lives in the palette lit row.</summary>
    public static float KeyStrength(KeyBody body, float sunAltDeg, float moonAltDeg) => body switch
    {
        KeyBody.Sun => Godot.Mathf.SmoothStep(-2f, 4f, sunAltDeg),
        KeyBody.Moon => Godot.Mathf.SmoothStep(-7f, -12f, sunAltDeg) * Godot.Mathf.SmoothStep(-2f, 4f, moonAltDeg),
        _ => throw new ArgumentOutOfRangeException(nameof(body)),
    };

    /// <summary>Clear-weather shadow strength: sun 1 above −2°, none in the blue hour, moon up to 0.45.</summary>
    public static float BaseShadowStrength(KeyBody body, float sunAltDeg, float moonAltDeg) => body switch
    {
        KeyBody.Sun => sunAltDeg >= -2f ? 1f : 0f,
        KeyBody.Moon => MoonShadowMax * Godot.Mathf.SmoothStep(-7f, -12f, sunAltDeg) * Godot.Mathf.SmoothStep(15f, 20f, moonAltDeg),
        _ => throw new ArgumentOutOfRangeException(nameof(body)),
    };

    /// <summary>1 − 0.8 × overcast: daytime never below 0.2, so both shading steps survive.</summary>
    public static float WeatherShadowFactor(float overcast) => 1f - 0.8f * Math.Clamp(overcast, 0f, 1f);

    /// <summary>Rainy nights have no moon shadow; snow keeps a lighter one (SYS-WEATHER 2).</summary>
    public static float RainNightFactor(float precip, float precipKind)
    {
        var rain = Math.Clamp(precip, 0f, 1f) * (1f - Math.Clamp(precipKind, 0f, 1f));
        return 1f - Godot.Mathf.SmoothStep(0f, 0.15f, rain);
    }

    /// <summary>Retreat line: −10 m at 2.5° rising to 80 m at 0.3° (ART-SKYLIGHT 3.4); dawn runs in reverse.</summary>
    public static float RetreatLine(float sunAltDeg)
    {
        var t = Math.Clamp((2.5f - sunAltDeg) / (2.5f - 0.3f), 0f, 1f);
        return RetreatLow + (RetreatHigh - RetreatLow) * t;
    }

    public static Vector3 ClampAltitude(Vector3 dir, float minAltDeg)
    {
        var horizontal = new Vector2(dir.X, dir.Z);
        var length = horizontal.Length();
        if (length < 1e-6f) return Vector3.Normalize(dir);
        var alt = MathF.Atan2(dir.Y, length) * (180f / MathF.PI);
        return alt >= minAltDeg ? Vector3.Normalize(dir) : WithElevation(dir, minAltDeg);
    }

    /// <summary>Same azimuth as <paramref name="dir"/>, given elevation.</summary>
    public static Vector3 WithElevation(Vector3 dir, float elevationDeg)
    {
        var horizontal = new Vector2(dir.X, dir.Z);
        var length = horizontal.Length();
        if (length < 1e-6f) return Vector3.UnitY;
        horizontal /= length;
        var e = elevationDeg * (MathF.PI / 180f);
        var c = MathF.Cos(e);
        return new(c * horizontal.X, MathF.Sin(e), c * horizontal.Y);
    }
}
