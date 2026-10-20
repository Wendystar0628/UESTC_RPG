using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;

namespace Campus.Kernel.Bake;

/// <summary>Fauna owner-tile site records and their Day/Night/Rain content sets.</summary>
internal static class TileFaunaSection
{
    internal static void Fauna(BinaryWriter writer, FaunaTileContent content)
    {
        var key = content.Key;
        TileBinaryFields.RequireTile(content.Key, content.Tile, BakeModuleIds.FaunaRuntime);
        TileBinaryFields.Json(writer, content.Tile); TileBinaryFields.Records(writer, content.Content.Log, TileBinaryFields.Text);
        writer.Write(content.Content.Sites.Count);
        foreach (var site in content.Content.Sites)
        {
            ValidateSite(site);
            TileBinaryFields.Text(writer, site.Key); writer.Write((byte)site.Kind); TileBinaryFields.Box(writer, site.Bounds);
            foreach (var output in site.Sets) TileContentSection.Output(writer, output, key);
        }
    }

    internal static FaunaTileContent Fauna(BinaryReader reader, BakeKey key)
    {
        var tile = TileBinaryFields.Json<TileKey>(reader); TileBinaryFields.RequireTile(key, tile, BakeModuleIds.FaunaRuntime);
        var log = TileBinaryFields.Records(reader, key, r => TileBinaryFields.Text(r, BakeCacheStore.MaximumProductBytes));
        var sites = new FaunaSiteContent[TileBinaryFields.Count(reader)];
        for (var i = 0; i < sites.Length; i++)
        {
            var id = TileBinaryFields.Text(reader, 1024); var kind = (FaunaKind)reader.ReadByte(); var bounds = TileBinaryFields.Box(reader);
            var sets = new ContentOutput[3];
            for (var j = 0; j < sets.Length; j++) sets[j] = TileContentSection.Output(reader, key);
            sites[i] = new(id, kind, bounds, Array.AsReadOnly(sets)); ValidateSite(sites[i]);
        }
        if (sites.Select(site => site.Key).Distinct(StringComparer.Ordinal).Count() != sites.Length)
            throw new InvalidDataException("Duplicate fauna site key.");
        return new(key, tile, new(Array.AsReadOnly(sites), Array.AsReadOnly(log)));
    }

    private static void ValidateSite(FaunaSiteContent site)
    {
        if (string.IsNullOrWhiteSpace(site.Key) || !Enum.IsDefined(site.Kind) || site.Sets is null || site.Sets.Count != 3 || site.Sets.Any(set => set is null))
            throw new InvalidDataException("Fauna requires explicit Day/Night/Rain sets.");
        TileGeometrySection.Bounds(site.Bounds);
    }

}
