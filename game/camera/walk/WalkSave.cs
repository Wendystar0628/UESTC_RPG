using System.Text.Json.Serialization;
using Campus.Contracts.Save;

namespace Campus.Camera.Walk;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WalkSaveData))]
public partial class WalkSaveJsonContext : JsonSerializerContext;
