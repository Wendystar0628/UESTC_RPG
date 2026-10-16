using System.Numerics;
using System.Text.Json;

namespace Campus.Contracts;

/// <summary>Pure parsing of one authored interiors.json entrance into absolute world coordinates.</summary>
public static class InteriorEntranceJson
{
    /// <summary>
    /// Missing heightRef and "absolute" preserve authored coordinates without querying terrain.
    /// "pad" adds terrain.PadY(feature) once to all three Y coordinates; authored floor offsets remain intact.
    /// The result contains no height reference. Call only on raw catalog records, never on a resolved EntranceDecl.
    /// </summary>
    /// <remarks>
    /// The caller proves feature ownership and supplies the intended map's terrain. This parser checks that the
    /// record names the same feature, but does not prove terrain readiness or substitute a flat implementation.
    /// An explicitly supplied terrain may legitimately return zero. No IO, registry or engine dependency.
    /// </remarks>
    public static EntranceDecl Parse(JsonElement entrance, FeatureId feature, ITerrain? terrain = null)
    {
        if (entrance.ValueKind != JsonValueKind.Object) throw new JsonException("An entrance must be an object.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in entrance.EnumerateObject())
        {
            if (property.Name is not ("id" or "featureId" or "doorCenter" or "facingDeg" or
                "outsideLanding" or "insideLanding" or "clearWidth" or "open" or "heightRef"))
                throw new JsonException($"Unknown entrance field: {property.Name}.");
            if (!seen.Add(property.Name)) throw new JsonException($"Duplicate entrance field: {property.Name}.");
        }

        var id = Text(Required(entrance, "id"), "id");
        var featureId = Text(Required(entrance, "featureId"), "featureId");
        if (!StringComparer.Ordinal.Equals(featureId, feature.Value))
            throw new JsonException($"Entrance '{id}' belongs to '{featureId}', not '{feature.Value}'.");
        var outside = Vector(Required(entrance, "outsideLanding"), "outsideLanding");
        var inside = Vector(Required(entrance, "insideLanding"), "insideLanding");
        var center = Vector(Required(entrance, "doorCenter"), "doorCenter");
        var width = Number(Required(entrance, "clearWidth"), "clearWidth");
        var yaw = Number(Required(entrance, "facingDeg"), "facingDeg");
        if (width < 1.1f) throw new JsonException($"Entrance '{id}' clearWidth must be at least 1.1 m.");
        var open = Required(entrance, "open");
        if (open.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new JsonException($"Entrance '{id}' open must be a boolean.");
        var heightRef = entrance.TryGetProperty("heightRef", out var reference)
            ? Text(reference, "heightRef") : "absolute";
        switch (heightRef)
        {
            case "absolute":
                break;
            case "pad":
                if (terrain is null)
                    throw new JsonException($"Entrance '{id}' on '{featureId}' requires explicit terrain for heightRef 'pad'.");
                float padY;
                try { padY = terrain.PadY(feature); }
                catch (Exception error) when (error is ArgumentException or KeyNotFoundException or NotSupportedException or InvalidOperationException)
                {
                    throw new JsonException($"Cannot resolve pad height for entrance '{id}' on '{featureId}'.", error);
                }
                if (!float.IsFinite(padY))
                    throw new JsonException($"Pad height for entrance '{id}' on '{featureId}' must be finite.");
                outside = Raise(outside, padY, id, "outsideLanding");
                inside = Raise(inside, padY, id, "insideLanding");
                center = Raise(center, padY, id, "doorCenter");
                break;
            default:
                throw new JsonException($"Unknown heightRef '{heightRef}' for entrance '{id}'; expected 'absolute' or 'pad'.");
        }
        return new(id, outside, inside, width, open.GetBoolean()) { Center = center, YawDeg = yaw };
    }

    private static JsonElement Required(JsonElement value, string name) => value.TryGetProperty(name, out var field)
        ? field : throw new JsonException($"Missing entrance field: {name}.");

    private static string Text(JsonElement value, string name) => value.ValueKind == JsonValueKind.String
        && value.GetString() is { Length: > 0 } text ? text : throw new JsonException($"Entrance {name} must be a nonempty string.");

    private static float Number(JsonElement value, string name) => value.ValueKind == JsonValueKind.Number
        && value.TryGetSingle(out var number) && float.IsFinite(number) ? number
        : throw new JsonException($"Entrance {name} must be a finite number.");

    private static Vector3 Vector(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 3)
            throw new JsonException($"Entrance {name} must contain exactly three numbers.");
        return new(Number(value[0], name), Number(value[1], name), Number(value[2], name));
    }

    private static Vector3 Raise(Vector3 point, float padY, string id, string name)
    {
        point.Y += padY;
        if (!float.IsFinite(point.Y)) throw new JsonException($"Entrance '{id}' resolved {name}.Y must be finite.");
        return point;
    }
}
