using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Tests;
using GdUnit4;

namespace Campus.Systems.Weather.Tests;

[TestSuite]
public sealed class WeatherProcessTests
{
    private static readonly MapId Map = new("weather-test-a");
    private static WeatherProfile Profile => WeatherProfile.Parse(File.ReadAllText(Path.Combine(
        Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? throw new InvalidOperationException("UESTC_REPOSITORY is required."),
        "game", "data", "weather", "chengdu", "profile.json")));

    [TestCase]
    public void SameSeedMapAndTimeGiveTheSameWeatherInAnyCallOrder()
    {
        var profile = Profile;
        var a = Create(127, Season.Autumn, profile);
        var b = Create(127, Season.Autumn, profile);
        for (int hour = 1; hour <= 240; hour++) b.Step(Tick(hour));
        a.Step(Tick(240));
        Equal(a, b);
        var c = Create(128, Season.Autumn, profile);
        Check(!Forecast(Create(127, Season.Autumn, profile), Season.Autumn, 30).SequenceEqual(Forecast(c, Season.Autumn, 30)),
            "Different seeds must change the weather.");
    }

    [TestCase]
    public void MapIdentifierSeparatesCounterStreams()
    {
        var source = new Source(Profile);
        var a = new WeatherProcess(source);
        var b = new WeatherProcess(source);
        a.Reset(new(42)); b.Reset(new(42));
        a.Enter(Map, "test", 0); b.Enter(new("weather-test-b"), "test", 0);
        Check(!Forecast(a, Season.Autumn, 30).SequenceEqual(Forecast(b, Season.Autumn, 30)),
            "Map identifier must separate the weather stream.");
    }

    [TestCase]
    public void GeneratedWeatherRespectsTwilightMinimumsAndPrecipitationLimits()
    {
        var profile = Profile;
        foreach (var season in Enum.GetValues<Season>())
        {
            var ordered = Forecast(Create(195, season, profile), season, 60).OrderBy(s => s.FromHours).ToArray();
            foreach (var span in ordered)
            {
                Check(WeatherRules.IsNative(span.Kind, season), "Non-native weather generated.");
                Check(!WeatherRules.IsProtected(span.FromHours, 0, season), "Automatic start enters a twilight band.");
                Check(!WeatherRules.IsProtected(span.ToHours, 0, season), "Automatic end enters a twilight band.");
                Check(span.ToHours - span.FromHours + 1e-9 >= WeatherRules.MinimumHours(span.Kind), "Phase too short.");
                if (span.Kind == WeatherKind.Fog)
                {
                    Check(WeatherRules.IsNight(span.FromHours, 0, season), "Fog starts during daylight.");
                    double clock = WeatherRules.Clock(span.ToHours);
                    var bands = WeatherRules.Bands(season);
                    Check(clock >= bands.DawnTo && clock < bands.DuskFrom, "Fog clears before the sun is above +3 degrees.");
                }
            }
            // Days are independent, so the refractory gap and the daily cap hold inside each day.
            foreach (var day in ordered.Where(s => WeatherRules.IsPrecipitation(s.Kind))
                .GroupBy(s => Math.Floor((s.FromHours - profile.DayBoundaryClock) / 24)))
            {
                var rain = day.ToArray();
                for (int i = 1; i < rain.Length; i++) Check(rain[i].FromHours - rain[i - 1].ToHours >= 8 - 1e-9, "Precipitation refractory gap violated.");
                Check(rain.Sum(s => s.ToHours - s.FromHours) <= 12 + 1e-9, "Daily precipitation exceeds 12 hours.");
            }
        }
    }

