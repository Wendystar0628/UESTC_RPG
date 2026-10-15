using System.Text;

namespace Campus.Contracts;

/// <summary>Versioned, process-independent 32-bit hashes for procedural seeds.</summary>
/// <remarks>
/// FNV-1a-32 over this byte protocol: ASCII CSH1, seed as little-endian uint32,
/// part count as little-endian uint32, then each part's UTF-8 byte count as
/// little-endian uint32 followed by its UTF-8 bytes. UTF-8 is strict, without BOM
/// or Unicode normalization. Null parts and unpaired UTF-16 surrogates are rejected.
/// Length framing distinguishes ["ab", "c"] from ["a", "bc"] before hashing;
/// a 32-bit result still has ordinary hash collisions and is not a unique ID.
/// Changing this protocol changes generated content and requires a bake ABI change.
/// </remarks>
public static class StableHash
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static uint Hash32(uint seed, params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        uint hash = 2166136261u;
        AppendUInt32(ref hash, 0x31485343u); // CSH1, explicitly little endian.
        AppendUInt32(ref hash, seed);
        AppendUInt32(ref hash, (uint)parts.Length);
        foreach (string part in parts)
        {
            ArgumentNullException.ThrowIfNull(part);
            byte[] bytes = Utf8.GetBytes(part);
            AppendUInt32(ref hash, (uint)bytes.Length);
            foreach (byte value in bytes)
            {
                AppendByte(ref hash, value);
            }
        }

        return hash;
    }

    private static void AppendUInt32(ref uint hash, uint value)
    {
        unchecked
        {
            AppendByte(ref hash, (byte)value);
            AppendByte(ref hash, (byte)(value >> 8));
            AppendByte(ref hash, (byte)(value >> 16));
            AppendByte(ref hash, (byte)(value >> 24));
        }
    }

    private static void AppendByte(ref uint hash, byte value)
    {
        hash = unchecked((hash ^ value) * 16777619u);
    }
}
