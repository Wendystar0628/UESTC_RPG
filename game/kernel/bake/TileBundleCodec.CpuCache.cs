using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

public static partial class TileBundleCodec
{
    internal static byte[] EncodeCache<T>(BakeKey key, T product) where T : class
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true);
        writer.Write(0x55504342); writer.Write(1); Key(writer, key);
        switch (product)
        {
            case TerrainFrozenData terrain: writer.Write((byte)1); Terrain(writer, terrain, key); break;
            case BakeResidentData resident: writer.Write((byte)2); Resident(writer, resident, key); break;
            case BakeSourceIndexData sources: writer.Write((byte)3); Sources(writer, sources, key); break;
            default: throw new NotSupportedException($"Unsupported CPU cache: {typeof(T).Name}");
        }
        if (stream.Length > BakeCacheStore.MaximumProductBytes) throw new InvalidDataException("CPU cache exceeds its product limit.");
        return stream.ToArray();
    }

    internal static T DecodeCache<T>(byte[] bytes, BakeKey key) where T : class
    {
        if (bytes.Length > BakeCacheStore.MaximumProductBytes) throw new InvalidDataException("CPU cache exceeds its product limit.");
        using var reader = new BinaryReader(new MemoryStream(bytes, false), new UTF8Encoding(false, true));
        try
        {
            if (reader.ReadInt32() != 0x55504342 || reader.ReadInt32() != 1 || Key(reader) != key)
                throw new InvalidDataException("CPU cache header/key mismatch.");
            object product = reader.ReadByte() switch
            {
                1 when typeof(T) == typeof(TerrainFrozenData) => Terrain(reader, key),
                2 when typeof(T) == typeof(BakeResidentData) => Resident(reader, key),
                3 when typeof(T) == typeof(BakeSourceIndexData) => Sources(reader, key),
                _ => throw new InvalidDataException("CPU cache kind mismatch.")
            };
            if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing CPU cache bytes.");
            return (T)product;
        }
        catch (Exception error) when (error is EndOfStreamException or JsonException or ArgumentException or OverflowException)
        { throw new InvalidDataException("Invalid CPU cache payload.", error); }
    }

    private static void SourceCells(BinaryWriter writer, IReadOnlyList<BakeSourceCell> cells, BakeKey key)
    {
        Channel<TileKey>(writer, cells.Select(cell => cell.Cell).ToArray(), key);
        foreach (var cell in cells) Text(writer, cell.Stamp);
    }

    private static BakeSourceCell[] SourceCells(BinaryReader reader, BakeKey key)
    {
        var cells = Channel<TileKey>(reader, key); var result = new BakeSourceCell[cells.Length];
        for (var i = 0; i < cells.Length; i++) result[i] = new(cells[i], Text(reader, 128));
        return result;
    }

    private static void Sources(BinaryWriter writer, BakeSourceIndexData data, BakeKey key)
    {
        Text(writer, data.SourceVersion); Text(writer, data.ResidentStamp); Text(writer, data.CommonStamp); Text(writer, data.PaletteStamp);
        SourceCells(writer, data.Cells, key); SourceCells(writer, data.Windows, key);
        SourceCells(writer, data.Vegetation, key); SourceCells(writer, data.Furniture, key);
        Records(writer, data.Products, (w, product) => { Key(w, product.Key); Text(w, product.Stamp); });
    }

    private static BakeSourceIndexData Sources(BinaryReader reader, BakeKey key) => new()
    {
        SourceVersion = Text(reader), ResidentStamp = Text(reader, 128), CommonStamp = Text(reader, 128), PaletteStamp = Text(reader, 128),
        Cells = SourceCells(reader, key), Windows = SourceCells(reader, key), Vegetation = SourceCells(reader, key), Furniture = SourceCells(reader, key),
        Products = Records(reader, key, r => new BakeProductInput(Key(r), Text(r, 128)))
    };
}
