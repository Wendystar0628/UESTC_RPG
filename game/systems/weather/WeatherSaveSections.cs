using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Systems.Weather;

/// <summary>The whole weather save: the seed plus the manual choice, pin, quiet window and wetness residue. Automatic weather is not saved.</summary>
public sealed record WeatherSave(uint Seed, WeatherProcessState Process);

[KernelService(typeof(WeatherSection), Owner = "WP-45a")]
[SaveSection("weather", Owner = "WP-45a", Version = 1, Scope = SaveScope.Global, Phase = RestorePhase.World, Order = 20)]
public sealed class WeatherSection(WeatherProcess weather, IMapService maps, IWeatherProfileSource profiles) : ISaveSection<WeatherSave>
{
    public JsonTypeInfo<WeatherSave> TypeInfo => WeatherJsonContext.Contract.WeatherSave;
    public bool IsSettled => !weather.Transitioning;

    public WeatherSave Capture(in CaptureContext c) =>
        new(weather.Seed, c.Moment is { } moment ? weather.CaptureAt(c.Map, in moment) : weather.Capture(c.Map));

    public void Restore(WeatherSave dto, RestoreContext c)
    {
        var state = dto.Process;
        if (!WeatherRules.IsNative(state.Selection.Kind, state.Season))
        {
            var mapped = WeatherRules.MapToSeason(state.Selection.Kind, state.Season);
            state = state with
            {
                Selection = state.Selection with
                {
                    Kind = mapped,
                    Intensity = state.Selection.Kind == WeatherKind.Thunderstorm ? 1 : state.Selection.Intensity
                }
            };
            c.Repair("天气已替换为本季可用的天气。");
        }
        weather.RestoreSeed(dto.Seed);
        weather.Restore(c.Map, state);
    }

    public WeatherSave CreateDefault(NewGameContext c)
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
        return new(c.Seed, initial.Capture(c.Map));
    }
}
