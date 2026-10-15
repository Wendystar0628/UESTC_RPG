using System.Numerics;

namespace Campus.Contracts;

public readonly record struct FeatureId(string Value);
public readonly record struct PartId(string Value);
public readonly record struct ModuleId(string Value);
public readonly record struct ZoneId(string Value);
public readonly record struct WpId(string Value);
public readonly record struct ChannelId(string Value);
public readonly record struct PaletteToken(string Value);
public readonly record struct MapId(string Value);
public readonly record struct WaypointRef(MapId Map, string Id);
public readonly record struct TileKey(int X, int Z);

public readonly record struct Box3(Vector3 Min, Vector3 Max)
{
    public Vector3 Size => Max - Min;
    public Vector3 Center => (Min + Max) * 0.5f;
    public bool Contains(Vector3 point) =>
        point.X >= Min.X && point.X <= Max.X &&
        point.Y >= Min.Y && point.Y <= Max.Y &&
        point.Z >= Min.Z && point.Z <= Max.Z;
    public bool Intersects(Box3 other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X &&
        Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
        Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
}

public enum LodLevel : byte { L0 = 0, L1 = 1, L2 = 2, L3 = 3, L4 = 4 }
