using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text.Json;
using Godot;
using FileAccess = System.IO.FileAccess;

namespace Campus.Kernel.Bake;

/// <summary>Only this store touches the local bake cache. Files contain bounded data, never engine resources.</summary>
public sealed class BakeCacheStore
{
    public const int MaximumTileUploadBytes = 8 * 1024 * 1024;
    public const int MaximumCompressedTileBytes = 8 * 1024 * 1024;
    // Header/statistics allowance is separate from the native raw-buffer upload budget.
    public const int MaximumPayloadBytes = MaximumTileUploadBytes + 1024 * 1024;
    public const int MaximumProductBytes = 128 * 1024 * 1024;
    private const int MaximumIndexBytes = 16 * 1024 * 1024;
    private const uint CompressionMagic = 0x315A4254; // TBZ1
    private readonly string _root;
    private readonly object _sync = new();
    private readonly Dictionary<string, CacheIndex> _indices = new(StringComparer.Ordinal);
    private long _sequence;
    private long _bytes;
    private bool _initialized;
    private static long _temporarySequence;

    public BakeCacheStore(string root)
    {
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("Cache root must be absolute.", nameof(root));
        _root = Path.GetFullPath(root);
    }

    public long CacheBytes => Interlocked.Read(ref _bytes);

    /// <summary>Called by a worker, never by the UI polling CacheBytes.</summary>
    public void Initialize()
    {
        lock (_sync)
        {
            if (_initialized) return;
            _bytes = Directory.Exists(_root) ? SafeFiles(_root)
                .Where(p => p.EndsWith(".tbin", StringComparison.Ordinal)).Sum(p => new FileInfo(p).Length) : 0;
            _initialized = true;
        }
    }

    public static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(XxHash128.Hash(bytes));

