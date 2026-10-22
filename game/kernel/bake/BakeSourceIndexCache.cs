using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>The sole persisted source index, including final product stamps and its captured input version.</summary>
internal sealed class BakeSourceIndexCache(TerrainCacheService terrain, MapId map, string sourceVersion, string codeIdentity)
{
    private readonly BakeKey _key = new(map, "resident", "source-index");
    private readonly string _identity = Identity(codeIdentity);
    internal static string Identity(string codeIdentity) => BakeOptions.Abi + "/source-index-2/" + codeIdentity;
    private readonly object _sync = new();
    private BakeSourceIndexData? _saved;
    private string _residentStamp = "";
    private string _writtenVersion = "";
    private int _vegetationCount = -1;
    private int _furnitureCount = -1;
    private int _productCount = -1;

    internal string ResolveResidentStamp(Func<string> compute)
    {
        var saved = terrain.ReadProduct<BakeSourceIndexData>(_key, _identity, value =>
        {
            if (!Complete(value)) throw new InvalidDataException("Incomplete cached source index.");
        });
        _saved = null;
        _residentStamp = saved is not null && saved.SourceVersion == sourceVersion ? saved.ResidentStamp : compute();
        if (saved is not null && saved.ResidentStamp == _residentStamp)
        {
            _writtenVersion = saved.SourceVersion;
            _vegetationCount = saved.Vegetation.Length;
            _furnitureCount = saved.Furniture.Length;
            _productCount = saved.Products.Length;
            _saved = saved with { SourceVersion = sourceVersion };
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
