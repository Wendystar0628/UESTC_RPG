using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Current feature geometry used by facade layouts, preserving authored order and rings.</summary>
public static class WorldFeatureGeometry
{
    /// <summary>Physical polygon rings in authored order; points and lines have no polygon footprint.</summary>
    public static IReadOnlyList<PolygonRings> PolygonsOf(OverlayGeometry geometry) => geometry switch
    {
        PolygonGeometry polygon => [polygon.Rings],
        MultiPolygonGeometry multi => multi.Polygons,
        PolylineGeometry or PointGeometry => [],
        _ => throw new ArgumentException("Unknown geometry.")
    };

    /// <summary>Facade layout follows the current source part, falling back to its physical geometry.</summary>
    public static OverlayGeometry FacadeGeometryOf(WorldFeaturePart part) => part.FacadeGeometry ?? part.Geometry;

    /// <summary>Current parts in their existing order, or the current authored geometry after reshape/add.</summary>
    public static IReadOnlyList<PolygonRings> FacadePolygonsOf(WorldFeature feature) => feature.Parts.Count == 0
        ? PolygonsOf(feature.Geometry).ToArray()
        : feature.Parts.SelectMany(part => PolygonsOf(FacadeGeometryOf(part))).ToArray();
}
