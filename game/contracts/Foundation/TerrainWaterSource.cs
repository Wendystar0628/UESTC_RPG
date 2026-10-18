namespace Campus.Contracts;

/// <summary>Optional immutable surface-water queries and geometry on the same ITerrain instance, independent of visual residency.</summary>
/// <remarks>Empty means confirmed no surface water. An absent capability does not make that claim.
/// The producer freezes final polygons (including holes) and approved wade areas, orders by ordinal Water ID,
/// and excludes underground channels. Surface levels follow ITerrain.WaterY; beds remain HeightAt.
/// </remarks>
public interface ITerrainWaterSource
{
    IReadOnlyList<TerrainWaterBody> Waters { get; }

    /// <summary>Queries final surface-water geometry, including holes and approved wade areas; null means no surface water at the point.</summary>
    /// <remarks>Surface is the same level as ITerrain.WaterY. Underground channels are excluded.</remarks>
    (FeatureId Id, float Surface, bool Wade)? WaterAt(float x, float z);
}

public sealed record TerrainWaterBody(FeatureId Water, IReadOnlyList<PolygonRings> Polygons,
    IReadOnlyList<PolygonRings> WadeAreas);
