using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Resident metadata, static ground masks and interior layout records.</summary>
internal static class TileResidentSection
{
    internal static void Resident(BinaryWriter writer, BakeResidentData data, BakeKey key)
    {
        TileLightingSection.Lighting(writer, data.Metadata.Lighting, key);
        TileBinaryFields.Text(writer, data.Metadata.Entrances.Map.Value);
        TileBinaryFields.MetadataRecords(writer, data.Metadata.Entrances.Entrances);
        TileLightingSection.Windows(writer, data.Windows, key);
        TileBinaryFields.MetadataRecords(writer, data.Coverage);
        TileBinaryFields.Records(writer, data.Layouts, (w, layout) => Layout(w, layout, key));
        var modules = data.Directory.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
        TileBinaryFields.Records(writer, modules, (w, module) => { TileBinaryFields.Text(w, module.Key); TileBinaryFields.Channel<TileKey>(w, module.Value.ToArray(), key); });
        TileBinaryFields.MetadataRecords(writer, data.Directory.Auxiliary);
        TileBinaryFields.Channel<TileKey>(writer, data.TerrainPoints, key);
        TileBinaryFields.Records(writer, data.OutdoorAccess, (w, source) =>
        {
            TileBinaryFields.Text(w, source.Id); TileBinaryFields.Text(w, source.Feature.Value);
            TileBinaryFields.Records(w, source.Areas, (areaWriter, area) =>
            {
                TileBinaryFields.Json(areaWriter, area with { Footprint = [] }); TileBinaryFields.Channel<Vector2>(areaWriter, area.Footprint.ToArray(), key);
            });
        });
        TileBinaryFields.Json(writer, data.Metadata.GroundCover.Grid);
        TileBinaryFields.Channel<byte>(writer, data.Metadata.GroundCover.Mask1, key);
        TileBinaryFields.Channel<byte>(writer, data.Metadata.GroundCover.Mask2, key);
    }

    internal static BakeResidentData Resident(BinaryReader reader, BakeKey key)
    {
        var lighting = TileLightingSection.Lighting(reader, key);
        var entrances = new EntranceInventoryDocument(new(TileBinaryFields.Text(reader)), TileBinaryFields.MetadataRecords<BakedEntrance>(reader, key));
        var windows = TileLightingSection.Windows(reader, key); var coverage = TileBinaryFields.MetadataRecords<BakeService.InteriorCoverage>(reader, key);
        var layouts = TileBinaryFields.Records(reader, key, r => Layout(r, key));
        var modules = new Dictionary<string, IReadOnlyList<TileKey>>(StringComparer.Ordinal);
        foreach (var module in TileBinaryFields.Records(reader, key, r => new KeyValuePair<string, IReadOnlyList<TileKey>>(TileBinaryFields.Text(r), TileBinaryFields.Channel<TileKey>(r, key))))
            if (!modules.TryAdd(module.Key, module.Value)) throw new InvalidDataException("Duplicate resident module.");
        var directory = new PreparedBakeTiles(modules, TileBinaryFields.MetadataRecords<BakeTileEntry>(reader, key));
        var terrainPoints = TileBinaryFields.Channel<TileKey>(reader, key);
        var outdoor = TileBinaryFields.Records(reader, key, sourceReader => new OutdoorAccessArea(TileBinaryFields.Text(sourceReader), new(TileBinaryFields.Text(sourceReader)),
            TileBinaryFields.Records(sourceReader, key, areaReader => TileBinaryFields.Json<StoreyInfo>(areaReader) with { Footprint = TileBinaryFields.Channel<Vector2>(areaReader, key) })));
        var grid = TileBinaryFields.Json<GroundMaskGrid>(reader);
        var mask1 = TileBinaryFields.Channel<byte>(reader, key);
        var mask2 = TileBinaryFields.Channel<byte>(reader, key);
        grid.Validate(mask1.Length, mask2.Length);
        return new(new(lighting, entrances, new(grid, mask1, mask2)), windows, coverage, layouts, directory, terrainPoints) { OutdoorAccess = outdoor };
    }

    private static void Layout(BinaryWriter writer, InteriorLayout value, BakeKey key)
    {
        TileBinaryFields.Json(writer, value with
        {
            Storeys = [], OpenAreas = [], Zones = [], Portals = [], Entrances = [], Elevators = [], OpenStoreys = [], AtriumVisibleStoreys = [], Doors = []
        });
        TileBinaryFields.Records(writer, value.Storeys, (w, storey) =>
        {
            TileBinaryFields.Json(w, storey with { Footprint = [] }); TileBinaryFields.Channel<Vector2>(w, storey.Footprint.ToArray(), key);
        });
        TileBinaryFields.Records(writer, value.OpenAreas, (w, area) =>
        {
            TileBinaryFields.Json(w, area with { Footprint = [] }); TileBinaryFields.Channel<Vector2>(w, area.Footprint.ToArray(), key);
        });
        TileBinaryFields.Records(writer, value.Zones, (w, zone) => { TileBinaryFields.Json(w, zone with { Ring = [] }); TileBinaryFields.Channel<Vector2>(w, zone.Ring.ToArray(), key); });
        TileBinaryFields.MetadataRecords(writer, value.Portals); TileBinaryFields.MetadataRecords(writer, value.Entrances); TileBinaryFields.MetadataRecords(writer, value.Elevators);
        TileBinaryFields.Channel<int>(writer, value.OpenStoreys.ToArray(), key); TileBinaryFields.Channel<int>(writer, value.AtriumVisibleStoreys.ToArray(), key);
        TileBinaryFields.MetadataRecords(writer, value.Doors);
    }

    private static InteriorLayout Layout(BinaryReader reader, BakeKey key)
    {
        var value = TileBinaryFields.Json<InteriorLayout>(reader);
        return value with
        {
            Storeys = TileBinaryFields.Records(reader, key, r => TileBinaryFields.Json<StoreyInfo>(r) with { Footprint = TileBinaryFields.Channel<Vector2>(r, key) }),
            OpenAreas = TileBinaryFields.Records(reader, key, r => TileBinaryFields.Json<InteriorOpenArea>(r) with { Footprint = TileBinaryFields.Channel<Vector2>(r, key) }),
            Zones = TileBinaryFields.Records(reader, key, r => TileBinaryFields.Json<ZoneVolume>(r) with { Ring = TileBinaryFields.Channel<Vector2>(r, key) }),
            Portals = TileBinaryFields.MetadataRecords<PortalDecl>(reader, key), Entrances = TileBinaryFields.MetadataRecords<EntranceDecl>(reader, key),
            Elevators = TileBinaryFields.MetadataRecords<ElevatorDecl>(reader, key), OpenStoreys = TileBinaryFields.Channel<int>(reader, key),
            AtriumVisibleStoreys = TileBinaryFields.Channel<int>(reader, key), Doors = TileBinaryFields.MetadataRecords<DoorLeafDecl>(reader, key)
        };
    }

}
