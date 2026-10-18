using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Typed property reads shared by all generators. Missing optional values return null; malformed values fail.</summary>
public static class WorldProps
{
    public static JsonElement RequiredProps(this WorldFeature feature) =>
        feature.Props ?? throw new InvalidOperationException($"{feature.Id.Value} has no props.");

    public static string Text(this WorldFeature feature, params string[] path) => feature.RequiredProps().Text(path);
    public static string? OptionalText(this WorldFeature feature, params string[] path) => feature.Props?.OptionalText(path);
    public static float Number(this WorldFeature feature, params string[] path) => feature.RequiredProps().Number(path);
    public static float? OptionalNumber(this WorldFeature feature, params string[] path) => feature.Props?.OptionalNumber(path);
    public static float[] Numbers(this WorldFeature feature, params string[] path) => feature.RequiredProps().Numbers(path);
    public static Vector2 Pair(this WorldFeature feature, params string[] path) => feature.RequiredProps().Pair(path);
    public static string? TryText(this WorldFeature feature, params string[] path) => TryText(feature.Props, path);
    public static float? TryNumber(this WorldFeature feature, params string[] path) => TryNumber(feature.Props, path);

    /// <summary>Lenient authored metadata read: absent, null and wrong-type fields all mean no value.</summary>
    public static string? TryText(this JsonElement? props, params string[] path) =>
        props is { } source && Find(source, path, strict: false) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    public static float? TryNumber(this JsonElement? props, params string[] path) =>
        props is { } source && Find(source, path, strict: false) is { ValueKind: JsonValueKind.Number } value
        && value.TryGetSingle(out var number) && float.IsFinite(number) ? number : null;

    public static string Text(this JsonElement props, params string[] path) =>
        props.OptionalText(path) ?? throw Missing(path);

    public static string? OptionalText(this JsonElement props, params string[] path)
    {
        var value = Find(props, path);
        if (value is null || value.Value.ValueKind == JsonValueKind.Null) return null;
        return value.Value.ValueKind == JsonValueKind.String ? value.Value.GetString() : throw Missing(path);
    }

    public static float Number(this JsonElement props, params string[] path) =>
        props.OptionalNumber(path) ?? throw Missing(path);

    public static float? OptionalNumber(this JsonElement props, params string[] path)
    {
        var value = Find(props, path);
        if (value is null || value.Value.ValueKind == JsonValueKind.Null) return null;
        return value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetSingle(out var number) && float.IsFinite(number)
            ? number : throw Missing(path);
    }

    public static float[] Numbers(this JsonElement props, params string[] path)
    {
        var value = Find(props, path);
        if (value is not { ValueKind: JsonValueKind.Array } array) throw Missing(path);
        return array.EnumerateArray().Select(item => item.Number()).ToArray();
    }

    public static Vector2 Pair(this JsonElement props, params string[] path)
    {
        var numbers = props.Numbers(path);
        return numbers.Length == 2 ? new(numbers[0], numbers[1]) : throw Missing(path);
    }

    private static JsonElement? Find(JsonElement props, string[] path, bool strict = true)
    {
        foreach (var key in path)
        {
            if (props.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return null;
            if (props.ValueKind != JsonValueKind.Object)
                return strict ? throw Missing(path) : null;
            if (!props.TryGetProperty(key, out props)) return null;
        }
        return props.ValueKind == JsonValueKind.Undefined ? null : props;
    }

    private static InvalidOperationException Missing(string[] path) =>
        new($"Missing or invalid props.{string.Join('.', path)}.");
}
