using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Lod;

/// <summary>Inverts the prepared map's shared immutable L0 module index for the streaming loop.</summary>
public sealed class ModuleTileCatalog
{
    private readonly Dictionary<StreamTile, string[]> _modules;
    public readonly record struct AuxiliaryEntry(StreamTile Tile, BakeTileEntry Content);
    private ModuleTileCatalog(Dictionary<StreamTile, string[]> modules, IReadOnlyList<AuxiliaryEntry> auxiliary)
    { _modules = modules; Auxiliary = auxiliary; }
    public IReadOnlyList<AuxiliaryEntry> Auxiliary { get; }
    public IReadOnlyList<string> Modules(StreamTile tile) => _modules.TryGetValue(tile, out var modules) ? modules : [];
    public static ModuleTileCatalog Build(PreparedBakeTiles prepared)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        var byTile = new Dictionary<StreamTile, SortedSet<string>>();
        foreach (var (module, tiles) in prepared.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
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
        var auxiliary = prepared.Auxiliary.Select(entry =>
        {
            var parsed = BakeRequestKeys.Parse(entry.Key);
            if (parsed.Collision || parsed.Level != LodLevel.L0)
                throw new InvalidDataException($"Invalid auxiliary directory key: {entry.Key}.");
            return new AuxiliaryEntry(new(parsed.Tile, parsed.Level), entry);
        }).ToArray();
        return new(byTile.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()), auxiliary);
    }
}
