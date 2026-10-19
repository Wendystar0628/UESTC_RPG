using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;

namespace Campus.Style.Palette.Keys;

// Wire shapes of the five colour-script data files (SYS-TIME). The source-generated context rejects unknown members and
// missing required ones with the JSON path; names, ranges and cross-references that JSON cannot express are checked once
// after reading (ColorScriptJson.Validate). Members the runtime does not read are optional documentation.

#region Shared shapes

/// <summary>Two numbers written as [a, b] (altitude span, hue range, chroma edges).</summary>
public readonly record struct DoublePair(double First, double Second);

/// <summary>Chroma and hue (degrees) of an a-b only colour.</summary>
public sealed record ChromaHueJson(double C, double H);

/// <summary>One value per season; the four members are all required.</summary>
public sealed record PerSeason<T>(T Spring, T Summer, T Autumn, T Winter)
{
    public T For(Season s) => s switch
    {
        Season.Spring => Spring,
        Season.Summer => Summer,
        Season.Autumn => Autumn,
        Season.Winter => Winter,
        _ => throw new ArgumentOutOfRangeException(nameof(s))
    };
}

#endregion

#region autumn.keys.json

public sealed record AutumnKeysFile(string Format, double[] SkyStopsDeg, KeyJson[] Keys,
    string? Owner = null, string? Doc = null, string? Season = null, string[]? Notes = null);

public sealed record KeyJson(string Id, string Name, KeyClass Class, KeySide Side, double AltDeg, double ExampleClock,
    KeySkyJson Sky, KeyLightJson Key, KeyShadeJson Shade, KeyFogJson Fog, KeyLineJson Line, KeyColorsJson Colors,
    KeyPostJson Post, KeySkyFxJson SkyFx, KeyFogLowJson FogLow,
    string? Mood = null, int? SlotDeg = null, bool? Default = null, KeyLightsJson? Lights = null);

public sealed record KeySkyJson(Lab[] Sun, Lab[] Anti, string? Body = null, string? AntiNotes = null);

public sealed record KeyLightJson(Lab Color, double Intensity, double SatGain, double LitLift,
    double VegChroma = 1.0, string? Source = null, string? Note = null);

public sealed record KeyShadeJson(double R, ChromaHueJson AmbientSky, double WMin, double VegDepth = 1.0);

public sealed record KeyFogJson(Lab Sun, Lab Anti, double StartM, double SilhouetteM, double Density, double HazeDeg);

public sealed record KeyLineJson(double Darken, Lab Warm, Lab Cool);

public sealed record KeyColorsJson(
    [property: JsonPropertyName("bounce.ground")] Lab BounceGround,
    [property: JsonPropertyName("shaft.near")] Lab ShaftNear,
    [property: JsonPropertyName("shaft.far")] Lab ShaftFar,
    Lab Rim,
    [property: JsonPropertyName("glass.glare")] Lab GlassGlare,
    [property: JsonPropertyName("sky.cloudLit")] Lab SkyCloudLit,
    [property: JsonPropertyName("sky.cloudRim")] Lab SkyCloudRim,
    [property: JsonPropertyName("sky.cloudBelly")] Lab SkyCloudBelly,
    [property: JsonPropertyName("sky.cloudDeep")] Lab SkyCloudDeep,
    [property: JsonPropertyName("sky.moon")] Lab SkyMoon,
    [property: JsonPropertyName("interior.warm")] ChromaHueJson InteriorWarm);

public sealed record KeyPostJson(double ExposureEv, double GlowBloom, double GlowIntensity, double GlowHdrThreshold,
    double GlowHdrScale, KeyVignetteJson Vignette, KeyCdlJson Cdl);

public sealed record KeyVignetteJson(double Strength, double[] Tint);

public sealed record KeyCdlJson(double Slope, double Offset, double Power, double Saturation);

public sealed record KeySkyFxJson(double Stars, double CloudAmount);

public sealed record KeyFogLowJson(double TopM, double Density, double Lake);

/// <summary>Reference lamp values; windows and streetlamps follow the lighting schedule, not the palette.</summary>
public sealed record KeyLightsJson(double Windows, double Street, string? Note = null);

#endregion

#region derive.json

public sealed record DeriveFile(string Format, DeriveLitJson Lit, DeriveShadeJson Shade, DeriveInteriorJson Interior,
    SwatchLJson SwatchL, Lab ShadowReference, Dictionary<string, MaterialClassJson> MaterialClasses, string[] FrameTokens,
    string? Owner = null, string? Doc = null, string[]? Notes = null, DeepJson? Deep = null);

