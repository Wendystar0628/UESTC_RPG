using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Collision;
using Campus.Kernel.Lod;
using Campus.Kernel.Stream;

namespace Campus.Kernel.Bake;

public sealed partial class BakeService
{
    /// <summary>Explicit CPU batch; interactive startup keeps its small background catalogue.</summary>
    internal async Task BakeOffline(IMapContext context)
    {
        var map = Mount(context);
        var directory = await map.GetPreparedModuleTilesAsync(context);
        if (!_options.MapOnly)
        {
            // Use exactly the stream tree and module index: conservative module directories
            // may contain cells outside the world, which the stream can never request.
            var tree = new LodTree(context.Descriptor.Bounds, new StreamSettings());
            var catalog = ModuleTileCatalog.Build(directory);
            var pending = new Stack<StreamTile>();
            pending.Push(StreamTile.Root);
            while (pending.TryPop(out var tile))
            {
                foreach (var module in catalog.Modules(tile))
                    Schedule(map, BakeRequestKeys.Tile(context.Descriptor.Id, module, tile.Tile, tile.Level), BakePriority.Background);
                foreach (var child in tree.Children(tile).Reverse()) pending.Push(child);
            }

            foreach (var binding in map.Data.Value.InteriorBindings.OrderBy(value => value.ProviderId, StringComparer.Ordinal))
                foreach (var feature in binding.FeatureIds.OrderBy(value => value.Value, StringComparer.Ordinal))
                {
                    var entry = context.World.Interiors.Get(feature);
                    if (entry is null || !entry.Open) continue;
                    foreach (var floor in entry.OpenStoreys.Order())
                        foreach (var detail in new[] { "IF", "IS" })
                            Schedule(map, new(context.Descriptor.Id, "interior." + binding.ProviderId,
                                FormattableString.Invariant($"{feature.Value}/{floor}/{detail}")), BakePriority.Background);
                }

            // Collision streaming uses this same authored map tile range.
            var descriptor = context.Descriptor;
            for (var x = descriptor.TileMin.X; x <= descriptor.TileMax.X; x++)
                for (var z = descriptor.TileMin.Z; z <= descriptor.TileMax.Z; z++)
                {
                    var key = CollisionBakeProducer.Key(descriptor.Id, new(x, z));
                    if (map.Producers.Any(producer => producer.Value.CanBuild(key)))
                        Schedule(map, key, BakePriority.Background);
                }
        }
        var mapKey = new BakeKey(context.Descriptor.Id, "map", "base");
        if (map.Producers.Any(producer => producer.Value.CanBuild(mapKey)))
            Schedule(map, mapKey, BakePriority.Background);

        while (true)
        {
            if (TryTakeError(out var failure)) throw new InvalidDataException(failure);
            var progress = Progress;
            if (progress.Done == progress.Total) break;
            await Task.Delay(50);
        }
        if (TryTakeError(out var lastFailure)) throw new InvalidDataException(lastFailure);
    }
}
