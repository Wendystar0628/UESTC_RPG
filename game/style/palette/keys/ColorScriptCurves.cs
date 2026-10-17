using System.Text.Json;
using Campus.Contracts;
using static Campus.Style.Palette.Keys.FrameLayout;

namespace Campus.Style.Palette.Keys;

/// <summary>
/// SYS-TIME art sun, mirroring WP-34's ArtEphemeris (including its autumn 17:30 = 5.5° calibration). The live frame reads
/// IWorldState.Sun.AltDeg; this class only supplies the side of the arc, the noon cap, and altitudes for IPaletteProbe hours.
/// </summary>
public static class ArtSun
{
    // (sunrise h, sunset h, H noon cap °)
    private static readonly (double Rise, double Set, double H)[] Params =
    [
        (6 + 50 / 60.0, 19 + 20 / 60.0, 55.0),   // spring
        (6 + 10 / 60.0, 19 + 55 / 60.0, 62.0),   // summer
        (7 + 35 / 60.0, 18 + 5 / 60.0, 41.0),    // autumn
        (7 + 55 / 60.0, 17 + 55 / 60.0, 34.0),   // winter
    ];
    public const double NightH = 24.0, Exponent = 1.15, AutumnH = 41.0, NormFrom = 25.0;
    private static readonly double HeroSine = Math.Sin(Math.PI * (17.5 - 455 / 60.0) / ((1085 - 455) / 60.0));
    private static readonly double HeroDelta = 5.5 - 41.0 * Math.Pow(HeroSine, Exponent);

    public static double NoonCap(Season s) => Params[(int)s].H;

    /// <summary>Altitude (°) and side for a clock in a season; noon and nadir fall on the side that contains them.</summary>
    public static (double H, KeySide Side) Altitude(double clock, Season season)
    {
        var (rise, set, cap) = Params[(int)season];
        var c = clock % 24.0;
        if (c < 0) c += 24.0;
        if (c >= rise && c < set)
        {
            var u = (c - rise) / (set - rise);
            var sine = Math.Sin(Math.PI * u);
            var h = cap * Math.Pow(Math.Max(0.0, sine), Exponent);
            // WP-34 ArtEphemeris autumn calibration: autumn 17:30 is exactly 5.5° (SYS-TIME).
            if (season == Season.Autumn) h += HeroDelta * sine * (1.0 - sine) / (HeroSine * (1.0 - HeroSine));
            return (h, u < 0.5 ? KeySide.Dawn : KeySide.Dusk);
        }
        var night = 24.0 - set + rise;
        var un = ((c - set) % 24.0 + 24.0) % 24.0 / night;
        return (-NightH * Math.Pow(Math.Sin(Math.PI * un), Exponent), un < 0.5 ? KeySide.Dusk : KeySide.Dawn);
    }

    /// <summary>Season altitude → autumn-equivalent altitude: above 25° the arc is normalised to noon.</summary>
    public static double ToAutumnX(double h, Season season)
    {
        var cap = NoonCap(season);
        return h <= NormFrom ? h : NormFrom + (h - NormFrom) / (cap - NormFrom) * (AutumnH - NormFrom);
    }

    /// <summary>Monotone day phase p ∈ [0, 2): nadir 0 → dawn side → noon 1 → dusk side → nadir 2.</summary>
    public static double Phase(double x, KeySide side) => side switch
    {
        KeySide.Nadir => 0.0,
        KeySide.Noon => 1.0,
        KeySide.Dawn => (x + NightH) / (AutumnH + NightH),
        KeySide.Dusk => 1.0 + (AutumnH - x) / (AutumnH + NightH),
        _ => throw new ArgumentOutOfRangeException(nameof(side))
    };

    public static double PhaseAt(double clock, Season season)
    {
        var (h, side) = Altitude(clock, season);
        return Phase(ToAutumnX(h, season), side);
    }
}

