using System.Text.Json;
using Campus.Contracts.Save;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class Core5SaveContractTests
{
    [TestCase]
    public void CaptureContextKeepsLegacyShapeAndCarriesOneImmutableMoment()
    {
        var map = new MapId("core5-test");
        var legacy = new CaptureContext(SaveReason.Settled, map, true);
        var (reason, capturedMap, forced) = legacy;
        Check(reason == SaveReason.Settled && capturedMap == map && forced && legacy.Moment is null,
            "The existing three-argument capture context changed.");
        var moment = new WorldSaveMoment(31.5, 1.25, Season.Summer, 0);
        var current = legacy with { Moment = moment };
        Check(current.Moment == moment && legacy.Moment is null, "The moment must belong only to the new context value.");
        var json = JsonSerializer.SerializeToUtf8Bytes(moment, ContractJson.Options);
        Check(JsonSerializer.Deserialize<WorldSaveMoment>(json, ContractJson.Options) == moment,
            "The pure contract moment lost a field in serialization.");
    }

    [TestCase]
    public void LegacyProvidersExplicitlyRejectUnsupportedSnapshotAndFaultOperations()
    {
        IWorldRestore world = new LegacyWorldRestore();
        Check(!world.RestoreFaulted, "A legacy provider is not faulted by default.");
        Throws<NotSupportedException>(() => world.GetSaveMoment());
        Throws<NotSupportedException>(world.AbortRestore);

        var implementation = new LegacyWeather();
        IWeatherProcess weather = implementation;
        var moment = new WorldSaveMoment(24, 0, Season.Autumn, 1);
        Throws<NotSupportedException>(() => weather.CaptureAt(new MapId("core5-test"), moment));
        Check(implementation.Captures == 0 && implementation.Steps == 0,
            "The default CaptureAt must not silently capture the old moment or advance weather.");
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
        public void Reset(WeatherBoot boot) { }
        public void Step(in WeatherTick tick) => Steps++;
        public bool IsNative(WeatherKind kind, Season season) => true;
        public void Select(WeatherSel selection) { }
        public void Reseason(double hours, Season season, double offset) { }
        public void Quiet(double hours, double until) { }
        public void Pin(double hours, WeatherKind kind, float? intensity, double from, double to) { }
        public void Enter(MapId map, string profile, double hours) { }
        public void Leave(MapId map, double hours) { }
        public WeatherProcessState Capture(MapId map)
        {
            Captures++;
            return new(map, 0, 0, Selection, [], 0, null, null, null, null, 0, 0, 0, 0);
        }
        public void Restore(MapId map, WeatherProcessState state) { }
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
