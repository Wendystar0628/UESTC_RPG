namespace Campus.Contracts;

/// <summary>Contract-28 route B. Logical colour indices and seasonal sample coordinates are separate domains.</summary>
public static class PaletteLayout
{
    public const int EncodingVersion = 2;
    public const string LayoutId = "banked9-rows-v1";
    public const int LegacyCapacity = 256;
    public const int Capacity = 512;
    public const int Width = 256;
    public const int LegacyHeight = 64;
    public const int Height = 68;
    public const int FrameRowCount = 4;
    public const int FirstSeasonalRow = 4;
    public const int SeasonalRowCount = 60;
    public const int SeasonSampleCount = 256;
    public const int LegacyByteCount = Width * LegacyHeight * 4;
    public const int ByteCount = Width * Height * 4;

    public static (int Column, int Row) FrameTexel(PaletteIndex index, int frameRow)
    {
        if (frameRow is < 0 or >= FrameRowCount) throw new ArgumentOutOfRangeException(nameof(frameRow));
        return (index.Value & 255, frameRow + (index.Value >> 8) * LegacyHeight);
    }

    public static (int Column, int Row) SeasonalTexel(int sample, int row)
    {
        if (sample is < 0 or >= SeasonSampleCount) throw new ArgumentOutOfRangeException(nameof(sample));
        if (row is < FirstSeasonalRow or >= LegacyHeight) throw new ArgumentOutOfRangeException(nameof(row));
        return (sample, row);
    }
}

/// <summary>Pure byte/numeric encoding. These helpers do not upload, check native formats or make a renderer ready.</summary>
public static class PaletteEncoding
{
    public const byte SemanticFlagsMask = 15;
    public const byte ColorBankFlag = 16;
    public const byte DecalBaseBankFlag = 32;
    public const byte PackedFlagsMask = 63;
    private const int LampBankUnit = 32768;
    private const int LampEnergyUnit = 256;
    private const int LampMaxEnergyPercent = 100;

    /// <summary>
    /// R=colour low byte, G=unchanged AO byte, B=semantic flags plus the two bank bits, A=unchanged reserved byte.
    /// Only a decal may supply decalBase; when supplied, reserved must be zero and A is the base index's low byte.
    /// Glass/window types, gradients and other non-decal A meanings are never repurposed.
    /// </summary>
    public static (byte R, byte G, byte B, byte A) EncodeColor(
        PaletteIndex color, byte ao, byte flags, byte reserved = 0, PaletteIndex? decalBase = null)
    {
        if ((flags & ~SemanticFlagsMask) != 0)
            throw new ArgumentException("Input flags may only use the existing low four semantic bits.", nameof(flags));
        if (decalBase.HasValue && ((flags & VertexFlag.Decal) == 0 || reserved != 0))
            throw new ArgumentException("A full decal base index requires a decal and no competing reserved byte.", nameof(decalBase));
        var packedFlags = flags | ((color.Value >> 8) * ColorBankFlag);
        if (decalBase is { } background) packedFlags |= (background.Value >> 8) * DecalBaseBankFlag;
        return ((byte)(color.Value & 255), ao, (byte)packedFlags,
            decalBase is { } value ? (byte)(value.Value & 255) : reserved);
    }

    public static PaletteIndex DecodeColor(byte lowByte, byte packedFlags)
    {
        ValidatePackedFlags(packedFlags);
        return new(lowByte + ((packedFlags & ColorBankFlag) != 0 ? PaletteLayout.LegacyCapacity : 0));
    }

    public static PaletteIndex DecodeDecalBase(byte reserved, byte packedFlags)
    {
        ValidatePackedFlags(packedFlags);
        if ((packedFlags & VertexFlag.Decal) == 0) throw new ArgumentException("Reserved is not a colour index on a non-decal.", nameof(packedFlags));
        return new(reserved + ((packedFlags & DecalBaseBankFlag) != 0 ? PaletteLayout.LegacyCapacity : 0));
    }

    /// <summary>Godot float upload input with the existing UNORM8 truncation bias. Not a substitute for native byte readback.</summary>
    public static float Unorm8Input(byte value) => (value + 0.5f) / 255f;

    /// <summary>Rejects unassigned packed bits and a decal-base bank on non-decal geometry.</summary>
    public static void ValidatePackedFlags(byte packedFlags)
    {
        if ((packedFlags & ~PackedFlagsMask) != 0 ||
            ((packedFlags & DecalBaseBankFlag) != 0 && (packedFlags & VertexFlag.Decal) == 0))
            throw new ArgumentException("Invalid packed palette flag bits.", nameof(packedFlags));
    }

    /// <summary>Existing low byte + 256 * energy percentage, plus bank in bit 15. Every finite legacy input stays bit-identical.</summary>
    public static float PackLampColor(PaletteIndex index, float energy)
    {
        if (!float.IsFinite(energy)) throw new ArgumentOutOfRangeException(nameof(energy));
        return (index.Value & 255) + LampEnergyUnit * MathF.Round(Math.Clamp(energy, 0f, 1f) * LampMaxEnergyPercent)
            + LampBankUnit * (index.Value >> 8);
    }

    public static (PaletteIndex Index, float Energy) UnpackLampColor(float packed)
    {
        const int maximum = 255 + LampEnergyUnit * LampMaxEnergyPercent + LampBankUnit;
        if (!float.IsFinite(packed) || packed < 0f || packed > maximum || packed != MathF.Truncate(packed))
            throw new ArgumentOutOfRangeException(nameof(packed));
        var value = (int)packed;
        var percent = (value % LampBankUnit) / LampEnergyUnit;
        if (percent > LampMaxEnergyPercent) throw new ArgumentOutOfRangeException(nameof(packed), "Unused energy bits are not valid lamp data.");
        return (new((value & 255) + PaletteLayout.LegacyCapacity * (value / LampBankUnit)), percent / 100f);
    }
}
