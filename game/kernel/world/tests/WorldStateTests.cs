using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.World;
using GdUnit4;
using WorldFlow = Campus.Contracts.Flow;

namespace Campus.Tests.World;

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
        var weather = new RecordingWeather();
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
        var weather = new RecordingWeather();
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
            var world = CreateWorld(new RecordingWeather());
            world.SetFlow(rate);
            world.Tick(600);
            Near(world.WorldHours, 17.5 + rate * .1 / 3600);
            Near(world.AnimTime, 240.1);
        }
        var paused = CreateWorld(new RecordingWeather());
        paused.SetFlow(0);
        paused.Tick(.1, freezeAnimation: true);
        Equal(paused.AnimTime, 240);
        var wrapping = CreateWorld(new RecordingWeather());
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
            var world = CreateWorld(new RecordingWeather());
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
        var world = CreateWorld(new RecordingWeather());
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
        var world = CreateWorld(new RecordingWeather());
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
        var world = CreateWorld(new RecordingWeather());
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
        var weather = new RecordingWeather();
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
                        var world = CreateWorld(new RecordingWeather(), reduced ? new ReducedMotion() : null);
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

    [TestCase]
    public void WeatherIsNotSteppedBeforeTheFirstMapEnters()
    {
        var weather = new RecordingWeather();
        var world = new WorldStateService(weather);
        world.Tick(.0625);
        Equal(weather.Steps.Count, 0);
        world.OnMapLoaded(Context(new WorldPreset(17.5, Season.Autumn), true));
        world.Tick(.0625);
        Equal(weather.Steps.Count, 1);
        Equal(weather.Enters, 1);
    }

    [TestCase]
    public void WinterEarlyClockInitializesWithoutWeatherTransitions()
    {
        var weather = new RecordingWeather();
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
        var context = Context(new WorldPreset(6, Season.Winter, WeatherKind.Snow), false);
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
        var weather = new RecordingWeather();
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
        var world = CreateWorld(new RecordingWeather(), new ReducedMotion());
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
        var weather = new RecordingWeather();
        var world = CreateWorld(weather);
        var context = Context(new WorldPreset(17.5, Season.Autumn, WeatherKind.Clear), true);
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
        Equal(weather.Leaves, 1);
        Equal(weather.Enters, 2);
    }

    [TestCase]
    public void RestoreCommitsOneInstanceAndDropsAllTransitionHistory()
    {
        var weather = new RecordingWeather();
        var world = CreateWorld(weather);
        world.Freeze(17.5, Season.Autumn, 240);
        world.Advance(24);
        world.SetSeason(Season.Winter);
        world.Tick(.1);
        var instance = world.Instance;
        var dto = new WorldSaveV1(7, .2, 0, Season.Spring, 10, 10, 10.25);
        world.Restore(dto, Restore());
        Equal(world.Instance, instance);
        Equal(world.WorldHours, 7);
        Check(!world.IsSettled && world.Omega == 0 && world.Transition == WorldTransition.None,
            "Restore must clear transition history but remain unsettled until commit.");
        Throws<InvalidOperationException>(() => world.Tick(.1));
        world.CompleteRestore();
        Equal(world.Instance, instance + 1);
        Equal(world.Capture(default), dto);
        Throws<InvalidOperationException>(world.CompleteRestore);
        Throws<InvalidOperationException>(() => world.Freeze(1, Season.Spring, 0));
        Equal(weather.Resets, 0);
        Equal(weather.Quiets, 0);
    }

    [TestCase]
    public void SaveMomentUsesTheSameFreshEndpointAsWorldCaptureWithoutStepping()
    {
        var weather = new RecordingWeather();
        var world = CreateWorld(weather);
        world.Tick(.0625);
        world.Advance(1); // A HUD action after this frame's world/weather step.
        world.SetSeason(Season.Winter);
        var before = (world.WorldHours, world.Clock, world.SeasonPos, world.AnimTime,
            world.Rev, world.Instance, weather.Steps.Count);
        var moment = world.GetSaveMoment();
        var captured = world.Capture(default);
        Equal(moment.WorldHours, captured.WorldHours);
        Equal(moment.ClockOffset, captured.ClockOffset);
        Equal(moment.Season, captured.SeasonTarget);
        Equal(moment.FlowRate, captured.FlowRate);
        Check(moment.WorldHours > world.WorldHours && moment.Season == Season.Winter,
            "Save moment used the last frame instead of the new settled endpoint.");
        Equal((world.WorldHours, world.Clock, world.SeasonPos, world.AnimTime,
            world.Rev, world.Instance, weather.Steps.Count), before);
        var context = new CaptureContext(SaveReason.Quit, new("test-world"), true) { Moment = moment };
        world.FlowTo(18, 60);
        var shared = world.Capture(in context);
        Equal(shared.WorldHours, moment.WorldHours);
        Equal(shared.ClockOffset, moment.ClockOffset);
        Equal(shared.SeasonTarget, moment.Season);
        Equal(shared.FlowRate, moment.FlowRate);
        moment = world.GetSaveMoment();
        Equal(moment.FlowRate, 0);
        Near(ArtEphemeris.Wrap(moment.WorldHours + moment.ClockOffset, 24), 18);
        Equal(weather.Steps.Count, before.Count);
    }

    [TestCase]
    public void FailedRestoreFreezesUntilAFreshSuccessfulRestoreCommits()
    {
        var weather = new RecordingWeather();
        var world = CreateWorld(weather);
        var instance = world.Instance;
        var failed = new WorldSaveV1(7, .2, 0, Season.Spring, 10, 10, 10.25);
        world.Restore(failed, Restore());
        Throws<InvalidOperationException>(() => world.Tick(.1));
        world.AbortRestore();
        Check(world.RestoreFaulted && !world.IsSettled, "Restore failure must be explicit and block a settled save.");
        var frozen = LiveReadings(world, weather);
        for (var frame = 0; frame < 5; frame++) world.Tick(.1);
        Equal(LiveReadings(world, weather), frozen);
        Throws<InvalidOperationException>(() => world.Capture(default));
        Throws<InvalidOperationException>(() => world.GetSaveMoment());
        Throws<InvalidOperationException>(world.CompleteRestore);
        Throws<JsonException>(() => world.Restore(failed with { WorldHours = double.NaN }, Restore()));
        Check(world.RestoreFaulted, "An invalid retry must not clear the fault.");
        var retry = new WorldSaveV1(11, .1, 1, Season.Summer, 60, 60, 120);
        world.Restore(retry, Restore());
        var prepared = LiveReadings(world, weather);
        world.Tick(.1);
        Equal(LiveReadings(world, weather), prepared);
        Equal(world.Instance, instance);
        Check(world.RestoreFaulted, "Preparing a retry must not resume a partially restored world.");
        world.CompleteRestore();
        Check(!world.RestoreFaulted && world.IsSettled, "Successful commit did not release the fault freeze.");
        Equal(world.Instance, instance + 1);
        Equal(world.Capture(default), retry);
        world.Tick(.1);
        Check(world.WorldHours > retry.WorldHours && world.AnimTime > retry.AnimTime,
            "Successful recovery remained frozen.");
        Equal(weather.Steps.Count, frozen.Steps + 1);
    }

    [TestCase]
    public void AbortBeforeOrAfterWorldCommitRejectsAllControlsAndSaves()
    {
        foreach (var afterCommit in new[] { false, true })
        {
            var weather = new RecordingWeather();
            var world = CreateWorld(weather);
            if (afterCommit)
            {
                world.Restore(new WorldSaveV1(7, .2, 0, Season.Spring, 10, 10, 10.25), Restore());
                world.CompleteRestore();
            }
            var moment = world.GetSaveMoment();
            var instance = world.Instance;
            world.AbortRestore();
            world.AbortRestore();
            Check(world.RestoreFaulted, "Abort must fault before Restore as well as after World commit.");
            Equal(world.Instance, instance);
            var frozen = LiveReadings(world, weather);
            var calls = weather.Calls.Count;
            Action[] controls =
            [
                () => world.SetClock(8),
                () => world.SetClock(8, ClockSetMode.Drag),
                () => world.Advance(1),
                () => world.FlowTo(18, 60),
                () => world.SetFlow(0),
                () => world.SetSeason(Season.Winter),
                () => world.SetWeather(WeatherKind.Rain),
                () => world.SetWeatherIntensity(.8f),
                world.SetWeatherAuto,
                () => world.Apply(new WorldPreset(12, Season.Spring)),
                world.ResetDefault,
                () => world.Freeze(12, Season.Spring, 0),
                () => world.GetSaveMoment(),
                () => world.Capture(default),
                () => world.Capture(new CaptureContext(SaveReason.Quit, new("test-world"), true) { Moment = moment }),
            ];
            foreach (var control in controls) Throws<InvalidOperationException>(control);
            Throws<InvalidOperationException>(world.CompleteRestore);
            world.Tick(.1);
            Equal(LiveReadings(world, weather), frozen);
            Equal(weather.Calls.Count, calls);
            var fresh = world.CreateDefault(new NewGameContext(5, new("test-world"), new WorldPreset(17.5, Season.Autumn)));
            world.Restore(fresh, Restore());
            Check(world.RestoreFaulted, "CreateDefault or Restore must not clear a failed transaction by itself.");
            world.CompleteRestore();
            Check(!world.RestoreFaulted, "A complete fresh new game must remain available from the fault screen.");
            world.SetFlow(1);
            world.Tick(.1);
            Check(world.WorldHours > fresh.WorldHours, "Controls did not resume after successful new game recovery.");
        }
    }

    [TestCase]
    public void WorldSaveUsesIndependentContractOptionsAfterSharedOptionsAreLocked()
    {
        ContractJson.Options.MakeReadOnly(populateMissingResolver: true);
        var world = CreateWorld(new RecordingWeather());
        var typeInfo = world.TypeInfo;
        Check(ContractJson.Options.IsReadOnly, "Shared contract options must be locked for this regression.");
        Check(!ReferenceEquals(typeInfo.Options, ContractJson.Options),
            "World source generation must own an independent copy of the shared options.");
        var snapshot = world.Capture(default);
        foreach (var (season, text) in new[]
        {
            (Season.Spring, "spring"), (Season.Summer, "summer"),
            (Season.Autumn, "autumn"), (Season.Winter, "winter")
        })
        {
            var dto = snapshot with { SeasonTarget = season, SeasonPos = (int)season };
            var json = JsonSerializer.Serialize(dto, typeInfo);
            using var document = JsonDocument.Parse(json);
            Equal(document.RootElement.GetProperty("seasonTarget").GetString(), text);
            Equal(document.RootElement.GetProperty("worldHours").GetDouble(), dto.WorldHours);
            Equal(JsonSerializer.Deserialize(json, typeInfo), dto);
            // Previously written PascalCase strings remain readable; new writes use the canonical casing.
            var legacy = json.Replace($"\"{text}\"", $"\"{season}\"", StringComparison.Ordinal);
            Equal(JsonSerializer.Deserialize(legacy, typeInfo), dto);
        }
        var autumn = JsonSerializer.Serialize(snapshot, typeInfo);
        Throws<JsonException>(() => JsonSerializer.Deserialize(
            autumn.Replace("\"autumn\"", "2", StringComparison.Ordinal), typeInfo));
    }

    [TestCase]
    public void JsonRoundTripIsStrictAndContinuesBitExactly()
    {
        var a = CreateWorld(new RecordingWeather());
        a.SetFlow(60);
        for (var i = 0; i < 100; i++) a.Tick(.017);
        var dto = a.Capture(default);
        var json = JsonSerializer.Serialize(dto, a.TypeInfo);
        var parsed = JsonSerializer.Deserialize(json, a.TypeInfo)!;
        Equal(parsed, dto);
        var b = CreateWorld(new RecordingWeather());
        b.Restore(parsed, Restore()); b.CompleteRestore();
        for (var i = 0; i < 150; i++)
        {
            var dt = i % 2 == 0 ? .013 : .027;
            a.Tick(dt); b.Tick(dt);
            Equal(a.Capture(default), b.Capture(default));
            Equal(a.Sun, b.Sun);
        }
        Throws<JsonException>(() => JsonSerializer.Deserialize(json[..^1] + ",\"unexpected\":1}", a.TypeInfo));
        Throws<JsonException>(() => JsonSerializer.Deserialize("{}", a.TypeInfo));
        Throws<JsonException>(() => JsonSerializer.Deserialize(json.Replace("\"autumn\"", "\"unknown\"", StringComparison.Ordinal), a.TypeInfo));
    }

    [TestCase]
    public void InvalidControlsAndInvalidCoreSaveAreRejectedWithoutMutation()
    {
        var world = CreateWorld(new RecordingWeather());
        var initial = world.Capture(default);
        Throws<ArgumentOutOfRangeException>(() => world.Advance(0));
        Throws<ArgumentOutOfRangeException>(() => world.Advance(double.NaN));
        Throws<ArgumentOutOfRangeException>(() => world.SetFlow(2));
        Throws<ArgumentOutOfRangeException>(() => world.FlowTo(24, 60));
        Throws<ArgumentOutOfRangeException>(() => world.SetClock(-1));
        Throws<ArgumentOutOfRangeException>(() => world.SetSeason((Season)9));
        Throws<JsonException>(() => world.Restore(initial with { WorldHours = double.NaN }, Restore()));
        Throws<JsonException>(() => world.Restore(initial with { SeasonPos = 2.1 }, Restore()));
        Equal(world.Capture(default), initial);
    }

    private static RestoreContext Restore() => new() { Map = new("test-world"), FromVersion = 1, Missing = false };
    private static (double Hours, double Clock, double Target, double Season, WorldFlow Flow, double Anim,
        Body Sun, Body Moon, long Rev, long Instance, int Steps) LiveReadings(WorldStateService world, RecordingWeather weather) =>
        (world.WorldHours, world.Clock, world.TargetHours, world.SeasonPos, world.Flow, world.AnimTime,
            world.Sun, world.Moon, world.Rev, world.Instance, weather.Steps.Count);
    private static WorldStateService CreateWorld(RecordingWeather weather, IMotionSettings? motion = null)
    {
        var world = new WorldStateService(weather, motion);
        world.OnMapLoaded(Context(new WorldPreset(17.5, Season.Autumn, WeatherKind.Clear), true));
        return world;
    }
    private static IMapContext Context(WorldPreset preset, bool quiet) => new TestContext(new MapDescriptor(
        new("test-world"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
        new(0, 0, 0, Vector2.Zero, Vector2.One), Vector2.Zero, new(Vector3.Zero, Vector3.One),
        new(Vector2.Zero, Vector2.One), new(1, 1, 1, -1, 1, 1, 1, 1), new(0, 0), new(0, 0),
        "entry", [], new(MapTimeRule.Shared, preset, new("默认", "Default"), quiet, "test-profile"),
        new("sky", "backdrop", []), new Dictionary<string, string>(), []));
    private sealed record TestContext(MapDescriptor Descriptor) : IMapContext
    {
        public BuildInputSnapshot SourceInputs => BuildInputSnapshot.Empty;
        public IWorldModel World => throw new NotSupportedException();
        public string DataRoot => "res://test/";
    }
    private sealed class ReducedMotion : IMotionSettings
    {
        public bool ReduceMotion => true;
        public event Action? Changed { add { } remove { } }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Near(double actual, double expected, double tolerance = 1e-10) =>
        Check(Math.Abs(actual - expected) <= tolerance, $"Expected {expected:R}, got {actual:R}.");
    private static void Equal<T>(T actual, T expected) => Check(EqualityComparer<T>.Default.Equals(actual, expected), $"Expected {expected}, got {actual}.");
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class RecordingWeather : IWeatherProcess
    {
        public readonly List<WeatherTick> Steps = [];
        public readonly List<string> Calls = [];
        public int Reseasons, Resets, Quiets, Enters, Leaves;
        public double PinFrom, PinTo, QuietTo, LastEnterHours;
        public WeatherKind PinnedKind;
        public float? PinnedIntensity;
        public WeatherSel Selection { get; private set; } = new(WeatherMode.Auto, WeatherKind.Clear, 0);
        public WeatherVec Now => default;
        public IReadOnlyList<WeatherSpan> Ahead => [];
        public uint Seed => 17;
        public bool Transitioning => false;
        public bool IsNative(WeatherKind kind, Season season) => true;
        public void Reset(WeatherBoot boot) => Resets++;
        public void Step(in WeatherTick tick) => Steps.Add(tick);
        public void Select(WeatherSel selection) { Selection = selection; Calls.Add("select"); }
        public void Reseason(double hours, Season season, double offset) { Reseasons++; Calls.Add("reseason"); }
        public void Quiet(double hours, double until) { Quiets++; QuietTo = until; Calls.Add("quiet"); }
        public void Pin(double hours, WeatherKind kind, float? intensity, double from, double to)
        { PinnedKind = kind; PinnedIntensity = intensity; PinFrom = from; PinTo = to; Calls.Add("pin"); }
        public void Enter(MapId map, string profile, double hours) { Enters++; LastEnterHours = hours; }
        public void Leave(MapId map, double hours) => Leaves++;
        public WeatherProcessState Capture(MapId map) => throw new NotSupportedException();
        public void Restore(MapId map, WeatherProcessState state) => throw new NotSupportedException();
    }
}
