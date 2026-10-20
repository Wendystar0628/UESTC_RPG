using Campus.Gen.BakeInput;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using static Campus.Kernel.Bake.BakeService;
using System.Collections.ObjectModel;

namespace Campus.Kernel.Bake;

internal sealed class BakeModuleTiles
{
    private readonly IMapContext _context;
    private readonly BakeContentSource _content;
    private readonly CancellationToken _lifetime;
    private readonly Func<bool> _active;
    internal BakeAuxiliaryInputs Auxiliary { get; } = new();

    internal BakeModuleTiles(IMapContext context, BakeContentSource content, CancellationToken lifetime, Func<bool> active)
    { _context = context; _content = content; _lifetime = lifetime; _active = active; }

    private Task<PreparedBakeTiles>? _moduleTiles;
    private PreparedBakeTiles? _preparedTiles;

    internal void Prepare(BakeMapMetadata metadata, PreparedBakeTiles? restored = null)
    {
        _content.FreezeInputs(metadata);
        var moduleTiles = new Dictionary<string, IReadOnlyList<TileKey>>(StringComparer.Ordinal);
        if (restored is null)
            foreach (var (id, module) in _content.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                _lifetime.ThrowIfCancellationRequested();
                var context = _content.CreateAssetContext(_content.ModuleContexts[id]);
                var tiles = module.Value.Tiles(context).Distinct().OrderBy(tile => tile.X).ThenBy(tile => tile.Z).ToArray();
                ValidateTiles(id, tiles);
                moduleTiles.Add(id, Array.AsReadOnly(tiles));
            }
        else
            foreach (var pair in restored.Modules)
                moduleTiles.Add(pair.Key, Array.AsReadOnly(pair.Value.ToArray()));
        if (moduleTiles.Count != _content.Modules.Count || _content.Modules.Keys.Any(id => !moduleTiles.ContainsKey(id)))
            throw new InvalidDataException("Resident metadata has no complete module directory.");

        var modules = new ReadOnlyDictionary<string, IReadOnlyList<TileKey>>(moduleTiles);
        var contexts = _content.ModuleContexts;
        var prepared = new Lazy<IReadOnlyList<PreparedContentModule>>(() => Array.AsReadOnly(_content.Modules
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new PreparedContentModule(pair.Key, pair.Value.Value, contexts[pair.Key], modules[pair.Key])).ToArray()),
            LazyThreadSafetyMode.ExecutionAndPublication);
        foreach (var consumer in _content.Producers.Select(producer => producer.Value).OfType<IBakeContentConsumer>())
            consumer.Prepare(prepared, metadata.Lighting);
        var auxiliary = restored?.Auxiliary.ToArray() ?? _content.Producers.OrderBy(producer => producer.Id, StringComparer.Ordinal)
            .SelectMany(producer => producer.Value.Tiles(_content.CreateAssetContext(_content.GetPreparedBuildContext(producer.Value))))
            .OrderBy(entry => entry.Key.Module, StringComparer.Ordinal).ThenBy(entry => entry.Key.Detail, StringComparer.Ordinal).ToArray();
        ValidateAuxiliary(auxiliary, _context.Descriptor.Id, _content.Modules, _content.Producers);
        _preparedTiles = new(modules, Array.AsReadOnly(auxiliary));
        Auxiliary.Prepare(PreparedTiles, _content, _context, metadata.Lighting);
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
        if (!_active() || !ReferenceEquals(context, _context) || _content.Metadata is null || _moduleTiles is null)
            throw new InvalidOperationException("Map context identity is stale or its bake preparation is not ready.");
    }

    internal Task<PreparedBakeTiles> GetPreparedModuleTilesAsync(IMapContext context)
    {
        RequirePreparedTiles(context);
        return _moduleTiles!;
    }
}
