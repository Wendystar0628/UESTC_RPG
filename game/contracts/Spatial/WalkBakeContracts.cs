using System.Numerics;

namespace Campus.Contracts;

/// <summary>Resident 2 m classification grid. Index = z * Width + x; bounds are minimum-inclusive, maximum-exclusive.</summary>
/// <remarks>Origin is MapDescriptor.DataRect.Min and is aligned to 8 m. Classes encode WalkClass as a byte.
/// SupportY is absolute world Y for Walk, Interior and Wade; all other classes store zero.</remarks>
public sealed record WalkGrid(Vector2 Origin, int Width, int Height, byte[] Classes, byte[] Flags, float[] SupportY)
{
    public const float CellSize = 2f;
    /// <summary>Any part of the cell is within 3 m of the campus boundary; other flag bits are reserved.</summary>
    public const byte NearBoundaryFlag = 1;

    public void Validate()
    {
        WalkGridValidation.ValidateOrigin(Origin);
        int count = WalkGridValidation.CellCount(Width, Height);
        if (Classes.Length != count || Flags.Length != count || SupportY.Length != count)
            throw new ArgumentException("Walk-grid arrays must match Width * Height.");
        for (int i = 0; i < count; i++)
        {
            bool supported = (WalkClass)Classes[i] switch
            {
                WalkClass.Walk or WalkClass.Interior or WalkClass.Wade => true,
                WalkClass.Building or WalkClass.Water or WalkClass.Forbidden or WalkClass.Outside or WalkClass.Steep => false,
                _ => throw new ArgumentException("Unknown walk classification.")
            };
            if ((Flags[i] & ~NearBoundaryFlag) != 0) throw new ArgumentException("Unknown walk-grid flag bits.");
            if (!float.IsFinite(SupportY[i]) || (!supported && SupportY[i] != 0))
                throw new ArgumentException("Support heights must be finite; unsupported cells store zero.");
        }
    }
}

/// <summary>Resident 8 m nearest-safe-point grid, with XYZ-interleaved coordinates in row-major cell order.</summary>
/// <remarks>World = QuantizationOrigin + Coordinates * 0.25 m. QuantizationOrigin is explicit, normally
/// (map rectangle centre X, 0, map rectangle centre Z). Producers round relative X/Z to nearest (ties-to-even)
/// and round relative Y toward positive infinity so a decoded safe point is never below its source surface.
/// Valid coordinates are -32767 through 32767; reject overflow, never clamp. Three MinValues mark no safe point;
/// mixed sentinel components are invalid. Grid queries still check actual Euclidean maxDist and adjacent cells.</remarks>
public sealed record SafePointGrid(Vector2 Origin, int Width, int Height, Vector3 QuantizationOrigin, short[] Coordinates)
{
    public const float CellSize = 8f;
    public const float CoordinateStep = 0.25f;
    public const short InvalidCoordinate = short.MinValue;

    public void Validate()
    {
        WalkGridValidation.ValidateOrigin(Origin);
        int count = WalkGridValidation.CellCount(Width, Height);
        if (!WalkGridValidation.Finite(QuantizationOrigin)) throw new ArgumentException("Quantization origin must be finite.");
        if ((long)count * 3 != Coordinates.LongLength) throw new ArgumentException("Safe-point coordinates must contain three values per cell.");
        for (int i = 0; i < count; i++) _ = TryDecode(i, out _);
    }

    /// <summary>Decodes a flat cell index; false means all three components contain the invalid sentinel.</summary>
    public bool TryDecode(int index, out Vector3 world)
    {
        int count = WalkGridValidation.CellCount(Width, Height);
        if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
        int offset = checked(index * 3);
        if (Coordinates.Length - offset < 3) throw new ArgumentException("Truncated safe-point coordinates.");
        short x = Coordinates[offset], y = Coordinates[offset + 1], z = Coordinates[offset + 2];
        bool invalidX = x == InvalidCoordinate, invalidY = y == InvalidCoordinate, invalidZ = z == InvalidCoordinate;
        if (invalidX || invalidY || invalidZ)
        {
            if (!(invalidX && invalidY && invalidZ)) throw new ArgumentException("Safe-point sentinel must occupy all three components.");
            world = default;
            return false;
        }
        world = QuantizationOrigin + new Vector3(x, y, z) * CoordinateStep;
        if (!WalkGridValidation.Finite(world)) throw new ArgumentException("Decoded safe point is not finite.");
        return true;
    }
}

