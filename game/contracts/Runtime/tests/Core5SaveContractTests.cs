using System.Text.Json;
using Campus.Contracts.Save;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class Core5SaveContractTests
{
    [TestCase]
    public void CaptureContextKeepsLegacyShapeAndCarriesOneImmutableMoment()
    {
        var map = new MapId("core5-test");
        var legacy = new CaptureContext(SaveReason.Interval, map, true);
        var (reason, capturedMap, forced) = legacy;
        AssertThat(reason == SaveReason.Interval && capturedMap == map && forced && legacy.Moment is null).OverrideFailureMessage("The existing three-argument capture context changed.").IsTrue();
        var moment = new WorldSaveMoment(31.5, 1.25, Season.Summer, 0);
        var current = legacy with { Moment = moment };
        AssertThat(current.Moment == moment && legacy.Moment is null).OverrideFailureMessage("The moment must belong only to the new context value.").IsTrue();
        var json = JsonSerializer.SerializeToUtf8Bytes(moment, ContractJson.Options);
        AssertThat(JsonSerializer.Deserialize<WorldSaveMoment>(json, ContractJson.Options) == moment).OverrideFailureMessage("The pure contract moment lost a field in serialization.").IsTrue();
    }

    [TestCase]
    public void LegacyProvidersExplicitlyRejectUnsupportedSnapshotAndFaultOperations()
    {
        IWorldRestore world = new LegacyWorldRestore();
        AssertThat(!world.RestoreFaulted).OverrideFailureMessage("A legacy provider is not faulted by default.").IsTrue();
        AssertThrown(() => world.GetSaveMoment()).IsInstanceOf<NotSupportedException>();
        AssertThrown(world.AbortRestore).IsInstanceOf<NotSupportedException>();

        var implementation = new LegacyWeather();
        IWeatherProcess weather = implementation;
        var moment = new WorldSaveMoment(24, 0, Season.Autumn, 1);
        AssertThrown(() => weather.CaptureAt(moment)).IsInstanceOf<NotSupportedException>();
        AssertThat(implementation.Captures == 0 && implementation.Steps == 0).OverrideFailureMessage("The default CaptureAt must not silently capture the old moment or advance weather.").IsTrue();
    }

    private sealed class LegacyWorldRestore : IWorldRestore
    {
        public void CompleteRestore() { }
    }

    private sealed class LegacyWeather : IWeatherProcess
    {
        public int Captures { get; private set; }
        public int Steps { get; private set; }
        public WeatherSel Selection => new(WeatherMode.Auto, WeatherKind.Clear, 1);
        public WeatherVec Now => default;
        public IReadOnlyList<WeatherSpan> Ahead => [];
        public uint Seed => 1;
        public bool Transitioning => false;
        public WeatherBoot ParseBootOptions(IReadOnlyDictionary<string, string> arguments, uint seed, Season season) => throw new NotSupportedException();
        public WeatherKind MapToSeason(WeatherKind kind, Season season) => throw new NotSupportedException();
        public void Reset(WeatherBoot boot) { }
        public void Step(in WeatherTick tick) => Steps++;
        public bool IsNative(WeatherKind kind, Season season) => true;
        public void Select(WeatherSel selection) { }
        public void Reseason(double hours, Season season, double offset) { }
        public void Quiet(double hours, double until) { }
        public void Pin(double hours, WeatherKind kind, float? intensity, double from, double to) { }
        public void Enter(MapId map, string profile, double hours) { }
        public WeatherProcessState Capture()
        {
            Captures++;
            return new(new MapId("core5-test"), Selection, null, null, null, null, 0, 0, 0, 0);
        }
        public void Restore(WeatherProcessState state) { }
    }
}
