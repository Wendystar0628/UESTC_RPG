using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Fixed terrain payloads inside the existing compressed local product store.</summary>

internal static class TerrainCacheCodec
{
    public static byte[] EncodeBlock(TerrainRasterBlock block)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(block.Block.X); writer.Write(block.Block.Z);
        for (var i = 0; i < block.Heights.Length; i++) Write(writer, block.Heights[i], block.Facts[i]);
        return stream.ToArray();
    }
    public static TerrainRasterBlock DecodeBlock(byte[] bytes)
    {
        if (bytes.Length != 8 + TerrainRasterBlock.Count * 15) throw new InvalidDataException("Invalid terrain block payload length.");
        using var reader = new BinaryReader(new MemoryStream(bytes));
        var block = new TileKey(reader.ReadInt32(), reader.ReadInt32());
        var heights = new short[TerrainRasterBlock.Count]; var facts = new TerrainFacts[TerrainRasterBlock.Count];
        for (var i = 0; i < heights.Length; i++) (heights[i], facts[i]) = Read(reader);
        return new(block, heights, facts);
    }
    public static byte[] EncodePoints(IReadOnlyList<TerrainRasterPoint> points)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(points.Count);
        foreach (var point in points) { writer.Write(point.X); writer.Write(point.Z); Write(writer, point.Height, point.Facts); }
        return stream.ToArray();
    }
    public static TerrainRasterPoint[] DecodePoints(byte[] bytes)
    {
        using var reader = new BinaryReader(new MemoryStream(bytes));
        var count = reader.ReadInt32();
        if (count < 0 || 4L + count * 23L != bytes.Length) throw new InvalidDataException("Invalid terrain point payload length.");
        var points = new TerrainRasterPoint[count];
        for (var i = 0; i < count; i++)
        {
            var x = reader.ReadInt32(); var z = reader.ReadInt32(); var value = Read(reader);
            points[i] = new(x, z, value.Height, value.Facts);
        }
        return points;
    }
    private static void Write(BinaryWriter writer, short height, TerrainFacts facts)
    {
        writer.Write(height); writer.Write(facts.Grade); writer.Write((byte)facts.Class); writer.Write(facts.CrestDist); writer.Write(facts.ToeDist);
    }
    private static (short Height, TerrainFacts Facts) Read(BinaryReader reader) =>
        (reader.ReadInt16(), new(reader.ReadSingle(), (TerrainClass)reader.ReadByte(), reader.ReadSingle(), reader.ReadSingle()));
}
