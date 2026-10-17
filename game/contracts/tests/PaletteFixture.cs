namespace Campus.Contracts.Tests;

/// <summary>The complete source-data catalog supplied explicitly to generation fixtures.</summary>
internal static class PaletteFixture
{
    private static readonly Lazy<PaletteCatalog> Snapshot = new(Load);
    internal static PaletteCatalog Catalog => Snapshot.Value;

    private static PaletteCatalog Load()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                var palette = Path.Combine(directory.FullName, "game", "style", "palette");
                var core = Path.Combine(palette, "core.json");
                if (!File.Exists(core)) continue;
                var bank0 = new[] { core }.Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "WP-*.json").Order(StringComparer.Ordinal));
                var bank1 = Directory.GetFiles(Path.Combine(palette, "ext-v2"), "WP-*.json").Order(StringComparer.Ordinal);
                return PaletteCatalog.FromSources(bank0.Select(path => (path, File.ReadAllBytes(path))),
                    bank1.Select(path => (path, File.ReadAllBytes(path))));
            }
        throw new DirectoryNotFoundException("The palette source data for the fixture was not found.");
    }
}
