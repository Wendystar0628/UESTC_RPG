using System.Text.Json;
using Campus.Contracts;
using static Campus.Style.Palette.Keys.FrameLayout;

namespace Campus.Style.Palette.Keys;

public enum VegetationMode : byte { Hero, Albedo }

/// <summary>Reference light for vegetation defined by hero-frame lit colours (WLD-VEGETATION §2.3).</summary>
public sealed class HeroRef
{
    public required double KeyA { get; init; }
    public required double KeyB { get; init; }
    public required double SatGain { get; init; }
    public required VegetationMode Mode { get; init; }
    public double CRef { get; set; }
    public double HRef { get; set; }
    public double WarmRef { get; set; }
}

public readonly record struct MaterialColors(Lab Lit, Lab Shade, Lab ShadeIn, Lab LitIn);

/// <summary>Lit/shade derivation of ART-STYLE §3.2, §3.4, §8.3 with the WP-37 rules in derive.json.</summary>
public sealed class Deriver
{
    private readonly record struct ClassParams(double RMat, double KC, double DhMax);

    private readonly Dictionary<string, ClassParams> _classes = new(StringComparer.Ordinal);
    private readonly double _adaptRef, _tintK, _vegTintK, _capRatio, _capMinC, _chromaMulMax, _neutral0, _neutral1;
    private readonly double _yellowLo, _yellowHi, _greenLo, _greenHi;
    private readonly bool _scaleChroma;
    private readonly double _warmK, _warmAlt0, _warmAlt1, _warmC0, _warmC1, _hueMax, _albedoGain;
    private readonly double _rIn, _litChromaMul, _orangeHue, _litHueMax;
    public double SwatchLAmbient { get; }
    public double SwatchLInterior { get; }

    public Deriver(JsonElement derive)
    {
        var lit = derive.GetProperty("lit");
        _adaptRef = lit.GetProperty("adaptRef").GetDouble();
        _tintK = lit.GetProperty("tintK").GetDouble();
        _vegTintK = lit.GetProperty("vegetationTintK").GetDouble();
        var cap = lit.GetProperty("chromaCap");
        _capRatio = cap.GetProperty("ratio").GetDouble();
        _capMinC = cap.GetProperty("minAlbedoC").GetDouble();
        var warm = lit.GetProperty("vegetationWarmCap");
        _warmK = warm.GetProperty("k").GetDouble();
        _warmAlt0 = warm.GetProperty("altDeg")[0].GetDouble();
        _warmAlt1 = warm.GetProperty("altDeg")[1].GetDouble();
        _warmC0 = warm.GetProperty("keyC")[0].GetDouble();
        _warmC1 = warm.GetProperty("keyC")[1].GetDouble();
        _hueMax = warm.GetProperty("hueMax").GetDouble();
        _albedoGain = warm.GetProperty("albedoChromaGain").GetDouble();
        var shade = derive.GetProperty("shade");
        _chromaMulMax = shade.GetProperty("chromaMulMax").GetDouble();
        _neutral0 = shade.GetProperty("neutralCEdge")[0].GetDouble();
        _neutral1 = shade.GetProperty("neutralCEdge")[1].GetDouble();
        _yellowLo = shade.GetProperty("yellowGuard")[0].GetDouble();
        _yellowHi = shade.GetProperty("yellowGuard")[1].GetDouble();
        _greenLo = shade.GetProperty("greenGuard")[0].GetDouble();
        _greenHi = shade.GetProperty("greenGuard")[1].GetDouble();
        _scaleChroma = shade.GetProperty("scaleChromaWithLight").GetBoolean();
        var interior = derive.GetProperty("interior");
        _rIn = interior.GetProperty("rIn").GetDouble();
        _litChromaMul = interior.GetProperty("litChromaMul").GetDouble();
        _orangeHue = interior.GetProperty("orangeHue").GetDouble();
        _litHueMax = interior.GetProperty("litHueMax").GetDouble();
        SwatchLAmbient = derive.GetProperty("swatchL").GetProperty("ambient.sky").GetDouble();
        SwatchLInterior = derive.GetProperty("swatchL").GetProperty("interior.warm").GetDouble();
        foreach (var p in derive.GetProperty("materialClasses").EnumerateObject())
            _classes[p.Name] = new ClassParams(p.Value.GetProperty("rMat").GetDouble(), p.Value.GetProperty("kC").GetDouble(), p.Value.GetProperty("dhMax").GetDouble());
    }

    public bool HasClass(string cls) => _classes.ContainsKey(cls);

    public double LightM(FrameVector f) => Math.Min(1.0, Math.Cbrt(f.V[Intensity] / _adaptRef));