    public byte[]? TryRead(string map, string identity, string? expectedHash, bool allowUnlisted,
        int maximumPayload = MaximumPayloadBytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = FilePath(map, identity);
        if (!File.Exists(path)) return null;
        try
        {
            var fileLimit = maximumPayload == MaximumProductBytes ? MaximumProductBytes + 1024 * 1024 : MaximumCompressedTileBytes;
            var bytes = DecodeFile(ReadBounded(path, fileLimit), maximumPayload);
            cancellationToken.ThrowIfCancellationRequested();
            var hash = Hash(bytes);
            EnterCache(cancellationToken);
            try
            {
                var index = Index(map);
                var expected = expectedHash;
                if (expected is null && allowUnlisted && index.Entries.TryGetValue(identity, out var entry)) expected = entry.Hash;
                if (expected is null || !string.Equals(hash, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Cache hash mismatch: {identity}; expected={expected ?? "unlisted"}; actual={hash}");
                index.Entries[identity] = new(hash, new FileInfo(path).Length, ++_sequence);
                SaveIndex(map, index);
            }
            finally { Monitor.Exit(_sync); }
            return bytes;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or JsonException or ArgumentException or OverflowException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnterCache(cancellationToken);
            try
            {
                if (File.Exists(path)) { _bytes -= new FileInfo(path).Length; File.Delete(path); }
                Index(map).Entries.Remove(identity);
            }
            finally { Monitor.Exit(_sync); }
            return null;
        }
    }

    public void Write(string map, string identity, byte[] bytes, bool mapProduct = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var encoded = EncodeFile(bytes);
        cancellationToken.ThrowIfCancellationRequested();
        if (!mapProduct && encoded.Length > MaximumCompressedTileBytes)
            throw new InvalidDataException("Compressed tile exceeds 8 MiB on disk; split the generated tile.");
        EnterCache(cancellationToken);
        try
        {
            var path = FilePath(map, identity);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var oldLength = File.Exists(path) ? new FileInfo(path).Length : 0;
            AtomicWrite(path, encoded);
            _bytes += encoded.Length - oldLength;
            var index = Index(map);
            index.Entries[identity] = new(Hash(bytes), encoded.Length, ++_sequence);
            SaveIndex(map, index);
        }
        finally { Monitor.Exit(_sync); }
    }

    private void EnterCache(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled) { Monitor.Enter(_sync); return; }
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Monitor.TryEnter(_sync, 50)) continue;
            if (!cancellationToken.IsCancellationRequested) return;
            Monitor.Exit(_sync);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            if (Directory.Exists(_root))
                foreach (var path in SafeFiles(_root).Where(p => p.EndsWith(".tbin", StringComparison.Ordinal) ||
                    p.EndsWith(".tmp", StringComparison.Ordinal) || Path.GetFileName(p) == "cache.index")) File.Delete(path);
            _indices.Clear();
            _bytes = 0;
        }
    }

    public void Remove(string map, string identity, CancellationToken cancellationToken = default)
    {
        EnterCache(cancellationToken);
        try
        {
            var path = FilePath(map, identity);
            if (File.Exists(path)) { _bytes -= new FileInfo(path).Length; File.Delete(path); }
            Index(map).Entries.Remove(identity);
        }
        finally { Monitor.Exit(_sync); }
    }

    public void RemoveOtherAbis(string map)
    {
        lock (_sync)
        {
            var path = MapPath(map);
            if (!Directory.Exists(path)) return;
            foreach (var directory in Directory.EnumerateDirectories(path).Order(StringComparer.Ordinal))
            {
                if (Path.GetFileName(directory) == BakeOptions.Abi || IsLink(directory)) continue;
                // No recursive delete: only known bake files under a validated map directory are removed.
                foreach (var file in SafeFiles(directory).Where(p => p.EndsWith(".tbin", StringComparison.Ordinal) ||
                    p.EndsWith(".tmp", StringComparison.Ordinal)))
                {
                    if (file.EndsWith(".tbin", StringComparison.Ordinal)) _bytes -= new FileInfo(file).Length;
                    File.Delete(file);
                }
            }
        }
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

    public static byte[] DecodeFile(byte[] file, int maximumPayload = MaximumPayloadBytes)
    {
        if (file.Length < 12 || BinaryPrimitives.ReadUInt32LittleEndian(file) != CompressionMagic)
            throw new InvalidDataException("Invalid compressed tile header.");
        var length = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(4));
        var compressedLength = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(8));
        if (maximumPayload is <= 0 or > MaximumProductBytes || length <= 0 || length > maximumPayload ||
            compressedLength != file.Length - 12 || compressedLength <= 0)
            throw new InvalidDataException("Invalid compressed tile lengths.");
        var payload = GD.Decompress(file[12..], length, Godot.FileAccess.CompressionMode.Zstd);
        if (payload.Length != length) throw new InvalidDataException("Truncated or corrupt Zstd tile.");
        return payload;
    }

    public static byte[] ReadBounded(string path, int limit)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is <= 0 || stream.Length > limit) throw new InvalidDataException($"File length exceeds limit: {path}");
        var result = new byte[checked((int)stream.Length)];
        stream.ReadExactly(result);
        return result;
    }

    public static void AtomicWrite(string path, byte[] bytes)
    {
        var temporary = path + "." + System.Environment.ProcessId + "." + Interlocked.Increment(ref _temporarySequence) + ".tmp";
        using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            file.Write(bytes);
            file.Flush(flushToDisk: true);
        }
        File.Move(temporary, path, overwrite: true);
    }

    private string MapPath(string map)
    {
        if (string.IsNullOrWhiteSpace(map) || map.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            throw new ArgumentException("Map ID is not a safe cache segment.", nameof(map));
        var path = Path.GetFullPath(Path.Combine(_root, map));
        if (Directory.Exists(_root) && IsLink(_root) || Directory.Exists(path) && IsLink(path))
            throw new IOException("Cache paths must not be reparse points.");
        return path;
    }

    private string FilePath(string map, string identity)
    {
        // The full canonical key is in the payload and index; a digest avoids path traversal and Windows path limits.
        var directory = Path.Combine(MapPath(map), BakeOptions.Abi);
        if (Directory.Exists(directory) && IsLink(directory)) throw new IOException("ABI directory is a reparse point.");
        if (identity.Contains("/interior.", StringComparison.Ordinal)) directory = Path.Combine(directory, "interior");
        if (Directory.Exists(directory) && IsLink(directory)) throw new IOException("Cache directory is a reparse point.");
        var path = Path.Combine(directory, Hash(System.Text.Encoding.UTF8.GetBytes(identity)) + ".tbin");
        if (File.Exists(path) && IsLink(path)) throw new IOException("Cache file is a reparse point.");
        return path;
    }

    private CacheIndex Index(string map)
    {
        if (_indices.TryGetValue(map, out var found)) return found;
        var path = Path.Combine(MapPath(map), "cache.index");
        CacheIndex index;
        try
        {
            index = File.Exists(path) ? JsonSerializer.Deserialize<CacheIndex>(ReadBounded(path, MaximumIndexBytes)) ?? new() : new();
            if (index.Schema != 1 || index.Entries is null || index.Entries.Count > 100000) index = new();
        }
        catch (Exception error) when (error is IOException or InvalidDataException or JsonException) { index = new(); }
        foreach (var entry in index.Entries.Values) _sequence = Math.Max(_sequence, entry.LastUsed);
        _indices.Add(map, index);
        return index;
    }

    private void SaveIndex(string map, CacheIndex index)
    {
        var directory = MapPath(map);
        Directory.CreateDirectory(directory);
        AtomicWrite(Path.Combine(directory, "cache.index"), JsonSerializer.SerializeToUtf8Bytes(index));
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

    public sealed class CacheIndex
    {
        public int Schema { get; set; } = 1;
        public SortedDictionary<string, CacheEntry> Entries { get; set; } = new(StringComparer.Ordinal);
    }
    public sealed record CacheEntry(string Hash, long Bytes, long LastUsed);
}
