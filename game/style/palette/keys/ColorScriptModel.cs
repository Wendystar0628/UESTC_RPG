using Campus.Contracts;

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
    public const int Bounce = 45, Rim = 48, Glare = 51;
    public const int CloudLit = 54, CloudRim = 57, CloudBelly = 60, CloudDeep = 63, Moon = 66;
    public const int ColorEnd = 69;
    // ab pairs.
    public const int AmbientA = 69, AmbientB = 70, InteriorA = 71, InteriorB = 72;
    // Scalars.
    public const int Intensity = 73, SatGain = 74, LitLift = 75, R = 76, WMin = 77, LineDarken = 78;
    public const int FogDensity = 79, HazeDeg = 80, FogStart = 81, FogSilhouette = 82;
    public const int FogLowTop = 83, FogLowDensity = 84, FogLake = 85;
    public const int Stars = 86, CloudAmount = 87;
    public const int ExposureEv = 88, GlowBloom = 89, GlowIntensity = 90, GlowHdrThreshold = 91, GlowHdrScale = 92;
    public const int Vignette = 93, VignetteTint = 94; // 3 components
    public const int CdlSlope = 97, CdlOffset = 98, CdlPower = 99, CdlSaturation = 100;
    public const int LampPoolGain = 101;
    public const int VegChroma = 102;      // key.vegChroma：植被受光与影的彩度倍率（缺省 1），与雨天的倍率相乘
    public const int VegDepth = 103;       // shade.vegDepth：三阶类（植被、花）影的明度差倍率（缺省 1）
    public const int Count = 104;

    /// <summary>Scalars interpolated in log2 (SYS-TIME: light intensity and fog density in the log domain).</summary>
    public static bool IsLog(int i) => i is Intensity or FogDensity or FogStart or FogSilhouette;

    /// <summary>The colour triples that form the sky bands (hue swings > 60° route through magenta).</summary>
    public static bool IsSky(int colorIndex) => colorIndex < FogSun;

    public static readonly (string Token, int Index)[] ColorTokens =
    [
        ("bounce.ground", Bounce), ("rim", Rim), ("glass.glare", Glare),
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

/// <summary>Autumn base keyframe (SYS-TIME) with its altitude anchor.</summary>
public sealed record KeyframeInfo(string Id, string Name, KeyClass Class, KeySide Side, double AltDeg, double ExampleClock);

public sealed class ColorScriptData
{
    public required IReadOnlyList<KeyframeInfo> Keys { get; init; }
    public required IReadOnlyList<FrameVector> AutumnFrames { get; init; }
    public required double[] SkyStopsDeg { get; init; }
    public required DeriveFile Derive { get; init; }
    public required SeasonsFile Seasons { get; init; }
    public required VegetationFile Vegetation { get; init; }
    public required WeatherFile Weather { get; init; }
    public required IReadOnlyList<string> FrameTokens { get; init; }

    /// <summary>Parses the five data files of game/style/palette/keys (UTF-8 JSON bytes); errors name the file and JSON path.</summary>
    public static ColorScriptData Load(byte[] autumn, byte[] derive, byte[] seasons, byte[] vegetation, byte[] weather)
    {
        var keyFile = ColorScriptJson.Keys(autumn);
        var deriveFile = ColorScriptJson.Derive(derive);
        var seasonsFile = ColorScriptJson.Seasons(seasons);
        var vegetationFile = ColorScriptJson.Vegetation(vegetation);
        var weatherFile = ColorScriptJson.Weather(weather);
        ColorScriptJson.Validate(keyFile, deriveFile, seasonsFile, vegetationFile);
        return new ColorScriptData
        {
            Keys = keyFile.Keys.Select(k => new KeyframeInfo(k.Id, k.Name, k.Class, k.Side, k.AltDeg, k.ExampleClock)).ToArray(),
            AutumnFrames = keyFile.Keys.Select(ToFrame).ToArray(),
            SkyStopsDeg = keyFile.SkyStopsDeg,
            Derive = deriveFile,
            Seasons = seasonsFile,
            Vegetation = vegetationFile,
            Weather = weatherFile,
            FrameTokens = deriveFile.FrameTokens
        };
    }

    private static FrameVector ToFrame(KeyJson k)
    {
        var f = new FrameVector();
        var v = f.V;
        for (var i = 0; i < 5; i++)
        {
            f.SetColor(FrameLayout.SkySun + 3 * i, k.Sky.Sun[i]);
            f.SetColor(FrameLayout.SkyAnti + 3 * i, k.Sky.Anti[i]);
        }
        f.SetColor(FrameLayout.FogSun, k.Fog.Sun);
        f.SetColor(FrameLayout.FogAnti, k.Fog.Anti);
        v[FrameLayout.FogStart] = k.Fog.StartM;
        v[FrameLayout.FogSilhouette] = k.Fog.SilhouetteM;
        v[FrameLayout.FogDensity] = k.Fog.Density;
        v[FrameLayout.HazeDeg] = k.Fog.HazeDeg;
        v[FrameLayout.FogLowTop] = k.FogLow.TopM;
        v[FrameLayout.FogLowDensity] = k.FogLow.Density;
        v[FrameLayout.FogLake] = k.FogLow.Lake;
        f.SetColor(FrameLayout.Key, k.Key.Color);
        v[FrameLayout.Intensity] = k.Key.Intensity;
        v[FrameLayout.SatGain] = k.Key.SatGain;
        v[FrameLayout.LitLift] = k.Key.LitLift;
        v[FrameLayout.VegChroma] = k.Key.VegChroma;
        v[FrameLayout.R] = k.Shade.R;
        v[FrameLayout.WMin] = k.Shade.WMin;
        v[FrameLayout.VegDepth] = k.Shade.VegDepth;
        SetAb(v, FrameLayout.AmbientA, k.Shade.AmbientSky);
        v[FrameLayout.LineDarken] = k.Line.Darken;
        f.SetColor(FrameLayout.LineWarm, k.Line.Warm);
        f.SetColor(FrameLayout.LineCool, k.Line.Cool);
        var colors = k.Colors;
        f.SetColor(FrameLayout.Bounce, colors.BounceGround);
        f.SetColor(FrameLayout.Rim, colors.Rim);
        f.SetColor(FrameLayout.Glare, colors.GlassGlare);
        f.SetColor(FrameLayout.CloudLit, colors.SkyCloudLit);
        f.SetColor(FrameLayout.CloudRim, colors.SkyCloudRim);
        f.SetColor(FrameLayout.CloudBelly, colors.SkyCloudBelly);
        f.SetColor(FrameLayout.CloudDeep, colors.SkyCloudDeep);
        f.SetColor(FrameLayout.Moon, colors.SkyMoon);
        SetAb(v, FrameLayout.InteriorA, colors.InteriorWarm);
        var post = k.Post;
        v[FrameLayout.ExposureEv] = post.ExposureEv;
        v[FrameLayout.GlowBloom] = post.GlowBloom;
        v[FrameLayout.GlowIntensity] = post.GlowIntensity;
        v[FrameLayout.GlowHdrThreshold] = post.GlowHdrThreshold;
        v[FrameLayout.GlowHdrScale] = post.GlowHdrScale;
        v[FrameLayout.Vignette] = post.Vignette.Strength;
        for (var i = 0; i < 3; i++) v[FrameLayout.VignetteTint + i] = post.Vignette.Tint[i];
        v[FrameLayout.CdlSlope] = post.Cdl.Slope;
        v[FrameLayout.CdlOffset] = post.Cdl.Offset;
        v[FrameLayout.CdlPower] = post.Cdl.Power;
        v[FrameLayout.CdlSaturation] = post.Cdl.Saturation;
        v[FrameLayout.Stars] = k.SkyFx.Stars;
        v[FrameLayout.CloudAmount] = k.SkyFx.CloudAmount;
        v[FrameLayout.LampPoolGain] = 1.0;
        return f;
    }

    private static void SetAb(double[] v, int index, ChromaHueJson ch)
    {
        var h = ch.H * Math.PI / 180.0;
        v[index] = ch.C * Math.Cos(h);
        v[index + 1] = ch.C * Math.Sin(h);
    }
}
