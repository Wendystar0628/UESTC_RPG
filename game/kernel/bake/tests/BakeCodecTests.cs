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
    [TestCategory(TestReason.Contract)]
    public void CollisionRoundTripKeepsLocalCoordinatesAndFootMaterial()
    {
        var key = BakeRequestKeys.Collision(new("fixture"), "collision.world", new(-1, 2));
        var box = new Box3(new(-64, -2, 128), new(0, 20, 192));
        var original = new CollisionTile(key, new(-1, 2), box,
            [new WallCollider(new(1, 2), new(3, 4), .1f, 0, 3, SurfaceTag.Wall) { Foot = FootMat.Metal },
                new HeightFieldCollider(default, Enumerable.Range(0, 65 * 65).Select(i => (short)(i - 2000)).ToArray(), SurfaceTag.Ground)
                { FootMatHalfMeter = new byte[128 * 128], Patches = [new(new(1, 2), new(3, 4), .5f, FootMat.Metal, 321)] }]);
        var bytes = TileBundleCodec.Encode("fixture/identity", original);
        var decoded = (CollisionTile)TileBundleCodec.Decode("fixture/identity", key, bytes);
        var wall = (WallCollider)decoded.Colliders[0];
        var expectedField = (HeightFieldCollider)original.Colliders[1]; var field = (HeightFieldCollider)decoded.Colliders[1];
        if (!field.HeightsCm.SequenceEqual(expectedField.HeightsCm) || !field.FootMatHalfMeter!.SequenceEqual(expectedField.FootMatHalfMeter!) ||
            !field.Patches!.SequenceEqual(expectedField.Patches!)) throw new InvalidOperationException("Heightfield channels lost their exact values.");
        if (wall.A != new Vector2(1, 2) || wall.B != new Vector2(3, 4) || decoded.Bounds != box || wall.Foot != FootMat.Metal)
            throw new InvalidOperationException("Collision codec changed local parameters or bounds.");
        if (!bytes.SequenceEqual(TileBundleCodec.Encode("fixture/identity", decoded)))
            throw new InvalidOperationException("Canonical codec bytes changed after a round trip.");
        var floors = Enumerable.Range(0, 2048).Select(index => (Collider)new SlabCollider(
            Enumerable.Range(0, 64).Select(vertex => new Vector2(index % 64 + .5f, index / 64 + .5f)
                + .4f * new Vector2(MathF.Cos(vertex * float.Tau / 64), MathF.Sin(vertex * float.Tau / 64))).ToArray(),
            .1f, .2f, SurfaceTag.Ground) { Foot = FootMat.StonePaving }).ToArray();
        var dense = new CollisionTile(key, original.Tile, box, floors);
        var denseBytes = TileBundleCodec.Encode("fixture/dense", dense);
        if (denseBytes.Length <= 1024 * 1024)
            throw new InvalidOperationException("The collision payload must exercise data larger than the metadata envelope.");
        var denseDecoded = (CollisionTile)TileBundleCodec.Decode("fixture/dense", key, denseBytes);
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
        var bytes = TileBundleCodec.Encode("fixture/identity", new TileBundle(key, new(0, 0), LodLevel.L0,
            new Box3(Vector3.Zero, new(64, 64, 64)), []));
        for (var i = 0; i < bytes.Length; i++) {
            var rejected = false;
            try { TileBundleCodec.Decode("fixture/identity", key, bytes[..i]); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        {
            var rejected = false;
            try { TileBundleCodec.Decode("other", key, bytes); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        {
            var rejected = false;
            try { TileBundleCodec.Decode("fixture/identity", key with { Module = "other" }, bytes); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        {
            var rejected = false;
            try { TileBundleCodec.Decode("fixture/identity", key, [.. bytes, 0]); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Malformed bake data was accepted.").IsTrue();
        }
        var bounds = new Box3(Vector3.Zero, new(64, 64, 64));
        var oversizedSurface = new BakedSurface(new(new(new("fixture.lit"), new("fixture.shade"))),
            0, BakedPrimitive.Triangles, 3, 3, new byte[BakeCacheStore.MaximumTileUploadBytes + 1],
            ReadOnlyMemory<byte>.Empty, new byte[] { 0, 0, 1, 0, 2, 0 }, bounds);
        AssertThrown(() => TileBundleCodec.Encode("fixture/identity", new TileBundle(key, default, LodLevel.L0, bounds, [oversizedSurface])))
            .IsInstanceOf<InvalidDataException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OversizedSectionIsRejectedBeforeAllocation()
    {
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var bytes = TileBundleCodec.Encode("fixture/identity", new TileBundle(key, new(0, 0), LodLevel.L0,
            new Box3(Vector3.Zero, new(64, 64, 64)), []));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), int.MaxValue);
        {
            var rejected = false;
            try { TileBundleCodec.Decode("fixture/identity", key, bytes); }
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
    public void UnloadingAnInFlightRequestKeepsItsOldHandleFailed()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var queue = new BakeQueue(1, static () => { });
        try
        {
            var handle = queue.Ensure(key, BakePriority.KnownDestination, () =>
            {
                started.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker release timed out.");
                return new object();
            });
            if (!started.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker did not start.");
            queue.ForgetMap(key.Map);
            release.Set();
            queue.Dispose();
            if (handle.State != BakeState.Failed || handle.Product is not null || string.IsNullOrWhiteSpace(handle.Error))
                throw new InvalidOperationException("An unloaded map's in-flight request became a ready product.");
            if (queue.Progress.Total != 0 || queue.Progress.Done != 0)
                throw new InvalidOperationException("An unloaded map left an unfinished request in progress.");
        }
        finally { release.Set(); queue.Dispose(); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void StoppingRejectsPendingWorkAndLateProductsWhileDisposeStillDrainsWorkers()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var queue = new BakeQueue(1, static () => { });
        var finished = false;
        var pendingRan = false;
        try
        {
            var running = queue.Ensure(key, BakePriority.Normal, () =>
            {
                started.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker release timed out.");
                finished = true;
                return new object();
            });
            if (!started.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture worker did not start.");
            var pending = queue.Ensure(key with { Detail = "pending" }, BakePriority.Background, () =>
            {
                pendingRan = true;
                return new object();
            });
            queue.Stop(); // Must return while generation is still paused.
            AssertThat(running.State == BakeState.Failed && pending.State == BakeState.Failed && !finished)
                .OverrideFailureMessage("Stopping waited for generation or left publishable work.").IsTrue();
            AssertThrown(() => queue.Ensure(key, BakePriority.Normal, () => new object()))
                .IsInstanceOf<ObjectDisposedException>();
            release.Set();
            queue.Dispose(); // Even after Stop, ordinary disposal must join the remaining CPU worker.
            AssertThat(finished && !pendingRan && running.Product is null && pending.Product is null && queue.Failures.IsEmpty)
                .OverrideFailureMessage("A stopped job ran or published, or disposal failed to drain its worker.").IsTrue();
        }
        finally { release.Set(); queue.Dispose(); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public void LayeredCacheDecodesCandidatesAndOnlyChangesLocalProducts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "uestc-rpg-bake-cache-" + Guid.NewGuid().ToString("N"));
        var localRoot = Path.Combine(directory, "local");
        var packagedRoot = Path.Combine(directory, "packaged");
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var writer = new BakeCacheStore(localRoot, "version-one", packagedRoot);
        var package = new BakeCacheStore(packagedRoot, "version-one");
        var original = new TileBundle(key, default, LodLevel.L0, new(Vector3.Zero, new(64, 64, 64)), []);
        var local = new TileBundle(key, default, LodLevel.L0, new(Vector3.Zero, new(64, 80, 64)), []);
        TileBundle Decode(byte[] bytes) => (TileBundle)TileBundleCodec.Decode("fixture/identity", key, bytes);
        try
        {
            package.Write(key, BakePayloadClass.Tile, TileBundleCodec.Encode("fixture/identity", original));
            var packagedPath = Directory.GetFiles(packagedRoot, "*.tbin", SearchOption.AllDirectories).Single();
            var packagedBytes = File.ReadAllBytes(packagedPath);
            writer.Write(key, BakePayloadClass.Tile, TileBundleCodec.Encode("fixture/identity", local));
            if (writer.TryRead(key, BakePayloadClass.Tile, Decode)?.Bounds != local.Bounds)
                throw new InvalidOperationException("A packaged product hid a valid local override.");
            var localPath = Directory.GetFiles(localRoot, "*.tbin", SearchOption.AllDirectories).Single();
            File.WriteAllBytes(localPath, [1, 2, 3]);
            RequirePackaged();
            foreach (var invalid in new[] { TileBundleCodec.Encode("stale/identity", local), new byte[] { 1, 2, 3 } })
            {
                writer.Write(key, BakePayloadClass.Tile, invalid);
                RequirePackaged();
            }
            writer.Write(key, BakePayloadClass.Tile, TileBundleCodec.Encode("fixture/identity", local));
            writer.Clear();
            RequirePackaged();
            writer.Write(key, BakePayloadClass.Tile, TileBundleCodec.Encode("fixture/identity", local));
            var updated = new BakeCacheStore(localRoot, "version-two", packagedRoot);
            updated.Initialize();
            if (updated.CacheBytes != 0 || updated.TryRead(key, BakePayloadClass.Tile, Decode) is not null ||
                !File.ReadAllBytes(packagedPath).SequenceEqual(packagedBytes))
                throw new InvalidOperationException("Version migration reused incompatible products or changed the installation.");
            File.WriteAllBytes(packagedPath, [1, 2, 3]);
            var corrupt = new BakeCacheStore(localRoot, "version-one", packagedRoot);
            if (corrupt.TryRead(key, BakePayloadClass.Tile, Decode) is not null || !File.ReadAllBytes(packagedPath).SequenceEqual(new byte[] { 1, 2, 3 }))
                throw new InvalidOperationException("A corrupt packaged product was accepted or removed.");
            corrupt.Write(key, BakePayloadClass.Tile, TileBundleCodec.Encode("fixture/identity", local));
            if (corrupt.TryRead(key, BakePayloadClass.Tile, Decode)?.Bounds != local.Bounds)
                throw new InvalidOperationException("A missing packaged product could not be replaced locally.");
            var cpuKey = key with { Module = BakeModuleIds.VegetationTrees };
            var cpuBytes = new byte[BakeCacheStore.MaximumPayloadBytes + 64];
            var rng = new Rng(76);
            for (var offset = 0; offset < cpuBytes.Length; offset += sizeof(int))
                BinaryPrimitives.WriteInt32LittleEndian(cpuBytes.AsSpan(offset), rng.NextInt(int.MaxValue));
            package.Write(cpuKey, BakePayloadClass.Product, cpuBytes);
            var cpuPath = Directory.GetFiles(packagedRoot, "*.tbin", SearchOption.AllDirectories).Single(path => path != packagedPath);
            var cpuFile = File.ReadAllBytes(cpuPath);
            if (cpuFile.Length <= BakeCacheStore.MaximumCompressedTileBytes ||
                writer.TryRead(cpuKey, BakePayloadClass.Product, value => value) is not { } restoredCpu || !restoredCpu.SequenceEqual(cpuBytes))
                throw new InvalidOperationException("A CPU cache product retained the compressed or decoded render-tile limit.");
            writer.Write(cpuKey, BakePayloadClass.Product, [1, 2, 3]);
            var fallbackCpu = writer.TryRead(cpuKey, BakePayloadClass.Product, value => value.Length == cpuBytes.Length ? value :
                throw new InvalidDataException("Malformed local CPU fixture."));
            if (fallbackCpu is null || !fallbackCpu.SequenceEqual(cpuBytes) || !File.ReadAllBytes(cpuPath).SequenceEqual(cpuFile))
                throw new InvalidOperationException("CPU candidate validation hid or modified the packaged product.");
            AssertThrown(() => writer.Write(key, BakePayloadClass.Tile, cpuBytes)).IsInstanceOf<InvalidDataException>();
            writer.Stop();
            AssertThrown(() => writer.Write(key, BakePayloadClass.Tile, [1])).IsInstanceOf<OperationCanceledException>();
            AssertThrown(() => writer.TryRead(key, BakePayloadClass.Tile, Decode)).IsInstanceOf<OperationCanceledException>();
            void RequirePackaged()
            {
                if (writer.TryRead(key, BakePayloadClass.Tile, Decode)?.Bounds != original.Bounds || File.Exists(localPath) ||
                    !File.ReadAllBytes(packagedPath).SequenceEqual(packagedBytes))
                    throw new InvalidOperationException("A bad local candidate hid the package or caused a packaged write.");
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

}
