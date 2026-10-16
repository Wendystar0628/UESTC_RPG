using System.Buffers.Binary;
using System.Text;
using Campus.Contracts.Bake;
using Campus.Pipeline.Map;

namespace Campus.Kernel.Bake;

internal static class BakeProductCodec
{
    private const uint MapMagic = 0x3150414D;
    public static byte[] Encode(string identity, object product)
    {
        if (product is not MapBakeProduct map) return TileBundleCodec.Encode(identity, product);
        var key = Encoding.UTF8.GetBytes(identity);
        var payload = MapBakeCodec.Serialize(map);
        var result = new byte[checked(8 + key.Length + payload.Length)];
        BinaryPrimitives.WriteUInt32LittleEndian(result, MapMagic);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4), key.Length);
        key.CopyTo(result, 8); payload.CopyTo(result, 8 + key.Length);
        return result;
    }
    public static object Decode(string identity, BakeKey key, byte[] bytes)
    {
        if (key.Module != "map") return TileBundleCodec.Decode(identity, key, bytes);
        if (bytes.Length < 8 || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != MapMagic) throw new InvalidDataException("Invalid map bake wrapper.");
        var length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4));
        if (length is < 1 or > 2048 || length > bytes.Length - 8 || Encoding.UTF8.GetString(bytes, 8, length) != identity)
            throw new InvalidDataException("Map bake identity mismatch.");
        return MapBakeCodec.Deserialize(bytes.AsSpan(8 + length));
    }
}
