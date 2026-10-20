using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Bake;

public sealed partial class BakeService
{
    private string SourceVersion(IMapContext context, uint seed, BuildInputSnapshot globals,
        IReadOnlyDictionary<string, ContentModuleInput> modules, IReadOnlyDictionary<string, Interior> interiors, IReadOnlyList<Producer> producers)
    {
        var hash = new BakeInputHash();
        hash.Add(new { BakeOptions.Abi, seed, code = IndexCode(modules, interiors, producers), map = context.Descriptor,
            inputs = context.SourceInputs.Version, globals = globals.Version,
            producers = producers.OrderBy(producer => producer.Id, StringComparer.Ordinal).Select(producer => new { producer.Id, producer.CodeHash }).ToArray(),
            modules = modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray(),
            interiors = interiors.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray() });
        return hash.Finish();
    }

    private string ResidentStamp(IMapContext context, uint seed, PaletteCatalog catalog,
        IReadOnlyDictionary<string, Interior> interiors, IReadOnlyDictionary<string, ContentModuleInput> modules,
        BuildInputSnapshot globals, Func<FeatureId, Aspects, string?> claimLookup, IReadOnlyList<Producer> producers)
    {
        var hash = new BakeInputHash();
        hash.Add(new { seed, code = _terrain.CodeFingerprints.Chain(BakeCodeChains.Shared), map = context.Descriptor,
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

    private string ProductInputHash(MapBuild map, BuildContext context, BakeKey key)
    {
        var dependencies = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var declared = map.Modules.TryGetValue(key.Module, out var module) ? module.Value.BakeInputs :
            map.Interiors.TryGetValue(key.Module, out var interior) ? interior.Value.BakeInputs :
            map.Producers.Single(producer => producer.Value.CanBuild(key)).Value.Inputs(key);
        if (map.TryAuxiliaryInput(key, out var entry, out var sourceCodes, out var lightingStamp))
            return BakeInputFingerprint.Compute(context, key, ProductCode(map, key), map.SourceIndex(context), declared, sourceCodes,
                new MapRect(new(entry.Bounds.Min.X, entry.Bounds.Min.Z), new(entry.Bounds.Max.X, entry.Bounds.Max.Z)), lightingStamp);
        if (key.Detail.StartsWith("C/", StringComparison.Ordinal))
        {
            var area = BakeInputFingerprint.Area(context, key);
            var moduleTiles = map.GetPreparedModuleTilesAsync(map.Context).GetAwaiter().GetResult();
            foreach (var (id, tiles) in moduleTiles.Modules)
                if (tiles.Any(tile =>
                    (tile.X + 1) * TileGrid.BaseEdge >= area.Min.X && tile.X * TileGrid.BaseEdge <= area.Max.X &&
                    (tile.Z + 1) * TileGrid.BaseEdge >= area.Min.Y && tile.Z * TileGrid.BaseEdge <= area.Max.Y))
                {
                    dependencies[id] = map.Modules[id].CodeHash;
                    declared = Merge(declared, map.Modules[id].Value.CollisionInputs);
                }
            foreach (var (id, source) in map.Interiors)
                if (source.Value.FeatureIds.Any(feature => context.World.Features.TryGetValue(feature, out var value) &&
                    value.Bounds.Min.X <= area.Max.X && value.Bounds.Max.X >= area.Min.X &&
                    value.Bounds.Min.Z <= area.Max.Y && value.Bounds.Max.Z >= area.Min.Y))
                {
                    dependencies[id] = source.CodeHash;
                    declared = Merge(declared, source.Value.CollisionInputs);
                }
        }
        MapRect? interiorArea = null;
        if (map.Interiors.ContainsKey(key.Module))
        {
            var id = new FeatureId(key.Detail.Split('/')[0]);
            if (!context.World.Features.TryGetValue(id, out var feature)) throw new InvalidDataException("Interior source feature is absent.");
            interiorArea = new(new(feature.Bounds.Min.X - TileGrid.BaseEdge, feature.Bounds.Min.Z - TileGrid.BaseEdge),
                new(feature.Bounds.Max.X + TileGrid.BaseEdge, feature.Bounds.Max.Z + TileGrid.BaseEdge));
        }
        return BakeInputFingerprint.Compute(context, key, ProductCode(map, key), map.SourceIndex(context), declared, dependencies, interiorArea);
    }

    private string IndexCode(IReadOnlyDictionary<string, ContentModuleInput> modules,
        IReadOnlyDictionary<string, Interior> interiors, IReadOnlyList<Producer> producers)
    {
        var hash = new BakeInputHash();
        hash.Add(new
        {
            chains = new[] { BakeCodeChains.Shared, BakeCodeChains.Collision, BakeCodeChains.Nightlight }
                .Select(key => new { key, code = _terrain.CodeFingerprints.Chain(key) }).ToArray(),
            modules = modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray(),
            interiors = interiors.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { pair.Key, pair.Value.CodeHash }).ToArray(),
            producers = producers.OrderBy(producer => producer.Id, StringComparer.Ordinal).Select(producer => new { producer.Id, producer.CodeHash }).ToArray()
        });
        return hash.Finish();
    }

    private string ProductCode(MapBuild map, BakeKey key)
    {
        var chain = map.Modules.ContainsKey(key.Module) || map.Interiors.ContainsKey(key.Module) ? BakeCodeChains.Shared :
            map.Producers.Single(producer => producer.Value.CanBuild(key)).Value.ProductKind switch
            {
                BakeProductKind.Collision => BakeCodeChains.Collision,
                BakeProductKind.Map or BakeProductKind.Vegetation or BakeProductKind.Fauna => BakeCodeChains.Shared,
                BakeProductKind.Nightlight => BakeCodeChains.Nightlight,
                _ => throw new InvalidDataException("Unknown fixed bake payload kind.")
            };
        return _terrain.CodeFingerprints.Chain(chain);
    }

    private static BakeInputDependencies Merge(BakeInputDependencies first, BakeInputDependencies second) => new(
        first.Kinds | second.Kinds,
        first.MapFiles.Concat(second.MapFiles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
        first.GlobalFiles.Concat(second.GlobalFiles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());

    private void FlushInputIdentities()
    {
        lock (_inputIdentitySync)
        {
            MapBuild[] maps;
            lock (_sync)
            {
                if (_stopped) return;
                maps = _contexts.Values.Where(map => map.Active).ToArray();
            }
            foreach (var map in maps) map.PersistInputIdentities();
        }
    }

    private void PersistInputIdentities(IEnumerable<MapBuild> maps)
    {
        lock (_inputIdentitySync)
            foreach (var map in maps) map.PersistInputIdentities();
    }
}
