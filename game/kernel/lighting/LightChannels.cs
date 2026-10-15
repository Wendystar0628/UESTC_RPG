using Campus.Contracts;

namespace Campus.Kernel.Lighting;

/// <summary>Adaptive Δ per slot (ENG-PERF 10.3): 0.3 s of the current rate, clamped to [0.005, 0.15].</summary>
public sealed class ChannelDeltaTracker
{
    public const float DeltaMin = 0.005f;
    public const float DeltaMax = 0.15f;
    public const float RampSeconds = 0.3f;

    private readonly float[] _previous = new float[LightChannelCatalog.Count];
    private long _instance = long.MinValue;

    /// <summary>Pairs each value with its Δ; a new world instance or a zero step snaps to Δmin.</summary>
    public void Track(ReadOnlySpan<float> values, double realDelta, long instance, Span<System.Numerics.Vector2> output)
    {
        if (values.Length != LightChannelCatalog.Count || output.Length != LightChannelCatalog.Count)
            throw new ArgumentException("Expected one entry per light channel slot.");
        var fresh = instance != _instance;
        _instance = instance;
        for (var i = 0; i < values.Length; i++)
        {
            var rate = fresh || realDelta <= 0 ? 0f : MathF.Abs(values[i] - _previous[i]) / (float)realDelta;
            output[i] = new(values[i], Math.Clamp(rate * RampSeconds, DeltaMin, DeltaMax));
            _previous[i] = values[i];
        }
    }
}

/// <summary>Inputs of the stand-in schedule; h_eff = h − 6° × overcast − 4° × precipitation (SYS-WEATHER 7).</summary>
public readonly record struct ChannelEvalInput(double Clock, float SunAltDeg, WeatherVec Weather)
{
    public float EffectiveSunAltDeg => SunAltDeg - 6f * Weather.Overcast - 4f * Weather.Precip;
}

/// <summary>
/// Stand-in curves until WP-39's schedule is merged: window channels follow the overall window curve,
/// the first-lamp group lights between 8° and 4°, other fixtures between 3° and −1°. No occupancy,
/// no weather floors, no per-map data.
/// </summary>
public static class LightScheduleFallback
{
    public static void Evaluate(in ChannelEvalInput input, Span<float> values)
    {
        if (values.Length != LightChannelCatalog.Count)
            throw new ArgumentException("Expected one value per light channel slot.", nameof(values));
        var h = input.EffectiveSunAltDeg;
        var window = 0.85f * KeyLightModel.SmoothStep(9f, 2f, h);
        var first = KeyLightModel.SmoothStep(8f, 4f, h);
        var lamps = KeyLightModel.SmoothStep(3f, -1f, h);
        for (var slot = 0; slot < values.Length; slot++)
        {
            values[slot] = slot switch
            {
                <= 11 => window,
                12 or 13 or 23 => first,   // lamp.first, lamp.first.half, lamp.cue
                <= 27 => lamps,
                28 or 29 or 30 => 0f,
                31 => 1f,
                _ => throw new InvalidOperationException(),
            };
        }
    }
}