public sealed record DeriveLitJson(double AdaptRef, double TintK, double VegetationTintK, WarmCapJson VegetationWarmCap,
    HeroGuardJson HeroGuard, ChromaCapJson ChromaCap);

public sealed record WarmCapJson(double K, DoublePair AltDeg, DoublePair KeyC, double HueMax, double AlbedoChromaGain);

public sealed record HeroGuardJson(DoublePair HueRange, double MinHeroC, double WarmMirror, double ChromaFloor);

public sealed record ChromaCapJson(double Ratio, double MinAlbedoC);

public sealed record DeriveShadeJson(double ChromaMulMax, DoublePair NeutralCEdge, DoublePair YellowGuard, DoublePair GreenGuard,
    bool ScaleChromaWithLight, DoublePair? OliveZone = null, DoublePair? GreenRange = null);

public sealed record DeriveInteriorJson(double ShadeLRatio, double ShadeHueMax, double LitGain, double LitLMax, double ShaderGain);

public sealed record SwatchLJson(
    [property: JsonPropertyName("ambient.sky")] double AmbientSky,
    [property: JsonPropertyName("interior.warm")] double InteriorWarm);

/// <summary>Per-class shade rule; Steps is two, twoHalf, soft or three ("three" selects the three-step shade).</summary>
public sealed record MaterialClassJson(double RMat, [property: JsonPropertyName("kC")] double KC, double DhMax, string Steps);

public sealed record DeepJson(double LMul, double CMul);

#endregion

#region seasons.json

public sealed record SeasonsFile(string Format, double[] SkyFalloff, Dictionary<string, ScopeJson> Scopes, SeasonLimitsJson Limits,
    PerSeason<SeasonDeltaJson> Seasons, string? Owner = null, string? Doc = null, string[]? Notes = null)
{
    /// <summary>Weight of a named scope on one keyframe; unlisted keyframes weigh 0.</summary>
    public double ScopeWeight(string scope, string keyId) => Scopes[scope].Weight(keyId);
}

/// <summary>"all" (weight 1 everywhere) or per-keyframe weights.</summary>
public sealed record ScopeJson(Dictionary<string, double>? Weights)
{
    public double Weight(string keyId) => Weights is null ? 1.0 : Weights.GetValueOrDefault(keyId);
}

public sealed record SeasonLimitsJson(Dictionary<string, double> RMax, Dictionary<string, double>? ZenithMinC = null);

public sealed record SeasonDeltaJson(SeasonSkyJson Sky, KeyOpJson[] Key, SeasonShadeJson Shade, SeasonFogJson Fog,
    SeasonPostJson Post, SeasonOverrideJson[] Overrides, string? Name = null, double LampPoolGain = 1.0);

public sealed record SeasonSkyJson(ZenithDeltaJson Zenith, string Scope);

public sealed record ZenithDeltaJson(
    [property: JsonPropertyName("dL")] double DL,
    [property: JsonPropertyName("dC")] double DC,
    [property: JsonPropertyName("dh")] double Dh);

/// <summary>Key-light operation: "toward" needs Color and Amount, "chroma" needs Mul.</summary>
public sealed record KeyOpJson(string Op, string Scope, Lab? Color = null, double? Amount = null, double? Mul = null);

public sealed record SeasonShadeJson(SeasonAmbientJson AmbientSky, double Dr, string Scope);

public sealed record SeasonAmbientJson([property: JsonPropertyName("dC")] double DC, [property: JsonPropertyName("dh")] double Dh);

/// <summary>Toward is written as null when the season leaves the fog colour alone.</summary>
public sealed record SeasonFogJson(Lab? Toward, double AbAmount, [property: JsonPropertyName("dL")] double DL,
    double VisibilityMul, double HazeMul, string Scope);

public sealed record SeasonPostJson(double GlowIntensityMul);

/// <summary>Hand-tuned colours for the listed keyframes (colour names as in FrameLayout.ColorTokens).</summary>
public sealed record SeasonOverrideJson(string[] Keys, string? What = null, Dictionary<string, Lab>? Colors = null,
    SeasonOverrideSkyFxJson? SkyFx = null);

public sealed record SeasonOverrideSkyFxJson(double? CloudAmountAdd = null);

#endregion

#region vegetation.json

public sealed record VegetationFile(string Format, PerSeason<string> HeroFrames, SeasonalJson[] Seasonal, ConstantJson[] Constant,
    VariantJson[]? Variants = null, string? Owner = null, string? Doc = null, string[]? Notes = null);

