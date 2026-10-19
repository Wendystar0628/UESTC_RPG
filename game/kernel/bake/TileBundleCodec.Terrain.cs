using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Bake;

public static partial class TileBundleCodec
{
    private static void Terrain(BinaryWriter writer, TerrainFrozenData data, BakeKey key)
    {
        Json(writer, data.DataRect); Text(writer, data.SourceInputs.CommonStamp);
        SourceCells(writer, data.SourceInputs.Cells.Select(cell => new BakeSourceCell(cell.Cell, cell.Stamp)).ToArray(), key);
        Levels(writer, data.Pads); Levels(writer, data.WaterLevels);
        Records(writer, data.Waters, (w, water) =>
        {
            Text(w, water.Water.Value);
            Records(w, water.Polygons, (r, polygon) => Rings(r, polygon, key));
            Records(w, water.WadeAreas, (r, polygon) => Rings(r, polygon, key));
        });
        Records(writer, data.Banks, (w, bank) =>
        {
            var span = bank.Span;
            Text(w, span.Id); Ids(w, span.Waters); Ids(w, span.Roads); Ids(w, span.Bridges);
            Channel<Vector2>(w, span.Shore.Points.ToArray(), key);
            w.Write(span.CoreFrom); w.Write(span.CoreTo); w.Write(span.TransitionLength); w.Write((byte)span.Kind);
            Channel<double>(w, bank.Stations, key); Channel<short>(w, bank.Heights, key);
        });
        Channel<Vector2>(writer, data.Supports.SelectMany(rect => new[] { rect.Min, rect.Max }).ToArray(), key);
        Channel<short>(writer, data.DistantHeights, key);
        Records(writer, data.Diagnostics, Text);
    }

    private static TerrainFrozenData Terrain(BinaryReader reader, BakeKey key)
    {
        var rect = Json<MapRect>(reader); var common = Text(reader);
        var sources = SourceCells(reader, key).Select(cell => new TerrainInputCell(cell.Cell, cell.Stamp)).ToArray();
        var pads = Levels(reader, key); var levels = Levels(reader, key);
        var waters = Records(reader, key, r => new TerrainWaterBody(new(Text(r)),
            Records(r, key, w => Rings(w, key)), Records(r, key, w => Rings(w, key))));
        var banks = Records(reader, key, r =>
        {
            var id = Text(r); var water = Ids(r, key); var roads = Ids(r, key); var bridges = Ids(r, key);
            var shore = Channel<Vector2>(r, key);
            var span = new TerrainBankSpan(id, water, roads, bridges, new(shore), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), (TerrainBankKind)r.ReadByte());
            return new TerrainBankProfile(span, Channel<double>(r, key), Channel<short>(r, key));
        });
        var corners = Channel<Vector2>(reader, key);
        if (corners.Length % 2 != 0) throw new InvalidDataException("Terrain supports require pairs of corners.");
        var supports = new MapRect[corners.Length / 2];
        for (var i = 0; i < supports.Length; i++) supports[i] = new(corners[i * 2], corners[i * 2 + 1]);
        return new(rect, new(common, sources), pads, levels, waters, banks, supports,
            Channel<short>(reader, key), Records(reader, key, r => Text(r)));
    }

    private static void Levels(BinaryWriter writer, IReadOnlyList<TerrainFeatureLevel> values) => Records(writer, values,
        (w, value) => { Text(w, value.Feature.Value); w.Write(value.Y); });
    private static TerrainFeatureLevel[] Levels(BinaryReader reader, BakeKey key) => Records(reader, key,
        r => new TerrainFeatureLevel(new(Text(r)), r.ReadSingle()));
    private static void Ids(BinaryWriter writer, IReadOnlyList<FeatureId> values) => Records(writer, values, (w, id) => Text(w, id.Value));
    private static FeatureId[] Ids(BinaryReader reader, BakeKey key) => Records(reader, key, r => new FeatureId(Text(r)));
}
