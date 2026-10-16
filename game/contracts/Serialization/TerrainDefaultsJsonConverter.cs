using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts.Maps;

// Type-local boundary: global map/contract serialization options remain unchanged.
internal sealed class TerrainDefaultsJsonConverter : JsonConverter<TerrainDefaults>
{
    public TerrainDefaultsJsonConverter() { }

    public override TerrainDefaults Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("terrainDefaults must be an object or null.");
        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var field in document.RootElement.EnumerateObject())
        {
            if (field.Name is not ("lakeBankSlope" or "lakeMaxDepth" or "channelBankSlope" or "channelDepth" or "corridorShoulder") ||
                !fields.TryAdd(field.Name, field.Value))
                throw new JsonException($"Unknown or duplicate terrainDefaults field: {field.Name}");
        }
        if (fields.Count != 5) throw new JsonException("terrainDefaults requires all five explicit parameters.");
        var data = new TerrainDefaults(Text(fields["lakeBankSlope"]), Number(fields["lakeMaxDepth"]),
            Text(fields["channelBankSlope"]), Number(fields["channelDepth"]), Number(fields["corridorShoulder"]));
        try { TerrainOverlayJson.ValidateTerrainDefaults(data); }
        catch (ArgumentException error) { throw new JsonException(error.Message, error); }
        return data;
    }

    public override void Write(Utf8JsonWriter writer, TerrainDefaults value, JsonSerializerOptions options)
    {
        try { TerrainOverlayJson.ValidateTerrainDefaults(value); }
        catch (ArgumentException error) { throw new JsonException(error.Message, error); }
        writer.WriteStartObject();
        writer.WriteString("lakeBankSlope", value.LakeBankSlope);
        writer.WriteNumber("lakeMaxDepth", value.LakeMaxDepth);
        writer.WriteString("channelBankSlope", value.ChannelBankSlope);
        writer.WriteNumber("channelDepth", value.ChannelDepth);
        writer.WriteNumber("corridorShoulder", value.CorridorShoulder);
        writer.WriteEndObject();
    }

    private static string Text(JsonElement value) => value.ValueKind == JsonValueKind.String
        ? value.GetString()! : throw new JsonException("Terrain slope must be a positive 1:n string.");

    private static float Number(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out var number) || !float.IsFinite(number) || number <= 0)
            throw new JsonException("Terrain parameter must be a finite positive float.");
        return number;
    }
}
