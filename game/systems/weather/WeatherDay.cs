using System.Globalization;
using Campus.Contracts;

namespace Campus.Systems.Weather;

/// <summary>
/// One day of automatic weather, cut at the profile's day boundary. A day is a pure function of
/// (profile, seed, map, day, season, clock offset): no event crosses the boundary, so no day needs another.
/// </summary>
internal static class WeatherDay
{
    public static long DayOf(WeatherProfile profile, double hours, double offset) =>
        (long)Math.Floor((hours + offset - profile.DayBoundaryClock) / 24);

    public static double StartOf(WeatherProfile profile, long day, double offset) =>
        day * 24.0 + profile.DayBoundaryClock - offset;

    /// <summary>Contiguous spans covering the whole day; clear weather fills everything between events.</summary>
    public static WeatherSpan[] Spans(WeatherProfile profile, uint seed, MapId map, long day, Season season, double offset)
    {
        double start = StartOf(profile, day, offset), end = start + 24, margin = profile.ClearGapHours / 2;
        var events = new List<WeatherSpan[]>();
        foreach (var template in profile.Templates)
        {
            if (template.Season != season) continue;
            int count = Count(template.RatePerDay, Stream(seed, map, day, template.Id, "count"));
            for (int k = 0; k < count; k++)
            {
                double at = start + WeatherRules.Clock(template.FromClock - profile.DayBoundaryClock) +
                    Stream(seed, map, day, template.Id, Purpose("start", k, 0)).NextFloat() * template.WindowHours;
                var spans = Expand(template, seed, map, day, k, at, season, offset);
                // The boundary stays inside a clear interval: an event that would reach it is dropped.
                if (spans is null || spans[0].FromHours < start + margin || spans[^1].ToHours > end - margin) continue;
                events.Add(spans);
            }
        }

        var result = new List<WeatherSpan>();
        double cursor = start, lastEnd = double.NegativeInfinity, lastPrecipEnd = double.NegativeInfinity, precipHours = 0;
        foreach (var spans in events.OrderBy(e => e[0].FromHours))
        {
            if (spans[0].FromHours - lastEnd < profile.ClearGapHours) continue;
            var precip = spans.Where(s => WeatherRules.IsPrecipitation(s.Kind)).ToArray();
            if (precip.Length > 0)
            {
                double hours = precip.Sum(s => s.ToHours - s.FromHours);
                if (precip[0].FromHours - lastPrecipEnd < profile.PrecipGapHours ||
                    precipHours + hours > profile.MaxPrecipHoursPerDay + 1e-9) continue;
                precipHours += hours;
                lastPrecipEnd = precip[^1].ToHours;
            }
            if (spans[0].FromHours > cursor) result.Add(new(cursor, spans[0].FromHours, WeatherKind.Clear, 1));
            result.AddRange(spans);
            cursor = lastEnd = spans[^1].ToHours;
        }
        if (end > cursor) result.Add(new(cursor, end, WeatherKind.Clear, 1));
        return result.ToArray();
    }

    private static WeatherSpan[]? Expand(WeatherTemplate template, uint seed, MapId map, long day, int k, double at,
        Season season, double offset)
    {
        var spans = new WeatherSpan[template.Phases.Length];
        double from = WeatherRules.AfterProtected(at, offset, season);
        for (int i = 0; i < spans.Length; i++)
        {
            var phase = template.Phases[i];
            bool fog = phase.Kind == WeatherKind.Fog;
            if (fog && !WeatherRules.IsNight(from, offset, season)) return null;
            var length = Stream(seed, map, day, template.Id, Purpose("duration", k, i));
            double to = from + phase.MinHours + length.NextFloat() * (phase.MaxHours - phase.MinHours);
            to = fog ? WeatherRules.FogEnd(to, offset, season) : WeatherRules.AfterProtected(to, offset, season);
            var draw = Stream(seed, map, day, template.Id, Purpose("intensity", k, i));
            float strength = phase.MinIntensity + draw.NextFloat() * (phase.MaxIntensity - phase.MinIntensity);
            spans[i] = new(from, to, phase.Kind, strength);
            from = to;
        }
        return spans;
    }

    /// <summary>Poisson draw for the number of events a template starts today; rates are below one per day.</summary>
    private static int Count(double rate, Rng rng)
    {
        double draw = rng.NextFloat(), term = Math.Exp(-rate), cumulative = term;
        int count = 0;
        while (draw >= cumulative && count < 3)
        {
            count++;
            term *= rate / count;
            cumulative += term;
        }
        return count;
    }

    private static string Purpose(string name, int k, int phase) => FormattableString.Invariant($"{name}:{k}:{phase}");

    private static Rng Stream(uint seed, MapId map, long day, string template, string purpose) =>
        new(StableHash.Hash32(seed, map.Value, day.ToString(CultureInfo.InvariantCulture), template, purpose));
}