public sealed record SeasonalJson(string Token, string Class, PerSeason<VegAnchor> Lit,
    string? Name = null, bool? WinterBare = null, string? Note = null, Dictionary<string, Dictionary<string, Lab>>? Check = null);

public sealed record ConstantJson(string Token, string Class, VegAnchor Lit, string HeroSeason,
    string? Name = null, string[]? Seasons = null, bool? Row0 = null);

/// <summary>Follows another token with an OKLCH offset applied after derivation.</summary>
public sealed record VariantJson(string Token, string Base, [property: JsonPropertyName("dL")] double DL,
    [property: JsonPropertyName("dh")] double Dh, string? Name = null);

/// <summary>Hero-frame lit colour (a plain colour string) or a plain albedo ({ "albedo": colour }).</summary>
public readonly record struct VegAnchor(Lab Color, bool IsAlbedo);

#endregion

#region weather.json

public sealed record WeatherFile(string Format, WeatherKeyEvJson KeyEv, WeatherShadeJson Shade, WeatherSkyJson Sky,
    double WashedSatAdd, double RainVegetationChromaMul, WeatherTokensJson Tokens,
    string? Owner = null, string? Doc = null, string? Status = null, string[]? Notes = null, string? CloudAmount = null,
    WeatherGlowJson? GlowBloomAdd = null);

public sealed record WeatherKeyEvJson(double Overcast, double HeavyRainExtra);

public sealed record WeatherShadeJson(WeatherRFloorJson RFloor, WeatherAmbientTargetJson AmbientSkyTarget);

public sealed record WeatherRFloorJson(double Base, double PerOvercast);

public sealed record WeatherAmbientTargetJson(double C, double H, string? Token = null);

public sealed record WeatherSkyJson(WeatherOvercastJson Overcast, double WashedChromaAdd,
    string? OvercastToken = null, Lab? SnowOvercast = null, double? NightRainMinZenithL = null);

public sealed record WeatherOvercastJson(double MinL, double MinC, double C, double H, double RainLDrop);

public sealed record WeatherGlowJson(double Fog, double Rain);

/// <summary>Palette tokens of the weather layer; Overcast and NightRain are OKLCH, SnowOvercast is a colour.</summary>
public sealed record WeatherTokensJson(
    [property: JsonPropertyName("weather.overcast")] LchTokenJson Overcast,
    [property: JsonPropertyName("weather.snowOvercast")] Lab SnowOvercast,
    [property: JsonPropertyName("weather.nightRain")] LchTokenJson NightRain,
    [property: JsonPropertyName("weather.cbBase")] Lab? CbBase = null,
    [property: JsonPropertyName("weather.fog")] Lab? Fog = null,
    [property: JsonPropertyName("weather.washedSky")] string? WashedSky = null);

public sealed record LchTokenJson(double L, double C, double H, string? Note = null);

#endregion

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AutumnKeysFile))]
[JsonSerializable(typeof(DeriveFile))]
[JsonSerializable(typeof(SeasonsFile))]
[JsonSerializable(typeof(VegetationFile))]
[JsonSerializable(typeof(WeatherFile))]
internal partial class ColorScriptJsonContext : JsonSerializerContext;

/// <summary>Strict reader and post-read checks of the colour-script data files.</summary>
internal static class ColorScriptJson
{
    private static readonly ColorScriptJsonContext Context = CreateContext();

    /// <summary>Values of MaterialClassJson.Steps.</summary>
    private static readonly string[] ShadeSteps = ["two", "twoHalf", "soft", "three"];

