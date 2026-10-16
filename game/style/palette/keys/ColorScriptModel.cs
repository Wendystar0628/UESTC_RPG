using System.Text.Json;

namespace Campus.Style.Palette.Keys;

public enum KeySide : byte { Nadir, Dawn, Noon, Dusk }
public enum KeyClass : byte { Day, Default, LowSun, Twilight, Night }

/// <summary>
/// Flat vector of one colour-script frame (keyframe or interpolated). Colours are OKLab triples,
/// ab-only colours (ambient.sky, interior.warm) are (a, b) pairs, the rest are scalars.
/// Layout is fixed; interpolation and season deltas address components by index.
/// </summary>
public static class FrameLayout
{
    // Colour triples: index of L; a = +1, b = +2.
    public const int SkySun = 0;            // 5 triples, horizon → zenith
    public const int SkyAnti = 15;          // 5 triples
    public const int FogSun = 30, FogAnti = 33, Key = 36, LineWarm = 39, LineCool = 42;
    public const int Bounce = 45, ShaftNear = 48, ShaftFar = 51, Rim = 54, Glare = 57;
    public const int CloudLit = 60, CloudRim = 63, CloudBelly = 66, CloudDeep = 69, Moon = 72;
    public const int ColorEnd = 75;
    // ab pairs.
    public const int AmbientA = 75, AmbientB = 76, InteriorA = 77, InteriorB = 78;
    // Scalars.
    public const int Intensity = 79, SatGain = 80, LitLift = 81, R = 82, WMin = 83, LineDarken = 84;
    public const int FogDensity = 85, HazeDeg = 86, FogStart = 87, FogSilhouette = 88;
    public const int FogLowTop = 89, FogLowDensity = 90, FogLake = 91;
    public const int Stars = 92, CloudAmount = 93;
    public const int ExposureEv = 94, GlowBloom = 95, GlowIntensity = 96, GlowHdrThreshold = 97, GlowHdrScale = 98;
    public const int Vignette = 99, VignetteTint = 100; // 3 components
    public const int CdlSlope = 103, CdlOffset = 104, CdlPower = 105, CdlSaturation = 106;
    public const int LampPoolGain = 107;
    public const int VegChroma = 108;      // key.vegChroma：植被受光与影的彩度倍率（缺省 1），与雨天的倍率相乘
    public const int VegDepth = 109;       // shade.vegDepth：三阶类（植被、花）影的明度差倍率（缺省 1）
    public const int Count = 110;

    /// <summary>Scalars interpolated in log2 (SYS-TIME §3.1: light intensity and fog density in the log domain).</summary>
    public static bool IsLog(int i) => i is Intensity or FogDensity or FogStart or FogSilhouette;

    /// <summary>The colour triples that form the sky bands (hue swings > 60° route through magenta).</summary>
    public static bool IsSky(int colorIndex) => colorIndex < FogSun;

    public static readonly (string Token, int Index)[] ColorTokens =
    [
        ("bounce.ground", Bounce), ("shaft.near", ShaftNear), ("shaft.far", ShaftFar), ("rim", Rim), ("glass.glare", Glare),
        ("sky.cloudLit", CloudLit), ("sky.cloudRim", CloudRim), ("sky.cloudBelly", CloudBelly), ("sky.cloudDeep", CloudDeep),
        ("sky.moon", Moon)
    ];
}

/// <summary>One frame of the colour script. Values are copied, never shared between frames.</summary>
public sealed class FrameVector
{
    public readonly double[] V = new double[FrameLayout.Count];
    public FrameVector Clone()
    {
        var f = new FrameVector();
        Array.Copy(V, f.V, V.Length);
        return f;
    }
    public Lab Color(int index) => new(V[index], V[index + 1], V[index + 2]);
    public void SetColor(int index, Lab c) { V[index] = c.L; V[index + 1] = c.A; V[index + 2] = c.B; }
    public Lab Sky(bool sunSide, int band) => Color((sunSide ? FrameLayout.SkySun : FrameLayout.SkyAnti) + 3 * band);
    public (double C, double H) Ambient => AbToCh(V[FrameLayout.AmbientA], V[FrameLayout.AmbientB]);
    public (double C, double H) InteriorWarm => AbToCh(V[FrameLayout.InteriorA], V[FrameLayout.InteriorB]);
    private static (double, double) AbToCh(double a, double b)
    {
        var c = Math.Sqrt(a * a + b * b);
        return (c, c > 1e-12 ? Lab.Mod360(Math.Atan2(b, a) * 180.0 / Math.PI) : 0.0);
    }
}

