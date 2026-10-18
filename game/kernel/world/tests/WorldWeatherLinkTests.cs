using static GdUnit4.Assertions;
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
        var weather = new TestWeatherProcess();
        var world = new WorldStateService(weather);
        world.Tick(.0625);
        AssertThat(weather.Steps.Count).IsEqual(0);
        world.OnMapLoaded(PresetContext(new WorldPreset(17.5, Season.Autumn), true));
        world.Tick(.0625);
        AssertThat(weather.Steps.Count).IsEqual(1);
        AssertThat(weather.Enters).IsEqual(1);
    }

    [TestCase]
    public void WinterEarlyClockInitializesWithoutWeatherTransitions()
    {
        var weather = new TestWeatherProcess();
        var world = new WorldStateService(weather);
        var instance = world.Instance;
        world.Freeze(6, Season.Winter, 240);
        AssertThat(world.WorldHours).IsEqual(6);
        AssertThat(world.Clock).IsEqual(6);
        AssertThat(world.SeasonTarget).IsEqual(Season.Winter);
        AssertThat(world.Instance).IsEqual(instance);
        AssertThat(weather.Reseasons).IsEqual(0);
        AssertThat(weather.Resets).IsEqual(0);
        AssertThat(weather.Quiets).IsEqual(0);
        AssertThat(weather.Enters).IsEqual(0);
        AssertThat(weather.Steps.Count).IsEqual(0);
        AssertThat(weather.Calls.Count).IsEqual(0);

        // Boot owns Reset after Freeze; the first World tick supplies the initial season and offset.
        weather.Reset(new WeatherBoot(1, WeatherKind.Snow));
        var context = PresetContext(new WorldPreset(6, Season.Winter, WeatherKind.Snow), false);
        world.OnMapLoaded(context);
        world.OnMapLoaded(context);
        AssertThat(weather.Enters).IsEqual(1);
        AssertThat(weather.LastEnterHours).IsEqual(6);
        world.Tick(.0625);
        AssertThat(weather.Steps.Count).IsEqual(1);
        var first = weather.Steps[0];
        AssertThat(first.WorldHours).IsEqual(6);
        AssertThat(first.TargetHours).IsEqual(6);
        AssertThat(first.ClockOffset).IsEqual(0);
        AssertThat(first.Season).IsEqual(Season.Winter);
        AssertThat(first.SunAltDeg).IsEqual(world.Sun.AltDeg);
        AssertThat(weather.Reseasons).IsEqual(0);
    }

    [TestCase]
    public void ApplyOrdersSeasonClockPinAndCapturesTheirCommonEndpoint()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather);
        world.SetFlow(0);
        world.Apply(new WorldPreset(21, Season.Winter, WeatherKind.Thunderstorm));
        AssertThat(weather.Calls.SequenceEqual(new[] { "reseason", "pin" })).OverrideFailureMessage("Apply weather hooks are out of order.").IsTrue();
        AssertThat(weather.PinnedKind).IsEqual(WeatherKind.Snow);
        AssertThat(weather.PinnedIntensity).IsEqual(1f);
        AssertThat(Math.Abs((weather.PinFrom) - (world.TargetHours - .5)) <= 1e-10).OverrideFailureMessage($"Expected {world.TargetHours - .5:R}, got {weather.PinFrom:R}.").IsTrue();
        AssertThat(Math.Abs((weather.PinTo) - (world.TargetHours + 2)) <= 1e-10).OverrideFailureMessage($"Expected {world.TargetHours + 2:R}, got {weather.PinTo:R}.").IsTrue();
        var snapshot = world.Capture(default);
        for (var i = 0; i < 60; i++) world.Tick(.1);
        AssertThat(Math.Abs((world.Clock) - (21)) <= 1e-10).OverrideFailureMessage($"Expected {21:R}, got {world.Clock:R}.").IsTrue();
        AssertThat(world.SeasonTarget).IsEqual(Season.Winter);
        AssertThat(world.Capture(default)).IsEqual(snapshot with { AnimTime = world.AnimTime });
    }

    [TestCase]
    public void ReducedMotionUsesHalfSecondTransitionTiming()
    {
        var world = CreateWorld(new TestWeatherProcess(), new TestMotionSettings(true));
        world.SetFlow(0);
        world.Apply(new WorldPreset(21, Season.Spring));
        for (var i = 0; i < 5; i++) world.Tick(.1);
        AssertThat(world.IsSettled).OverrideFailureMessage("Reduced-motion world transition exceeded half a second.").IsTrue();
        AssertThat(Math.Abs((world.Clock) - (21)) <= 1e-10).OverrideFailureMessage($"Expected {21:R}, got {world.Clock:R}.").IsTrue();
        AssertThat(world.SeasonTarget).IsEqual(Season.Spring);
    }

    [TestCase]
    public void DefaultComesFromMapAndKeepsHeroBandWithoutChangingInstance()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather);
        var context = PresetContext(new WorldPreset(17.5, Season.Autumn, WeatherKind.Clear), true);
        var notifications = 0;
        world.InstanceChanged += _ => notifications++;
        world.OnMapLoaded(context);
        world.Freeze(17 + 35 / 60d, Season.Autumn, 240);
        var instance = world.Instance;
        world.ResetDefault();
        AssertThat(Math.Abs((world.TargetHours) - (17 + 35 / 60d)) <= 1e-10).OverrideFailureMessage($"Expected {17 + 35 / 60d:R}, got {world.TargetHours:R}.").IsTrue();
        AssertThat(Math.Abs((weather.QuietTo) - (ArtEphemeris.DuskEnd(Season.Autumn))) <= 1e-10).OverrideFailureMessage($"Expected {ArtEphemeris.DuskEnd(Season.Autumn):R}, got {weather.QuietTo:R}.").IsTrue();
        AssertThat(world.Flow.Rate).IsEqual(1);
        AssertThat(world.Instance).IsEqual(instance);
        world.OnMapUnloading(context.Descriptor.Id);
        var before = world.WorldHours;
        var calls = weather.Steps.Count;
        world.Tick(.1);
        AssertThat(world.WorldHours > before && weather.Steps.Count == calls).OverrideFailureMessage("Map unload must not stop the shared clock or step an absent map.").IsTrue();
        world.OnMapLoaded(context);
        AssertThat(world.Instance).IsEqual(instance);
        AssertThat(weather.Enters).IsEqual(2);
        AssertThat(notifications).IsEqual(0);
    }
}
