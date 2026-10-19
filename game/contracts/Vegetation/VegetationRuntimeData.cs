using Campus.Contracts.Bake;

namespace Campus.Contracts.Vegetation;

/// <summary>Vegetation display pools. Tree payload prototype order is L0Plus, L0, L1, Shadow; Far belongs to static tiles.</summary>
public enum VegPool : byte { L0Plus = 0, L0 = 1, L1 = 2, Far = 3, Shadow = 4 }

/// <summary>Prepared geometry and complete instance records for one vegetation mother model.</summary>
public sealed class VegetationMotherData
{
    public VegetationMotherData(string key, WpId owner, ReadOnlyMemory<float> records,
        IReadOnlyList<GeometryPrototype> prototypes, IReadOnlyList<Box3> bounds)
    {
        Key = key;
        Owner = owner;
        Records = records.ToArray();
        Prototypes = Array.AsReadOnly(prototypes.ToArray());
        Bounds = Array.AsReadOnly(bounds.ToArray());
    }

    public string Key { get; }
    public WpId Owner { get; }
    public ReadOnlyMemory<float> Records { get; }
    public IReadOnlyList<GeometryPrototype> Prototypes { get; }
    /// <summary>World-space L0+ bounds in the same tree order as Records.</summary>
    public IReadOnlyList<Box3> Bounds { get; }
}

/// <summary>Prepared tree or grass data for one requested owner tile.</summary>
public sealed record VegetationTileContent(BakeKey Key, TileKey Tile,
    IReadOnlyList<VegetationMotherData> Mothers);

/// <summary>Production capability exposing prepared vegetation to the display runtime.</summary>
public interface IVegetationRuntimeSource : IContentModule
{
    /// <summary>Candidate owner tiles from authored grass scopes; querying the directory does not build grass instances.</summary>
    IReadOnlyList<TileKey> GrassTiles(BuildContext context);
    IReadOnlyList<VegetationMotherData> PrepareTrees(BuildContext context, TileKey tile);
    IReadOnlyList<VegetationMotherData> PrepareGrass(BuildContext context, TileKey tile);
    /// <summary>
    /// The packed species table for the tree_species global texture (WLD-VEGETATION): RGBA32F texels, row-major,
    /// 8 texels per species row and 32 rows; the layout is documented on Campus.Gen.Vegetation.SpeciesTable.
    /// </summary>
    float[] PrepareSpeciesTable(BuildContext context);
}