/// <summary>Autumn base keyframes + seasonal deltas and overrides (SYS-TIME; seasons.json).</summary>
public static class SeasonComposer
{
    public static string SeasonKey(Season s) => s switch
    {
        Season.Spring => "spring",
        Season.Summer => "summer",
        Season.Autumn => "autumn",
        Season.Winter => "winter",
        _ => throw new ArgumentOutOfRangeException(nameof(s))
    };

    private static string ClassKey(KeyClass c) => c switch
    {
        KeyClass.Day => "day",
        KeyClass.Default => "default",
        KeyClass.LowSun => "lowSun",
        KeyClass.Twilight => "twilight",
        KeyClass.Night => "night",
        _ => throw new ArgumentOutOfRangeException(nameof(c))
    };

    private static double ScopeWeight(JsonElement seasons, string scope, string keyId)
    {
        var sc = seasons.GetProperty("scopes").GetProperty(scope);
        if (sc.ValueKind == JsonValueKind.String) return sc.GetString() == "all" ? 1.0 : throw new InvalidDataException($"Bad scope {scope}.");
        return sc.TryGetProperty(keyId, out var w) ? w.GetDouble() : 0.0;
    }

    private static Lab ShiftLch(Lab c, double dL, double dC, double dh, double w) =>
        Oklab.FromLch(c.L + dL * w, Math.Max(0.0, c.Chroma + dC * w), c.Hue + dh * w);

