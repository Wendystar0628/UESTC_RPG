using System.Buffers.Binary;
using System.Text.Json;
using Campus.Contracts.Bake;
using Campus.Kernel.Registry;
using Godot;
using FileAccess = System.IO.FileAccess;

namespace Campus.Kernel.Bake;

/// <summary>Local writable products over an optional read-only packaged tree. Each candidate must decode before acceptance.</summary>
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
    private readonly string? _packagedRoot;
    private readonly string _version;
    private readonly object _sync = new();
    private long _bytes;
    private bool _initialized;
    private bool _packagedAvailable;
    private bool _stopped;
    private static long _temporarySequence;

    public BakeCacheStore(string root, string? version = null, string? packagedRoot = null)
    {
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("Cache root must be absolute.", nameof(root));
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        if (packagedRoot is not null)
        {
            if (!Path.IsPathFullyQualified(packagedRoot)) throw new ArgumentException("Packaged cache root must be absolute.", nameof(packagedRoot));
            _packagedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(packagedRoot));
            if (ContainsPath(_root, _packagedRoot) || ContainsPath(_packagedRoot, _root))
                throw new ArgumentException("Writable and packaged cache roots must not overlap.", nameof(packagedRoot));
        }
        _version = (version ?? BuildInfo.Version) + "/" + BakeOptions.Abi;
    }

    public long CacheBytes => Interlocked.Read(ref _bytes);

    /// <summary>Version changes discard player products before a worker can read one.</summary>
    public void Initialize()
    {
        lock (_sync)
        {
            ThrowIfStopped();
            if (_initialized) return;
            if (Directory.Exists(_root) && IsLink(_root)) throw new IOException("Cache root must not be a reparse point.");
            Directory.CreateDirectory(_root);
            var marker = Path.Combine(_root, "cache.version");
            if (!File.Exists(marker) || File.ReadAllText(marker) != _version)
            {
                DeleteProducts();
                AtomicWrite(marker, System.Text.Encoding.UTF8.GetBytes(_version));
            }
            _bytes = SafeFiles(_root).Where(path => path.EndsWith(".tbin", StringComparison.Ordinal))
                .Sum(path => new FileInfo(path).Length);
            if (_packagedRoot is not null && Directory.Exists(_packagedRoot) && !IsLink(_packagedRoot))
            {
                var packagedMarker = Path.Combine(_packagedRoot, "cache.version");
                try { _packagedAvailable = File.Exists(packagedMarker) && !IsLink(packagedMarker) && File.ReadAllText(packagedMarker) == _version; }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { _packagedAvailable = false; }
            }
            _initialized = true;
        }
    }

    public T? TryRead<T>(BakeKey key, BakePayloadClass payload, Func<byte[], T> decode,
        CancellationToken cancellationToken = default) where T : class
    {
        foreach (var value in ReadCandidates(key, payload, decode, cancellationToken)) return value;
        return null;
    }

    internal IEnumerable<T> ReadCandidates<T>(BakeKey key, BakePayloadClass payload, Func<byte[], T> decode,
        CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        Initialize();
        if (ReadCandidate(_root, key, payload, decode, cancellationToken) is { } local) yield return local;
        if (_packagedAvailable && ReadCandidate(_packagedRoot!, key, payload, decode, cancellationToken) is { } packaged)
            yield return packaged;
    }

    private T? ReadCandidate<T>(string root, BakeKey key, BakePayloadClass payload, Func<byte[], T> decode,
        CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = FilePath(root, key);
        if (!File.Exists(path)) return null;
        try
        {
            var file = ReadBounded(path, MaximumFile(payload));
            byte[] bytes;
            EnterCache(cancellationToken);
            try
            {
                ThrowIfStopped();
                bytes = DecodeFile(file, MaximumPayload(payload), key);
            }
            finally { Monitor.Exit(_sync); }
            cancellationToken.ThrowIfCancellationRequested();
            var product = decode(bytes) ?? throw new InvalidDataException("Empty cached product.");
            cancellationToken.ThrowIfCancellationRequested();
            return product;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or ArgumentException or OverflowException or JsonException or UnauthorizedAccessException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (root == _root) Remove(key, cancellationToken);
            return null;
        }
    }

    public void Write(BakeKey key, BakePayloadClass payload, byte[] bytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Initialize();
        EnterCache(cancellationToken);
        try
        {
            ThrowIfStopped();
            var maximum = MaximumPayload(payload);
            if (bytes.Length <= 0 || bytes.Length > maximum)
                throw new InvalidDataException($"Bake payload {key.Map.Value}/{key.Module}/{key.Detail}: {bytes.Length} bytes exceeds limit {maximum}.");
            var encoded = EncodeFile(bytes);
            cancellationToken.ThrowIfCancellationRequested();
            if (encoded.Length > MaximumFile(payload))
                throw new InvalidDataException($"Compressed bake product {key.Map.Value}/{key.Module}/{key.Detail}: {encoded.Length} bytes exceeds limit {MaximumFile(payload)}.");
            var path = FilePath(_root, key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var oldLength = File.Exists(path) ? new FileInfo(path).Length : 0;
            AtomicWrite(path, encoded, cancellationToken);
            _bytes += encoded.Length - oldLength;
        }
        finally { Monitor.Exit(_sync); }
    }

    // After this returns, no worker can enter Godot's compression API or publish another cache file.
    internal void Stop()
    {
        lock (_sync) _stopped = true;
    }

    private void ThrowIfStopped()
    {
        if (_stopped) throw new OperationCanceledException("Bake cache stopped.");
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
            ThrowIfStopped();
            DeleteProducts();
            _bytes = 0;
        }
    }

    private void DeleteProducts()
    {
        if (!Directory.Exists(_root)) return;
        foreach (var path in SafeFiles(_root).Where(path => path.EndsWith(".tbin", StringComparison.Ordinal) ||
            path.EndsWith(".tmp", StringComparison.Ordinal) || Path.GetFileName(path) == "cache.index")) File.Delete(path);
    }

    private void Remove(BakeKey key, CancellationToken cancellationToken = default)
    {
        EnterCache(cancellationToken);
        try
        {
            ThrowIfStopped();
            var path = FilePath(_root, key);
            if (File.Exists(path)) { _bytes -= new FileInfo(path).Length; File.Delete(path); }
        }
        finally { Monitor.Exit(_sync); }
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
        var temporary = path + "." + System.Environment.ProcessId + "." + Interlocked.Increment(ref _temporarySequence) + ".tmp";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes);
                file.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static string FilePath(string root, BakeKey key)
    {
        var detail = key.Detail.Split('/');
        var segments = new List<string> { root, Segment(key.Map.Value), Segment(key.Module) };
        segments.AddRange(detail.Select(Segment));
        var path = Path.Combine(segments.ToArray()) + ".tbin";
        for (var current = path; current != root; current = Path.GetDirectoryName(current)!)
        {
            // Exists refreshes FileInfo's attributes; reuse them for the reparse check.
            var entry = new FileInfo(current);
            if ((entry.Exists || Directory.Exists(current)) && (entry.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Cache paths must not be reparse points.");
        }
        return path;
    }

    private static string Segment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or "..") throw new ArgumentException("Invalid cache key segment.");
        return Uri.EscapeDataString(value);
    }

    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    private static bool ContainsPath(string parent, string child)
    {
        var relative = Path.GetRelativePath(parent, child);
        return !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
    private static IEnumerable<string> SafeFiles(string root)
    {
        if (IsLink(root)) yield break;
        foreach (var file in Directory.EnumerateFiles(root).Order(StringComparer.Ordinal))
            if (!IsLink(file)) yield return file;
        foreach (var directory in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
            if (!IsLink(directory)) foreach (var file in SafeFiles(directory)) yield return file;
    }
}
