using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Parts;

namespace Campus.Kernel.Bake;

/// <summary>Content-output records and placed-part parameters, transforms and printed glyphs.</summary>
internal static class TileContentSection
{
    private static void ValidateOutput(ContentOutput output)
    {
        if (output.Meshes is null || output.Parts is null || output.Instances is null || output.Entrances is null ||
            output.Windows is null || output.LooseWindows is null || output.Seats is null || output.Meshes.Any(mesh => mesh is null))
            throw new InvalidDataException("Incomplete fauna content output.");
        foreach (var mesh in output.Meshes) mesh.ValidateVertexData();
    }

    internal static void Output(BinaryWriter writer, ContentOutput output, BakeKey key)
    {
        ValidateOutput(output);
        TileBinaryFields.Records(writer, output.Meshes, (w, mesh) => TileGeometrySection.Geometry(w, GeometryBuffer.Freeze(mesh), key));
        TileBinaryFields.Records(writer, output.Parts, (w, part) => Part(w, part, key));
        writer.Write(output.Emitters is not null);
        if (output.Emitters is { } emitters) TileBinaryFields.MetadataRecords(writer, emitters);
        writer.Write(output.Occupancy is not null);
        if (output.Occupancy is { } occupancy)
            TileBinaryFields.Records(writer, occupancy, (w, footprint) =>
            {
                TileBinaryFields.Text(w, footprint.Feature.Value); w.Write(footprint.MinY); w.Write(footprint.MaxY); TileBinaryFields.Rings(w, footprint.Rings, key);
            });
        TileBinaryFields.MetadataRecords(writer, output.Entrances);
        TileLightingSection.Windows(writer, new(output.Windows, output.LooseWindows), key);
        TileBinaryFields.MetadataRecords(writer, output.Seats);
        TileBinaryFields.Records(writer, output.Instances, (w, instance) =>
        {
            TileBinaryFields.Text(w, instance.Id); TileGeometrySection.Prototype(w, instance.Prototype, key);
            TileBinaryFields.Channel(w, instance.Data.Span, key); TileBinaryFields.Box(w, instance.Bounds);
        });
    }

    internal static ContentOutput Output(BinaryReader reader, BakeKey key)
    {
        var meshes = TileBinaryFields.Records(reader, key, r => TileGeometrySection.Geometry(r, key), TileBinaryFields.MaximumItems);
        var parts = TileBinaryFields.Records(reader, key, r => Part(r, key));
        var emitters = reader.ReadBoolean() ? TileBinaryFields.MetadataRecords<LightEmitter>(reader, key) : null;
        var occupancy = reader.ReadBoolean() ? TileBinaryFields.Records(reader, key, r =>
        {
            var feature = new FeatureId(TileBinaryFields.Text(r)); var min = r.ReadSingle(); var max = r.ReadSingle();
            return new Footprint2D(feature, TileBinaryFields.Rings(r, key), min, max);
        }) : null;
        var entrances = TileBinaryFields.MetadataRecords<EntranceDecl>(reader, key);
        var windows = TileLightingSection.Windows(reader, key); var seats = TileBinaryFields.MetadataRecords<SeatDecl>(reader, key);
        var instances = TileBinaryFields.Records(reader, key, r =>
        {
            var id = TileBinaryFields.Text(r, 1024); var prototype = TileGeometrySection.Prototype(r, key);
            var data = TileBinaryFields.Channel<float>(r, key); return GeometryInstanceStream.TakeOwnership(id, prototype, data, TileBinaryFields.Box(r));
        }, TileBinaryFields.MaximumItems);
        var result = new ContentOutput(Array.AsReadOnly(meshes), Array.AsReadOnly(parts), emitters, occupancy)
        {
            Entrances = entrances, Windows = windows.Facades, LooseWindows = windows.Windows, Seats = seats,
            Instances = Array.AsReadOnly(instances)
        };
        ValidateOutput(result);
        return result;
    }

    private static void Part(BinaryWriter writer, PlacedPart part, BakeKey key)
    {
        var entry = PartCatalog.Find(part.Part) ?? throw new InvalidDataException($"Unknown part: {part.Part.Value}");
        if (part.Params.GetType() != entry.ParameterType) throw new InvalidDataException($"Wrong parameters for part: {part.Part.Value}");
        TileBinaryFields.Json(writer, new PartMetadata(part.Part, part.Tint, part.Emitter, part.Variant, part.IgnoreOcclusionCulling));
        TileBinaryFields.Channel<Matrix4x4>(writer, [part.At], key);
        var printed = part.Params switch { SignParams sign => sign.PrintedText, EquipmentParams equipment => equipment.PrintedText, _ => null };
        object parameters = part.Params switch
        {
            SignParams sign => sign with { PrintedText = null },
            EquipmentParams equipment => equipment with { PrintedText = null },
            _ => part.Params
        };
        TileBinaryFields.Json(writer, parameters);
        writer.Write(printed is not null);
        if (printed is not null) { writer.Write(printed.Width); TileBinaryFields.Channel<SceneTextGlyph>(writer, printed.Glyphs.ToArray(), key); }
    }

    private static PlacedPart Part(BinaryReader reader, BakeKey key)
    {
        var meta = TileBinaryFields.Json<PartMetadata>(reader);
        var entry = PartCatalog.Find(meta.Part) ?? throw new InvalidDataException($"Unknown part: {meta.Part.Value}");
        var matrices = TileBinaryFields.Channel<Matrix4x4>(reader, key);
        if (matrices.Length != 1) throw new InvalidDataException("A placed part requires one transform.");
        var parameters = JsonSerializer.Deserialize(TileBinaryFields.Bytes(reader, TileBinaryFields.MaximumJson), entry.ParameterType, TileBinaryFields.TileJson)
            ?? throw new InvalidDataException("Missing part parameters.");
        if (reader.ReadBoolean())
        {
            var printed = new SceneTextRun(reader.ReadSingle(), TileBinaryFields.Channel<SceneTextGlyph>(reader, key));
            parameters = parameters switch
            {
                SignParams sign => sign with { PrintedText = printed },
                EquipmentParams equipment => equipment with { PrintedText = printed },
                _ => throw new InvalidDataException("This part does not declare printed text.")
            };
        }
        return new(meta.Part, parameters, matrices[0], meta.Tint, meta.Emitter, meta.Variant)
        { IgnoreOcclusionCulling = meta.IgnoreOcclusionCulling };
    }

    private sealed record PartMetadata(PartId Part, PaletteToken? Tint, ChannelId? Emitter, VariantTag? Variant, bool IgnoreOcclusionCulling);

}
