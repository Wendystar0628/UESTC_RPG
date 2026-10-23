using Campus.Tests;
using static GdUnit4.Assertions;
using System.Buffers.Binary;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
using GdUnit4;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeCodecTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void PublishedIdentityProbeRequiresCurrentIndexAndDoesNotReadParts()
    {
        var root = Path.Combine(Path.GetTempPath(), "uestc-rpg-identity-" + Guid.NewGuid().ToString("N"));
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", default, LodLevel.L0);
        var store = new BakeCacheStore(root);
        try
        {
            store.Initialize();
            var index = Path.Combine(root, key.Map.Value, key.Module, key.Detail + ".tbin");
            Directory.CreateDirectory(Path.GetDirectoryName(index)!);
            var valid = BakeCacheIndex.Encode(BakePayloadClass.Tile, 1, "fixture/current");
            File.WriteAllBytes(index, valid);
            if (!store.MatchesInput(key, BakePayloadClass.Tile, "fixture/current"))
                throw new InvalidOperationException("An index-only hit tried to require an absent payload.");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            AssertThrown(() => store.MatchesInput(key, BakePayloadClass.Tile, "fixture/current", cancellation.Token))
                .IsInstanceOf<OperationCanceledException>();
            if (store.TryRead(key, BakePayloadClass.Tile, "fixture/current", bytes => bytes) is not null || File.Exists(index))
                throw new InvalidOperationException("Formal reading accepted an index without payload parts.");
            var wrongClass = valid.ToArray(); wrongClass[4] = (byte)BakePayloadClass.Product;
            var wrongCount = valid.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(wrongCount.AsSpan(5), 0);
            var wrongLength = valid.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(wrongLength.AsSpan(9), 0);
            var invalidUtf8 = valid.ToArray(); invalidUtf8[^1] = 255;
            foreach (var bytes in new[] { valid[..8], valid[..^1], valid.Concat(new byte[] { 0 }).ToArray(),
                wrongClass, wrongCount, wrongLength, invalidUtf8,
                BakeCacheIndex.Encode(BakePayloadClass.Tile, 1, "fixture/stale") })
            {
                File.WriteAllBytes(index, bytes);
                if (store.MatchesInput(key, BakePayloadClass.Tile, "fixture/current") || File.Exists(index))
                    throw new InvalidOperationException("A stale or invalid published identity survived rejection.");
            }
            AssertThrown(() => BakeCacheIndex.Encode(BakePayloadClass.Tile, 1, ""))
                .IsInstanceOf<InvalidDataException>();
            store.Stop();
            AssertThrown(() => store.MatchesInput(key, BakePayloadClass.Tile, "fixture/current"))
                .IsInstanceOf<OperationCanceledException>();
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CollisionRoundTripKeepsLocalCoordinatesAndFootMaterial()
    {
        var key = BakeRequestKeys.Collision(new("fixture"), "collision.world", new(-1, 2));
        var box = new Box3(new(-64, -2, 128), new(0, 20, 192));
        var original = new CollisionTile(key, new(-1, 2), box,
            [new WallCollider(new(1, 2), new(3, 4), .1f, 0, 3, SurfaceTag.Wall) { Foot = FootMat.Metal },
                new HeightFieldCollider(default, Enumerable.Range(0, 65 * 65).Select(i => (short)(i - 2000)).ToArray(), SurfaceTag.Ground)
                { FootMatHalfMeter = new byte[128 * 128], Patches = [new(new(1, 2), new(3, 4), .5f, FootMat.Metal, 321)] }]);
        var bytes = TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/identity", original);
        var decoded = (CollisionTile)TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/identity", key, bytes);
        var exposed = (HeightFieldCollider)decoded.Colliders[1];
        exposed.HeightsCm[0] = short.MaxValue;
        exposed.FootMatHalfMeter![0] = byte.MaxValue;
        if (((HeightFieldCollider)decoded.Colliders[1]).HeightsCm[0] == short.MaxValue ||
            ((HeightFieldCollider)decoded.Colliders[1]).FootMatHalfMeter![0] == byte.MaxValue)
            throw new InvalidOperationException("Owned collision decoding weakened defensive getter snapshots.");
        var wall = (WallCollider)decoded.Colliders[0];
        var expectedField = (HeightFieldCollider)original.Colliders[1]; var field = (HeightFieldCollider)decoded.Colliders[1];
        if (!field.HeightsCm.SequenceEqual(expectedField.HeightsCm) || !field.FootMatHalfMeter!.SequenceEqual(expectedField.FootMatHalfMeter!) ||
            !field.Patches!.SequenceEqual(expectedField.Patches!)) throw new InvalidOperationException("Heightfield channels lost their exact values.");
        if (wall.A != new Vector2(1, 2) || wall.B != new Vector2(3, 4) || decoded.Bounds != box || wall.Foot != FootMat.Metal)
            throw new InvalidOperationException("Collision codec changed local parameters or bounds.");
        if (!bytes.SequenceEqual(TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/identity", decoded)))
            throw new InvalidOperationException("Canonical codec bytes changed after a round trip.");
        var floors = Enumerable.Range(0, 2048).Select(index => (Collider)new SlabCollider(
            Enumerable.Range(0, 64).Select(vertex => new Vector2(index % 64 + .5f, index / 64 + .5f)
                + .4f * new Vector2(MathF.Cos(vertex * float.Tau / 64), MathF.Sin(vertex * float.Tau / 64))).ToArray(),
            .1f, .2f, SurfaceTag.Ground) { Foot = FootMat.StonePaving }).ToArray();
        var dense = new CollisionTile(key, original.Tile, box, floors);
        var denseBytes = TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/dense", dense);
        if (denseBytes.Length <= 1024 * 1024)
            throw new InvalidOperationException("The collision payload must exercise data larger than the metadata envelope.");
        var denseDecoded = (CollisionTile)TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/dense", key, denseBytes);
        if (denseDecoded.Colliders.Count != floors.Length || denseDecoded.Colliders.Zip(floors).Any(pair =>
            pair.First is not SlabCollider a || pair.Second is not SlabCollider b || !a.Ring.SequenceEqual(b.Ring)
            || a.Y != b.Y || a.Thick != b.Thick || a.Foot != b.Foot))
            throw new InvalidOperationException("A legal collision payload lost shapes or parameters during its round trip.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EveryTruncatedPayloadAndWrongIdentityIsRejected()
    {
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var bytes = TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/identity", new TileBundle(key, new(0, 0), LodLevel.L0,
            new Box3(Vector3.Zero, new(64, 64, 64)), []));
        for (var i = 0; i < bytes.Length; i++) {
            var rejected = false;
            try { TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/identity", key, bytes[..i]); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        {
            var rejected = false;
            try { TileBundleCodec.Decode(BakePayloadClass.Tile, "other", key, bytes); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        {
            var rejected = false;
            try { TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/identity", key with { Module = "other" }, bytes); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        {
            var rejected = false;
            try { TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/identity", key, [.. bytes, 0]); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        var bounds = new Box3(Vector3.Zero, new(64, 64, 64));
        var oversizedSurface = new BakedSurface(new(new(new("fixture.lit"), new("fixture.shade"))),
            0, BakedPrimitive.Triangles, 3, 3, new byte[BakeCacheStore.MaximumTileUploadBytes + 1],
            ReadOnlyMemory<byte>.Empty, new byte[] { 0, 0, 1, 0, 2, 0 }, bounds);
        AssertThrown(() => TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/identity", new TileBundle(key, default, LodLevel.L0, bounds, [oversizedSurface])))
            .IsInstanceOf<InvalidDataException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OversizedSectionIsRejectedBeforeAllocation()
    {
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var bytes = TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/identity", new TileBundle(key, new(0, 0), LodLevel.L0,
            new Box3(Vector3.Zero, new(64, 64, 64)), []));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), int.MaxValue);
        {
            var rejected = false;
            try { TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/identity", key, bytes); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CompressedHeaderRejectsBombBeforeCallingEngine()
    {
        var bytes = new byte[13];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0x315A4254);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), int.MaxValue);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), 1);
        {
            var rejected = false;
            try { BakeCacheStore.DecodeFile(bytes); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        foreach (var module in new[] { BakeModuleIds.VegetationTrees, BakeModuleIds.VegetationGrass, BakeModuleIds.FaunaRuntime,
            BakeModuleIds.NightlightLamps, BakeModuleIds.NightlightWindows })
        {
            var key = BakeRequestKeys.Tile(new("fixture"), module, default, LodLevel.L0);
            AssertThrown(() => BakeCacheStore.DecodeFile(bytes, BakeCacheStore.MaximumPayload(BakePayloadClass.Tile), key))
                .IsInstanceOf<InvalidDataException>();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    [TestCategory("NativeRender")]
    public void LocalCacheRejectsDamageAndInvalidatesOnlyForAbiChanges()
    {
        var directory = Path.Combine(Path.GetTempPath(), "uestc-rpg-bake-cache-" + Guid.NewGuid().ToString("N"));
        var localRoot = Path.Combine(directory, "local");
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var writer = new BakeCacheStore(localRoot);
        var local = new TileBundle(key, default, LodLevel.L0, new(Vector3.Zero, new(64, 80, 64)), []);
        TileBundle Decode(byte[] bytes) => (TileBundle)TileBundleCodec.Decode(BakePayloadClass.Tile, "fixture/identity", key, bytes);
        try
        {
            writer.Write(key, BakePayloadClass.Tile, "fixture/identity", TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/identity", local));
            if (writer.TryRead(key, BakePayloadClass.Tile, "fixture/identity", Decode)?.Bounds != local.Bounds)
                throw new InvalidOperationException("A valid local product did not round trip.");
            var localPath = CacheIndexes(localRoot).Single();
            File.WriteAllBytes(localPath, [1, 2, 3]);
            RequireMissing();
            foreach (var invalid in new[] { TileBundleCodec.Encode(BakePayloadClass.Tile, "stale/identity", local), new byte[] { 1, 2, 3 } })
            {
                writer.Write(key, BakePayloadClass.Tile, "fixture/identity", invalid);
                RequireMissing();
            }
            writer.Write(key, BakePayloadClass.Tile, "fixture/identity", TileBundleCodec.Encode(BakePayloadClass.Tile, "fixture/identity", local));
            if (writer.RebuildFailures(key).Count != 0 || writer.TryRead(key, BakePayloadClass.Tile, "fixture/identity", Decode)?.Bounds != local.Bounds)
                throw new InvalidOperationException("A valid replacement retained a rejected product's failure.");
            var reopened = new BakeCacheStore(localRoot);
            if (reopened.TryRead(key, BakePayloadClass.Tile, "fixture/identity", Decode)?.Bounds != local.Bounds)
                throw new InvalidOperationException("Reopening the current ABI discarded a valid product.");
            File.WriteAllText(Path.Combine(localRoot, "cache.version"), "obsolete-abi");
            var updated = new BakeCacheStore(localRoot);
            updated.Initialize();
            if (updated.CacheBytes != 0 || updated.TryRead(key, BakePayloadClass.Tile, "fixture/identity", Decode) is not null ||
                File.ReadAllText(Path.Combine(localRoot, "cache.version")) != BakeOptions.Abi)
                throw new InvalidOperationException("An ABI change retained cached products or the old marker.");
            writer = updated;
            var cpuKey = key with { Module = BakeModuleIds.VegetationTrees };
            var cpuBytes = new byte[BakeCacheStore.MaximumPayloadBytes + 64];
            var rng = new Rng(76);
            for (var offset = 0; offset < cpuBytes.Length; offset += sizeof(int))
                BinaryPrimitives.WriteInt32LittleEndian(cpuBytes.AsSpan(offset), rng.NextInt(int.MaxValue));
            writer.Write(cpuKey, BakePayloadClass.Product, "fixture/identity", cpuBytes);
            var cpuIndex = CacheIndexes(localRoot).Single();
            var cpuPath = BakeCacheIndex.Files(cpuIndex).Single();
            var cpuFile = File.ReadAllBytes(cpuPath);
            if (cpuFile.Length <= BakeCacheStore.MaximumCompressedTileBytes ||
                writer.TryRead(cpuKey, BakePayloadClass.Product, "fixture/identity", value => value) is not { } restoredCpu || !restoredCpu.SequenceEqual(cpuBytes))
                throw new InvalidOperationException("A CPU cache product retained the compressed or decoded render-tile limit.");
            writer.Write(cpuKey, BakePayloadClass.Product, "fixture/identity", [1, 2, 3]);
            var malformedCpu = writer.TryRead(cpuKey, BakePayloadClass.Product, "fixture/identity", value => value.Length == cpuBytes.Length ? value :
                throw new InvalidDataException("Malformed local CPU fixture."));
            if (malformedCpu is not null || File.Exists(cpuIndex) || File.Exists(cpuPath) || writer.RebuildFailures(cpuKey).Count != 1)
                throw new InvalidOperationException("A malformed CPU product survived local cache rejection.");
            AssertThrown(() => writer.Write(key, BakePayloadClass.Tile, "fixture/identity", cpuBytes)).IsInstanceOf<InvalidDataException>();
            writer.Clear();
            if (writer.CacheBytes != 0 || CacheIndexes(localRoot).Any())
                throw new InvalidOperationException("Clearing retained local cache products.");
            writer.Stop();
            AssertThrown(() => writer.Write(key, BakePayloadClass.Tile, "fixture/identity", [1])).IsInstanceOf<OperationCanceledException>();
            AssertThrown(() => writer.TryRead(key, BakePayloadClass.Tile, "fixture/identity", Decode)).IsInstanceOf<OperationCanceledException>();
            void RequireMissing()
            {
                if (writer.TryRead(key, BakePayloadClass.Tile, "fixture/identity", Decode) is not null || File.Exists(localPath) ||
                    writer.RebuildFailures(key).Count != 1)
                    throw new InvalidOperationException("A damaged local product survived rejection or lost its cause.");
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NegativeTileParentsUseFloorDivision()
    {
        if (BakeRequestKeys.Parent(new(-1, -3), LodLevel.L1) != new TileKey(-1, -2))
            throw new InvalidOperationException("Negative grid coordinates were truncated toward zero.");
        var key = BakeRequestKeys.Tile(new("fixture"), "module", new(-1, -2), LodLevel.L2);
        var parsed = BakeRequestKeys.Parse(key);
        if (parsed.Tile != new TileKey(-1, -2) || parsed.Level != LodLevel.L2 || parsed.Collision)
            throw new InvalidOperationException("Tile request identity did not round trip.");
    }

    private static IEnumerable<string> CacheIndexes(string root) => Directory.GetFiles(root, "*.tbin", SearchOption.AllDirectories)
        .Where(path => !Path.GetDirectoryName(path)!.EndsWith(".parts", StringComparison.Ordinal));
}
