using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

/// <summary>UI-VISUAL 3.8 color token data. This table never enters the 3D palette.</summary>
public sealed record UiPaletteDocument(
    [property: JsonPropertyName("ramps")] IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Ramps,
    [property: JsonPropertyName("special")] IReadOnlyDictionary<string, UiColorToken> Special,
    [property: JsonPropertyName("themes")] UiThemePalette Themes,
    [property: JsonPropertyName("map")] UiMapPalette Map,
    [property: JsonPropertyName("$schema"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Schema = null);

public sealed record UiThemePalette(
    [property: JsonPropertyName("light")] IReadOnlyDictionary<string, string> Light,
    [property: JsonPropertyName("dark")] IReadOnlyDictionary<string, string> Dark);

public sealed record UiMapPalette(
    [property: JsonPropertyName("light")] IReadOnlyDictionary<string, UiColorToken> Light,
    [property: JsonPropertyName("dark")] IReadOnlyDictionary<string, UiColorToken> Dark);

/// <summary>A color/reference string, or named color components such as fill/line and seasonal green.</summary>
[JsonConverter(typeof(UiColorTokenJsonConverter))]
public sealed record UiColorToken(
    string? Color = null,
    IReadOnlyDictionary<string, UiColorToken>? Components = null,
    bool? Scene = null);

public sealed class UiColorTokenJsonConverter : JsonConverter<UiColorToken>
{
    public override UiColorToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new UiColorToken(Color: reader.GetString());
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("A UI token must be a color or a component object.");

        var components = new Dictionary<string, UiColorToken>(StringComparer.Ordinal);
        bool? scene = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (components.Count == 0) throw new JsonException("A UI token object needs at least one color component.");
                return new UiColorToken(Components: components, Scene: scene);
            }
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
            var name = reader.GetString()!;
            if (!reader.Read()) throw new JsonException();
            if (name == "scene")
            {
                if (scene.HasValue || reader.TokenType is not (JsonTokenType.True or JsonTokenType.False)) throw new JsonException();
                scene = reader.GetBoolean();
            }
            else if (!components.TryAdd(name, Read(ref reader, typeToConvert, options)))
                throw new JsonException("Duplicate UI color component.");
        }
        throw new JsonException("Incomplete UI token object.");
    }

    public override void Write(Utf8JsonWriter writer, UiColorToken value, JsonSerializerOptions options)
    {
        if (value.Color is { } color)
        {
            if (value.Components is not null || value.Scene.HasValue) throw new JsonException("A string token cannot have components.");
            writer.WriteStringValue(color);
            return;
        }
        if (value.Components is null || value.Components.Count == 0) throw new JsonException("A UI token needs a color or components.");
        writer.WriteStartObject();
        foreach (var entry in value.Components.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (entry.Key == "scene") throw new JsonException("scene is reserved for the scenery annotation.");
            writer.WritePropertyName(entry.Key);
            Write(writer, entry.Value, options);
        }
        if (value.Scene is { } scene) writer.WriteBoolean("scene", scene);
        writer.WriteEndObject();
    }
}
