using System.Text.Json.Serialization;
using Campus.Contracts;

namespace Campus.Kernel.World;

/// <summary>Version 1 world segment. Runtime instance numbers and transition history never enter a save.</summary>
public sealed record WorldSaveV1(
    [property: JsonRequired] double WorldHours,
    [property: JsonRequired] double ClockOffset,
    [property: JsonRequired] double SeasonPos,
    [property: JsonRequired] Season SeasonTarget,
    [property: JsonRequired] int FlowRate,
    [property: JsonRequired] int FlowRateBeforePause,
    [property: JsonRequired] double AnimTime);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(WorldSaveV1))]
internal partial class WorldSaveJson : JsonSerializerContext { }
