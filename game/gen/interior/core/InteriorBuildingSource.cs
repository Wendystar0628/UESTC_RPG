using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Interior;

public sealed record InteriorBuildingPart(string Key, string SourceId, PolygonRings Footprint, IReadOnlyList<StoreyInfo> Storeys);

/// <summary>World height fixes the roof; an entrance anchors its declared floor within that complete building height.</summary>
public static class InteriorBuildingSource
{
    public static IReadOnlyList<InteriorBuildingPart> Read(IWorldModel world, FeatureId id, IReadOnlyList<EntranceDecl> entrances, IReadOnlyList<InteriorOpenPart> openParts)
    {
        if (!world.Features.TryGetValue(id, out var feature)) throw new InvalidDataException($"Missing interior feature '{id.Value}'.");
        IReadOnlyList<WorldFeaturePart> parts = feature.Parts.Count == 0
            ? [new("",feature.Geometry,feature.Height,feature.Storeys) { Props=feature.Props }] : feature.Parts;
        var openFloors = InteriorOpenPart.IndoorFloorsFor(openParts, id);
        var padY = world.Terrain.PadY(id);
        var result = new List<InteriorBuildingPart>();
        foreach (var part in parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
        {
            var kind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind",out var declared)
                ? declared.GetString() : feature.Kind.Value;
            if (kind != "building") continue;
            var total = part.Height ?? feature.Height ?? throw new InvalidDataException($"Interior '{id.Value}/{part.SourceId}' needs world height.");
            var count = part.Storeys ?? feature.Storeys ?? throw new InvalidDataException($"Interior '{id.Value}/{part.SourceId}' needs world storeys.");
            if (!float.IsFinite(total) || total <= 0 || count <= 0) throw new InvalidDataException($"Invalid interior world height/storeys: {id.Value}/{part.SourceId}.");
            var anchorFloor = entrances.Count == 0 ? 1 : openFloors.Min();
            if (anchorFloor < 1 || anchorFloor > count)
                throw new InvalidDataException($"Interior entrance floor is outside its world part: {id.Value}/{part.SourceId}.");
            var anchorY = entrances.Count == 0 ? padY + GroundLevels.Paving : entrances[0].Center.Y;
            var height = (padY + total - anchorY) / (count - anchorFloor + 1);
            var baseY = anchorY - (anchorFloor - 1) * height;
            if (!float.IsFinite(height) || height <= 0 || !float.IsFinite(baseY))
                throw new InvalidDataException($"Interior entrance leaves no valid height below the world roof: {id.Value}/{part.SourceId}.");
            IReadOnlyList<PolygonRings> polygons = part.Geometry switch
            { PolygonGeometry polygon => [polygon.Rings], MultiPolygonGeometry multi => multi.Polygons, _ => [] };
            int polygonIndex = 0;
            foreach (var polygon in polygons)
            {
                var levels = Enumerable.Range(1,count).Select(floor => new StoreyInfo(floor,
                    baseY+(floor-1)*height,height,polygon.Outer)).ToArray();
                result.Add(new(part.SourceId+"/"+(polygonIndex++).ToString(System.Globalization.CultureInfo.InvariantCulture),part.SourceId,polygon,levels));
            }
        }
        if (result.Count == 0) throw new InvalidDataException($"Interior '{id.Value}' has no authored building footprint.");
        return result;
    }
}
