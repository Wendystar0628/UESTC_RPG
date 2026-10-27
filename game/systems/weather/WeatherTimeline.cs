using Campus.Contracts;

namespace Campus.Systems.Weather;

/// <summary>World-level weather choices layered over the automatic weather: everything a save keeps besides the seed.</summary>
internal sealed class WeatherSettings
{
    public const double WetHours = 2, PuddleHours = 3, SnowHours = 6;

    public Season Season = Season.Autumn;
    public double ClockOffset;
    public bool FrameBound;
    public WeatherSel? Manual;
    public double? PinFrom, PinTo;
    public WeatherKind? PinKind;
    public float? PinIntensity;
    public double QuietUntil;
    public float? WetOverride;
    /// <summary>Surface amounts at <see cref="ResidueAt"/>; snow grows or drains along the subsequent weather spans.</summary>
    public float ResidueWet, ResiduePuddle, ResidueSnow;
    public double ResidueAt;

    public WeatherSettings Copy() => (WeatherSettings)MemberwiseClone();

    public void ClearChoices()
    {
        Manual = null;
        PinFrom = PinTo = null;
        PinKind = null;
        PinIntensity = null;
        QuietUntil = 0;
        WetOverride = null;
    }

    public void ClearResidue() => ResidueWet = ResiduePuddle = ResidueSnow = 0;

    public void SetResidue(WeatherVec vector, double hours)
    {
        ResidueWet = vector.Wet;
        ResiduePuddle = vector.Puddle;
        ResidueSnow = vector.SnowAdd;
        ResidueAt = hours;
    }

    /// <summary>A season/clock change preserves existing wet residue; only snow samples the settled weather.</summary>
    public void SetSnowAnchor(float amount, double hours)
    {
        var residue = ResidueNow(hours);
        ResidueWet = residue.Wet;
        ResiduePuddle = residue.Puddle;
        ResidueSnow = amount;
        ResidueAt = hours;
    }

    public (float Wet, float Puddle) ResidueNow(double hours)
    {
        double age = Math.Max(0, hours - ResidueAt);
        return (Drain(ResidueWet, age, WetHours), Drain(ResiduePuddle, age, PuddleHours));
    }

    // A constant drain rate (a full reading empties in the given hours) lets a save re-anchor the residue without changing its course.
    private static float Drain(float value, double age, double hours) => (float)Math.Max(0, value - age / hours);

    public static float Fade(double age, double hours) => (float)Math.Max(0, 1 - age / hours);

    /// <summary>The weather frame changes season: manual and pinned kinds are mapped in place (雨 ↔ 雪).</summary>
    public void Reseason(Season to, double offset)
    {
        if (Manual is { } manual)
        {
            var kind = WeatherRules.MapToSeason(manual.Kind, to);
            Manual = manual with { Kind = kind, Intensity = manual.Kind == WeatherKind.Thunderstorm && kind != manual.Kind ? 1 : manual.Intensity };
        }
        if (PinKind is { } pin) PinKind = WeatherRules.MapToSeason(pin, to);
        WetOverride = null;
        if (to != Season.Winter) ResidueSnow = 0;
        Season = to;
        ClockOffset = offset;
        FrameBound = true;
    }
}

/// <summary>
/// Weather at any world hour for one (seed, map): the automatic days plus the settings on top. Nothing here is state
/// beyond a cache of generated days, so every query gives the same answer in any call order.
/// </summary>
internal sealed class WeatherTimeline(WeatherProfile profile, uint seed, MapId map, WeatherSettings settings)
{
    private readonly Dictionary<long, WeatherSpan[]> _days = [];
    private readonly List<WeatherSpan> _segments = [];
    private readonly List<WeatherSpan> _automatic = [];
    private readonly List<double> _cuts = [];
    private Season _season;
    private double _offset;
    private bool _primed;

    /// <summary>The choice in force at the hour; automatic weather reports the current span.</summary>
    public WeatherSel SelectionAt(double hours)
    {
        if (settings.Manual is { } manual) return manual;
        var (kind, intensity) = Overlay(hours, null);
        return new(WeatherMode.Auto, kind, intensity);
    }