/// <summary>Autumn base keyframe (SYS-TIME §3.2) with its altitude anchor.</summary>
public sealed record KeyframeInfo(string Id, string Name, KeyClass Class, KeySide Side, double AltDeg, double ExampleClock);

public sealed class ColorScriptData
{
    public required IReadOnlyList<KeyframeInfo> Keys { get; init; }
    public required IReadOnlyList<FrameVector> AutumnFrames { get; init; }
    public required double[] SkyStopsDeg { get; init; }
    public required JsonElement Derive { get; init; }
    public required JsonElement Seasons { get; init; }
    public required JsonElement Vegetation { get; init; }
    public required JsonElement Weather { get; init; }
    public required IReadOnlyList<string> FrameTokens { get; init; }

    /// <summary>Parses the five data files of game/style/palette/keys (UTF-8 JSON bytes).</summary>
    public static ColorScriptData Load(byte[] autumn, byte[] derive, byte[] seasons, byte[] vegetation, byte[] weather)
    {
        var autumnDoc = JsonDocument.Parse(autumn).RootElement.Clone();
        var keys = new List<KeyframeInfo>();
        var frames = new List<FrameVector>();
        foreach (var k in autumnDoc.GetProperty("keys").EnumerateArray())
        {
            keys.Add(new KeyframeInfo(
                k.GetProperty("id").GetString()!,
                k.GetProperty("name").GetString()!,
                ParseClass(k.GetProperty("class").GetString()!),
                ParseSide(k.GetProperty("side").GetString()!),
                k.GetProperty("altDeg").GetDouble(),
                k.GetProperty("exampleClock").GetDouble()));
            frames.Add(ParseFrame(k));
        }
        if (keys.Count != 12) throw new InvalidDataException($"Expected 12 autumn keyframes, got {keys.Count}.");
        var stops = autumnDoc.GetProperty("skyStopsDeg").EnumerateArray().Select(e => e.GetDouble()).ToArray();
        if (stops.Length != 5 || stops[0] != 0 || stops[4] != 90) throw new InvalidDataException("skyStopsDeg must be five elevations from 0 to 90.");
        var deriveDoc = JsonDocument.Parse(derive).RootElement.Clone();
        return new ColorScriptData
        {
            Keys = keys,
            AutumnFrames = frames,
            SkyStopsDeg = stops,
            Derive = deriveDoc,
            Seasons = JsonDocument.Parse(seasons).RootElement.Clone(),
            Vegetation = JsonDocument.Parse(vegetation).RootElement.Clone(),
            Weather = JsonDocument.Parse(weather).RootElement.Clone(),
            FrameTokens = deriveDoc.GetProperty("frameTokens").EnumerateArray().Select(e => e.GetString()!).ToArray()
        };
    }

    private static KeyClass ParseClass(string s) => s switch
    {
        "day" => KeyClass.Day,
        "default" => KeyClass.Default,
        "lowSun" => KeyClass.LowSun,
        "twilight" => KeyClass.Twilight,
        "night" => KeyClass.Night,
        _ => throw new InvalidDataException($"Unknown keyframe class '{s}'.")
    };

    private static KeySide ParseSide(string s) => s switch
    {
        "nadir" => KeySide.Nadir,
        "dawn" => KeySide.Dawn,
        "noon" => KeySide.Noon,
        "dusk" => KeySide.Dusk,
        _ => throw new InvalidDataException($"Unknown keyframe side '{s}'.")
    };

