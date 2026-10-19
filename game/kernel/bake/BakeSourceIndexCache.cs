using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Persists source-cell stamps in the existing local store; its map identity never enters product fingerprints.</summary>
internal sealed class BakeSourceIndexCache(TerrainCacheService terrain, MapId map, string sourceStamp)
{
    private readonly BakeKey _key = new(map, "resident", "source-index");
    private readonly string _identity = BakeOptions.Abi + "/" + sourceStamp;
    private readonly object _sync = new();
    private int _vegetationCount = -1;
    private int _furnitureCount = -1;

    internal BakeSourceIndexData? Read()
    {
        var saved = terrain.ReadJson<BakeSourceIndexData>(_key, _identity);
        if (saved is not null && (saved.CommonStamp is not { Length: 32 } || saved.PaletteStamp is not { Length: 32 } ||
            !Complete(saved.Cells) || !Complete(saved.Windows) || !Complete(saved.Vegetation) || !Complete(saved.Furniture)))
        {
            terrain.Store.Remove(_key);
            saved = null;
        }
        if (saved is null) terrain.RequireMiss(_key);
        else { _vegetationCount = saved.Vegetation.Length; _furnitureCount = saved.Furniture.Length; }
        return saved;

        static bool Complete(BakeSourceCell[]? cells)
        {
            if (cells is null) return false;
            var keys = new HashSet<TileKey>();
            foreach (var cell in cells)
                if (cell.Stamp is not { Length: 32 } || !keys.Add(cell.Cell)) return false;
            return true;
        }
    }

    internal void Write(BakeSourceIndex index)
    {
        lock (_sync)
        {
            // Only completed Lazy values enter the snapshot, and another worker may already have saved them.
            var saved = index.Snapshot();
            if (saved.Vegetation.Length == _vegetationCount && saved.Furniture.Length == _furnitureCount) return;
            terrain.WriteJson(_key, _identity, saved);
            _vegetationCount = saved.Vegetation.Length;
            _furnitureCount = saved.Furniture.Length;
        }
    }
}

internal readonly record struct BakeSourceCell(TileKey Cell, string Stamp);

internal sealed record BakeSourceIndexData
{
    public required string CommonStamp { get; init; }
    public required string PaletteStamp { get; init; }
    public required BakeSourceCell[] Cells { get; init; }
    public required BakeSourceCell[] Windows { get; init; }
    public required BakeSourceCell[] Vegetation { get; init; }
    public required BakeSourceCell[] Furniture { get; init; }
}
