using System.Numerics;
using Campus.Contracts;
using static Campus.Style.Palette.Keys.FrameLayout;

namespace Campus.Style.Palette.Keys;

/// <summary>The evaluated colour script for one world state: blended frame plus what derivation needs.</summary>
public sealed record WorldFrame(FrameVector Frame, double X, double SunAltDeg, Season SeasonA, Season SeasonB, double SeasonT, double VegChromaMul);

/// <summary>Colour script runtime (SYS-TIME): season sets, day curves, weather layer and derivation. Pure .NET.</summary>
public sealed class ColorScript
{
    public ColorScriptData Data { get; }
    public Deriver Deriver { get; }
    public VegetationSet Vegetation { get; }
    private readonly DayCurve[] _curves = new DayCurve[4];

    public ColorScript(ColorScriptData data)
    {
        Data = data;
        Deriver = new Deriver(data.Derive);
        var seasonKeys = new IReadOnlyList<FrameVector>[4];
        foreach (var s in Enum.GetValues<Season>())
        {
            var frames = Enumerable.Range(0, data.Keys.Count).Select(i => SeasonComposer.Apply(data, i, s)).ToArray();
            seasonKeys[(int)s] = frames;
            _curves[(int)s] = new DayCurve(data.Keys, frames);
        }
        Vegetation = new VegetationSet(data, Deriver, seasonKeys);
    }

    public FrameVector Evaluate(Season season, double clock) => _curves[(int)season].Evaluate(ArtSun.PhaseAt(clock, season));

    private FrameVector EvaluateAt(Season season, double altDeg, KeySide side) =>
        _curves[(int)season].Evaluate(ArtSun.Phase(ArtSun.ToAutumnX(altDeg, season), side));

    /// <summary>
    /// Evaluates the world frame. Keyframes are indexed by sun altitude (SYS-TIME): <paramref name="sunAltDeg"/> is
    /// IWorldState.Sun.AltDeg (WP-34, single source); the side of the arc comes from the clock. Null uses the mirrored art sun.
    /// </summary>
    public WorldFrame EvaluateWorld(double clock, double seasonPos, double? sunAltDeg, in WeatherVec weather)
    {
        var sp = ((seasonPos % 4.0) + 4.0) % 4.0;
        var ia = (int)Math.Floor(sp) % 4;
        var a = (Season)ia;
        var b = (Season)((ia + 1) % 4);
        var t = ScalarMath.Smootherstep(sp - Math.Floor(sp));
        var (ha, sideA) = ArtSun.Altitude(clock, a);
        var h = sunAltDeg ?? ha;
        var f = EvaluateAt(a, h, sideA);
        if (t > 1e-9) f = DayCurve.Blend(f, EvaluateAt(b, h, ArtSun.Altitude(clock, b).Side), t);
        WeatherCorrection.Apply(Data.Weather, f, weather, h);
        return new WorldFrame(f, ArtSun.ToAutumnX(h, a), h, a, b, t,
            f.V[VegChroma] * WeatherCorrection.VegetationChromaMul(Data.Weather, weather));
    }

    /// <summary>IPaletteProbe: 24 whole-hour horizon colours, average of the sun-side and anti-sun horizon bands.</summary>
    public void SampleHorizon(Season season, in WeatherVec weather, Span<Vector3> srgb24)
    {
        if (srgb24.Length != 24) throw new ArgumentException("IPaletteProbe expects exactly 24 samples.", nameof(srgb24));
        for (var hour = 0; hour < 24; hour++)
        {
            var f = Evaluate(season, hour);
            var (h, _) = ArtSun.Altitude(hour, season);
            WeatherCorrection.Apply(Data.Weather, f, weather, h);
            var (r, g, b) = Oklab.ToSrgbClipped(Oklab.Lerp(f.Sky(true, 0), f.Sky(false, 0), 0.5));
            srgb24[hour] = new Vector3((float)r, (float)g, (float)b);
        }
    }
}

