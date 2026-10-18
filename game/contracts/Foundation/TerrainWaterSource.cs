namespace Campus.Contracts;

/// <summary>Optional immutable geometry directory on the same ITerrain instance, independent of visual residency.</summary>
/// <remarks>Empty means confirmed no surface water. An absent capability does not make that claim.
/// The producer freezes final polygons (including holes) and approved wade areas, orders by ordinal Water ID,
/// and excludes underground channels. Levels remain exclusively ITerrain.WaterY; beds remain HeightAt.
/// </remarks>
public interface ITerrainWaterSource
{
    IReadOnlyList<TerrainWaterBody> Waters { get; }
}

public sealed record TerrainWaterBody(FeatureId Water, IReadOnlyList<PolygonRings> Polygons,
    IReadOnlyList<PolygonRings> WadeAreas);
