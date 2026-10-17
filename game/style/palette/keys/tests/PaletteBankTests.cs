using System.Text.Json;
using Campus.Contracts;
using Campus.Style.Palette.Keys;
using GdUnit4;

namespace Campus.Style.Palette.Keys.Tests;

/// <summary>
/// The catalog fills bank-0 frame and seasonal rows plus bank-1 pairs in the 256×68 layout. The extension pairs here are
/// test entries at the bank edges (256/257, 510/511) plus one per material class in between, not the registered ext-v2 table.
/// </summary>
[TestSuite]
public sealed class PaletteBankTests
{
    [TestCase]
    public void CatalogFillsBothBanksAndSeasonalRows()
    {
        var (script, catalog) = Load();
        var full = new PaletteComposer(script, catalog);
        Check(full.Pixels.Length == PaletteLayout.ByteCount, "The palette must be 256×68 RGBA8.");
        foreach (var (clock, season) in new[] { (17.5, Season.Autumn), (12.0, Season.Summer), (6.0, Season.Spring), (21.0, Season.Winter) })
        {
            var w = script.EvaluateWorld(clock, (double)season, null, default);
            full.ComposeCurrent(w);
            full.StepSeasonal(w, full: true);
            foreach (var e in catalog.Entries.Where(e => e.Role != PaletteRole.Retired))
            {
                var rows = e.Role is PaletteRole.Lit or PaletteRole.Shade ? PaletteLayout.FrameRowCount : 1;
                for (var row = 0; row < rows; row++)
                {
                    var (col, texelRow) = PaletteLayout.FrameTexel(e.Index, row);
                    var expectedRow = e.Index.Value < PaletteLayout.LegacyCapacity ? row : PaletteLayout.LegacyHeight + row;
                    Check(texelRow == expectedRow && col == (e.Index.Value & 255), $"{e.Token.Value} maps to the wrong texel.");
                    Check(full.Pixels[(texelRow * PaletteLayout.Width + col) * 4 + 3] == 255, $"{e.Token.Value} row {row} was not written at {season} {clock}.");
                }
                if (e is { Seasonal: true, Role: PaletteRole.Lit, Row: { } seasonalRow })
                    for (var row = seasonalRow; row <= seasonalRow + 1; row++)
                        for (var sample = 0; sample < PaletteLayout.SeasonSampleCount; sample++)
                            Check(full.Pixels[(row * PaletteLayout.Width + sample) * 4 + 3] == 255,
                                $"{e.Token.Value} seasonal row {row} sample {sample} was not written at {season} {clock}.");
            }
        }
        // The pair derives like a legacy pair of the same class and albedo: lit brighter than shade under the default frame.
        var at = script.EvaluateWorld(17.5, (double)Season.Autumn, null, default);
        full.ComposeCurrent(at);
        foreach (var e in catalog.Entries.Where(e => e.Index.Value >= PaletteLayout.LegacyCapacity && e.Role == PaletteRole.Lit))
        {
            var lit = Texel(full, e.Index.Value);
            var shade = Texel(full, e.Index.Value + 1);
            Check(lit.L > shade.L, $"{e.Token.Value}: lit L {lit.L:F3} is not above shade L {shade.L:F3}.");
        }
    }

    private static Lab Texel(PaletteComposer composer, int id)
    {
        var (col, row) = PaletteLayout.FrameTexel(new PaletteIndex(id), 0);
        var i = (row * PaletteLayout.Width + col) * 4;
        var p = composer.Pixels;
        return Oklab.FromSrgb(p[i] / 255.0, p[i + 1] / 255.0, p[i + 2] / 255.0);
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
        foreach (var file in new[] { Path.Combine(palette, "core.json") }
            .Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "WP-*.json").Order(StringComparer.Ordinal)))
            entries.AddRange(JsonSerializer.Deserialize<PaletteRegistryData>(File.ReadAllBytes(file), ContractJson.Options)!.Entries);
        var extensions = new List<PaletteEntryV2>();
        foreach (var (lit, cls, albedo) in new[]
        {
            (256, "building", new System.Numerics.Vector3(0.62f, 0.36f, 0.26f)), (330, "metal", new System.Numerics.Vector3(0.32f, 0.31f, 0.29f)),
            (400, "ground", new System.Numerics.Vector3(0.45f, 0.45f, 0.42f)), (510, "fabric", new System.Numerics.Vector3(0.16f, 0.43f, 0.69f))
        })
        {
            extensions.Add(new(new PaletteToken($"test.bank{lit}.lit"), new PaletteIndex(lit), albedo, cls, false, PaletteRole.Lit));
            extensions.Add(new(new PaletteToken($"test.bank{lit}.shade"), new PaletteIndex(lit + 1), albedo, cls, false, PaletteRole.Shade));
        }
        return (script, new PaletteCatalog(new PaletteRegistry(entries), extensions));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
