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
    public void SolarHeroAndFourSeasonPresetsAreAuthored()
    {
        var hero = ArtEphemeris.Evaluate(17.5, 2).Sun;
        Near(hero.AltDeg, 5.5, 1e-6);
        Near(hero.AzDeg, 241, 1e-6);
        foreach (var season in Enum.GetValues<Season>())
        {
            var track = ArtEphemeris.At(season);
            Near(ArtEphemeris.Evaluate(track.Rise, (int)season).Sun.AltDeg, 0, 1e-5);
            Near(ArtEphemeris.Evaluate((track.Rise + track.Set) / 2, (int)season).Sun.AltDeg, track.Height, 1e-5);
            Near(ArtEphemeris.Preset(ClockPreset.Dusk, season), track.Set - 35 / 60d);
            Near(ArtEphemeris.Evaluate(track.Set, (int)season).Moon.AzDeg, 62);
        }
        Near(ArtEphemeris.DuskEnd(Season.Autumn), 19.6, .025);
    }

    [TestCase]
    public void SolarHeightQueryNormalizesPeriodsAndNeverMutatesWorld()
    {
        var weather = new RecordingWeatherProcess();
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
                Equal(height, ArtEphemeris.Evaluate(clock, (int)season).Sun.AltDeg);
                Equal(world.SunAltDegAt(clock + 72, season), height);
                Equal(world.SunAltDegAt(clock - 72, season), height);
            }
        }
        Check(float.IsFinite(world.SunAltDegAt(double.MaxValue, Season.Spring)), "Finite periodic input must remain finite.");
        Throws<ArgumentOutOfRangeException>(() => world.SunAltDegAt(double.NaN, Season.Spring));
        Throws<ArgumentOutOfRangeException>(() => world.SunAltDegAt(double.PositiveInfinity, Season.Spring));
        Throws<ArgumentOutOfRangeException>(() => world.SunAltDegAt(double.NegativeInfinity, Season.Spring));
        Throws<ArgumentOutOfRangeException>(() => world.SunAltDegAt(12, (Season)4));
        Equal(world.Capture(default), snapshot);
        Equal((world.Rev, world.Instance, world.WorldHours, world.Clock, world.SeasonPos,
            world.Sun, world.Moon, world.Transition, weather.Steps.Count, weather.Reseasons), readings);
    }

    [TestCase]
    public void JumpWrapsForwardAndStopsWithinThreeSeconds()
    {
        var weather = new RecordingWeatherProcess();
        var world = CreateWorld(weather);
        world.SetFlow(0);
        world.SetClock(6);
        Check(world.WorldHours == 17.5 && world.TargetHours == 30, "Jump must have a future target and no immediate hard cut.");
        var previous = world.WorldHours;
        for (var i = 0; i < 31; i++)
        {
            world.Tick(.1);
            Check(world.WorldHours >= previous && world.WorldHours <= 30, "Jump reversed or overshot.");
            previous = world.WorldHours;
        }
        Equal(world.WorldHours, 30);
        Equal(world.Clock, 6);
        Check(world.IsSettled && weather.Steps.Count == 31, "Clock/weather frame sequence missing.");
        Check(weather.Steps[0].TargetHours == 30 && weather.Steps[^1].WorldHours == 30, "Weather did not receive final jump target.");
        world.SetClock(6);
        world.Tick(.1);
        Equal(world.WorldHours, 30);
    }

    [TestCase]
    public void EveryFlowRateAndLongResumeDeltaUseWorldSeconds()
    {
        foreach (var rate in new[] { 0, 1, 10, 60, 360, 1440 })
        {
            var world = CreateWorld(new RecordingWeatherProcess());
            world.SetFlow(rate);
            world.Tick(600);
            Near(world.WorldHours, 17.5 + rate * .1 / 3600);
            Near(world.AnimTime, 240.1);
        }
        var paused = CreateWorld(new RecordingWeatherProcess());
        paused.SetFlow(0);
        paused.Tick(.1, freezeAnimation: true);
        Equal(paused.AnimTime, 240);
        var wrapping = CreateWorld(new RecordingWeatherProcess());
        wrapping.Freeze(0, Season.Spring, 479.99);
        wrapping.SetFrameReadouts(ViewMode.Fly, 15);
        wrapping.Tick(.1);
        Near(wrapping.AnimTime, .09);
        Near(wrapping.Step12, 1 / 15d);
    }

    [TestCase]
    public void FlowToLandsBitExactlyAtAllCinematicSpeeds()
    {
        foreach (var rate in new[] { 60, 360, 1440 })
        {
            var world = CreateWorld(new RecordingWeatherProcess());
            world.Freeze(17 + 5 / 60d, Season.Autumn, 240);
            world.FlowTo(17.5, rate);
            for (var i = 0; i < 400 && world.Flow.Rate != 0; i++) world.Tick(.1);
            Equal(world.WorldHours, 17.5);
            Equal(world.Clock, 17.5);
            Equal(world.Flow.Rate, 0);
            Check(world.Transition == WorldTransition.None, "FlowTo did not clear its time transition.");
        }
    }

    [TestCase]
    public void SetFlowAndSecondFlowToCancelThePreviousStop()
    {
        var world = CreateWorld(new RecordingWeatherProcess());
        world.FlowTo(17.51, 1440);
        world.SetFlow(1440);
        world.Tick(.1);
        Check(world.Clock > 17.51 && world.Flow.Rate == 1440, "SetFlow left the previous stop active.");
        world.FlowTo(17.56, 1440);
        world.FlowTo(17.6, 1440);
        world.Tick(.1);
        world.Tick(.1);
        Equal(world.Clock, 17.6);
        Equal(world.Flow.Rate, 0);
        world.FlowTo(world.Clock, 1);
        Equal(world.Flow.Rate, 0);
    }

    [TestCase]
    public void FlowToKeepsItsClockDestinationWhenTheSeasonChanges()
    {
        var world = CreateWorld(new RecordingWeatherProcess());
        var instance = world.Instance;
        world.FlowTo(18, 60);
        world.SetSeason(Season.Summer);
        var endpoint = world.Capture(default);
        Near(ArtEphemeris.Wrap(endpoint.WorldHours + endpoint.ClockOffset, 24), 18);
        var previous = world.WorldHours;
        for (var frame = 0; frame < 15000 && world.Flow.Rate != 0; frame++)
        {
            world.Tick(.1);
            Check(world.WorldHours >= previous, "Season rebasing reversed the FlowTo timeline.");
            previous = world.WorldHours;
        }
        Equal(world.Flow.Rate, 0);
        Near(world.Clock, 18);
        Equal(world.SeasonTarget, Season.Summer);
        Equal(world.Instance, instance);
        Check(world.WorldHours > 24 && world.WorldHours <= 17.5 + 24,
            "Summer's later solar clock requires the next forward appearance of 18:00.");
    }

    [TestCase]
    public void DragUsesDampedMonotoneMotionAcrossMidnightAndRejectsReverse()
    {
        var world = CreateWorld(new RecordingWeatherProcess());
        world.Freeze(23.9, Season.Autumn, 240);
        world.SetClock(.1, ClockSetMode.Drag);
        Near(world.TargetHours, 24.1);
        world.SetClock(23.9, ClockSetMode.Drag);
        Near(world.TargetHours, 24.1);
        var previous = world.WorldHours;
        for (var i = 0; i < 100; i++)
        {
            world.Tick(i % 3 == 0 ? .1 : .016);
            Check(world.WorldHours >= previous && world.WorldHours <= 24.1 + 1e-10, "Spring reversed or overshot.");
            previous = world.WorldHours;
        }
        Near(world.Clock, .1);
        Check(!world.Scrubbing, "Drag hold was never released.");
    }

    [TestCase]
    public void SeasonPreservesSolarArcAndTimelineAndInterruptsFromCurrentValue()
    {
        var weather = new RecordingWeatherProcess();
        var world = CreateWorld(weather);
        world.SetFlow(0);
        var instance = world.Instance;
        var phase = ArtEphemeris.Phase(world.Clock, world.SeasonPos);
        world.SetSeason(Season.Spring);
        Check(world.SeasonTo == 4, "Season shader target must be unfolded forward.");
        for (var i = 0; i < 20; i++)
        {
            world.Tick(.1);
            Near(ArtEphemeris.Phase(world.Clock, world.SeasonPos), phase);
            Equal(world.WorldHours, 17.5);
        }
        var before = world.SeasonPos;
        world.SetSeason(Season.Summer);
        Near(world.SeasonFrom, before);
        for (var i = 0; i < 60; i++) world.Tick(.1);
        Equal(world.SeasonPos, 1);
        Near(ArtEphemeris.Phase(world.Clock, world.SeasonPos), phase);
        Equal(world.WorldHours, 17.5);
        Equal(world.Instance, instance);
        Equal(weather.Reseasons, 2);
    }

    [TestCase]
    public void EveryForwardSeasonDistanceUsesItsSpecifiedDuration()
    {
        foreach (var start in Enum.GetValues<Season>())
            foreach (var distance in new[] { 1, 2, 3 })
                foreach (var reduced in new[] { false, true })
                {
                    foreach (var dt in new[] { .0625, .1 })
                    {
                        var world = CreateWorld(new RecordingWeatherProcess(), reduced ? new TestMotionSettings(true) : null);
                        world.Freeze(17.5, start, 240);
                        var target = (Season)(((int)start + distance) % 4);
                        world.SetSeason(target);
                        Equal(world.SeasonTo, (double)(int)start + distance);
                        var duration = reduced ? .5 : distance == 1 ? 4 : 5.5;
                        var frames = (int)Math.Round(duration / dt);
                        // Cover exact binary intervals and decimal accumulation rounding.
                        for (var frame = 0; frame < frames - 1; frame++) world.Tick(dt);
                        Check((world.Transition & WorldTransition.Season) != 0,
                            $"Season transition finished early: {start}, +{distance}, reduced={reduced}, dt={dt}.");
                        world.Tick(dt);
                        Check((world.Transition & WorldTransition.Season) == 0,
                            $"Season transition exceeded its duration: {start}, +{distance}, reduced={reduced}, dt={dt}.");
                        Equal(world.SeasonPos, (double)(int)target);
                        Equal(world.WorldHours, 17.5);
                    }
                }
    }
}
