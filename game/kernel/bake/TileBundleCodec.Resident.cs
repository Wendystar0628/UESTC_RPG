using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

public static partial class TileBundleCodec
{
    private static void Resident(BinaryWriter writer, BakeResidentData data, BakeKey key)
    {
        Lighting(writer, data.Metadata.Lighting, key);
        Text(writer, data.Metadata.Entrances.Map.Value);
        MetadataRecords(writer, data.Metadata.Entrances.Entrances);
        Windows(writer, data.Windows, key);
        MetadataRecords(writer, data.Coverage);
        Records(writer, data.Layouts, (w, layout) => Layout(w, layout, key));
        var modules = data.Directory.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
        Records(writer, modules, (w, module) => { Text(w, module.Key); Channel<TileKey>(w, module.Value.ToArray(), key); });
        MetadataRecords(writer, data.Directory.Auxiliary);
        Channel<TileKey>(writer, data.TerrainPoints, key);
    }

    private static BakeResidentData Resident(BinaryReader reader, BakeKey key)
    {
        var lighting = Lighting(reader, key);
        var entrances = new EntranceInventoryDocument(new(Text(reader)), MetadataRecords<BakedEntrance>(reader, key));
        var windows = Windows(reader, key); var coverage = MetadataRecords<BakeService.InteriorCoverage>(reader, key);
        var layouts = Records(reader, key, r => Layout(r, key));
        var modules = new Dictionary<string, IReadOnlyList<TileKey>>(StringComparer.Ordinal);
        foreach (var module in Records(reader, key, r => new KeyValuePair<string, IReadOnlyList<TileKey>>(Text(r), Channel<TileKey>(r, key))))
            if (!modules.TryAdd(module.Key, module.Value)) throw new InvalidDataException("Duplicate resident module.");
        var directory = new PreparedBakeTiles(modules, MetadataRecords<BakeTileEntry>(reader, key));
        return new(new(lighting, entrances), windows, coverage, layouts, directory, Channel<TileKey>(reader, key));
    }

    private static void Lighting(BinaryWriter writer, LightInventoryDocument value, BakeKey key)
    {
        Text(writer, value.Map.Value); MetadataRecords(writer, value.Lamps);
        Windows(writer, new(value.Facades, value.Windows), key);
        Channel<LampRecord>(writer, value.Nightlight.Lamps.ToArray(), key);
        Channel(writer, value.Nightlight.LampTable.Span, key); Channel(writer, value.Nightlight.LampGrid.Span, key);
        writer.Write(value.Nightlight.GridWidth); writer.Write(value.Nightlight.GridHeight);
        writer.Write(value.WindowNumbering is not null);
        if (value.WindowNumbering is { } numbering)
        {
            Text(writer, numbering.Map.Value); Text(writer, numbering.Fingerprint);
            Records(writer, numbering.Allocations, (w, allocation) =>
            {
                Text(w, allocation.Feature.Value); Text(w, allocation.Key);
                w.Write(allocation.BaseId); w.Write(allocation.Columns); w.Write(allocation.Rows);
            });
        }
    }

    private static LightInventoryDocument Lighting(BinaryReader reader, BakeKey key)
    {
        var map = new MapId(Text(reader)); var lamps = MetadataRecords<LightEmitter>(reader, key);
        var windows = Windows(reader, key);
        var records = Channel<LampRecord>(reader, key); var table = Channel<float>(reader, key); var grid = Channel<float>(reader, key);
        var catalog = new NightlightCatalog(records, table, grid, reader.ReadInt32(), reader.ReadInt32());
        WindowNumberingSnapshot? numbering = null;
        if (reader.ReadBoolean())
        {
            var numberingMap = new MapId(Text(reader)); var fingerprint = Text(reader, 128);
            numbering = new(numberingMap, fingerprint, Records(reader, key,
                r => new WindowAllocation(new(Text(r)), Text(r), r.ReadInt32(), r.ReadInt32(), r.ReadInt32())));
        }
        return new(map, lamps, windows.Facades, windows.Windows, catalog) { WindowNumbering = numbering };
    }

    private static void Layout(BinaryWriter writer, InteriorLayout value, BakeKey key)
    {
        Json(writer, value with
        {
            Storeys = [], Zones = [], Portals = [], Entrances = [], Elevators = [], OpenStoreys = [], AtriumVisibleStoreys = [], Doors = []
        });
        Records(writer, value.Storeys, (w, storey) =>
        {
            Json(w, storey with { Footprint = [] }); Channel<Vector2>(w, storey.Footprint.ToArray(), key);
        });
        Records(writer, value.Zones, (w, zone) => { Json(w, zone with { Ring = [] }); Channel<Vector2>(w, zone.Ring.ToArray(), key); });
        MetadataRecords(writer, value.Portals); MetadataRecords(writer, value.Entrances); MetadataRecords(writer, value.Elevators);
        Channel<int>(writer, value.OpenStoreys.ToArray(), key); Channel<int>(writer, value.AtriumVisibleStoreys.ToArray(), key);
        MetadataRecords(writer, value.Doors);
    }

    private static InteriorLayout Layout(BinaryReader reader, BakeKey key)
    {
        var value = Json<InteriorLayout>(reader);
        return value with
        {
            Storeys = Records(reader, key, r => Json<StoreyInfo>(r) with { Footprint = Channel<Vector2>(r, key) }),
            Zones = Records(reader, key, r => Json<ZoneVolume>(r) with { Ring = Channel<Vector2>(r, key) }),
            Portals = MetadataRecords<PortalDecl>(reader, key), Entrances = MetadataRecords<EntranceDecl>(reader, key),
            Elevators = MetadataRecords<ElevatorDecl>(reader, key), OpenStoreys = Channel<int>(reader, key),
            AtriumVisibleStoreys = Channel<int>(reader, key), Doors = MetadataRecords<DoorLeafDecl>(reader, key)
        };
    }
}
