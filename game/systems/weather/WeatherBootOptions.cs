using Campus.Contracts;

namespace Campus.Systems.Weather;

internal static class WeatherBootOptions
{
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
}
