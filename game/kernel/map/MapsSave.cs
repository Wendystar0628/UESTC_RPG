using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Maps;

public sealed record MapsSave(MapId Current, IReadOnlyList<MapVisit> Visits);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(MapsSave))]
internal partial class MapsSaveJsonContext : JsonSerializerContext
{
    public static MapsSaveJsonContext Contract { get; } = new(new System.Text.Json.JsonSerializerOptions(ContractJson.Options)
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow });
}
