using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.World;
using GdUnit4;
using static Campus.Tests.World.WorldTestKit;

namespace Campus.Tests.World;

/// <summary>Restore, save moments and the strict JSON form of the world section.</summary>
[TestSuite]
public sealed class WorldRestoreTests
{
    [TestCase]
    public void RestoreCommitsOneInstanceAndDropsAllTransitionHistory()
    {
        var weather = new RecordingWeatherProcess();
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
        var weather = new RecordingWeatherProcess();
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
        var weather = new RecordingWeatherProcess();
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
            var weather = new RecordingWeatherProcess();
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
        var world = CreateWorld(new RecordingWeatherProcess());
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
        var a = CreateWorld(new RecordingWeatherProcess());
        a.SetFlow(60);
        for (var i = 0; i < 100; i++) a.Tick(.017);
        var dto = a.Capture(default);
        var json = JsonSerializer.Serialize(dto, a.TypeInfo);
        var parsed = JsonSerializer.Deserialize(json, a.TypeInfo)!;
        Equal(parsed, dto);
        var b = CreateWorld(new RecordingWeatherProcess());
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
        var world = CreateWorld(new RecordingWeatherProcess());
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
}
