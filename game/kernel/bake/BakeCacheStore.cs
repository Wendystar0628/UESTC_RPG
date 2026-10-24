using System.Buffers.Binary;
using System.Text.Json;
using System.Collections.Concurrent;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Registry;
using Godot;
using FileAccess = System.IO.FileAccess;

namespace Campus.Kernel.Bake;

/// <summary>Local writable logical indexes and bounded parts. Every part must decode before acceptance.</summary>
public sealed class BakeCacheStore
{
    public const int MaximumTileUploadBytes = 8 * 1024 * 1024;
    public const int MaximumCompressedTileBytes = 8 * 1024 * 1024;
    public const int MaximumPayloadBytes = MaximumTileUploadBytes + 1024 * 1024;
    public const int MaximumProductBytes = 128 * 1024 * 1024;
    internal static int MaximumPayload(BakePayloadClass payload) => payload switch
    {
        BakePayloadClass.Tile => MaximumPayloadBytes,
        BakePayloadClass.Product => MaximumProductBytes,
        _ => throw new ArgumentOutOfRangeException(nameof(payload))
    };
    internal static int MaximumFile(BakePayloadClass payload) => payload switch
    {
        BakePayloadClass.Tile => MaximumCompressedTileBytes,
        BakePayloadClass.Product => MaximumProductBytes + 1024 * 1024,
        _ => throw new ArgumentOutOfRangeException(nameof(payload))
    };

    private const uint CompressionMagic = 0x315A4254; // TBZ1
    private readonly string _root;
    // Readers keep a whole logical group stable; mutation and native shutdown wait for them.
    private readonly ReaderWriterLockSlim _sync = new();
    private long _bytes;
    private long _hits, _misses, _rebuilds;
    private bool _bytesKnown;
    private volatile bool _initialized;
    private volatile bool _stopped;
    private static long _stagingSequence;
    private readonly ConcurrentDictionary<BakeKey, Exception> _rejected = new();
    private readonly Dictionary<BakeKey, long> _publications = [];
    private long _publicationSequence;
    // ENG-ARCH: retain the cause until the owner has published a valid local replacement.
    internal IReadOnlyList<Exception> RebuildFailures(BakeKey key) =>
        _rejected.TryGetValue(key, out var error) ? [error] : [];

    public BakeCacheStore(string root)
    {
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("Cache root must be absolute.", nameof(root));
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
    }

    public BakeCacheStatistics CacheStatistics => new(Interlocked.Read(ref _hits),
        Interlocked.Read(ref _misses), Interlocked.Read(ref _rebuilds));

    public long CacheBytes
    {
        get
        {
            Initialize();
            _sync.EnterWriteLock();
            try
            {
                ThrowIfStopped();
                // This total is only used by the storage settings; cache reads do not need an inventory.
                if (!_bytesKnown)
                {
                    _bytes = SafeFiles(_root).Where(path => path.EndsWith(".tbin", StringComparison.Ordinal))
                        .Sum(path => new FileInfo(path).Length);
                    _bytesKnown = true;
                }
                return _bytes;
            }
            finally { _sync.ExitWriteLock(); }
        }
    }

    /// <summary>ABI changes discard cached products before a worker can read one.</summary>
    public void Initialize()
    {
        ThrowIfStopped();
        if (_initialized) return;
        _sync.EnterWriteLock();
        try
        {
            ThrowIfStopped();
            if (_initialized) return;
            if (Directory.Exists(_root) && IsLink(_root)) throw new IOException("Cache root must not be a reparse point.");
            Directory.CreateDirectory(_root);
            var marker = Path.Combine(_root, "cache.version");
            if (!File.Exists(marker) || File.ReadAllText(marker) != BakeOptions.Abi)
            {
                DeleteProducts();
                AtomicWrite(marker, System.Text.Encoding.UTF8.GetBytes(BakeOptions.Abi));
                _bytesKnown = true;
            }
            _initialized = true;
        }
        finally { _sync.ExitWriteLock(); }
    }

