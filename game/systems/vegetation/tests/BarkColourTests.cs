using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Vegetation;
using Campus.Style.Palette.Keys;
using GdUnit4;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>
/// The bark pair (tree.bark, colour IDs 252–253, ext/WP-23.json) evaluated by the WP-37 colour script. The max-011 reviews
/// set two lit targets, autumn 17:30 #8A7B70 and noon #8C8076 (warm grey-brown, low chroma), and asked for the albedo that
/// best serves both: the registered albedo is the least-squares OKLab compromise, so each frame lands near its target
/// (17:30 a little warmer) and the shade is whatever ART-STYLE §3 derives from it.
/// </summary>
[TestSuite]
public sealed class BarkColourTests
{
    /// <summary>Largest OKLab distance from each review target that still counts as the compromise.</summary>
    private const double Tolerance = 0.02;

    [TestCase]
    public void BarkLitServesBothReviewFrames()
    {
        var (script, registry) = Load();
        var composer = new PaletteComposer(script, registry);
        foreach (var (clock, target) in new[] { (17.5, "#8A7B70"), (12.0, "#8C8076") })
        {
            composer.ComposeCurrent(script.EvaluateWorld(clock, (double)Season.Autumn, null, default));
            var lit = Hex(composer, registry, MotherModels.Bark);
            var litLab = Oklab.Parse(lit);
            var want = Oklab.Parse(target);
            var d = Math.Sqrt((litLab.L - want.L) * (litLab.L - want.L) + (litLab.A - want.A) * (litLab.A - want.A) + (litLab.B - want.B) * (litLab.B - want.B));
            if (d > Tolerance) throw new InvalidOperationException($"tree.bark.lit at autumn {clock} is {lit}, {d:F3} from the review's {target}");
            // Warm grey-brown: red over blue at every frame, never the neutral cool grey the first albedo gave at noon.
            if (!(litLab.A > 0.0 && litLab.B >= 0.0)) throw new InvalidOperationException($"tree.bark.lit at autumn {clock} is {lit}, not warm");
            var shade = Oklab.Parse(Hex(composer, registry, MotherModels.BarkShade));
            // ART-STYLE §3: shade by lightness (r 0.78 on the default frame), never darker than the trunk reads as wood.
            if (shade.L >= litLab.L * 0.88 || shade.L <= litLab.L * 0.65)
                throw new InvalidOperationException($"tree.bark shade L {shade.L:F3} against lit L {litLab.L:F3} at {clock}");
        }
    }

    private static string Hex(PaletteComposer composer, PaletteRegistry registry, PaletteToken token)
    {
        var id = registry.Get(token).Id;
        var p = composer.Pixels;
        return $"#{p[id * 4]:X2}{p[id * 4 + 1]:X2}{p[id * 4 + 2]:X2}";
    }

    private static (ColorScript, PaletteRegistry) Load()
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
        return (script, new PaletteRegistry(entries));
    }
}
