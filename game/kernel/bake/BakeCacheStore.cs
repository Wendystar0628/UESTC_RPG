using System.Buffers.Binary;
using Campus.Contracts.Bake;
using Campus.Kernel.Registry;
using Godot;
using FileAccess = System.IO.FileAccess;

namespace Campus.Kernel.Bake;

/// <summary>Local CPU products, stored once per map/product/module/cell/LOD. The payload owns its input stamp.</summary>
public sealed class BakeCacheStore
{
    public const int MaximumTileUploadBytes = 8 * 1024 * 1024;
    public const int MaximumCompressedTileBytes = 8 * 1024 * 1024;
    public const int MaximumPayloadBytes = MaximumTileUploadBytes + 1024 * 1024;
    public const int MaximumProductBytes = 128 * 1024 * 1024;
    private const uint CompressionMagic = 0x315A4254; // TBZ1
    private readonly string _root;
    private readonly string _version;
    private readonly object _sync = new();
    private long _bytes;
    private bool _initialized;
    private bool _stopped;
    private static long _temporarySequence;

    public BakeCacheStore(string root, string? version = null)
    {
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("Cache root must be absolute.", nameof(root));
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
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
            _initialized = true;
        }
    }

    public byte[]? TryRead(BakeKey key, int maximumPayload = MaximumPayloadBytes,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Initialize();
        var path = FilePath(key);
        if (!File.Exists(path)) return null;
        try
        {
            var limit = maximumPayload == MaximumProductBytes ? MaximumProductBytes + 1024 * 1024 : MaximumCompressedTileBytes;
            var file = ReadBounded(path, limit);
            EnterCache(cancellationToken);
            try
            {
                ThrowIfStopped();
                var bytes = DecodeFile(file, maximumPayload);
                cancellationToken.ThrowIfCancellationRequested();
                return bytes;
            }
            finally { Monitor.Exit(_sync); }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or ArgumentException or OverflowException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Remove(key, cancellationToken);
            return null;
        }
    }

    public void Write(BakeKey key, byte[] bytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Initialize();
        EnterCache(cancellationToken);
        try
        {
            ThrowIfStopped();
            var encoded = EncodeFile(bytes);
            cancellationToken.ThrowIfCancellationRequested();
            if (key.Module != "map" && encoded.Length > MaximumCompressedTileBytes)
                throw new InvalidDataException("Compressed tile exceeds 8 MiB on disk; split the generated tile.");
            var path = FilePath(key);
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

    public void Remove(BakeKey key, CancellationToken cancellationToken = default)
    {
        EnterCache(cancellationToken);
        try
        {
            ThrowIfStopped();
            var path = FilePath(key);
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

    private string FilePath(BakeKey key)
    {
        var detail = key.Detail.Split('/');
        var product = key.Module == "terrain" ? "terrain" : key.Module == "resident" ? "resident" : key.Module == "map" ? "map" : key.Module.StartsWith("interior.", StringComparison.Ordinal) ? "interior" :
            detail[0] == "C" ? "collision" : "geometry";
        var segments = new List<string> { _root, Segment(key.Map.Value), product, Segment(key.Module) };
        segments.AddRange(detail.Select(Segment));
        var path = Path.Combine(segments.ToArray()) + ".tbin";
        for (var current = path; current != _root; current = Path.GetDirectoryName(current)!)
            if ((File.Exists(current) || Directory.Exists(current)) && IsLink(current))
                throw new IOException("Cache paths must not be reparse points.");
        return path;
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
