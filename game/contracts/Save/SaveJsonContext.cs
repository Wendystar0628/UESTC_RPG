using System.Text.Json.Serialization;
using Campus.Contracts.Atlas;

namespace Campus.Contracts.Save;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SaveMeta))]
[JsonSerializable(typeof(AtlasSave))]
internal partial class SaveJsonContext : JsonSerializerContext
{
    public static SaveJsonContext Contract { get; } = new(ContractJson.Options);
}
