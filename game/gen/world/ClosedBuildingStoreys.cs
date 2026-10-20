using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Closed-building shell levels only; open-building levels are supplied by InteriorLayout.Storeys.</summary>
public static class ClosedBuildingStoreys
{
    public const float DefaultHeight = 3.6f;
    public static IReadOnlyList<StoreyInfo> ClosedBuilding(WorldFeature feature, ITerrain terrain)
    {
        IReadOnlyList<WorldFeaturePart> parts = feature.Parts.Count == 0
            ? [new("", feature.Geometry, feature.Height, feature.Storeys) { Props = feature.Props }] : feature.Parts;
        var result = new List<StoreyInfo>();
        foreach (var part in parts)
        {
            var kind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var declared)
                ? declared.GetString() : feature.Kind.Value;
            if (kind != "building") continue;
            IReadOnlyList<PolygonRings> polygons = part.Geometry switch
            { PolygonGeometry polygon => [polygon.Rings], MultiPolygonGeometry multi => multi.Polygons, _ => [] };
            int count = Math.Max(1, part.Storeys ?? (int)MathF.Ceiling((part.Height ?? DefaultHeight) / DefaultHeight));
            float height = (part.Height ?? count * DefaultHeight) / count;
            foreach (var polygon in polygons)
                for (var floor = 1; floor <= count; floor++)
                    result.Add(new(floor, terrain.PadY(feature.Id) + (floor - 1) * height, height, polygon.Outer));
        }
        return result.AsReadOnly();
    }
}
