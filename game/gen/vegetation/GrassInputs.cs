using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Vegetation;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Reads grass-area additions from the current prepared world's ordinary overlay/&lt;group&gt;.d shards.
/// Geometry is polygon or multiPolygon; props has required category (short, tall, wildflower, ornamental,
/// exclude) and optional nonnegative setbackMeters. Overlay owner, zone, map and reason keep their existing meanings.
/// No file IO, default planting extent or terrain sampling occurs here. GroundSection.Lawn remains the grass-colour authority.
/// </summary>
public static class GrassInputs
{
    public static GrassRuntimeData Read(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var areas = new List<GrassAreaData>();
        foreach (var feature in context.World.Features.Values.Where(f => f.Kind.Value == "grass-area")
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not (PolygonGeometry or MultiPolygonGeometry))
                throw Invalid(feature, "geometry must be polygon or multiPolygon.");
            var polygons = feature.Polygons();
            if (polygons.Count == 0)
                throw Invalid(feature, "geometry needs at least one polygon.");
            if (feature.Props is not { ValueKind: JsonValueKind.Object } props)
                throw Invalid(feature, "props must contain category.");
            GrassAreaProperties properties;
            try
            {
                properties = props.Deserialize(GrassJson.Context.GrassAreaProperties)
                    ?? throw Invalid(feature, "props cannot be null.");
            }
            catch (JsonException error)
            {
                throw new InvalidDataException($"grass-area {feature.Id.Value}: {error.Message}", error);
            }
            if (properties.SetbackMeters is { } setback && (!float.IsFinite(setback) || setback < 0f))
                throw Invalid(feature, "setbackMeters must be finite and nonnegative.");
            areas.Add(new GrassAreaData(feature.Id, feature.Zone, properties.Category, polygons, properties.SetbackMeters));
        }
        return new GrassRuntimeData(context.Map.Id, areas);
    }

    private static InvalidDataException Invalid(WorldFeature feature, string message) =>
        new($"grass-area {feature.Id.Value}: {message}");
}

internal sealed record GrassAreaProperties(GrassCategory Category, float? SetbackMeters = null);

internal static class GrassJson
{
    internal static GrassJsonContext Context { get; } = new(new JsonSerializerOptions(WireJson.Options));
}

[JsonSerializable(typeof(GrassAreaProperties))]
internal partial class GrassJsonContext : JsonSerializerContext { }