    [TestCase]
    public void NewGameSeedsRemainClearAtHandoff()
    {
        var source = new Source(Profile);
        var maps = TestMaps();
        for (uint seed = 0; seed < 500; seed++)
        {
            var p = new WeatherProcess(source);
            var section = new WeatherSection(p, maps, source);
            var dto = section.CreateDefault(new NewGameContext(seed, Map, new(17 + 5 / 60.0, Season.Autumn, WeatherKind.Clear)));
            if (seed == 0)
            {
                string json = JsonSerializer.Serialize(dto, section.TypeInfo);
                Check(json.Contains("\"pinFromHours\":null", StringComparison.Ordinal), "Strict nullable constructor fields must be written.");
                dto = JsonSerializer.Deserialize(json, section.TypeInfo)!;
            }
            section.Restore(dto, new RestoreContext { Map = Map, FromVersion = 2, Missing = false });
            p.Enter(Map, "test", 17.5);
            p.Step(Tick(17.5));
            Check(p.Seed == seed && p.Selection.Kind == WeatherKind.Clear && p.Selection.Mode == WeatherMode.Auto && p.Now.Precip == 0,
                $"New-game seed {seed} is not clear at handoff.");
        }
    }

    [TestCase]
    public void SaveRoundTripContinuesIdentically()
    {
        var profile = Profile;
        var source = new Source(profile);
        foreach (var season in new[] { Season.Autumn, Season.Winter })
        {
            var rain = season == Season.Winter ? WeatherKind.Snow : WeatherKind.Rain;
            var a = Create(17, season, profile);
            a.Step(Tick(150, season));
            a.Select(new(WeatherMode.Manual, rain, .6f));
            a.Step(Tick(150.5, season)); a.Step(Tick(150.5, season));
            var section = new WeatherSection(a, TestMaps(), source);
            var saved = section.Capture(new CaptureContext(SaveReason.Quit, Map, true));
            var restored = JsonSerializer.Deserialize(JsonSerializer.Serialize(saved, section.TypeInfo), section.TypeInfo)!;
            var b = new WeatherProcess(source);
            new WeatherSection(b, TestMaps(), source).Restore(restored, new RestoreContext { Map = Map, FromVersion = 2, Missing = false });
            b.Enter(Map, "test", 150.5);
            Equal(a, b);
            a.Select(new(WeatherMode.Auto, rain, .6f)); b.Select(new(WeatherMode.Auto, rain, .6f));
            for (int i = 0; i < 300; i++)
            {
                double hours = 150.5 + i * .137;
                a.Step(Tick(hours, season)); b.Step(Tick(hours, season));
                if (i >= 2) Equal(a, b);
            }
        }
    }

    [TestCase]
    public void ManualWetnessDrainsAfterReturningToAuto()
    {
        var p = Create(5, Season.Autumn, Profile);
        p.Select(new(WeatherMode.Manual, WeatherKind.Rain, .6f));
        p.Step(Tick(10));
        p.Select(new(WeatherMode.Auto, WeatherKind.Rain, .6f));
        p.Step(Tick(10.5));
        Check(p.Capture().Wet > 0, "Leftover wetness was not kept.");
        p.Step(Tick(13));
        var saved = p.Capture();
        Check(saved.Wet == 0 && saved.Puddle == 0, "Leftover wetness did not drain.");
    }

    [TestCase]
    public void ManualCaptureAndSeasonReplacementAreSettled()
    {
        var profile = Profile;
        var a = Create(9, Season.Summer, profile);
        a.Select(new(WeatherMode.Manual, WeatherKind.Thunderstorm, .8f));
        a.Step(Tick(0, Season.Summer) with { RealDt = .1 });
        Check(a.Transitioning, "Manual selection should transition.");
        a.Reseason(0, Season.Winter, 0);
        var saved = a.Capture();
        Check(saved.Selection.Kind == WeatherKind.Snow && saved.Selection.Intensity == 1, "Winter storm should become heavy snow.");
        var b = Create(9, Season.Winter, profile); b.Restore(saved);
        Check(!b.Transitioning && b.Now.PrecipKind == 1, "Restore must land immediately on the replacement.");
        b.Reseason(0, Season.Autumn, 0);
        Check(b.Selection.Kind == WeatherKind.Rain, "Snow should become rain outside winter.");
    }

