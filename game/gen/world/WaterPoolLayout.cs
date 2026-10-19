using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.World;

public readonly record struct WaterPoolFrame(Vector2 Origin, Vector2 Along, Vector2 Across, float Length, float Width)
{
    public Vector2 Coordinates(Vector2 point) => new(Vector2.Dot(point - Origin, Along), Vector2.Dot(point - Origin, Across));
}

/// <summary>Pool identity and the authored axis shared by terrain, water and pool structures.</summary>
public static class WaterPoolLayout
{
    public static bool IsPool(WorldFeature feature) => feature.Props is { ValueKind: JsonValueKind.Object } props &&
        (props.TryGetProperty("leisure", out var leisure) && leisure.GetString() == "swimming_pool" ||
         props.TryGetProperty("sport", out var sport) && sport.GetString() == "swimming");

    public static WaterPoolFrame Frame(WorldFeature feature)
    {
        if (feature.Props is not { ValueKind: JsonValueKind.Object } props ||
            !props.TryGetProperty("poolAxis", out var axis) || axis.GetArrayLength() != 2)
            throw new WorldDataException("world", feature.Id.Value, "Pool requires its authored poolAxis endpoints.");
        var origin = new Vector2(axis[0][0].GetSingle(), axis[0][1].GetSingle());
        var end = new Vector2(axis[1][0].GetSingle(), axis[1][1].GetSingle());
        var length = Vector2.Distance(origin, end);
        if (!float.IsFinite(length) || length <= 0)
            throw new WorldDataException("world", feature.Id.Value, "Pool axis must have positive finite length.");
        var along = (end - origin) / length;
        var across = new Vector2(along.Y, -along.X);
        var polygons = feature.Parts.Count > 0 ? feature.Parts.Select(p => p.Geometry) : [feature.Geometry];
        var points = polygons.SelectMany(geometry => geometry switch
        {
            PolygonGeometry p => p.Rings.Outer,
            MultiPolygonGeometry m => m.Polygons.SelectMany(p => p.Outer),
            _ => Enumerable.Empty<Vector2>()
        }).Select(point => Vector2.Dot(point - origin, across)).ToArray();
        if (points.Length == 0) throw new WorldDataException("world", feature.Id.Value, "Pool requires a polygon.");
        return new(origin, along, across, length, points.Max() - points.Min());
    }
}
