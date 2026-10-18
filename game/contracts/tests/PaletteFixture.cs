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
        var files = new[] { core }.Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "*.json")
            .Where(path => Path.GetFileName(path) != "ui.json").Order(StringComparer.Ordinal));
        return PaletteCatalog.FromSources(files.Select(path => (path, File.ReadAllBytes(path))));
    }
}
