using System.Globalization;
using Campus.Contracts;

namespace Campus.Systems.Weather;

/// <summary>A whole event is committed at once: look-ahead refill cadence cannot consume random draws.</summary>
internal sealed class WeatherTimeline(MapId map, WeatherProfile profile, Season season, double hours)
{
    public MapId Map { get; } = map;
    public WeatherProfile Profile { get; } = profile;
    public Season Season = season;
    public double ClockOffset;
    public double Hours = hours;
    public uint Epoch;
    public int Segment;
    public int Attempt;
    public WeatherSel Selection = new(WeatherMode.Auto, WeatherKind.Clear, 1);
    public readonly List<WeatherSpan> Queue = [];
    public readonly List<double> Minutes = [];
    public double? PinFrom, PinTo;
    public WeatherKind? PinKind;
    public float? PinIntensity;
    public double QuietUntil;
    public float? WetOverride;

    public void Ensure(uint seed, double horizon)
    {
        if (Queue.Count == 0) Add(new(Hours, Math.Max(Hours + Profile.ClearGapHours, QuietUntil), WeatherKind.Clear, 1));
        while (Queue[^1].ToHours < horizon) Generate(seed);
    }

    private void Generate(uint seed)
    {
        double cursor = Queue[^1].ToHours;
        var templates = Profile.Templates.Where(t => t.Season == Season).ToArray();
        double bound = templates.Sum(t => t.RatePerDay / t.WindowHours);
        double candidate = cursor;
        for (int retry = 0; retry < 4096; retry++)
        {
            int attempt = Attempt++;
            var gapRng = Stream(seed, attempt, "gap");
            candidate += -Math.Log(1 - gapRng.NextFloat()) / bound + 1e-8;
            candidate = WeatherRules.AfterProtected(candidate, ClockOffset, Season);
            double clock = WeatherRules.Clock(candidate + ClockOffset);
            double hazard = templates.Sum(t => t.Hazard(clock));
            var thinning = Stream(seed, attempt, "accept");
            if (hazard == 0 || thinning.NextFloat() * bound >= hazard) continue;
            var choice = Stream(seed, attempt, "template");
            double pick = choice.NextFloat() * hazard;
            var template = templates[^1];
            foreach (var item in templates)
            {
                pick -= item.Hazard(clock);
                if (pick < 0) { template = item; break; }
            }
            var spans = Expand(template, seed, attempt, candidate);
            if (spans is null || !PrecipitationAllowed(spans)) continue;
            AppendClear(cursor, candidate);
            foreach (var span in spans) Add(span);
            double end = Queue[^1].ToHours;
            Add(new(end, WeatherRules.AfterProtected(end + Profile.ClearGapHours, ClockOffset, Season), WeatherKind.Clear, 1));
            Segment = checked(Segment + 1);
            Attempt = 0;
            return;
        }
        throw new InvalidDataException("Weather profile cannot produce a legal event after 4096 candidates.");
    }

    private List<WeatherSpan>? Expand(WeatherTemplate template, uint seed, int attempt, double start)
    {
        var spans = new List<WeatherSpan>();
        for (int i = 0; i < template.Phases.Length; i++)
        {
            var phase = template.Phases[i];
            if (phase.Kind == WeatherKind.Fog && !WeatherRules.IsNight(start, ClockOffset, Season)) return null;
            var duration = Stream(seed, attempt, "duration:" + i.ToString(CultureInfo.InvariantCulture));
            double end = start + phase.MinHours + duration.NextFloat() * (phase.MaxHours - phase.MinHours);
            end = phase.Kind == WeatherKind.Fog ? WeatherRules.FogEnd(end, ClockOffset, Season)
                : WeatherRules.AfterProtected(end, ClockOffset, Season);
            var intensity = Stream(seed, attempt, "intensity:" + i.ToString(CultureInfo.InvariantCulture));
            float strength = phase.MinIntensity + intensity.NextFloat() * (phase.MaxIntensity - phase.MinIntensity);
            spans.Add(new(start, end, phase.Kind, strength));
            start = end;
        }
        return spans;
    }

    private bool PrecipitationAllowed(List<WeatherSpan> spans)
    {
        var added = spans.Where(s => WeatherRules.IsPrecipitation(s.Kind)).ToArray();
        if (added.Length == 0) return true;
        double first = added[0].FromHours;
        var previous = Queue.LastOrDefault(s => WeatherRules.IsPrecipitation(s.Kind));
        if (previous.ToHours > 0 && first - previous.ToHours < Profile.PrecipGapHours) return false;
        var rain = Queue.Where(s => WeatherRules.IsPrecipitation(s.Kind) && s.ToHours > first - 24).Concat(added).ToArray();
        foreach (double edge in rain.SelectMany(s => new[] { s.FromHours, s.ToHours, s.FromHours + 24, s.ToHours + 24 }))
        {
            double sum = rain.Sum(s => Math.Max(0, Math.Min(edge, s.ToHours) - Math.Max(edge - 24, s.FromHours)));
            if (sum > Profile.MaxPrecipHoursPerDay + 1e-9) return false;
        }
        return true;
    }

