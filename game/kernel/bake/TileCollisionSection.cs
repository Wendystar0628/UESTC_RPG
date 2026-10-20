using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Collision tile records and shape-specific binary channels.</summary>
internal static class TileCollisionSection
{
    internal const byte CollisionKind = 2;

    private const int MaximumCollisionShapes = 16384;

    private static void CollisionShape(BinaryWriter writer, Collider collider, BakeKey key)
    {
        ValidateCollider(collider);
        switch (collider)
        {
            case HeightFieldCollider field:
                writer.Write((byte)1);
                TileBinaryFields.Json<Collider>(writer, field with { HeightsCm = [], FootMatHalfMeter = null, Patches = null });
                TileBinaryFields.Channel<short>(writer, field.HeightsCm, key);
                TileBinaryFields.OptionalChannel<byte>(writer, field.FootMatHalfMeter is { } feet ? new ReadOnlyMemory<byte>(feet) : null, key);
                writer.Write(field.Patches is not null);
                if (field.Patches is { } patches) TileBinaryFields.MetadataRecords(writer, patches);
                break;
            case SlabCollider slab:
                writer.Write((byte)2); TileBinaryFields.Json<Collider>(writer, slab with { Ring = [] });
                TileBinaryFields.Channel<Vector2>(writer, slab.Ring.ToArray(), key);
                break;
            default: writer.Write((byte)0); TileBinaryFields.Json(writer, collider); break;
        }
    }

    private static Collider CollisionShape(BinaryReader reader, BakeKey key)
    {
        var kind = reader.ReadByte(); var metadata = TileBinaryFields.Json<Collider>(reader);
        Collider collider = (kind, metadata) switch
        {
            (0, WallCollider or RampCollider or PostCollider or BoxCollider or SlopedWallCollider) => metadata,
            (1, HeightFieldCollider field) => field with
            {
                HeightsCm = TileBinaryFields.Channel<short>(reader, key), FootMatHalfMeter = TileBinaryFields.OptionalChannel<byte>(reader, key),
                Patches = reader.ReadBoolean() ? TileBinaryFields.MetadataRecords<FootPatch>(reader, key) : null
            },
            (2, SlabCollider slab) => slab with { Ring = TileBinaryFields.Channel<Vector2>(reader, key) },
            _ => throw new InvalidDataException("Invalid collision record kind.")
        };
        ValidateCollider(collider);
        return collider;
    }

    internal static void Collision(BinaryWriter writer, CollisionTile collision)
    {
        if (collision.Colliders.Count > MaximumCollisionShapes)
            throw new InvalidDataException($"Too many collision shapes in tile {collision.Tile}: {collision.Colliders.Count} > {MaximumCollisionShapes}.");
        writer.Write(TileCollisionSection.CollisionKind); TileBinaryFields.Key(writer, collision.Key); TileBinaryFields.Json(writer, collision.Tile); TileBinaryFields.Json(writer, collision.Bounds);
        TileBinaryFields.Records(writer, collision.Colliders, (w, collider) => CollisionShape(w, collider, collision.Key)); TileBinaryFields.Text(writer, collision.StatisticsJson);
    }

    internal static CollisionTile Collision(BinaryReader reader, BakeKey key)
    {
        var tile = TileBinaryFields.Json<TileKey>(reader); var bounds = TileBinaryFields.Json<Box3>(reader);
        var colliders = TileBinaryFields.Records(reader, key, r => CollisionShape(r, key), MaximumCollisionShapes);
        return new(key, tile, bounds, colliders, TileBinaryFields.Statistics(reader));
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

}
