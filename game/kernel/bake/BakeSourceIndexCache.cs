using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>The sole persisted source index, including final product stamps and its captured input version.</summary>
internal sealed class BakeSourceIndexCache(TerrainCacheService terrain, MapId map, string sourceVersion)
{
    private readonly BakeKey _key = new(map, "resident", "source-index");
    private readonly string _identity = BakeOptions.Abi + "/source-index-2";
    private readonly object _sync = new();
    private BakeSourceIndexData? _saved;
    private string _residentStamp = "";
    private string _writtenVersion = "";
    private int _vegetationCount = -1;
    private int _furnitureCount = -1;
    private int _productCount = -1;

    internal string ResolveResidentStamp(Func<string> compute)
    {
        var candidates = terrain.ReadProductCandidates<BakeSourceIndexData>(_key, _identity, saved =>
        {
            if (!Complete(saved)) throw new InvalidDataException("Incomplete cached source index.");
        }).ToArray();
        _saved = null;
        _residentStamp = candidates.FirstOrDefault(saved => saved.SourceVersion == sourceVersion)?.ResidentStamp ?? compute();
        var matching = candidates.Where(saved => saved.ResidentStamp == _residentStamp).ToArray();
        if (matching.Length > 0)
        {
            var preferred = matching[0];
            _writtenVersion = preferred.SourceVersion;
            _vegetationCount = preferred.Vegetation.Length;
            _furnitureCount = preferred.Furniture.Length;
            _productCount = preferred.Products.Length;
            // Local entries win; a partial local snapshot must not hide packaged product stamps.
            _saved = preferred with
            {
                SourceVersion = sourceVersion,
                Vegetation = matching.SelectMany(saved => saved.Vegetation).DistinctBy(cell => cell.Cell).ToArray(),
                Furniture = matching.SelectMany(saved => saved.Furniture).DistinctBy(cell => cell.Cell).ToArray(),
                Products = matching.SelectMany(saved => saved.Products).DistinctBy(product => product.Key).ToArray()
            };
        }
        return _residentStamp;
    }

    internal BakeSourceIndexData? Read()
    {
        if (_saved is null) terrain.RequireMiss(_key);
        return _saved;
    }

    internal void Write(BakeSourceIndex index)
    {
        lock (_sync)
        {
            if (index.VegetationCount == _vegetationCount && index.FurnitureCount == _furnitureCount &&
                index.ProductCount == _productCount && _writtenVersion == sourceVersion) return;
            var saved = index.Snapshot(sourceVersion, _residentStamp);
            terrain.WriteProduct(_key, _identity, saved);
            _vegetationCount = saved.Vegetation.Length;
            _furnitureCount = saved.Furniture.Length;
            _productCount = saved.Products.Length;
            _writtenVersion = sourceVersion;
        }
    }

    private static bool Complete(BakeSourceIndexData saved) => !string.IsNullOrWhiteSpace(saved.SourceVersion) &&
        saved.ResidentStamp is { Length: 32 } && saved.CommonStamp is { Length: 32 } && saved.PaletteStamp is { Length: 32 } &&
        Cells(saved.Cells) && Cells(saved.Windows) && Cells(saved.Vegetation) && Cells(saved.Furniture) &&
        saved.Products is not null && saved.Products.All(product => product.Stamp is { Length: 32 }) &&
        saved.Products.Select(product => product.Key).Distinct().Count() == saved.Products.Length;

    private static bool Cells(BakeSourceCell[]? cells) => cells is not null &&
        cells.All(cell => cell.Stamp is { Length: 32 }) && cells.Select(cell => cell.Cell).Distinct().Count() == cells.Length;
}

internal readonly record struct BakeSourceCell(TileKey Cell, string Stamp);
internal readonly record struct BakeProductInput(BakeKey Key, string Stamp);

internal sealed record BakeSourceIndexData
{
    public required string SourceVersion { get; init; }
    public required string ResidentStamp { get; init; }
    public required string CommonStamp { get; init; }
    public required string PaletteStamp { get; init; }
    public required BakeSourceCell[] Cells { get; init; }
    public required BakeSourceCell[] Windows { get; init; }
    public required BakeSourceCell[] Vegetation { get; init; }
    public required BakeSourceCell[] Furniture { get; init; }
    public required BakeProductInput[] Products { get; init; }
}
