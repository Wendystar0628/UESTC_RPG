using Campus.Gen.BakeInput;
using Campus.Kernel.Registry;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using static Campus.Kernel.Bake.BakeService;

namespace Campus.Kernel.Bake;

internal sealed class BakeInputIdentity
{
    private readonly BakeSourceIndexCache? _cache;
    internal BakeInputIdentity(BakeSourceIndexCache? cache) => _cache = cache;

    private readonly object _sourceIndexLock = new();
    private BakeSourceIndex? _sourceIndex;
    internal BakeSourceIndex SourceIndex(BuildContext context)
    {
        lock (_sourceIndexLock)
            return _sourceIndex ??= new BakeSourceIndex(context, _cache);
    }
    internal void PersistInputIdentities() => Volatile.Read(ref _sourceIndex)?.Persist();

    internal static string SourceVersion(BuildCodeIndex codes, IMapContext context, uint seed, BuildInputSnapshot globals,
        IReadOnlyDictionary<string, ContentModuleInput> modules, IReadOnlyDictionary<string, Interior> interiors, IReadOnlyList<Producer> producers)
    {
        var hash = new BakeInputHash();
        hash.Add(new { BakeOptions.Abi, seed, code = IndexCode(codes, modules, interiors, producers), map = context.Descriptor,
            inputs = context.SourceInputs.Version, globals = globals.Version,
            producers = producers.OrderBy(producer => producer.Id, StringComparer.Ordinal).Select(producer => new { producer.Id, producer.CodeHash }).ToArray(),
            modules = modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray(),
            interiors = interiors.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray() });
        return hash.Finish();
    }

    internal static string ResidentStamp(BuildCodeIndex codes, IMapContext context, uint seed, PaletteCatalog catalog,
        IReadOnlyDictionary<string, Interior> interiors, IReadOnlyDictionary<string, ContentModuleInput> modules,
        BuildInputSnapshot globals, Func<FeatureId, Aspects, string?> claimLookup, IReadOnlyList<Producer> producers)
    {
        var hash = new BakeInputHash();
        hash.Add(new { seed, code = codes.Chain(BakeCodeChains.Shared), map = context.Descriptor,
            producers = producers.OrderBy(producer => producer.Id, StringComparer.Ordinal).Select(producer => new { producer.Id, producer.CodeHash }).ToArray(),
            modules = modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray(),
            interiors = interiors.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray(),
            catalog = catalog.Entries.OrderBy(entry => entry.Index.Value).ToArray(),
            claims = context.World.Features.Keys.OrderBy(id => id.Value, StringComparer.Ordinal).Select(id => new
                { id, owners = Enum.GetValues<Aspects>().Select(aspect => new { aspect, owner = claimLookup(id, aspect) }).ToArray() }).ToArray() });
        foreach (var key in context.SourceInputs.Entries.Keys.Order(StringComparer.Ordinal)) hash.File(context.SourceInputs.Entries, key);
        foreach (var key in globals.Entries.Keys.Order(StringComparer.Ordinal)) hash.File(globals.Entries, key);
        return hash.Finish();
    }

