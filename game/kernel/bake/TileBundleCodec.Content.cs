using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Parts;

namespace Campus.Kernel.Bake;

public static partial class TileBundleCodec
{
    private static void Output(BinaryWriter writer, ContentOutput output, BakeKey key)
    {
        ValidateOutput(output);
        Records(writer, output.Meshes, (w, mesh) => Geometry(w, GeometryBuffer.Freeze(mesh), key));
        Records(writer, output.Parts, (w, part) => Part(w, part, key));
        writer.Write(output.Emitters is not null);
        if (output.Emitters is { } emitters) MetadataRecords(writer, emitters);
        writer.Write(output.Occupancy is not null);
        if (output.Occupancy is { } occupancy)
            Records(writer, occupancy, (w, footprint) =>
            {
                Text(w, footprint.Feature.Value); w.Write(footprint.MinY); w.Write(footprint.MaxY); Rings(w, footprint.Rings, key);
            });
        MetadataRecords(writer, output.Entrances);
        Windows(writer, new(output.Windows, output.LooseWindows), key);
        MetadataRecords(writer, output.Seats);
        Records(writer, output.Instances, (w, instance) =>
        {
            Text(w, instance.Id); Prototype(w, instance.Prototype, key);
            Channel(w, instance.Data.Span, key); Box(w, instance.Bounds);
        });
    }

    private static ContentOutput Output(BinaryReader reader, BakeKey key)
    {
        var meshes = Records(reader, key, r => Geometry(r, key), MaximumItems);
        var parts = Records(reader, key, r => Part(r, key));
        var emitters = reader.ReadBoolean() ? MetadataRecords<LightEmitter>(reader, key) : null;
        var occupancy = reader.ReadBoolean() ? Records(reader, key, r =>
        {
            var feature = new FeatureId(Text(r)); var min = r.ReadSingle(); var max = r.ReadSingle();
            return new Footprint2D(feature, Rings(r, key), min, max);
        }) : null;
        var entrances = MetadataRecords<EntranceDecl>(reader, key);
        var windows = Windows(reader, key); var seats = MetadataRecords<SeatDecl>(reader, key);
        var instances = Records(reader, key, r =>
        {
            var id = Text(r, 1024); var prototype = Prototype(r, key);
            var data = Channel<float>(r, key); return new GeometryInstanceStream(id, prototype, data, Box(r));
        }, MaximumItems);
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
        Json(writer, new PartMetadata(part.Part, part.Tint, part.Emitter, part.Variant, part.IgnoreOcclusionCulling));
        Channel<Matrix4x4>(writer, [part.At], key);
        var printed = part.Params switch { SignParams sign => sign.PrintedText, EquipmentParams equipment => equipment.PrintedText, _ => null };
        object parameters = part.Params switch
        {
            SignParams sign => sign with { PrintedText = null },
            EquipmentParams equipment => equipment with { PrintedText = null },
            _ => part.Params
        };
        Json(writer, parameters);
        writer.Write(printed is not null);
        if (printed is not null) { writer.Write(printed.Width); Channel<SceneTextGlyph>(writer, printed.Glyphs.ToArray(), key); }
    }

    private static PlacedPart Part(BinaryReader reader, BakeKey key)
    {
        var meta = Json<PartMetadata>(reader);
        var entry = PartCatalog.Find(meta.Part) ?? throw new InvalidDataException($"Unknown part: {meta.Part.Value}");
        var matrices = Channel<Matrix4x4>(reader, key);
        if (matrices.Length != 1) throw new InvalidDataException("A placed part requires one transform.");
        var parameters = JsonSerializer.Deserialize(Bytes(reader, MaximumJson), entry.ParameterType, TileJson)
            ?? throw new InvalidDataException("Missing part parameters.");
        if (reader.ReadBoolean())
        {
            var printed = new SceneTextRun(reader.ReadSingle(), Channel<SceneTextGlyph>(reader, key));
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
