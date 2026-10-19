using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

public static partial class TileBundleCodec
{
    private static void CollisionShape(BinaryWriter writer, Collider collider, BakeKey key)
    {
        ValidateCollider(collider);
        switch (collider)
        {
            case HeightFieldCollider field:
                writer.Write((byte)1);
                Json<Collider>(writer, field with { HeightsCm = [], FootMatHalfMeter = null, Patches = null });
                Channel<short>(writer, field.HeightsCm, key);
                OptionalChannel<byte>(writer, field.FootMatHalfMeter is { } feet ? new ReadOnlyMemory<byte>(feet) : null, key);
                writer.Write(field.Patches is not null);
                if (field.Patches is { } patches) MetadataRecords(writer, patches);
                break;
            case SlabCollider slab:
                writer.Write((byte)2); Json<Collider>(writer, slab with { Ring = [] });
                Channel<Vector2>(writer, slab.Ring.ToArray(), key);
                break;
            default: writer.Write((byte)0); Json(writer, collider); break;
        }
    }

    private static Collider CollisionShape(BinaryReader reader, BakeKey key)
    {
        var kind = reader.ReadByte(); var metadata = Json<Collider>(reader);
        Collider collider = (kind, metadata) switch
        {
            (0, WallCollider or RampCollider or PostCollider or BoxCollider or SlopedWallCollider) => metadata,
            (1, HeightFieldCollider field) => field with
            {
                HeightsCm = Channel<short>(reader, key), FootMatHalfMeter = OptionalChannel<byte>(reader, key),
                Patches = reader.ReadBoolean() ? MetadataRecords<FootPatch>(reader, key) : null
            },
            (2, SlabCollider slab) => slab with { Ring = Channel<Vector2>(reader, key) },
            _ => throw new InvalidDataException("Invalid collision record kind.")
        };
        ValidateCollider(collider);
        return collider;
    }
}
