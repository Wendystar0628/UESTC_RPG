using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;
using Campus.Contracts.Vegetation;

namespace Campus.Kernel.Bake;

public static partial class TileBundleCodec
{
    private static void Vegetation(BinaryWriter writer, VegetationTileContent content)
    {
        var key = content.Key;
        RequireTile(content.Key, content.Tile, BakeModuleIds.VegetationTrees, BakeModuleIds.VegetationGrass);
        Json(writer, content.Tile);
        writer.Write(content.Mothers.Count);
        foreach (var mother in content.Mothers)
        {
            ValidateMother(content.Key, mother);
            Text(writer, mother.Key);
            Channel(writer, mother.Records.Span, key);
            writer.Write(mother.Bounds.Count);
            foreach (var bounds in mother.Bounds) Box(writer, bounds);
            writer.Write(mother.Prototypes.Count);
            foreach (var prototype in mother.Prototypes) Prototype(writer, prototype, key);
        }
    }

    private static VegetationTileContent Vegetation(BinaryReader reader, BakeKey key)
    {
        var tile = Json<TileKey>(reader);
        RequireTile(key, tile, BakeModuleIds.VegetationTrees, BakeModuleIds.VegetationGrass);
        var mothers = new VegetationMotherData[Count(reader)];
        for (var i = 0; i < mothers.Length; i++)
        {
            var id = Text(reader, 1024);
            var records = Channel<float>(reader, key);
            var bounds = new Box3[Count(reader, BakeCacheStore.MaximumProductBytes / 24)];
            for (var j = 0; j < bounds.Length; j++) bounds[j] = Box(reader);
            var prototypes = new GeometryPrototype[Count(reader, 4)];
            for (var j = 0; j < prototypes.Length; j++) prototypes[j] = Prototype(reader, key);
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
        Finite(mother.Records.Span);
        foreach (var bounds in mother.Bounds) Bounds(bounds);
    }

    private static void Fauna(BinaryWriter writer, FaunaTileContent content)
    {
        var key = content.Key;
        RequireTile(content.Key, content.Tile, BakeModuleIds.FaunaRuntime);
        Json(writer, content.Tile); Records(writer, content.Content.Log, Text);
        writer.Write(content.Content.Sites.Count);
        foreach (var site in content.Content.Sites)
        {
            ValidateSite(site);
            Text(writer, site.Key); writer.Write((byte)site.Kind); Box(writer, site.Bounds);
            foreach (var output in site.Sets) Output(writer, output, key);
        }
    }

    private static FaunaTileContent Fauna(BinaryReader reader, BakeKey key)
    {
        var tile = Json<TileKey>(reader); RequireTile(key, tile, BakeModuleIds.FaunaRuntime);
        var log = Records(reader, key, r => Text(r, BakeCacheStore.MaximumProductBytes));
        var sites = new FaunaSiteContent[Count(reader)];
        for (var i = 0; i < sites.Length; i++)
        {
            var id = Text(reader, 1024); var kind = (FaunaKind)reader.ReadByte(); var bounds = Box(reader);
            var sets = new ContentOutput[3];
            for (var j = 0; j < sets.Length; j++) sets[j] = Output(reader, key);
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
        Bounds(site.Bounds);
    }

    private static void ValidateOutput(ContentOutput output)
    {
        if (output.Meshes is null || output.Parts is null || output.Instances is null || output.Entrances is null ||
            output.Windows is null || output.LooseWindows is null || output.Seats is null || output.Meshes.Any(mesh => mesh is null))
            throw new InvalidDataException("Incomplete fauna content output.");
        foreach (var mesh in output.Meshes) mesh.ValidateVertexData();
    }

    private static void Prototype(BinaryWriter writer, GeometryPrototype prototype, BakeKey key)
    {
        Text(writer, prototype.Id); Box(writer, prototype.Bounds); writer.Write(prototype.Surfaces.Count);
        foreach (var surface in prototype.Surfaces) Geometry(writer, surface, key);
    }

    private static GeometryPrototype Prototype(BinaryReader reader, BakeKey key)
    {
        var id = Text(reader, 1024); var bounds = Box(reader);
        var surfaces = new GeometryBuffer[Count(reader)];
        for (var i = 0; i < surfaces.Length; i++)
        {
            surfaces[i] = GeometryBuffer.Freeze(Geometry(reader, key));
        }
        return new(id, surfaces, bounds);
    }

    private static void Nightlight(BinaryWriter writer, NightlightTileContent content)
    {
        var key = content.Key;
        ValidateNightlight(content);
        Json(writer, content.Tile); Channel<int>(writer, content.LampIds.ToArray(), key);
        writer.Write((byte)content.Glow.Kind); writer.Write(content.Glow.Count);
        Channel<float>(writer, content.Glow.Buffer, key); Box(writer, content.Glow.Bounds);
        Channel<int>(writer, content.ReflectionWindowIndices.ToArray(), key);
    }

    private static NightlightTileContent Nightlight(BinaryReader reader, BakeKey key)
    {
        var tile = Json<TileKey>(reader); var ids = Channel<int>(reader, key);
        var kind = (GlowKind)reader.ReadByte(); var count = reader.ReadInt32();
        var buffer = Channel<float>(reader, key); var bounds = Box(reader);
        var content = new NightlightTileContent(key, tile, ids, new(kind, count, buffer, bounds), Channel<int>(reader, key));
        ValidateNightlight(content);
        return content;
    }

    private static void ValidateNightlight(NightlightTileContent content)
    {
        RequireTile(content.Key, content.Tile, BakeModuleIds.NightlightLamps, BakeModuleIds.NightlightWindows);
        var glow = content.Glow;
        if (glow is null || glow.Buffer is null || content.LampIds is null || content.ReflectionWindowIndices is null || glow.Count < 0 || glow.Buffer.Length != (long)glow.Count * GlowSet.Stride ||
            glow.Kind != (content.Key.Module == BakeModuleIds.NightlightLamps ? GlowKind.Lamp : GlowKind.Window))
            throw new InvalidDataException("Invalid fixed nightlight glow payload.");
        Finite(glow.Buffer); Bounds(glow.Bounds);
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

    private static void RequireTile(BakeKey key, TileKey tile, params string[] modules)
    {
        var request = BakeRequestKeys.Parse(key);
        if (!modules.Contains(key.Module, StringComparer.Ordinal) || request.Collision || request.Level != LodLevel.L0 || request.Tile != tile)
            throw new InvalidDataException("Auxiliary payload key, kind and owner tile disagree.");
    }

    private static void Bounds(Box3 bounds)
    {
        if (!VectorMath.Finite(bounds.Min) || !VectorMath.Finite(bounds.Max) ||
            bounds.Min.X > bounds.Max.X || bounds.Min.Y > bounds.Max.Y || bounds.Min.Z > bounds.Max.Z)
            throw new InvalidDataException("Invalid auxiliary bounds.");
    }
    private static void Finite(ReadOnlySpan<float> values)
    {
        foreach (var value in values)
            if (!float.IsFinite(value)) throw new InvalidDataException("Non-finite auxiliary instance record.");
    }
}
