using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;

namespace Campus.Kernel.Boot;

/// <summary>Validated application arguments. Explicit inputs remain distinguishable from capture defaults.</summary>
public sealed class BootArguments
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "gate", "map", "spawn", "pose", "clock", "season", "anim", "flow", "mode", "ui", "seed",
        "weather", "weather-intensity", "wet", "weather-seed", "weather-remaining", "sunshower",
        "quality", "quality-set", "bench", "without", "inspect", "shots", "frames", "out",
        "gen", "bake", "bake-cache", "bake-bg", "bake-strict", "title", "save", "slot",
        "load", "save-dir", "save-at", "audio", "audio-fixed", "audio-seed", "audio-limiter", "audio-log",
        "ui-theme", "motion", "ui-scale", "ui-gallery", "ui-export-theme", "amb-solo", "amb-off", "amb-debug",
        "audio-probe", "audio-out", "preset", "travel-sequence", "movie-frames", "movie-events"
    };
    private static readonly HashSet<string> Flags = new(StringComparer.Ordinal) { "audio-probe" };
    private static readonly string[] WorldKeys = ["clock", "season", "anim", "flow", "seed", "weather", "weather-intensity",
        "weather-seed", "weather-remaining", "sunshower", "wet", "pose", "mode", "map", "spawn", "movie-events"];
    private readonly Dictionary<string, string> _values;
    private readonly HashSet<string> _provided;
    private IReadOnlyDictionary<QualityKnob, float> _qualityOverrides = new Dictionary<QualityKnob, float>();

    private BootArguments(Dictionary<string, string> values)
    {
        _values = values;
        _provided = new(values.Keys, StringComparer.Ordinal);
    }

    private bool CaptureMode => _provided.Overlaps(["gate", "shots", "bench", "frames", "movie-frames"]);
    private bool CpuBakeMode => Has("bake");
    private bool Title => Value("title", "1") == "1";
    private bool HasLoad => Has("slot") || Has("load");
    private bool Has(string key) => _provided.Contains(key);
    private string Value(string key, string absentValue = "") => _values.GetValueOrDefault(key, absentValue);

    public static BootRunOptions Parse(IEnumerable<string> args, bool development) => Parse(args, development, out _);

    internal static BootRunOptions Parse(IEnumerable<string> args, bool development, out IReadOnlyList<FixedShot> fixedShots)
    {
        var arguments = args.ToArray();
        fixedShots = FixedShotPlan.Read(arguments, development);
        if (fixedShots.Count == 0) return ParseSingle(arguments, development);
        var first = fixedShots[0].Options;
        return first with { Capture = first.Capture with { Shots = fixedShots.Select(shot => shot.Name).ToArray() } };
    }

    internal static BootRunOptions ParseSingle(IEnumerable<string> args, bool development)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal)) throw Invalid(arg);
            var pair = arg[2..].Split('=', 2);
            if (!Allowed.Contains(pair[0]) || (pair.Length == 1 && !Flags.Contains(pair[0])) ||
                (pair.Length == 2 && string.IsNullOrWhiteSpace(pair[1])) ||
                !values.TryAdd(pair[0], pair.Length == 2 ? pair[1] : "1")) throw Invalid(arg);
        }
        var result = new BootArguments(values);
        if (result.Has("without"))
            throw Invalid("NotSupported: --without module filtering is not implemented; no modules have been excluded.");
        if (result.Has("slot") && result.Has("load")) throw Invalid("--slot and --load are mutually exclusive.");
        if (result.HasLoad && WorldKeys.Any(result.Has)) throw Invalid("Saved state cannot be combined with world overrides.");
        result.Validate();
        if (result.Has("travel-sequence") && (result.CpuBakeMode || result.CaptureMode || result.Has("inspect")))
            throw Invalid("--travel-sequence cannot be combined with a bake, capture or inspect command.");
        if (result.CpuBakeMode && result._provided.Overlaps(["gate", "shots", "bench", "frames", "inspect"]))
            throw Invalid("CPU baking cannot be combined with a rendering or gate command.");
        result.AddDefaults();
        if (result.Value("pose") == "T0" && !result.Title) throw Invalid("T0 requires --title=1.");
        if (result.Has("shots") && !result.Has("out")) throw Invalid("--shots requires an absolute --out directory.");
        if (result.Has("movie-events") && !result.Has("movie-frames"))
            throw Invalid("--movie-events requires --movie-frames.");
        if (result.Has("movie-frames") && (!development || !result.Has("shots") || result.HasLoad || result.Title ||
            result._provided.Overlaps(["frames", "gate", "bench", "inspect", "travel-sequence", "bake"]) ||
            result.Value("flow") != "0" || result.Value("mode") != "fly"))
            throw Invalid("Movie capture requires development fixed shots in paused fly mode and no other run command.");
        return result.Resolve();
    }

    private void Validate()
    {
        foreach (var key in new[] { "ui", "title", "bench", "bake-bg", "bake-strict", "audio-fixed", "audio-limiter", "audio-log", "amb-debug" })
            OneOf(key, "0", "1");
        OneOf("season", "spring", "summer", "autumn", "winter");
        OneOf("flow", "0", "1", "10", "60", "360", "1440");
        OneOf("mode", "fly", "walk");
        OneOf("quality", "balanced", "fine", "ultra");
        OneOf("weather", "auto", "clear", "few-clouds", "cloudy", "overcast", "fog", "light-rain", "rain", "thunderstorm", "after-rain", "snow", "after-snow");
        OneOf("sunshower", "auto", "on", "off");
        OneOf("save", "off", "1", "2", "3");
        OneOf("slot", "1", "2", "3", "s1", "s2", "s3");
        OneOf("audio", "on", "off");
        OneOf("ui-theme", "auto", "light", "dark");
        OneOf("motion", "full", "reduced", "instant");
        OneOf("gen", "live");
        Number("clock", 0, 24, exclusiveMax: true);
        Number("anim", 0, WorldTime.AnimLoopSeconds, exclusiveMax: true);
        Number("weather-intensity", 0, 1); Number("wet", 0, 1);
        var uiScale = UiSettingDefinitions.Scale.Range!;
        Number("ui-scale", uiScale.Min, uiScale.Max);
        Number("weather-remaining", 0, double.MaxValue);
        foreach (var key in new[] { "seed", "weather-seed", "audio-seed" })
            if (Has(key) && !uint.TryParse(Value(key), NumberStyles.None, CultureInfo.InvariantCulture, out _)) throw Invalid(key);
        foreach (var key in new[] { "frames", "save-at", "movie-frames" })
            if (Has(key) && (!int.TryParse(Value(key), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0)) throw Invalid(key);
        foreach (var key in new[] { "out", "load", "save-dir", "bake-cache", "audio-out", "ui-export-theme" })
            if (Has(key) && !Path.IsPathFullyQualified(Value(key))) throw Invalid($"--{key} must be absolute.");
        _qualityOverrides = QualityOverrides();
        if (Has("shots") && Value("shots").Split(',').Any(id => id.Length == 0 || id.Any(c => !char.IsLetterOrDigit(c) && c is not '_' and not '-')))
            throw Invalid("Invalid screenshot identifier.");
        if (Has("travel-sequence") && Value("travel-sequence").Split(',').Any(id => id.Length == 0 || id.Any(char.IsWhiteSpace)))
            throw Invalid("Invalid travel sequence marker identifier.");
    }

    private void AddDefaults()
    {
        _values.TryAdd("title", CaptureMode ? "0" : "1");
        _values.TryAdd("ui", CaptureMode ? "0" : "1");
        _values.TryAdd("audio", CaptureMode ? "off" : "on");
        if (CaptureMode)
        {
            _values.TryAdd("save", "off"); _values.TryAdd("bake-bg", "0");
            _values.TryAdd("quality", "fine");
            if (!HasLoad)
            {
                _values.TryAdd("clock", "17.5"); _values.TryAdd("season", "autumn"); _values.TryAdd("anim", WorldTime.InitialAnimTime.ToString(CultureInfo.InvariantCulture));
                _values.TryAdd("flow", "0"); _values.TryAdd("mode", "fly"); _values.TryAdd("seed", "1");
                _values.TryAdd("weather", "clear"); _values.TryAdd("weather-seed", "1");
                if (!Title && !Has("spawn")) _values.TryAdd("pose", "V1");
            }
        }
        _values.TryAdd("motion", CaptureMode && Value("ui") == "1" ? "instant" : "full");
    }

    private IReadOnlyDictionary<QualityKnob, float> QualityOverrides()
    {
        var result = new Dictionary<QualityKnob, float>();
        if (!Has("quality-set")) return result;
        foreach (var item in Value("quality-set").Split(','))
        {
            var pair = item.Split('=', 2);
            var names = Enum.GetValues<QualityKnob>().Where(k => Snake(k.ToString()) == pair[0]).ToArray();
            if (pair.Length != 2 || names.Length != 1 || !float.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                !float.IsFinite(value) || !result.TryAdd(names[0], value)) throw Invalid("quality-set=" + item);
        }
        return result;
    }

    private BootRunOptions Resolve()
    {
        string? Text(string key) => _values.GetValueOrDefault(key);
        string? Explicit(string key) => Has(key) ? Text(key) : null;
        double? NumberValue(string key) => Text(key) is { } value ? double.Parse(value, CultureInfo.InvariantCulture) : null;
        uint? Seed(string key, bool explicitOnly = false) => (explicitOnly ? Explicit(key) : Text(key)) is { } value
            ? uint.Parse(value, CultureInfo.InvariantCulture) : null;
        SlotId? Slot(string? value) => value is null or "off" ? null : new SlotId("s" + value.TrimStart('s'));
        var weather = Text("weather") is { } weatherText ? ParseWeather(weatherText) : null;
        return new BootRunOptions
        {
            Mode = CpuBakeMode ? BootRunMode.CpuBake : CaptureMode ? BootRunMode.Capture : BootRunMode.Interactive,
            Automated = CaptureMode || Has("inspect"),
            FreezeAnimation = Has("shots") && !Has("bench") && !Has("movie-frames"),
            Title = Title,
            SaveDirectory = Text("save-dir"),
            TravelSequence = Text("travel-sequence")?.Split(',') ?? [],
            Flow = new BootFlowRequest(ShowTitle: Title,
                Slot: Slot(Text("slot")), LoadFile: Text("load"),
                Map: Text("map") is { } map ? new MapId(map) : null, Spawn: Text("spawn"),
                Seed: Seed("weather-seed") ?? Seed("seed")),
            World = new WorldBootOptions
            {
                Clock = NumberValue("clock"),
                Season = Text("season") is { } season ? Enum.Parse<Season>(season, true) : null,
                AnimationTime = NumberValue("anim"), Seed = Seed("seed"),
                FlowRate = NumberValue("flow") is { } flow ? (int)flow : null,
                ExplicitFlow = Has("flow"), ExplicitViewMode = Has("mode"),
                ViewMode = Text("mode") switch { null => null, "fly" => ViewMode.Fly, "walk" => ViewMode.Walk, _ => throw Invalid("mode") },
                Pose = Text("pose"),
                Weather = new(Seed("weather-seed"), weather, (float?)NumberValue("weather-intensity"),
                    (float?)NumberValue("wet"), NumberValue("weather-remaining"),
                    new[] { "weather", "weather-intensity", "weather-remaining", "wet" }.Any(_values.ContainsKey))
            },
            Ui = new(Explicit("ui") is { } ui ? ui == "1" : null, (float?)NumberValue("ui-scale"),
                Explicit("ui-gallery"), Explicit("ui-export-theme"),
                Explicit("motion") switch { null => null, "full" => MotionLevel.Full, "reduced" => MotionLevel.Reduced, "instant" => MotionLevel.Instant, _ => throw Invalid("motion") },
                Explicit("ui-theme") switch { null => null, "auto" => UiThemeMode.Auto, "light" => UiThemeMode.Light, "dark" => UiThemeMode.Dark, _ => throw Invalid("ui-theme") }),
            Audio = new(Value("audio") == "on", CaptureMode || Value("audio-fixed", "0") == "1", Value("audio-limiter", "1") != "0"),
            Save = new(Value("save") != "off" && (!Has("load") || Has("save") && Value("save") != "off"),
                Slot(Text("save")), NumberValue("save-at") is { } at ? (int)at : null, Seed("weather-seed")),
            Bake = new(Explicit("bake-cache"), Explicit("gen") == "live", Explicit("bake-strict") == "1",
                Explicit("bake-bg") != "0", Explicit("bake") == "map", Seed("seed", explicitOnly: true)),
            Capture = new()
            {
                Gate = Text("gate"), Inspect = Text("inspect"), Output = Text("out"),
                Shots = Text("shots")?.Split(',') ?? [],
                FrameLimit = NumberValue("frames") is { } frames ? (int)frames : null,
                MovieFrames = NumberValue("movie-frames") is { } movieFrames ? (int)movieFrames : null,
                MovieEvents = MovieEvents(),
                IncludeCanvas = Value("ui") == "1"
            },
            Quality = new()
            {
                Preset = Text("quality") is { } quality ? Enum.Parse<QualityPreset>(quality, true) : null,
                Overrides = _qualityOverrides
            }
        };
    }

    private IReadOnlyList<CaptureMovieEvent> MovieEvents()
    {
        if (!Has("movie-events")) return [];
        var limit = int.Parse(Value("movie-frames"), CultureInfo.InvariantCulture);
        var events = new List<CaptureMovieEvent>();
        foreach (var item in Value("movie-events").Split(';'))
        {
            var parts = item.Split(':', 2);
            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var frame) ||
                frame < 0 || frame >= limit || events.Count > 0 && frame <= events[^1].Frame)
                throw Invalid("Movie event frames must be strictly increasing and inside --movie-frames.");
            Season? season = null;
            double? clock = null;
            WeatherKind? weather = null;
            float? intensity = null;
            var automatic = false;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in parts[1].Split(','))
            {
                var pair = field.Split('=', 2);
                if (pair.Length != 2 || string.IsNullOrWhiteSpace(pair[1]) || !keys.Add(pair[0]))
                    throw Invalid("Invalid or duplicate movie event field: " + field);
                switch (pair[0])
                {
                    case "season":
                        season = pair[1] switch
                        {
                            "spring" => Season.Spring, "summer" => Season.Summer,
                            "autumn" => Season.Autumn, "winter" => Season.Winter,
                            _ => throw Invalid("movie-events season=" + pair[1])
                        };
                        break;
                    case "clock":
                        if (!double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var hours) ||
                            !double.IsFinite(hours) || hours < 0 || hours >= 24) throw Invalid(field);
                        clock = hours;
                        break;
                    case "weather":
                        weather = ParseWeather(pair[1]);
                        automatic = pair[1] == "auto";
                        break;
                    case "intensity":
                        if (!float.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount) ||
                            !float.IsFinite(amount) || amount < 0 || amount > 1) throw Invalid(field);
                        intensity = amount;
                        break;
                    default: throw Invalid("Unknown movie event field: " + pair[0]);
                }
            }
            if (season is null && clock is null && weather is null && !automatic ||
                intensity is not null && weather is null)
                throw Invalid("Movie events require a world change; intensity requires a manual weather choice.");
            events.Add(new(frame, season, clock, weather, automatic, intensity));
        }
        return events;
    }

    private static WeatherKind? ParseWeather(string value) => value switch
    {
        "auto" => null,
        "clear" => WeatherKind.Clear, "few-clouds" => WeatherKind.FewClouds,
        "cloudy" => WeatherKind.Cloudy, "overcast" => WeatherKind.Overcast,
        "fog" => WeatherKind.Fog, "light-rain" => WeatherKind.LightRain,
        "rain" => WeatherKind.Rain, "thunderstorm" => WeatherKind.Thunderstorm,
        "after-rain" => WeatherKind.AfterRain, "snow" => WeatherKind.Snow,
        "after-snow" => WeatherKind.AfterSnow, _ => throw Invalid("weather=" + value)
    };

    private static string Snake(string value) => string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) &&
        (!char.IsUpper(value[i - 1]) || i + 1 < value.Length && char.IsLower(value[i + 1])) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    private void OneOf(string key, params string[] allowed)
    { if (Has(key) && !allowed.Contains(Value(key), StringComparer.Ordinal)) throw Invalid(key + "=" + Value(key)); }
    private void Number(string key, double min, double max, bool exclusiveMax = false)
    {
        if (!Has(key)) return;
        if (!double.TryParse(Value(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || !double.IsFinite(n) || n < min || n > max || exclusiveMax && n == max)
            throw Invalid(key + "=" + Value(key));
    }
    private static ArgumentException Invalid(string value) => new("Invalid application argument: " + value);
}
