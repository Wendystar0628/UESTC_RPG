using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Systems.Weather.Tests;

[TestSuite]
public sealed class WeatherProcessTests
{
    private static readonly MapId Map = new("weather-test-a");
    private static WeatherProfile Profile => WeatherProfile.Parse(File.ReadAllText(
        RepositoryFixture.FilePath("game", "data", "weather", "chengdu", "profile.json")));

    [TestCase]
    [TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void SameSeedMapAndTimeGiveTheSameWeatherInAnyCallOrder()
    {
        var profile = Profile;
        foreach (var season in new[] { Season.Autumn, Season.Winter })
        {
            var a = Create(127, season, profile);
            var b = Create(127, season, profile);
            for (int hour = 1; hour <= 240; hour++) b.Step(Tick(hour, season));
            a.Step(Tick(240, season));
            Equal(a, b);
        }
        var c = Create(128, Season.Autumn, profile);
        AssertThat(!Forecast(Create(127, Season.Autumn, profile), Season.Autumn, 30).SequenceEqual(Forecast(c, Season.Autumn, 30))).OverrideFailureMessage("Different seeds must change the weather.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MapIdentifierSeparatesCounterStreams()
    {
        var source = new Source(Profile);
        var a = new WeatherProcess(source);
        var b = new WeatherProcess(source);
        a.Reset(new(42)); b.Reset(new(42));
        a.Enter(Map, "test", 0); b.Enter(new("weather-test-b"), "test", 0);
        AssertThat(!Forecast(a, Season.Autumn, 30).SequenceEqual(Forecast(b, Season.Autumn, 30))).OverrideFailureMessage("Map identifier must separate the weather stream.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void GeneratedWeatherRespectsTwilightMinimumsAndPrecipitationLimits()
    {
        var profile = Profile;
        foreach (var season in Enum.GetValues<Season>())
        {
            var ordered = Forecast(Create(195, season, profile), season, 60).OrderBy(s => s.FromHours).ToArray();
            foreach (var span in ordered)
            {
                AssertThat(WeatherRules.IsNative(span.Kind, season)).OverrideFailureMessage("Non-native weather generated.").IsTrue();
                AssertThat(!WeatherRules.IsProtected(span.FromHours, 0, season)).OverrideFailureMessage("Automatic start enters a twilight band.").IsTrue();
                AssertThat(!WeatherRules.IsProtected(span.ToHours, 0, season)).OverrideFailureMessage("Automatic end enters a twilight band.").IsTrue();
                AssertThat(span.ToHours - span.FromHours + TestTolerance.WorldHours >= WeatherRules.MinimumHours(span.Kind)).OverrideFailureMessage("Phase too short.").IsTrue();
                if (span.Kind == WeatherKind.Fog)
                {
                    AssertThat(WeatherRules.IsNight(span.FromHours, 0, season)).OverrideFailureMessage("Fog starts during daylight.").IsTrue();
                    double clock = WeatherRules.Clock(span.ToHours);
                    var bands = WeatherRules.Bands(season);
                    AssertThat(clock >= bands.DawnTo && clock < bands.DuskFrom).OverrideFailureMessage("Fog clears before the sun is above +3 degrees.").IsTrue();
                }
            }
            // Days are independent, so the refractory gap and the daily cap hold inside each day.
            foreach (var day in ordered.Where(s => WeatherRules.IsPrecipitation(s.Kind))
                .GroupBy(s => Math.Floor((s.FromHours - profile.DayBoundaryClock) / 24)))
            {
                var rain = day.ToArray();
                for (int i = 1; i < rain.Length; i++) AssertThat(rain[i].FromHours - rain[i - 1].ToHours >= 8 - TestTolerance.WorldHours).OverrideFailureMessage("Precipitation refractory gap violated.").IsTrue();
                AssertThat(rain.Sum(s => s.ToHours - s.FromHours) <= 12 + TestTolerance.WorldHours).OverrideFailureMessage("Daily precipitation exceeds 12 hours.").IsTrue();
            }
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NewGameSeedsRemainClearAtHandoff()
    {
        var source = new Source(Profile);
        var maps = TestMaps();
        for (uint seed = 0; seed < (uint)TestSamples.WeatherSeeds; seed++)
        {
            var p = new WeatherProcess(source);
            var section = new WeatherSection(p, maps, source);
            var dto = section.CreateDefault(new NewGameContext(seed, Map, new(17 + 5 / 60.0, Season.Autumn, WeatherKind.Clear), ViewMode.Walk));
            if (seed == 0)
            {
                string json = JsonSerializer.Serialize(dto, section.TypeInfo);
                AssertThat(json.Contains("\"pinFromHours\":null", StringComparison.Ordinal)).OverrideFailureMessage("Strict nullable constructor fields must be written.").IsTrue();
                dto = JsonSerializer.Deserialize(json, section.TypeInfo)!;
            }
            section.Restore(dto, new RestoreContext { Map = Map, FromVersion = 2, Missing = false });
            p.Enter(Map, "test", 17.5);
            p.Step(Tick(17.5));
            AssertThat(p.Seed == seed && p.Selection.Kind == WeatherKind.Clear && p.Selection.Mode == WeatherMode.Auto && p.Now.Precip == 0).OverrideFailureMessage($"New-game seed {seed} is not clear at handoff.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void SaveRoundTripContinuesIdentically()
    {
        var profile = Profile;
        var source = new Source(profile);
        foreach (var season in new[] { Season.Autumn, Season.Winter })
        {
            var rain = season == Season.Winter ? WeatherKind.Snow : WeatherKind.Rain;
            var a = Create(17, season, profile);
            a.Step(Tick(150, season));
            a.Select(new(WeatherMode.Manual, rain, 1));
            double savedAt = season == Season.Winter ? 157.5 : 150.5;
            a.Step(Tick(savedAt, season)); a.Step(Tick(savedAt, season));
            var section = new WeatherSection(a, TestMaps(), source);
            var saved = section.Capture(new CaptureContext(SaveReason.Quit, Map, true));
            var restored = JsonSerializer.Deserialize(JsonSerializer.Serialize(saved, section.TypeInfo), section.TypeInfo)!;
            var b = new WeatherProcess(source);
            new WeatherSection(b, TestMaps(), source).Restore(restored, new RestoreContext { Map = Map, FromVersion = 2, Missing = false });
            b.Enter(Map, "test", savedAt);
            Equal(a, b, restoredSnow: true);
            if (season == Season.Winter)
                AssertThat(saved.Process.SnowAdd > 1 && saved.Process.SnowAdd <= WeatherRules.MaxSnow).OverrideFailureMessage("Deep snow did not survive the save section.").IsTrue();
            a.Select(new(WeatherMode.Auto, rain, .6f)); b.Select(new(WeatherMode.Auto, rain, .6f));
            for (int i = 0; i < 300; i++)
            {
                double hours = savedAt + i * .137;
                a.Step(Tick(hours, season)); b.Step(Tick(hours, season));
                if (i >= 2) Equal(a, b, restoredSnow: true);
            }
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ManualSurfaceAmountsGrowAndDrainWithWorldTime()
    {
        var p = Create(5, Season.Autumn, Profile);
        p.Select(new(WeatherMode.Manual, WeatherKind.Rain, .6f));
        p.Step(Tick(10));
        p.Select(new(WeatherMode.Auto, WeatherKind.Rain, .6f));
        p.Step(Tick(10.5));
        AssertThat(p.Capture().Wet > 0).OverrideFailureMessage("Leftover wetness was not kept.").IsTrue();
        p.Step(Tick(13));
        var saved = p.Capture();
        AssertThat(saved.Wet == 0 && saved.Puddle == 0).OverrideFailureMessage("Leftover wetness did not drain.").IsTrue();

        var snow = Create(5, Season.Winter, Profile);
        snow.Select(new(WeatherMode.Manual, WeatherKind.Snow, 1));
        AssertThat(snow.Capture().SnowAdd == 0).OverrideFailureMessage("Selecting snow created an instant snow target.").IsTrue();
        snow.Step(Tick(1, Season.Winter));
        AssertThat(Math.Abs(snow.Now.SnowAdd - WeatherRules.SnowGrowthPerHour) <= TestTolerance.FloatRelative).OverrideFailureMessage("Snow did not grow by world time.").IsTrue();
        var paused = snow.Now;
        snow.Step(Tick(1, Season.Winter) with { RealDt = 20, FlowRate = 0 });
        AssertThat(snow.Now == paused).OverrideFailureMessage("Paused snow grew with real time.").IsTrue();
        var landing = new WorldSaveMoment(10, 0, Season.Winter, 0);
        var deep = snow.CaptureAt(in landing);
        AssertThat(deep.SnowAdd == WeatherRules.MaxSnow && snow.Now == paused && snow.Capture().LeftAtHours == 1).OverrideFailureMessage("Projected snow did not reach its cap, or capture advanced the live world.").IsTrue();
        snow.Step(Tick(10, Season.Winter));
        var pinned = Create(5, Season.Winter, Profile);
        pinned.Pin(0, WeatherKind.Snow, 1, 0, 10);
        pinned.Step(Tick(9, Season.Winter));
        AssertThat(pinned.Now.SnowAdd > 1 && pinned.Now.SnowAdd < deep.SnowAdd).OverrideFailureMessage("Pinned snow bypassed accumulation.").IsTrue();
        snow.Select(new(WeatherMode.Manual, WeatherKind.AfterSnow, 1));
        snow.Step(Tick(11, Season.Winter));
        AssertThat(snow.Now.SnowAdd < deep.SnowAdd && snow.Now.SnowAdd > 1).OverrideFailureMessage("After-snow weather injected a fresh target instead of draining the saved snow.").IsTrue();
        foreach (float intensity in new[] { WeatherRules.LightSnowIntensity, .5f, .8f })
        {
            var strength = Create(5, Season.Winter, Profile);
            strength.Select(new(WeatherMode.Manual, WeatherKind.Snow, intensity));
            var late = new WorldSaveMoment(20, 0, Season.Winter, 0);
            double target = intensity >= WeatherRules.HeavySnowIntensity ? WeatherRules.MaxSnow : WeatherRules.SnowTarget(intensity);
            AssertThat(Math.Abs(strength.CaptureAt(in late).SnowAdd - target) <= TestTolerance.FloatRelative).OverrideFailureMessage("Snow missed its intensity target or natural heavy snow could not reach maximum.").IsTrue();
        }
        float previous = 0;
        foreach (var kind in new[] { WeatherKind.Clear, WeatherKind.Cloudy, WeatherKind.Overcast })
        {
            var melting = Create(5, Season.Winter, Profile);
            melting.Restore(deep);
            melting.Select(new(WeatherMode.Manual, kind, 1));
            melting.Step(Tick(11, Season.Winter));
            AssertThat(melting.Now.SnowAdd > previous && melting.Now.SnowAdd < deep.SnowAdd).OverrideFailureMessage("Snow did not melt faster in clear weather than under clouds.").IsTrue();
            previous = melting.Now.SnowAdd;
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ManualCaptureAndSeasonReplacementAreSettled()
    {
        var profile = Profile;
        var a = Create(9, Season.Summer, profile);
        a.Select(new(WeatherMode.Manual, WeatherKind.Thunderstorm, .8f));
        a.Step(Tick(0, Season.Summer) with { RealDt = .1 });
        AssertThat(a.Transitioning).OverrideFailureMessage("Manual selection should transition.").IsTrue();
        a.Reseason(0, Season.Winter, 0);
        var saved = a.Capture();
        AssertThat(saved.Selection.Kind == WeatherKind.Snow && saved.Selection.Intensity == 1).OverrideFailureMessage("Winter storm should become heavy snow.").IsTrue();
        var b = Create(9, Season.Winter, profile); b.Restore(saved);
        AssertThat(!b.Transitioning && b.Now.PrecipKind == 1).OverrideFailureMessage("Restore must land immediately on the replacement.").IsTrue();
        b.Reseason(0, Season.Autumn, 0);
        AssertThat(b.Selection.Kind == WeatherKind.Rain).OverrideFailureMessage("Snow should become rain outside winter.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PinAndFastForwardCaptureUseTheLandingTime()
    {
        var p = Create(15, Season.Autumn, Profile);
        p.Pin(0, WeatherKind.Fog, .5f, 4.5, 7);
        p.Step(Tick(1) with { TargetHours = 5 });
        var saved = p.Capture();
        AssertThat(saved.LeftAtHours == 5 && saved.PinKind == WeatherKind.Fog && saved.PinToHours == 7).OverrideFailureMessage("Forced capture must match the world landing time.").IsTrue();
        var restored = Create(15, Season.Autumn, Profile); restored.Restore(saved);
        AssertThat(restored.Selection.Kind == WeatherKind.Fog && !restored.Transitioning).OverrideFailureMessage("Pinned weather must restore settled.").IsTrue();
        restored.Step(Tick(5));
        AssertThat(restored.Now.Fog > 1).OverrideFailureMessage("First restored step must preserve the pin.").IsTrue();
        var snow = Create(15, Season.Winter, Profile);
        snow.Pin(0, WeatherKind.Snow, .8f, 0, 20);
        snow.Step(Tick(1, Season.Winter) with { TargetHours = 7.5 });
        var snowSave = snow.Capture();
        AssertThat(snowSave.LeftAtHours == 7.5 && snowSave.SnowAdd > 1).OverrideFailureMessage("Fast-forward snow capture used the visual frame instead of the landing time.").IsTrue();
        var snowResume = Create(15, Season.Winter, Profile);
        snowResume.Restore(snowSave);
        snow.Step(Tick(8, Season.Winter)); snowResume.Step(Tick(8, Season.Winter));
        Equal(snow, snowResume, restoredSnow: true);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PauseFreezesNaturalWeatherAndBootRejectsInvalidInput()
    {
        var p = Create(1, Season.Autumn, Profile);
        p.Step(Tick(16.1)); var before = p.Now;
        for (int i = 0; i < 40; i++) p.Step(Tick(16.1) with { FlowRate = 0, RealDt = .5 });
        AssertThat(p.Now == before).OverrideFailureMessage("Paused natural weather changed.").IsTrue();
        AssertThrown(() => p.ValidateBoot(new(1, WeatherKind.Snow), Season.Summer)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => p.ValidateBoot(new(1, Wet: float.NaN), Season.Autumn)).IsInstanceOf<ArgumentException>();
        p.Reset(new(4));
        p.Step(Tick(0));
        AssertThat(p.Seed == 4 && p.Capture().LeftAtHours == 0).OverrideFailureMessage("New-game Reset retained the previous instance's clock.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void CaptureAtIsPureAndProjectsTheSaveMoment()
    {
        var profile = Profile;
        var process = Create(9, Season.Summer, profile);
        process.Select(new(WeatherMode.Manual, WeatherKind.Thunderstorm, .8f));
        process.Step(Tick(0, Season.Summer) with { RealDt = .1 });
        string before = SnapshotJson(process.Capture());
        var beforeNow = process.Now;
        var beforeAhead = process.Ahead.ToArray();
        var moment = new WorldSaveMoment(2, 1.25, Season.Winter, 0);
        var saved = process.CaptureAt(in moment);
        AssertThat(saved.LeftAtHours == 2 && saved.Season == Season.Winter && saved.ClockOffset == 1.25 &&
            saved.Selection.Kind == WeatherKind.Snow && saved.Selection.Intensity == 1 && saved.Wet == 0 && saved.Puddle == 0).OverrideFailureMessage("Save projection changed existing wet residue or missed the moment's season/offset.").IsTrue();
        AssertThat(before == SnapshotJson(process.Capture()) && process.Now == beforeNow && process.Ahead.SequenceEqual(beforeAhead) && process.Transitioning).OverrideFailureMessage("CaptureAt changed live state.").IsTrue();
        var resumed = Create(9, Season.Winter, profile);
        resumed.Restore(JsonSerializer.Deserialize(SnapshotJson(saved), TestJson.Contract.WeatherProcessState)!);
        process.Reseason(0, Season.Winter, 1.25);
        var first = Tick(2, Season.Winter) with { ClockOffset = 1.25 };
        process.Step(first); resumed.Step(first);
        for (int i = 1; i < 100; i++)
        {
            var tick = Tick(2 + i * .17, Season.Winter) with { ClockOffset = 1.25 };
            process.Step(tick); resumed.Step(tick);
            Equal(process, resumed, restoredSnow: true);
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void QuietWindowAndBootWetSurviveTheFirstStepAndKeepTheBlend()
    {
        var process = new WeatherProcess(new Source(Profile));
        process.Reset(new(83, Wet: .37f));
        process.Enter(Map, "test", 6);
        double until = WeatherRules.QuietEnd(6, .75, Season.Winter);
        process.Quiet(6, until);
        var tick = Tick(6, Season.Winter) with { ClockOffset = .75, FlowRate = 0, RealDt = .1 };
        process.Step(tick);
        var saved = process.Capture();
        AssertThat(saved.Season == Season.Winter && saved.ClockOffset == .75 && saved.QuietUntilHours == until && saved.WetOverride == .37f &&
            process.Selection is { Mode: WeatherMode.Auto, Kind: WeatherKind.Clear }).OverrideFailureMessage("Boot options overwrote Quiet or lost the explicit wet value.").IsTrue();
        process.Step(tick with { RealDt = 2.8 });
        AssertThat(process.Transitioning && process.Now.Wet < .37f).OverrideFailureMessage("Quiet shortened the three-second blend.").IsTrue();
        process.Step(tick with { RealDt = .11 });
        AssertThat(!process.Transitioning && process.Now.Wet == .37f).OverrideFailureMessage("Quiet did not settle after three real seconds.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PinBeforeTheFirstStepBindsToTheSeasonAndBlendsInThreeSeconds()
    {
        var process = new WeatherProcess(new Source(Profile));
        process.Reset(new(84));
        process.Enter(Map, "test", 6);
        process.Pin(6, WeatherKind.Snow, .4f, 5.5, 8);
        var tick = Tick(6, Season.Winter) with { ClockOffset = .25, FlowRate = 0, RealDt = .5 };
        process.Step(tick);
        AssertThat(process.Selection is { Mode: WeatherMode.Auto, Kind: WeatherKind.Snow, Intensity: .4f } &&
            process.Now.PrecipKind is > 0 and < 1 && process.Transitioning).OverrideFailureMessage("Initial snow Pin did not begin its real-time blend.").IsTrue();
        process.Step(tick with { RealDt = 2.4 });
        AssertThat(process.Transitioning && process.Now.PrecipKind < 1).OverrideFailureMessage("Initial Pin settled before three seconds.").IsTrue();
        process.Step(tick with { RealDt = .11 });
        AssertThat(!process.Transitioning && process.Now.PrecipKind == 1).OverrideFailureMessage("Initial Pin did not settle.").IsTrue();
        process.Pin(6, WeatherKind.Clear, null, 6, 8);
        process.Step(tick with { RealDt = .1 });
        process.Step(tick with { RealDt = 2.8 });
        AssertThat(process.Transitioning && process.Now.PrecipKind > 0).OverrideFailureMessage("A normal Pin lost its three-second blend.").IsTrue();
        process.Step(tick with { RealDt = .11 });
        AssertThat(!process.Transitioning && process.Now.PrecipKind == 0).OverrideFailureMessage("Normal Pin did not settle.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ExplicitBootWeatherKeepsEveryParameter()
    {
        var profile = Profile;
        var manual = new WeatherProcess(new Source(profile));
        IWeatherProcess options = manual;
        var boot = new WeatherBoot(85, WeatherKind.Snow, .82f, .27f);
        options.ValidateBoot(boot, Season.Winter);
        manual.Reset(boot); manual.Enter(Map, "test", 6);
        manual.Quiet(6, WeatherRules.QuietEnd(6, 0, Season.Winter));
        manual.Step(Tick(6, Season.Winter));
        var saved = manual.Capture();
        AssertThat(manual.Seed == 85 && saved.Selection is { Mode: WeatherMode.Manual, Kind: WeatherKind.Snow, Intensity: .82f } && saved.WetOverride == .27f &&
            saved.QuietUntilHours == 0).OverrideFailureMessage("A map default consumed explicit boot weather or scalar arguments.").IsTrue();
        AssertThat(options.MapToSeason(WeatherKind.Rain, Season.Winter) == WeatherKind.Snow &&
            options.MapToSeason(WeatherKind.Snow, Season.Autumn) == WeatherKind.Rain).OverrideFailureMessage("Map boot weather did not follow the target season.").IsTrue();

        var timed = new WeatherProcess(new Source(profile));
        var timedBoot = boot with { RemainingMinutes = 90 };
        options.ValidateBoot(timedBoot, Season.Winter);
        timed.Reset(timedBoot); timed.Enter(Map, "test", 6);
        timed.Step(Tick(6, Season.Winter));
        saved = timed.Capture();
        AssertThat(timed.Selection is { Mode: WeatherMode.Auto, Kind: WeatherKind.Snow, Intensity: .82f } &&
            saved.PinKind == WeatherKind.Snow && saved.PinFromHours == 6 && saved.PinToHours == 7.5).OverrideFailureMessage("--weather-remaining did not become a pinned window.").IsTrue();
    }

    private static List<WeatherSpan> Forecast(WeatherProcess p, Season season, int days)
    {
        var seen = new List<WeatherSpan>();
        for (double hour = 0; hour <= 24 * days; hour += 6)
        {
            p.Step(Tick(hour, season));
            foreach (var span in p.Ahead)
                if (span.Kind != WeatherKind.Clear && span.FromHours > hour && span.ToHours < hour + 24 &&
                    !seen.Any(s => s.FromHours == span.FromHours && s.Kind == span.Kind)) seen.Add(span);
        }
        return seen;
    }

    private static WeatherProcess Create(uint seed, Season season, WeatherProfile profile)
    {
        var p = new WeatherProcess(new Source(profile));
        p.Reset(new(seed)); p.Enter(Map, "test", 0); p.Step(Tick(0, season));
        return p;
    }
    private static WeatherTick Tick(double hours, Season season = Season.Autumn) => new(hours, hours, 0, season, 10, 60, 3);
    private static void Equal(WeatherProcess a, WeatherProcess b, bool restoredSnow = false)
    {
        var nowA = a.Now;
        var nowB = b.Now;
        if (restoredSnow)
        {
            // Re-anchoring the saved float can change the last bit of later double arithmetic.
            AssertThat(Math.Abs(nowA.SnowAdd - nowB.SnowAdd) <= TestTolerance.FloatRelative).OverrideFailureMessage("Restored snow diverged.").IsTrue();
            nowA = nowA with { SnowAdd = 0 };
            nowB = nowB with { SnowAdd = 0 };
        }
        var liveEqual = a.Selection.Mode == b.Selection.Mode && a.Selection.Kind == b.Selection.Kind
            && BitConverter.SingleToInt32Bits(a.Selection.Intensity) == BitConverter.SingleToInt32Bits(b.Selection.Intensity)
            && MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref nowA, 1))
                .SequenceEqual(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref nowB, 1)));
        var forecastEqual = a.Ahead.Count == b.Ahead.Count && a.Ahead.Zip(b.Ahead).All(pair =>
            BitConverter.DoubleToInt64Bits(pair.First.FromHours) == BitConverter.DoubleToInt64Bits(pair.Second.FromHours)
            && BitConverter.DoubleToInt64Bits(pair.First.ToHours) == BitConverter.DoubleToInt64Bits(pair.Second.ToHours)
            && pair.First.Kind == pair.Second.Kind
            && BitConverter.SingleToInt32Bits(pair.First.Intensity) == BitConverter.SingleToInt32Bits(pair.Second.Intensity));
        AssertThat(liveEqual && forecastEqual).OverrideFailureMessage("Live weather or forecast differs bitwise.").IsTrue();
        var savedA = a.Capture();
        var savedB = b.Capture();
        if (restoredSnow)
        {
            AssertThat(Math.Abs(savedA.SnowAdd - savedB.SnowAdd) <= TestTolerance.FloatRelative).OverrideFailureMessage("Captured snow differs after restore.").IsTrue();
            savedA = savedA with { SnowAdd = 0 };
            savedB = savedB with { SnowAdd = 0 };
        }
        AssertThat(SnapshotJson(savedA) == SnapshotJson(savedB)).OverrideFailureMessage("Captured weather state differs.").IsTrue();
    }
    private static string SnapshotJson(WeatherProcessState state) => JsonSerializer.Serialize(state, TestJson.Contract.WeatherProcessState);

    private sealed class Source(WeatherProfile profile) : IWeatherProfileSource { public WeatherProfile Load(string name) => profile; }
    private static TestMapService TestMaps() => new()
    {
        Rev = 1,
        Default = Map,
        Current = Map,
        Maps =
        [
            new MapDescriptor(Map, new("测试", "Test"), MapKind.Outdoor, 1, 1, null!, default, default, null!, null!,
                default, default, "test", Array.Empty<MapSpawn>(),
                new(MapTimeRule.Shared, new(17.5, Season.Autumn, WeatherKind.Clear), new("测试", "Test"), true, "test"),
                null!, Array.Empty<MapPortal>())
        ],
    };
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(WeatherProcessState))]
internal partial class TestJson : JsonSerializerContext
{
    public static TestJson Contract { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    });
}
