using System.Text;
using Godot;

namespace Campus.Kernel.Bake;

internal static class BakeBootStore
{
    private static readonly SemaphoreSlim Loads = new(3, 3);
    public static string FileName(string identity) => BakeCacheStore.Hash(Encoding.UTF8.GetBytes(identity)) + ".res";

    public static byte[]? TryRead(string bootRoot, string identity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!bootRoot.StartsWith("res://", StringComparison.Ordinal))
            throw new InvalidDataException("Boot resources must come from res://.");
        var path = bootRoot.TrimEnd('/') + "/" + FileName(identity);
        if (!ResourceLoader.Exists(path)) return null;
        Loads.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var error = ResourceLoader.LoadThreadedRequest(path, "", false, ResourceLoader.CacheMode.Ignore);
            if (error != Error.Ok) throw new IOException($"Boot tile request failed: {path}: {error}");
            while (true)
            {
                switch (ResourceLoader.LoadThreadedGetStatus(path))
                {
                    case ResourceLoader.ThreadLoadStatus.InProgress:
                        Thread.Sleep(5);
                        continue;
                    case ResourceLoader.ThreadLoadStatus.Loaded:
                        using (var resource = ResourceLoader.LoadThreadedGet(path))
                        {
                            // Accepted native requests must drain before cancellation can leave the engine boundary.
                            cancellationToken.ThrowIfCancellationRequested();
                            if (resource is not BootTileResource tile || tile.Payload.Length is <= 0 or > BakeCacheStore.MaximumPayloadBytes)
                                throw new InvalidDataException($"Invalid boot tile payload: {path}");
                            return tile.Payload;
                        }
                    case ResourceLoader.ThreadLoadStatus.Failed:
                    case ResourceLoader.ThreadLoadStatus.InvalidResource:
                        throw new InvalidDataException($"Boot tile could not load: {path}");
                    default: throw new InvalidOperationException("Unknown threaded resource load state.");
                }
            }
        }
        finally { Loads.Release(); }
    }
}
