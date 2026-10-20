using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Interior;

internal sealed record InteriorBuildingPart(string Key, PolygonRings Footprint, IReadOnlyList<StoreyInfo> Storeys);

/// <summary>Open-building dimensions have one source: the authored world feature, its parts and pad.</summary>
internal static class InteriorBuildingSource
{
    public static IReadOnlyList<InteriorBuildingPart> Read(IWorldModel world, FeatureId id, IReadOnlyList<EntranceDecl> entrances, IReadOnlyList<int> openFloors)
    {
        if (!world.Features.TryGetValue(id, out var feature)) throw new InvalidDataException($"Missing interior feature '{id.Value}'.");
        IReadOnlyList<WorldFeaturePart> parts = feature.Parts.Count == 0
            ? [new("body",feature.Geometry,feature.Height,feature.Storeys) { Props=feature.Props }] : feature.Parts;
        var result = new List<InteriorBuildingPart>();
        foreach (var part in parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
        {
            var kind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind",out var declared)
                ? declared.GetString() : feature.Kind.Value;
            if (kind != "building") continue;
            var total = part.Height ?? feature.Height ?? throw new InvalidDataException($"Interior '{id.Value}/{part.SourceId}' needs world height.");
            var count = part.Storeys ?? feature.Storeys ?? throw new InvalidDataException($"Interior '{id.Value}/{part.SourceId}' needs world storeys.");
            if (!float.IsFinite(total) || total <= 0 || count <= 0) throw new InvalidDataException($"Invalid interior world height/storeys: {id.Value}/{part.SourceId}.");
            IReadOnlyList<PolygonRings> polygons = part.Geometry switch
            { PolygonGeometry polygon => [polygon.Rings], MultiPolygonGeometry multi => multi.Polygons, _ => [] };
            int polygonIndex = 0;
            foreach (var polygon in polygons)
            {
                var height = total/count;
                var baseY=entrances.Count==0 ? world.Terrain.PadY(id)+GroundLevels.Paving : entrances[0].Center.Y-(openFloors.Min()-1)*height;
                var levels = Enumerable.Range(1,count).Select(floor => new StoreyInfo(floor,
                    baseY+(floor-1)*height,height,polygon.Outer)).ToArray();
                result.Add(new(part.SourceId+"/"+(polygonIndex++).ToString(System.Globalization.CultureInfo.InvariantCulture),polygon,levels));
            }
        }
        if (result.Count == 0) throw new InvalidDataException($"Interior '{id.Value}' has no authored building footprint.");
        return result;
    }
}
