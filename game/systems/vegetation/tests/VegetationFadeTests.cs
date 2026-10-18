using Campus.Tests;
using System.Globalization;
using System.Text.RegularExpressions;
using Campus.Gen.Vegetation;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Systems.Vegetation.Tests;

[TestSuite]
public sealed class VegetationFadeTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ShadersMirrorTheFadeConstants()
    {
        var shaders = Campus.Tests.RepositoryFixture.FilePath("game", "style", "shaders");
        foreach (var user in new[] { "veg_card.gdshader", "veg_trunk.gdshader" })
            if (!File.ReadAllText(Path.Combine(shaders, user)).Contains("veg_common.gdshaderinc", StringComparison.Ordinal))
                throw new InvalidOperationException($"{user} must include veg_common.gdshaderinc");
        {
            const string name = "veg_common.gdshaderinc";
            var text = File.ReadAllText(Path.Combine(shaders, name));
            ExpectVec2(text, "VEG_POOL_FADE", VegFade.NearFadeStart, VegFade.NearFadeEnd, name);
            ExpectVec2(text, "VEG_CLOUD_FADE", VegFade.CloudFadeStart, VegFade.CloudFadeEnd, name);
            ExpectVec2(text, "VEG_UNDER_FADE", Understorey.FadeStart, Understorey.DrawDistance, name);
            if (!Regex.IsMatch(text, $@"const bool VEG_CLOUD_READY = {(VegFade.CloudReady ? "true" : "false")};"))
                throw new InvalidOperationException($"{name}: VEG_CLOUD_READY differs from VegFade.CloudReady");
            if (!Regex.IsMatch(text, $@"const int VEG_VARIANT_UNDER = {Understorey.UnderVariantBit};"))
                throw new InvalidOperationException($"{name}: VEG_VARIANT_UNDER differs from Understorey.UnderVariantBit");
        }
    }

    private static void ExpectVec2(string text, string name, float a, float b, string file)
    {
        var m = Regex.Match(text, $@"const vec2 {name} = vec2\(([-0-9.]+), ([-0-9.]+)\);");
        AssertThat(m.Success).OverrideFailureMessage($"{file}: {name} not found").IsTrue();
        if (!m.Success) return;
        var x = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var y = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        AssertThat(x == a && y == b).OverrideFailureMessage($"{file}: {name} = ({x}, {y}), expected ({a}, {b})").IsTrue();
    }
}
