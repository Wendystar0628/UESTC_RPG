using System.Buffers.Binary;
using System.Globalization;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Small logical cache index and deterministic physical filenames, shared by every bake product.</summary>
internal static class BakeCacheIndex
{
    private const uint Magic = 0x31504254; // TBP1
    private const int Length = 9;

    internal static byte[] Encode(BakePayloadClass payload, int count)
    {
        if (count < 1) throw new InvalidDataException("A cache index needs delivery parts.");
        var bytes = new byte[Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, Magic);
        bytes[4] = (byte)payload;
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(5), count);
        return bytes;
    }

    internal static int Read(string path, BakePayloadClass payload)
    {
        var bytes = BakeCacheStore.ReadBounded(path, Length);
        if (bytes.Length != Length || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic || bytes[4] != (byte)payload)
            throw new InvalidDataException("Invalid bake delivery index.");
        var count = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(5));
        if (count <= 0) throw new InvalidDataException("Invalid bake delivery part count.");
        return count;
    }

    private static string DirectoryPath(string indexPath) => indexPath[..^".tbin".Length] + ".parts";
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
