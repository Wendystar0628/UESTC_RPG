using Campus.Contracts;
using static Campus.Style.Palette.Keys.FrameLayout;

namespace Campus.Style.Palette.Keys;

public enum VegetationMode : byte { Hero, Albedo }

/// <summary>Reference light for vegetation defined by hero-frame lit colours (WLD-VEGETATION).</summary>
public sealed class HeroRef
{
    public required double KeyA { get; init; }
    public required double KeyB { get; init; }
    public required double SatGain { get; init; }
    public required VegetationMode Mode { get; init; }
    public double CRef { get; set; }
    public double HRef { get; set; }
    public double WarmRef { get; set; }
    /// <summary>Yellow-orange hero colour (lit.heroGuard): hue never past HRef towards green, chroma floor in dim light.</summary>
    public bool Guard { get; set; }
}

public readonly record struct MaterialColors(Lab Lit, Lab Shade, Lab ShadeIn, Lab LitIn);

/// <summary>Lit/shade derivation of ART-STYLE with the WP-37 rules in derive.json.</summary>
public sealed class Deriver
{
    private readonly record struct ClassParams(double RMat, double KC, double DhMax, bool Three);

    private readonly Dictionary<string, ClassParams> _classes = new(StringComparer.Ordinal);
    private readonly double _adaptRef, _tintK, _vegTintK, _capRatio, _capMinC, _chromaMulMax, _neutral0, _neutral1;
    private readonly double _yellowLo, _yellowHi, _greenLo, _greenHi;
    private readonly bool _scaleChroma;
    private readonly double _warmK, _warmAlt0, _warmAlt1, _warmC0, _warmC1, _hueMax, _albedoGain;
    private readonly double _guardLo, _guardHi, _guardMinC, _guardMirror, _guardFloor;
    private readonly double _rIn, _litGain, _litLMax, _shaderGain;
    public double SwatchLAmbient { get; }
    public double SwatchLInterior { get; }

    public Deriver(DeriveFile derive)
    {
        var lit = derive.Lit;
        _adaptRef = lit.AdaptRef;
        _tintK = lit.TintK;
        _vegTintK = lit.VegetationTintK;
        _capRatio = lit.ChromaCap.Ratio;
        _capMinC = lit.ChromaCap.MinAlbedoC;
        var warm = lit.VegetationWarmCap;
        _warmK = warm.K;
        (_warmAlt0, _warmAlt1) = (warm.AltDeg.First, warm.AltDeg.Second);
        (_warmC0, _warmC1) = (warm.KeyC.First, warm.KeyC.Second);
        _hueMax = warm.HueMax;
        _albedoGain = warm.AlbedoChromaGain;
        var guard = lit.HeroGuard;
        (_guardLo, _guardHi) = (guard.HueRange.First, guard.HueRange.Second);
        _guardMinC = guard.MinHeroC;
        _guardMirror = guard.WarmMirror;
        _guardFloor = guard.ChromaFloor;
        var shade = derive.Shade;
        _chromaMulMax = shade.ChromaMulMax;
        (_neutral0, _neutral1) = (shade.NeutralCEdge.First, shade.NeutralCEdge.Second);
        (_yellowLo, _yellowHi) = (shade.YellowGuard.First, shade.YellowGuard.Second);
        (_greenLo, _greenHi) = (shade.GreenGuard.First, shade.GreenGuard.Second);
        _scaleChroma = shade.ScaleChromaWithLight;
        _rIn = derive.Interior.RIn;
        _litGain = derive.Interior.LitGain;
        _litLMax = derive.Interior.LitLMax;
        _shaderGain = derive.Interior.ShaderGain;
        SwatchLAmbient = derive.SwatchL.AmbientSky;
        SwatchLInterior = derive.SwatchL.InteriorWarm;
        foreach (var (name, c) in derive.MaterialClasses) _classes[name] = new ClassParams(c.RMat, c.KC, c.DhMax, c.Steps == "three");
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
        hero.Guard = hero.CRef >= _guardMinC && hero.HRef >= _guardLo && hero.HRef <= _guardHi;
    }

