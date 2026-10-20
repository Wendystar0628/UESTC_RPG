using System.Numerics;

namespace Campus.Contracts;

public readonly record struct FeatureId(string Value);
public readonly record struct PartId(string Value);
public readonly record struct ModuleId(string Value);
public readonly record struct ZoneId(string Value);
public readonly record struct ChannelId(string Value);
public readonly record struct PaletteToken(string Value);
public readonly record struct MapId(string Value);
public readonly record struct WaypointRef(MapId Map, string Id);
/// <summary>Cell index at the accompanying LOD: floor(worldXZ / (64 &lt;&lt; lod)); Tiles(ctx) emits L0 cells. L4 uses (0, 0) for the whole map.</summary>
public readonly record struct TileKey(int X, int Z);

public enum LodLevel : byte { L0 = 0, L1 = 1, L2 = 2, L3 = 3, L4 = 4 }
