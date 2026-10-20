using Campus.Contracts;
using static Campus.Style.Palette.Keys.FrameLayout;

namespace Campus.Style.Palette.Keys;

/// <summary>Colour-script coordinates derived from the shared authored solar track.</summary>
public static class ArtSun
{
    public const double NormFrom = 25.0;
    public static double AutumnH => ArtEphemeris.At(Season.Autumn).Height;
    public static double NoonCap(Season season) => ArtEphemeris.At(season).Height;

    /// <summary>Altitude and side; noon and nadir fall on the side that contains them.</summary>
    public static (double H, KeySide Side) Altitude(double clock, Season season)
    {
        var c = ArtEphemeris.Wrap(clock, 24d);
        var phase = ArtEphemeris.Phase(c, (int)season);
        var side = phase < 1
            ? (phase < 0.5 ? KeySide.Dawn : KeySide.Dusk)
            : (phase < 1.5 ? KeySide.Dusk : KeySide.Dawn);
        return (ArtEphemeris.SunAt(c, (int)season).Altitude, side);
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
        KeySide.Dawn => (x + ArtEphemeris.NightDepth) / (AutumnH + ArtEphemeris.NightDepth),
        KeySide.Dusk => 1.0 + (AutumnH - x) / (AutumnH + ArtEphemeris.NightDepth),
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
    private static string ClassKey(KeyClass c) => c switch
    {
        KeyClass.Day => "day",
        KeyClass.Default => "default",
        KeyClass.LowSun => "lowSun",
        KeyClass.Twilight => "twilight",
        KeyClass.Night => "night",
        _ => throw new ArgumentOutOfRangeException(nameof(c))
    };

    private static Lab ShiftLch(Lab c, double dL, double dC, double dh, double w) =>
        Oklab.FromLch(c.L + dL * w, Math.Max(0.0, c.Chroma + dC * w), c.Hue + dh * w);

    public static FrameVector Apply(ColorScriptData data, int keyIndex, Season season)
    {
        var info = data.Keys[keyIndex];
        var f = data.AutumnFrames[keyIndex].Clone();
        var seasons = data.Seasons;
        var sd = seasons.Seasons.For(season);
        var kid = info.Id;
        ApplySky(f, seasons, sd, kid);
        ApplyKeyLight(f, seasons, sd, kid);
        ApplyShadeAndLimits(f, seasons, sd, info);
        ApplyFog(f, seasons, sd, kid);
        f.V[GlowIntensity] *= sd.Post.GlowIntensityMul;
        f.V[LampPoolGain] = sd.LampPoolGain;
        ApplyOverrides(f, sd, kid);
        return f;
    }

    private static void ApplySky(FrameVector f, SeasonsFile seasons, SeasonDeltaJson sd, string kid)
    {
        // Sky: zenith delta spread over the five bands by skyFalloff.
        var z = sd.Sky.Zenith;
        var wSky = seasons.ScopeWeight(sd.Sky.Scope, kid);
        var fall = seasons.SkyFalloff;
        for (var band = 0; band < 5; band++)
        {
            foreach (var start in new[] { SkySun, SkyAnti })
            {
                var i = start + 3 * band;
                f.SetColor(i, ShiftLch(f.Color(i), z.DL, z.DC, z.Dh, wSky * fall[band]));
            }
        }
    }

    private static void ApplyKeyLight(FrameVector f, SeasonsFile seasons, SeasonDeltaJson sd, string kid)
    {
        // Key light.
        foreach (var op in sd.Key)
        {
            var w = seasons.ScopeWeight(op.Scope, kid);
            if (w <= 0) continue;
            if (op.Op == "toward")
                f.SetColor(Key, Oklab.Lerp(f.Color(Key), op.Color!.Value, op.Amount!.Value * w));
            else
            {
                var k = f.Color(Key);
                f.SetColor(Key, Oklab.FromLch(k.L, k.Chroma * (1.0 + (op.Mul!.Value - 1.0) * w), k.Hue));
            }
        }
    }

    private static void ApplyShadeAndLimits(FrameVector f, SeasonsFile seasons, SeasonDeltaJson sd, KeyframeInfo info)
    {
        var v = f.V;
        var kid = info.Id;
        // Shade: ambient.sky and r, clamped by limits.
        var shade = sd.Shade;
        var ws = seasons.ScopeWeight(shade.Scope, kid);
        var (ac, ah) = f.Ambient;
        ac = Math.Max(0.0, ac + shade.AmbientSky.DC * ws);
        ah += shade.AmbientSky.Dh * ws;
        v[AmbientA] = ac * Math.Cos(ah * Math.PI / 180.0);
        v[AmbientB] = ac * Math.Sin(ah * Math.PI / 180.0);
        v[R] += shade.Dr * ws;
        var limits = seasons.Limits;
        if (limits.RMax.TryGetValue(ClassKey(info.Class), out var rmax) && v[R] > rmax + 1e-9)
            v[R] = rmax;
        if (limits.ZenithMinC is { } zenithMinC && zenithMinC.TryGetValue(kid, out var zmin))
        {
            foreach (var i in new[] { SkySun + 12, SkyAnti + 12 })
            {
                var c = f.Color(i);
                if (c.Chroma < zmin) f.SetColor(i, Oklab.FromLch(c.L, zmin, c.Hue));
            }
        }
    }

    private static void ApplyFog(FrameVector f, SeasonsFile seasons, SeasonDeltaJson sd, string kid)
    {
        var v = f.V;
        // Fog.
        var fog = sd.Fog;
        var wf = seasons.ScopeWeight(fog.Scope, kid);
        foreach (var i in new[] { FogSun, FogAnti })
        {
            var c = f.Color(i);
            double a = c.A, b = c.B;
            if (fog.Toward is { } t)
            {
                var amount = fog.AbAmount * wf;
                a = ScalarMath.Lerp(a, t.A, amount);
                b = ScalarMath.Lerp(b, t.B, amount);
            }
            f.SetColor(i, new Lab(Math.Min(1.0, c.L + fog.DL), a, b));
        }
        v[FogStart] *= fog.VisibilityMul;
        v[FogSilhouette] *= fog.VisibilityMul;
        v[HazeDeg] *= fog.HazeMul;
    }

    private static void ApplyOverrides(FrameVector f, SeasonDeltaJson sd, string kid)
    {
        var v = f.V;
        // Season-specific overrides (hand-tuned colours).
        foreach (var ov in sd.Overrides)
        {
            if (!ov.Keys.Contains(kid)) continue;
            if (ov.Colors is { } colors)
                foreach (var (token, color) in colors)
                    f.SetColor(Array.Find(FrameLayout.ColorTokens, t => t.Token == token).Index, color);
            if (ov.SkyFx?.CloudAmountAdd is { } add)
                v[CloudAmount] = Math.Min(1.0, v[CloudAmount] + add);
        }
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
        var te = ScalarMath.Smoothstep(0.0, 1.0, t);
        for (var c = 0; c < ColorEnd; c += 3)
        {
            var a0 = _f[i].Color(c);
            var a1 = _f[j].Color(c);
            if (a0.Chroma < 0.02 || a1.Chroma < 0.02) continue;
            var d = Oklab.HueDiff(a0.Hue, a1.Hue);
            if (Math.Abs(d) <= 40.0) continue;
            var dd = HuePath(a0.Hue, a1.Hue, IsSky(c) && Math.Abs(d) > 60.0);
            var chroma = ScalarMath.Lerp(a0.Chroma, a1.Chroma, te);
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
                ? Math.Pow(2.0, ScalarMath.Lerp(Math.Log2(Math.Max(1e-6, a.V[c])), Math.Log2(Math.Max(1e-6, b.V[c])), t))
                : ScalarMath.Lerp(a.V[c], b.V[c], t);
        return f;
    }
}

/// <summary>Weather correction layer (SYS-WEATHER, weather.json). Clear weather leaves the frame untouched.</summary>
public static class WeatherCorrection
{
    // Same solar-altitude handover as sky_night_k in fog.gdshaderinc.
    private const double NightSideFromDeg = -6.0;
    private const double NightSideToDeg = -13.0;

    public static bool IsClear(in WeatherVec w) => w.Overcast == 0f && w.Precip == 0f && w.Wet == 0f;

    public static void Apply(WeatherFile weather, FrameVector f, in WeatherVec w, double sunAltDeg)
    {
        if (IsClear(w)) return;
        var v = f.V;
        double g = w.Overcast, p = w.Precip, k = w.PrecipKind, wet = w.Wet;
        var ev = weather.KeyEv.Overcast * g + weather.KeyEv.HeavyRainExtra * p * (1.0 - k);
        v[Intensity] *= Math.Pow(2.0, ev);
        var rf = weather.Shade.RFloor;
        var target = Math.Max(v[R], rf.Base + rf.PerOvercast * g);
        v[R] += g * (target - v[R]);
        var tgt = weather.Shade.AmbientSkyTarget;
        var (c0, h0) = f.Ambient;
        var c1 = ScalarMath.Lerp(c0, tgt.C, g);
        var h1 = (h0 + Oklab.HueDiff(h0, tgt.H) * g) * Math.PI / 180.0;
        v[AmbientA] = c1 * Math.Cos(h1);
        v[AmbientB] = c1 * Math.Sin(h1);
        var so = weather.Sky.Overcast;
        var night = 1.0 - ScalarMath.Smoothstep(NightSideToDeg, NightSideFromDeg, sunAltDeg);
        var nr = weather.Tokens.NightRain;
        var oc = Oklab.FromLch(Math.Max(so.MinL, 0.78 - so.RainLDrop * p * (1.0 - k)), so.C, so.H);
        oc = Oklab.Lerp(oc, Oklab.FromLch(nr.L, nr.C, nr.H), night);
        var washed = wet * (1.0 - g) * (1.0 - p);
        var washedC = weather.Sky.WashedChromaAdd * washed;
        for (var i = SkySun; i < FogSun; i += 3)
        {
            var c = Oklab.Lerp(f.Color(i), oc, g);
            double l = c.L, ch = c.Chroma;
            if (g > 0.5)
            {
                l = Math.Max(l, so.MinL * (1.0 - night));
                if ((i == SkySun + 12 || i == SkyAnti + 12) && weather.Sky.NightRainMinZenithL is { } nightMinL)
                    l = Math.Max(l, nightMinL * night);
                ch = Math.Max(ch, so.MinC);
            }
            f.SetColor(i, Oklab.FromLch(l, ch + washedC, c.Hue));
        }
        v[SatGain] += weather.WashedSatAdd * washed;
        v[CloudAmount] = Math.Max(v[CloudAmount] * (1.0 - g), w.Cloud);
    }

    /// <summary>Rainy vegetation chroma +15% (SYS-WEATHER), continuous in precipitation and wetness.</summary>
    public static double VegetationChromaMul(WeatherFile weather, in WeatherVec w)
    {
        var p = w.Precip * (1.0 - w.PrecipKind);
        return 1.0 + (weather.RainVegetationChromaMul - 1.0) * Math.Min(1.0, Math.Max(p, w.Wet));
    }
}
