using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Nightlight owner-tile lamp and window glow buffers.</summary>
internal static class TileNightlightSection
{
    internal static void Nightlight(BinaryWriter writer, NightlightTileContent content)
    {
        var key = content.Key;
        ValidateNightlight(content);
        TileBinaryFields.Json(writer, content.Tile); TileBinaryFields.Channel<int>(writer, content.LampIds.ToArray(), key);
        writer.Write((byte)content.Glow.Kind); writer.Write(content.Glow.Count);
        TileBinaryFields.Channel<float>(writer, content.Glow.Buffer, key); TileBinaryFields.Box(writer, content.Glow.Bounds);
        TileBinaryFields.Channel<int>(writer, content.ReflectionWindowIndices.ToArray(), key);
    }

    internal static NightlightTileContent Nightlight(BinaryReader reader, BakeKey key)
    {
        var tile = TileBinaryFields.Json<TileKey>(reader); var ids = TileBinaryFields.Channel<int>(reader, key);
        var kind = (GlowKind)reader.ReadByte(); var count = reader.ReadInt32();
        var buffer = TileBinaryFields.Channel<float>(reader, key); var bounds = TileBinaryFields.Box(reader);
        var content = new NightlightTileContent(key, tile, ids, new(kind, count, buffer, bounds), TileBinaryFields.Channel<int>(reader, key));
        ValidateNightlight(content);
        return content;
    }

    private static void ValidateNightlight(NightlightTileContent content)
    {
        TileBinaryFields.RequireTile(content.Key, content.Tile, BakeModuleIds.NightlightLamps, BakeModuleIds.NightlightWindows);
        var glow = content.Glow;
        if (glow is null || glow.Buffer is null || content.LampIds is null || content.ReflectionWindowIndices is null || glow.Count < 0 || glow.Buffer.Length != (long)glow.Count * GlowSet.Stride ||
            glow.Kind != (content.Key.Module == BakeModuleIds.NightlightLamps ? GlowKind.Lamp : GlowKind.Window))
            throw new InvalidDataException("Invalid fixed nightlight glow payload.");
        TileGeometrySection.Finite(glow.Buffer); TileGeometrySection.Bounds(glow.Bounds);
        if (content.LampIds.Any(id => id < 0) || content.LampIds.Distinct().Count() != content.LampIds.Count ||
            content.ReflectionWindowIndices.Any(index => index < 0 || index >= glow.Count) ||
            content.ReflectionWindowIndices.Distinct().Count() != content.ReflectionWindowIndices.Count)
            throw new InvalidDataException("Invalid nightlight ID or reflection buffer index.");
        if (glow.Kind == GlowKind.Lamp)
        {
            if (content.LampIds.Count != glow.Count || content.ReflectionWindowIndices.Count != 0)
                throw new InvalidDataException("Lamp glow records must match resident catalog IDs.");
            for (var i = 0; i < glow.Count; i++)
                if (glow.Buffer[i * GlowSet.Stride + 12] != content.LampIds[i])
                    throw new InvalidDataException("Lamp glow custom ID differs from its catalog ID.");
        }
        else if (content.LampIds.Count != 0) throw new InvalidDataException("Window payload cannot own lamp IDs.");
    }

}
