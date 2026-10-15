using Campus.Contracts;

namespace Campus.Kernel.Lighting;

/// <summary>
/// Slot table of the 32 light channels (ENG-PERF 10.3), drafted with WP-39. The slot is what
/// geometry encodes (window UV2.y = slot + 32 × columns) and what light_channels stores per texel,
/// so gen and runtime must share it: proposed for game/contracts in WP-03's contract request.
/// Per-map lighting-schedule.json files only give curves per slot; they never move slots.
/// </summary>
public static class LightChannelCatalog
{
    public const int Count = 32;

    private static readonly string[] Names =
    [
        // 0–11 window-type channels: L = share of lit windows (≤ 0.85, library 0.9)
        "window.general", "window.dorm", "window.teach", "window.library", "window.lab", "window.canteen",
        "window.outskirts", "window.allnight", "window.cafe", "window.venue", "window.gym", "interior.public",
        // 12–27 fixtures: L = retained share; each lamp's sun-altitude threshold lives in lamp_table
        "lamp.first", "lamp.first.half", "lamp.street", "lamp.landscape", "lamp.statue", "lamp.canopy",
        "lamp.pilotis", "lamp.corridor", "lamp.court", "sign.shop", "sign.signal", "lamp.cue",
        "lamp.fountain", "clock.face", "lamp.aviation", "water.pool",
        // 28–29 reserved; 30 sys.cue: L = 1 while the title cue overrides, Δ = its target sun altitude (°);
        // 31 sys.poolGain: L = lamp pool gain (winter nights 1.5), not a 0–1 value
        "reserved.28", "reserved.29", "sys.cue", "sys.poolGain",
    ];

    private static readonly Dictionary<string, int> Slots = BuildSlots();

    public static ChannelId Id(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, Count);
        return new(Names[slot]);
    }

    /// <summary>Slot of an allocated channel, or −1.</summary>
    public static int Slot(ChannelId id) =>
        id.Value is { } value && Slots.TryGetValue(value, out var slot) ? slot : -1;

    private static Dictionary<string, int> BuildSlots()
    {
        if (Names.Length != Count) throw new InvalidOperationException("Light channel catalog must have 32 slots.");
        var slots = new Dictionary<string, int>(Count, StringComparer.Ordinal);
        for (var i = 0; i < Names.Length; i++)
            if (!slots.TryAdd(Names[i], i)) throw new InvalidOperationException($"Duplicate light channel {Names[i]}.");
        return slots;
    }
}

/// <summary>
/// Write side of the light channels (proposed contract, WP-03 implements, WP-39 calls once per frame
/// in FramePriority.Channels). Each element is (L, Δ) for one slot; Δ is the adaptive transition
/// width clamp(|dL/dt| × 0.3 s, Δmin, Δmax) of ENG-PERF 10.3; sys.cue carries a target altitude in Δ.
/// </summary>
public interface ILightChannelSink
{
    void Submit(ReadOnlySpan<System.Numerics.Vector2> lDelta);
}

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
