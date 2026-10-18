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
        var boot = new WeatherBoot(seed, kind, intensity, wet, remaining);
        Validate(boot, season);
        return boot;
    }

    public static void Validate(WeatherBoot boot, Season? season = null)
    {
        if (boot.Intensity is { } i) WeatherRules.ValidateIntensity(i);
        if (boot.Wet is { } wet) WeatherRules.ValidateIntensity(wet);
        if (boot.RemainingMinutes is { } minutes && (!double.IsFinite(minutes) || minutes <= 0 || !boot.Kind.HasValue))
            throw new ArgumentException("A positive --weather-remaining requires an explicit weather kind.");
        if (boot.Kind is { } kind)
        {
            _ = WeatherRules.DefaultIntensity(kind);
            if (season is { } target && !WeatherRules.IsNative(kind, target))
                throw new ArgumentException($"{kind} is unavailable in {target}; use {WeatherRules.MapToSeason(kind, target)}.");
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