    public WeatherVec Evaluate(double hours)
    {
        var selection = SelectionAt(hours);
        var vector = WeatherVectors.For(selection.Kind, selection.Intensity);
        // Pin/quiet still fix the atmosphere, while snow follows elapsed world time.
        if (hours < settings.QuietUntil || InPin(hours)) return vector with { SnowAdd = SnowAt(hours) };
        float wet = vector.Wet, puddle = vector.Puddle;
        if (settings.Manual is null)
        {
            vector = Blend(hours);
            wet = vector.Wet;
            puddle = vector.Puddle;
            foreach (var span in Segments(hours - WeatherSettings.SnowHours, hours))
            {
                if (!WeatherRules.IsPrecipitation(span.Kind)) continue;
                var peak = WeatherVectors.For(span.Kind, span.Intensity);
                double age = Math.Max(0, hours - span.ToHours);
                float rise = ScalarMath.Smoothstep((float)((hours - span.FromHours) / profile.BlendHours));
                wet = Math.Max(wet, peak.Wet * rise * WeatherSettings.Fade(age, WeatherSettings.WetHours));
                puddle = Math.Max(puddle, peak.Puddle * rise * WeatherSettings.Fade(age, WeatherSettings.PuddleHours));
            }
        }
        var residue = settings.ResidueNow(hours);
        return vector with { Wet = Math.Max(wet, residue.Wet), Puddle = Math.Max(puddle, residue.Puddle), SnowAdd = SnowAt(hours) };
    }

    /// <summary>Integrates whole weather spans, never frames. The saved residue is the only surface-state anchor.</summary>
    public float SnowAt(double hours)
    {
        if (settings.Season != Season.Winter) return 0;
        double anchor = settings.ResidueAt;
        if (hours <= anchor) return settings.ResidueSnow;
        if (settings.Manual is { } manual)
            return (float)SnowAfter(settings.ResidueSnow, hours - anchor, manual.Kind, manual.Intensity);

        // Most queries forget earlier snow after one dry interval. Extend only until the possible
        // [0, maximum] readings agree, or until the actual saved/manual-choice anchor is reached.
        double window = 24;
        while (true)
        {
            double from = Math.Max(anchor, hours - window);
            double low = from == anchor ? settings.ResidueSnow : 0;
            double high = from == anchor ? low : WeatherRules.MaxSnow;
            foreach (var span in Segments(from, hours))
            {
                double duration = span.ToHours - span.FromHours;
                low = SnowAfter(low, duration, span.Kind, span.Intensity);
                high = SnowAfter(high, duration, span.Kind, span.Intensity);
            }
            if (low == high) return (float)low;
            window *= 2;
        }
    }

    private static double SnowAfter(double amount, double hours, WeatherKind kind, float intensity)
    {
        if (kind != WeatherKind.Snow) return Math.Max(0, amount - hours / WeatherRules.SnowDrainHours(kind));
        double target = WeatherRules.SnowTarget(intensity);
        double toTarget = Math.Max(0, target - amount) / WeatherRules.SnowGrowthPerHour;
        amount += Math.Min(hours, toTarget) * WeatherRules.SnowGrowthPerHour;
        if (intensity >= WeatherRules.HeavySnowIntensity)
            amount = Math.Min(WeatherRules.MaxSnow, amount + Math.Max(0, hours - toTarget) * WeatherRules.HeavySnowGrowthPerHour);
        return amount;
    }

    /// <summary>The next 24 hours as contiguous spans, merged where the weather does not change.</summary>
    public void Ahead(double hours, List<WeatherSpan> result)
    {
        result.Clear();
        if (settings.Manual is { } manual)
        {
            result.Add(new(hours, hours + 24, manual.Kind, manual.Intensity));
            return;
        }
        foreach (var span in Segments(hours, hours + 24))
        {
            if (result.Count > 0 && result[^1].Kind == span.Kind && result[^1].Intensity == span.Intensity)
                result[^1] = result[^1] with { ToHours = span.ToHours };
            else result.Add(span);
        }
    }

