using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
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
    public void QueueRefillCadenceAndSameSeedAreBitIdentical()
    {
        var profile = Profile;
        var a = Create(127, Season.Autumn, profile);
        var b = Create(127, Season.Autumn, profile);
        for (int hour = 1; hour <= 240; hour++) b.Step(Tick(hour));
        a.Step(Tick(240));
        Equal(a, b);
        var c = Create(128, Season.Autumn, profile);
        c.Step(Tick(240));
        Check(!a.Capture(Map).Queue.SequenceEqual(c.Capture(Map).Queue), "Different seeds must change the queue.");
    }

    [TestCase]
    public void MapIdentifierSeparatesCounterStreams()
    {
        var source = new Source(Profile);
        var a = new WeatherProcess(source);
        var b = new WeatherProcess(source);
        a.Reset(new(42)); b.Reset(new(42));
        a.Enter(Map, "test", 0); b.Enter(new("weather-test-b"), "test", 0);
        a.Step(Tick(0)); b.Step(Tick(0));
        Check(!a.Capture(Map).Queue.SequenceEqual(b.Capture(new("weather-test-b")).Queue), "Map identifier must separate the forecast stream.");
    }

    [TestCase]
    public void GeneratedEdgesRespectTwilightMinimumsAndPrecipitationLimits()
    {
        var profile = Profile;
        foreach (var season in Enum.GetValues<Season>())
        {
            var process = Create(195, season, profile);
            var observed = new List<WeatherSpan>();
            for (int hour = 0; hour <= 24 * 14; hour += 12)
            {
                process.Step(Tick(hour, season));
                var saved = process.Capture(Map);
                double from = saved.QueueFromHours;
                foreach (var item in saved.Queue)
                {
                    var span = new WeatherSpan(from, from + item.Minutes / 60, item.Kind, item.Intensity!.Value);
                    if (!observed.Any(s => s.FromHours == span.FromHours && s.Kind == span.Kind)) observed.Add(span);
                    from = span.ToHours;
                }
            }
            var ordered = observed.Where(s => s.Kind != WeatherKind.Clear).OrderBy(s => s.FromHours).ToArray();
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
            var rain = ordered.Where(s => WeatherRules.IsPrecipitation(s.Kind)).ToArray();
            for (int i = 1; i < rain.Length; i++) Check(rain[i].FromHours - rain[i - 1].ToHours >= 8 - 1e-9, "Precipitation refractory gap violated.");
            foreach (var span in rain)
            {
                double end = span.ToHours;
                double total = rain.Sum(s => Math.Max(0, Math.Min(end, s.ToHours) - Math.Max(end - 24, s.FromHours)));
                Check(total <= 12 + 1e-9, "Rolling 24 hour precipitation exceeds 12 hours.");
            }
        }
    }

    [TestCase]
    public void TwoThousandNewGameSeedsRemainClearAtHandoff()
    {
        var source = new Source(Profile);
        var maps = new TestMaps();
        for (uint seed = 0; seed < 2000; seed++)
        {
            var p = new WeatherProcess(source);
            var seedSection = new WeatherSeedSection(p);
            var mapSection = new WeatherMapSection(p, maps, source);
            var context = new NewGameContext(seed, Map, new(17 + 5 / 60.0, Season.Autumn, WeatherKind.Clear));
            var restore = new RestoreContext { Map = Map, FromVersion = 1, Missing = false };
            var dto = mapSection.CreateDefault(context);
            if (seed == 0)
            {
                string json = JsonSerializer.Serialize(dto, mapSection.TypeInfo);
                Check(json.Contains("\"pinFromHours\":null", StringComparison.Ordinal), "Strict nullable constructor fields must be written.");
                dto = JsonSerializer.Deserialize(json, mapSection.TypeInfo)!;
            }
            seedSection.Restore(seedSection.CreateDefault(context), restore);
            mapSection.Restore(dto, restore);
            p.Step(Tick(17.5));
            Check(p.Selection.Kind == WeatherKind.Clear && p.Selection.Mode == WeatherMode.Auto && p.Now.Precip == 0,
                $"New-game seed {seed} is not clear at handoff.");
        }
    }

    [TestCase]
    public void SaveRoundTripContinuesIdenticallyThroughTrimAndRefill()
    {
        var profile = Profile;
        foreach (var season in new[] { Season.Autumn, Season.Winter })
        {
            var a = Create(17, season, profile);
            for (int hour = 1; hour < 150; hour++) a.Step(Tick(hour, season));
            var dto = a.Capture(Map);
            string json = JsonSerializer.Serialize(dto, TestJson.Contract.WeatherProcessState);
            var restored = JsonSerializer.Deserialize(json, TestJson.Contract.WeatherProcessState)!;
            var b = Create(17, season, profile);
            b.Restore(Map, restored);
            Equal(a, b);
            for (int i = 0; i < 300; i++)
            {
                double hours = 149 + i * .137;
                a.Step(Tick(hours, season)); b.Step(Tick(hours, season));
                Equal(a, b);
            }
        }
    }

    [TestCase]
    public void RainSnowAndWetOverrideResumeWithoutNewDecayAnchors()
    {
        var profile = Profile;
        foreach (var season in new[] { Season.Autumn, Season.Winter })
        {
            var rain = season == Season.Winter ? WeatherKind.Snow : WeatherKind.Rain;
            var after = season == Season.Winter ? WeatherKind.AfterSnow : WeatherKind.AfterRain;
            var boot = new WeatherBoot(3, Wet: .37f, Queue: new[] { new WeatherQueueItem(rain, 180) { Intensity = .8f }, new WeatherQueueItem(after, 90) { Intensity = 1 } });
            var a = new WeatherProcess(new Source(profile));
            a.Reset(boot); a.Enter(Map, "test", 0); a.Step(Tick(0, season)); a.Step(Tick(3.2, season));
            var b = Create(3, season, profile); b.Restore(Map, a.Capture(Map));
            for (int i = 0; i < 40; i++)
            {
                double hours = 3.2 + i * .2;
                a.Step(Tick(hours, season)); b.Step(Tick(hours, season));
                Equal(a, b);
                Check(a.Now.Wet == .37f, "Explicit wet override was lost.");
            }
        }
    }

    [TestCase]
    public void ShortExplicitQueueBoundariesAreContinuousAndResumeBitIdentically()
    {
        var profile = Profile;
        string[] queues =
        [
            "clear:60,cloudy:1,clear:60",
            "clear:60,few-clouds:1,cloudy:1,few-clouds:1,clear:60",
            "clear:60,cloudy:1,rain:45,after-rain:1,clear:60"
        ];
        foreach (string queue in queues)
        {
            var boot = WeatherBootOptions.Parse(new Dictionary<string, string> { ["weather-queue"] = queue }, 73, Season.Autumn);
            var items = boot.Queue!;
            double boundary = 0;
            foreach (var item in items.Take(items.Count - 1))
            {
                boundary += item.Minutes / 60;
                const double epsilon = 1e-7;
                var original = new WeatherProcess(new Source(profile));
                original.Reset(boot); original.Enter(Map, "test", 0); original.Step(Tick(0));
                original.Step(Tick(boundary - epsilon));
                var before = original.Now;
                var saved = original.Capture(Map);
                string json = JsonSerializer.Serialize(saved, TestJson.Contract.WeatherProcessState);
                var restored = Create(73, Season.Autumn, profile);
                restored.Restore(Map, JsonSerializer.Deserialize(json, TestJson.Contract.WeatherProcessState)!);
                Equal(original, restored);
                foreach (double hours in new[] { boundary, boundary + epsilon, boundary + .005, boundary + .6 })
                {
                    original.Step(Tick(hours)); restored.Step(Tick(hours));
                    Equal(original, restored);
                    if (hours <= boundary + epsilon)
                        Check(Distance(before, original.Now) < .0001f, $"Short queue hard-cut at {boundary}: {queue}");
                }
            }
        }
    }

    [TestCase]
    public void ManualForcedCaptureAndSeasonReplacementAreSettled()
    {
        var profile = Profile;
        var a = Create(9, Season.Summer, profile);
        a.Select(new(WeatherMode.Manual, WeatherKind.Thunderstorm, .8f));
        a.Step(Tick(0, Season.Summer) with { RealDt = .1 });
        Check(a.Transitioning, "Manual selection should transition.");
        a.Reseason(0, Season.Winter, 0);
        var saved = a.Capture(Map);
        Check(saved.Selection.Kind == WeatherKind.Snow && saved.Selection.Intensity == 1, "Winter storm should become heavy snow.");
        var b = Create(9, Season.Winter, profile); b.Restore(Map, saved);
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
        var saved = p.Capture(Map);
        Check(saved.LeftAtHours == 5 && saved.Selection.Kind == WeatherKind.Fog, "Forced capture must match the world landing time.");
        var restored = Create(15, Season.Autumn, Profile); restored.Restore(Map, saved);
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
        Reject(() => WeatherBootOptions.Parse(new Dictionary<string, string> { ["weather"] = "snow" }, 1, Season.Summer));
        Reject(() => WeatherBootOptions.Parse(new Dictionary<string, string> { ["weather-seed"] = "4", ["load"] = "save.sav" }, 1, Season.Autumn));
        Reject(() => WeatherBootOptions.Parse(new Dictionary<string, string> { ["weather-queue"] = "rain:3" }, 1, Season.Autumn));
        Reject(() => WeatherBootOptions.Parse(new Dictionary<string, string> { ["wet"] = "NaN" }, 1, Season.Autumn));
        p.Reset(new(4));
        p.Step(Tick(0));
        Check(p.Seed == 4 && p.Capture(Map).LeftAtHours == 0, "New-game Reset retained the previous instance's clock.");
    }

    [TestCase]
    public void RestoreBatchDoesNotOverwriteAnInactiveMapsSavedClock()
    {
        var profile = Profile;
        var a = Create(1, Season.Autumn, profile);
        a.Step(Tick(12));
        var saved = a.Capture(Map);
        a.Restore(Map, saved);
        a.Leave(Map, 80);
        Check(a.Capture(Map).LeftAtHours == 12, "Old scene unload overwrote newly restored map time.");
        a.Enter(Map, "test", 12);
        a.Leave(Map, 13);
        Check(a.Capture(Map).LeftAtHours == 13, "Normal Leave should resume after Enter.");
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
        string sa = JsonSerializer.Serialize(a.Capture(Map), TestJson.Contract.WeatherProcessState);
        string sb = JsonSerializer.Serialize(b.Capture(Map), TestJson.Contract.WeatherProcessState);
        Check(sa == sb, "Captured weather state differs.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static float Distance(WeatherVec a, WeatherVec b) => new[]
    {
        Math.Abs(a.Cloud - b.Cloud), Math.Abs(a.CloudType - b.CloudType), Math.Abs(a.Cb - b.Cb),
        Math.Abs(a.Overcast - b.Overcast), Math.Abs(a.Precip - b.Precip), Math.Abs(a.PrecipKind - b.PrecipKind),
        Math.Abs(a.Wet - b.Wet), Math.Abs(a.Puddle - b.Puddle), Math.Abs(a.Fog - b.Fog),
        Math.Abs(a.GroundFog - b.GroundFog), Math.Abs(a.Storm - b.Storm), Math.Abs(a.SnowAdd - b.SnowAdd),
        Math.Abs(a.Rainbow - b.Rainbow), Math.Abs(a.Lightning - b.Lightning)
    }.Max();
    private static void Reject(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid input was accepted.");
    }
    private sealed class Source(WeatherProfile profile) : IWeatherProfileSource { public WeatherProfile Load(string name) => profile; }
    private sealed class TestMaps : IMapService
    {
        public long Rev => 1;
        public MapId Default => Map;
        public MapId Current => Map;
        public IMapContext? Context => null;
        public IMapTransition? Transition => null;
        public IReadOnlyList<MapVisit> Visits => Array.Empty<MapVisit>();
        public event Action<MapId, MapTravelCause>? Arrived { add { } remove { } }
        public IReadOnlyList<MapDescriptor> Maps { get; } = new[]
        {
            new MapDescriptor(Map, new("测试", "Test"), MapKind.Outdoor, 1, 1, null!, default, default, null!, null!,
                default, default, "test", Array.Empty<MapSpawn>(),
                new(MapTimeRule.Shared, new(17.5, Season.Autumn, WeatherKind.Clear), new("测试", "Test"), true, "test"),
                null!, new Dictionary<string, string>(), Array.Empty<MapPortal>())
        };
        public IMapTransition Travel(MapTravelRequest request) => throw new NotSupportedException();
    }
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
