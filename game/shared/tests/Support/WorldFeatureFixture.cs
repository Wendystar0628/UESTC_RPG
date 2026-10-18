using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Tests;

/// <summary>Small authored features for tests; properties are detached from their JSON document.</summary>
internal static class WorldFeatureFixture
{
    public static WorldFeature Create(string id, string kind, OverlayGeometry geometry, string? props = null,
        FeatureSource source = FeatureSource.Design, float? height = null, int? storeys = null,
        IReadOnlyList<WorldFeaturePart>? parts = null)
    {
        var points = Points(geometry).ToArray();
        var min = points.Length == 0 ? Vector3.Zero : points.Aggregate(Vector3.Min);
        var max = points.Length == 0 ? Vector3.Zero : points.Aggregate(Vector3.Max);
        if (height is { } extent) max.Y = MathF.Max(max.Y, min.Y + extent);
        JsonElement? properties = null;
        if (props is not null)
        {
            using var document = JsonDocument.Parse(props);
            properties = document.RootElement.Clone();
        }
        return new(new(id), source, new(kind), geometry)
        {
            Props = properties,
            Height = height,
            Storeys = storeys,
            Parts = parts ?? [],
            Bounds = new(min, max),
        };
    }

    private static IEnumerable<Vector3> Points(OverlayGeometry geometry) => geometry switch
    {
        PointGeometry point => [point.Position],
        PolylineGeometry line => line.Line.Points.Select(p => new Vector3(p.X, 0f, p.Y)),
        PolygonGeometry polygon => polygon.Rings.Outer.Select(p => new Vector3(p.X, 0f, p.Y)),
        MultiPolygonGeometry multi => multi.Polygons.SelectMany(p => p.Outer).Select(p => new Vector3(p.X, 0f, p.Y)),
        _ => throw new ArgumentOutOfRangeException(nameof(geometry)),
    };
}
