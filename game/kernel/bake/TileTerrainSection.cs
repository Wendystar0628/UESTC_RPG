using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Bake;

/// <summary>Frozen terrain records, water polygons, bank profiles and support corners.</summary>
internal static class TileTerrainSection
{
    internal static void Terrain(BinaryWriter writer, TerrainFrozenData data, BakeKey key)
    {
        TileBinaryFields.Json(writer, data.DataRect); TileBinaryFields.Text(writer, data.SourceInputs.CommonStamp);
        TileSourceIndexSection.SourceCells(writer, data.SourceInputs.Cells.Select(cell => new BakeSourceCell(cell.Cell, cell.Stamp)).ToArray(), key);
        Levels(writer, data.Pads); Levels(writer, data.WaterLevels);
        TileBinaryFields.Records(writer, data.Waters, (w, water) =>
        {
            TileBinaryFields.Text(w, water.Water.Value);
            TileBinaryFields.Records(w, water.Polygons, (r, polygon) => TileBinaryFields.Rings(r, polygon, key));
            TileBinaryFields.Records(w, water.WadeAreas, (r, polygon) => TileBinaryFields.Rings(r, polygon, key));
        });
        TileBinaryFields.Records(writer, data.Banks, (w, bank) =>
        {
            var span = bank.Span;
            TileBinaryFields.Text(w, span.Id); Ids(w, span.Waters); Ids(w, span.Roads); Ids(w, span.Bridges);
            TileBinaryFields.Channel<Vector2>(w, span.Shore.Points.ToArray(), key);
            w.Write(span.CoreFrom); w.Write(span.CoreTo); w.Write(span.TransitionLength); w.Write((byte)span.Kind);
            TileBinaryFields.Channel<double>(w, bank.Stations, key); TileBinaryFields.Channel<short>(w, bank.Heights, key);
        });
        TileBinaryFields.Channel<Vector2>(writer, data.Supports.SelectMany(rect => new[] { rect.Min, rect.Max }).ToArray(), key);
        TileBinaryFields.Channel<short>(writer, data.DistantHeights, key);
        TileBinaryFields.Records(writer, data.Diagnostics, TileBinaryFields.Text);
    }

    internal static TerrainFrozenData Terrain(BinaryReader reader, BakeKey key)
    {
        var rect = TileBinaryFields.Json<MapRect>(reader); var common = TileBinaryFields.Text(reader);
        var sources = TileSourceIndexSection.SourceCells(reader, key).Select(cell => new TerrainInputCell(cell.Cell, cell.Stamp)).ToArray();
        var pads = Levels(reader, key); var levels = Levels(reader, key);
        var waters = TileBinaryFields.Records(reader, key, r => new TerrainWaterBody(new(TileBinaryFields.Text(r)),
            TileBinaryFields.Records(r, key, w => TileBinaryFields.Rings(w, key)), TileBinaryFields.Records(r, key, w => TileBinaryFields.Rings(w, key))));
        var banks = TileBinaryFields.Records(reader, key, r =>
        {
            var id = TileBinaryFields.Text(r); var water = Ids(r, key); var roads = Ids(r, key); var bridges = Ids(r, key);
            var shore = TileBinaryFields.Channel<Vector2>(r, key);
            var span = new TerrainBankSpan(id, water, roads, bridges, new(shore), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), (TerrainBankKind)r.ReadByte());
            return new TerrainBankProfile(span, TileBinaryFields.Channel<double>(r, key), TileBinaryFields.Channel<short>(r, key));
        });
        var corners = TileBinaryFields.Channel<Vector2>(reader, key);
        if (corners.Length % 2 != 0) throw new InvalidDataException("Terrain supports require pairs of corners.");
        var supports = new MapRect[corners.Length / 2];
        for (var i = 0; i < supports.Length; i++) supports[i] = new(corners[i * 2], corners[i * 2 + 1]);
        return new(rect, new(common, sources), pads, levels, waters, banks, supports,
            TileBinaryFields.Channel<short>(reader, key), TileBinaryFields.Records(reader, key, r => TileBinaryFields.Text(r)));
    }

    private static void Levels(BinaryWriter writer, IReadOnlyList<TerrainFeatureLevel> values) => TileBinaryFields.Records(writer, values,
        (w, value) => { TileBinaryFields.Text(w, value.Feature.Value); w.Write(value.Y); });

    private static TerrainFeatureLevel[] Levels(BinaryReader reader, BakeKey key) => TileBinaryFields.Records(reader, key,
        r => new TerrainFeatureLevel(new(TileBinaryFields.Text(r)), r.ReadSingle()));

    private static void Ids(BinaryWriter writer, IReadOnlyList<FeatureId> values) => TileBinaryFields.Records(writer, values, (w, id) => TileBinaryFields.Text(w, id.Value));

    private static FeatureId[] Ids(BinaryReader reader, BakeKey key) => TileBinaryFields.Records(reader, key, r => new FeatureId(TileBinaryFields.Text(r)));

}
