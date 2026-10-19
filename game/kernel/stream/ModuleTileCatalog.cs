using Campus.Contracts;
using Campus.Kernel.Lod;

namespace Campus.Kernel.Stream;

/// <summary>Inverts the prepared map's shared immutable L0 module index for the streaming loop.</summary>
public sealed class ModuleTileCatalog
{
    private readonly Dictionary<StreamTile, string[]> _modules;
    private ModuleTileCatalog(Dictionary<StreamTile, string[]> modules) => _modules = modules;
    public IReadOnlyList<string> Modules(StreamTile tile) => _modules.TryGetValue(tile, out var modules) ? modules : [];
    public static ModuleTileCatalog Build(IReadOnlyDictionary<string, IReadOnlyList<TileKey>> moduleTiles)
    {
        ArgumentNullException.ThrowIfNull(moduleTiles);
        var byTile = new Dictionary<StreamTile, SortedSet<string>>();
        foreach (var (module, tiles) in moduleTiles.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            foreach (var key in tiles)
            {
                var tile = new StreamTile(key, LodLevel.L0);
                while (true)
                {
                    if (!byTile.TryGetValue(tile, out var ids)) byTile.Add(tile, ids = new(StringComparer.Ordinal));
                    ids.Add(module);
                    if (tile.Level == LodLevel.L4) break;
                    tile = tile.Parent;
                }
            }
        }
        return new(byTile.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
    }
}
