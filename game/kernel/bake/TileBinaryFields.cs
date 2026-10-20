using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Existing TBIN field, record and fixed-channel framing shared by the binary sections.</summary>
internal static class TileBinaryFields
{
    internal const int MaximumItems = 4096;

    internal const int MaximumJson = 1024 * 1024;

    internal static readonly JsonSerializerOptions TileJson = CreateTileJson();

    private static JsonSerializerOptions CreateTileJson()
    {
        var options = new JsonSerializerOptions(ContractJson.Options);
        // A VariantTag may carry the exact empty season mask (default struct value).
        // Retain that bit mask rather than requiring a named enum member or changing its meaning.
        options.Converters.Insert(0, new JsonStringEnumConverter<SeasonMask>(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
        return options;
    }

    internal static void Key(BinaryWriter writer, BakeKey key) { Text(writer, key.Map.Value); Text(writer, key.Module); Text(writer, key.Detail); }

    internal static BakeKey Key(BinaryReader reader) => new(new(Text(reader, 128)), Text(reader, 256), Text(reader, 1024));

    internal static void Text(BinaryWriter writer, string text) => Bytes(writer, Encoding.UTF8.GetBytes(text));

    internal static string Text(BinaryReader reader, int maximum = MaximumJson) => new UTF8Encoding(false, true).GetString(Bytes(reader, maximum));

    internal static void Json<T>(BinaryWriter writer, T value, int maximum = MaximumJson)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, TileJson);
        if (bytes.Length > maximum) throw new InvalidDataException($"Tile JSON section exceeds its limit: {bytes.Length} > {maximum}.");
        Bytes(writer, bytes);
    }

    internal static T Json<T>(BinaryReader reader, int maximum = MaximumJson) => JsonSerializer.Deserialize<T>(Bytes(reader, maximum), TileJson)
        ?? throw new InvalidDataException($"Missing {typeof(T).Name} data.");

    internal static void Bytes(BinaryWriter writer, ReadOnlyMemory<byte> bytes) { writer.Write(bytes.Length); writer.Write(bytes.Span); }

    internal static byte[] Bytes(BinaryReader reader, int maximum = BakeCacheStore.MaximumPayloadBytes)
    {
        var length = reader.ReadInt32();
        if (length < 0 || length > maximum || length > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new InvalidDataException("Tile section length exceeds the remaining payload or its limit.");
        return reader.ReadBytes(length);
    }

    internal static int Count(BinaryReader reader, int maximum = MaximumItems)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > maximum || count > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new InvalidDataException("Tile collection count exceeds limit.");
        return count;
    }

    internal static string Statistics(BinaryReader reader)
    {
        var text = Text(reader, 65536);
        using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 16 });
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Statistics must be a JSON object.");
        return text;
    }

    internal static void Channel<T>(BinaryWriter writer, ReadOnlySpan<T> values, BakeKey key) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(values);
        var maximum = BakeCacheStore.MaximumProductBytes;
        if (bytes.Length > maximum) throw new InvalidDataException($"CPU channel {key.Map.Value}/{key.Module}/{key.Detail}: {bytes.Length} bytes exceeds limit {maximum}.");
        writer.Write(values.Length);
        writer.Write(bytes);
    }

    internal static T[] Channel<T>(BinaryReader reader, BakeKey key) where T : unmanaged
    {
        var count = reader.ReadInt32();
        var bytes = (long)count * Unsafe.SizeOf<T>();
        var maximum = BakeCacheStore.MaximumProductBytes;
        var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (count < 0 || bytes > maximum || bytes > remaining)
            throw new InvalidDataException($"CPU channel {key.Map.Value}/{key.Module}/{key.Detail}: {bytes} bytes, remaining={remaining}, limit={maximum}.");
        var values = new T[count];
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(values.AsSpan()));
        return values;
    }

    internal static void OptionalChannel<T>(BinaryWriter writer, ReadOnlyMemory<T>? values, BakeKey key) where T : unmanaged
    {
        writer.Write(values.HasValue);
        if (values is { } memory) Channel(writer, memory.Span, key);
    }

    internal static T[]? OptionalChannel<T>(BinaryReader reader, BakeKey key) where T : unmanaged => reader.ReadBoolean() ? Channel<T>(reader, key) : null;

    internal static void Box(BinaryWriter writer, Box3 bounds)
    {
        writer.Write(bounds.Min.X); writer.Write(bounds.Min.Y); writer.Write(bounds.Min.Z);
        writer.Write(bounds.Max.X); writer.Write(bounds.Max.Y); writer.Write(bounds.Max.Z);
    }

    internal static Box3 Box(BinaryReader reader) => new(
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));

    internal static void Records<T>(BinaryWriter writer, IReadOnlyList<T> values, Action<BinaryWriter, T> write)
    {
        writer.Write(values.Count);
        foreach (var value in values) write(writer, value);
    }

    internal static T[] Records<T>(BinaryReader reader, BakeKey key, Func<BinaryReader, T> read, int maximum = int.MaxValue)
    {
        var values = new T[Count(reader, Math.Min(maximum, BakeCacheStore.MaximumProductBytes))];
        for (var i = 0; i < values.Length; i++) values[i] = read(reader);
        return values;
    }

    internal static void MetadataRecords<T>(BinaryWriter writer, IReadOnlyList<T> values) => Records(writer, values, (w, v) => Json(w, v));

    internal static T[] MetadataRecords<T>(BinaryReader reader, BakeKey key) => Records(reader, key, r => Json<T>(r));

    internal static void Rings(BinaryWriter writer, PolygonRings rings, BakeKey key)
    {
        Channel<Vector2>(writer, rings.Outer.ToArray(), key);
        Records(writer, rings.Holes, (w, hole) => Channel<Vector2>(w, hole.ToArray(), key));
    }

    internal static PolygonRings Rings(BinaryReader reader, BakeKey key) => new(Channel<Vector2>(reader, key),
        Records<IReadOnlyList<Vector2>>(reader, key, r => Channel<Vector2>(r, key)));

    internal static void RequireTile(BakeKey key, TileKey tile, params string[] modules)
    {
        var request = BakeRequestKeys.Parse(key);
        if (!modules.Contains(key.Module, StringComparer.Ordinal) || request.Collision || request.Level != LodLevel.L0 || request.Tile != tile)
            throw new InvalidDataException("Auxiliary payload key, kind and owner tile disagree.");
    }

}
