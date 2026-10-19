using System.Collections.ObjectModel;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Bake;

public sealed partial class BakeService
{
    public Task<IReadOnlyDictionary<string, IReadOnlyList<TileKey>>> GetPreparedModuleTilesAsync(IMapContext context)
    {
        MapBuild map;
        lock (_sync)
        {
            if (!_contexts.TryGetValue(context.Descriptor.Id, out map!))
                throw new InvalidOperationException("The requested map has no prepared bake context.");
            map.RequirePreparedTiles(context);
        }
        return map.GetPreparedModuleTilesAsync(context);
    }

    internal sealed partial record MapBuild
    {
        private Lazy<Task<IReadOnlyDictionary<string, IReadOnlyList<TileKey>>>>? _moduleTiles;

        internal void PrepareDirectory()
        {
            foreach (var (id, module) in Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                Lifetime.Token.ThrowIfCancellationRequested();
                var tiles = module.Value.Tiles(CreateAssetContext(module.CodeHash)).Distinct().OrderBy(tile => tile.X).ThenBy(tile => tile.Z).ToArray();
                if (tiles.Any(tile => tile.X <= -512 || tile.X >= 512 || tile.Z <= -512 || tile.Z >= 512))
                    throw new InvalidDataException($"Module {id} returned an out-of-range tile.");
                ResidentModuleTiles.Add(id, tiles);
            }
        }

        private void PrepareModuleTiles()
        {
            var contexts = _preparedContexts!;
            _moduleTiles = new(() => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<TileKey>>>(
                new ReadOnlyDictionary<string, IReadOnlyList<TileKey>>(ResidentModuleTiles)), LazyThreadSafetyMode.ExecutionAndPublication);
            foreach (var producer in Producers.Select(producer => producer.Value).OfType<IBakeContentConsumer>())
                producer.Prepare(new(() =>
                {
                    var tiles = GetPreparedModuleTilesAsync(Context).GetAwaiter().GetResult();
                    return Array.AsReadOnly(Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => new PreparedContentModule(pair.Key, pair.Value.Value, contexts[pair.Key], tiles[pair.Key])).ToArray());
                }, LazyThreadSafetyMode.ExecutionAndPublication));
        }

        internal void RequirePreparedTiles(IMapContext context)
        {
            if (!Active || !ReferenceEquals(context, Context) || Metadata is null || _moduleTiles is null)
                throw new InvalidOperationException("Map context identity is stale or its bake preparation is not ready.");
        }

        internal Task<IReadOnlyDictionary<string, IReadOnlyList<TileKey>>> GetPreparedModuleTilesAsync(IMapContext context)
        {
            RequirePreparedTiles(context);
            return _moduleTiles!.Value;
        }
    }
}
