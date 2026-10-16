using System.Text;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Style.Palette.Tests;

/// <summary>
/// The schema-2 extension loader (contract-28 §6): ext-v2/WP-*.json joined with the unchanged legacy registry into one
/// <see cref="PaletteCatalog"/>. Global token/index uniqueness, complete Lit/Shade pairs, the 256–511 range and the
/// non-seasonal rule are refused as invalid data; the registered bank-1 names resolve to their allocated indices.
/// </summary>
[TestSuite]
public sealed class PaletteCatalogLoaderTests
{
    [TestCase]
    public void RegisteredExtensionsJoinTheLegacyRegistry()
    {
        var (legacy, files) = Load();
        var catalog = PaletteRegistryLoader.LoadCatalog(legacy, files);
        Check(ReferenceEquals(catalog.LegacyRegistry, legacy), "The catalog must reuse the legacy registry instance.");
        Check(catalog.ExtensionCount == 14, $"Expected the seven approved bank-1 pairs, found {catalog.ExtensionCount} extension entries.");
        foreach (var (family, lit) in new[]
        {
            ("bronze", 320), ("stone.black", 322), ("glass.tea", 324), ("band.ochre", 326), ("glass.deep", 328),
            ("street.blue", 448), ("hazard.black", 450)
        })
        {
            Check(catalog.Resolve(new PaletteToken(family + ".lit")).Value == lit, $"{family}.lit is not at {lit}.");
            Check(catalog.Resolve(new PaletteToken(family + ".shade")).Value == lit + 1, $"{family}.shade is not at {lit + 1}.");
            Check(!legacy.TryGet(new PaletteToken(family + ".lit"), out _), $"{family} leaked into the legacy byte registry.");
            catalog.Validate(new ToonSpec(new PaletteToken(family + ".lit"), new PaletteToken(family + ".shade")));
        }
    }

    [TestCase]
    public void InvalidExtensionsAreRefused()
    {
        var (legacy, _) = Load();
        string Entry(string token, int index, string role, bool seasonal = false) =>
            $"{{\"token\":\"{token}\",\"index\":{index},\"albedo\":[0.5,0.5,0.5],\"materialClass\":\"building\",\"seasonal\":{(seasonal ? "true" : "false")},\"role\":\"{role}\"}}";
        string File(int schema, params string[] entries) => $"{{\"schema\":{schema},\"entries\":[{string.Join(",", entries)}]}}";
        var cases = new (string Name, string Json)[]
        {
            ("schema 1 in ext-v2", File(1, Entry("t.lit", 256, "lit"), Entry("t.shade", 257, "shade"))),
            ("legacy token reused", File(2, Entry("wall.lit", 256, "lit"), Entry("t.shade", 257, "shade"))),
            ("index below 256", File(2, Entry("t.lit", 254, "lit"), Entry("t.shade", 255, "shade"))),
            ("index above 511", File(2, Entry("t.lit", 512, "lit"), Entry("t.shade", 513, "shade"))),
            ("missing shade", File(2, Entry("t.lit", 256, "lit"))),
            ("odd lit", File(2, Entry("t.shade", 256, "shade"), Entry("t.lit", 257, "lit"))),
            ("seasonal", File(2, Entry("t.lit", 256, "lit", true), Entry("t.shade", 257, "shade", true))),
            ("legacy byte id field", File(2, Entry("t.lit", 256, "lit").Replace("\"index\"", "\"id\"", StringComparison.Ordinal), Entry("t.shade", 257, "shade")))
        };
        foreach (var (name, json) in cases)
        {
            var refused = false;
            try { PaletteRegistryLoader.LoadCatalog(legacy, [("test/" + name, Encoding.UTF8.GetBytes(json))]); }
            catch (InvalidDataException) { refused = true; }
            Check(refused, $"The loader accepted an invalid extension: {name}.");
        }
        var twice = File(2, Entry("t.lit", 256, "lit"), Entry("t.shade", 257, "shade"));
        var duplicated = false;
        try { PaletteRegistryLoader.LoadCatalog(legacy, [("a", Encoding.UTF8.GetBytes(twice)), ("b", Encoding.UTF8.GetBytes(twice))]); }
        catch (InvalidDataException) { duplicated = true; }
        Check(duplicated, "The same token and index in two extension files must be refused.");
    }

    private static (PaletteRegistry Legacy, (string, byte[])[] Files) Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(Path.Combine(dir.FullName, "ownership.json"))) dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("Repository root not found from the test directory.");
        var palette = Path.Combine(dir.FullName, "game", "style", "palette");
        var entries = new List<PaletteEntry>();
        foreach (var file in new[] { Path.Combine(palette, "core.json") }
            .Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "WP-*.json").Order(StringComparer.Ordinal)))
            entries.AddRange(JsonSerializer.Deserialize<PaletteRegistryData>(System.IO.File.ReadAllBytes(file), ContractJson.Options)!.Entries);
        var files = Directory.GetFiles(Path.Combine(palette, "ext-v2"), "WP-*.json").Order(StringComparer.Ordinal)
            .Select(file => (file, System.IO.File.ReadAllBytes(file))).ToArray();
        return (new PaletteRegistry(entries), files);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
