using Campus.Tests;
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
    [TestCase(false), TestCase(true), TestCategory(TestReason.Regression)]
    public void RequiredInstanceParticipantFailureRemainsTheOwnerFailure(bool ending)
    {
        using var world = CreateWorld(new TestWeatherProcess());
        var original = new IOException();
        var observed = false;
        world.InstanceAdvancing += _ => throw original;
        world.InstanceChanged += _ => observed = true;
        if (!ending) world.Restore(world.Capture(SaveContext(world)), Restore());
        Exception? actual = null;
        try { if (ending) world.EndInstance(); else world.CompleteRestore(); }
        catch (Exception error) { actual = error; }
        AssertThat(ReferenceEquals(actual, original) && !observed).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WeatherIsNotSteppedBeforeTheFirstMapEnters()
    {
        var weather = new TestWeatherProcess();
        var world = new WorldStateService(weather);
        world.Tick(.0625);
        AssertThat(weather.Steps.Count).IsEqual(0);
        var context = PresetContext(new WorldPreset(17.5, Season.Autumn), true);
        world.Freeze(context.Descriptor.World.Default.Clock, context.Descriptor.World.Default.Season, WorldTime.InitialAnimTime);
        world.OnMapLoaded(context);
        world.CompleteRestore();
        world.Tick(.0625);
        AssertThat(weather.Steps.Count).IsEqual(1);
        AssertThat(weather.Enters).IsEqual(1);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
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

        AssertThat(!world.IsSettled && world.Instance == 0).OverrideFailureMessage("Prepared boot values signed an active world.").IsTrue();
        AssertThrown(() => world.Tick(.0625)).IsInstanceOf<InvalidOperationException>();
        world.CompleteRestore();
        AssertThat(world.Instance).IsEqual(instance + 1);
        // The first World tick supplies the initial season and offset after the explicit commit.
        weather.Reset(new WeatherBoot(1, WeatherKind.Snow));
        var context = PresetContext(new WorldPreset(6, Season.Winter, WeatherKind.Snow), false);
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
    [TestCategory(TestReason.Contract)]
    public void ApplyOrdersSeasonClockPinAndCapturesTheirCommonEndpoint()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather);
        world.SetFlow(0);
        world.Apply(new WorldPreset(21, Season.Winter, WeatherKind.Thunderstorm));
        AssertThat(weather.Calls.SequenceEqual(new[] { "reseason", "pin" })).OverrideFailureMessage("Apply weather hooks are out of order.").IsTrue();
        AssertThat(weather.PinnedKind).IsEqual(WeatherKind.Snow);
        AssertThat(weather.PinnedIntensity).IsEqual(1f);
        AssertThat(Math.Abs((weather.PinFrom) - (world.TargetHours - .5)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {world.TargetHours - .5:R}, got {weather.PinFrom:R}.").IsTrue();
        AssertThat(Math.Abs((weather.PinTo) - (world.TargetHours + 2)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {world.TargetHours + 2:R}, got {weather.PinTo:R}.").IsTrue();
        var snapshot = world.Capture(SaveContext(world));
        for (var i = 0; i < 60; i++) world.Tick(.1);
        AssertThat(Math.Abs((world.Clock) - (21)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {21:R}, got {world.Clock:R}.").IsTrue();
        AssertThat(world.SeasonTarget).IsEqual(Season.Winter);
        AssertThat(world.Capture(SaveContext(world))).IsEqual(snapshot with { AnimTime = world.AnimTime });
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ReducedMotionUsesHalfSecondTransitionTiming()
    {
        var world = CreateWorld(new TestWeatherProcess(), new TestMotionSettings(true));
        world.SetFlow(0);
        var previous = world.SeasonPos;
        double? captured = null;
        world.ReducedTransitionRequested += () => captured ??= world.SeasonPos;
        world.Apply(new WorldPreset(21, Season.Spring));
        AssertThat(captured == previous).OverrideFailureMessage("The old season was not captured before the transition.").IsTrue();
        world.Tick(.1);
        AssertThat(world.SeasonPos).IsEqual((double)(int)Season.Spring);
        AssertThat(Math.Abs(world.Clock - 21) <= TestTolerance.WorldHours)
            .OverrideFailureMessage("The new scene was not settled beneath the reduced-motion screenshot.").IsTrue();
        for (var i = 1; i < 5; i++) world.Tick(.1);
        AssertThat(world.IsSettled).OverrideFailureMessage("Reduced-motion world transition exceeded half a second.").IsTrue();
        AssertThat(Math.Abs((world.Clock) - (21)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {21:R}, got {world.Clock:R}.").IsTrue();
        AssertThat(world.SeasonTarget).IsEqual(Season.Spring);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DefaultComesFromMapAndKeepsHeroBandWithoutChangingInstance()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather, initialize: false);
        var context = PresetContext(new WorldPreset(17.5, Season.Autumn, WeatherKind.Clear), true);
        var notifications = 0;
        world.InstanceChanged += _ => notifications++;
        world.OnMapLoaded(context);
        world.Freeze(17 + 35 / 60d, Season.Autumn, 240);
        world.CompleteRestore();
        notifications = 0;
        var instance = world.Instance;
        world.ResetDefault();
        AssertThat(Math.Abs((world.TargetHours) - (17 + 35 / 60d)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {17 + 35 / 60d:R}, got {world.TargetHours:R}.").IsTrue();
        AssertThat(Math.Abs((weather.QuietTo) - (ArtEphemeris.DuskEnd(Season.Autumn))) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {ArtEphemeris.DuskEnd(Season.Autumn):R}, got {weather.QuietTo:R}.").IsTrue();
        AssertThat(world.Flow.Rate).IsEqual(1);
        AssertThat(world.Instance).IsEqual(instance);
        world.OnMapUnloading(context.Descriptor.Id);
        var before = world.WorldHours;
        var calls = weather.Steps.Count;
        world.Tick(.1);
        AssertThat(world.WorldHours > before && weather.Steps.Count == calls).OverrideFailureMessage("Map unload must not stop the shared clock or step an absent map.").IsTrue();
        AssertThat(world.Instance).IsEqual(instance);
        AssertThat(weather.Enters).IsEqual(1);
        AssertThat(notifications).IsEqual(0);
    }
}
