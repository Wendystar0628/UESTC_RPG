
namespace Campus.Contracts.Bake;

public readonly record struct BakeKey(MapId Map, [property: DataId] string Module, string Detail);
