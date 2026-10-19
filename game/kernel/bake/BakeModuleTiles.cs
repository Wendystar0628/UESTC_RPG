using System.Collections.ObjectModel;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Bake;

public sealed partial class BakeService
{
    public Task<PreparedBakeTiles> GetPreparedModuleTilesAsync(IMapContext context)
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
        private Task<PreparedBakeTiles>? _moduleTiles;
        private PreparedBakeTiles? _preparedTiles;

        internal void PrepareDirectory(BakeMapMetadata metadata, PreparedBakeTiles? restored = null)
        {
            FreezeInputs(metadata);
            var moduleTiles = new Dictionary<string, IReadOnlyList<TileKey>>(StringComparer.Ordinal);
            if (restored is null)
                foreach (var (id, module) in Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    Lifetime.Token.ThrowIfCancellationRequested();
                    var tiles = module.Value.Tiles(CreateAssetContext(module.CodeHash)).Distinct().OrderBy(tile => tile.X).ThenBy(tile => tile.Z).ToArray();
                    ValidateTiles(id, tiles);
                    moduleTiles.Add(id, Array.AsReadOnly(tiles));
                }
            else
                foreach (var pair in restored.Modules)
                    moduleTiles.Add(pair.Key, Array.AsReadOnly(pair.Value.ToArray()));
            if (moduleTiles.Count != Modules.Count || Modules.Keys.Any(id => !moduleTiles.ContainsKey(id)))
                throw new InvalidDataException("Resident metadata has no complete module directory.");

            var modules = new ReadOnlyDictionary<string, IReadOnlyList<TileKey>>(moduleTiles);
            var contexts = _preparedContexts!;
            var prepared = new Lazy<IReadOnlyList<PreparedContentModule>>(() => Array.AsReadOnly(Modules
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new PreparedContentModule(pair.Key, pair.Value.Value, contexts[pair.Key], modules[pair.Key])).ToArray()),
                LazyThreadSafetyMode.ExecutionAndPublication);
            foreach (var consumer in Producers.Select(producer => producer.Value).OfType<IBakeContentConsumer>())
                consumer.Prepare(prepared, metadata.Lighting);
            var auxiliary = restored?.Auxiliary.ToArray() ?? Producers.OrderBy(producer => producer.Id, StringComparer.Ordinal)
                .SelectMany(producer => producer.Value.Tiles(CreateAssetContext(producer.CodeHash)))
                .OrderBy(entry => entry.Key.Module, StringComparer.Ordinal).ThenBy(entry => entry.Key.Detail, StringComparer.Ordinal).ToArray();
            ValidateAuxiliary(auxiliary, Context.Descriptor.Id, Modules, Producers);
            _preparedTiles = new(modules, Array.AsReadOnly(auxiliary));
            PrepareAuxiliaryInputs(metadata.Lighting);
        }

        internal static void ValidateTiles(string id, IReadOnlyList<TileKey> tiles)
        {
            if (tiles is null || tiles.Distinct().Count() != tiles.Count ||
                tiles.Any(tile => tile.X <= -512 || tile.X >= 512 || tile.Z <= -512 || tile.Z >= 512))
                throw new InvalidDataException($"Module {id} returned an invalid tile directory.");
        }

        internal static void ValidateAuxiliary(IReadOnlyList<BakeTileEntry> entries, MapId map,
            IReadOnlyDictionary<string, ContentModuleInput> modules, IReadOnlyList<Producer> producers)
        {
            if (entries.Any(entry => entry is null) || entries.Select(entry => entry.Key).Distinct().Count() != entries.Count)
                throw new InvalidDataException("Duplicate auxiliary bake key.");
            foreach (var entry in entries)
            {
                var request = BakeRequestKeys.Parse(entry.Key);
                if (entry.Key.Map != map || request.Collision || request.Level != LodLevel.L0 ||
                    modules.ContainsKey(entry.Key.Module) ||
                    entry.Key.Module is not (BakeModuleIds.VegetationTrees or BakeModuleIds.VegetationGrass or BakeModuleIds.FaunaRuntime or BakeModuleIds.NightlightLamps or BakeModuleIds.NightlightWindows) ||
                    producers.Count(producer => producer.Value.CanBuild(entry.Key)) != 1 ||
                    !VectorMath.Finite(entry.Bounds.Min) || !VectorMath.Finite(entry.Bounds.Max) ||
                    entry.Bounds.Min.X > entry.Bounds.Max.X || entry.Bounds.Min.Y > entry.Bounds.Max.Y || entry.Bounds.Min.Z > entry.Bounds.Max.Z)
                    throw new InvalidDataException("Invalid auxiliary bake directory entry.");
                ValidateTiles(entry.Key.Module, [request.Tile]);
            }
        }

        internal PreparedBakeTiles PreparedTiles => _preparedTiles ?? throw new InvalidOperationException("Bake directory has not been frozen.");
        internal void PublishDirectory() => _moduleTiles = Task.FromResult(PreparedTiles);

        internal void RequirePreparedTiles(IMapContext context)
        {
            if (!Active || !ReferenceEquals(context, Context) || Metadata is null || _moduleTiles is null)
                throw new InvalidOperationException("Map context identity is stale or its bake preparation is not ready.");
        }

        internal Task<PreparedBakeTiles> GetPreparedModuleTilesAsync(IMapContext context)
        {
            RequirePreparedTiles(context);
            return _moduleTiles!;
        }
    }
}