    private static FrameVector ParseFrame(JsonElement k)
    {
        var f = new FrameVector();
        var v = f.V;
        var sky = k.GetProperty("sky");
        var i = 0;
        foreach (var c in sky.GetProperty("sun").EnumerateArray()) f.SetColor(FrameLayout.SkySun + 3 * i++, Oklab.Parse(c.GetString()!));
        i = 0;
        foreach (var c in sky.GetProperty("anti").EnumerateArray()) f.SetColor(FrameLayout.SkyAnti + 3 * i++, Oklab.Parse(c.GetString()!));
        var fog = k.GetProperty("fog");
        f.SetColor(FrameLayout.FogSun, Oklab.Parse(fog.GetProperty("sun").GetString()!));
        f.SetColor(FrameLayout.FogAnti, Oklab.Parse(fog.GetProperty("anti").GetString()!));
        v[FrameLayout.FogStart] = fog.GetProperty("startM").GetDouble();
        v[FrameLayout.FogSilhouette] = fog.GetProperty("silhouetteM").GetDouble();
        v[FrameLayout.FogDensity] = fog.GetProperty("density").GetDouble();
        v[FrameLayout.HazeDeg] = fog.GetProperty("hazeDeg").GetDouble();
        var low = k.GetProperty("fogLow");
        v[FrameLayout.FogLowTop] = low.GetProperty("topM").GetDouble();
        v[FrameLayout.FogLowDensity] = low.GetProperty("density").GetDouble();
        v[FrameLayout.FogLake] = low.GetProperty("lake").GetDouble();
        var key = k.GetProperty("key");
        f.SetColor(FrameLayout.Key, Oklab.Parse(key.GetProperty("color").GetString()!));
        v[FrameLayout.Intensity] = key.GetProperty("intensity").GetDouble();
        v[FrameLayout.SatGain] = key.GetProperty("satGain").GetDouble();
        v[FrameLayout.LitLift] = key.GetProperty("litLift").GetDouble();
        v[FrameLayout.VegChroma] = key.TryGetProperty("vegChroma", out var vegChroma) ? vegChroma.GetDouble() : 1.0;
        var shade = k.GetProperty("shade");
        v[FrameLayout.R] = shade.GetProperty("r").GetDouble();
        v[FrameLayout.WMin] = shade.GetProperty("wMin").GetDouble();
        v[FrameLayout.VegDepth] = shade.TryGetProperty("vegDepth", out var vegDepth) ? vegDepth.GetDouble() : 1.0;
        SetAb(v, FrameLayout.AmbientA, shade.GetProperty("ambientSky"));
        var line = k.GetProperty("line");
        v[FrameLayout.LineDarken] = line.GetProperty("darken").GetDouble();
        f.SetColor(FrameLayout.LineWarm, Oklab.Parse(line.GetProperty("warm").GetString()!));
        f.SetColor(FrameLayout.LineCool, Oklab.Parse(line.GetProperty("cool").GetString()!));
        var colors = k.GetProperty("colors");
        foreach (var (token, index) in FrameLayout.ColorTokens) f.SetColor(index, Oklab.Parse(colors.GetProperty(token).GetString()!));
        SetAb(v, FrameLayout.InteriorA, colors.GetProperty("interior.warm"));
        var post = k.GetProperty("post");
        v[FrameLayout.ExposureEv] = post.GetProperty("exposureEv").GetDouble();
        v[FrameLayout.GlowBloom] = post.GetProperty("glowBloom").GetDouble();
        v[FrameLayout.GlowIntensity] = post.GetProperty("glowIntensity").GetDouble();
        v[FrameLayout.GlowHdrThreshold] = post.GetProperty("glowHdrThreshold").GetDouble();
        v[FrameLayout.GlowHdrScale] = post.GetProperty("glowHdrScale").GetDouble();
        var vignette = post.GetProperty("vignette");
        v[FrameLayout.Vignette] = vignette.GetProperty("strength").GetDouble();
        i = 0;
        foreach (var t in vignette.GetProperty("tint").EnumerateArray()) v[FrameLayout.VignetteTint + i++] = t.GetDouble();
        var cdl = post.GetProperty("cdl");
        v[FrameLayout.CdlSlope] = cdl.GetProperty("slope").GetDouble();
        v[FrameLayout.CdlOffset] = cdl.GetProperty("offset").GetDouble();
        v[FrameLayout.CdlPower] = cdl.GetProperty("power").GetDouble();
        v[FrameLayout.CdlSaturation] = cdl.GetProperty("saturation").GetDouble();
        var fx = k.GetProperty("skyFx");
        v[FrameLayout.Stars] = fx.GetProperty("stars").GetDouble();
        v[FrameLayout.CloudAmount] = fx.GetProperty("cloudAmount").GetDouble();
        v[FrameLayout.LampPoolGain] = 1.0;
        return f;
    }

    private static void SetAb(double[] v, int index, JsonElement ch)
    {
        var c = ch.GetProperty("c").GetDouble();
        var h = ch.GetProperty("h").GetDouble() * Math.PI / 180.0;
        v[index] = c * Math.Cos(h);
        v[index + 1] = c * Math.Sin(h);
    }
}
