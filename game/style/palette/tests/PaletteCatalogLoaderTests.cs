using Campus.Tests;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Style.Palette.Tests;

/// <summary>Core and content sources share one DTO; bank rules remain determined by logical index.</summary>
[TestSuite]
public sealed class PaletteCatalogLoaderTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RegisteredContentSourcesJoinTheCore()
    {
        var (legacy, files) = Load();
        var catalog = LoadCatalog(legacy, files);
        AssertThat(catalog.Get(legacy.Entries[0].Index).Albedo == legacy.Entries[0].Albedo).OverrideFailureMessage("The base metadata changed while composing the catalog.").IsTrue();
        var extensions = files.SelectMany(file => JsonSerializer.Deserialize<PaletteRegistryData>(file.Item2, ContractJson.Options)!.Entries).ToArray();
        AssertThat(catalog.ExtensionCount == extensions.Count(entry => entry.Index.Value >= PaletteLayout.LegacyCapacity)).OverrideFailureMessage("An authored extension entry was lost.").IsTrue();
        foreach (var entry in extensions)
        {
            AssertThat(catalog.Resolve(entry.Token) == entry.Index).OverrideFailureMessage($"{entry.Token.Value} did not retain its authored index.").IsTrue();
            AssertThat(!legacy.TryGet(entry.Token, out _)).OverrideFailureMessage($"{entry.Token.Value} unexpectedly exists in the core source.").IsTrue();
            if (entry.Role == PaletteRole.Lit)
            {
                var shade = extensions.Single(candidate => candidate.Index.Value == entry.Index.Value + 1);
                catalog.Validate(new ToonSpec(entry.Token, shade.Token));
            }
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void InvalidExtensionsAreRefused()
    {
        var (legacy, _) = Load();
        string Entry(string token, int index, string role, bool seasonal = false) =>
            $"{{\"token\":\"{token}\",\"index\":{index},\"albedo\":[0.5,0.5,0.5],\"materialClass\":\"building\",\"seasonal\":{(seasonal ? "true" : "false")},\"role\":\"{role}\"}}";
        string File(int schema, params string[] entries) => $"{{\"schema\":{schema},\"entries\":[{string.Join(",", entries)}]}}";
        var cases = new (string Name, string Json)[]
        {
            ("obsolete source schema", File(1, Entry("t.lit", 256, "lit"), Entry("t.shade", 257, "shade"))),
            ("legacy token reused", File(2, Entry("wall.lit", 256, "lit"), Entry("t.shade", 257, "shade"))),
            ("frame in bank 1", File(2, Entry("t.lit", 256, "frame"), Entry("t.shade", 257, "shade"))),
            ("index above 511", File(2, Entry("t.lit", 512, "lit"), Entry("t.shade", 513, "shade"))),
            ("missing shade", File(2, Entry("t.lit", 256, "lit"))),
            ("odd lit", File(2, Entry("t.shade", 256, "shade"), Entry("t.lit", 257, "lit"))),
            ("seasonal", File(2, Entry("t.lit", 256, "lit", true), Entry("t.shade", 257, "shade", true))),
            ("legacy byte id field", File(2, Entry("t.lit", 256, "lit").Replace("\"index\"", "\"id\"", StringComparison.Ordinal), Entry("t.shade", 257, "shade")))
        };
        foreach (var (name, json) in cases)
        {
            var refused = false;
            try { LoadCatalog(legacy, [("test/" + name, Encoding.UTF8.GetBytes(json))]); }
            catch (InvalidDataException) { refused = true; }
            AssertThat(refused).OverrideFailureMessage($"The loader accepted an invalid extension: {name}.").IsTrue();
        }
        var twice = File(2, Entry("t.lit", 256, "lit"), Entry("t.shade", 257, "shade"));
        var duplicated = false;
        try { LoadCatalog(legacy, [("a", Encoding.UTF8.GetBytes(twice)), ("b", Encoding.UTF8.GetBytes(twice))]); }
        catch (InvalidDataException) { duplicated = true; }
        AssertThat(duplicated).OverrideFailureMessage("The same token and index in two extension files must be refused.").IsTrue();
    }

    private static PaletteCatalog LoadCatalog(PaletteRegistry bank0, IEnumerable<(string Path, byte[] Json)> extensions) =>
        PaletteCatalog.FromSources(new[] { ("fixture/bank0.json", JsonSerializer.SerializeToUtf8Bytes(new PaletteRegistryData(bank0.Entries), ContractJson.Options)) }.Concat(extensions));

    private static (PaletteRegistry Legacy, (string, byte[])[] Files) Load()
    {
        var palette = Campus.Tests.RepositoryFixture.FilePath("game", "style", "palette");
        var core = JsonSerializer.Deserialize<PaletteRegistryData>(System.IO.File.ReadAllBytes(Path.Combine(palette, "core.json")), ContractJson.Options)!;
        var entries = core.Entries;
        var files = Directory.GetFiles(Path.Combine(palette, "ext"), "*.json")
            .Where(path => Path.GetFileName(path) != "ui.json").Order(StringComparer.Ordinal)
            .Select(file => (file, System.IO.File.ReadAllBytes(file))).ToArray();
        return (new PaletteRegistry(entries), files);
    }
}
