using Campus.Contracts.Atlas;
using System.Buffers.Binary;
using System.Text;
using Campus.Contracts.Bake;
using Campus.Gen.BakeInput;

namespace Campus.Kernel.Bake;

internal static class BakeProductCodec
{
    private const uint MapMagic = 0x3150414D;
    internal static IReadOnlyList<byte[]> EncodeParts(BakePayloadClass payloadClass, string identity, object product, Func<byte[], bool> fits) =>
        product is TileBundle tile ? TileBundleParts.Encode(identity, tile, fits) : [Encode(payloadClass, identity, product)];

    internal static object DecodeParts(BakePayloadClass payloadClass, string identity, BakeKey key, IReadOnlyList<byte[]> parts)
    {
        if (parts.Count == 0) throw new InvalidDataException("Missing bake delivery parts.");
        if (key.Module == MapBaker.Module)
            return parts.Count == 1 ? Decode(payloadClass, identity, key, parts[0]) :
                throw new InvalidDataException("Map products must have one delivery part.");
        var decoded = parts.Select(bytes => TileBundleCodec.DecodePart(payloadClass, identity, key, bytes)).ToArray();
        if (decoded[0] is not TileBundleParts.Part)
            return decoded.Length == 1 ? decoded[0] : throw new InvalidDataException("Non-render products must have one delivery part.");
        if (decoded.Any(value => value is not TileBundleParts.Part)) throw new InvalidDataException("Mixed bake delivery parts.");
        return TileBundleParts.Join(decoded.Cast<TileBundleParts.Part>().ToArray());
    }

    public static byte[] Encode(BakePayloadClass payloadClass, string identity, object product)
    {
        if (product is not MapBakeProduct map) return TileBundleCodec.Encode(payloadClass, identity, product);
        var key = Encoding.UTF8.GetBytes(identity);
        var payload = MapBakeCodec.Serialize(map);
        var request = new BakeKey(new(map.Manifest.Map), MapBaker.Module, MapBaker.Detail);
        var length = checked(8 + key.Length + payload.Length);
        var maximum = BakeCacheStore.MaximumPayload(payloadClass);
        if (length > maximum) throw new InvalidDataException($"Bake container {request.Map.Value}/{request.Module}/{request.Detail}: {length} bytes exceeds limit {maximum}.");
        var result = new byte[length];
        BinaryPrimitives.WriteUInt32LittleEndian(result, MapMagic);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4), key.Length);
        key.CopyTo(result, 8); payload.CopyTo(result, 8 + key.Length);
        return result;
    }
    public static object Decode(BakePayloadClass payloadClass, string identity, BakeKey key, byte[] bytes)
    {
        if (key.Module != MapBaker.Module) return TileBundleCodec.Decode(payloadClass, identity, key, bytes);
        var maximum = BakeCacheStore.MaximumPayload(payloadClass);
        if (bytes.Length > maximum) throw new InvalidDataException($"Bake container {key.Map.Value}/{key.Module}/{key.Detail}: {bytes.Length} bytes exceeds limit {maximum}.");
        if (bytes.Length < 8 || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != MapMagic) throw new InvalidDataException("Invalid map bake wrapper.");
        var length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4));
        if (length is < 1 or > 2048 || length > bytes.Length - 8 || Encoding.UTF8.GetString(bytes, 8, length) != identity)
            throw new InvalidDataException("Map bake identity mismatch.");
        try { return MapBakeCodec.Deserialize(bytes.AsSpan(8 + length)); }
        catch (Exception error) when (error is EndOfStreamException or System.Text.Json.JsonException or ArgumentException or OverflowException)
        { throw new InvalidDataException("Invalid local map payload.", error); }
    }
}
