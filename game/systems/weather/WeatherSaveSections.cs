using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Systems.Weather;

public sealed record WeatherSeedSave(uint Seed);

[KernelService(typeof(WeatherSeedSection), Owner = "WP-45a")]
[SaveSection("weather", Owner = "WP-45a", Version = 1, Scope = SaveScope.Global, Phase = RestorePhase.World, Order = 20)]
public sealed class WeatherSeedSection(WeatherProcess weather) : ISaveSection<WeatherSeedSave>
{
    public JsonTypeInfo<WeatherSeedSave> TypeInfo => WeatherJsonContext.Contract.WeatherSeedSave;
    public WeatherSeedSave Capture(in CaptureContext c) => new(weather.Seed);
    public void Restore(WeatherSeedSave dto, RestoreContext c) => weather.RestoreSeed(dto.Seed);
    public WeatherSeedSave CreateDefault(NewGameContext c) => new(c.Seed);
}

[KernelService(typeof(WeatherMapSection), Owner = "WP-45a")]
[SaveSection("weather", Owner = "WP-45a", Version = 1, Scope = SaveScope.PerMap, Phase = RestorePhase.World, Order = 21)]
public sealed class WeatherMapSection(WeatherProcess weather, IMapService maps, IWeatherProfileSource profiles) : ISaveSection<WeatherProcessState>
{
    public JsonTypeInfo<WeatherProcessState> TypeInfo => WeatherJsonContext.Contract.WeatherProcessState;
    public bool IsSettled => !weather.Transitioning;
    public WeatherProcessState Capture(in CaptureContext c) => c.Map == maps.Current && c.Moment is { } moment
        ? weather.CaptureAt(c.Map, in moment) : weather.Capture(c.Map);
    public void Restore(WeatherProcessState dto, RestoreContext c)
    {
        if (dto.Map != c.Map) throw new InvalidDataException("Weather section belongs to a different map.");
        if (!WeatherRules.IsNative(dto.Selection.Kind, dto.Season))
        {
            var mapped = WeatherRules.MapToSeason(dto.Selection.Kind, dto.Season);
            dto = dto with
            {
                Selection = dto.Selection with
                {
                    Kind = mapped,
                    Intensity = dto.Selection.Kind == WeatherKind.Thunderstorm ? 1 : dto.Selection.Intensity
                }
            };
            c.Repair("天气已替换为本季可用的天气。");
        }
        weather.BindProfile(c.Map, maps.Maps.Single(m => m.Id == c.Map).World.WeatherProfile);
        weather.Restore(c.Map, dto);
    }
    public WeatherProcessState CreateDefault(NewGameContext c)
    {
        var descriptor = maps.Maps.Single(m => m.Id == c.Map);
        var initial = new WeatherProcess(profiles);
        initial.Reset(new(c.Seed));
        initial.Enter(c.Map, descriptor.World.WeatherProfile, c.Start.Clock);
        initial.Step(new(c.Start.Clock, c.Start.Clock, 0, c.Start.Season, 0, 0, 0));
        if (descriptor.World.QuietUntilDusk)
            initial.Quiet(c.Start.Clock, WeatherRules.QuietEnd(c.Start.Clock, 0, c.Start.Season));
        else if (c.Start.Weather is { } kind && kind != WeatherKind.Clear)
            initial.Pin(c.Start.Clock, kind, c.Start.Intensity, c.Start.Clock, c.Start.Clock + 2);
        return initial.Capture(c.Map);
    }
}
