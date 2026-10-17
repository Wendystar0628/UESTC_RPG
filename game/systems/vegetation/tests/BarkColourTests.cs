using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Vegetation;
using Campus.Style.Palette.Keys;
using GdUnit4;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>
/// The bark pair (tree.bark, colour IDs 252–253, ext/WP-23.json) evaluated by the WP-37 colour script. The albedo has OKLab
/// chroma ~0.048, hue ~62: below ~0.06 the script treats a material as neutral and mixes its shade towards ambient.sky
/// (ART-STYLE, w = smoothstep(0.06, 0.015, C)), which painted the trunks under the 17:30 canopy blue-violet (0q). With
/// that chroma the shade keeps the bark hue. Lit targets are the 0q renders: autumn 17:30 #9F703E and noon #9F7E5E.
/// </summary>
[TestSuite]
public sealed class BarkColourTests
{
    /// <summary>Largest OKLab distance from each review target that still counts as the compromise.</summary>
    private const double Tolerance = 0.02;

    [TestCase]
    public void BarkLitServesBothReviewFrames()
    {
        var (script, catalog) = Load();
        var composer = new PaletteComposer(script, catalog);
        foreach (var (clock, target) in new[] { (17.5, "#9F703E"), (12.0, "#9F7E5E") })
        {
            composer.ComposeCurrent(script.EvaluateWorld(clock, (double)Season.Autumn, null, default));
            var lit = Hex(composer, catalog, MotherModels.Bark);
            var litLab = Oklab.Parse(lit);
            var want = Oklab.Parse(target);
            var d = Math.Sqrt((litLab.L - want.L) * (litLab.L - want.L) + (litLab.A - want.A) * (litLab.A - want.A) + (litLab.B - want.B) * (litLab.B - want.B));
            if (d > Tolerance) throw new InvalidOperationException($"tree.bark.lit at autumn {clock} is {lit}, {d:F3} from the review's {target}");
            // Warm grey-brown: red over blue at every frame, never the neutral cool grey the first albedo gave at noon.
            if (!(litLab.A > 0.0 && litLab.B >= 0.0)) throw new InvalidOperationException($"tree.bark.lit at autumn {clock} is {lit}, not warm");
            var shade = Oklab.Parse(Hex(composer, catalog, MotherModels.BarkShade));
            // The shade keeps the bark's own warm hue (0q): never pulled to the sky's blue-violet, and not grey.
            var shadeHue = Math.Atan2(shade.B, shade.A) * 180.0 / Math.PI;
            var shadeChroma = Math.Sqrt(shade.A * shade.A + shade.B * shade.B);
            if (shadeHue < 35.0 || shadeHue > 85.0 || shadeChroma < 0.025)
                throw new InvalidOperationException($"tree.bark shade at {clock} has hue {shadeHue:F0}, chroma {shadeChroma:F3}; want a warm 35–85 hue and C >= 0.025");
            // ART-STYLE: shade by lightness (r 0.78 on the default frame), never darker than the trunk reads as wood.
            if (shade.L >= litLab.L * 0.88 || shade.L <= litLab.L * 0.65)
                throw new InvalidOperationException($"tree.bark shade L {shade.L:F3} against lit L {litLab.L:F3} at {clock}");
        }
    }

    private static string Hex(PaletteComposer composer, PaletteCatalog catalog, PaletteToken token)
    {
        var (col, row) = PaletteLayout.FrameTexel(catalog.Resolve(token), 0);
        var offset = (row * PaletteLayout.Width + col) * 4;
        var p = composer.Pixels;
        return $"#{p[offset]:X2}{p[offset + 1]:X2}{p[offset + 2]:X2}";
    }

    private static (ColorScript, PaletteCatalog) Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ownership.json"))) dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("Repository root not found from the test directory.");
        var palette = Path.Combine(dir.FullName, "game", "style", "palette");
        byte[] Key(string name) => File.ReadAllBytes(Path.Combine(palette, "keys", name));
        var script = new ColorScript(ColorScriptData.Load(Key("autumn.keys.json"), Key("derive.json"), Key("seasons.json"),
            Key("vegetation.json"), Key("weather.json")));
        var entries = new List<PaletteEntry>();
        var files = new[] { Path.Combine(palette, "core.json") }
            .Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "WP-*.json").Order(StringComparer.Ordinal));
        foreach (var file in files)
            entries.AddRange(JsonSerializer.Deserialize<PaletteRegistryData>(File.ReadAllBytes(file), ContractJson.Options)!.Entries);
        return (script, new PaletteCatalog(new PaletteRegistry(entries), []));
    }
}