    /// <summary>Convolve the piecewise-constant targets with a smoothstep-derivative kernel of the profile's blend length:
    /// a span contributes S((h-from)/B)-S((h-to)/B), so short consecutive phases overlap instead of restarting.</summary>
    private WeatherVec Blend(double hours)
    {
        double duration = profile.BlendHours;
        var spans = Segments(hours - duration, hours);
        if (spans.Count == 0)
        {
            var selection = SelectionAt(hours);
            return WeatherVectors.For(selection.Kind, selection.Intensity);
        }
        var result = WeatherVectors.For(spans[0].Kind, spans[0].Intensity);
        float total = 1 - ScalarMath.Smoothstep((float)((hours - spans[0].FromHours) / duration));
        foreach (var span in spans)
        {
            float weight = ScalarMath.Smoothstep((float)((hours - span.FromHours) / duration))
                - ScalarMath.Smoothstep((float)((hours - span.ToHours) / duration));
            if (weight <= 0) continue;
            var value = WeatherVectors.For(span.Kind, span.Intensity);
            result = total == 0 ? value : WeatherVectors.Lerp(result, value, weight / (total + weight));
            total += weight;
        }
        return result;
    }

    /// <summary>The automatic days over [lo, hi] with quiet and pin windows applied, cut into contiguous spans.</summary>
    private List<WeatherSpan> Segments(double lo, double hi)
    {
        // Queries consume these spans before the next query; only Ahead publishes into its owner's container.
        var result = _segments;
        result.Clear();
        if (!(hi > lo)) return result;
        var auto = _automatic;
        auto.Clear();
        for (long day = DayOf(lo), last = DayOf(hi); day <= last; day++) auto.AddRange(DaySpans(day));
        var cuts = _cuts;
        cuts.Clear();
        cuts.Add(lo);
        cuts.Add(hi);
        foreach (var span in auto)
        {
            Cut(span.FromHours);
            Cut(span.ToHours);
        }
        Cut(settings.PinFrom);
        Cut(settings.PinTo);
        Cut(settings.QuietUntil);
        cuts.Sort();
        double from = lo;
        for (int i = 1; i < cuts.Count; i++)
        {
            double cut = cuts[i];
            if (cut == from) continue;
            var (kind, intensity) = Overlay((from + cut) / 2, auto);
            result.Add(new(from, cut, kind, intensity));
            from = cut;
        }
        return result;

        void Cut(double? value)
        {
            if (value is { } x && x > lo && x < hi) cuts.Add(x);
        }
    }

    /// <summary>Quiet beats pin beats the automatic span; <paramref name="auto"/> null looks the span up by day.</summary>
    private (WeatherKind Kind, float Intensity) Overlay(double hours, List<WeatherSpan>? auto)
    {
        if (hours < settings.QuietUntil) return (WeatherKind.Clear, 1);
        if (InPin(hours)) return (settings.PinKind!.Value, settings.PinIntensity ?? WeatherRules.DefaultIntensity(settings.PinKind.Value));
        if (auto is null)
        {
            long day = DayOf(hours);
            for (int step = 0; step < 3; step++)
                foreach (var span in DaySpans(step == 0 ? day : step == 1 ? day - 1 : day + 1))
                    if (hours >= span.FromHours && hours < span.ToHours) return (span.Kind, span.Intensity);
            return (WeatherKind.Clear, 1);
        }
        foreach (var span in auto)
            if (hours >= span.FromHours && hours < span.ToHours) return (span.Kind, span.Intensity);
        return (WeatherKind.Clear, 1);
    }

    private bool InPin(double hours) => settings.PinKind.HasValue && settings.PinFrom <= hours && hours < settings.PinTo;

    private long DayOf(double hours) => WeatherDay.DayOf(profile, hours, settings.ClockOffset);

    private WeatherSpan[] DaySpans(long day)
    {
        if (!_primed || _season != settings.Season || _offset != settings.ClockOffset)
        {
            _days.Clear();
            _season = settings.Season;
            _offset = settings.ClockOffset;
            _primed = true;
        }
        if (_days.TryGetValue(day, out var spans)) return spans;
        if (_days.Count >= 8) _days.Clear();
        return _days[day] = WeatherDay.Spans(profile, seed, map, day, _season, _offset);
    }
}
