global using Campus.Gen.World;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.Common;

/// <summary>
/// Reads of the map data by feature ID (D-239): the sites keep IDs and their own shaping values, never a copy of a coordinate,
/// outline, storey count, height or name the map data already carries. Every read throws when the data is missing.
/// </summary>
public static partial class FeatureReads
{
    public static WorldFeature Feature(this IWorldModel world, string id) =>
        world.Features.TryGetValue(new(id), out var feature) ? feature : throw new InvalidOperationException($"{id} is missing from the world model.");

    /// <summary>One source part of the feature.</summary>
    public static WorldFeaturePart Part(this WorldFeature feature, string sourceId) =>
        feature.Parts.FirstOrDefault(p => p.SourceId == sourceId) ?? throw new InvalidOperationException($"{feature.Id.Value} part {sourceId} is missing.");

    /// <summary>Height and storeys of a part; both must be recorded.</summary>
    public static (float Height, int Storeys) Levels(this WorldFeaturePart part) =>
        (part.Height ?? throw new InvalidOperationException($"{part.SourceId} has no height."),
         part.Storeys ?? throw new InvalidOperationException($"{part.SourceId} has no storeys."));

    /// <summary>World plan position (x, z) of a point feature (an entity anchor or an overlay point).</summary>
    public static Vector2 Point(this WorldFeature feature) =>
        feature.Geometry is PointGeometry point ? new(point.Position.X, point.Position.Z) : throw new InvalidOperationException($"{feature.Id.Value} is not a point.");

    /// <summary>Outer ring of a single-polygon feature.</summary>
    public static IReadOnlyList<Vector2> Outer(this WorldFeature feature) =>
        feature.Geometry is PolygonGeometry polygon ? polygon.Rings.Outer : throw new InvalidOperationException($"{feature.Id.Value} is not a polygon.");

    /// <summary>The outer rings of the feature's source parts (their facade layout), or of its geometry when it has none.</summary>
    public static IReadOnlyList<IReadOnlyList<Vector2>> Outers(this WorldFeature feature)
    {
        static IEnumerable<IReadOnlyList<Vector2>> Of(WorldGeometry geometry) =>
            WorldFeatureGeometry.PolygonsOf(geometry).Select(rings => rings.Outer);
        return [.. feature.Parts.Count > 0 ? feature.Parts.SelectMany(p => Of(WorldFeatureGeometry.FacadeGeometryOf(p))) : Of(feature.Geometry)];
    }

    public static IReadOnlyList<Vector2> Line(this WorldFeature feature) =>
        feature.Geometry is PolylineGeometry line ? line.Line.Points : throw new InvalidOperationException($"{feature.Id.Value} is not a polyline.");

}
