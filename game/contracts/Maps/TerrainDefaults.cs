using System.Text.Json.Serialization;

namespace Campus.Contracts.Maps;

/// <summary>Authored parameters for automatic terrain operations only. No values are inferred when absent.</summary>
[JsonConverter(typeof(TerrainDefaultsJsonConverter))]
public sealed record TerrainDefaults(string LakeBankSlope, float LakeMaxDepth,
    string ChannelBankSlope, float ChannelDepth, float CorridorShoulder);
