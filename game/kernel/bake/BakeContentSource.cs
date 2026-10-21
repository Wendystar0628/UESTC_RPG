using Campus.Gen.BakeInput;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using static Campus.Kernel.Bake.BakeService;
using System.Collections.ObjectModel;
using Campus.Contracts.Parts;

namespace Campus.Kernel.Bake;

internal sealed class BakeContentSource
{
    private readonly IMapContext _context;
    private readonly Lazy<LoadedMap> _data;
    private readonly Func<bool> _active;
    private readonly TerrainCacheService? _terrain;
    private readonly BuildInputSnapshot _globalInputs;
    private readonly IReadOnlyList<IGroundMaskContributor> _groundMaskContributors;
    private readonly PartLodCatalog _partLods;
    private readonly PaletteCatalog _catalog;
    private readonly Func<FeatureId, Aspects, string?> _claimLookup;
    private IReadOnlyDictionary<string, BuildContext>? _preparedContexts;
    private IReadOnlyDictionary<object, BuildContext>? _preparedBuildContexts;
    internal IReadOnlyDictionary<string, ContentModuleInput> Modules { get; }
    internal IReadOnlyDictionary<string, Interior> Interiors { get; }
    internal Producer[] Producers { get; }
    internal BakeGeometryBuilder Geometry { get; }
    internal Dictionary<FeatureId, InteriorLayout> Layouts { get; } = [];
    internal BakeMapMetadata? Metadata { get; private set; }
    internal IReadOnlyList<InteriorCoverage> Coverage { get; set; } = [];

    internal BakeContentSource(IMapContext context, IReadOnlyDictionary<string, ContentModuleInput> modules,
        IReadOnlyDictionary<string, Interior> interiors, Producer[] producers, BakeGeometryBuilder geometry,
        Func<FeatureId, Aspects, string?> claimLookup, Lazy<LoadedMap> data, BuildInputSnapshot globalInputs,
        IReadOnlyList<IGroundMaskContributor> contributors, PartLodCatalog partLods, PaletteCatalog catalog,
        Func<bool> active, TerrainCacheService? terrain)
    {
        _context = context; Modules = modules; Interiors = interiors; Producers = producers; Geometry = geometry;
        _claimLookup = claimLookup; _data = data; _globalInputs = globalInputs; _groundMaskContributors = contributors;
        _partLods = partLods; _catalog = catalog; _active = active; _terrain = terrain;
    }

    internal void CompletePreparation(BakeMapMetadata metadata)
    {
        if (!_active() || !_data.IsValueCreated)
            throw new InvalidOperationException("Map preparation has not completed.");
        if (!ReferenceEquals(metadata.Lighting.WindowNumbering, _data.Value.Windows.Snapshot))
            throw new InvalidDataException("Lighting publication must retain the prepared window snapshot instance.");
        Metadata = metadata;
    }

    internal T GetModule<T>(IMapContext context, string moduleId) where T : class, IContentModule
    {
        _ = GetPreparedContext(context, moduleId);
        return Modules[moduleId].Value as T ??
            throw new InvalidOperationException($"Content module '{moduleId}' does not provide {typeof(T).Name}.");
    }

    internal BuildContext CreateAssetContext(string codeHash) => CreateAssetContext(CreateBuildContext(codeHash));

    internal BuildContext CreateAssetContext(BuildContext context)
    {
        if (_terrain is null) return context;
        var world = _terrain.AssetWorld(context.World);
        return context.Memo(world, () => context.WithWorld(world));
    }
    internal BuildContext CreateBuildContext(string codeHash)
    {
        var data = _data.Value;
        return new()
        {
            World = _context.World,
            Map = _context.Descriptor,
            WorldSeed = data.Seed,
            Catalog = _catalog,
            ClaimedBy = _claimLookup,
            SourceInputs = data.Inputs,
            CodeHash = codeHash,
            InteriorProviders = data.InteriorProviders,
            WindowNumbering = data.Windows.Snapshot,
            InteriorLayouts = new ReadOnlyDictionary<FeatureId, InteriorLayout>(Layouts),
            GlobalInputs = _globalInputs,
            GroundMaskContributors = _groundMaskContributors,
            PartLods = _partLods
        };
    }

    internal void FreezeInputs(BakeMapMetadata metadata)
    {
        if (!_active() || !_data.IsValueCreated || !ReferenceEquals(metadata.Lighting.WindowNumbering, _data.Value.Windows.Snapshot))
            throw new InvalidOperationException("Map preparation inputs are not ready.");
        var contexts = new Dictionary<object, BuildContext>(ReferenceEqualityComparer.Instance);
        foreach (var module in Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            contexts.Add(module.Value.Value, CreateBuildContext(module.Value.CodeHash));
        foreach (var interior in Interiors.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            contexts.Add(interior.Value.Value, CreateBuildContext(interior.Value.CodeHash));
        foreach (var producer in Producers.OrderBy(producer => producer.Id, StringComparer.Ordinal))
            contexts.Add(producer.Value, CreateBuildContext(producer.CodeHash));
        _preparedBuildContexts = new ReadOnlyDictionary<object, BuildContext>(contexts);
        _preparedContexts = new ReadOnlyDictionary<string, BuildContext>(Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => contexts[pair.Value.Value], StringComparer.Ordinal));
    }

    internal IReadOnlyDictionary<string, BuildContext> ModuleContexts => _preparedContexts!;

    internal BuildContext GetPreparedBuildContext(object producer)
    {
        if (_preparedBuildContexts is null || !_preparedBuildContexts.TryGetValue(producer, out var context))
            throw new InvalidOperationException("The producer has no frozen build context.");
        return context;
    }

    internal BuildContext GetPreparedContext(IMapContext context, string moduleId)
    {
        RequirePrepared(context);
        return _preparedContexts!.TryGetValue(moduleId, out var prepared) ? prepared :
            throw new InvalidOperationException($"No prepared content module '{moduleId}' in map {context.Descriptor.Id.Value}.");
    }

    internal GroundCoverData GetPreparedGroundCover(IMapContext context)
    {
        RequirePrepared(context);
        return Metadata!.GroundCover;
    }

    private void RequirePrepared(IMapContext context)
    {
        if (!_active() || context.Descriptor.Id != _context.Descriptor.Id || !ReferenceEquals(context, _context) ||
            Metadata is null || _preparedContexts is null)
            throw new InvalidOperationException("Map context identity is stale or its bake preparation is not ready.");
    }
}
