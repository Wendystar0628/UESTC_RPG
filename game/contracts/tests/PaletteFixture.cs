namespace Campus.Contracts.Tests;

/// <summary>The complete source-data catalog supplied explicitly to generation fixtures.</summary>
internal static class PaletteFixture
{
    private static readonly Lazy<PaletteCatalog> Snapshot = new(Load);
    internal static PaletteCatalog Catalog => Snapshot.Value;

    private static PaletteCatalog Load()
    {
        var palette = Campus.Tests.RepositoryFixture.FilePath("game", "style", "palette");
        var core = Path.Combine(palette, "core.json");
        var bank0 = new[] { core }.Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "WP-*.json").Order(StringComparer.Ordinal));
        var bank1 = Directory.GetFiles(Path.Combine(palette, "ext-v2"), "WP-*.json").Order(StringComparer.Ordinal);
        return PaletteCatalog.FromSources(bank0.Select(path => (path, File.ReadAllBytes(path))),
            bank1.Select(path => (path, File.ReadAllBytes(path))));
    }
}
