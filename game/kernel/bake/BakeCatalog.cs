using Campus.Gen.BakeInput;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using static Campus.Kernel.Bake.BakeService;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Collision;
using Campus.Kernel.Lod;

namespace Campus.Kernel.Bake;

internal sealed class BakeCatalog
{
    private readonly BakeOptions _options;
    private readonly BakeQueue _queue;
    private readonly Action<MapBuild, BakeKey, BakePriority> _schedule;
    private readonly object _catalogSync = new();
    private readonly HashSet<MapBuild> _enumerating = [];
    private readonly List<(MapBuild Map, Thread Thread, Task Completion)> _threads = [];
    private AppState _phase;

    internal BakeCatalog(BakeOptions options, BakeQueue queue, Action<MapBuild, BakeKey, BakePriority> schedule)
    { _options = options; _queue = queue; _schedule = schedule; }

    internal void SetPhase(AppState phase) { lock (_catalogSync) _phase = phase; }
    internal void Forget(MapBuild map)
    {
        lock (_catalogSync)
        {
            _enumerating.Remove(map);
            _threads.RemoveAll(entry => ReferenceEquals(entry.Map, map));
        }
    }

    // The explicit CPU batch and interactive completion enumerate exactly the same products.
    internal static IEnumerable<BakeKey> Keys(MapBuild map, bool mapOnly)
    {
        var context = map.Context;
        var cancellation = map.Lifetime.Token;
        cancellation.ThrowIfCancellationRequested();
        if (!mapOnly)
        {
            var directory = map.Directory.GetPreparedModuleTilesAsync(context).GetAwaiter().GetResult();
            var tree = new LodTree(context.Descriptor.Bounds, new StreamSettings());
            var catalog = ModuleTileCatalog.Build(directory);
            var pending = new Stack<StreamTile>();
            pending.Push(StreamTile.Root);
            while (pending.TryPop(out var tile))
            {
                cancellation.ThrowIfCancellationRequested();
                foreach (var module in catalog.Modules(tile))
                    yield return BakeRequestKeys.Tile(context.Descriptor.Id, module, tile.Tile, tile.Level);
                foreach (var child in tree.Children(tile).Reverse()) pending.Push(child);
            }

            foreach (var entry in directory.Auxiliary)
            {
                cancellation.ThrowIfCancellationRequested();
                yield return entry.Key;
            }

            foreach (var binding in map.Data.Value.InteriorBindings.OrderBy(value => value.ProviderId, StringComparer.Ordinal))
                foreach (var feature in binding.InteriorFeatures.OrderBy(value => value.Value, StringComparer.Ordinal))
                {
                    cancellation.ThrowIfCancellationRequested();
                    var entry = context.World.Interiors.Get(feature);
                    if (entry is null || !entry.Open) continue;
                    foreach (var floor in entry.OpenStoreys.Order())
                        foreach (var detail in new[] { "IF", "IS" })
                            yield return new(context.Descriptor.Id, "interior." + binding.ProviderId,
                                FormattableString.Invariant($"{feature.Value}/{floor}/{detail}"));
                }

            var descriptor = context.Descriptor;
            for (var x = descriptor.TileMin.X; x <= descriptor.TileMax.X; x++)
                for (var z = descriptor.TileMin.Z; z <= descriptor.TileMax.Z; z++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var key = CollisionBakeProducer.Key(descriptor.Id, new(x, z));
                    if (map.Content.Producers.Any(producer => producer.Value.CanBuild(key))) yield return key;
                }
        }
        cancellation.ThrowIfCancellationRequested();
        var mapKey = new BakeKey(context.Descriptor.Id, MapBaker.Module, MapBaker.Detail);
        if (map.Content.Producers.Any(producer => producer.Value.CanBuild(mapKey))) yield return mapKey;
    }

    internal void Queue(MapBuild map)
    {
        foreach (var key in Keys(map, _options.MapOnly)) _schedule(map, key, BakePriority.Background);
    }

    internal Thread? Begin(MapBuild map)
    {
        lock (_catalogSync)
        {
            if (!map.Active || map.Content.Metadata is null || _enumerating.Contains(map) ||
                !_options.Background || _options.MapOnly || _phase is not (AppState.Title or AppState.Playing)) return null;
            _enumerating.Add(map);
            _queue.BeginCatalog();
        }
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Exception? failure = null;
            try { Queue(map); }
            catch (OperationCanceledException) when (map.Lifetime.IsCancellationRequested) { }
            catch (Exception error)
            {
                _queue.RecordFailure(error);
                failure = error;
            }
            finally
            {
                _queue.EndCatalog();
                failure ??= _queue.Failure;
                if (failure is not null) completion.TrySetException(failure);
                else completion.TrySetResult();
            }
        })
        { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "Campus bake catalog" };
        lock (_catalogSync) _threads.Add((map, thread, completion.Task));
        return thread;
    }

    internal Task DrainAsync(MapBuild map)
    {
        lock (_catalogSync)
            return Task.WhenAll(_threads.Where(entry => ReferenceEquals(entry.Map, map)).Select(entry => entry.Completion));
    }

    internal void Drain()
    {
        (MapBuild Map, Thread Thread, Task Completion)[] threads;
        lock (_catalogSync) threads = _threads.ToArray();
        try { Task.WhenAll(threads.Select(entry => entry.Completion)).GetAwaiter().GetResult(); }
        finally
        {
            foreach (var entry in threads)
                if (entry.Thread != Thread.CurrentThread) entry.Thread.Join();
            lock (_catalogSync) _threads.Clear();
        }
    }
}
