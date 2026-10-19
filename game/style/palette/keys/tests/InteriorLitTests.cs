using Campus.Tests;
using System.Text.Json;
using Campus.Contracts;
using Campus.Style.Palette.Keys;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests.Style;

/// <summary>
/// ART-STYLE interior rows of the WP-37 colour script: the window-patch colour only raises L (L × gain, capped), keeps the
/// lit colour's C and hue, and palette row 3 stores the colour that toon's shade + g × (lit − shade) composite turns into it.
/// Pure .NET: reads the key data and the registry from the repository.
/// </summary>
[TestSuite]
public sealed class InteriorLitTests
{
    private static readonly JsonElement InteriorRule = DeriveRoot().GetProperty("interior");
    private static double LitLMax => InteriorRule.GetProperty("litLMax").GetDouble();
    private static double LitGain => InteriorRule.GetProperty("litGain").GetDouble();

    // Hand-registered references: equip.fireRed is WP-12's metal albedo (back-solved from the reviewed autumn 17:30 lit #C8473B).
    private static readonly (string Token, string Class, double R, double G, double B) FireRed = ("equip.fireRed", "metal", 0.656184, 0.259853, 0.295443);
    private static readonly string[] HighChroma = ["brick.lit", "band.terracotta.lit", "uestc.blue.lit"];
    private static readonly string[] LowChroma = ["roof.grey.lit", "frame.alu.lit", "stone.beige.lit", "wall.lit", "glass.lit", "roof.silverMetal.lit"];

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void HighChromaPatchKeepsChromaAndCapsLightness()
    {
        var (deriver, frame, x) = GoldenFrame();
        foreach (var (token, albedo, cls) in Materials(["equip.fireRed", .. HighChroma]))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            var patch = deriver.InteriorComposite(m.ShadeIn, m.LitIn);
            AssertThat(patch.L <= LitLMax + TestTolerance.FloatRelative).OverrideFailureMessage($"{token}: patch L {patch.L:F4} exceeds {LitLMax}.").IsTrue();
            AssertThat(patch.L > m.Lit.L).OverrideFailureMessage($"{token}: patch L {patch.L:F4} must be lifted above the lit L {m.Lit.L:F4}.").IsTrue();
            AssertThat(patch.Chroma <= m.Lit.Chroma + TestTolerance.FloatRelative).OverrideFailureMessage($"{token}: patch C {patch.Chroma:F4} exceeds the lit C {m.Lit.Chroma:F4}.").IsTrue();
            AssertThat(Math.Abs(Oklab.HueDiff(m.Lit.Hue, patch.Hue)) < TestTolerance.AngleDegrees).OverrideFailureMessage($"{token}: hue moved from {m.Lit.Hue:F3} to {patch.Hue:F3}.").IsTrue();
            AssertThat(m.LitIn.L <= LitLMax + TestTolerance.FloatRelative).OverrideFailureMessage($"{token}: row 3 L {m.LitIn.L:F4} exceeds {LitLMax}.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LowChromaPatchOnlyRaisesLightness()
    {
        var (deriver, frame, x) = GoldenFrame();
        foreach (var (token, albedo, cls) in Materials(LowChroma))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            var patch = deriver.InteriorComposite(m.ShadeIn, m.LitIn);
            AssertThat(patch.L).OverrideFailureMessage($"{$"{token}: L"}: expected {Math.Min(LitLMax, m.Lit.L * LitGain):R}, got {patch.L:R}.").IsEqualApprox(Math.Min(LitLMax, m.Lit.L * LitGain), TestTolerance.FloatRelative);
            AssertThat(patch.Chroma).OverrideFailureMessage($"{$"{token}: C"}: expected {m.Lit.Chroma:R}, got {patch.Chroma:R}.").IsEqualApprox(m.Lit.Chroma, TestTolerance.FloatRelative);
            AssertThat(Math.Abs(Oklab.HueDiff(m.Lit.Hue, patch.Hue)) < TestTolerance.AngleDegrees).OverrideFailureMessage($"{token}: hue moved from {m.Lit.Hue:F3} to {patch.Hue:F3}.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RowThreeCompositesOntoThePatchAndStaysInGamut()
    {
        var (deriver, frame, x) = GoldenFrame();
        foreach (var (token, albedo, cls) in Materials(["equip.fireRed", .. HighChroma, .. LowChroma]))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            var patch = deriver.InteriorPatch(m.Lit);
            var composite = deriver.InteriorComposite(m.ShadeIn, m.LitIn);
            AssertThat(Dist(composite, patch) < TestTolerance.FloatRelative).OverrideFailureMessage($"{token}: composite {Oklab.ToHex(composite)} is not the patch {Oklab.ToHex(patch)}.").IsTrue();
            var (r, g, b) = Oklab.ToLinear(m.LitIn);
            AssertThat(r >= -TestTolerance.FloatRelative && g >= -TestTolerance.FloatRelative && b >= -TestTolerance.FloatRelative && r <= 1 + TestTolerance.FloatRelative && g <= 1 + TestTolerance.FloatRelative && b <= 1 + TestTolerance.FloatRelative).OverrideFailureMessage($"{token}: row 3 is out of gamut.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void InteriorShadeKeepsPatchLightnessAndMaterialIdentity()
    {
        var (deriver, frame, x) = GoldenFrame();
        var neutralEdges = DeriveRoot().GetProperty("shade").GetProperty("neutralCEdge").EnumerateArray().Select(v => v.GetDouble()).ToArray();
        var neutralMax = neutralEdges.Min();
        var colouredMin = neutralEdges.Max();
        var lightnessRatio = InteriorRule.GetProperty("shadeLRatio").GetDouble();
        var hueMax = InteriorRule.GetProperty("shadeHueMax").GetDouble();
        foreach (var (token, albedo, cls) in Materials(["equip.fireRed", .. HighChroma, .. LowChroma]))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            var patch = deriver.InteriorPatch(m.Lit);
            AssertThat(m.ShadeIn.L + TestTolerance.FloatRelative >= patch.L * lightnessRatio).OverrideFailureMessage($"{token}: interior shade L {m.ShadeIn.L:F4} is below {lightnessRatio:P0} of window-patch L {patch.L:F4}.").IsTrue();
            if (albedo.Chroma <= neutralMax)
            {
                AssertThat(m.ShadeIn.Chroma <= neutralMax + TestTolerance.FloatRelative).OverrideFailureMessage($"{token}: neutral interior shade C {m.ShadeIn.Chroma:F4} exceeds {neutralMax:F4}.").IsTrue();
            }
            else if (albedo.Chroma >= colouredMin)
            {
                AssertThat(m.ShadeIn.Chroma <= albedo.Chroma + TestTolerance.FloatRelative).OverrideFailureMessage($"{token}: interior shade C {m.ShadeIn.Chroma:F4} exceeds albedo C {albedo.Chroma:F4}.").IsTrue();
                AssertThat(Math.Abs(Oklab.HueDiff(albedo.Hue, m.ShadeIn.Hue)) <= hueMax + TestTolerance.AngleDegrees).OverrideFailureMessage($"{token}: interior shade hue moved more than {hueMax} degrees from the material hue.").IsTrue();
                var (r, g, b) = Oklab.ToLinear(Oklab.FromLch(m.ShadeIn.L, albedo.Chroma, m.ShadeIn.Hue));
                if (r >= 0 && g >= 0 && b >= 0 && r <= 1 && g <= 1 && b <= 1)
                {
                    AssertThat(m.ShadeIn.Chroma).OverrideFailureMessage($"{token}: in-gamut interior shade must retain albedo chroma.").IsEqualApprox(albedo.Chroma, TestTolerance.FloatRelative);
                }
            }
        }
    }

    private static (Deriver Deriver, FrameVector Frame, double X) GoldenFrame()
    {
        var keys = Path.Combine(Campus.Tests.RepositoryFixture.Root, "game", "style", "palette", "keys");
        byte[] Read(string name) => File.ReadAllBytes(Path.Combine(keys, name));
        var script = new ColorScript(ColorScriptData.Load(Read("autumn.keys.json"), Read("derive.json"), Read("seasons.json"),
            Read("vegetation.json"), Read("weather.json")));
        var world = script.EvaluateWorld(17.5, (double)Season.Autumn, null, default(WeatherVec));
        return (script.Deriver, world.Frame, world.X);
    }

    private static JsonElement DeriveRoot() =>
        JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Campus.Tests.RepositoryFixture.Root, "game", "style", "palette", "keys", "derive.json"))).RootElement;

    private static IEnumerable<(string Token, Lab Albedo, string Class)> Materials(IEnumerable<string> tokens)
    {
        var core = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Campus.Tests.RepositoryFixture.Root, "game", "style", "palette", "core.json"))).RootElement.GetProperty("entries");
        foreach (var token in tokens)
        {
            if (token == FireRed.Token)
            {
                yield return (token, Oklab.FromSrgb(FireRed.R, FireRed.G, FireRed.B), FireRed.Class);
                continue;
            }
            var entry = core.EnumerateArray().First(e => e.GetProperty("token").GetString() == token);
            var c = entry.GetProperty("albedo").EnumerateArray().Select(v => v.GetDouble()).ToArray();
            yield return (token, Oklab.FromSrgb(c[0], c[1], c[2]), entry.GetProperty("materialClass").GetString()!);
        }
    }

    private static double Dist(Lab a, Lab b) => Math.Sqrt((a.L - b.L) * (a.L - b.L) + (a.A - b.A) * (a.A - b.A) + (a.B - b.B) * (a.B - b.B));
}
