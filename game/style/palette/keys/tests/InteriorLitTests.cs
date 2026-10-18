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
    private const double LitLMax = 0.90, LitGain = 1.27;

    // Hand-registered references: equip.fireRed is WP-12's metal albedo (back-solved from the reviewed autumn 17:30 lit #C8473B).
    private static readonly (string Token, string Class, double R, double G, double B) FireRed = ("equip.fireRed", "metal", 0.656184, 0.259853, 0.295443);
    private static readonly string[] HighChroma = ["brick.lit", "band.terracotta.lit", "uestc.blue.lit"];
    private static readonly string[] LowChroma = ["roof.grey.lit", "frame.alu.lit", "stone.beige.lit", "wall.lit", "glass.lit", "roof.silverMetal.lit"];

    [TestCase]
    public void HighChromaPatchKeepsChromaAndCapsLightness()
    {
        var (deriver, frame, x) = GoldenFrame();
        foreach (var (token, albedo, cls) in Materials(["equip.fireRed", .. HighChroma]))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            var patch = deriver.InteriorComposite(m.ShadeIn, m.LitIn);
            AssertThat(patch.L <= LitLMax + 1e-6).OverrideFailureMessage($"{token}: patch L {patch.L:F4} exceeds {LitLMax}.").IsTrue();
            AssertThat(patch.L > m.Lit.L).OverrideFailureMessage($"{token}: patch L {patch.L:F4} must be lifted above the lit L {m.Lit.L:F4}.").IsTrue();
            AssertThat(patch.Chroma <= m.Lit.Chroma + 1e-6).OverrideFailureMessage($"{token}: patch C {patch.Chroma:F4} exceeds the lit C {m.Lit.Chroma:F4}.").IsTrue();
            AssertThat(Math.Abs(Oklab.HueDiff(m.Lit.Hue, patch.Hue)) < 0.01).OverrideFailureMessage($"{token}: hue moved from {m.Lit.Hue:F3} to {patch.Hue:F3}.").IsTrue();
            AssertThat(m.LitIn.L <= LitLMax + 1e-6).OverrideFailureMessage($"{token}: row 3 L {m.LitIn.L:F4} exceeds {LitLMax}.").IsTrue();
        }
    }

    [TestCase]
    public void LowChromaPatchOnlyRaisesLightness()
    {
        var (deriver, frame, x) = GoldenFrame();
        foreach (var (token, albedo, cls) in Materials(LowChroma))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            var patch = deriver.InteriorComposite(m.ShadeIn, m.LitIn);
            AssertThat(patch.L).OverrideFailureMessage($"{$"{token}: L"}: expected {Math.Min(LitLMax, m.Lit.L * LitGain):R}, got {patch.L:R}.").IsEqualApprox(Math.Min(LitLMax, m.Lit.L * LitGain), 1e-4);
            AssertThat(patch.Chroma).OverrideFailureMessage($"{$"{token}: C"}: expected {m.Lit.Chroma:R}, got {patch.Chroma:R}.").IsEqualApprox(m.Lit.Chroma, 1e-4);
            AssertThat(Math.Abs(Oklab.HueDiff(m.Lit.Hue, patch.Hue)) < 0.05).OverrideFailureMessage($"{token}: hue moved from {m.Lit.Hue:F3} to {patch.Hue:F3}.").IsTrue();
        }
    }

    [TestCase]
    public void RowThreeCompositesOntoThePatchAndStaysInGamut()
    {
        var (deriver, frame, x) = GoldenFrame();
        foreach (var (token, albedo, cls) in Materials(["equip.fireRed", .. HighChroma, .. LowChroma]))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            var patch = deriver.InteriorPatch(m.Lit);
            var composite = deriver.InteriorComposite(m.ShadeIn, m.LitIn);
            AssertThat(Dist(composite, patch) < 1e-4).OverrideFailureMessage($"{token}: composite {Oklab.ToHex(composite)} is not the patch {Oklab.ToHex(patch)}.").IsTrue();
            var (r, g, b) = Oklab.ToLinear(m.LitIn);
            AssertThat(r >= -1e-6 && g >= -1e-6 && b >= -1e-6 && r <= 1 + 1e-6 && g <= 1 + 1e-6 && b <= 1 + 1e-6).OverrideFailureMessage($"{token}: row 3 is out of gamut.").IsTrue();
        }
    }

    // Same colours as the Python reference (pipeline/colorscript, default grid cell autumn 17:30): the two implementations agree.
    [TestCase]
    public void RowThreeMatchesTheReference()
    {
        var (deriver, frame, x) = GoldenFrame();
        (string Token, string Hex)[] expected =
        [
            ("equip.fireRed", "#ED6D5F"), ("brick.lit", "#F09D7D"), ("wall.lit", "#EBD0AC"), ("roof.grey.lit", "#BBAE93")
        ];
        foreach (var (token, albedo, cls) in Materials(expected.Select(e => e.Token)))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            var actual = Oklab.ToHex(m.LitIn);
            var want = expected.First(e => e.Token == token).Hex;
            for (var i = 1; i < 7; i += 2)
                AssertThat(Math.Abs(Convert.ToInt32(actual.Substring(i, 2), 16) - Convert.ToInt32(want.Substring(i, 2), 16)) <= 2).OverrideFailureMessage($"{token}: row 3 {actual}, reference {want}.").IsTrue();
        }
    }

    // Row 2 has no light-gain term: chroma only follows the shade rule (x1.1 capped at +chromaMulMax) and the interior.warm blend.
    [TestCase]
    public void InteriorShadeIsNotScaledByTheGain()
    {
        var (deriver, frame, x) = GoldenFrame();
        var cap = DeriveRoot().GetProperty("shade").GetProperty("chromaMulMax").GetDouble();
        var warmC = frame.InteriorWarm.C;
        foreach (var (token, albedo, cls) in Materials(["equip.fireRed", .. HighChroma, .. LowChroma]))
        {
            var m = deriver.Material(albedo, cls, frame, x, null);
            AssertThat(m.ShadeIn.Chroma <= Math.Max(albedo.Chroma + cap, warmC) + 1e-9).OverrideFailureMessage($"{token}: interior shade C {m.ShadeIn.Chroma:F4} grew past the shade rule.").IsTrue();
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
