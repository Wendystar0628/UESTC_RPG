using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts.Cinematic;

public sealed class CameraLookConverter : JsonConverter<CameraLook>
{
    public override CameraLook Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("Expected camera look object.");
        CameraLookMode? mode = null;
        float? lead = null, pitchBias = null, blendIn = null;
        Vector3? target = null;
        string? targetAnchor = null;
        CameraLookSettle? settle = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(property.Name)) throw new JsonException($"Duplicate look property: {property.Name}");
            switch (property.Name)
            {
                case "mode": mode = property.Value.Deserialize<CameraLookMode>(options); break;
                case "lead": lead = ReadNumber(property.Value); break;
                case "pitchBias": pitchBias = ReadNumber(property.Value); break;
                case "blendIn": blendIn = ReadNumber(property.Value); break;
                case "settle": settle = property.Value.Deserialize<CameraLookSettle>(options); break;
                case "target":
                    switch (property.Value.ValueKind)
                    {
                        case JsonValueKind.String:
                            targetAnchor = property.Value.GetString();
                            if (!CinematicAnchors.IsPoseAnchor(targetAnchor)) throw new JsonException("Unknown look target anchor.");
                            break;
                        case JsonValueKind.Array: target = property.Value.Deserialize<Vector3>(options); break;
                        case JsonValueKind.Null: break;
                        default: throw new JsonException("Look target must be a position or pose anchor.");
                    }
                    break;
                default: throw new JsonException($"Unknown camera look property: {property.Name}");
            }
        }
        if (mode is null) throw new JsonException("Camera look mode is required.");
        var result = new CameraLook(mode.Value, lead, pitchBias, target) { TargetAnchor = targetAnchor, Settle = settle, BlendIn = blendIn };
        Validate(result);
        return result;
    }

    public override void Write(Utf8JsonWriter writer, CameraLook value, JsonSerializerOptions options)
    {
        Validate(value);
        writer.WriteStartObject();
        writer.WritePropertyName("mode"); JsonSerializer.Serialize(writer, value.Mode, options);
        if (value.Lead is { } lead) writer.WriteNumber("lead", lead);
        if (value.PitchBias is { } pitchBias) writer.WriteNumber("pitchBias", pitchBias);
        if (value.Target is { } target)
        {
            writer.WritePropertyName("target"); JsonSerializer.Serialize(writer, target, options);
        }
        else if (value.TargetAnchor is { } anchor)
        {
            if (!CinematicAnchors.IsPoseAnchor(anchor)) throw new JsonException("Unknown look target anchor.");
            writer.WriteString("target", anchor);
        }
        if (value.Settle is { } settle)
        {
            writer.WritePropertyName("settle"); JsonSerializer.Serialize(writer, settle, options);
        }
        if (value.BlendIn is { } blendIn) writer.WriteNumber("blendIn", blendIn);
        writer.WriteEndObject();
    }

    private static float? ReadNumber(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.Number) throw new JsonException("Expected a look number.");
        var number = value.GetSingle();
        if (!float.IsFinite(number)) throw new JsonException("Look numbers must be finite.");
        return number;
    }

    private static void Validate(CameraLook value)
    {
        if (value.Lead is { } lead && (!float.IsFinite(lead) || lead < 0) ||
            value.BlendIn is { } blend && (!float.IsFinite(blend) || blend < 0) ||
            value.PitchBias is { } pitch && !float.IsFinite(pitch))
            throw new JsonException("Invalid look number.");
        if (value.Target is { } target && (!float.IsFinite(target.X) || !float.IsFinite(target.Y) || !float.IsFinite(target.Z)))
            throw new JsonException("Look target must be finite.");
        if (value.Target is not null && value.TargetAnchor is not null) throw new JsonException("Look has two targets.");
        if (value.TargetAnchor is { } anchor && !CinematicAnchors.IsPoseAnchor(anchor)) throw new JsonException("Unknown look target anchor.");
        if (value.Settle is { } settle && (!CinematicAnchors.IsPoseAnchor(settle.Anchor) || !float.IsFinite(settle.From) || settle.From is < 0 or > 1))
            throw new JsonException("Invalid look settle anchor or normalized progress.");
        switch (value.Mode)
        {
            case CameraLookMode.Fixed: break;
            case CameraLookMode.Ahead:
                if (value.Lead is null) throw new JsonException("Ahead look requires lead.");
                break;
            case CameraLookMode.Target:
                if (value.Target is null && value.TargetAnchor is null) throw new JsonException("Target look requires a position or anchor.");
                break;
            default: throw new JsonException("Unknown look mode.");
        }
    }
}

public static class CinematicAnchors
{
    // $current is captured once when a playback begins, never reread on each frame.
    public static bool IsPoseAnchor(string? anchor) => anchor switch
    {
        "$title" or "$current" or "$dest" or "$destApproach" => true,
        not null when anchor.StartsWith("$waypoint:", StringComparison.Ordinal) =>
            anchor.Length > 10 && anchor.AsSpan(10).IndexOfAnyExcept("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-".AsSpan()) < 0,
        _ => false,
    };
}

public static class CinematicTiming
{
    /// <summary>
    /// A position discontinuity, orientation snap, or velocity/rate-check exemption
    /// is permitted only at this fully covered timeline time. Fade ramps and
    /// letterboxes never qualify. The mask envelope is [T0,T1): with zero FadeOut,
    /// the mask has already disappeared at T1. Visible paths retain all normal checks.
    /// </summary>
    public static bool IsFullyCoveredAt(CinematicDef definition, double time)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!double.IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));
        var covered = false;
        foreach (var mask in definition.Masks)
        {
            if (!double.IsFinite(mask.T0) || !double.IsFinite(mask.T1) || mask.T0 < 0 || mask.T1 <= mask.T0 ||
                !double.IsFinite(mask.FadeIn) || !double.IsFinite(mask.FadeOut) || mask.FadeIn < 0 || mask.FadeOut < 0 ||
                mask.FadeIn + mask.FadeOut > mask.T1 - mask.T0)
                throw new ArgumentException("Invalid cinematic mask envelope.", nameof(definition));
            var fullScreen = mask.Kind switch
            {
                MaskKind.CloudCover or MaskKind.Veil => true,
                MaskKind.Letterbox => false,
                _ => throw new ArgumentOutOfRangeException(nameof(definition)),
            };
            if (fullScreen && time >= mask.T0 + mask.FadeIn && time <= mask.T1 - mask.FadeOut && time < mask.T1) covered = true;
        }
        return covered;
    }
}
