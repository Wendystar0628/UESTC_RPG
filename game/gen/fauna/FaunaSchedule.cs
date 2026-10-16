using Campus.Contracts;

namespace Campus.Gen.Fauna;

public enum FaunaKind : byte { Cat, Pigeon, Sparrow, Swan, Crow }

/// <summary>The sets of one site (WLD-LIFE §6.2): day and night positions, and the cats' "under the eaves" set for rain.</summary>
public enum FaunaSet : byte { Day, Night, Rain }

/// <summary>The weather dimension of the latched state (WLD-LIFE §6.2): clear, rain (snow counts as rain for animals), after rain.</summary>
public enum WeatherDim : byte { Clear, Rain, AfterRain }

/// <summary>
/// Target set of each animal kind for a sun altitude and weather vector (WLD-LIFE §1, §6.2). Pure: the target depends only on
/// the world state, so every site converges to the same result once the camera looks away. Night means the sun below −6°.
/// Rain is read from the weather vector only (SYS-WEATHER §8); the module never decides the weather itself.
/// </summary>
public static class FaunaSchedule
{
    /// <summary>Night: the sun below −6° (WLD-LIFE §1).</summary>
    public const float NightSunDeg = -6f;
    /// <summary>"Rain" once precipitation is clearly present (WLD-LIFE §7, P0 initial value; rain or snow alike for animals).</summary>
    public const float RainPrecip = 0.15f;
    /// <summary>"After rain" while the ground is still wet after precipitation stops (WLD-LIFE §7, P0 initial value).</summary>
    public const float AfterRainWet = 0.4f;
    /// <summary>Swans stay on the water in light and moderate rain; from this much precipitation on they use the night set (WLD-LIFE §1).</summary>
    public const float HeavyPrecip = 0.6f;
    /// <summary>Dusk crows: afternoon, sun from +8° down to −6° (D-175, WLD-LIFE §1).</summary>
    public const float DuskHighDeg = 8f;
    public const double DuskAfterClock = 12.0;

    public static WeatherDim Classify(in WeatherVec w) =>
        w.Precip >= RainPrecip ? WeatherDim.Rain : w.Wet >= AfterRainWet ? WeatherDim.AfterRain : WeatherDim.Clear;

    /// <summary>The set a kind shows now, or null when it shows nothing (sparrows at night and in rain, crows outside dusk).</summary>
    public static FaunaSet? Target(FaunaKind kind, float sunAltDeg, double clock, in WeatherVec weather)
    {
        var night = sunAltDeg < NightSunDeg;
        var dim = Classify(weather);
        var rain = dim switch
        {
            WeatherDim.Rain => true,
            WeatherDim.Clear or WeatherDim.AfterRain => false,
            _ => throw new ArgumentOutOfRangeException(nameof(weather), dim, "Unknown weather dimension."),
        };
        return kind switch
        {
            // Cats: the only animals active at night; in rain they shelter under the eaves, day or night (WLD-LIFE §1).
            FaunaKind.Cat => rain ? FaunaSet.Rain : night ? FaunaSet.Night : FaunaSet.Day,
            // Pigeons: on the plaza by day; at night and in rain they roost under nearby eaves, never on the ground.
            FaunaKind.Pigeon => rain || night ? FaunaSet.Night : FaunaSet.Day,
            // Sparrows: day birds; back in the nest at night and in rain.
            FaunaKind.Sparrow => rain || night ? null : FaunaSet.Day,
            // Black swans: on the water by day and in light rain, tucked in by the shore at night and in heavy rain.
            FaunaKind.Swan => night || weather.Precip >= HeavyPrecip ? FaunaSet.Night : FaunaSet.Day,
            // Crows: a still silhouette only at dusk, never in rain or snow; the one "dusk" set is stored as Day.
            FaunaKind.Crow => !rain && clock >= DuskAfterClock && sunAltDeg <= DuskHighDeg && !night ? FaunaSet.Day : null,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown fauna kind."),
        };
    }
}
