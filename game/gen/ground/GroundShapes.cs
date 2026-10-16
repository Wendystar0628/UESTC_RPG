using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>Source surface classes in field order. Each class gets one signed distance field per tile.</summary>
internal enum SurfaceKind : byte { Water, Carriageway, Parking, Plaza, Walk, Court, Turf, Track, Building }

/// <summary>One source shape: a polygon with holes, or a centre line with a half width (a chain of capsules).</summary>
internal sealed class GroundShape
{
    public required SurfaceKind Kind { get; init; }
    public required FeatureId Id { get; init; }
    public Vector2[][]? Rings { get; init; }
    public Vector2[]? Line { get; init; }
    public float HalfWidth { get; init; }
    public required Vector2 Min { get; init; }
    public required Vector2 Max { get; init; }
}

/// <summary>A centre line of a carriageway or footpath from the road network; drives markings.</summary>
internal sealed record RoadLine(FeatureId Id, Vector2[] Points, float Width, bool Carriageway, string Type);

/// <summary>Maps world-model kinds and OSM-style road types to ground surface classes.</summary>
internal static class GroundClassifier
{
    // Default widths when the data has none (same table as the dataset builder that produced campus.json).
    private static float DefaultWidth(string type) => type switch
    {
        "footway" => 2.5f,
        "path" => 2f,
        "steps" => 2.5f,
        "cycleway" => 3f,
        "service" => 5f,
        "residential" => 6f,
        "tertiary" => 8f,
        "secondary" => 10f,
        "pedestrian" => 5f,
        _ => 4f
    };

    public static bool IsCarriageway(string type) => type switch
    {
        "service" or "residential" or "tertiary" or "secondary" or "primary" or "unclassified" or "living_street" or
            "construction" or "road" or "trunk" => true,
        _ => false
    };

    /// <summary>Surface class of a road with the given OSM highway type.</summary>
    public static SurfaceKind RoadKind(string type) => type switch
    {
        "pedestrian" => SurfaceKind.Plaza,
        "footway" or "path" or "cycleway" or "steps" or "track" or "bridleway" or "corridor" => SurfaceKind.Walk,
        _ => IsCarriageway(type) ? SurfaceKind.Carriageway : SurfaceKind.Walk
    };

    public static string RoadType(JsonElement? props) =>
        Text(props, "roadType") ?? Text(props, "highway") ?? "";

    /// <summary>
    /// Surface class of one source record from its kind and its own props; null for kinds that leave the lawn untouched
    /// (POIs, green areas). Buildings return <see cref="SurfaceKind.Building"/>: a footprint, not paving.
    /// </summary>
    public static SurfaceKind? KindOf(string kind, JsonElement? props)
    {
        switch (kind)
        {
            case "road":
                return RoadKind(RoadType(props));
            case "parking":
                return SurfaceKind.Parking;
            case "water":
                return SurfaceKind.Water;
            case "building":
                return SurfaceKind.Building;
            case "sport":
                return Text(props, "sport") switch
                {
                    "running" => SurfaceKind.Track,
                    "soccer" => SurfaceKind.Turf,
                    "swimming" => SurfaceKind.Water,
                    _ => SurfaceKind.Court
                };
            default:
                return null;
        }
    }

    public static float Width(JsonElement? props, string type, float given) =>
        given > 0f && float.IsFinite(given) ? given : Number(props, "width") is { } w && w > 0f ? w : DefaultWidth(type);

    public static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    public static float? Number(JsonElement? props, string name)
    {
        if (props is not { ValueKind: JsonValueKind.Object } p || !p.TryGetProperty(name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetSingle(out var f) && float.IsFinite(f)) return f;
        return null;
    }
}