    [TestCase]
    public void PinAndFastForwardCaptureUseTheLandingTime()
    {
        var p = Create(15, Season.Autumn, Profile);
        p.Pin(0, WeatherKind.Fog, .5f, 4.5, 7);
        p.Step(Tick(1) with { TargetHours = 5 });
        var saved = p.Capture();
        Check(saved.LeftAtHours == 5 && saved.PinKind == WeatherKind.Fog && saved.PinToHours == 7, "Forced capture must match the world landing time.");
        var restored = Create(15, Season.Autumn, Profile); restored.Restore(saved);
        Check(restored.Selection.Kind == WeatherKind.Fog && !restored.Transitioning, "Pinned weather must restore settled.");
        restored.Step(Tick(5));
        Check(restored.Now.Fog > 1, "First restored step must preserve the pin.");
    }

    [TestCase]
    public void PauseFreezesNaturalWeatherAndCommandLineRejectsInvalidInput()
    {
        var p = Create(1, Season.Autumn, Profile);
        p.Step(Tick(16.1)); var before = p.Now;
        for (int i = 0; i < 40; i++) p.Step(Tick(16.1) with { FlowRate = 0, RealDt = .5 });
        Check(p.Now == before, "Paused natural weather changed.");
        Reject(() => p.ParseBootOptions(new Dictionary<string, string> { ["weather"] = "snow" }, 1, Season.Summer));
        Reject(() => p.ParseBootOptions(new Dictionary<string, string> { ["weather-seed"] = "4", ["load"] = "save.sav" }, 1, Season.Autumn));
        Reject(() => p.ParseBootOptions(new Dictionary<string, string> { ["wet"] = "NaN" }, 1, Season.Autumn));
        p.Reset(new(4));
        p.Step(Tick(0));
        Check(p.Seed == 4 && p.Capture().LeftAtHours == 0, "New-game Reset retained the previous instance's clock.");
    }

