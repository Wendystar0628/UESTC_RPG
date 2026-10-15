using System.Text.Json;
using Campus.Contracts;
using Campus.Kernel.Stubs;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class Core5WorldRestoreTests
{
    [TestCase]
    public void FaultLatchRejectsControlsUntilFreshRestoreAndCommit()
    {
        var weather = new WeatherProcessStub();
        var world = new WorldStateStub(weather, new TestRuleset());
        var instance = world.Instance;
        var initial = world.GetSaveMoment();
        world.AbortRestore();
        Check(world.RestoreFaulted && world.Instance == instance, "Abort committed an instance or failed to latch.");
        var faultRevision = world.Rev;
        world.AbortRestore();
        Check(world.Rev == faultRevision, "Repeated AbortRestore must be idempotent.");
        Action[] controls =
        [
            () => world.SetClock(18), () => world.Advance(1), () => world.FlowTo(18, 60),
            () => world.SetSeason(Season.Spring), () => world.SetFlow(0),
            () => world.SetWeather(WeatherKind.Clear), () => world.SetWeatherIntensity(.5f),
            world.SetWeatherAuto, () => world.Apply(new WorldPreset(18, Season.Autumn)),
            world.ResetDefault, () => world.Freeze(1, Season.Spring, 0),
            () => world.SetViewMode(ViewMode.Walk),
            () => world.SetMode(ViewMode.Walk, ModeSwitchVia.Ui).GetAwaiter().GetResult(),
        ];
        foreach (var control in controls) Throws<InvalidOperationException>(control);
        Throws<InvalidOperationException>(() => world.GetSaveMoment());
        Throws<InvalidOperationException>(world.CompleteRestore);
        Check(world.WorldHours == initial.WorldHours && world.Flow.Rate == initial.FlowRate &&
            world.SeasonTarget == initial.Season && world.Instance == instance, "Rejected controls mutated the frozen world.");

        var recovered = new WorldSaveMoment(9, .25, Season.Winter, 10);
        world.Restore(recovered);
        Check(world.RestoreFaulted && world.Instance == instance, "Restore alone cleared the fault or changed instance.");
        Throws<InvalidOperationException>(() => world.GetSaveMoment());
        world.AbortRestore();
        Throws<InvalidOperationException>(world.CompleteRestore);
        world.Restore(recovered);
        world.CompleteRestore();
        Check(!world.RestoreFaulted && world.Instance == instance + 1 && world.GetSaveMoment() == recovered,
            "Only a fresh successful restore/commit may recover the world.");
        Check(world.Clock == 9.25, "The restored clock offset was lost.");
        Throws<InvalidOperationException>(world.CompleteRestore);
        world.AbortRestore();
        world.BeginNewInstance();
        Check(!world.RestoreFaulted && world.Instance == instance + 2 &&
            world.GetSaveMoment() == new WorldSaveMoment(17.5, 0, Season.Autumn, 1),
            "A new game did not recover through a fresh restore/commit.");
    }

    [TestCase]
    public void CaptureAtUsesTheSuppliedMomentWithoutChangingLiveWeatherOrFrozenMaps()
    {
        var weather = new WeatherProcessStub();
        var active = new MapId("core5-active");
        var inactive = new MapId("core5-inactive");
        weather.Reset(new WeatherBoot(42));
        weather.Enter(inactive, "test", 4);
        weather.Select(new WeatherSel(WeatherMode.Manual, WeatherKind.Fog, .75f));
        weather.Leave(inactive, 4);
        weather.Enter(active, "test", 17.5);
        weather.Select(new WeatherSel(WeatherMode.Manual, WeatherKind.Rain, .65f));
        var before = JsonSerializer.Serialize(weather.Capture(active), ContractJson.Options);
        var frozen = JsonSerializer.Serialize(weather.Capture(inactive), ContractJson.Options);
        var live = (weather.Selection, weather.Now, weather.Ahead, weather.Seed, weather.Transitioning);
        var moment = new WorldSaveMoment(31.5, .25, Season.Autumn, 0);
        var snapshot = weather.CaptureAt(active, moment);
        Check(JsonSerializer.Serialize(weather.CaptureAt(inactive, moment), ContractJson.Options) == frozen,
            "CaptureAt advanced an inactive map's frozen endpoint.");
        Check(snapshot.LeftAtHours == moment.WorldHours && snapshot.ClockOffset == moment.ClockOffset &&
            snapshot.Season == moment.Season && snapshot.Selection == live.Selection,
            "CaptureAt ignored the requested endpoint or changed the settled selection.");
        Check(JsonSerializer.Serialize(weather.Capture(active), ContractJson.Options) == before &&
            JsonSerializer.Serialize(weather.Capture(inactive), ContractJson.Options) == frozen &&
            (weather.Selection, weather.Now, weather.Ahead, weather.Seed, weather.Transitioning) == live,
            "Snapshot evaluation mutated the live weather or an inactive map.");
    }

    private sealed class TestRuleset : IRuleset
    {
        public RulesetDescriptor Current { get; } = new(new("core5-test"), [ViewMode.Fly, ViewMode.Walk],
            WorldControlMask.All, "all-unlocked", [], []);
        public long Rev => 1;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
