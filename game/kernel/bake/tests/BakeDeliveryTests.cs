using System.Buffers.Binary;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;
using Format = Godot.RenderingServer.ArrayFormat;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeDeliveryTests
{
    [TestCase, TestCategory(TestReason.Contract), RequireGodotRuntime]
    public void CacheStatisticsCountLogicalDecodeMissAndPublicationWithoutResettingOnClear()
    {
        var root = Path.Combine(Path.GetTempPath(), "uestc-rpg-bake-statistics-" + Guid.NewGuid().ToString("N"));
        var cache = new BakeCacheStore(root);
        var key = new BakeKey(new("fixture"), "fixture.statistics", "product");
        try
        {
            Check(new BakeCacheStatistics());
            if (Read() is not null) throw new InvalidOperationException("Missing cache product was accepted.");
            Check(new(0, 1, 0));
            if (cache.MatchesInput(key, BakePayloadClass.Product, "fixture"))
                throw new InvalidOperationException("Missing cache index was accepted.");
            Check(new(0, 1, 0));
            cache.WriteParts(key, BakePayloadClass.Product, "fixture", [new byte[] { 1 }, new byte[] { 2 }]);
            Check(new(0, 1, 1));
            if (Read()?.Length != 2) throw new InvalidOperationException("Complete logical cache product was lost.");
            Check(new(1, 1, 1));
            using var cancellation = new CancellationTokenSource();
            AssertThrown(() => cache.TryReadParts(key, BakePayloadClass.Product, "fixture", parts =>
            {
                cancellation.Cancel();
                return parts.SelectMany(part => part).ToArray();
            }, cancellation.Token)).IsInstanceOf<OperationCanceledException>();
            AssertThrown(() => cache.Write(key, BakePayloadClass.Product, "fixture", [3], cancellation.Token))
                .IsInstanceOf<OperationCanceledException>();
            Check(new(1, 1, 1));
            if (cache.TryReadParts(key, BakePayloadClass.Product, "changed-input", static parts => parts[0]) is not null)
                throw new InvalidOperationException("Stale cache input was accepted.");
            Check(new(1, 2, 1));
            cache.Clear();
            Check(new(1, 2, 1));
            if (new BakeCacheStore(root).CacheStatistics != default)
                throw new InvalidOperationException("A new application store inherited the old counters.");

            byte[]? Read() => cache.TryReadParts(key, BakePayloadClass.Product, "fixture",
                static parts => parts.SelectMany(part => part).ToArray());
            void Check(BakeCacheStatistics expected)
            {
                if (cache.CacheStatistics != expected) throw new InvalidOperationException("Cache counters crossed a logical operation or lifetime boundary.");
            }
        }
        finally
        {
            cache.Stop();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory(TestTag.Determinism)]
    public void IndependentSurfacesWithinTheByteLimitShareOnePhysicalPart()
    {
        var fixture = Tile(false);
        var tile = new TileBundle(fixture.Key, fixture.Tile, fixture.Level, fixture.Bounds,
            fixture.Surfaces.Take(2).ToArray());
        var parts = Encode(tile);
        if (tile.Stats.Bytes > BakeCacheStore.MaximumTileUploadBytes || parts.Count != 1)
            throw new InvalidOperationException("Independent legal surfaces were split despite fitting the physical byte limit.");
        var restored = Decode(tile.Key, parts);
        if (restored.Stats != tile.Stats || restored.Surfaces.Count != tile.Surfaces.Count ||
            !Encode(restored)[0].SequenceEqual(parts[0]))
            throw new InvalidOperationException("Independent surfaces changed after a physical delivery round trip.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory(TestTag.Determinism)]
    public void PhysicalPartsRestoreTheEntireLogicalTileAndOriginalInstanceSlots()
    {
        var tile = Tile(true);
        var parts = Encode(tile);
        if (tile.Stats.Bytes <= BakeCacheStore.MaximumTileUploadBytes || parts.Count <= 1)
            throw new InvalidOperationException("Fixture did not exceed the single physical delivery limit.");
        foreach (var bytes in parts)
        {
            var part = (TileBundleParts.Part)TileBundleCodec.DecodePart(BakePayloadClass.Tile, "fixture/delivery", tile.Key, bytes);
            if (bytes.Length > BakeCacheStore.MaximumPayloadBytes || part.Tile.Stats.Bytes > BakeCacheStore.MaximumTileUploadBytes)
                throw new InvalidOperationException("A physical part exceeded the existing delivery limits.");
        }
        var restored = Decode(tile.Key, parts);
        if (restored.Stats != tile.Stats || restored.Tile != tile.Tile || restored.Level != tile.Level ||
            restored.Bounds != tile.Bounds || restored.StatisticsJson != tile.StatisticsJson || restored.Surfaces.Count != tile.Surfaces.Count ||
            restored.MultiMeshes.Count != tile.MultiMeshes.Count)
            throw new InvalidOperationException("Logical tile metadata, counts or instance slot numbering changed.");
        for (var slot = 0; slot < tile.MultiMeshes.Count; slot++)
            if (restored.MultiMeshes[slot].Bounds != tile.MultiMeshes[slot].Bounds ||
                !restored.MultiMeshes[slot].InstanceData.Span.SequenceEqual(tile.MultiMeshes[slot].InstanceData.Span))
                throw new InvalidOperationException("Instance slicing changed the original stream or its slot.");
        var reencoded = Encode(restored);
        if (reencoded.Count != parts.Count || reencoded.Where((bytes, index) => !bytes.SequenceEqual(parts[index])).Any())
            throw new InvalidOperationException("Physical delivery bytes changed after a logical round trip.");
        AssertThrown(() => Decode(tile.Key, parts.Skip(1).ToArray())).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => Decode(tile.Key, parts.Reverse().ToArray())).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => Decode(tile.Key, [parts[0], .. parts])).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/delivery", tile.Key, parts[0]))
            .IsInstanceOf<InvalidDataException>();
        AssertThrown(() => BakeProductCodec.DecodeParts(BakePayloadClass.Tile, "stale/delivery", tile.Key, parts))
            .IsInstanceOf<InvalidDataException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    [TestCategory("NativeRender")]
    public void MissingOrCorruptPartsDiscardTheLocalGroupAndRewritesRemoveObsoleteParts()
    {
        var root = Path.Combine(Path.GetTempPath(), "uestc-rpg-bake-delivery-" + Guid.NewGuid().ToString("N"));
        var localRoot = Path.Combine(root, "local");
        var local = new BakeCacheStore(localRoot);
        var tile = Tile(false);
        var empty = new TileBundle(tile.Key, tile.Tile, tile.Level, tile.Bounds, []);
        try
        {
            var parts = BakeProductCodec.EncodeParts(BakePayloadClass.Tile, "fixture/delivery", tile,
                bytes => local.Fits(BakePayloadClass.Tile, bytes, default));
            local.WriteParts(tile.Key, BakePayloadClass.Tile, "fixture/delivery", parts);
            local = new BakeCacheStore(localRoot);
            local.Initialize();
            var index = Directory.GetFiles(localRoot, "*.tbin", SearchOption.AllDirectories)
                .Single(path => !Path.GetDirectoryName(path)!.EndsWith(".parts", StringComparison.Ordinal));
            foreach (var path in BakeCacheIndex.Files(index))
            {
                if (new FileInfo(path).Length > BakeCacheStore.MaximumCompressedTileBytes ||
                    BakeCacheStore.DecodeFile(File.ReadAllBytes(path)).Length > BakeCacheStore.MaximumPayloadBytes)
                    throw new InvalidOperationException("Stored physical delivery exceeded a file or payload limit.");
            }
            if (Read()?.Stats != tile.Stats) throw new InvalidOperationException("A complete logical tile did not become ready.");
            File.Delete(BakeCacheIndex.Files(index).Last());
            if (!local.MatchesInput(tile.Key, BakePayloadClass.Tile, "fixture/delivery"))
                throw new InvalidOperationException("An identity probe inspected physical payload parts.");
            RequireMissing();
            local.WriteParts(tile.Key, BakePayloadClass.Tile, "fixture/delivery", parts);
            File.WriteAllBytes(BakeCacheIndex.Files(index).First(), [1, 2, 3]);
            RequireMissing();
            local.WriteParts(tile.Key, BakePayloadClass.Tile, "fixture/delivery", parts);
            local.WriteParts(tile.Key, BakePayloadClass.Tile, "fixture/delivery", Encode(empty));
            if (BakeCacheIndex.Files(index).Count() != 1 || Read()?.Stats != empty.Stats)
                throw new InvalidOperationException("A smaller replacement retained obsolete parts or old geometry.");
            RequireBytes();
            local.WriteParts(tile.Key, BakePayloadClass.Tile, "fixture/delivery", Encode(empty));
            RequireBytes();
            AssertThrown(() => local.WriteParts(tile.Key, BakePayloadClass.Tile, "fixture/replacement", [parts[0], Array.Empty<byte>()]))
                .IsInstanceOf<InvalidDataException>();
            if (File.Exists(index) || local.MatchesInput(tile.Key, BakePayloadClass.Tile, "fixture/delivery"))
                throw new InvalidOperationException("A partial replacement retained the previous published identity.");
            local.Clear();
            if (local.CacheBytes != 0) throw new InvalidOperationException("Clearing retained counted cache bytes.");
            if (Directory.GetFiles(localRoot, "*.tbin", SearchOption.AllDirectories).Length != 0 || Read() is not null)
                throw new InvalidOperationException("Clearing retained physical parts or a readable product.");
            local.Stop();
            AssertThrown(() => local.Fits(BakePayloadClass.Tile, parts[0], default)).IsInstanceOf<OperationCanceledException>();

            TileBundle? Read() => local.TryReadParts(tile.Key, BakePayloadClass.Tile, "fixture/delivery", bytes => Decode(tile.Key, bytes));
            void RequireMissing()
            {
                if (Read() is not null || File.Exists(index) || BakeCacheIndex.Files(index).Any())
                    throw new InvalidOperationException("An incomplete local group became ready or survived rejection.");
            }
            void RequireBytes()
            {
                var bytes = Directory.GetFiles(localRoot, "*.tbin", SearchOption.AllDirectories).Sum(path => new FileInfo(path).Length);
                if (local.CacheBytes != bytes)
                    throw new InvalidOperationException("A reopened cache lost its exact byte total after replacement or removal.");
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static IReadOnlyList<byte[]> Encode(TileBundle tile) =>
        BakeProductCodec.EncodeParts(BakePayloadClass.Tile, "fixture/delivery", tile, static _ => true);
    private static TileBundle Decode(BakeKey key, IReadOnlyList<byte[]> parts) =>
        (TileBundle)BakeProductCodec.DecodeParts(BakePayloadClass.Tile, "fixture/delivery", key, parts);

    private static TileBundle Tile(bool instances)
    {
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.delivery", new(-4, -3), LodLevel.L1);
        var bounds = new Box3(new(-512, 0, -384), new(-384, 64, -256));
        var material = new ToonRef(new ToonSpec(new("fixture.lit"), new("fixture.shade")));
        var vertices = GeometryVertexLimits.Maximum;
        var format = (ulong)(Format.FlagFormatVersion2 | Format.FormatVertex | Format.FormatNormal | Format.FormatTangent |
            Format.FormatColor | Format.FormatTexUV | Format.FormatIndex | Format.FlagCompressAttributes);
        byte[] indices = [0, 0, 255, 255, 1, 0];
        var a = new BakedSurface(material, format, BakedPrimitive.Triangles, vertices, 3,
            new byte[vertices * 12], new byte[vertices * 8], indices, bounds) { Reflect = true };
        var b = new BakedSurface(material, format, BakedPrimitive.Triangles, vertices, 3,
            new byte[vertices * 12], new byte[vertices * 8], indices, bounds, ignoreOcclusionCulling: true);
        var surfaces = Enumerable.Range(0, checked((int)(BakeCacheStore.MaximumTileUploadBytes / a.ByteCount + 1)))
            .Select(index => index % 2 == 0 ? a : b).ToArray();
        if (!instances) return new(key, new(-4, -3), LodLevel.L1, bounds, surfaces);
        var prototype = new BakedSurface(material, format, BakedPrimitive.Triangles, 3, 3,
            new byte[3 * 12], new byte[3 * 8], new byte[] { 0, 0, 1, 0, 2, 0 }, bounds);
        var count = BakeCacheStore.MaximumTileUploadBytes / BakedMultiMesh.InstanceStrideBytes + 1;
        var records = new byte[count * BakedMultiMesh.InstanceStrideBytes];
        for (var index = 0; index < count; index++)
            BinaryPrimitives.WriteSingleLittleEndian(records.AsSpan(index * BakedMultiMesh.InstanceStrideBytes), index);
        BakedMultiMesh[] multis = [new(prototype, count, records, bounds), new(prototype, 0, ReadOnlyMemory<byte>.Empty, bounds)];
        return new(key, new(-4, -3), LodLevel.L1, bounds, surfaces, multis,
            "{\"instanceStreams\":[{\"firstMultiMesh\":0},{\"firstMultiMesh\":1}]}");
    }
}
