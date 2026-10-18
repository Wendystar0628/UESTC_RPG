namespace Campus.Contracts.Vegetation;

/// <summary>Vegetation display pools in instance-prototype order.</summary>
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

/// <summary>Production capability exposing prepared vegetation to the display runtime.</summary>
public interface IVegetationRuntimeSource : IContentModule
{
    IReadOnlyList<VegetationMotherData> PrepareVegetation(BuildContext context);
    /// <summary>Authored grass scopes from this exact map version; actual prototypes use the existing geometry instance pools.</summary>
    GrassRuntimeData PrepareGrass(BuildContext context);
    /// <summary>
    /// The packed species table for the tree_species global texture (WLD-VEGETATION): RGBA32F texels, row-major,
    /// 8 texels per species row and 32 rows; the layout is documented on Campus.Gen.Vegetation.SpeciesTable.
    /// </summary>
    float[] PrepareSpeciesTable(BuildContext context);
}