    internal string ProductInputHash(BuildCodeIndex codes, MapBuild map, BuildContext context, BakeKey key)
    {
        var dependencies = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var declared = map.Content.Modules.TryGetValue(key.Module, out var module) ? module.Value.BakeInputs :
            map.Content.Interiors.TryGetValue(key.Module, out var interior) ? interior.Value.BakeInputs :
            map.Content.Producers.Single(producer => producer.Value.CanBuild(key)).Value.Inputs(key);
        if (map.Directory.Auxiliary.TryAuxiliaryInput(key, out var entry, out var sourceCodes, out var lightingStamp))
            return BakeInputFingerprint.Compute(context, key, ProductCode(codes, map, key), map.Inputs.SourceIndex(context), declared, sourceCodes,
                new MapRect(new(entry.Bounds.Min.X, entry.Bounds.Min.Z), new(entry.Bounds.Max.X, entry.Bounds.Max.Z)), lightingStamp);
        if (key.Detail.StartsWith("C/", StringComparison.Ordinal))
        {
            var area = BakeInputFingerprint.Area(context, key);
            var moduleTiles = map.Directory.PreparedTiles;
            foreach (var (id, tiles) in moduleTiles.Modules)
                if (tiles.Any(tile =>
                    (tile.X + 1) * TileGrid.BaseEdge >= area.Min.X && tile.X * TileGrid.BaseEdge <= area.Max.X &&
                    (tile.Z + 1) * TileGrid.BaseEdge >= area.Min.Y && tile.Z * TileGrid.BaseEdge <= area.Max.Y))
                {
                    dependencies[id] = map.Content.Modules[id].CodeHash;
                    declared = Merge(declared, map.Content.Modules[id].Value.CollisionInputs);
                }
            foreach (var (id, source) in map.Content.Interiors)
                if (source.Value.FeatureIds.Any(feature => context.World.Features.TryGetValue(feature, out var value) &&
                    value.Bounds.Min.X <= area.Max.X && value.Bounds.Max.X >= area.Min.X &&
                    value.Bounds.Min.Z <= area.Max.Y && value.Bounds.Max.Z >= area.Min.Y))
                {
                    dependencies[id] = source.CodeHash;
                    declared = Merge(declared, source.Value.CollisionInputs);
                }
        }
        MapRect? interiorArea = null;
        if (map.Content.Interiors.ContainsKey(key.Module))
        {
            var id = new FeatureId(key.Detail.Split('/')[0]);
            if (!context.World.Features.TryGetValue(id, out var feature)) throw new InvalidDataException("Interior source feature is absent.");
            interiorArea = new(new(feature.Bounds.Min.X - TileGrid.BaseEdge, feature.Bounds.Min.Z - TileGrid.BaseEdge),
                new(feature.Bounds.Max.X + TileGrid.BaseEdge, feature.Bounds.Max.Z + TileGrid.BaseEdge));
        }
        return BakeInputFingerprint.Compute(context, key, ProductCode(codes, map, key), map.Inputs.SourceIndex(context), declared, dependencies, interiorArea);
    }

    internal static string IndexCode(BuildCodeIndex codes, IReadOnlyDictionary<string, ContentModuleInput> modules,
        IReadOnlyDictionary<string, Interior> interiors, IReadOnlyList<Producer> producers)
    {
        var hash = new BakeInputHash();
        hash.Add(new
        {
            chains = new[] { BakeCodeChains.Shared, BakeCodeChains.Collision, BakeCodeChains.Nightlight }
                .Select(key => new { key, code = codes.Chain(key) }).ToArray(),
            modules = modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray(),
            interiors = interiors.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray(),
            producers = producers.OrderBy(producer => producer.Id, StringComparer.Ordinal).Select(producer => new { producer.Id, producer.CodeHash }).ToArray()
        });
        return hash.Finish();
    }

    private static string ProductCode(BuildCodeIndex codes, MapBuild map, BakeKey key)
    {
        var chain = map.Content.Modules.ContainsKey(key.Module) || map.Content.Interiors.ContainsKey(key.Module) ? BakeCodeChains.Shared :
            map.Content.Producers.Single(producer => producer.Value.CanBuild(key)).Value.ProductKind switch
            {
                BakeProductKind.Collision => BakeCodeChains.Collision,
                BakeProductKind.Map or BakeProductKind.Vegetation or BakeProductKind.Fauna => BakeCodeChains.Shared,
                BakeProductKind.Nightlight => BakeCodeChains.Nightlight,
                _ => throw new InvalidDataException("Unknown fixed bake payload kind.")
            };
        return codes.Chain(chain);
    }

    private static BakeInputDependencies Merge(BakeInputDependencies first, BakeInputDependencies second) => new(
        first.Kinds | second.Kinds,
        first.MapFiles.Concat(second.MapFiles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
        first.GlobalFiles.Concat(second.GlobalFiles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
}
