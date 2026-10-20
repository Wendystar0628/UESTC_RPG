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
            mothers[i] = new(id, records, prototypes, bounds);
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
            mother.Prototypes.Count != (key.Module == BakeModuleIds.VegetationTrees ? 4 : 1))
            throw new InvalidDataException("Invalid vegetation mother records or prototype slots.");
        TileGeometrySection.Finite(mother.Records.Span);
        foreach (var bounds in mother.Bounds) TileGeometrySection.Bounds(bounds);
    }

}