    public Lab Lit(Lab albedo, FrameVector f, HeroRef? hero)
    {
        var v = f.V;
        var m = LightM(f);
        var l = m * (1.0 - Math.Pow(1.0 - Math.Clamp(albedo.L, 0.0, 1.0), v[LitLift]));
        double ka = v[Key + 1], kb = v[Key + 2];
        if (hero is null || hero.Mode == VegetationMode.Albedo)
            return new(l, m * (v[SatGain] * albedo.A + _tintK * ka), m * (v[SatGain] * albedo.B + _tintK * kb));
        var g = v[SatGain] / hero.SatGain;
        return new(l, m * (g * albedo.A + _vegTintK * (ka - hero.KeyA)), m * (g * albedo.B + _vegTintK * (kb - hero.KeyB)));
    }

    /// <summary>Vegetation albedo from a hero-frame lit colour: only lightness is inverted, (a, b) keep the lit value.</summary>
    public (Lab Albedo, HeroRef Hero) InvertLit(Lab lit, FrameVector heroFrame)
    {
        var v = heroFrame.V;
        var m = LightM(heroFrame);
        var la = 1.0 - Math.Pow(Math.Max(0.0, 1.0 - lit.L / m), 1.0 / v[LitLift]);
        return (new Lab(la, lit.A / m, lit.B / m),
            new HeroRef { KeyA = v[Key + 1], KeyB = v[Key + 2], SatGain = v[SatGain], Mode = VegetationMode.Hero });
    }

    public HeroRef AlbedoRef(FrameVector heroFrame) =>
        new() { KeyA = heroFrame.V[Key + 1], KeyB = heroFrame.V[Key + 2], SatGain = heroFrame.V[SatGain], Mode = VegetationMode.Albedo };

    /// <summary>Fills CRef/HRef/WarmRef from the hero frame (vegetationWarmCap).</summary>
    public void CalibrateHero(Lab albedo, HeroRef hero, FrameVector heroFrame, double heroX)
    {
        var lit = Lit(albedo, heroFrame, hero);
        hero.CRef = lit.Chroma;
        if (hero.Mode == VegetationMode.Albedo)
            hero.CRef = Math.Min(hero.CRef, albedo.Chroma * heroFrame.V[SatGain] * _albedoGain);
        hero.HRef = lit.Hue;
        hero.WarmRef = WarmLow(heroFrame, heroX);
    }

    public double WarmLow(FrameVector f, double x) =>
        (1.0 - Oklab.Smoothstep(_warmAlt0, _warmAlt1, x)) * Oklab.Smoothstep(_warmC0, _warmC1, f.Color(Key).Chroma);

    private Lab CapVegetation(Lab lit, FrameVector f, double x, HeroRef hero)
    {
        var cmax = hero.CRef * (1.0 - _warmK * Math.Max(0.0, WarmLow(f, x) - hero.WarmRef));
        var h = lit.Hue;
        var d = Oklab.HueDiff(hero.HRef, h);
        if (Math.Abs(d) > _hueMax && hero.CRef > 1e-6) h = hero.HRef + Math.CopySign(_hueMax, d);
        return Oklab.FromLch(lit.L, Math.Min(lit.Chroma, cmax), h);
    }

    private double RotateHue(double ha, double hs, double dhMax)
    {
        var d = Oklab.HueDiff(ha, hs);
        var step = Math.Min(dhMax, Math.Abs(d));
        if (ha >= _yellowLo && ha <= _yellowHi && d > 0.0) return ha - step;      // 黄、橙不往绿转
        if (ha > _greenLo && ha <= _greenHi && d < 0.0) return ha + step;         // 绿不往黄转
        return ha + Math.CopySign(step, d);
    }

    public Lab Shade(Lab albedo, Lab lit, FrameVector f, string cls, double? rOverride = null, (double C, double H)? ambientOverride = null)
    {
        var mc = _classes[cls];
        var r = rOverride ?? f.V[R];
        var (cs, hs) = ambientOverride ?? f.Ambient;
        var ca = albedo.Chroma;
        var l = lit.L * r * mc.RMat;
        var c1 = Math.Min(ca * mc.KC, ca + _chromaMulMax);
        if (_scaleChroma) c1 *= LightM(f);
        var h1 = ca > 1e-6 ? RotateHue(albedo.Hue, hs, mc.DhMax) : hs;
        var own = Oklab.FromLch(l, c1, h1);
        var w = Math.Max(f.V[WMin], Oklab.Smoothstep(_neutral0, _neutral1, ca));
        var sky = Oklab.FromLch(1.0, cs, hs);
        return new(l, Oklab.Lerp(own.A, sky.A, w), Oklab.Lerp(own.B, sky.B, w));
    }

