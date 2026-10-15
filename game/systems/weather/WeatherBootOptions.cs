using System.Globalization;
using Campus.Contracts;

namespace Campus.Systems.Weather;

public static class WeatherBootOptions
{
    public static WeatherBoot Parse(IReadOnlyDictionary<string, string> arguments, uint seed, Season season)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.ContainsKey("weather-seed") && (arguments.ContainsKey("slot") || arguments.ContainsKey("load")))
            throw new ArgumentException("--weather-seed cannot be combined with --slot or --load.");
        if (arguments.TryGetValue("weather-seed", out var seedText) && !uint.TryParse(seedText, NumberStyles.None, CultureInfo.InvariantCulture, out seed))
            throw new ArgumentException("--weather-seed requires a u32.");
        WeatherKind? kind = null;
        if (arguments.TryGetValue("weather", out var weather) && weather != "auto") kind = ParseKind(weather);
        float? intensity = Float(arguments, "weather-intensity"), wet = Float(arguments, "wet");
        double? remaining = null;
        if (arguments.TryGetValue("weather-remaining", out var minutes))
        {
            if (!double.TryParse(minutes, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value) || value <= 0)
                throw new ArgumentException("--weather-remaining requires positive finite minutes.");
            remaining = value;
        }
        IReadOnlyList<WeatherQueueItem>? queue = null;
        if (arguments.TryGetValue("weather-queue", out var text))
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("--weather-queue cannot be empty.");
            var items = new List<WeatherQueueItem>();
            foreach (var token in text.Split(','))
            {
                var pieces = token.Split(':');
                if (pieces.Length != 2 || !double.TryParse(pieces[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value) || value <= 0)
                    throw new ArgumentException("--weather-queue requires kind:positive-minutes pairs.");
                items.Add(new(ParseKind(pieces[0]), value) { Intensity = intensity });
            }
            queue = Array.AsReadOnly(items.ToArray());
        }
        var boot = new WeatherBoot(seed, kind, intensity, wet, remaining, queue);
        Validate(boot, season);
        return boot;
    }

    public static void Validate(WeatherBoot boot, Season? season = null, WeatherProfile? profile = null)
    {
        if (boot.Intensity is { } i) WeatherRules.ValidateIntensity(i);
        if (boot.Wet is { } wet) WeatherRules.ValidateIntensity(wet);
        if (boot.RemainingMinutes is { } minutes && (!double.IsFinite(minutes) || minutes <= 0 || !boot.Kind.HasValue))
            throw new ArgumentException("A positive --weather-remaining requires an explicit weather kind.");
        if (boot.Queue is not null && (boot.Queue.Count is 0 or > 252 || boot.RemainingMinutes.HasValue))
            throw new ArgumentException("A weather queue must have 1–252 items and cannot use --weather-remaining (four slots are reserved for its return to automatic weather).");
        if (boot.Kind is { } kind)
        {
            _ = WeatherRules.DefaultIntensity(kind);
            Native(kind);
        }
        if (boot.Queue is { } queue)
        {
            WeatherKind? previous = null;
            foreach (var item in queue)
            {
                Native(item.Kind);
                if (!double.IsFinite(item.Minutes) || item.Minutes <= 0 || item.Minutes / 60 < WeatherRules.MinimumHours(item.Kind))
                    throw new ArgumentException($"Queue duration for {item.Kind} is shorter than its minimum.");
                if (item.Intensity is { } strength) WeatherRules.ValidateIntensity(strength);
                if (previous is { } from && profile is not null && !profile.Allows(from, item.Kind))
                    throw new ArgumentException($"Unvalidated weather edge: {from} -> {item.Kind}.");
                previous = item.Kind;
            }
        }
        void Native(WeatherKind value)
        {
            _ = WeatherRules.DefaultIntensity(value);
            if (season is { } target && !WeatherRules.IsNative(value, target))
                throw new ArgumentException($"{value} is unavailable in {target}; use {WeatherRules.MapToSeason(value, target)}.");
        }
    }

    public static WeatherKind ParseKind(string text) => text switch
    {
        "clear" => WeatherKind.Clear,
        "few-clouds" => WeatherKind.FewClouds,
        "cloudy" => WeatherKind.Cloudy,
        "overcast" => WeatherKind.Overcast,
        "fog" => WeatherKind.Fog,
        "light-rain" => WeatherKind.LightRain,
        "rain" => WeatherKind.Rain,
        "thunderstorm" => WeatherKind.Thunderstorm,
        "after-rain" => WeatherKind.AfterRain,
        "snow" => WeatherKind.Snow,
        "after-snow" => WeatherKind.AfterSnow,
        _ => throw new ArgumentException($"Unknown weather: {text}.", nameof(text))
    };

    private static float? Float(IReadOnlyDictionary<string, string> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out var text)) return null;
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value) || value is < 0 or > 1)
            throw new ArgumentException($"--{name} requires a number in [0, 1].");
        return value;
    }
}
