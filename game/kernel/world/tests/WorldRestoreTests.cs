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

/// <summary>Restore, save moments and the strict JSON form of the world section.</summary>
[TestSuite]
public sealed class WorldRestoreTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RestoreCommitsOneInstanceAndDropsAllTransitionHistory()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather, initialize: false);
        world.Freeze(17.5, Season.Autumn, 240);
        world.Advance(24);
        world.SetSeason(Season.Winter);
        world.Tick(.1);
        var instance = world.Instance;
        var notifications = new List<long>();
        WorldSaveV1? notifiedWorld = null;
        world.InstanceChanged += value => { notifications.Add(value); notifiedWorld = world.Capture(default); };
        var dto = new WorldSaveV1(7, .2, 0, Season.Spring, 10, 10, 10.25);
        world.Restore(dto, Restore());
        AssertThat(world.Instance).IsEqual(instance);
        AssertThat(notifications.Count).IsEqual(0);
        AssertThat(world.WorldHours).IsEqual(7);
        AssertThat(!world.IsSettled && world.Omega == 0 && world.Transition == WorldTransition.None).OverrideFailureMessage("Restore must clear transition history but remain unsettled until commit.").IsTrue();
        AssertThrown(() => world.Tick(.1)).IsInstanceOf<InvalidOperationException>();
        world.CompleteRestore();
        AssertThat(world.Instance).IsEqual(instance + 1);
        AssertThat(notifications.Count).IsEqual(1);
        AssertThat(notifications[0]).IsEqual(instance + 1);
        AssertThat(notifiedWorld).IsEqual(dto);
        AssertThat(world.Capture(default)).IsEqual(dto);
        AssertThrown(world.CompleteRestore).IsInstanceOf<InvalidOperationException>();
        AssertThat(notifications.Count).IsEqual(1);
        AssertThrown(() => world.Freeze(1, Season.Spring, 0)).IsInstanceOf<InvalidOperationException>();
        AssertThat(weather.Resets).IsEqual(0);
        AssertThat(weather.Quiets).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SaveMomentUsesTheSameFreshEndpointAsWorldCaptureWithoutStepping()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather);
        world.Tick(.0625);
        world.Advance(1); // A HUD action after this frame's world/weather step.
        world.SetSeason(Season.Winter);
        var before = (world.WorldHours, world.Clock, world.SeasonPos, world.AnimTime,
            world.Rev, world.Instance, weather.Steps.Count);
        var moment = world.GetSaveMoment();
        var captured = world.Capture(default);
        AssertThat(moment.WorldHours).IsEqual(captured.WorldHours);
        AssertThat(moment.ClockOffset).IsEqual(captured.ClockOffset);
        AssertThat(moment.Season).IsEqual(captured.SeasonTarget);
        AssertThat(moment.FlowRate).IsEqual(captured.FlowRate);
        AssertThat(moment.WorldHours > world.WorldHours && moment.Season == Season.Winter).OverrideFailureMessage("Save moment used the last frame instead of the new settled endpoint.").IsTrue();
        AssertThat((world.WorldHours, world.Clock, world.SeasonPos, world.AnimTime,
            world.Rev, world.Instance, weather.Steps.Count)).IsEqual(before);
        var context = new CaptureContext(SaveReason.Quit, new("test-world"), true) { Moment = moment };
        using var flow = world.AcquireFlow(FlowPriority.Cinematic, 0);
        flow.FlowTo(18, 60);
        var shared = world.Capture(in context);
        AssertThat(shared.WorldHours).IsEqual(moment.WorldHours);
        AssertThat(shared.ClockOffset).IsEqual(moment.ClockOffset);
        AssertThat(shared.SeasonTarget).IsEqual(moment.Season);
        AssertThat(shared.FlowRate).IsEqual(moment.FlowRate);
        moment = world.GetSaveMoment();
        AssertThat(moment.FlowRate).IsEqual(world.BaseFlow.Rate);
        AssertThat(Math.Abs((ArtEphemeris.Wrap(moment.WorldHours + moment.ClockOffset, 24)) - (18)) <= TestTolerance.WorldHours).OverrideFailureMessage($"Expected {18:R}, got {ArtEphemeris.Wrap(moment.WorldHours + moment.ClockOffset, 24):R}.").IsTrue();
        AssertThat(weather.Steps.Count).IsEqual(before.Count);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FailedRestoreFreezesUntilAFreshSuccessfulRestoreCommits()
    {
        var weather = new TestWeatherProcess();
        var world = CreateWorld(weather);
        var instance = world.Instance;
        var notifications = new List<long>();
        world.InstanceChanged += notifications.Add;
        var failed = new WorldSaveV1(7, .2, 0, Season.Spring, 10, 10, 10.25);
        world.Restore(failed, Restore());
        AssertThrown(() => world.Tick(.1)).IsInstanceOf<InvalidOperationException>();
        world.AbortRestore();
        AssertThat(world.RestoreFaulted && !world.IsSettled).OverrideFailureMessage("Restore failure must be explicit and block a settled save.").IsTrue();
        var frozen = LiveReadings(world, weather);
        for (var frame = 0; frame < 5; frame++) world.Tick(.1);
        AssertThat(LiveReadings(world, weather)).IsEqual(frozen);
        AssertThrown(() => world.Capture(default)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => world.GetSaveMoment()).IsInstanceOf<InvalidOperationException>();
        AssertThrown(world.CompleteRestore).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => world.Restore(failed with { WorldHours = double.NaN }, Restore())).IsInstanceOf<JsonException>();
        AssertThat(world.RestoreFaulted).OverrideFailureMessage("An invalid retry must not clear the fault.").IsTrue();
        var retry = new WorldSaveV1(11, .1, 1, Season.Summer, 60, 60, 120);
        world.Restore(retry, Restore());
        var prepared = LiveReadings(world, weather);
        world.Tick(.1);
        AssertThat(LiveReadings(world, weather)).IsEqual(prepared);
        AssertThat(world.Instance).IsEqual(instance);
        AssertThat(notifications.Count).IsEqual(0);
        AssertThat(world.RestoreFaulted).OverrideFailureMessage("Preparing a retry must not resume a partially restored world.").IsTrue();
        world.CompleteRestore();
        AssertThat(!world.RestoreFaulted && world.IsSettled).OverrideFailureMessage("Successful commit did not release the fault freeze.").IsTrue();
        AssertThat(world.Instance).IsEqual(instance + 1);
        AssertThat(notifications.Count).IsEqual(1);
        AssertThat(notifications[0]).IsEqual(instance + 1);
        AssertThat(world.Capture(default)).IsEqual(retry);
        world.Tick(.1);
        AssertThat(world.WorldHours > retry.WorldHours && world.AnimTime > retry.AnimTime).OverrideFailureMessage("Successful recovery remained frozen.").IsTrue();
        AssertThat(weather.Steps.Count).IsEqual(frozen.Steps + 1);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void AbortBeforeOrAfterWorldCommitRejectsAllControlsAndSaves()
    {
        foreach (var afterCommit in new[] { false, true })
        {
            var weather = new TestWeatherProcess();
            var world = CreateWorld(weather);
            if (afterCommit)
            {
                world.Restore(new WorldSaveV1(7, .2, 0, Season.Spring, 10, 10, 10.25), Restore());
                world.CompleteRestore();
            }
            using var flow = world.AcquireFlow(FlowPriority.Cinematic, world.Flow.Rate);
            var moment = world.GetSaveMoment();
            var instance = world.Instance;
            world.AbortRestore();
            world.AbortRestore();
            AssertThat(world.RestoreFaulted).OverrideFailureMessage("Abort must fault before Restore as well as after World commit.").IsTrue();
            AssertThat(world.Instance).IsEqual(instance);
            var frozen = LiveReadings(world, weather);
            var calls = weather.Calls.Count;
            Action[] controls =
            [
                () => world.SetClock(8),
                () => world.SetClock(8, ClockSetMode.Drag),
                () => world.Advance(1),
                () => flow.FlowTo(18, 60),
                () => flow.SetRate(60),
                () => world.AcquireFlow(FlowPriority.Pause, 0),
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
            foreach (var control in controls) AssertThrown(control).IsInstanceOf<InvalidOperationException>();
            AssertThrown(world.CompleteRestore).IsInstanceOf<InvalidOperationException>();
            world.Tick(.1);
            AssertThat(LiveReadings(world, weather)).IsEqual(frozen);
            AssertThat(weather.Calls.Count).IsEqual(calls);
            var fresh = world.CreateDefault(new NewGameContext(5, new("test-world"), new WorldPreset(17.5, Season.Autumn), ViewMode.Walk));
            world.Restore(fresh, Restore());
            AssertThat(world.RestoreFaulted).OverrideFailureMessage("CreateDefault or Restore must not clear a failed transaction by itself.").IsTrue();
            world.CompleteRestore();
            AssertThat(!world.RestoreFaulted).OverrideFailureMessage("A complete fresh new game must remain available from the fault screen.").IsTrue();
            flow.Dispose();
            world.SetFlow(1);
            world.Tick(.1);
            AssertThat(world.WorldHours > fresh.WorldHours).OverrideFailureMessage("Controls did not resume after successful new game recovery.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WorldSaveUsesIndependentContractOptionsAfterSharedOptionsAreLocked()
    {
        ContractJson.Options.MakeReadOnly(populateMissingResolver: true);
        var world = CreateWorld(new TestWeatherProcess());
        var typeInfo = world.TypeInfo;
        AssertThat(ContractJson.Options.IsReadOnly).OverrideFailureMessage("Shared contract options must be locked for this regression.").IsTrue();
        AssertThat(!ReferenceEquals(typeInfo.Options, ContractJson.Options)).OverrideFailureMessage("World source generation must own an independent copy of the shared options.").IsTrue();
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
            AssertThat(document.RootElement.GetProperty("seasonTarget").GetString()).IsEqual(text);
            AssertThat(document.RootElement.GetProperty("worldHours").GetDouble()).IsEqual(dto.WorldHours);
            AssertThat(JsonSerializer.Deserialize(json, typeInfo)).IsEqual(dto);
            // Previously written PascalCase strings remain readable; new writes use the canonical casing.
            var legacy = json.Replace($"\"{text}\"", $"\"{season}\"", StringComparison.Ordinal);
            AssertThat(JsonSerializer.Deserialize(legacy, typeInfo)).IsEqual(dto);
        }
        var autumn = JsonSerializer.Serialize(snapshot, typeInfo);
        AssertThrown(() => JsonSerializer.Deserialize(
            autumn.Replace("\"autumn\"", "2", StringComparison.Ordinal), typeInfo)).IsInstanceOf<JsonException>();
    }

    [TestCase]
    [TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void JsonRoundTripIsStrictAndContinuesBitExactly()
    {
        var a = CreateWorld(new TestWeatherProcess());
        a.SetFlow(60);
        for (var i = 0; i < 100; i++) a.Tick(.017);
        var dto = a.Capture(default);
        var json = JsonSerializer.Serialize(dto, a.TypeInfo);
        var parsed = JsonSerializer.Deserialize(json, a.TypeInfo)!;
        AssertThat(parsed).IsEqual(dto);
        var b = CreateWorld(new TestWeatherProcess());
        b.Restore(parsed, Restore()); b.CompleteRestore();
        for (var i = 0; i < 150; i++)
        {
            var dt = i % 2 == 0 ? .013 : .027;
            a.Tick(dt); b.Tick(dt);
            AssertThat(a.Capture(default)).IsEqual(b.Capture(default));
            AssertThat(a.Sun).IsEqual(b.Sun);
        }
        AssertThrown(() => JsonSerializer.Deserialize(json[..^1] + ",\"unexpected\":1}", a.TypeInfo)).IsInstanceOf<JsonException>();
        AssertThrown(() => JsonSerializer.Deserialize("{}", a.TypeInfo)).IsInstanceOf<JsonException>();
        AssertThrown(() => JsonSerializer.Deserialize(json.Replace("\"autumn\"", "\"unknown\"", StringComparison.Ordinal), a.TypeInfo)).IsInstanceOf<JsonException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void InvalidControlsAndInvalidCoreSaveAreRejectedWithoutMutation()
    {
        var world = CreateWorld(new TestWeatherProcess());
        var initial = world.Capture(default);
        using var flow = world.AcquireFlow(FlowPriority.Cinematic, world.Flow.Rate);
        AssertThrown(() => world.Advance(0)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => world.Advance(double.NaN)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => world.SetFlow(2)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => flow.FlowTo(24, 60)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => world.SetClock(-1)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => world.SetSeason((Season)9)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => world.Restore(initial with { WorldHours = double.NaN }, Restore())).IsInstanceOf<JsonException>();
        AssertThrown(() => world.Restore(initial with { SeasonPos = 2.1 }, Restore())).IsInstanceOf<JsonException>();
        AssertThat(world.Capture(default)).IsEqual(initial);
    }
}