    private Lab CapLitChroma(Lab albedo, Lab lit, Lab shade)
    {
        if (albedo.Chroma < _capMinC) return lit;
        var cmax = shade.Chroma / _capRatio;
        return lit.Chroma <= cmax ? lit : Oklab.FromLch(lit.L, cmax, lit.Hue);
    }

    public MaterialColors Material(Lab albedo, string cls, FrameVector f, double x, HeroRef? hero, double vegChromaMul = 1.0)
    {
        var lit = Lit(albedo, f, hero);
        if (hero is not null) lit = CapVegetation(lit, f, x, hero);
        var shade = Shade(albedo, lit, f, cls);
        lit = CapLitChroma(albedo, lit, shade);
        var shadeIn = Shade(albedo, lit, f, cls, _rIn, f.InteriorWarm);
        var d = Oklab.HueDiff(lit.Hue, _orangeHue);
        var h2 = lit.Chroma > 1e-6 ? lit.Hue + Math.CopySign(Math.Min(_litHueMax, Math.Abs(d)), d) : lit.Hue;
        var litIn = Oklab.FromLch(lit.L, lit.Chroma * _litChromaMul, h2);
        if (vegChromaMul != 1.0 && hero is not null)
        {
            lit = Oklab.FromLch(lit.L, lit.Chroma * vegChromaMul, lit.Hue);
            shade = Oklab.FromLch(shade.L, shade.Chroma * vegChromaMul, shade.Hue);
        }
        return new(lit, shade, shadeIn, litIn);
    }

    /// <summary>Lamp-pool colour (palette row 1, mainline placeholder): albedo under the warm window/lamp light.</summary>
    public Lab LampLit(Lab albedo, Lab lamp)
    {
        const double lampIntensityM = 0.95;     // (0.6 / adaptRef)^(1/3) at adaptRef 0.7
        var l = lampIntensityM * Math.Clamp(albedo.L, 0.0, 1.0);
        return new(l, lampIntensityM * (albedo.A + _tintK * lamp.A), lampIntensityM * (albedo.B + _tintK * lamp.B));
    }
}

/// <summary>Seasonal vegetation anchors (vegetation.json): per season an albedo and its hero reference.</summary>
public sealed class VegetationSet
{
    public sealed record Entry(string Token, string Class, Lab[] Albedo, HeroRef[] Hero, bool Constant);

    public IReadOnlyDictionary<string, Entry> Entries { get; }

    public VegetationSet(ColorScriptData data, Deriver deriver, IReadOnlyList<FrameVector>[] seasonKeys)
    {
        var heroes = data.Vegetation.GetProperty("heroFrames");
        var keyIndex = data.Keys.Select((k, i) => (k.Id, i)).ToDictionary(p => p.Id, p => p.i, StringComparer.Ordinal);
        (FrameVector Frame, double X) Hero(Season s)
        {
            var id = heroes.GetProperty(SeasonComposer.SeasonKey(s)).GetString()!;
            var i = keyIndex[id];
            return (seasonKeys[(int)s][i], data.Keys[i].AltDeg);
        }
        (Lab, HeroRef) Anchor(JsonElement value, Season s)
        {
            var (frame, x) = Hero(s);
            Lab albedo;
            HeroRef hero;
            if (value.ValueKind == JsonValueKind.Object)
            {
                albedo = Oklab.Parse(value.GetProperty("albedo").GetString()!);
                hero = deriver.AlbedoRef(frame);
            }
            else (albedo, hero) = deriver.InvertLit(Oklab.Parse(value.GetString()!), frame);
            deriver.CalibrateHero(albedo, hero, frame, x);
            return (albedo, hero);
        }
        var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (var v in data.Vegetation.GetProperty("seasonal").EnumerateArray())
        {
            var albedo = new Lab[4];
            var hero = new HeroRef[4];
            foreach (var s in Enum.GetValues<Season>())
                (albedo[(int)s], hero[(int)s]) = Anchor(v.GetProperty("lit").GetProperty(SeasonComposer.SeasonKey(s)), s);
            var token = v.GetProperty("token").GetString()!;
            entries[token] = new Entry(token, v.GetProperty("class").GetString()!, albedo, hero, false);
        }
        foreach (var v in data.Vegetation.GetProperty("constant").EnumerateArray())
        {
            var season = Enum.Parse<Season>(v.GetProperty("heroSeason").GetString()!, true);
            var (a, h) = Anchor(v.GetProperty("lit"), season);
            var token = v.GetProperty("token").GetString()!;
            entries[token] = new Entry(token, v.GetProperty("class").GetString()!, [a, a, a, a], [h, h, h, h], true);
        }
        Entries = entries;
    }
}
