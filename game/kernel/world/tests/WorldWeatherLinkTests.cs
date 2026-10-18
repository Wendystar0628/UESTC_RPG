using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.World;
using GdUnit4;
using static Campus.Tests.World.WorldTestKit;

namespace Campus.Tests.World;

/// <summary>How the world hands the weather process its map, presets and defaults.</summary>
[TestSuite]
public sealed class WorldWeatherLinkTests
{
    [TestCase]
    public void WeatherIsNotSteppedBeforeTheFirstMapEnters()
    {
        var weather = new RecordingWeatherProcess();
        var world = new WorldStateService(weather);
        world.Tick(.0625);
        Equal(weather.Steps.Count, 0);
        world.OnMapLoaded(PresetContext(new WorldPreset(17.5, Season.Autumn), true));
        world.Tick(.0625);
        Equal(weather.Steps.Count, 1);
        Equal(weather.Enters, 1);
    }

    [TestCase]
    public void WinterEarlyClockInitializesWithoutWeatherTransitions()
    {
        var weather = new RecordingWeatherProcess();
        var world = new WorldStateService(weather);
        var instance = world.Instance;
        world.Freeze(6, Season.Winter, 240);
        Equal(world.WorldHours, 6);
        Equal(world.Clock, 6);
        Equal(world.SeasonTarget, Season.Winter);
        Equal(world.Instance, instance);
        Equal(weather.Reseasons, 0);
        Equal(weather.Resets, 0);
        Equal(weather.Quiets, 0);
        Equal(weather.Enters, 0);
        Equal(weather.Steps.Count, 0);
        Equal(weather.Calls.Count, 0);

        // Boot owns Reset after Freeze; the first World tick supplies the initial season and offset.
        weather.Reset(new WeatherBoot(1, WeatherKind.Snow));
        var context = PresetContext(new WorldPreset(6, Season.Winter, WeatherKind.Snow), false);
        world.OnMapLoaded(context);
        world.OnMapLoaded(context);
        Equal(weather.Enters, 1);
        Equal(weather.LastEnterHours, 6);
        world.Tick(.0625);
        Equal(weather.Steps.Count, 1);
        var first = weather.Steps[0];
        Equal(first.WorldHours, 6);
        Equal(first.TargetHours, 6);
        Equal(first.ClockOffset, 0);
        Equal(first.Season, Season.Winter);
        Equal(first.SunAltDeg, world.Sun.AltDeg);
        Equal(weather.Reseasons, 0);
    }

    [TestCase]
    public void ApplyOrdersSeasonClockPinAndCapturesTheirCommonEndpoint()
    {
        var weather = new RecordingWeatherProcess();
        var world = CreateWorld(weather);
        world.SetFlow(0);
        world.Apply(new WorldPreset(21, Season.Winter, WeatherKind.Thunderstorm));
        Check(weather.Calls.SequenceEqual(new[] { "reseason", "pin" }), "Apply weather hooks are out of order.");
        Equal(weather.PinnedKind, WeatherKind.Snow);
        Equal(weather.PinnedIntensity, 1f);
        Near(weather.PinFrom, world.TargetHours - .5);
        Near(weather.PinTo, world.TargetHours + 2);
        var snapshot = world.Capture(default);
        for (var i = 0; i < 60; i++) world.Tick(.1);
        Near(world.Clock, 21);
        Equal(world.SeasonTarget, Season.Winter);
        Equal(world.Capture(default), snapshot with { AnimTime = world.AnimTime });
    }

    [TestCase]
    public void ReducedMotionUsesHalfSecondTransitionTiming()
    {
        var world = CreateWorld(new RecordingWeatherProcess(), new TestMotionSettings(true));
        world.SetFlow(0);
        world.Apply(new WorldPreset(21, Season.Spring));
        for (var i = 0; i < 5; i++) world.Tick(.1);
        Check(world.IsSettled, "Reduced-motion world transition exceeded half a second.");
        Near(world.Clock, 21);
        Equal(world.SeasonTarget, Season.Spring);
    }

    [TestCase]
    public void DefaultComesFromMapAndKeepsHeroBandWithoutChangingInstance()
    {
        var weather = new RecordingWeatherProcess();
        var world = CreateWorld(weather);
        var context = PresetContext(new WorldPreset(17.5, Season.Autumn, WeatherKind.Clear), true);
        world.OnMapLoaded(context);
        world.Freeze(17 + 35 / 60d, Season.Autumn, 240);
        var instance = world.Instance;
        world.ResetDefault();
        Near(world.TargetHours, 17 + 35 / 60d);
        Near(weather.QuietTo, ArtEphemeris.DuskEnd(Season.Autumn));
        Equal(world.Flow.Rate, 1);
        Equal(world.Instance, instance);
        world.OnMapUnloading(context.Descriptor.Id);
        var before = world.WorldHours;
        var calls = weather.Steps.Count;
        world.Tick(.1);
        Check(world.WorldHours > before && weather.Steps.Count == calls, "Map unload must not stop the shared clock or step an absent map.");
        world.OnMapLoaded(context);
        Equal(world.Instance, instance);
        Equal(weather.Enters, 2);
    }
}