/// <summary>Per-map startup data, generated offline. It is independent of physics tile residency.</summary>
/// <remarks>InputHash is lowercase SHA-256. Hash the UTF-8 prefix "campus-walk-v1\0", then source files ordered
/// by ordinal slash-separated relative path. Each item is uint32LE UTF-8 path length, path bytes,
/// uint64LE raw file length, raw bytes. Include map/campus/network/zones and every input overlay;
/// exclude generated outputs and downstream fidelity data. Changes to fixed bake rules require a version bump.</remarks>
public sealed record WalkBakeData(int Version, MapId Map, string InputHash, WalkGrid Walk,
    SafePointGrid SafePoints, IReadOnlyList<WallCollider> Walls)
{
    public const int CurrentVersion = 1;

    public void Validate()
    {
        if (Version != CurrentVersion) throw new ArgumentException("Unsupported walk bake version.");
        if (string.IsNullOrWhiteSpace(Map.Value)) throw new ArgumentException("Walk bake map is required.");
        if (InputHash.Length != 64 || InputHash.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("Walk input hash must be 64 lowercase hexadecimal characters.");
        Walk.Validate(); SafePoints.Validate();
        if (Walk.Origin != SafePoints.Origin || (long)SafePoints.Width * 4 != Walk.Width || (long)SafePoints.Height * 4 != Walk.Height)
            throw new ArgumentException("Walk and safe-point grids must cover the same map rectangle.");
        Vector2 max = Walk.Origin + new Vector2(Walk.Width * WalkGrid.CellSize, Walk.Height * WalkGrid.CellSize);
        for (int i = 0; i < SafePoints.Width * SafePoints.Height; i++)
        {
            if (!SafePoints.TryDecode(i, out Vector3 point)) continue;
            if (point.X < Walk.Origin.X || point.X >= max.X || point.Z < Walk.Origin.Y || point.Z >= max.Y)
                throw new ArgumentException("Safe point lies outside the map data rectangle.");
            int x = (int)((point.X - Walk.Origin.X) / WalkGrid.CellSize);
            int z = (int)((point.Z - Walk.Origin.Y) / WalkGrid.CellSize);
            int cell = z * Walk.Width + x;
            if ((WalkClass)Walk.Classes[cell] is not (WalkClass.Walk or WalkClass.Interior))
                throw new ArgumentException("Safe points must lie on dry walkable or interior cells.");
            float support = Walk.SupportY[cell];
            if (point.Y < support || point.Y - support > SafePointGrid.CoordinateStep + 0.0001f)
                throw new ArgumentException("Quantized safe-point height must be on or immediately above support.");
        }
        foreach (WallCollider wall in Walls)
        {
            if (!float.IsFinite(wall.A.X) || !float.IsFinite(wall.A.Y) || !float.IsFinite(wall.B.X) || !float.IsFinite(wall.B.Y)
                || !float.IsFinite(wall.HalfT) || wall.HalfT <= 0 || !float.IsFinite(wall.Y0) || !float.IsFinite(wall.Y1)
                || wall.Y1 <= wall.Y0 || wall.A == wall.B)
                throw new ArgumentException("Walk bake wall dimensions must be finite and nondegenerate.");
        }
    }
}

/// <summary>World-model access to shared walk data without referencing its generator assembly.</summary>
public interface IWalkDataSource
{
    WalkBakeData? Walk { get; }
}

internal static class WalkGridValidation
{
    internal static int CellCount(int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height > Array.MaxLength)
            throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive and bounded by an array length.");
        return width * height;
    }

    internal static void ValidateOrigin(Vector2 origin)
    {
        if (!float.IsFinite(origin.X) || !float.IsFinite(origin.Y) || origin.X % 8 != 0 || origin.Y % 8 != 0)
            throw new ArgumentException("Grid origin must be finite and aligned to 8 metres.");
    }

    internal static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