    private Rng Stream(uint seed, int attempt, string purpose) => new(StableHash.Hash32(seed, Map.Value,
        Epoch.ToString(CultureInfo.InvariantCulture), Segment.ToString(CultureInfo.InvariantCulture),
        attempt.ToString(CultureInfo.InvariantCulture), purpose));

    private void AppendClear(double from, double to)
    {
        if (to <= from) return;
        if (Queue.Count > 0 && Queue[^1].Kind == WeatherKind.Clear)
        {
            Minutes[^1] = (to - Queue[^1].FromHours) * 60;
            Queue[^1] = Queue[^1] with { ToHours = Queue[^1].FromHours + Minutes[^1] / 60 };
        }
        else Add(new(from, to, WeatherKind.Clear, 1));
    }

    public void Add(WeatherSpan span)
    {
        double from = Queue.Count == 0 ? span.FromHours : Queue[^1].ToHours;
        double minutes = (span.ToHours - from) * 60;
        Minutes.Add(minutes);
        Queue.Add(span with { FromHours = from, ToHours = from + minutes / 60 });
    }

    public void ClearQueue() { Queue.Clear(); Minutes.Clear(); }

    public void Trim(double hours)
    {
        // Keep complete spans, including the predecessor of the 24 h history boundary.
        int count = 0;
        while (count + 1 < Queue.Count && Queue[count + 1].ToHours < hours - 24) count++;
        if (count > 0) { Queue.RemoveRange(0, count); Minutes.RemoveRange(0, count); }
    }

    public WeatherSpan At(double hours)
    {
        foreach (var span in Queue)
            if (hours >= span.FromHours && hours < span.ToHours) return span;
        return new(hours, hours + 24, WeatherKind.Clear, 1);
    }

    public WeatherSel SelectionAt(double hours)
    {
        if (Selection.Mode == WeatherMode.Manual) return Selection;
        if (hours < QuietUntil) return new(WeatherMode.Auto, WeatherKind.Clear, 1);
        if (PinKind is { } pinned && PinFrom <= hours && hours < PinTo)
            return new(WeatherMode.Auto, pinned, PinIntensity ?? WeatherRules.DefaultIntensity(pinned));
        var span = At(hours);
        return new(WeatherMode.Auto, span.Kind, span.Intensity);
    }

    public WeatherVec Evaluate(double hours)
    {
        var selection = SelectionAt(hours);
        var target = WeatherVectors.For(selection.Kind, selection.Intensity);
        if (selection.Mode == WeatherMode.Manual || hours < QuietUntil || PinKind.HasValue && PinFrom <= hours && hours < PinTo)
            return target;
        target = BlendQueue(hours);
        float wet = target.Wet, puddle = target.Puddle, snow = target.SnowAdd;
        foreach (var span in Queue)
        {
            if (span.FromHours > hours || span.ToHours < hours - 6 || !WeatherRules.IsPrecipitation(span.Kind)) continue;
            var peak = WeatherVectors.For(span.Kind, span.Intensity);
            double age = Math.Max(0, hours - span.ToHours);
            float rise = WeatherVectors.Smooth((float)((hours - span.FromHours) / Profile.BlendHours));
            wet = Math.Max(wet, peak.Wet * rise * (float)Math.Max(0, 1 - age / 2));
            puddle = Math.Max(puddle, peak.Puddle * rise * (float)Math.Max(0, 1 - age / 3));
            snow = Math.Max(snow, peak.SnowAdd * rise * (float)Math.Max(0, 1 - age / 6));
        }
        return target with { Wet = wet, Puddle = puddle, SnowAdd = snow };
    }

    private WeatherVec BlendQueue(double hours)
    {
        // Convolve the piecewise-constant targets with a finite smoothstep-derivative kernel.
        // A span contributes S((h-from)/B)-S((h-to)/B). An isolated edge is the usual smoothstep;
        // short consecutive phases overlap continuously instead of restarting at a full target.
        // Only the preceding B hours matter, so the saved queue is sufficient without frame state.
        double duration = Profile.BlendHours;
        var first = Queue[0];
        var result = WeatherVectors.For(first.Kind, first.Intensity);
        // Before the queue origin the initial weather is already settled (new game / explicit queue).
        float total = 1 - WeatherVectors.Smooth((float)((hours - first.FromHours) / duration));
        foreach (var span in Queue)
        {
            if (span.ToHours <= hours - duration) continue;
            if (span.FromHours > hours) break;
            float weight = WeatherVectors.Smooth((float)((hours - span.FromHours) / duration))
                - WeatherVectors.Smooth((float)((hours - span.ToHours) / duration));
            if (weight <= 0) continue;
            var value = WeatherVectors.For(span.Kind, span.Intensity);
            result = total == 0 ? value : WeatherVectors.Lerp(result, value, weight / (total + weight));
            total += weight;
        }
        return result;
    }
}
