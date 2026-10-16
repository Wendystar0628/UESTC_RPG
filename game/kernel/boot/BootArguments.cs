using System.Globalization;
using Campus.Contracts.Quality;

namespace Campus.Kernel.Boot;

/// <summary>Validated application arguments. Explicit inputs remain distinguishable from capture defaults.</summary>
public sealed class BootArguments
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "gate", "map", "spawn", "pose", "clock", "season", "anim", "flow", "mode", "ui", "seed",
        "weather", "weather-intensity", "wet", "weather-seed", "weather-remaining", "weather-queue", "sunshower",
        "quality", "quality-set", "adaptive", "bench", "tier", "without", "inspect", "shots", "frames", "out",
        "gen", "bake", "bake-cache", "bake-bg", "bake-strict", "bake-verify", "title", "cine", "save", "slot",
        "load", "save-dir", "save-at", "audio", "audio-fixed", "audio-seed", "audio-limiter", "audio-log",
        "ui-theme", "motion", "ui-scale", "ui-gallery", "ui-export-theme", "amb-solo", "amb-off", "amb-debug",
        "audio-probe", "audio-out", "preset", "focus", "pose-file"
    };
    private static readonly HashSet<string> Flags = new(StringComparer.Ordinal) { "bake-verify", "audio-probe" };
    private static readonly string[] WorldKeys = ["clock", "season", "anim", "flow", "seed", "weather", "weather-intensity",
        "weather-seed", "weather-remaining", "weather-queue", "sunshower", "wet", "pose", "mode", "map", "spawn", "focus", "pose-file"];
    private readonly Dictionary<string, string> _values;
    private readonly HashSet<string> _provided;

    private BootArguments(Dictionary<string, string> values)
    {
        _values = values;
        _provided = new(values.Keys, StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, string> Values => _values;
    public IReadOnlySet<string> ExplicitArguments => _provided;
    public bool CaptureMode => _provided.Overlaps(["gate", "shots", "bench", "frames"]);
    public bool CpuBakeMode => Has("bake") || Value("bake-verify") == "1";
    public bool Title => Value("title", "1") == "1";
    public bool HasLoad => Has("slot") || Has("load");
    public bool Has(string key) => _provided.Contains(key);
    public string Value(string key, string fallback = "") => _values.GetValueOrDefault(key, fallback);

    public static BootArguments Parse(IEnumerable<string> args, bool development)
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
        if (!development && (result.Has("focus") || result.Has("pose-file"))) throw Invalid("Preview arguments require a development build.");
        if (new[] { "pose", "focus", "pose-file" }.Count(result.Has) > 1) throw Invalid("--pose, --focus and --pose-file are mutually exclusive.");
        if (result.Has("slot") && result.Has("load")) throw Invalid("--slot and --load are mutually exclusive.");
        if (result.HasLoad && WorldKeys.Any(result.Has)) throw Invalid("Saved state cannot be combined with world overrides.");
        result.Validate();
        if (result.CpuBakeMode && result._provided.Overlaps(["gate", "shots", "bench", "frames", "inspect"]))
            throw Invalid("CPU baking cannot be combined with a rendering or gate command.");
        result.AddDefaults();
        if (result.Value("pose") == "T0" && !result.Title) throw Invalid("T0 requires --title=1.");
        if (result.Has("cine") && !result.Title) throw Invalid("--cine requires --title=1.");
        if (result.Has("shots") && !result.Has("out")) throw Invalid("--shots requires an absolute --out directory.");
        return result;
    }

    private void Validate()
    {
        foreach (var key in new[] { "ui", "title", "adaptive", "bench", "bake-bg", "bake-strict", "bake-verify", "audio-fixed", "audio-limiter", "audio-log", "amb-debug" })
            OneOf(key, "0", "1");
        OneOf("season", "spring", "summer", "autumn", "winter");
        OneOf("flow", "0", "1", "10", "60", "360", "1440");
        OneOf("mode", "fly", "walk");
        OneOf("quality", "smooth", "balanced", "fine", "ultra");
        OneOf("tier", "min-spec", "dgpu-mid", "dgpu-high");
        OneOf("weather", "auto", "clear", "few-clouds", "cloudy", "overcast", "fog", "light-rain", "rain", "thunderstorm", "after-rain", "snow", "after-snow");
        OneOf("sunshower", "auto", "on", "off");
        OneOf("save", "off", "1", "2", "3");
        OneOf("slot", "1", "2", "3", "s1", "s2", "s3");
        OneOf("audio", "on", "off");
        OneOf("ui-theme", "auto", "light", "dark");
        OneOf("motion", "full", "reduced", "instant");
        OneOf("gen", "live");
        OneOf("cine", "enter_new", "enter_new_short", "enter_continue", "leave");
        Number("clock", 0, 24, exclusiveMax: true);
        Number("anim", 0, 480, exclusiveMax: true);
        Number("weather-intensity", 0, 1); Number("wet", 0, 1); Number("ui-scale", .75, 1.5);
        Number("weather-remaining", 0, double.MaxValue);
        foreach (var key in new[] { "seed", "weather-seed", "audio-seed" })
            if (Has(key) && !uint.TryParse(Value(key), NumberStyles.None, CultureInfo.InvariantCulture, out _)) throw Invalid(key);
        foreach (var key in new[] { "frames", "save-at" })
            if (Has(key) && (!int.TryParse(Value(key), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0)) throw Invalid(key);
        foreach (var key in new[] { "out", "load", "save-dir", "bake-cache", "pose-file", "audio-out", "ui-export-theme" })
            if (Has(key) && !Path.IsPathFullyQualified(Value(key))) throw Invalid($"--{key} must be absolute.");
        _ = QualityOverrides();
        if (Has("shots") && Value("shots").Split(',').Any(id => id.Length == 0 || id.Any(c => !char.IsLetterOrDigit(c) && c is not '_' and not '-')))
            throw Invalid("Invalid screenshot identifier.");
    }

    private void AddDefaults()
    {
        var mainlineGate = Value("gate") is "mainline" or "mainline-resume";
        _values.TryAdd("title", CaptureMode && !mainlineGate ? "0" : "1");
        _values.TryAdd("ui", CaptureMode ? "0" : "1");
        _values.TryAdd("audio", CaptureMode ? "off" : "on");
        if (CaptureMode)
        {
            _values.TryAdd("save", "off"); _values.TryAdd("adaptive", "0"); _values.TryAdd("bake-bg", "0");
            if (!Has("tier")) _values.TryAdd("quality", "fine");
            if (!HasLoad)
            {
                _values.TryAdd("clock", "17.5"); _values.TryAdd("season", "autumn"); _values.TryAdd("anim", "240");
                _values.TryAdd("flow", "0"); _values.TryAdd("mode", "fly"); _values.TryAdd("seed", "1");
                _values.TryAdd("weather", "clear"); _values.TryAdd("weather-seed", "1");
                if (!Title && !Has("spawn") && !Has("focus") && !Has("pose-file")) _values.TryAdd("pose", "V1");
            }
        }
        _values.TryAdd("motion", CaptureMode && Value("ui") == "1" ? "instant" : "full");
    }

    public IReadOnlyDictionary<QualityKnob, float> QualityOverrides()
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