    public static FrameVector Apply(ColorScriptData data, int keyIndex, Season season)
    {
        var info = data.Keys[keyIndex];
        var f = data.AutumnFrames[keyIndex].Clone();
        var v = f.V;
        var seasons = data.Seasons;
        var sd = seasons.GetProperty("seasons").GetProperty(SeasonKey(season));
        var kid = info.Id;
        // Sky: zenith delta spread over the five bands by skyFalloff.
        var sky = sd.GetProperty("sky");
        var z = sky.GetProperty("zenith");
        var wSky = ScopeWeight(seasons, sky.GetProperty("scope").GetString()!, kid);
        var fall = seasons.GetProperty("skyFalloff").EnumerateArray().Select(e => e.GetDouble()).ToArray();
        for (var band = 0; band < 5; band++)
        {
            foreach (var start in new[] { SkySun, SkyAnti })
            {
                var i = start + 3 * band;
                f.SetColor(i, ShiftLch(f.Color(i), z.GetProperty("dL").GetDouble(), z.GetProperty("dC").GetDouble(),
                    z.GetProperty("dh").GetDouble(), wSky * fall[band]));
            }
        }
        // Key light.
        foreach (var op in sd.GetProperty("key").EnumerateArray())
        {
            var w = ScopeWeight(seasons, op.GetProperty("scope").GetString()!, kid);
            if (w <= 0) continue;
            switch (op.GetProperty("op").GetString())
            {
                case "toward":
                    f.SetColor(Key, Oklab.Lerp(f.Color(Key), Oklab.Parse(op.GetProperty("color").GetString()!), op.GetProperty("amount").GetDouble() * w));
                    break;
                case "chroma":
                    var k = f.Color(Key);
                    f.SetColor(Key, Oklab.FromLch(k.L, k.Chroma * (1.0 + (op.GetProperty("mul").GetDouble() - 1.0) * w), k.Hue));
                    break;
                default:
                    throw new InvalidDataException($"Unknown key op {op.GetProperty("op").GetString()}.");
            }
        }
        // Shade: ambient.sky and r, clamped by limits.
        var shade = sd.GetProperty("shade");
        var ws = ScopeWeight(seasons, shade.GetProperty("scope").GetString()!, kid);
        var (ac, ah) = f.Ambient;
        var amb = shade.GetProperty("ambientSky");
        ac = Math.Max(0.0, ac + amb.GetProperty("dC").GetDouble() * ws);
        ah += amb.GetProperty("dh").GetDouble() * ws;
        v[AmbientA] = ac * Math.Cos(ah * Math.PI / 180.0);
        v[AmbientB] = ac * Math.Sin(ah * Math.PI / 180.0);
        v[R] += shade.GetProperty("dr").GetDouble() * ws;
        var limits = seasons.GetProperty("limits");
        if (limits.GetProperty("rMax").TryGetProperty(ClassKey(info.Class), out var rmax) && v[R] > rmax.GetDouble() + 1e-9)
            v[R] = rmax.GetDouble();
        if (limits.TryGetProperty("zenithMinC", out var zmc) && zmc.TryGetProperty(kid, out var zmin))
        {
            foreach (var i in new[] { SkySun + 12, SkyAnti + 12 })
            {
                var c = f.Color(i);
                if (c.Chroma < zmin.GetDouble()) f.SetColor(i, Oklab.FromLch(c.L, zmin.GetDouble(), c.Hue));
            }
        }
        // Fog.
        var fog = sd.GetProperty("fog");
        var wf = ScopeWeight(seasons, fog.GetProperty("scope").GetString()!, kid);
        Lab? toward = fog.GetProperty("toward").ValueKind == JsonValueKind.String ? Oklab.Parse(fog.GetProperty("toward").GetString()!) : null;
        foreach (var i in new[] { FogSun, FogAnti })
        {
            var c = f.Color(i);
            double a = c.A, b = c.B;
            if (toward is { } t)
            {
                var amount = fog.GetProperty("abAmount").GetDouble() * wf;
                a = Oklab.Lerp(a, t.A, amount);
                b = Oklab.Lerp(b, t.B, amount);
            }
            f.SetColor(i, new Lab(Math.Min(1.0, c.L + fog.GetProperty("dL").GetDouble()), a, b));
        }
        v[FogStart] *= fog.GetProperty("visibilityMul").GetDouble();
        v[FogSilhouette] *= fog.GetProperty("visibilityMul").GetDouble();
        v[HazeDeg] *= fog.GetProperty("hazeMul").GetDouble();
        v[GlowIntensity] *= sd.GetProperty("post").GetProperty("glowIntensityMul").GetDouble();
        v[LampPoolGain] = sd.TryGetProperty("lampPoolGain", out var lamp) ? lamp.GetDouble() : 1.0;
        // Season-specific overrides (hand-tuned colours).
        foreach (var ov in sd.GetProperty("overrides").EnumerateArray())
        {
            if (!ov.GetProperty("keys").EnumerateArray().Any(e => e.GetString() == kid)) continue;
            if (ov.TryGetProperty("colors", out var colors))
                foreach (var p in colors.EnumerateObject())
                {
                    var found = Array.Find(FrameLayout.ColorTokens, t => t.Token == p.Name);
                    if (found.Token is null) throw new InvalidDataException($"Unknown override colour {p.Name}.");
                    f.SetColor(found.Index, Oklab.Parse(p.Value.GetString()!));
                }
            if (ov.TryGetProperty("skyFx", out var fx) && fx.TryGetProperty("cloudAmountAdd", out var add))
                v[CloudAmount] = Math.Min(1.0, v[CloudAmount] + add.GetDouble());
        }
        return f;
    }
}

/// <summary>
/// Periodic monotone cubic Hermite (Fritsch–Butland) over the 12 anchors of one season (SYS-TIME):
/// colours in OKLab; segments whose hue swings more than 40° take an OKLCH path (sky bands over 60° go through magenta);
/// intensity and fog in the log domain. Mirrors DayCurve in pipeline/colorscript/colorscript.py.
/// </summary>
public sealed class DayCurve
{
    private readonly double[] _p;
    private readonly FrameVector[] _f;
    private readonly double[][] _m;
    public IReadOnlyList<FrameVector> Frames => _f;

