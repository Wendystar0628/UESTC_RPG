using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Current feature geometry used by facade layouts, preserving authored order and rings.</summary>
public static class WorldFeatureGeometry
{
    /// <summary>The polygons of an area feature, one or many; non-area features fail.</summary>
    public static IReadOnlyList<PolygonRings> Polygons(this WorldFeature feature) =>
        feature.Geometry is PolygonGeometry or MultiPolygonGeometry ? PolygonsOf(feature.Geometry)
            : throw new InvalidOperationException($"{feature.Id.Value} is not an area.");

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

    /// <summary>Facade polygons of one kind, with source-part kinds taking precedence over the entity kind.</summary>
    public static IEnumerable<PolygonRings> FacadePolygonsOf(WorldFeature feature, string kind)
    {
        IEnumerable<OverlayGeometry> geometries;
        if (feature.Parts.Count == 0)
            geometries = feature.Kind.Value == kind ? [feature.Geometry] : [];
        else
            geometries = feature.Parts.Where(part =>
                (part.Props is { ValueKind: System.Text.Json.JsonValueKind.Object } props && props.TryGetProperty("kind", out var declared)
                    ? declared.GetString() : feature.Kind.Value) == kind).Select(FacadeGeometryOf);
        foreach (var geometry in geometries)
            foreach (var rings in PolygonsOf(geometry))
                if (rings.Outer.Count >= 3) yield return rings;
    }
}
