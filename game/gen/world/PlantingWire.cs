using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>
/// The strict wire reader of the per-map data files (planting.json, species.json): source-generated System.Text.Json over
/// typed records, where unknown keys, missing required keys and null in non-nullable fields are errors. Parameters with a
/// default are the optional keys. Owner, Doc and Notes are file commentary, never read.
/// </summary>
public static class WireJson
{
    public static JsonSerializerOptions Options { get; } = new(ContractJson.Options)
    {
        // The naming policy of a context built from runtime options comes from these options, not from attributes on the context.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        NumberHandling = JsonNumberHandling.Strict
    };

    /// <summary>Reads one document; shape errors surface as <see cref="InvalidDataException"/> carrying the JSON path.</summary>
    public static T Read<T>(ReadOnlySpan<byte> json, JsonTypeInfo<T> type, string file) where T : class
    {
        try { return JsonSerializer.Deserialize(json, type) ?? throw new InvalidDataException($"{file}: empty document."); }
        catch (JsonException error) { throw new InvalidDataException($"{file}: {error.Message}", error); }
    }
}

/// <summary>The one reader of <c>vegetation/planting.json</c>: vegetation checks and uses it all, furniture takes the road rows.</summary>
public static class PlantingJson
{
    private static readonly PlantingJsonContext Context = new(new JsonSerializerOptions(WireJson.Options));

    public static PlantingFile Read(ReadOnlySpan<byte> json) => WireJson.Read(json, Context.PlantingFile, "planting");
}

public sealed record PlantingFile(string Format, IReadOnlyList<RoadWire> Roads, OsmTreesWire OsmTrees, GrovesWire Groves,
    PitsWire? Pits = null, UnderstoreyWire? Understorey = null,
    string? Owner = null, string? Doc = null, IReadOnlyList<string>? Notes = null);

public sealed record RoadWire(string Name, IReadOnlyList<string> Species, IReadOnlyList<RowWire> Rows, float Spacing, bool Wash,
    BandWire? Band = null);

public sealed record RowWire(float Offset, bool Stagger = false);

public sealed record BandWire(IReadOnlyList<string> Species, float Offset, float Spacing);

public sealed record OsmTreesWire(float Near, string Default);

public sealed record PitsWire(string Square, string Round);

public sealed record GrovesWire(Dictionary<string, float> Mix, IReadOnlyList<AreaWire> Areas, IReadOnlyList<WashAreaWire> WashNear);

public sealed record AreaWire(string Name, float Radius, Dictionary<string, float> Mix);

public sealed record WashAreaWire(string Name, float Radius);

public sealed record UnderstoreyWire(PitCoverWire? PitCover = null, BorderWire? Borders = null);

public sealed record PitCoverWire(string Species, float Height, float Spacing);

public sealed record BorderWire(float Spacing, float Inset, float Run, float Share, float HedgeShare, float Reach,
    IReadOnlyList<string> Hedge, Dictionary<string, float> Mix);

[JsonSerializable(typeof(PlantingFile))]
internal partial class PlantingJsonContext : JsonSerializerContext { }