    [TestCase]
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
        Check(saved.LeftAtHours == 2 && saved.Season == Season.Winter && saved.ClockOffset == 1.25 &&
            saved.Selection.Kind == WeatherKind.Snow && saved.Selection.Intensity == 1,
            "Save moment's season/offset were not applied to the private snapshot.");
        Check(before == SnapshotJson(process.Capture()) && process.Now == beforeNow && process.Ahead.SequenceEqual(beforeAhead) && process.Transitioning,
            "CaptureAt changed live state.");
        var resumed = Create(9, Season.Winter, profile);
        resumed.Restore(JsonSerializer.Deserialize(SnapshotJson(saved), TestJson.Contract.WeatherProcessState)!);
        process.Reseason(0, Season.Winter, 1.25);
        var first = Tick(2, Season.Winter) with { ClockOffset = 1.25 };
        process.Step(first); resumed.Step(first);
        for (int i = 1; i < 100; i++)
        {
            var tick = Tick(2 + i * .17, Season.Winter) with { ClockOffset = 1.25 };
            process.Step(tick); resumed.Step(tick);
            Equal(process, resumed);
        }
    }

    [TestCase]
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
        Check(saved.Season == Season.Winter && saved.ClockOffset == .75 && saved.QuietUntilHours == until && saved.WetOverride == .37f &&
            process.Selection is { Mode: WeatherMode.Auto, Kind: WeatherKind.Clear }, "Boot options overwrote Quiet or lost the explicit wet value.");
        process.Step(tick with { RealDt = 2.8 });
        Check(process.Transitioning && process.Now.Wet < .37f, "Quiet shortened the three-second blend.");
        process.Step(tick with { RealDt = .11 });
        Check(!process.Transitioning && process.Now.Wet == .37f, "Quiet did not settle after three real seconds.");
    }

    [TestCase]
    public void PinBeforeTheFirstStepBindsToTheSeasonAndBlendsInThreeSeconds()
    {
        var process = new WeatherProcess(new Source(Profile));
        process.Reset(new(84));
        process.Enter(Map, "test", 6);
        process.Pin(6, WeatherKind.Snow, .4f, 5.5, 8);
        var tick = Tick(6, Season.Winter) with { ClockOffset = .25, FlowRate = 0, RealDt = .5 };
        process.Step(tick);
        Check(process.Selection is { Mode: WeatherMode.Auto, Kind: WeatherKind.Snow, Intensity: .4f } &&
            process.Now.PrecipKind is > 0 and < 1 && process.Transitioning, "Initial snow Pin did not begin its real-time blend.");
        process.Step(tick with { RealDt = 2.4 });
        Check(process.Transitioning && process.Now.PrecipKind < 1, "Initial Pin settled before three seconds.");
        process.Step(tick with { RealDt = .11 });
        Check(!process.Transitioning && process.Now.PrecipKind == 1, "Initial Pin did not settle.");
        process.Pin(6, WeatherKind.Clear, null, 6, 8);
        process.Step(tick with { RealDt = .1 });
        process.Step(tick with { RealDt = 2.8 });
        Check(process.Transitioning && process.Now.PrecipKind > 0, "A normal Pin lost its three-second blend.");
        process.Step(tick with { RealDt = .11 });
        Check(!process.Transitioning && process.Now.PrecipKind == 0, "Normal Pin did not settle.");
    }

    [TestCase]
    public void ExplicitBootWeatherKeepsEveryParameter()
    {
        var profile = Profile;
        var manual = new WeatherProcess(new Source(profile));
        IWeatherProcess options = manual;
        var arguments = new Dictionary<string, string>
        {
            ["weather-seed"] = "85", ["weather"] = "snow", ["weather-intensity"] = ".82", ["wet"] = ".27"
        };
        manual.Reset(options.ParseBootOptions(arguments, 1, Season.Winter)); manual.Enter(Map, "test", 6);
        manual.Quiet(6, WeatherRules.QuietEnd(6, 0, Season.Winter));
        manual.Step(Tick(6, Season.Winter));
        var saved = manual.Capture();
        Check(manual.Seed == 85 && saved.Selection is { Mode: WeatherMode.Manual, Kind: WeatherKind.Snow, Intensity: .82f } && saved.WetOverride == .27f &&
            saved.QuietUntilHours == 0, "A map default consumed explicit boot weather or scalar arguments.");
        Check(options.MapToSeason(WeatherKind.Rain, Season.Winter) == WeatherKind.Snow &&
            options.MapToSeason(WeatherKind.Snow, Season.Autumn) == WeatherKind.Rain,
            "Map boot weather did not follow the target season.");

        var timed = new WeatherProcess(new Source(profile));
        arguments["weather-remaining"] = "90";
        timed.Reset(options.ParseBootOptions(arguments, 1, Season.Winter)); timed.Enter(Map, "test", 6);
        timed.Step(Tick(6, Season.Winter));
        saved = timed.Capture();
        Check(timed.Selection is { Mode: WeatherMode.Auto, Kind: WeatherKind.Snow, Intensity: .82f } &&
            saved.PinKind == WeatherKind.Snow && saved.PinFromHours == 6 && saved.PinToHours == 7.5, "--weather-remaining did not become a pinned window.");
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
    private static void Equal(WeatherProcess a, WeatherProcess b)
    {
        Check(a.Selection == b.Selection && a.Now == b.Now && a.Ahead.SequenceEqual(b.Ahead), "Live weather or forecast differs bitwise.");
        Check(SnapshotJson(a.Capture()) == SnapshotJson(b.Capture()), "Captured weather state differs.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string SnapshotJson(WeatherProcessState state) => JsonSerializer.Serialize(state, TestJson.Contract.WeatherProcessState);
    private static void Reject(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid input was accepted.");
    }
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
                null!, new Dictionary<string, string>(), Array.Empty<MapPortal>())
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
