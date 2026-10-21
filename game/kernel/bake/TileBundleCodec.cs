using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Assembles TBIN tile and CPU cache envelopes and dispatches their binary sections.</summary>
public static class TileBundleCodec
{
    private const byte VegetationKind = 3;

    private const byte FaunaKind = 4;

    private const byte NightlightKind = 5;

    private const byte TerrainCacheKind = 1;

    private const byte ResidentCacheKind = 2;

    private const byte SourcesCacheKind = 3;

    private const byte FidelityCacheKind = 4;

    private const uint Magic = 0x4E494254;

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
            case TileBundle tile: TileRenderSection.RenderTile(writer, TileBundleParts.Whole(tile)); break;
            case TileBundleParts.Part part: TileRenderSection.RenderTile(writer, part); break;
            case CollisionTile collision: TileCollisionSection.Collision(writer, collision); break;
            case Campus.Contracts.Vegetation.VegetationTileContent vegetation:
                writer.Write(VegetationKind); TileBinaryFields.Key(writer, vegetation.Key); TileVegetationSection.Vegetation(writer, vegetation);
                break;
            case Campus.Contracts.Fauna.FaunaTileContent fauna:
                writer.Write(FaunaKind); TileBinaryFields.Key(writer, fauna.Key); TileFaunaSection.Fauna(writer, fauna);
                break;
            case NightlightTileContent nightlight:
                writer.Write(NightlightKind); TileBinaryFields.Key(writer, nightlight.Key); TileNightlightSection.Nightlight(writer, nightlight);
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
        TileRenderSection.RenderTile(writer, part);
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
            var key = TileBinaryFields.Key(reader);
            if (key != expectedKey) throw new InvalidDataException("Tile request key mismatch.");
            object result;
            switch (kind)
            {
                case TileRenderSection.GeometryKind: result = TileRenderSection.RenderTile(reader, key); break;
                case TileCollisionSection.CollisionKind: result = TileCollisionSection.Collision(reader, key); break;
                case VegetationKind: result = TileVegetationSection.Vegetation(reader, key); break;
                case FaunaKind: result = TileFaunaSection.Fauna(reader, key); break;
                case NightlightKind: result = TileNightlightSection.Nightlight(reader, key); break;
                default: throw new InvalidDataException("Unknown tile payload kind.");
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing bytes in tile payload.");
            if (result is TileBundleParts.Part part) TileBundleParts.Validate(part);
            return result;
        }
        catch (Exception error) when (error is EndOfStreamException or JsonException or ArgumentException or OverflowException)
        { throw new InvalidDataException("Invalid tile payload: " + error.Message, error); }
    }

    internal static byte[] EncodeCache<T>(BakePayloadClass payloadClass, BakeKey key, T product) where T : class
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true);
        CacheHeader(writer, key);
        switch (product)
        {
            case FidelityFrozenData fidelity: writer.Write(FidelityCacheKind); TileFidelitySection.Fidelity(writer, fidelity, key); break;
            case TerrainFrozenData terrain: writer.Write(TerrainCacheKind); TileTerrainSection.Terrain(writer, terrain, key); break;
            case BakeResidentData resident: writer.Write(ResidentCacheKind); TileResidentSection.Resident(writer, resident, key); break;
            case BakeSourceIndexData sources: writer.Write(SourcesCacheKind); TileSourceIndexSection.Sources(writer, sources, key); break;
            default: throw new NotSupportedException($"Unsupported CPU cache: {typeof(T).Name}");
        }
        if (stream.Length > BakeCacheStore.MaximumPayload(payloadClass)) throw new InvalidDataException("CPU cache exceeds its product limit.");
        return stream.ToArray();
    }

    internal static T DecodeCache<T>(BakePayloadClass payloadClass, byte[] bytes, BakeKey key) where T : class
    {
        if (bytes.Length > BakeCacheStore.MaximumPayload(payloadClass)) throw new InvalidDataException("CPU cache exceeds its product limit.");
        using var reader = new BinaryReader(new MemoryStream(bytes, false), new UTF8Encoding(false, true));
        try
        {
            CacheHeader(reader, key);
            object product = reader.ReadByte() switch
            {
                FidelityCacheKind when typeof(T) == typeof(FidelityFrozenData) => TileFidelitySection.Fidelity(reader, key),
                TerrainCacheKind when typeof(T) == typeof(TerrainFrozenData) => TileTerrainSection.Terrain(reader, key),
                ResidentCacheKind when typeof(T) == typeof(BakeResidentData) => TileResidentSection.Resident(reader, key),
                SourcesCacheKind when typeof(T) == typeof(BakeSourceIndexData) => TileSourceIndexSection.Sources(reader, key),
                _ => throw new InvalidDataException("CPU cache kind mismatch.")
            };
            if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing CPU cache bytes.");
            return (T)product;
        }
        catch (Exception error) when (error is EndOfStreamException or JsonException or ArgumentException or OverflowException)
        { throw new InvalidDataException("Invalid CPU cache payload.", error); }
    }

    private static void Header(BinaryWriter writer, string identity)
    { writer.Write(Magic); writer.Write(4); TileBinaryFields.Text(writer, identity); }

    private static void Header(BinaryReader reader, string identity)
    {
        if (reader.ReadUInt32() != Magic || reader.ReadInt32() != 4 || TileBinaryFields.Text(reader, 2048) != identity)
            throw new InvalidDataException("Tile header/key mismatch.");
    }

    private static void CacheHeader(BinaryWriter writer, BakeKey key)
    { writer.Write(0x55504342); writer.Write(2); TileBinaryFields.Key(writer, key); }

    private static void CacheHeader(BinaryReader reader, BakeKey key)
    {
        if (reader.ReadInt32() != 0x55504342 || reader.ReadInt32() != 2 || TileBinaryFields.Key(reader) != key)
            throw new InvalidDataException("CPU cache header/key mismatch.");
    }

}
