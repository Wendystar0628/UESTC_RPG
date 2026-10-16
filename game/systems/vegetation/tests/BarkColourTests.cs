using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Vegetation;
using Campus.Style.Palette.Keys;
using GdUnit4;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>
/// The bark pair (tree.bark, colour IDs 252–253, ext/WP-23.json) evaluated by the WP-37 colour script: the max-011 review
/// fixed the lit colour on the autumn 17:30 frame at #8A7B70, so the registered albedo is the inverse of that frame and
/// the shade is whatever ART-STYLE §3 derives from it.
/// </summary>
[TestSuite]
public sealed class BarkColourTests
{
    [TestCase]
    public void BarkLitMatchesTheReviewAtAutumnGoldenHour()
    {
        var (script, registry) = Load();
        var composer = new PaletteComposer(script, registry);
        composer.ComposeCurrent(script.EvaluateWorld(17.5, (double)Season.Autumn, null, default));
        var lit = Hex(composer, registry, MotherModels.Bark);
        if (lit != "#8A7B70") throw new InvalidOperationException($"tree.bark.lit at autumn 17:30 is {lit}, review wants #8A7B70");
        var shade = Oklab.Parse(Hex(composer, registry, MotherModels.BarkShade));
        var litLab = Oklab.Parse(lit);
        // ART-STYLE §3: shade by lightness (r 0.78 on the default frame), never darker than the trunk reads as wood.
        if (shade.L >= litLab.L * 0.85 || shade.L <= litLab.L * 0.65)
            throw new InvalidOperationException($"tree.bark shade L {shade.L:F3} against lit L {litLab.L:F3}");
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
