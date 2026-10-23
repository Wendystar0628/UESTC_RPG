using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Small logical cache index and deterministic physical filenames, shared by every bake product.</summary>
internal static class BakeCacheIndex
{
    private const uint Magic = 0x32504254; // TBP2
    private const int HeaderLength = 13;
    private const int MaximumIdentityBytes = 2048;
    private static readonly Encoding IdentityEncoding = new UTF8Encoding(false, true);

    internal static byte[] Encode(BakePayloadClass payload, int count, string identity)
    {
        if (count < 1) throw new InvalidDataException("A cache index needs delivery parts.");
        _ = BakeCacheStore.MaximumPayload(payload);
        if (string.IsNullOrWhiteSpace(identity)) throw new InvalidDataException("A cache index needs an input identity.");
        var encodedIdentity = IdentityEncoding.GetBytes(identity);
        if (encodedIdentity.Length > MaximumIdentityBytes) throw new InvalidDataException("Cache input identity exceeds its limit.");
        var bytes = new byte[HeaderLength + encodedIdentity.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, Magic);
        bytes[4] = (byte)payload;
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(5), count);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(9), encodedIdentity.Length);
        encodedIdentity.CopyTo(bytes, HeaderLength);
        return bytes;
    }

    internal static int Read(string path, BakePayloadClass payload, string identity)
    {
        _ = BakeCacheStore.MaximumPayload(payload);
        var bytes = BakeCacheStore.ReadBounded(path, HeaderLength + MaximumIdentityBytes);
        if (bytes.Length < HeaderLength || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic || bytes[4] != (byte)payload)
            throw new InvalidDataException("Invalid bake delivery index.");
        var count = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(5));
        var length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(9));
        if (count <= 0) throw new InvalidDataException("Invalid bake delivery part count.");
        if (length <= 0 || length > MaximumIdentityBytes || length != bytes.Length - HeaderLength)
            throw new InvalidDataException("Invalid bake input identity length.");
        var published = IdentityEncoding.GetString(bytes, HeaderLength, length);
        if (string.IsNullOrWhiteSpace(published) || !string.Equals(published, identity, StringComparison.Ordinal))
            throw new InvalidDataException("Cached product identity mismatch.");
        return count;
    }

    internal static string DirectoryPath(string indexPath) => indexPath[..^".tbin".Length] + ".parts";
    internal static string PartPath(string indexPath, int index) => Path.Combine(DirectoryPath(indexPath),
        index.ToString("D10", CultureInfo.InvariantCulture) + ".tbin");

    internal static IEnumerable<string> Files(string indexPath)
    {
        var directory = DirectoryPath(indexPath);
        if (!Directory.Exists(directory)) return [];
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Cache part directories must not be reparse points.");
        return Directory.EnumerateFiles(directory, "*.tbin").Order(StringComparer.Ordinal).ToArray();
    }
}