    public DayCurve(IReadOnlyList<KeyframeInfo> keys, IReadOnlyList<FrameVector> frames)
    {
        var order = Enumerable.Range(0, keys.Count).OrderBy(i => ArtSun.Phase(keys[i].AltDeg, keys[i].Side)).ToArray();
        _p = order.Select(i => ArtSun.Phase(keys[i].AltDeg, keys[i].Side)).ToArray();
        _f = order.Select(i => frames[i]).ToArray();
        var n = _p.Length;
        _m = new double[Count][];
        for (var j = 0; j < Count; j++)
        {
            var y = new double[n];
            for (var i = 0; i < n; i++) y[i] = Value(_f[i].V[j], j);
            var d = new double[n];
            for (var i = 0; i < n; i++)
            {
                var x0 = _p[i];
                var x1 = _p[(i + 1) % n] + (i == n - 1 ? 2.0 : 0.0);
                d[i] = (y[(i + 1) % n] - y[i]) / (x1 - x0);
            }
            var m = new double[n];
            for (var i = 0; i < n; i++)
            {
                var dp = d[(i - 1 + n) % n];
                var dn = d[i];
                m[i] = dp * dn <= 0.0 ? 0.0 : 2.0 * dp * dn / (dp + dn);
            }
            _m[j] = m;
        }
    }

    private static double Value(double v, int j) => IsLog(j) ? Math.Log2(Math.Max(1e-6, v)) : v;

    private static double Hermite(double y0, double y1, double m0, double m1, double h, double t)
    {
        var t2 = t * t; var t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * y0 + (t3 - 2 * t2 + t) * h * m0 + (-2 * t3 + 3 * t2) * y1 + (t3 - t2) * h * m1;
    }

    private static bool ArcThrough(double h0, double d, double target) =>
        d >= 0 ? Lab.Mod360(target - h0) <= d : Lab.Mod360(h0 - target) <= -d;

    private static double HuePath(double h0, double h1, bool viaMagenta)
    {
        var d = Oklab.HueDiff(h0, h1);
        if (viaMagenta && !ArcThrough(h0, d, 345.0))
        {
            var alt = d - Math.CopySign(360.0, d);
            if (ArcThrough(h0, alt, 345.0)) return alt;
        }
        return d;
    }

    public FrameVector Evaluate(double phase)
    {
        var n = _p.Length;
        phase %= 2.0;
        if (phase < 0) phase += 2.0;
        int i = n - 1, j = 0;
        double t = 0, h = 1;
        for (var k = 0; k < n; k++)
        {
            var p0 = _p[k];
            var p1 = _p[(k + 1) % n] + (k == n - 1 ? 2.0 : 0.0);
            var pp = phase >= p0 ? phase : phase + 2.0;
            if (pp >= p0 && pp <= p1) { i = k; j = (k + 1) % n; t = (pp - p0) / (p1 - p0); h = p1 - p0; break; }
        }
        var outF = new FrameVector();
        var o = outF.V;
        for (var c = 0; c < Count; c++)
        {
            var y = Hermite(Value(_f[i].V[c], c), Value(_f[j].V[c], c), _m[c][i], _m[c][j], h, t);
            o[c] = IsLog(c) ? Math.Pow(2.0, y) : y;
        }
        var te = Oklab.Smoothstep(0.0, 1.0, t);
        for (var c = 0; c < ColorEnd; c += 3)
        {
            var a0 = _f[i].Color(c);
            var a1 = _f[j].Color(c);
            if (a0.Chroma < 0.02 || a1.Chroma < 0.02) continue;
            var d = Oklab.HueDiff(a0.Hue, a1.Hue);
            if (Math.Abs(d) <= 40.0) continue;
            var dd = HuePath(a0.Hue, a1.Hue, IsSky(c) && Math.Abs(d) > 60.0);
            var chroma = Oklab.Lerp(a0.Chroma, a1.Chroma, te);
            var hue = (a0.Hue + dd * te) * Math.PI / 180.0;
            o[c + 1] = chroma * Math.Cos(hue);
            o[c + 2] = chroma * Math.Sin(hue);
        }
        return outF;
    }

