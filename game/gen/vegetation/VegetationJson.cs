using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Wire boundary of species.json: typed records read by source-generated System.Text.Json through <see cref="WireJson"/>
/// (strict). Range and reference checks that JSON cannot express stay in <see cref="SpeciesTable.Parse"/>. planting.json is
/// read by <see cref="PlantingJson"/>, shared with furniture.
/// </summary>
internal static class VegetationJson
{
    internal static VegetationJsonContext Context { get; } = new(new JsonSerializerOptions(WireJson.Options));
}

internal sealed record SpeciesFile(string Format, IReadOnlyList<SpeciesRowWire> Species,
    string? Owner = null, string? Doc = null, IReadOnlyList<string>? Notes = null);

internal sealed record SpeciesRowWire(string Id, string Name, int Row, CrownType Crown, string Form, IReadOnlyList<float> Height,
    string Leaf, string? Bloom, CardShape Card, CardShape BloomCard, LitterShape Litter, bool Deciduous, bool BloomOnBlobs,
    IReadOnlyList<float> LeafAmount, IReadOnlyList<float> BloomAmount, float SwayDeg, string Sound);

[JsonSerializable(typeof(SpeciesFile))]
internal partial class VegetationJsonContext : JsonSerializerContext { }
