using System.Text.Json.Serialization;
using Campus.Contracts;

namespace Campus.Camera.Walk;

public sealed record WalkSave(Pose Feet, bool Crouching, int SpeedStep, IReadOnlyList<Pose> SafePoints);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WalkSave))]
public partial class WalkSaveJsonContext : JsonSerializerContext;