/// <summary>
/// Fills the 256×68 RGBA8 palette (ENG-PERF, contract-28 route B, layout <see cref="PaletteLayout.LayoutId"/>): row 0
/// current colours, row 1 lamp pools, rows 2–3 interior shade/lit, rows 4–63 seasonal rows along the season axis
/// (256 samples), rows 64–67 the same four rows for colour IDs 256–511 (bank 1). Colour IDs reach a texel only through
/// <see cref="PaletteLayout.FrameTexel"/>, season samples only through the seasonal rows, so the first 256×64 bytes are the
/// legacy layout byte for byte. Bytes are sRGB. Seasonal rows refresh one family per call to <see cref="StepSeasonal"/>
/// unless a full refresh is requested.
/// </summary>
public sealed class PaletteComposer
{
    public const int Width = PaletteLayout.Width, Height = PaletteLayout.Height;
    public byte[] Pixels { get; } = new byte[PaletteLayout.ByteCount];

    private sealed record Pair(int LitId, string Family, string Class, Lab Albedo, VegetationSet.Entry? Veg, VegetationSet.Variant? Variant = null);
    private sealed record SeasonalFamily(int LitId, int Row, VegetationSet.Entry Veg);

    private readonly ColorScript _script;
    private readonly List<(int Id, string Token, Lab Constant)> _frame = [];
    private readonly List<Pair> _pairs = [];
    private readonly List<SeasonalFamily> _seasonal = [];
    private readonly Lab _shadowReference;
    private readonly Lab _lampColor;
    private int _cursor;
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>The complete catalog: bank-0 entries plus bank-1 pairs (indices 256–511, non-seasonal).</summary>
    public PaletteComposer(ColorScript script, PaletteCatalog catalog)
    {
        _script = script;
        var warnings = new List<string>();
        _shadowReference = script.Data.Derive.ShadowReference;
        // Row 1 lamp light (mainline placeholder until the WP-39 rule): the warm streetlamp constant, else the warm window light.
        _lampColor = catalog.TryGet(new PaletteToken("lamp.warm"), out var lamp) || catalog.TryGet(new PaletteToken("window.warm"), out lamp)
            ? FromSrgb(lamp.Albedo) : new Lab(0.92, 0.01, 0.07);
        foreach (var e in catalog.Entries)
        {
            var id = e.Index.Value;
            switch (e.Role)
            {
                case PaletteRole.Frame:
                case PaletteRole.Weather:
                case PaletteRole.Emissive:
                    _frame.Add((id, e.Token.Value, FromSrgb(e.Albedo)));
                    break;
                case PaletteRole.Retired:
                case PaletteRole.Shade:
                    break;
                case PaletteRole.Lit:
                    var family = e.Token.Value.EndsWith(".lit", StringComparison.Ordinal) ? e.Token.Value[..^4] : e.Token.Value;
                    script.Vegetation.Entries.TryGetValue(family, out var veg);
                    if (script.Vegetation.Variants.TryGetValue(family, out var variant))
                    {
                        if (e.Seasonal) { warnings.Add($"palette: variant '{family}' is registered as seasonal; variants only fill row 0"); break; }
                        _pairs.Add(new Pair(id, family, e.MaterialClass, FromSrgb(e.Albedo), script.Vegetation.Entries[variant.Base], variant));
                        break;
                    }
                    var cls = script.Deriver.HasClass(e.MaterialClass) ? e.MaterialClass : "building";
                    if (cls != e.MaterialClass) warnings.Add($"palette: material class '{e.MaterialClass}' of {e.Token.Value} unknown, using building");
                    if (e.Seasonal)
                    {
                        if (veg is null) { warnings.Add($"palette: seasonal family '{family}' has no vegetation anchors"); break; }
                        _seasonal.Add(new SeasonalFamily(id, e.Row ?? throw new InvalidDataException($"{e.Token.Value} needs a row"), veg));
                    }
                    else _pairs.Add(new Pair(id, family, cls, FromSrgb(e.Albedo), veg));
                    break;
                default:
                    throw new InvalidDataException($"Unknown palette role {e.Role}.");
            }
        }
        foreach (var token in script.Data.FrameTokens)
            if (!_frame.Any(f => f.Token == token)) warnings.Add($"palette: registry has no column for frame token '{token}'");
        Warnings = warnings;
    }

    private static Lab FromSrgb(Vector3 c) => Oklab.FromSrgb(c.X, c.Y, c.Z);

    /// <summary>Linear RGB (ColorScriptSnapshot.FogSun / FogAnti are linear, unlike the sRGB palette bytes).</summary>
    private static Vector3 LinearRgb(Lab c)
    {
        var (r, g, b) = Oklab.ToSrgbClipped(c);
        return new Vector3((float)Oklab.SrgbToLinear(r), (float)Oklab.SrgbToLinear(g), (float)Oklab.SrgbToLinear(b));
    }

