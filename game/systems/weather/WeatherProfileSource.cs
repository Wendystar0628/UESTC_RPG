using Campus.Contracts;

namespace Campus.Systems.Weather;

/// <summary>The only engine IO in the weather process; a profile is loaded once per map profile.</summary>
[KernelService(typeof(IWeatherProfileSource))]
public sealed class WeatherProfileSource(IRequiredResources resources) : IWeatherProfileSource
{
    private readonly Dictionary<string, WeatherProfile> _cache = new(StringComparer.Ordinal);
    public WeatherProfile Load(string profile)
    {
        if (string.IsNullOrWhiteSpace(profile) || profile.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            throw new ArgumentException("Invalid weather profile name.", nameof(profile));
        if (!_cache.TryGetValue(profile, out var result))
        {
            result = WeatherProfile.Parse(resources.ReadText($"res://data/weather/{profile}/profile.json"));
            _cache.Add(profile, result);
        }
        return result;
    }
}
