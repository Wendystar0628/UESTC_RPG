using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Save;

namespace Campus.Camera;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ViewSaveData))]
public partial class ViewJsonContext : JsonSerializerContext
{
    public static ViewJsonContext Contract { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    });
}
