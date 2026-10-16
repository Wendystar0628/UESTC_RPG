using System.Text.Json;
using Campus.Contracts;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

/// <summary>
/// The planted tree rows of the furnished roads, read from the map's frozen build inputs (the map input
/// <see cref="FurnitureStyle.PlantingKey"/>, <c>vegetation/planting.json</c> relative to the map's DataRoot), never from WP-23's
/// code: for each road of <see cref="FurnitureStyle.Roads"/> the offsets of its <c>rows</c> and its <c>band</c> from the centre
/// line. Anything the input cannot say (no input, unreadable JSON, a road it does not list) keeps that road's fallback rows from
/// <see cref="FurnitureStyle.Roads"/> and adds one warning diagnostic. Pure and deterministic: the result depends on the bytes only.
/// </summary>
internal static class PlantingRows
{
    public const string MissingCode = "furniture.planting.missing";
    public const string UnreadableCode = "furniture.planting.unreadable";
    public const string RoadCode = "furniture.planting.road";

    public static (IReadOnlyList<RoadRule> Roads, IReadOnlyList<BuildDiagnostic> Diagnostics) Resolve(IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs) =>
        Resolve(inputs, S.Roads);

    /// <summary>The same for a given list of road rules (the style's own is the one the module uses).</summary>
    public static (IReadOnlyList<RoadRule> Roads, IReadOnlyList<BuildDiagnostic> Diagnostics) Resolve(IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs, IReadOnlyList<RoadRule> rules)
    {
        if (inputs is null || !inputs.TryGetValue(S.PlantingKey, out var bytes))
            return (rules, [Warn(MissingCode, $"Build input {S.PlantingKey} is not available; tree rows fall back to FurnitureStyle.Roads.")]);
        JsonDocument document;
        try { document = JsonDocument.Parse(bytes); }
        catch (JsonException) { return (rules, [Warn(UnreadableCode, $"Build input {S.PlantingKey} is not valid JSON; tree rows fall back to FurnitureStyle.Roads.")]); }
        using (document)
        {
            if (document.RootElement is not { ValueKind: JsonValueKind.Object } root || !root.TryGetProperty("roads", out var roads) || roads.ValueKind != JsonValueKind.Array)
                return (rules, [Warn(UnreadableCode, $"Build input {S.PlantingKey} has no roads list; tree rows fall back to FurnitureStyle.Roads.")]);
            var resolved = new List<RoadRule>();
            var diagnostics = new List<BuildDiagnostic>();
            foreach (var rule in rules)
            {
                var offsets = Find(roads, rule.Name) is { } road ? Offsets(road) : null;
                if (offsets is null)
                {
                    diagnostics.Add(Warn(RoadCode, $"Build input {S.PlantingKey} has no readable tree rows for road {rule.Name}; its rows fall back to FurnitureStyle.Roads."));
                    resolved.Add(rule);
                }
                else resolved.Add(rule with { Rows = Array.AsReadOnly(offsets) });
            }
            return (Array.AsReadOnly([.. resolved]), diagnostics.AsReadOnly());
        }
    }

    /// <summary>The road entry of the planting list with this name (the first one).</summary>
    private static JsonElement? Find(JsonElement roads, string name)
    {
        foreach (var road in roads.EnumerateArray())
            if (road.ValueKind == JsonValueKind.Object && road.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String && n.GetString() == name)
                return road;
        return null;
    }

    /// <summary>The offsets of a road's <c>rows</c>, then its <c>band</c> when it has one; null when the entry has no rows list or a bad offset.</summary>
    private static float[]? Offsets(JsonElement road)
    {
        if (!road.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array) return null;
        var offsets = new List<float>();
        foreach (var row in rows.EnumerateArray())
        {
            if (!Offset(row, out var offset)) return null;
            offsets.Add(offset);
        }
        if (road.TryGetProperty("band", out var band) && band.ValueKind != JsonValueKind.Null)
        {
            if (!Offset(band, out var offset)) return null;
            offsets.Add(offset);
        }
        return [.. offsets];
    }

    private static bool Offset(JsonElement element, out float offset)
    {
        offset = 0f;
        return element is { ValueKind: JsonValueKind.Object } && element.TryGetProperty("offset", out var value) && value.ValueKind == JsonValueKind.Number
            && value.TryGetSingle(out offset) && float.IsFinite(offset) && offset >= 0f;
    }

    private static BuildDiagnostic Warn(string code, string message) => new(DiagnosticLevel.Warning, code, message);
}
