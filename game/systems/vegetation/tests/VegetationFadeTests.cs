using System.Globalization;
using System.Text.RegularExpressions;
using Campus.Gen.Vegetation;
using GdUnit4;

namespace Campus.Systems.Vegetation.Tests;

[TestSuite]
public sealed class VegetationFadeTests
{
    [TestCase]
    public void ShadersMirrorTheFadeConstants()
    {
        var shaders = Path.Combine(FindRepository(), "game", "style", "shaders");
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
        if (!m.Success) throw new InvalidOperationException($"{file}: {name} not found");
        var x = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var y = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        if (x != a || y != b) throw new InvalidOperationException($"{file}: {name} = ({x}, {y}), expected ({a}, {b})");
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }
}