    private static ColorScriptJsonContext CreateContext()
    {
        var options = new JsonSerializerOptions
        {
            // 传了运行时选项，特性上的命名策略不再生效，这里要再写一次
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectRequiredConstructorParameters = true,
            RespectNullableAnnotations = true,
            AllowOutOfOrderMetadataProperties = false,
            NumberHandling = JsonNumberHandling.Strict,
            MaxDepth = 32
        };
        options.Converters.Add(new HexLabConverter());
        options.Converters.Add(new DoublePairConverter());
        options.Converters.Add(new VegAnchorConverter());
        options.Converters.Add(new ScopeConverter());
        options.Converters.Add(new JsonStringEnumConverter<KeyClass>(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.Converters.Add(new JsonStringEnumConverter<KeySide>(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return new(options);
    }

    public static AutumnKeysFile Keys(byte[] utf8) => Read(utf8, "autumn.keys.json", Context.AutumnKeysFile, "campus.palette.keys/1", f => f.Format);
    public static DeriveFile Derive(byte[] utf8) => Read(utf8, "derive.json", Context.DeriveFile, "campus.palette.derive/1", f => f.Format);
    public static SeasonsFile Seasons(byte[] utf8) => Read(utf8, "seasons.json", Context.SeasonsFile, "campus.palette.seasons/1", f => f.Format);
    public static VegetationFile Vegetation(byte[] utf8) => Read(utf8, "vegetation.json", Context.VegetationFile, "campus.palette.vegetation/1", f => f.Format);
    public static WeatherFile Weather(byte[] utf8) => Read(utf8, "weather.json", Context.WeatherFile, "campus.palette.weather/1", f => f.Format);

    private static T Read<T>(byte[] utf8, string file, JsonTypeInfo<T> info, string format, Func<T, string> formatOf) where T : class
    {
        T value;
        try { value = JsonSerializer.Deserialize(utf8, info) ?? throw new InvalidDataException("the document is null."); }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            var path = e is JsonException { Path: { } p } && !e.Message.Contains(p, StringComparison.Ordinal) ? $" (at {p})" : "";
            throw new InvalidDataException($"{file}: {e.Message}{path}", e);
        }
        if (formatOf(value) != format) throw new InvalidDataException($"{file}: $.format must be '{format}', got '{formatOf(value)}'.");
        return value;
    }

    /// <summary>Names, lengths and cross-references that the typed shapes cannot express. Messages carry the JSON path.</summary>
    public static void Validate(AutumnKeysFile keys, DeriveFile derive, SeasonsFile seasons, VegetationFile vegetation)
    {
        if (keys.Keys.Length != 12) throw new InvalidDataException($"autumn.keys.json: $.keys must hold 12 keyframes, got {keys.Keys.Length}.");
        if (keys.SkyStopsDeg.Length != 5 || keys.SkyStopsDeg[0] != 0 || keys.SkyStopsDeg[4] != 90)
            throw new InvalidDataException("autumn.keys.json: $.skyStopsDeg must be five elevations from 0 to 90.");
        for (var i = 0; i < keys.Keys.Length; i++)
        {
            var k = keys.Keys[i];
            Length("autumn.keys.json", $"$.keys[{i}].sky.sun", k.Sky.Sun.Length, 5);
            Length("autumn.keys.json", $"$.keys[{i}].sky.anti", k.Sky.Anti.Length, 5);
            Length("autumn.keys.json", $"$.keys[{i}].post.vignette.tint", k.Post.Vignette.Tint.Length, 3);
        }
        var ids = keys.Keys.Select(k => k.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, cls) in derive.MaterialClasses)
            if (!ShadeSteps.Contains(cls.Steps)) throw new InvalidDataException($"derive.json: $.materialClasses.{name}.steps '{cls.Steps}' is not one of {string.Join(", ", ShadeSteps)}.");

        Length("seasons.json", "$.skyFalloff", seasons.SkyFalloff.Length, 5);
        foreach (var (name, scope) in seasons.Scopes)
            foreach (var id in scope.Weights?.Keys ?? Enumerable.Empty<string>())
                Known("seasons.json", $"$.scopes.{name}.{id}", id, ids, "keyframe");
        foreach (var id in seasons.Limits.ZenithMinC?.Keys ?? Enumerable.Empty<string>())
            Known("seasons.json", $"$.limits.zenithMinC.{id}", id, ids, "keyframe");
        foreach (var season in Enum.GetValues<Season>())
        {
            var s = seasons.Seasons.For(season);
            var at = $"$.seasons.{season.ToString().ToLowerInvariant()}";
            Scope(seasons, $"{at}.sky.scope", s.Sky.Scope);
            Scope(seasons, $"{at}.shade.scope", s.Shade.Scope);
            Scope(seasons, $"{at}.fog.scope", s.Fog.Scope);
            for (var i = 0; i < s.Key.Length; i++)
            {
                var op = s.Key[i];
                var opAt = $"{at}.key[{i}]";
                Scope(seasons, $"{opAt}.scope", op.Scope);
                switch (op.Op)
                {
                    case "toward" when op.Color is null || op.Amount is null:
                        throw new InvalidDataException($"seasons.json: {opAt} 'toward' needs color and amount.");
                    case "chroma" when op.Mul is null:
                        throw new InvalidDataException($"seasons.json: {opAt} 'chroma' needs mul.");
                    case "toward" or "chroma":
                        break;
                    default:
                        throw new InvalidDataException($"seasons.json: {opAt}.op '{op.Op}' is not toward or chroma.");
                }
            }
            for (var i = 0; i < s.Overrides.Length; i++)
            {
                var ov = s.Overrides[i];
                foreach (var id in ov.Keys) Known("seasons.json", $"{at}.overrides[{i}].keys", id, ids, "keyframe");
                foreach (var token in ov.Colors?.Keys ?? Enumerable.Empty<string>())
                    if (Array.FindIndex(FrameLayout.ColorTokens, t => t.Token == token) < 0)
                        throw new InvalidDataException($"seasons.json: {at}.overrides[{i}].colors.{token} is not a frame colour name.");
            }
        }

        foreach (var season in Enum.GetValues<Season>())
            Known("vegetation.json", $"$.heroFrames.{season.ToString().ToLowerInvariant()}", vegetation.HeroFrames.For(season), ids, "keyframe");
        for (var i = 0; i < vegetation.Constant.Length; i++)
            if (!Enum.TryParse<Season>(vegetation.Constant[i].HeroSeason, true, out _))
                throw new InvalidDataException($"vegetation.json: $.constant[{i}].heroSeason '{vegetation.Constant[i].HeroSeason}' is not a season.");
        for (var i = 0; i < vegetation.Seasonal.Length; i++)
            Class(derive, $"$.seasonal[{i}].class", vegetation.Seasonal[i].Class);
        for (var i = 0; i < vegetation.Constant.Length; i++)
            Class(derive, $"$.constant[{i}].class", vegetation.Constant[i].Class);
    }

    private static void Length(string file, string path, int actual, int expected)
    {
        if (actual != expected) throw new InvalidDataException($"{file}: {path} must have {expected} entries, got {actual}.");
    }

    private static void Known(string file, string path, string id, HashSet<string> known, string what)
    {
        if (!known.Contains(id)) throw new InvalidDataException($"{file}: {path} names unknown {what} '{id}'.");
    }

    private static void Scope(SeasonsFile seasons, string path, string scope)
    {
        if (!seasons.Scopes.ContainsKey(scope)) throw new InvalidDataException($"seasons.json: {path} names unknown scope '{scope}'.");
    }

    private static void Class(DeriveFile derive, string path, string cls)
    {
        if (!derive.MaterialClasses.ContainsKey(cls)) throw new InvalidDataException($"vegetation.json: {path} names unknown material class '{cls}'.");
    }

    #region Converters

    private static double Number(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected a number.");
        return reader.GetDouble();
    }

    private static Lab Hex(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected a #RRGGBB colour string.");
        try { return Oklab.Parse(reader.GetString()!); }
        catch (FormatException e) { throw new JsonException(e.Message, e); }
    }

    private sealed class HexLabConverter : JsonConverter<Lab>
    {
        public override Lab Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => Hex(ref reader);
        public override void Write(Utf8JsonWriter writer, Lab value, JsonSerializerOptions options) => throw new NotSupportedException();
    }

    private sealed class DoublePairConverter : JsonConverter<DoublePair>
    {
        public override DoublePair Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected a [a, b] number pair.");
            reader.Read();
            var first = Number(ref reader);
            reader.Read();
            var second = Number(ref reader);
            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Expected exactly two numbers.");
            return new(first, second);
        }

        public override void Write(Utf8JsonWriter writer, DoublePair value, JsonSerializerOptions options) => throw new NotSupportedException();
    }

    private sealed class VegAnchorConverter : JsonConverter<VegAnchor>
    {
        public override VegAnchor Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String) return new(Hex(ref reader), false);
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected a colour string or { \"albedo\": colour }.");
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != "albedo")
                throw new JsonException("A vegetation anchor object has exactly one member, albedo.");
            reader.Read();
            var albedo = Hex(ref reader);
            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject) throw new JsonException("A vegetation anchor object has exactly one member, albedo.");
            return new(albedo, true);
        }

        public override void Write(Utf8JsonWriter writer, VegAnchor value, JsonSerializerOptions options) => throw new NotSupportedException();
    }

    private sealed class ScopeConverter : JsonConverter<ScopeJson>
    {
        public override ScopeJson Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
                return reader.GetString() == "all" ? new(null) : throw new JsonException("A scope string must be \"all\".");
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected \"all\" or an object of keyframe weights.");
            var weights = new Dictionary<string, double>(StringComparer.Ordinal);
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var id = reader.GetString()!;
                reader.Read();
                weights[id] = Number(ref reader);
            }
            return new(weights);
        }

        public override void Write(Utf8JsonWriter writer, ScopeJson value, JsonSerializerOptions options) => throw new NotSupportedException();
    }

    #endregion
}