    public double WarmLow(FrameVector f, double x) =>
        (1.0 - Oklab.Smoothstep(_warmAlt0, _warmAlt1, x)) * Oklab.Smoothstep(_warmC0, _warmC1, f.Color(Key).Chroma);

    private Lab CapVegetation(Lab lit, FrameVector f, double x, HeroRef hero)
    {
        var cmax = hero.CRef * (1.0 - _warmK * Math.Max(0.0, WarmLow(f, x) - hero.WarmRef));
        var h = lit.Hue;
        var d = Oklab.HueDiff(hero.HRef, h);
        if (Math.Abs(d) > _hueMax && hero.CRef > 1e-6) h = hero.HRef + Math.CopySign(_hueMax, d);
        // 黄橙英雄色不往绿越过英雄帧色相：往绿（色相增大）的那段按 warmMirror 折回暖侧
        if (hero.Guard && d > 0.0) h = hero.HRef - _guardMirror * Math.Min(d, _hueMax);
        return Oklab.FromLch(lit.L, Math.Min(lit.Chroma, cmax), h);
    }

    /// <summary>
    /// Chroma floor of yellow-orange hero colours (lit.heroGuard): dim or cool light keeps them reading as gold. The floor never
    /// passes this frame's chromaCap (shade C / ratio), so the shade keeps its identity share at every hour.
    /// </summary>
    private Lab FloorHeroChroma(Lab albedo, Lab lit, Lab shade, HeroRef hero)
    {
        var cmin = _guardFloor * hero.CRef;
        if (albedo.Chroma >= _capMinC) cmin = Math.Min(cmin, shade.Chroma / _capRatio);
        return lit.Chroma >= cmin ? lit : Oklab.FromLch(lit.L, cmin, lit.Hue);
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
        // 三阶类（植被、花）的影：明度差 1 − r × r_mat 再乘本帧的 vegDepth（室内行不乘）
        var l = mc.Three && rOverride is null ? lit.L * (1.0 - (1.0 - r * mc.RMat) * f.V[VegDepth]) : lit.L * r * mc.RMat;
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
        if (hero is { Guard: true }) lit = FloorHeroChroma(albedo, lit, shade, hero);
        if (vegChromaMul != 1.0 && hero is not null)
        {
            // 放大后越出色域的只降彩度（明度、色相不动），不交给写纹理时的逐通道截断；室内两行随后从放大后的受光色推导
            lit = Oklab.GamutMap(Oklab.FromLch(lit.L, lit.Chroma * vegChromaMul, lit.Hue));
            shade = Oklab.GamutMap(Oklab.FromLch(shade.L, shade.Chroma * vegChromaMul, shade.Hue));
        }
        var shadeIn = Shade(albedo, lit, f, cls, _rIn, f.InteriorWarm);
        var litIn = InteriorLit(lit, shadeIn);
        return new(lit, shade, shadeIn, litIn);
    }

    /// <summary>
    /// Window-patch target colour (ART-STYLE): only L is raised (L × litGain, capped at litLMax); C is the lit colour's C,
    /// not scaled by the gain, and the hue is unchanged. Out-of-gamut targets lose chroma only.
    /// </summary>
    public Lab InteriorPatch(Lab lit) =>
        Oklab.GamutMap(Oklab.FromLch(Math.Min(_litLMax, lit.L * _litGain), lit.Chroma, lit.Hue));

