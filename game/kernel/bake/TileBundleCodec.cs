using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>TBIN v4 stores bounded physical render parts and preserves full-precision surface metadata.</summary>








public static partial class TileBundleCodec
{
    private const byte GeometryKind = 1;
    private const byte CollisionKind = 2;
    private const byte VegetationKind = 3;
    private const byte FaunaKind = 4;
    private const byte NightlightKind = 5;
    private const byte TerrainCacheKind = 1;
    private const byte ResidentCacheKind = 2;
    private const byte SourcesCacheKind = 3;
    private const byte FidelityCacheKind = 4;
    private const uint Magic = 0x4E494254;
    private const int MaximumItems = 4096;
    private const int MaximumCollisionShapes = 16384;
    private const int MaximumJson = 1024 * 1024;
    private static readonly JsonSerializerOptions TileJson = CreateTileJson();

    private static JsonSerializerOptions CreateTileJson()
    {
        var options = new JsonSerializerOptions(ContractJson.Options);
        // A VariantTag may carry the exact empty season mask (default struct value).
        // Retain that bit mask rather than requiring a named enum member or changing its meaning.
        options.Converters.Insert(0, new JsonStringEnumConverter<SeasonMask>(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
        return options;
    }

    public static byte[] Encode(BakePayloadClass payloadClass, string identity, object product)
    {
        var productKey = product switch
        {
            TileBundle tile => tile.Key,
            TileBundleParts.Part part => part.Tile.Key,
            CollisionTile collision => collision.Key,
            Campus.Contracts.Vegetation.VegetationTileContent vegetation => vegetation.Key,
            Campus.Contracts.Fauna.FaunaTileContent fauna => fauna.Key,
            NightlightTileContent nightlight => nightlight.Key,
            _ => throw new NotSupportedException($"Unsupported bake payload: {product.GetType().FullName}")
        };
        var maximum = BakeCacheStore.MaximumPayload(payloadClass);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true);
        Header(writer, identity);
        switch (product)
        {
            case TileBundle tile: RenderTile(writer, TileBundleParts.Whole(tile)); break;
            case TileBundleParts.Part part: RenderTile(writer, part); break;
            case CollisionTile collision: Collision(writer, collision); break;
            case Campus.Contracts.Vegetation.VegetationTileContent vegetation:
                writer.Write(VegetationKind); Key(writer, vegetation.Key); Vegetation(writer, vegetation);
                break;
            case Campus.Contracts.Fauna.FaunaTileContent fauna:
                writer.Write(FaunaKind); Key(writer, fauna.Key); Fauna(writer, fauna);
                break;
            case NightlightTileContent nightlight:
                writer.Write(NightlightKind); Key(writer, nightlight.Key); Nightlight(writer, nightlight);
                break;
            default: throw new NotSupportedException($"Unsupported bake payload: {product.GetType().FullName}");
        }
        if (stream.Length > maximum) throw new InvalidDataException($"Bake container {productKey.Map.Value}/{productKey.Module}/{productKey.Detail}: {stream.Length} bytes exceeds limit {maximum}.");
        return stream.ToArray();
    }

    public static object Decode(BakePayloadClass payloadClass, string identity, BakeKey expectedKey, byte[] bytes)
    {
        var product = DecodePart(payloadClass, identity, expectedKey, bytes);
        return product is TileBundleParts.Part part ? TileBundleParts.Join([part]) : product;
    }

    internal static byte[]? EncodePartCandidate(string identity, TileBundleParts.Part part)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true);
        Header(writer, identity);
        RenderTile(writer, part);
        return stream.Length <= BakeCacheStore.MaximumPayloadBytes ? stream.ToArray() : null;
    }

    internal static object DecodePart(BakePayloadClass payloadClass, string identity, BakeKey expectedKey, byte[] bytes)
    {
        var maximum = BakeCacheStore.MaximumPayload(payloadClass);
        if (bytes.Length <= 0 || bytes.Length > maximum)
            throw new InvalidDataException($"Bake container {expectedKey.Map.Value}/{expectedKey.Module}/{expectedKey.Detail}: {bytes.Length} bytes exceeds limit {maximum}.");
        using var stream = new MemoryStream(bytes, false);
        using var reader = new BinaryReader(stream, new UTF8Encoding(false, true));
        try
        {
            Header(reader, identity);
            var kind = reader.ReadByte();
            var key = Key(reader);
            if (key != expectedKey) throw new InvalidDataException("Tile request key mismatch.");
            object result;
            switch (kind)
            {
                case GeometryKind: result = RenderTile(reader, key); break;
                case CollisionKind: result = Collision(reader, key); break;
                case VegetationKind: result = Vegetation(reader, key); break;
                case FaunaKind: result = Fauna(reader, key); break;
                case NightlightKind: result = Nightlight(reader, key); break;
                default: throw new InvalidDataException("Unknown tile payload kind.");
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing bytes in tile payload.");
            if (result is TileBundleParts.Part part) TileBundleParts.Validate(part);
            return result;
        }
        catch (Exception error) when (error is EndOfStreamException or JsonException or ArgumentException or OverflowException)
        { throw new InvalidDataException("Invalid tile payload: " + error.Message, error); }
    }

    private static void Header(BinaryWriter writer, string identity)
    { writer.Write(Magic); writer.Write(4); Text(writer, identity); }

    private static void Header(BinaryReader reader, string identity)
    {
        if (reader.ReadUInt32() != Magic || reader.ReadInt32() != 4 || Text(reader, 2048) != identity)
            throw new InvalidDataException("Tile header/key mismatch.");
    }

    private static void RenderTile(BinaryWriter writer, TileBundleParts.Part part)
    {
        TileBundleParts.Validate(part);
        var tile = part.Tile;
        writer.Write(GeometryKind); Key(writer, tile.Key); Json(writer, tile.Tile); writer.Write((byte)tile.Level); Json(writer, tile.Bounds);
        Json(writer, part.Location);
        writer.Write(tile.Surfaces.Count);
        foreach (var surface in tile.Surfaces) WriteSurface(writer, surface);
        writer.Write(tile.MultiMeshes.Count);
        foreach (var multi in tile.MultiMeshes)
        {
            WriteSurface(writer, multi.Surface); writer.Write(multi.InstanceCount); Bytes(writer, multi.InstanceData); Json(writer, multi.Bounds);
        }
        Text(writer, tile.StatisticsJson);
    }

    private static TileBundleParts.Part RenderTile(BinaryReader reader, BakeKey key)
    {
        var tile = Json<TileKey>(reader);
        var level = (LodLevel)reader.ReadByte();
        if (!Enum.IsDefined(level)) throw new InvalidDataException("Unknown tile LOD.");
        var bounds = Json<Box3>(reader);
        var location = Json<TileBundleParts.Location>(reader);
        var surfaces = new BakedSurface[Count(reader)];
        for (var i = 0; i < surfaces.Length; i++) surfaces[i] = Surface(reader);
        var multis = new BakedMultiMesh[Count(reader)];
        for (var i = 0; i < multis.Length; i++)
        {
            var surface = Surface(reader); var count = reader.ReadInt32(); var data = Bytes(reader); var box = Json<Box3>(reader);
            multis[i] = new(surface, count, data, box);
        }
        return new(new(key, tile, level, bounds, surfaces, multis, Statistics(reader)), location);
    }

    private static void Collision(BinaryWriter writer, CollisionTile collision)
    {
        if (collision.Colliders.Count > MaximumCollisionShapes)
            throw new InvalidDataException($"Too many collision shapes in tile {collision.Tile}: {collision.Colliders.Count} > {MaximumCollisionShapes}.");
        writer.Write(CollisionKind); Key(writer, collision.Key); Json(writer, collision.Tile); Json(writer, collision.Bounds);
        Records(writer, collision.Colliders, (w, collider) => CollisionShape(w, collider, collision.Key)); Text(writer, collision.StatisticsJson);
    }

    private static CollisionTile Collision(BinaryReader reader, BakeKey key)
    {
        var tile = Json<TileKey>(reader); var bounds = Json<Box3>(reader);
        var colliders = Records(reader, key, r => CollisionShape(r, key), MaximumCollisionShapes);
        return new(key, tile, bounds, colliders, Statistics(reader));
    }

    internal static void WriteSurface(BinaryWriter writer, BakedSurface surface)
    {
        Json(writer, new SurfaceMetadata(surface.Material, surface.Format, surface.Primitive, surface.VertexCount,
            surface.IndexCount, surface.Bounds, surface.At, surface.UvScale, surface.Variant, surface.IgnoreOcclusionCulling, surface.Shader, surface.Reflect));
        Bytes(writer, surface.VertexData); Bytes(writer, surface.AttributeData); Bytes(writer, surface.IndexData); Bytes(writer, surface.SkinData);
        writer.Write(surface.Lods.Count);
        foreach (var lod in surface.Lods) { writer.Write(lod.Error); writer.Write(lod.IndexCount); Bytes(writer, lod.IndexData); }
    }

    private static BakedSurface Surface(BinaryReader reader)
    {
        var meta = Json<SurfaceMetadata>(reader);
        var vertices = Bytes(reader); var attributes = Bytes(reader); var indices = Bytes(reader); var skin = Bytes(reader);
        var lods = new BakedMeshLod[Count(reader, 32)];
        for (var i = 0; i < lods.Length; i++) lods[i] = new(reader.ReadSingle(), reader.ReadInt32(), Bytes(reader));
        if (meta.Shader is { } shader && !Enum.IsDefined(shader)) throw new InvalidDataException("Unknown surface shader ID.");
        return new(meta.Material, meta.Format, meta.Primitive, meta.VertexCount, meta.IndexCount,
            vertices, attributes, indices, meta.Bounds, meta.At, lods, skin, meta.UvScale, meta.Variant, meta.IgnoreOcclusionCulling)
        { Shader = meta.Shader, Reflect = meta.Reflect };
    }

    private static void Key(BinaryWriter writer, BakeKey key) { Text(writer, key.Map.Value); Text(writer, key.Module); Text(writer, key.Detail); }
    private static BakeKey Key(BinaryReader reader) => new(new(Text(reader, 128)), Text(reader, 256), Text(reader, 1024));
    private static void Text(BinaryWriter writer, string text) => Bytes(writer, Encoding.UTF8.GetBytes(text));
    private static string Text(BinaryReader reader, int maximum = MaximumJson) => new UTF8Encoding(false, true).GetString(Bytes(reader, maximum));
    private static void Json<T>(BinaryWriter writer, T value, int maximum = MaximumJson)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, TileJson);
        if (bytes.Length > maximum) throw new InvalidDataException($"Tile JSON section exceeds its limit: {bytes.Length} > {maximum}.");
        Bytes(writer, bytes);
    }
    private static T Json<T>(BinaryReader reader, int maximum = MaximumJson) => JsonSerializer.Deserialize<T>(Bytes(reader, maximum), TileJson)
        ?? throw new InvalidDataException($"Missing {typeof(T).Name} data.");
    private static void Bytes(BinaryWriter writer, ReadOnlyMemory<byte> bytes) { writer.Write(bytes.Length); writer.Write(bytes.Span); }
    private static byte[] Bytes(BinaryReader reader, int maximum = BakeCacheStore.MaximumPayloadBytes)
    {
        var length = reader.ReadInt32();
        if (length < 0 || length > maximum || length > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new InvalidDataException("Tile section length exceeds the remaining payload or its limit.");
        return reader.ReadBytes(length);
    }
    private static int Count(BinaryReader reader, int maximum = MaximumItems)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > maximum || count > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new InvalidDataException("Tile collection count exceeds limit.");
        return count;
    }
    private static string Statistics(BinaryReader reader)
    {
        var text = Text(reader, 65536);
        using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 16 });
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Statistics must be a JSON object.");
        return text;
    }

    private static void ValidateCollider(Collider collider)
    {
        if (!Enum.IsDefined(collider.Tag) || !Enum.IsDefined(collider.Foot)) throw new InvalidDataException("Unknown collision material.");
        var scalars = collider switch
        {
            WallCollider c => new[] { c.A.X, c.A.Y, c.B.X, c.B.Y, c.HalfT, c.Y0, c.Y1 },
            RampCollider c => new[] { c.P0.X, c.P0.Y, c.P0.Z, c.P1.X, c.P1.Y, c.P1.Z, c.P2.X, c.P2.Y, c.P2.Z, c.P3.X, c.P3.Y, c.P3.Z, c.Thick },
            PostCollider c => new[] { c.C.X, c.C.Y, c.R, c.Y0, c.Y1 },
            BoxCollider c => new[] { c.At.M11, c.At.M12, c.At.M13, c.At.M14, c.At.M21, c.At.M22, c.At.M23, c.At.M24, c.At.M31, c.At.M32, c.At.M33, c.At.M34, c.At.M41, c.At.M42, c.At.M43, c.At.M44, c.Half.X, c.Half.Y, c.Half.Z },
            SlabCollider c when c.Ring.Count is >= 3 and <= 65536 => c.Ring.SelectMany(p => new[] { p.X, p.Y }).Append(c.Y).Append(c.Thick).ToArray(),
            HeightFieldCollider c when c.HeightsCm.Length == 65 * 65 && (c.FootMatHalfMeter is null || c.FootMatHalfMeter.Length == 128 * 128) => [],
            SlopedWallCollider c => new[] { c.A.X, c.A.Y, c.B.X, c.B.Y, c.HalfT, c.Y0a, c.Y1a, c.Y0b, c.Y1b },
            _ => throw new InvalidDataException("Invalid or unsupported collision shape.")
        };
        if (scalars.Any(v => !float.IsFinite(v))) throw new InvalidDataException("Collision parameters must be finite.");
    }

    private sealed record SurfaceMetadata(ToonRef Material, ulong Format, BakedPrimitive Primitive, int VertexCount,
        int IndexCount, Box3 Bounds, Matrix4x4 At, Vector4 UvScale, VariantTag? Variant, bool IgnoreOcclusionCulling,
        ShaderId? Shader, bool Reflect);
}
