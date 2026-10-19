using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

public static partial class TileBundleCodec
{
    // Collections are framed record by record; only each record's small metadata may be JSON.
    private static void Records<T>(BinaryWriter writer, IReadOnlyList<T> values, Action<BinaryWriter, T> write)
    {
        writer.Write(values.Count);
        foreach (var value in values) write(writer, value);
    }

    private static T[] Records<T>(BinaryReader reader, BakeKey key, Func<BinaryReader, T> read, int maximum = int.MaxValue)
    {
        var values = new T[Count(reader, Math.Min(maximum, BakeCacheStore.MaximumPayload(key)))];
        for (var i = 0; i < values.Length; i++) values[i] = read(reader);
        return values;
    }

    private static void MetadataRecords<T>(BinaryWriter writer, IReadOnlyList<T> values) => Records(writer, values, (w, v) => Json(w, v));
    private static T[] MetadataRecords<T>(BinaryReader reader, BakeKey key) => Records(reader, key, r => Json<T>(r));

    private static void Rings(BinaryWriter writer, PolygonRings rings, BakeKey key)
    {
        Channel<Vector2>(writer, rings.Outer.ToArray(), key);
        Records(writer, rings.Holes, (w, hole) => Channel<Vector2>(w, hole.ToArray(), key));
    }

    private static PolygonRings Rings(BinaryReader reader, BakeKey key) => new(Channel<Vector2>(reader, key),
        Records<IReadOnlyList<Vector2>>(reader, key, r => Channel<Vector2>(r, key)));

    private static void Facade(BinaryWriter writer, WindowFacadeDecl value, BakeKey key)
    {
        Json(writer, value with { SillY = [], Skip = [] });
        Channel<float>(writer, value.SillY.ToArray(), key); Channel<int>(writer, value.Skip.ToArray(), key);
    }

    private static WindowFacadeDecl Facade(BinaryReader reader, BakeKey key) => Json<WindowFacadeDecl>(reader) with
    { SillY = Channel<float>(reader, key), Skip = Channel<int>(reader, key) };

    private static void Windows(BinaryWriter writer, WindowMetadata windows, BakeKey key)
    {
        Records(writer, windows.Facades, (w, value) => Facade(w, value, key));
        MetadataRecords(writer, windows.Windows);
    }

    private static WindowMetadata Windows(BinaryReader reader, BakeKey key) => new(
        Records(reader, key, r => Facade(r, key)), MetadataRecords<WindowDecl>(reader, key));
}
