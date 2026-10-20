using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Sites;

public readonly record struct FeatureRectangle(PlanFrame Frame, float Width, float Depth);

public static partial class FeatureReads
{
    /// <summary>Explicit source part, otherwise the first part, otherwise the feature's own geometry.</summary>
    public static WorldGeometry ContourGeometry(this WorldFeature feature, string? sourceId = null, bool facade = true)
    {
        var part = sourceId is not null ? feature.Part(sourceId) : feature.Parts.FirstOrDefault();
        return part is null ? feature.Geometry : facade ? WorldFeatureGeometry.FacadeGeometryOf(part) : part.Geometry;
    }

    public static IReadOnlyList<Vector2> Contour(this WorldFeature feature, string? sourceId = null, bool facade = true) =>
        OuterOf(feature.ContourGeometry(sourceId, facade));

    public static IReadOnlyList<Vector2> Contour(this WorldFeaturePart part, bool facade = true) =>
        OuterOf(facade ? WorldFeatureGeometry.FacadeGeometryOf(part) : part.Geometry);

    /// <summary>Ordered endpoints of an authored two-point line.</summary>
    public static (Vector2 From, Vector2 To) PointPair(this WorldFeature feature)
    {
        var line = feature.Line();
        return line.Count == 2 ? (line[0], line[1]) : throw new InvalidOperationException($"{feature.Id.Value} needs exactly two points.");
    }

    /// <summary>Bounds along an explicit plan direction; origin is the rectangle centre, with no ring-index assumptions.</summary>
    public static FeatureRectangle Rectangle(this WorldFeature feature, Vector2 direction, string? sourceId = null, bool facade = true)
    {
        var frame = PlanFrame.Along(Vector2.Zero, direction);
        var points = feature.Contour(sourceId, facade).Select(frame.Local).ToArray();
        var min = points.Aggregate(Vector2.Min);
        var max = points.Aggregate(Vector2.Max);
        var center = (min + max) * 0.5f;
        return new(frame with { Origin = frame.P(center.X, center.Y) }, max.X - min.X, max.Y - min.Y);
    }

    public static IReadOnlyList<Vector2> TerrainLine(this IWorldModel world, string id)
    {
        if (!world.TerrainOperations.TryGetValue(id, out var operation))
            throw new InvalidOperationException($"Terrain operation {id} is missing.");
        return operation switch
        {
            TerrainSlopeOp { Geometry: PolylineGeometry line } => line.Line.Points,
            TerrainChannelOp { Line: { } line } => line.Points,
            TerrainTerraceOp terrace => terrace.Front.Points,
            _ => throw new InvalidOperationException($"Terrain operation {id} has no authored line."),
        };
    }

    private static IReadOnlyList<Vector2> OuterOf(WorldGeometry geometry) => geometry switch
    {
        PolygonGeometry polygon => polygon.Rings.Outer,
        MultiPolygonGeometry { Polygons.Count: > 0 } multi => multi.Polygons[0].Outer,
        _ => throw new InvalidOperationException("The selected contour is not an area."),
    };
}
