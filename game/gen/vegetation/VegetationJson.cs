using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Wire boundary of species.json and planting.json: typed records read by source-generated System.Text.Json, strict
/// (unknown keys, missing required keys and null in non-nullable fields are errors). Parameters with a default are the
/// optional keys. Range and reference checks that JSON cannot express stay in <see cref="SpeciesTable.Parse"/> and
/// <see cref="PlantingRules.Parse"/>. Owner, Doc and Notes are file commentary, never read.
/// </summary>
internal static class VegetationJson
{
    internal static VegetationJsonContext Context { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    {
        // The naming policy of a context built from runtime options comes from these options, not from attributes on the context.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        NumberHandling = JsonNumberHandling.Strict
    });

    /// <summary>Reads one document; shape errors surface as <see cref="InvalidDataException"/> carrying the JSON path.</summary>
    internal static T Read<T>(ReadOnlySpan<byte> json, JsonTypeInfo<T> type, string file) where T : class
    {
        try { return JsonSerializer.Deserialize(json, type) ?? throw new InvalidDataException($"{file}: empty document."); }
        catch (JsonException error) { throw new InvalidDataException($"{file}: {error.Message}", error); }
    }
}

internal sealed record SpeciesFile(string Format, IReadOnlyList<SpeciesRowWire> Species,
    string? Owner = null, string? Doc = null, IReadOnlyList<string>? Notes = null);

internal sealed record SpeciesRowWire(string Id, string Name, int Row, CrownType Crown, string Form, IReadOnlyList<float> Height,
    string Leaf, string? Bloom, CardShape Card, CardShape BloomCard, bool Deciduous, bool BloomOnBlobs,
    IReadOnlyList<float> LeafAmount, IReadOnlyList<float> BloomAmount, float SwayDeg, string Sound);

internal sealed record PlantingFile(string Format, IReadOnlyList<RoadWire> Roads, OsmTreesWire OsmTrees, GrovesWire Groves,
    PitsWire? Pits = null, UnderstoreyWire? Understorey = null,
    string? Owner = null, string? Doc = null, IReadOnlyList<string>? Notes = null);

internal sealed record RoadWire(string Name, IReadOnlyList<string> Species, IReadOnlyList<RowWire> Rows, float Spacing, bool Wash,
    BandWire? Band = null);

internal sealed record RowWire(float Offset, bool Stagger = false);

internal sealed record BandWire(IReadOnlyList<string> Species, float Offset, float Spacing);

internal sealed record OsmTreesWire(float Near, string Default);

internal sealed record PitsWire(string Square, string Round);

internal sealed record GrovesWire(Dictionary<string, float> Mix, IReadOnlyList<AreaWire> Areas, IReadOnlyList<WashAreaWire> WashNear);

internal sealed record AreaWire(string Name, float Radius, Dictionary<string, float> Mix);

internal sealed record WashAreaWire(string Name, float Radius);

internal sealed record UnderstoreyWire(PitCoverWire? PitCover = null, BorderWire? Borders = null, TuftWire? Tufts = null);

internal sealed record PitCoverWire(string Species, float Height, float Spacing);

internal sealed record BorderWire(float Spacing, float Inset, float Run, float Share, float HedgeShare, float Reach,
    IReadOnlyList<string> Hedge, Dictionary<string, float> Mix);

internal sealed record TuftWire(string Species, float Cell, float Edge, float Share, int PerTree);

[JsonSerializable(typeof(SpeciesFile))]
[JsonSerializable(typeof(PlantingFile))]
internal partial class VegetationJsonContext : JsonSerializerContext { }
