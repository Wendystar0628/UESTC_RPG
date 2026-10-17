using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;

namespace Campus.Camera;

public sealed record ViewSaveData(ViewMode Mode, Pose Pose, NavSpace? Space, int SpeedStep, int FlySpeedStep);

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
