using Campus.Gen.BakeInput;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Collision;
using Campus.Kernel.Lod;

namespace Campus.Kernel.Bake;

public sealed partial class BakeService
{
    // The explicit CPU batch and interactive completion enumerate exactly the same products.
    internal static IEnumerable<BakeKey> CatalogKeys(MapBuild map, bool mapOnly)
    {
        var context = map.Context;
        var cancellation = map.Lifetime.Token;
        cancellation.ThrowIfCancellationRequested();
        if (!mapOnly)
        {
            var directory = map.GetPreparedModuleTilesAsync(context).GetAwaiter().GetResult();
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
                    if (map.Producers.Any(producer => producer.Value.CanBuild(key))) yield return key;
                }
        }
        cancellation.ThrowIfCancellationRequested();
        var mapKey = new BakeKey(context.Descriptor.Id, MapBaker.Module, MapBaker.Detail);
        if (map.Producers.Any(producer => producer.Value.CanBuild(mapKey))) yield return mapKey;
    }

    private void QueueCatalog(MapBuild map)
    {
        foreach (var key in CatalogKeys(map, _options.MapOnly)) Schedule(map, key, BakePriority.Background);
    }

    internal void Prebake(IMapContext context)
    {
        var map = Mount(context);
        if (!_options.MapOnly && !_options.Background) return;
        var mapKey = new BakeKey(context.Descriptor.Id, MapBaker.Module, MapBaker.Detail);
        if (map.Producers.Any(producer => producer.Value.CanBuild(mapKey))) Schedule(map, mapKey, BakePriority.Normal);
        StartBackgroundCatalog(map);
    }

    private void StartBackgroundCatalog(MapBuild map)
    {
        lock (_sync)
        {
            if (_stopped || !map.Active || map.Metadata is null || map.EnumerationStarted ||
                !_options.Background || _options.MapOnly || _phase is not (AppState.Title or AppState.Playing)) return;
            map.EnumerationStarted = true;
            _queue.BeginCatalog();
        }
        var thread = new Thread(() =>
        {
            try { QueueCatalog(map); }
            catch (Exception) when (!map.Active) { }
            catch (Exception error)
            {
                lock (_sync)
                    if (map.Active && !_stopped)
                        _queue.Ensure(new(map.Context.Descriptor.Id, "bake.catalog", "enumerate"), BakePriority.Background,
                            () => throw new InvalidDataException("Background catalog failed: " + error.Message, error));
            }
            finally { map.EnumerationComplete = true; _queue.EndCatalog(); }
        })
        { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "Campus bake catalog" };
        thread.Start();
    }
}
