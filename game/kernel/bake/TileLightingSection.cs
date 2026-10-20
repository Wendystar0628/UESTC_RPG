using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Lighting inventories, window metadata and stable window numbering records.</summary>
internal static class TileLightingSection
{
    private static void Facade(BinaryWriter writer, WindowFacadeDecl value, BakeKey key)
    {
        TileBinaryFields.Json(writer, value with { SillY = [], Skip = [] });
        TileBinaryFields.Channel<float>(writer, value.SillY.ToArray(), key); TileBinaryFields.Channel<int>(writer, value.Skip.ToArray(), key);
    }

    private static WindowFacadeDecl Facade(BinaryReader reader, BakeKey key) => TileBinaryFields.Json<WindowFacadeDecl>(reader) with
    { SillY = TileBinaryFields.Channel<float>(reader, key), Skip = TileBinaryFields.Channel<int>(reader, key) };

    internal static void Windows(BinaryWriter writer, WindowMetadata windows, BakeKey key)
    {
        TileBinaryFields.Records(writer, windows.Facades, (w, value) => Facade(w, value, key));
        TileBinaryFields.MetadataRecords(writer, windows.Windows);
    }

    internal static WindowMetadata Windows(BinaryReader reader, BakeKey key) => new(
        TileBinaryFields.Records(reader, key, r => Facade(r, key)), TileBinaryFields.MetadataRecords<WindowDecl>(reader, key));

    internal static void Lighting(BinaryWriter writer, LightInventoryDocument value, BakeKey key)
    {
        TileBinaryFields.Text(writer, value.Map.Value); TileBinaryFields.MetadataRecords(writer, value.Lamps);
        Windows(writer, new(value.Facades, value.Windows), key);
        TileBinaryFields.Channel<LampRecord>(writer, value.Nightlight.Lamps.ToArray(), key);
        TileBinaryFields.Channel(writer, value.Nightlight.LampTable.Span, key); TileBinaryFields.Channel(writer, value.Nightlight.LampGrid.Span, key);
        writer.Write(value.Nightlight.GridWidth); writer.Write(value.Nightlight.GridHeight);
        TileBinaryFields.Channel(writer, value.Nightlight.WindowThresholds.Span, key);
        writer.Write(value.WindowNumbering is not null);
        if (value.WindowNumbering is { } numbering)
        {
            TileBinaryFields.Text(writer, numbering.Map.Value); TileBinaryFields.Text(writer, numbering.Fingerprint); writer.Write(numbering.NextId);
            TileBinaryFields.Records(writer, numbering.Allocations, (w, allocation) =>
            {
                TileBinaryFields.Text(w, allocation.Feature.Value); TileBinaryFields.Text(w, allocation.Key);
                w.Write(allocation.BaseId); w.Write(allocation.Columns); w.Write(allocation.Rows);
            });
        }
    }

    internal static LightInventoryDocument Lighting(BinaryReader reader, BakeKey key)
    {
        var map = new MapId(TileBinaryFields.Text(reader)); var lamps = TileBinaryFields.MetadataRecords<LightEmitter>(reader, key);
        var windows = Windows(reader, key);
        var records = TileBinaryFields.Channel<LampRecord>(reader, key); var table = TileBinaryFields.Channel<float>(reader, key); var grid = TileBinaryFields.Channel<float>(reader, key);
        var catalog = new NightlightCatalog(records, table, grid, reader.ReadInt32(), reader.ReadInt32())
        { WindowThresholds = TileBinaryFields.Channel<float>(reader, key) };
        WindowNumberingSnapshot? numbering = null;
        if (reader.ReadBoolean())
        {
            var numberingMap = new MapId(TileBinaryFields.Text(reader)); var fingerprint = TileBinaryFields.Text(reader, 128); var nextId = reader.ReadInt32();
            numbering = new(numberingMap, fingerprint, TileBinaryFields.Records(reader, key,
                r => new WindowAllocation(new(TileBinaryFields.Text(r)), TileBinaryFields.Text(r), r.ReadInt32(), r.ReadInt32(), r.ReadInt32())), nextId);
        }
        return new(map, lamps, windows.Facades, windows.Windows, catalog) { WindowNumbering = numbering };
    }

}
