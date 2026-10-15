using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;

namespace Campus.Systems.Weather;

public sealed record WeatherPhase(WeatherKind Kind, double MinHours, double MaxHours, float MinIntensity = 1, float MaxIntensity = 1);
public sealed record WeatherTemplate(string Id, Season Season, double RatePerDay, double FromClock, double ToClock, WeatherPhase[] Phases)
{
    public double WindowHours => FromClock == ToClock ? 24 : WeatherRules.Clock(ToClock - FromClock);
    public double Hazard(double clock) => WeatherRules.Clock(clock - FromClock) < WindowHours ? RatePerDay / WindowHours : 0;
}
public sealed record WeatherEdge(WeatherKind From, WeatherKind To);
public sealed record WeatherProfile(int Version, double ClearGapHours, double PrecipGapHours, double MaxPrecipHoursPerDay,
    double BlendHours, WeatherTemplate[] Templates, WeatherEdge[] Edges)
{
    public static WeatherProfile Parse(string json)
    {
        var profile = JsonSerializer.Deserialize(json, WeatherJsonContext.Contract.WeatherProfile)
            ?? throw new InvalidDataException("Weather profile is null.");
        if (profile.Version != 1 || !double.IsFinite(profile.ClearGapHours) || profile.ClearGapHours < 2.5 ||
            !double.IsFinite(profile.PrecipGapHours) || profile.PrecipGapHours < 8 ||
            !double.IsFinite(profile.MaxPrecipHoursPerDay) || profile.MaxPrecipHoursPerDay is <= 0 or > 12 ||
            !double.IsFinite(profile.BlendHours) || profile.BlendHours < .6 || profile.Templates is not { Length: > 0 } ||
            profile.Edges is not { Length: > 0 }) throw new InvalidDataException("Invalid weather profile limits.");
        if (profile.Templates.Select(t => t.Id).Distinct(StringComparer.Ordinal).Count() != profile.Templates.Length)
            throw new InvalidDataException("Weather template IDs must be unique.");
        foreach (var template in profile.Templates)
        {
            WeatherRules.ValidateSeason(template.Season);
            if (string.IsNullOrWhiteSpace(template.Id) || !double.IsFinite(template.RatePerDay) || template.RatePerDay <= 0 ||
                !double.IsFinite(template.FromClock) || template.FromClock is < 0 or >= 24 ||
                !double.IsFinite(template.ToClock) || template.ToClock is < 0 or >= 24 || template.Phases is not { Length: > 0 })
                throw new InvalidDataException("Invalid weather template.");
            WeatherKind previous = WeatherKind.Clear;
            foreach (var phase in template.Phases)
            {
                if (!WeatherRules.IsNative(phase.Kind, template.Season) || phase.Kind == WeatherKind.Clear ||
                    !double.IsFinite(phase.MinHours) || !double.IsFinite(phase.MaxHours) || phase.MinHours <= 0 ||
                    phase.MinHours < WeatherRules.MinimumHours(phase.Kind) || phase.MaxHours < phase.MinHours)
                    throw new InvalidDataException($"Invalid phase in {template.Id}.");
                WeatherRules.ValidateIntensity(phase.MinIntensity);
                WeatherRules.ValidateIntensity(phase.MaxIntensity);
                if (phase.MinIntensity > phase.MaxIntensity || !profile.Allows(previous, phase.Kind))
                    throw new InvalidDataException($"Unvalidated edge in {template.Id}.");
                previous = phase.Kind;
            }
            if (!profile.Allows(previous, WeatherKind.Clear)) throw new InvalidDataException($"Missing clear return in {template.Id}.");
        }
        foreach (var season in Enum.GetValues<Season>())
            if (!profile.Templates.Any(t => t.Season == season)) throw new InvalidDataException($"No weather for {season}.");
        return profile;
    }
    public bool Allows(WeatherKind from, WeatherKind to) => from == to || Edges.Any(e => e.From == from && e.To == to);
}

public interface IWeatherProfileSource
{
    WeatherProfile Load(string profile);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(WeatherProfile))]
[JsonSerializable(typeof(WeatherSeedSave))]
[JsonSerializable(typeof(WeatherProcessState))]
internal partial class WeatherJsonContext : JsonSerializerContext
{
    public static WeatherJsonContext Contract { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    });
}
