using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Source-index stamps, product inputs and source-cell channels.</summary>
internal static class TileSourceIndexSection
{
    internal static void SourceCells(BinaryWriter writer, IReadOnlyList<BakeSourceCell> cells, BakeKey key)
    {
        TileBinaryFields.Channel<TileKey>(writer, cells.Select(cell => cell.Cell).ToArray(), key);
        foreach (var cell in cells) TileBinaryFields.Text(writer, cell.Stamp);
    }

    internal static BakeSourceCell[] SourceCells(BinaryReader reader, BakeKey key)
    {
        var cells = TileBinaryFields.Channel<TileKey>(reader, key); var result = new BakeSourceCell[cells.Length];
        for (var i = 0; i < cells.Length; i++) result[i] = new(cells[i], TileBinaryFields.Text(reader, 128));
        return result;
    }

    internal static void Sources(BinaryWriter writer, BakeSourceIndexData data, BakeKey key)
    {
        TileBinaryFields.Text(writer, data.SourceVersion); TileBinaryFields.Text(writer, data.ResidentStamp); TileBinaryFields.Text(writer, data.CommonStamp); TileBinaryFields.Text(writer, data.PaletteStamp);
        SourceCells(writer, data.Cells, key); SourceCells(writer, data.Windows, key);
        SourceCells(writer, data.Vegetation, key); SourceCells(writer, data.Furniture, key);
        TileBinaryFields.Records(writer, data.Products, (w, product) => { TileBinaryFields.Key(w, product.Key); TileBinaryFields.Text(w, product.Stamp); });
    }

    internal static BakeSourceIndexData Sources(BinaryReader reader, BakeKey key) => new()
    {
        SourceVersion = TileBinaryFields.Text(reader), ResidentStamp = TileBinaryFields.Text(reader, 128), CommonStamp = TileBinaryFields.Text(reader, 128), PaletteStamp = TileBinaryFields.Text(reader, 128),
        Cells = SourceCells(reader, key), Windows = SourceCells(reader, key), Vegetation = SourceCells(reader, key), Furniture = SourceCells(reader, key),
        Products = TileBinaryFields.Records(reader, key, r => new BakeProductInput(TileBinaryFields.Key(r), TileBinaryFields.Text(r, 128)))
    };

}
