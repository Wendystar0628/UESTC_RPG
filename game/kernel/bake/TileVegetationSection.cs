using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Vegetation;

namespace Campus.Kernel.Bake;

/// <summary>Vegetation owner-tile records, mother instances and prototype slots.</summary>
internal static class TileVegetationSection
{
    internal static void Vegetation(BinaryWriter writer, VegetationTileContent content)
    {
        var key = content.Key;
        TileBinaryFields.RequireTile(content.Key, content.Tile, BakeModuleIds.VegetationTrees, BakeModuleIds.VegetationGrass);
        TileBinaryFields.Json(writer, content.Tile);
        writer.Write(content.Mothers.Count);
        foreach (var mother in content.Mothers)
        {
            ValidateMother(content.Key, mother);
            TileBinaryFields.Text(writer, mother.Key);
            TileBinaryFields.Channel(writer, mother.Records.Span, key);
            writer.Write(mother.Bounds.Count);
            foreach (var bounds in mother.Bounds) TileBinaryFields.Box(writer, bounds);
            writer.Write(mother.Prototypes.Count);
            foreach (var prototype in mother.Prototypes) TileGeometrySection.Prototype(writer, prototype, key);
            if (mother.GrassIndex is { } index) GrassIndex(writer, index, key);
        }
    }

    internal static VegetationTileContent Vegetation(BinaryReader reader, BakeKey key)
    {
        var tile = TileBinaryFields.Json<TileKey>(reader);
        TileBinaryFields.RequireTile(key, tile, BakeModuleIds.VegetationTrees, BakeModuleIds.VegetationGrass);
        var mothers = new VegetationMotherData[TileBinaryFields.Count(reader)];
        for (var i = 0; i < mothers.Length; i++)
        {
            var id = TileBinaryFields.Text(reader, 1024);
            var records = TileBinaryFields.Channel<float>(reader, key);
            var bounds = new Box3[TileBinaryFields.Count(reader, BakeCacheStore.MaximumProductBytes / 24)];
            for (var j = 0; j < bounds.Length; j++) bounds[j] = TileBinaryFields.Box(reader);
            var prototypes = new GeometryPrototype[TileBinaryFields.Count(reader, 4)];
            for (var j = 0; j < prototypes.Length; j++) prototypes[j] = TileGeometrySection.Prototype(reader, key);
            var index = key.Module == BakeModuleIds.VegetationGrass ? GrassIndex(reader, key, bounds.Length) : null;
            mothers[i] = VegetationMotherData.TakeOwnership(id, records, prototypes, bounds, index);
            ValidateMother(key, mothers[i]);
        }
        if (mothers.Select(mother => mother.Key).Distinct(StringComparer.Ordinal).Count() != mothers.Length)
            throw new InvalidDataException("Duplicate vegetation mother key.");
        return new(key, tile, Array.AsReadOnly(mothers));
    }

    private static void ValidateMother(BakeKey key, VegetationMotherData mother)
    {
        var count = mother.Records.Length / InstanceStreamLayout.FloatsPerInstance;
        if (string.IsNullOrWhiteSpace(mother.Key) ||
            mother.Records.Length % InstanceStreamLayout.FloatsPerInstance != 0 || mother.Bounds.Count != count ||
            mother.Prototypes.Count != (key.Module == BakeModuleIds.VegetationTrees ? 4 : 1) ||
            (key.Module == BakeModuleIds.VegetationGrass) != (mother.GrassIndex is not null))
            throw new InvalidDataException("Invalid vegetation mother records or prototype slots.");
        TileGeometrySection.Finite(mother.Records.Span);
        foreach (var bounds in mother.Bounds) TileGeometrySection.Bounds(bounds);
        if (mother.GrassIndex is { } index) ValidateGrassIndex(index, count);
    }

    private static void GrassIndex(BinaryWriter writer, GrassPoolIndex index, BakeKey key)
    {
        writer.Write(index.Capacity);
        TileBinaryFields.Records(writer, index.Cells, (w, cell) =>
        {
            w.Write(cell.X); w.Write(cell.Z); w.Write(cell.Start); w.Write(cell.Count);
            TileBinaryFields.Box(w, cell.Bounds);
        });
        TileBinaryFields.Channel(writer, index.Members.Span, key);
    }

    private static GrassPoolIndex GrassIndex(BinaryReader reader, BakeKey key, int count)
    {
        var capacity = reader.ReadInt32();
        var cells = TileBinaryFields.Records(reader, key, r => new GrassPoolCell(
            r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), TileBinaryFields.Box(r)), count);
        var members = TileBinaryFields.Channel<int>(reader, key);
        return GrassPoolIndex.TakeOwnership(cells, members, capacity);
    }

    private static void ValidateGrassIndex(GrassPoolIndex index, int count)
    {
        if (index.Members.Length != count || index.Capacity < 1 || index.Capacity > Math.Max(1, count))
            throw new InvalidDataException("Invalid grass index capacity or member count.");
        var cells = new HashSet<(int, int)>();
        var seen = new bool[count];
        var start = 0;
        foreach (var cell in index.Cells)
        {
            if (!cells.Add((cell.X, cell.Z)) || cell.Start != start || cell.Count <= 0 ||
                cell.Count > index.Capacity || cell.Count > count - start)
                throw new InvalidDataException("Invalid grass index cell range.");
            TileGeometrySection.Bounds(cell.Bounds);
            foreach (var member in index.Members.Span.Slice(start, cell.Count))
            {
                if ((uint)member >= (uint)count || seen[member])
                    throw new InvalidDataException("Invalid grass index member coverage.");
                seen[member] = true;
            }
            start += cell.Count;
        }
        if (start != count) throw new InvalidDataException("Incomplete grass index member coverage.");
    }
}
