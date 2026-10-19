using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Systems.Nightlight.Glow;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace Campus.Systems.Nightlight.Windows;

/// <summary>地图准备时按真实楼外轮廓到最终水面的距离挑楼；只返回原窗号快照与光晕缓冲的引用。</summary>
internal static class WaterfrontWindows
{
    private const double MaximumWaterDistance = 60d;
    private static readonly GeometryFactory Factory = new();

    public static HashSet<FeatureId> Buildings(IWorldModel world, WindowNumberingSnapshot numbering)
    {
        var source = world.Terrain as ITerrainWaterSource
            ?? throw new InvalidOperationException("Nightlight needs the prepared surface-water geometry for waterfront windows.");
        var water = new STRtree<Polygon>();
        foreach (var body in source.Waters)
            foreach (var rings in body.Polygons)
            {
                var polygon = Factory.CreatePolygon(Ring(rings.Outer), rings.Holes.Select(Ring).ToArray());
                water.Insert(polygon.EnvelopeInternal, polygon);
            }
        water.Build();

        var eligible = new HashSet<FeatureId>();
        foreach (var featureId in numbering.Allocations.Select(allocation => allocation.Feature).Distinct())
        {
            var feature = world.Features[featureId];
            foreach (var rings in BuildingRings(feature))
            {
                // 只用物理外轮廓；Parts 存在时不用实体的聚合查询包络，也不把内庭院当外轮廓。
                var outline = Ring(rings.Outer);
                var reach = new Envelope(outline.EnvelopeInternal);
                reach.ExpandBy(MaximumWaterDistance);
                if (!water.Query(reach).Any(surface => outline.IsWithinDistance(surface, MaximumWaterDistance))) continue;
                eligible.Add(featureId);
                break;
            }
        }
        return eligible;
    }

    public static int[] Indices(WindowNumberingSnapshot numbering, IReadOnlySet<FeatureId> buildings, GlowSet windows)
    {
        var bases = numbering.Allocations.Where(allocation => buildings.Contains(allocation.Feature))
            .Select(allocation => allocation.BaseId).ToHashSet();
        var indices = new List<int>();
        for (var i = 0; i < windows.Count; i++)
            if (bases.Contains((int)windows.Buffer[i * GlowSet.Stride + 13])) indices.Add(i);
        return indices.ToArray();
    }

    private static IEnumerable<PolygonRings> BuildingRings(WorldFeature feature)
    {
        if (feature.Parts.Count == 0)
        {
            if (feature.Kind.Value == "building")
                foreach (var rings in Polygons(feature.Geometry)) yield return rings;
            yield break;
        }
        foreach (var part in feature.Parts)
        {
            var kind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var value) &&
                value.ValueKind == JsonValueKind.String ? value.GetString() : feature.Kind.Value;
            if (kind != "building") continue;
            foreach (var rings in Polygons(part.Geometry)) yield return rings;
        }
    }

    private static IEnumerable<PolygonRings> Polygons(WorldGeometry geometry) => geometry switch
    {
        PolygonGeometry polygon => [polygon.Rings],
        MultiPolygonGeometry multi => multi.Polygons,
        _ => [],
    };

    private static LinearRing Ring(IReadOnlyList<Vector2> points)
    {
        var coordinates = new Coordinate[points.Count + 1];
        for (var i = 0; i < points.Count; i++) coordinates[i] = new(points[i].X, points[i].Y);
        coordinates[^1] = coordinates[0].Copy();
        return Factory.CreateLinearRing(coordinates);
    }
}