    private void Put(int row, int col, Lab c)
    {
        var (r, g, b) = Oklab.ToSrgbClipped(c);
        var i = (row * Width + col) * 4;
        Pixels[i] = Oklab.ToByte(r);
        Pixels[i + 1] = Oklab.ToByte(g);
        Pixels[i + 2] = Oklab.ToByte(b);
        Pixels[i + 3] = 255;
    }

    /// <summary>One of the four colour-ID rows (current, lamp, interior shade, interior lit) of a logical colour ID 0–511.</summary>
    private void PutFrame(int id, int frameRow, Lab c)
    {
        var (col, row) = PaletteLayout.FrameTexel(new PaletteIndex(id), frameRow);
        Put(row, col, c);
    }

    private Lab FrameColor(string token, Lab constant, WorldFrame w)
    {
        var f = w.Frame;
        var d = _script.Deriver;
        switch (token)
        {
            case "sun.light": return f.Color(Key);
            case "ambient.sky": { var (c, h) = f.Ambient; return Oklab.FromLch(d.SwatchLAmbient, c, h); }
            case "interior.warm": { var (c, h) = f.InteriorWarm; return Oklab.FromLch(d.SwatchLInterior, c, h); }
            case "line.warm": return f.Color(LineWarm);
            case "line.cool": return f.Color(LineCool);
            case "fog.sun": return f.Color(FogSun);
            case "fog.anti": return f.Color(FogAnti);
            case "shadow.1": return d.Material(_shadowReference, "building", f, w.X, null).Shade;
            case "weather.washedSky": { var z = f.Sky(true, 4); return Oklab.FromLch(z.L, z.Chroma + 0.03, z.Hue); }
            case "weather.overcast":
                {
                    var tokens = _script.Data.Weather.Tokens;
                    if (w.SeasonA == Season.Winter) return tokens.SnowOvercast;
                    return Oklab.FromLch(tokens.Overcast.L, tokens.Overcast.C, tokens.Overcast.H);
                }
            case "weather.nightRain":
                {
                    var o = _script.Data.Weather.Tokens.NightRain;
                    return Oklab.FromLch(o.L, o.C, o.H);
                }
        }
        if (token.StartsWith("sky.sun.", StringComparison.Ordinal)) return f.Sky(true, token[^1] - '0');
        if (token.StartsWith("sky.anti.", StringComparison.Ordinal)) return f.Sky(false, token[^1] - '0');
        foreach (var (t, index) in ColorTokens) if (t == token) return f.Color(index);
        return constant;   // window.*, weather constants, emissive
    }

    /// <summary>Material colours of a vegetation family under the current frame, season-blended (no per-plant phase).</summary>
    private MaterialColors VegetationNow(VegetationSet.Entry veg, WorldFrame w)
    {
        var d = _script.Deriver;
        var ca = d.Material(veg.Albedo[(int)w.SeasonA], veg.Class, w.Frame, w.X, veg.Hero[(int)w.SeasonA], w.VegChromaMul);
        if (w.SeasonT <= 1e-9) return ca;
        var cb = d.Material(veg.Albedo[(int)w.SeasonB], veg.Class, w.Frame, w.X, veg.Hero[(int)w.SeasonB], w.VegChromaMul);
        return new(Blend(ca.Lit, cb.Lit, w.SeasonT), Blend(ca.Shade, cb.Shade, w.SeasonT),
            Blend(ca.ShadeIn, cb.ShadeIn, w.SeasonT), Blend(ca.LitIn, cb.LitIn, w.SeasonT));
    }

    /// <summary>Season-axis colour blend: OKLCH when both ends are chromatic (green → yellow-green → gold, SYS-TIME), else OKLab.</summary>
    private static Lab Blend(Lab a, Lab b, double t) =>
        a.Chroma > 0.02 && b.Chroma > 0.02 ? Oklab.LerpLch(a, b, t) : Oklab.Lerp(a, b, t);

