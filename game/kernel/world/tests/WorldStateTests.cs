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

/// <summary>Solar arcs, clock jumps, flow rates and season transitions.</summary>
[TestSuite]
public sealed class WorldStateTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SolarHeroAndFourSeasonPresetsAreAuthored()
    {
        var hero = ArtEphemeris.Evaluate(17.5, 2).Sun;
        AssertThat(Math.Abs((hero.AltDeg) - (5.5)) <= TestTolerance.AngleDegrees).OverrideFailureMessage($"Expected {5.5:R}, got {hero.AltDeg:R}.").IsTrue();
        AssertThat(Math.Abs((hero.AzDeg) - (241)) <= TestTolerance.AngleDegrees).OverrideFailureMessage($"Expected {241:R}, got {hero.AzDeg:R}.").IsTrue();
        foreach (var season in Enum.GetValues<Season>())
        {
            var track = ArtEphemeris.At(season);
            AssertThat(Math.Abs((ArtEphemeris.Evaluate(track.Rise, (int)season).Sun.AltDeg) - (0)) <= TestTolerance.AngleDegrees).OverrideFailureMessage($"Expected {0:R}, got {ArtEphemeris.Evaluate(track.Rise, (int)season).Sun.AltDeg:R}.").IsTrue();
            AssertThat(Math.Abs((ArtEphemeris.Evaluate((track.Rise + track.Set) / 2, (int)season).Sun.AltDeg) - (track.Height)) <= TestTolerance.AngleDegrees).OverrideFailureMessage($"Expected {track.Height:R}, got {ArtEphemeris.Evaluate((track.Rise + track.Set) / 2, (int)season).Sun.AltDeg:R}.").IsTrue();
            AssertThat(Math.Abs((ArtEphemeris.Preset(ClockPreset.Dusk, season)) - (track.Set - 35 / 60d)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {track.Set - 35 / 60d:R}, got {ArtEphemeris.Preset(ClockPreset.Dusk, season):R}.").IsTrue();
            AssertThat(Math.Abs((ArtEphemeris.Evaluate(track.Set, (int)season).Moon.AzDeg) - (62)) <= TestTolerance.AngleDegrees).OverrideFailureMessage($"Expected {62:R}, got {ArtEphemeris.Evaluate(track.Set, (int)season).Moon.AzDeg:R}.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SolarHeightQueryNormalizesPeriodsAndNeverMutatesWorld()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather);
        world.SetFlow(0);
        world.SetSeason(Season.Winter);
        world.Advance(3);
        world.Tick(.0625);
        var snapshot = world.Capture(default);
        var readings = (world.Rev, world.Instance, world.WorldHours, world.Clock, world.SeasonPos,
            world.Sun, world.Moon, world.Transition, weather.Steps.Count, weather.Reseasons);
        foreach (var season in Enum.GetValues<Season>())
        {
            foreach (var clock in new[] { 0d, 6d, 12d, 17.5, 23.875 })
            {
                var height = world.SunAltDegAt(clock, season);
                AssertThat(height).IsEqual(ArtEphemeris.Evaluate(clock, (int)season).Sun.AltDeg);
                AssertThat(world.SunAltDegAt(clock + 72, season)).IsEqual(height);
                AssertThat(world.SunAltDegAt(clock - 72, season)).IsEqual(height);
            }
        }
        AssertThat(float.IsFinite(world.SunAltDegAt(double.MaxValue, Season.Spring))).OverrideFailureMessage("Finite periodic input must remain finite.").IsTrue();
        AssertThrown(() => world.SunAltDegAt(double.NaN, Season.Spring)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => world.SunAltDegAt(double.PositiveInfinity, Season.Spring)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => world.SunAltDegAt(double.NegativeInfinity, Season.Spring)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => world.SunAltDegAt(12, (Season)4)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThat(world.Capture(default)).IsEqual(snapshot);
        AssertThat((world.Rev, world.Instance, world.WorldHours, world.Clock, world.SeasonPos,
            world.Sun, world.Moon, world.Transition, weather.Steps.Count, weather.Reseasons)).IsEqual(readings);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void JumpWrapsForwardAndStopsWithinThreeSeconds()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather);
        world.SetFlow(0);
        world.SetClock(6);
        AssertThat(world.WorldHours == 17.5 && world.TargetHours == 30).OverrideFailureMessage("Jump must have a future target and no immediate hard cut.").IsTrue();
        var previous = world.WorldHours;
        for (var i = 0; i < 31; i++)
        {
            world.Tick(.1);
            AssertThat(world.WorldHours >= previous && world.WorldHours <= 30).OverrideFailureMessage("Jump reversed or overshot.").IsTrue();
            previous = world.WorldHours;
        }
        AssertThat(world.WorldHours).IsEqual(30);
        AssertThat(world.Clock).IsEqual(6);
        AssertThat(world.IsSettled && weather.Steps.Count == 31).OverrideFailureMessage("Clock/weather frame sequence missing.").IsTrue();
        AssertThat(weather.Steps[0].TargetHours == 30 && weather.Steps[^1].WorldHours == 30).OverrideFailureMessage("Weather did not receive final jump target.").IsTrue();
        world.SetClock(6);
        world.Tick(.1);
        AssertThat(world.WorldHours).IsEqual(30);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EveryFlowRateAndLongResumeDeltaUseWorldSeconds()
    {
        foreach (var rate in new[] { 0, 1, 10, 60, 360, 1440 })
        {
            var world = CreateWorld(new TestWeatherProcess());
            world.SetFlow(rate);
            world.Tick(600);
            AssertThat(Math.Abs((world.WorldHours) - (17.5 + rate * .1 / 3600)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {17.5 + rate * .1 / 3600:R}, got {world.WorldHours:R}.").IsTrue();
            AssertThat(Math.Abs((world.AnimTime) - (WorldTime.InitialAnimTime + .1)) <= TestTolerance.TimeSeconds).OverrideFailureMessage($"Expected {WorldTime.InitialAnimTime + .1:R}, got {world.AnimTime:R}.").IsTrue();
        }
        var paused = CreateWorld(new TestWeatherProcess());
        paused.SetFlow(0);
        paused.Tick(.1, freezeAnimation: true);
        AssertThat(paused.AnimTime).IsEqual(WorldTime.InitialAnimTime);
        var wrapping = CreateWorld(new TestWeatherProcess(), initialize: false);
        wrapping.Freeze(0, Season.Spring, WorldTime.AnimLoopSeconds - .01);
        wrapping.SetStepRate(WorldTime.ThirtyFpsStepRate);
        wrapping.Tick(.1);
        AssertThat(Math.Abs((wrapping.AnimTime) - (.09)) <= TestTolerance.TimeSeconds).OverrideFailureMessage($"Expected {.09:R}, got {wrapping.AnimTime:R}.").IsTrue();
        AssertThat(Math.Abs((wrapping.Step12) - (1d / WorldTime.ThirtyFpsStepRate)) <= TestTolerance.TimeSeconds).OverrideFailureMessage($"Expected {1d / WorldTime.ThirtyFpsStepRate:R}, got {wrapping.Step12:R}.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Determinism)]
    public void FlowToLandsBitExactlyAtAllCinematicSpeeds()
    {
        foreach (var rate in new[] { 60, 360, 1440 })
        {
            var world = CreateWorld(new TestWeatherProcess(), initialize: false);
            world.Freeze(17 + 5 / 60d, Season.Autumn, 240);
            using var flow = world.AcquireFlow(FlowPriority.Cinematic, 0);
            flow.FlowTo(17.5, rate);
            for (var i = 0; i < 400 && world.Flow.Rate != 0; i++) world.Tick(.1);
            AssertThat(world.WorldHours).IsEqual(17.5);
            AssertThat(world.Clock).IsEqual(17.5);
            AssertThat(world.Flow.Rate).IsEqual(0);
            AssertThat(world.Transition == WorldTransition.None).OverrideFailureMessage("FlowTo did not clear its time transition.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LeaseRateAndSecondFlowToCancelThePreviousStop()
    {
        var world = CreateWorld(new TestWeatherProcess());
        using var flow = world.AcquireFlow(FlowPriority.Cinematic, 0);
        flow.FlowTo(17.51, 1440);
        flow.SetRate(1440);
        world.Tick(.1);
        AssertThat(world.Clock > 17.51 && world.Flow.Rate == 1440).OverrideFailureMessage("Lease rate left the previous stop active.").IsTrue();
        flow.FlowTo(17.56, 1440);
        flow.FlowTo(17.6, 1440);
        world.Tick(.1);
        world.Tick(.1);
        AssertThat(world.Clock).IsEqual(17.6);
        AssertThat(world.Flow.Rate).IsEqual(0);
        flow.FlowTo(world.Clock, 1);
        AssertThat(world.Flow.Rate).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FlowToKeepsItsClockDestinationWhenTheSeasonChanges()
    {
        var world = CreateWorld(new TestWeatherProcess());
        var instance = world.Instance;
        using var flow = world.AcquireFlow(FlowPriority.Cinematic, 0);
        // Tick caps each delta at 0.1 s; the highest supported rate covers the next day within the frame budget.
        flow.FlowTo(18, FlowRates.All[^1]);
        world.SetSeason(Season.Summer);
        var endpoint = world.Capture(default);
        AssertThat(Math.Abs((ArtEphemeris.Wrap(endpoint.WorldHours + endpoint.ClockOffset, 24)) - (18)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {18:R}, got {ArtEphemeris.Wrap(endpoint.WorldHours + endpoint.ClockOffset, 24):R}.").IsTrue();
        var previous = world.WorldHours;
        for (var frame = 0; frame < TestSamples.FrameLoop && world.Flow.Rate != 0; frame++)
        {
            world.Tick(.1);
            AssertThat(world.WorldHours >= previous).OverrideFailureMessage("Season rebasing reversed the FlowTo timeline.").IsTrue();
            previous = world.WorldHours;
        }
        AssertThat(world.Flow.Rate).IsEqual(0);
        AssertThat(Math.Abs((world.Clock) - (18)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {18:R}, got {world.Clock:R}.").IsTrue();
        AssertThat(world.SeasonTarget).IsEqual(Season.Summer);
        AssertThat(world.Instance).IsEqual(instance);
        AssertThat(world.WorldHours > 24 && world.WorldHours <= 17.5 + 24).OverrideFailureMessage("Summer's later solar clock requires the next forward appearance of 18:00.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DragUsesDampedMonotoneMotionAcrossMidnightAndRejectsReverse()
    {
        var world = CreateWorld(new TestWeatherProcess(), initialize: false);
        world.Freeze(23.9, Season.Autumn, 240);
        world.SetClock(.1, ClockSetMode.Drag);
        AssertThat(Math.Abs((world.TargetHours) - (24.1)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {24.1:R}, got {world.TargetHours:R}.").IsTrue();
        world.SetClock(23.9, ClockSetMode.Drag);
        AssertThat(Math.Abs((world.TargetHours) - (24.1)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {24.1:R}, got {world.TargetHours:R}.").IsTrue();
        var previous = world.WorldHours;
        for (var i = 0; i < 100; i++)
        {
            world.Tick(i % 3 == 0 ? .1 : .016);
            AssertThat(world.WorldHours >= previous && world.WorldHours <= 24.1 + TestTolerance.WorldHours).OverrideFailureMessage("Spring reversed or overshot.").IsTrue();
            previous = world.WorldHours;
        }
        AssertThat(Math.Abs((world.Clock) - (.1)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {.1:R}, got {world.Clock:R}.").IsTrue();
        AssertThat(!world.Scrubbing).OverrideFailureMessage("Drag hold was never released.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SeasonPreservesSolarArcAndTimelineAndInterruptsFromCurrentValue()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather);
        world.SetFlow(0);
        var instance = world.Instance;
        var phase = ArtEphemeris.Phase(world.Clock, world.SeasonPos);
        world.SetSeason(Season.Spring);
        AssertThat(world.SeasonTo == 4).OverrideFailureMessage("Season shader target must be unfolded forward.").IsTrue();
        for (var i = 0; i < 20; i++)
        {
            world.Tick(.1);
            AssertThat(Math.Abs((ArtEphemeris.Phase(world.Clock, world.SeasonPos)) - (phase)) <= TestTolerance.FloatRelative).OverrideFailureMessage($"Expected {phase:R}, got {ArtEphemeris.Phase(world.Clock, world.SeasonPos):R}.").IsTrue();
            AssertThat(world.WorldHours).IsEqual(17.5);
        }
        var before = world.SeasonPos;
        world.SetSeason(Season.Summer);
        AssertThat(Math.Abs((world.SeasonFrom) - (before)) <= TestTolerance.FloatRelative).OverrideFailureMessage($"Expected {before:R}, got {world.SeasonFrom:R}.").IsTrue();
        for (var i = 0; i < 60; i++) world.Tick(.1);
        AssertThat(world.SeasonPos).IsEqual(1);
        AssertThat(Math.Abs((ArtEphemeris.Phase(world.Clock, world.SeasonPos)) - (phase)) <= TestTolerance.FloatRelative).OverrideFailureMessage($"Expected {phase:R}, got {ArtEphemeris.Phase(world.Clock, world.SeasonPos):R}.").IsTrue();
        AssertThat(world.WorldHours).IsEqual(17.5);
        AssertThat(world.Instance).IsEqual(instance);
        AssertThat(weather.Reseasons).IsEqual(2);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EveryForwardSeasonDistanceUsesItsSpecifiedDuration()
    {
        foreach (var start in Enum.GetValues<Season>())
            foreach (var distance in new[] { 1, 2, 3 })
                foreach (var reduced in new[] { false, true })
                {
                    foreach (var dt in new[] { .0625, .1 })
                    {
                        var world = CreateWorld(new TestWeatherProcess(), reduced ? new TestMotionSettings(true) : null, initialize: false);
                        world.Freeze(17.5, start, 240);
                        var target = (Season)(((int)start + distance) % 4);
                        world.SetSeason(target);
                        AssertThat(world.SeasonTo).IsEqual((double)(int)start + distance);
                        var duration = reduced ? .5 : distance == 1 ? 4 : 5.5;
                        var frames = (int)Math.Round(duration / dt);
                        // Cover exact binary intervals and decimal accumulation rounding.
                        for (var frame = 0; frame < frames - 1; frame++) world.Tick(dt);
                        AssertThat((world.Transition & WorldTransition.Season) != 0).OverrideFailureMessage($"Season transition finished early: {start}, +{distance}, reduced={reduced}, dt={dt}.").IsTrue();
                        world.Tick(dt);
                        AssertThat((world.Transition & WorldTransition.Season) == 0).OverrideFailureMessage($"Season transition exceeded its duration: {start}, +{distance}, reduced={reduced}, dt={dt}.").IsTrue();
                        AssertThat(world.SeasonPos).IsEqual((double)(int)target);
                        AssertThat(world.WorldHours).IsEqual(17.5);
                    }
                }
    }
}