    /// <summary>Blend two frames (season axis, SYS-TIME: smootherstep); log scalars blend in the log domain.</summary>
    public static FrameVector Blend(FrameVector a, FrameVector b, double t)
    {
        var f = new FrameVector();
        for (var c = 0; c < Count; c++)
            f.V[c] = IsLog(c)
                ? Math.Pow(2.0, Oklab.Lerp(Math.Log2(Math.Max(1e-6, a.V[c])), Math.Log2(Math.Max(1e-6, b.V[c])), t))
                : Oklab.Lerp(a.V[c], b.V[c], t);
        return f;
    }
}

/// <summary>Weather correction layer (SYS-WEATHER, weather.json). Clear weather leaves the frame untouched.</summary>
public static class WeatherCorrection
{
    public static bool IsClear(in WeatherVec w) => w.Overcast == 0f && w.Precip == 0f && w.Wet == 0f;

    public static void Apply(JsonElement weather, FrameVector f, in WeatherVec w, bool daylight)
    {
        if (IsClear(w)) return;
        var v = f.V;
        double g = w.Overcast, p = w.Precip, k = w.PrecipKind, wet = w.Wet;
        var keyEv = weather.GetProperty("keyEv");
        var ev = keyEv.GetProperty("overcast").GetDouble() * g + keyEv.GetProperty("heavyRainExtra").GetDouble() * p * (1.0 - k);
        v[Intensity] *= Math.Pow(2.0, ev);
        var rf = weather.GetProperty("shade").GetProperty("rFloor");
        var target = Math.Max(v[R], rf.GetProperty("base").GetDouble() + rf.GetProperty("perOvercast").GetDouble() * g);
        v[R] += g * (target - v[R]);
        var tgt = weather.GetProperty("shade").GetProperty("ambientSkyTarget");
        var (c0, h0) = f.Ambient;
        var c1 = Oklab.Lerp(c0, tgt.GetProperty("c").GetDouble(), g);
        var h1 = (h0 + Oklab.HueDiff(h0, tgt.GetProperty("h").GetDouble()) * g) * Math.PI / 180.0;
        v[AmbientA] = c1 * Math.Cos(h1);
        v[AmbientB] = c1 * Math.Sin(h1);
        var so = weather.GetProperty("sky").GetProperty("overcast");
        var minL = so.GetProperty("minL").GetDouble();
        var oc = Oklab.FromLch(Math.Max(minL, 0.78 - so.GetProperty("rainLDrop").GetDouble() * p * (1.0 - k)),
            so.GetProperty("c").GetDouble(), so.GetProperty("h").GetDouble());
        var washed = wet * (1.0 - g) * (1.0 - p);
        var washedC = weather.GetProperty("sky").GetProperty("washedChromaAdd").GetDouble() * washed;
        for (var i = SkySun; i < FogSun; i += 3)
        {
            var c = Oklab.Lerp(f.Color(i), oc, g);
            double l = c.L, ch = c.Chroma;
            if (g > 0.5)
            {
                if (daylight) l = Math.Max(l, minL);
                ch = Math.Max(ch, so.GetProperty("minC").GetDouble());
            }
            f.SetColor(i, Oklab.FromLch(l, ch + washedC, c.Hue));
        }
        v[SatGain] += weather.GetProperty("washedSatAdd").GetDouble() * washed;
        v[CloudAmount] = Math.Max(v[CloudAmount] * (1.0 - g), w.Cloud);
    }

    /// <summary>Rainy vegetation chroma +15% (SYS-WEATHER), continuous in precipitation and wetness.</summary>
    public static double VegetationChromaMul(JsonElement weather, in WeatherVec w)
    {
        var p = w.Precip * (1.0 - w.PrecipKind);
        return 1.0 + (weather.GetProperty("rainVegetationChromaMul").GetDouble() - 1.0) * Math.Min(1.0, Math.Max(p, w.Wet));
    }
}