    /// <summary>Rows 0–3: frame tokens, surface pairs, seasonal families at the current global season.</summary>
    public void ComposeCurrent(WorldFrame w)
    {
        var d = _script.Deriver;
        foreach (var (id, token, constant) in _frame) PutFrame(id, 0, FrameColor(token, constant, w));
        foreach (var p in _pairs)
        {
            var m = p.Veg is { } veg
                ? VegetationNow(veg, w)
                : d.Material(p.Albedo, p.Class, w.Frame, w.X, null);
            var lampAlbedo = p.Albedo;
            if (p.Variant is { } variant)
            {
                m = VegetationSet.Offset(m, variant);
                var a = p.Veg!.Albedo[(int)w.SeasonA];
                lampAlbedo = Oklab.FromLch(a.L + variant.DL, a.Chroma, a.Hue + variant.Dh);
            }
            WritePair(p.LitId, m, d.LampLit(lampAlbedo, _lampColor));
        }
        foreach (var s in _seasonal)
        {
            var m = VegetationNow(s.Veg, w);
            WritePair(s.LitId, m, d.LampLit(s.Veg.Albedo[(int)w.SeasonA], _lampColor));
        }
    }

    private void WritePair(int litId, MaterialColors m, Lab lamp)
    {
        PutFrame(litId, 0, m.Lit);
        PutFrame(litId + 1, 0, m.Shade);
        for (var c = litId; c <= litId + 1; c++)
        {
            PutFrame(c, 1, lamp);
            PutFrame(c, 2, m.ShadeIn);
            PutFrame(c, 3, m.LitIn);
        }
    }

    public int SeasonalCount => _seasonal.Count;

    /// <summary>Refreshes seasonal rows: one family per call (amortised, ≤ 2 rows per frame), or all when full.</summary>
    public void StepSeasonal(WorldFrame w, bool full)
    {
        if (_seasonal.Count == 0) return;
        var n = full ? _seasonal.Count : 1;
        for (var k = 0; k < n; k++)
        {
            UpdateFamily(_seasonal[_cursor], w);
            _cursor = (_cursor + 1) % _seasonal.Count;
        }
    }

    private void UpdateFamily(SeasonalFamily fam, WorldFrame w)
    {
        var d = _script.Deriver;
        Span<Lab> lit = stackalloc Lab[4];
        Span<Lab> shade = stackalloc Lab[4];
        for (var s = 0; s < 4; s++)
        {
            var m = d.Material(fam.Veg.Albedo[s], fam.Veg.Class, w.Frame, w.X, fam.Veg.Hero[s], w.VegChromaMul);
            lit[s] = m.Lit;
            shade[s] = m.Shade;
        }
        for (var i = 0; i < PaletteLayout.SeasonSampleCount; i++)
        {
            var sp = (i + 0.5) * 4.0 / PaletteLayout.SeasonSampleCount;
            var a = (int)Math.Floor(sp) % 4;
            var b = (a + 1) % 4;
            var t = ScalarMath.Smootherstep(sp - Math.Floor(sp));
            Put(fam.Row, i, Blend(lit[a], lit[b], t));
            Put(fam.Row + 1, i, Blend(shade[a], shade[b], t));
        }
    }

    public ColorScriptSnapshot Snapshot(WorldFrame w, long rev)
    {
        var v = w.Frame.V;
        var tint = new Vector3((float)v[VignetteTint], (float)v[VignetteTint + 1], (float)v[VignetteTint + 2]);
        return new ColorScriptSnapshot(
            Rev: rev,
            LineDarkness: (float)v[LineDarken],
            Glow: (float)v[GlowBloom],
            Vignette: (float)v[Vignette],
            Exposure: (float)v[ExposureEv],
            Stars: (float)v[Stars],
            CloudCover: (float)v[CloudAmount],
            CdlSlope: new Vector3((float)v[CdlSlope]),
            CdlOffset: new Vector3((float)v[CdlOffset]),
            CdlPower: new Vector3((float)v[CdlPower]),
            FogLow: new Vector4((float)v[FogLowTop], (float)v[FogLowDensity], (float)v[FogLake], 0f),
            CdlSaturation: (float)v[CdlSaturation],
            GlowHdrThreshold: (float)v[GlowHdrThreshold],
            GlowHdrScale: (float)v[GlowHdrScale],
            GlowIntensity: (float)v[GlowIntensity],
            VignetteTint: tint,
            Fog: new Vector4((float)v[FogDensity], (float)v[HazeDeg], (float)v[FogStart], (float)v[FogSilhouette]))
        {
            LampPoolGain = (float)v[LampPoolGain],
            FogSun = LinearRgb(w.Frame.Color(FogSun)),
            FogAnti = LinearRgb(w.Frame.Color(FogAnti))
        };
    }
}