    public T? TryRead<T>(BakeKey key, BakePayloadClass payload, string identity, Func<byte[], T> decode,
        CancellationToken cancellationToken = default) where T : class
    {
        return TryReadParts(key, payload, identity, parts => parts.Count == 1 ? decode(parts[0]) :
            throw new InvalidDataException("A single-payload product has multiple delivery parts."), cancellationToken);
    }

    internal T? TryReadParts<T>(BakeKey key, BakePayloadClass payload, string identity, Func<IReadOnlyList<byte[]>, T> decode,
        CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        Initialize();
        var path = FilePath(_root, key);
        long publication = 0;
        try
        {
            var parts = new List<byte[]>();
            EnterCache(cancellationToken);
            try
            {
                ThrowIfStopped();
                publication = _publications.GetValueOrDefault(key);
                if (!File.Exists(path))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Interlocked.Increment(ref _misses);
                    return null;
                }
                ValidatePath(_root, path);
                var count = BakeCacheIndex.Read(path, payload, identity);
                ValidateEntry(BakeCacheIndex.DirectoryPath(path));
                for (var index = 0; index < count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var partPath = BakeCacheIndex.PartPath(path, index);
                    ValidateEntry(partPath);
                    parts.Add(DecodeFile(ReadBounded(partPath, MaximumFile(payload)), MaximumPayload(payload), key));
                }
            }
            finally { _sync.ExitReadLock(); }
            cancellationToken.ThrowIfCancellationRequested();
            var product = decode(parts) ?? throw new InvalidDataException("Empty cached product.");
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _hits);
            return product;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or ArgumentException or OverflowException or JsonException or UnauthorizedAccessException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectProduct(key, publication, error, cancellationToken);
            Interlocked.Increment(ref _misses);
            return null;
        }
    }

    // A match proves only published input identity; mounting still validates every payload part.
    internal bool MatchesInput(BakeKey key, BakePayloadClass payload, string identity,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Initialize();
        var path = FilePath(_root, key);
        long publication = 0;
        try
        {
            EnterCache(cancellationToken);
            try
            {
                ThrowIfStopped();
                publication = _publications.GetValueOrDefault(key);
                if (!File.Exists(path)) return false;
                ValidatePath(_root, path);
                _ = BakeCacheIndex.Read(path, payload, identity);
                cancellationToken.ThrowIfCancellationRequested();
                return true;
            }
            finally { _sync.ExitReadLock(); }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or ArgumentException or OverflowException or UnauthorizedAccessException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectProduct(key, publication, error, cancellationToken);
            return false;
        }
    }

    private void RejectProduct(BakeKey key, long publication, [FailureHandler("ENG-ARCH")] Exception error,
        CancellationToken cancellationToken)
    {
        _rejected[key] = error;
        try { Remove(key, publication, cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception removalError)
        { throw new AggregateException("Damaged bake cache could not be removed.", error, removalError); }
    }

    public void Write(BakeKey key, BakePayloadClass payload, string identity, byte[] bytes, CancellationToken cancellationToken = default)
        => WriteParts(key, payload, identity, [bytes], cancellationToken);

    internal bool Fits(BakePayloadClass payload, byte[] bytes, CancellationToken cancellationToken)
    {
        EnterCache(cancellationToken);
        try
        {
            ThrowIfStopped();
            return bytes.Length <= MaximumPayload(payload) && EncodeFile(bytes).Length <= MaximumFile(payload);
        }
        finally { _sync.ExitReadLock(); }
    }

    internal void WriteParts(BakeKey key, BakePayloadClass payload, string identity, IReadOnlyList<byte[]> parts, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Initialize();
        EnterCache(cancellationToken, write: true);
        try
        {
            ThrowIfStopped();
            if (parts.Count == 0) throw new InvalidDataException("A cached product needs at least one delivery part.");
            var indexBytes = BakeCacheIndex.Encode(payload, parts.Count, identity);
            var path = FilePath(_root, key);
            ValidatePath(_root, path);
            _publications[key] = ++_publicationSequence;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var partDirectory = BakeCacheIndex.DirectoryPath(path);
            ValidateEntry(partDirectory);
            Directory.CreateDirectory(partDirectory);
            // Remove the published identity before replacing any member of this group.
            DeleteProductFile(path);
            for (var index = 0; index < parts.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytes = parts[index];
                var maximum = MaximumPayload(payload);
                if (bytes.Length <= 0 || bytes.Length > maximum)
                    throw new InvalidDataException($"Bake part {key}: {bytes.Length} bytes exceeds limit {maximum}.");
                var encoded = EncodeFile(bytes);
                if (encoded.Length > MaximumFile(payload))
                    throw new InvalidDataException($"Compressed bake part {key}: {encoded.Length} bytes exceeds limit {MaximumFile(payload)}.");
                var partPath = BakeCacheIndex.PartPath(path, index);
                ValidateEntry(partPath);
                Publish(partPath, encoded, cancellationToken);
            }
            // Publishing the small logical index is the commit point; it never contains geometry buffers.
            Publish(path, indexBytes, cancellationToken);
            Interlocked.Increment(ref _rebuilds);
            foreach (var obsolete in BakeCacheIndex.Files(path).Skip(parts.Count)) DeleteProductFile(obsolete);
            _rejected.TryRemove(key, out _);
        }
        finally { _sync.ExitWriteLock(); }
    }

    private void Publish(string path, byte[] bytes, CancellationToken cancellationToken)
    {
        var oldLength = _bytesKnown && File.Exists(path) ? new FileInfo(path).Length : 0;
        AtomicWrite(path, bytes, cancellationToken);
        if (_bytesKnown) _bytes += bytes.Length - oldLength;
    }

    private void DeleteProductFile(string path)
    {
        if (!File.Exists(path)) return;
        if (_bytesKnown) _bytes -= new FileInfo(path).Length;
        File.Delete(path);
    }

    // After this returns, no worker can enter Godot's compression API or publish another cache file.
    internal void Stop()
    {
        _sync.EnterWriteLock();
        try { _stopped = true; }
        finally { _sync.ExitWriteLock(); }
    }

    private void ThrowIfStopped()
    {
        if (_stopped) throw new OperationCanceledException("Bake cache stopped.");
    }

    private void EnterCache(CancellationToken cancellationToken, bool write = false)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            if (write) _sync.EnterWriteLock(); else _sync.EnterReadLock();
            return;
        }
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!(write ? _sync.TryEnterWriteLock(50) : _sync.TryEnterReadLock(50))) continue;
            if (!cancellationToken.IsCancellationRequested) return;
            if (write) _sync.ExitWriteLock(); else _sync.ExitReadLock();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public void Clear()
    {
        _sync.EnterWriteLock();
        try
        {
            ThrowIfStopped();
            DeleteProducts();
            foreach (var key in _publications.Keys.ToArray()) _publications[key] = ++_publicationSequence;
            _bytes = 0;
            _bytesKnown = true;
        }
        finally { _sync.ExitWriteLock(); }
    }

    private void DeleteProducts()
    {
        if (!Directory.Exists(_root)) return;
        foreach (var path in SafeFiles(_root).Where(path => path.EndsWith(".tbin", StringComparison.Ordinal) ||
            path.EndsWith(".tmp", StringComparison.Ordinal) || Path.GetFileName(path) == "cache.index")) File.Delete(path);
    }

    private void Remove(BakeKey key, long publication, CancellationToken cancellationToken = default)
    {
        EnterCache(cancellationToken, write: true);
        try
        {
            ThrowIfStopped();
            // A decoder may reject a detached snapshot after another worker has already replaced its files.
            if (_publications.GetValueOrDefault(key) != publication) return;
            _publications[key] = ++_publicationSequence;
            var path = FilePath(_root, key);
            ValidatePath(_root, path);
            DeleteProductFile(path);
            foreach (var part in BakeCacheIndex.Files(path)) DeleteProductFile(part);
        }
        finally { _sync.ExitWriteLock(); }
    }

    public static byte[] EncodeFile(byte[] payload)
    {
        if (payload.Length is <= 0 or > MaximumProductBytes) throw new InvalidDataException("Bake product exceeds its size limit.");
        var compressed = GD.Compress(payload, Godot.FileAccess.CompressionMode.Zstd);
        if (compressed.Length == 0) throw new InvalidDataException("Zstd compression failed.");
        var result = new byte[checked(compressed.Length + 12)];
        BinaryPrimitives.WriteUInt32LittleEndian(result, CompressionMagic);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4), payload.Length);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), compressed.Length);
        compressed.CopyTo(result, 12);
        return result;
    }

    public static byte[] DecodeFile(byte[] file, int maximumPayload = MaximumPayloadBytes, BakeKey? key = null)
    {
        if (file.Length < 12 || BinaryPrimitives.ReadUInt32LittleEndian(file) != CompressionMagic)
            throw new InvalidDataException("Invalid compressed tile header.");
        var length = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(4));
        var compressedLength = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(8));
        if (maximumPayload is <= 0 or > MaximumProductBytes || length <= 0 || length > maximumPayload ||
            compressedLength != file.Length - 12 || compressedLength <= 0)
            throw new InvalidDataException($"Invalid compressed product lengths for {key}: payload={length}, compressed={compressedLength}, actual={file.Length - 12}, limit={maximumPayload}.");
        var payload = GD.Decompress(file[12..], length, Godot.FileAccess.CompressionMode.Zstd);
        if (payload.Length != length) throw new InvalidDataException("Truncated or corrupt Zstd tile.");
        return payload;
    }

    public static byte[] ReadBounded(string path, int limit)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is <= 0 || stream.Length > limit) throw new InvalidDataException($"File {path}: {stream.Length} bytes exceeds limit {limit}.");
        var result = new byte[checked((int)stream.Length)];
        stream.ReadExactly(result);
        return result;
    }

    public static void AtomicWrite(string path, byte[] bytes, CancellationToken cancellationToken = default)
    {
        var stagingPath = path + "." + System.Environment.ProcessId + "." + Interlocked.Increment(ref _stagingSequence) + ".tmp";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var file = new FileStream(stagingPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes);
                file.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(stagingPath, path, overwrite: true);
        }
        catch (Exception error)
        {
            CleanupFailedWrite(stagingPath, error);
            throw;
        }
    }

    private static void CleanupFailedWrite(string path, [FailureHandler("ENG-ARCH")] Exception error)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception cleanupError) { throw new AggregateException("Bake publication and staging cleanup failed.", error, cleanupError); }
    }

    private static string FilePath(string root, BakeKey key)
    {
        var detail = key.Detail.Split('/');
        var segments = new List<string> { root, Segment(key.Map.Value), Segment(key.Module) };
        segments.AddRange(detail.Select(Segment));
        return Path.Combine(segments.ToArray()) + ".tbin";
    }

    private static void ValidatePath(string root, string path)
    {
        for (var current = path; current != root; current = Path.GetDirectoryName(current)!)
            ValidateEntry(current);
    }

    private static void ValidateEntry(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return;
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Cache paths must not be reparse points.");
    }

    private static string Segment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or "..") throw new ArgumentException("Invalid cache key segment.");
        return Uri.EscapeDataString(value);
    }

    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    private static IEnumerable<string> SafeFiles(string root)
    {
        if (IsLink(root)) yield break;
        foreach (var file in Directory.EnumerateFiles(root).Order(StringComparer.Ordinal))
            if (!IsLink(file)) yield return file;
        foreach (var directory in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
            if (!IsLink(directory)) foreach (var file in SafeFiles(directory)) yield return file;
    }
}
