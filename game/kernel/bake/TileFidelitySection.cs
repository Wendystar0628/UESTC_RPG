using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Frozen fidelity grid and facade, roof and feature-zone metadata.</summary>
internal static class TileFidelitySection
{
    internal static void Fidelity(BinaryWriter writer, FidelityFrozenData data, BakeKey key)
    {
        TileBinaryFields.Json(writer, data.Min); writer.Write(data.Width); writer.Write(data.Height);
        TileBinaryFields.Channel<byte>(writer, data.Ground, key); TileBinaryFields.Text(writer, data.InputHash);
        TileBinaryFields.MetadataRecords(writer, data.Facades); TileBinaryFields.MetadataRecords(writer, data.Roofs);
        TileBinaryFields.Json(writer, data.FeatureZones.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    internal static FidelityFrozenData Fidelity(BinaryReader reader, BakeKey key)
    {
        var min = TileBinaryFields.Json<System.Numerics.Vector2>(reader);
        var width = reader.ReadInt32(); var height = reader.ReadInt32();
        var ground = TileBinaryFields.Channel<byte>(reader, key); var hash = TileBinaryFields.Text(reader, 128);
        return new(min, width, height, ground, TileBinaryFields.MetadataRecords<FacadeSegment>(reader, key), TileBinaryFields.MetadataRecords<RoofZone>(reader, key),
            TileBinaryFields.Json<Dictionary<FeatureId, FidelityZone>>(reader), hash);
    }

}