    /// <summary>
    /// Palette row 3. toon composites interior light as shade + g × (lit − shade) in linear RGB with g = interior_light.x, so the
    /// row stores shade + (target − shade) / g: with the default gain the composite lands exactly on <see cref="InteriorPatch"/>.
    /// </summary>
    public Lab InteriorLit(Lab lit, Lab shadeIn)
    {
        var (sr, sg, sb) = Oklab.ToLinear(Oklab.GamutMap(shadeIn));
        var (tr, tg, tb) = Oklab.ToLinear(InteriorPatch(lit));
        return Oklab.FromLinear(sr + (tr - sr) / _shaderGain, sg + (tg - sg) / _shaderGain, sb + (tb - sb) / _shaderGain);
    }

    /// <summary>What toon draws inside a full-strength window patch for the given rows 2 and 3 (default gain).</summary>
    public Lab InteriorComposite(Lab shadeIn, Lab litIn)
    {
        var (sr, sg, sb) = Oklab.ToLinear(Oklab.GamutMap(shadeIn));
        var (lr, lg, lb) = Oklab.ToLinear(litIn);
        return Oklab.FromLinear(sr + _shaderGain * (lr - sr), sg + _shaderGain * (lg - sg), sb + _shaderGain * (lb - sb));
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

    /// <summary>A colour that follows another vegetation token with an OKLCH offset applied after derivation (vegetation.json variants).</summary>
    public sealed record Variant(string Token, string Base, double DL, double Dh);

    public IReadOnlyDictionary<string, Entry> Entries { get; }
    public IReadOnlyDictionary<string, Variant> Variants { get; }

    /// <summary>Offsets every derived colour of the base token (lit, shade, interior rows) the same way, so shade follows lit.</summary>
    public static MaterialColors Offset(MaterialColors m, Variant v)
    {
        Lab O(Lab c) => Oklab.FromLch(c.L + v.DL, c.Chroma, c.Hue + v.Dh);
        return new(O(m.Lit), O(m.Shade), O(m.ShadeIn), O(m.LitIn));
    }

    public VegetationSet(ColorScriptData data, Deriver deriver, IReadOnlyList<FrameVector>[] seasonKeys)
    {
        var veg = data.Vegetation;
        var keyIndex = data.Keys.Select((k, i) => (k.Id, i)).ToDictionary(p => p.Id, p => p.i, StringComparer.Ordinal);
        (FrameVector Frame, double X) Hero(Season s)
        {
            var i = keyIndex[veg.HeroFrames.For(s)];
            return (seasonKeys[(int)s][i], data.Keys[i].AltDeg);
        }
        (Lab, HeroRef) Anchor(VegAnchor value, Season s)
        {
            var (frame, x) = Hero(s);
            Lab albedo;
            HeroRef hero;
            if (value.IsAlbedo)
            {
                albedo = value.Color;
                hero = deriver.AlbedoRef(frame);
            }
            else (albedo, hero) = deriver.InvertLit(value.Color, frame);
            deriver.CalibrateHero(albedo, hero, frame, x);
            return (albedo, hero);
        }
        var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (var v in veg.Seasonal)
        {
            var albedo = new Lab[4];
            var hero = new HeroRef[4];
            foreach (var s in Enum.GetValues<Season>())
                (albedo[(int)s], hero[(int)s]) = Anchor(v.Lit.For(s), s);
            entries[v.Token] = new Entry(v.Token, v.Class, albedo, hero, false);
        }
        foreach (var v in veg.Constant)
        {
            var season = Enum.Parse<Season>(v.HeroSeason, true);
            var (a, h) = Anchor(v.Lit, season);
            entries[v.Token] = new Entry(v.Token, v.Class, [a, a, a, a], [h, h, h, h], true);
        }
        Entries = entries;
        var variants = new Dictionary<string, Variant>(StringComparer.Ordinal);
        foreach (var v in veg.Variants ?? [])
        {
            if (!entries.ContainsKey(v.Base)) throw new InvalidDataException($"vegetation.json: variant {v.Token} follows unknown token {v.Base}.");
            variants[v.Token] = new Variant(v.Token, v.Base, v.DL, v.Dh);
        }
        Variants = variants;
    }
}
