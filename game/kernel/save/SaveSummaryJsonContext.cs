using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;

namespace Campus.Kernel.Save;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Pose))]
[JsonSerializable(typeof(ViewMode))]
[JsonSerializable(typeof(NavSpace))]
[JsonSerializable(typeof(WeatherKind))]
internal partial class SaveSummaryJsonContext : JsonSerializerContext
{
    internal static SaveSummaryJsonContext Strict { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    });
}
