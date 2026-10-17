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
    public void CollisionRoundTripKeepsLocalCoordinatesAndFootMaterial()
    {
        var key = BakeRequestKeys.Collision(new("fixture"), "collision.world", new(-1, 2));
        var box = new Box3(new(-64, -2, 128), new(0, 20, 192));
        var original = new CollisionTile(key, new(-1, 2), box,
            [new WallCollider(new(1, 2), new(3, 4), .1f, 0, 3, SurfaceTag.Wall) { Foot = FootMat.Metal }]);
        var bytes = TileBundleCodec.Encode("fixture/identity", original);
        var decoded = (CollisionTile)TileBundleCodec.Decode("fixture/identity", key, bytes);
        var wall = (WallCollider)decoded.Colliders.Single();
        if (wall.A != new Vector2(1, 2) || wall.B != new Vector2(3, 4) || decoded.Bounds != box || wall.Foot != FootMat.Metal)
            throw new InvalidOperationException("Collision codec changed local parameters or bounds.");
        if (!bytes.SequenceEqual(TileBundleCodec.Encode("fixture/identity", decoded)))
            throw new InvalidOperationException("Canonical codec bytes changed after a round trip.");
        var floors = Enumerable.Range(0, 2048).Select(index => (Collider)new SlabCollider(
            Enumerable.Range(0, 32).Select(vertex => new Vector2(index % 64 + .5f, index / 64 + .5f)
                + .4f * new Vector2(MathF.Cos(vertex * float.Tau / 32), MathF.Sin(vertex * float.Tau / 32))).ToArray(),
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
    public void EveryTruncatedPayloadAndWrongIdentityIsRejected()
    {
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var bytes = TileBundleCodec.Encode("fixture/identity", new TileBundle(key, new(0, 0), LodLevel.L0,
            new Box3(Vector3.Zero, new(64, 64, 64)), []));
        for (var i = 0; i < bytes.Length; i++) Reject(() => TileBundleCodec.Decode("fixture/identity", key, bytes[..i]));
        Reject(() => TileBundleCodec.Decode("other", key, bytes));
        Reject(() => TileBundleCodec.Decode("fixture/identity", key with { Module = "other" }, bytes));
        Reject(() => TileBundleCodec.Decode("fixture/identity", key, [.. bytes, 0]));
    }

    [TestCase]
    public void OversizedSectionIsRejectedBeforeAllocation()
    {
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var bytes = TileBundleCodec.Encode("fixture/identity", new TileBundle(key, new(0, 0), LodLevel.L0,
            new Box3(Vector3.Zero, new(64, 64, 64)), []));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), int.MaxValue);
        Reject(() => TileBundleCodec.Decode("fixture/identity", key, bytes));
    }

    [TestCase]
    public void CompressedHeaderRejectsBombBeforeCallingEngine()
    {
        var bytes = new byte[13];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0x315A4254);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), int.MaxValue);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), 1);
        Reject(() => BakeCacheStore.DecodeFile(bytes));
    }

    [TestCase]
    public void ReferenceHashDoesNotTrustAnUnlistedReleaseKey()
    {
        var manifest = new BakeManifest();
        manifest.Add("listed", [1, 2, 3]);
        var parsed = BakeManifest.Parse(manifest.Encode());
        parsed.Verify("listed", [1, 2, 3], false);
        Reject(() => parsed.Verify("listed", [1, 2, 4], false));
        Reject(() => parsed.Verify("unlisted", [1, 2, 3], false));
        parsed.Verify("unlisted", [1, 2, 3], true);
    }

    [TestCase]
    public void LargeReferenceManifestRoundTripsAndAcceptsTheExactEightMiBBoundary()
    {
        var manifest = new BakeManifest();
        byte[] payload = [1, 2, 3];
        var identities = Enumerable.Range(0, 6000).Select(index => FormattableString.Invariant(
            $"{BakeOptions.Abi}/fixture/{new string('a', 64)}/fixture.content@{new string('b', 64)}/L0/{index:D7}")).ToArray();
        foreach (var identity in identities) manifest.Add(identity, payload);
        var encoded = manifest.Encode();
        if (encoded.Length <= 1024 * 1024 || encoded.Length >= BakeManifest.MaximumBytes)
            throw new InvalidOperationException("The realistic reference fixture did not exercise the old one-MiB limit.");
        var parsed = BakeManifest.Parse(encoded);
        if (parsed.Count != identities.Length || identities.Any(identity => parsed.Find(identity) != manifest.Find(identity)))
            throw new InvalidOperationException("A large reference manifest lost keys or hashes.");

        // All keys/hash values are ASCII without escapes. A subsequent property occupies key.Length + 38 bytes.
        const int keyLength = 1800;
        var fullEntries = (BakeManifest.MaximumBytes - encoded.Length) / (keyLength + 38);
        var remaining = BakeManifest.MaximumBytes - encoded.Length - fullEntries * (keyLength + 38);
        if (remaining is > 0 and < 39) { fullEntries--; remaining += keyLength + 38; }
        for (var index = 0; index < fullEntries; index++)
            manifest.Add(FormattableString.Invariant($"padding/{index:D6}/{new string('x', keyLength - 15)}"), payload);
        if (remaining > 0) manifest.Add(new string('z', remaining - 38), payload);
        var atLimit = manifest.Encode();
        if (atLimit.Length != BakeManifest.MaximumBytes || BakeManifest.Parse(atLimit).Count != manifest.Count)
            throw new InvalidOperationException("Encode and Parse disagree at the inclusive eight-MiB limit.");

        manifest.Add("over-limit", payload);
        var encodeError = Rejection(() => manifest.Encode());
        if (!encodeError.Contains($"count={manifest.Count}", StringComparison.Ordinal) ||
            !encodeError.Contains($"maximumBytes={BakeManifest.MaximumBytes}", StringComparison.Ordinal) || !encodeError.Contains("bytes=", StringComparison.Ordinal))
            throw new InvalidOperationException("Oversized encoding omitted its actual size/count/budget diagnostic.");
        var parseError = Rejection(() => BakeManifest.Parse([.. atLimit, (byte)' ']));
        if (!parseError.Contains($"bytes={BakeManifest.MaximumBytes + 1}", StringComparison.Ordinal) ||
            !parseError.Contains("count=unread", StringComparison.Ordinal) || !parseError.Contains($"maximumBytes={BakeManifest.MaximumBytes}", StringComparison.Ordinal))
            throw new InvalidOperationException("The parser did not reject oversize bytes before allocating a JSON document.");

        static string Rejection(Action action)
        {
            try { action(); }
            catch (InvalidDataException error) { return error.Message; }
            throw new InvalidOperationException("An oversized manifest was accepted.");
        }
    }

    [TestCase]
    public void UnloadingAnInFlightRequestKeepsItsOldHandleFailed()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var key = BakeRequestKeys.Tile(new("fixture"), "fixture.module", new(0, 0), LodLevel.L0);
        var queue = new BakeQueue(1);
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
    [RequireGodotRuntime]
    public void CancelledCacheReaderLeavesItsIndexUntouchedWithoutWaitingForTheOwnerLock()
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY")!;
        var directory = Path.Combine(root, "output", "bake-cancel", Guid.NewGuid().ToString("N"));
        var cache = new BakeCacheStore(directory);
        byte[] payload = [1, 2, 3, 4];
        cache.Write("fixture", "first", payload);
        var indexPath = Path.Combine(directory, "fixture", "cache.index");
        var index = File.ReadAllBytes(indexPath);
        var gate = typeof(BakeCacheStore).GetField("_sync", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!.GetValue(cache)!;
        using var cancellation = new CancellationTokenSource();
        using var started = new ManualResetEventSlim();
        using var done = new ManualResetEventSlim();
        Exception? stopped = null;
        var worker = new Thread(() =>
        {
            started.Set();
            try
            {
                cache.TryRead("fixture", "first", BakeCacheStore.Hash(payload), true,
                cancellationToken: cancellation.Token);
            }
            catch (Exception error) { stopped = error; }
            finally { done.Set(); }
        });
        try
        {
            lock (gate)
            {
                worker.Start();
                if (!started.Wait(TimeSpan.FromSeconds(5)) || !SpinWait.SpinUntil(() =>
                    (worker.ThreadState & ThreadState.WaitSleepJoin) != 0, TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Cache reader did not wait for the index lock.");
                cancellation.Cancel();
                if (!done.Wait(TimeSpan.FromSeconds(1))) throw new TimeoutException("Cancelled reader still waited for the index lock.");
            }
            worker.Join();
            if (stopped is not OperationCanceledException || !index.SequenceEqual(File.ReadAllBytes(indexPath)))
                throw new InvalidOperationException("Cancelled cache read changed the index or suppressed cancellation.");
            var writeCancelled = false;
            try { cache.Write("fixture", "cancelled", payload, cancellationToken: cancellation.Token); }
            catch (OperationCanceledException) { writeCancelled = true; }
            if (!writeCancelled || !index.SequenceEqual(File.ReadAllBytes(indexPath)) ||
                Directory.GetFiles(directory, "*.tbin", SearchOption.AllDirectories).Length != 1)
                throw new InvalidOperationException("A cancelled cache write committed another product or index.");
        }
        finally { cancellation.Cancel(); if (worker.IsAlive) worker.Join(); }
    }

    [TestCase]
    public void NegativeTileParentsUseFloorDivision()
    {
        if (BakeRequestKeys.Parent(new(-1, -3), LodLevel.L1) != new TileKey(-1, -2))
            throw new InvalidOperationException("Negative grid coordinates were truncated toward zero.");
        var key = BakeRequestKeys.Tile(new("fixture"), "module", new(-1, -2), LodLevel.L2);
        var parsed = BakeRequestKeys.Parse(key);
        if (parsed.Tile != new TileKey(-1, -2) || parsed.Level != LodLevel.L2 || parsed.Collision)
            throw new InvalidOperationException("Tile request identity did not round trip.");
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Malformed bake data was accepted.");
    }
}
