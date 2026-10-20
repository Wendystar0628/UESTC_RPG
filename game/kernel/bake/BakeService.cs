using Campus.Gen.BakeInput;
using Campus.Contracts.Content;
using System.Collections.Concurrent;

using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using Campus.Kernel.Stubs;

namespace Campus.Kernel.Bake;

/// <summary>One path for live generation and per-cell local caching. Ready always means validated CPU data.</summary>
[KernelService(typeof(BakeService))]
[KernelService(typeof(IBakeService))]
[KernelService(typeof(IBakeContextSource))]
[KernelService(typeof(IMapContentSource))]

public sealed class BakeService : IBakeService, IBakeContextSource, IMapContentSource, IDisposable, IRegistryExitParticipant
{
    private readonly IMapService? _maps;
    private readonly IAppFlow? _flow;
    private readonly BakeOptions _options;
    private readonly BakeQueue _queue;
    private readonly BakeCacheStore _cache;
    private readonly TerrainCacheService _terrain;
    private readonly BuildInputSnapshot _globalInputs;
    private readonly Dictionary<MapId, MapBuild> _contexts = [];
    private readonly object _sync = new();
    private readonly object _inputIdentitySync = new();
    private readonly BakeProductBuilder _products;
    private readonly BakeMapPreparation _preparation;
    private readonly BakeCatalog _catalog;
    private bool _stopped;
    private bool _exiting;

    public BakeService(IMapService maps, IAppFlow flow, TerrainCacheService terrain, GlobalBuildInputs globalInputs)
        : this(BakeOptions.FromApplication(), terrain, globalInputs.Snapshot)
    {
        _maps = maps; _flow = flow;
        ApplyPhase(flow.State);
        flow.Changed += PhaseChanged;
    }

    internal BakeService(BakeOptions options, TerrainCacheService terrain, BuildInputSnapshot globalInputs)
    {
        _options = options;
        _globalInputs = globalInputs;
        _queue = new(_options.MaximumWorkers, FlushInputIdentities);
        _terrain = terrain;
        _cache = terrain.Store;
        _products = new(options, terrain);
        _preparation = new(options, terrain);
        _catalog = new(options, _queue, (map, key, priority) => Schedule(map, key, priority), ReportCatalogFailure);
        ApplyPhase(AppState.Boot);
    }

    public BakeProgress Progress => _queue.Progress;
    public long CacheBytes => _cache.CacheBytes;
    internal bool TryTakeError(out string? error) => _queue.Failures.TryDequeue(out error);

    internal bool TryTakeDiagnostic(out CollectedDiagnostic? diagnostic)
    {
        lock (_sync)
            foreach (var map in _contexts.Values)
                if (map.Active && map.Diagnostics.TryDequeue(out diagnostic)) return true;
        diagnostic = null;
        return false;
    }

    public BuildContext GetPreparedContext(IMapContext context, string moduleId)
    {
        lock (_sync)
        {
            if (!_contexts.TryGetValue(context.Descriptor.Id, out var map))
                throw new InvalidOperationException("The requested map has no prepared bake context.");
            return map.Content.GetPreparedContext(context, moduleId);
        }
    }

    public IBakeHandle Ensure(BakeKey key, BakePriority priority)
    {
        MapBuild? map;
        lock (_sync) _contexts.TryGetValue(key.Map, out map);
        if (map is null)
        {
            var current = _maps?.Context;
            if (current is null || current.Descriptor.Id != key.Map)
                return _queue.Ensure(key, priority, () => throw new InvalidOperationException("Requested map is not mounted."));
            map = Mount(current);
        }
        return Schedule(map, key, priority);
    }

    public void Clear()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_stopped, this);
            _products.Clear();
        }
    }
    public void ClearFailures() => _queue.ClearFailures();

    internal MapBuild Mount(IMapContext context)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_stopped, this);
            if (_contexts.TryGetValue(context.Descriptor.Id, out var existing))
            {
                if (!ReferenceEquals(existing.Context, context))
                    throw new InvalidOperationException("Unload the preceding map context before preparing another version of the same map.");
                return existing;
            }
            var registry = Campus.Kernel.Registry.Kernel.Services;
            // Instantiate all generators on the main thread. Their Build methods receive only a captured pure context.
            var modules = registry.Query<IContentModule>(RegistrationKind.Content, context.Descriptor.Id).ToDictionary(e => e.Id,
                e => new ContentModuleInput((IContentModule)registry.Create(e.Type, context.Descriptor.Id), _terrain.CodeFingerprints.Type(e.Type), e.Owner, e.Map is null), StringComparer.Ordinal);
            var producers = registry.Query<IBakeProducer>(RegistrationKind.Bake, context.Descriptor.Id).Select(e =>
                new Producer(e.Id, (IBakeProducer)registry.Create(e.Type, context.Descriptor.Id), _terrain.CodeFingerprints.Type(e.Type))).ToArray();
            var registrations = registry.ForMap(context.Descriptor.Id.Value);
            var interiorBindings = context.SourceInputs.Entries.TryGetValue("interiors.json", out var interiorCatalog)
                ? InteriorAssembly.ResolveRegistrations(interiorCatalog, registrations) : [];
            var interiors = interiorBindings.ToDictionary(binding => "interior." + binding.ProviderId, binding =>
            {
                var declaration = binding.Registration;
                var value = typeof(IInteriorProviderFactory).IsAssignableFrom(declaration.Type)
                    ? registry.CreateInterior(declaration.Type, binding.ProviderId, context.Descriptor.Id)
                    : (IInteriorProvider)registry.Create(declaration.Type, context.Descriptor.Id);
                return new Interior(binding.ProviderId, declaration.IsStub, value,
                    _terrain.CodeFingerprints.Type(declaration.Type), declaration.Owner);
            }, StringComparer.Ordinal);
            // Capture one immutable source snapshot per MapBuild; never load extensions on a worker.
            var catalog = registry.Get<IPaletteCatalogSource>().Catalog;
            var globalInputs = _globalInputs;
            var groundContributors = GenerationInputCapture.CaptureGroundMaskContributors(registry, context.Descriptor.Id);
            var partLods = GenerationInputCapture.CapturePartLods(registry, context.Descriptor.Id);
            var geometry = new BakeGeometryBuilder(catalog, registry, context.Descriptor.Id);
            var seed = _options.SeedOverride ?? context.Descriptor.Seed;
            var claims = MapClaimCapture.Capture(registry, context.Descriptor.Id);
            var ledger = new Lazy<MapClaimLedger>(() => claims.Build(context.World), LazyThreadSafetyMode.ExecutionAndPublication);
            Func<FeatureId, Aspects, string?> claimLookup = (feature, aspects) => ledger.Value.ClaimedBy(feature, aspects);
            var sourceCache = new BakeSourceIndexCache(_terrain, context.Descriptor.Id,
                BakeInputIdentity.SourceVersion(_terrain.CodeFingerprints, context, seed, globalInputs, modules, interiors, producers),
                BakeInputIdentity.IndexCode(_terrain.CodeFingerprints, modules, interiors, producers));
            var map = new MapBuild(context, modules, interiors, producers, geometry,
                claimLookup, new(() => _preparation.Load(context, seed, catalog, interiors, modules, claimLookup, globalInputs,
                    groundContributors, partLods, sourceCache, producers), LazyThreadSafetyMode.ExecutionAndPublication),
                globalInputs, groundContributors, partLods, catalog, _terrain, sourceCache);
            _contexts.Add(context.Descriptor.Id, map);
            return map;
        }
    }

    internal void Unmount(MapId map)
    {
        MapBuild? removed;
        lock (_sync)
        {
            _contexts.Remove(map, out removed);
            if (removed is not null) removed.Active = false;
        }
        _queue.ForgetMap(map);
        if (removed is not null) _catalog.Forget(removed);
        removed?.Lifetime.Cancel(); // Forget first: cancellation is not a failed active-map build.
        if (removed is not null)
        {
            PersistInputIdentities([removed]);
            _terrain.Unload(removed.Context.World);
        }
    }

    internal async Task<BakeMapMetadata> PrepareMapData(IMapContext context)
    {
        var map = Mount(context);
        var metadata = await Task.Run(() => _preparation.Prepare(map));
        lock (_sync)
        {
            if (!map.Active || !_contexts.TryGetValue(context.Descriptor.Id, out var current) || !ReferenceEquals(map, current))
                throw new OperationCanceledException("Map unloaded while preparing resident metadata.");
            map.Content.CompletePreparation(metadata);
            map.Directory.PublishDirectory();
        }
        return metadata;
    }

    private IBakeHandle Schedule(MapBuild map, BakeKey key, BakePriority priority)
    {
        lock (_sync)
        {
            if (_stopped || !map.Active) throw new OperationCanceledException("Map unloaded before request scheduling.");
            return _queue.Ensure(key, priority, () => _products.Build(map, key));
        }
    }

    private void PhaseChanged(AppState previous, AppState next) => ApplyPhase(next);
    private void ApplyPhase(AppState state)
    {
        _catalog.SetPhase(state);
        _queue.SetLimit(state switch
        {
            AppState.Boot or AppState.Title => _options.MaximumWorkers,
            AppState.Entering or AppState.Playing or AppState.Leaving or AppState.Quitting => _options.PlayingWorkers,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        });
        if (state is not (AppState.Title or AppState.Playing)) return;
        MapBuild[] maps;
        lock (_sync) maps = _contexts.Values.Where(map => map.Active && map.Content.Metadata is not null).ToArray();
        foreach (var map in maps) StartBackgroundCatalog(map);
    }
    /// <summary>Called by the registry only after saving has allowed process exit. Remaining workers own CPU snapshots only.</summary>
    public void PrepareForExit()
    {
        Stop();
        // Drain only native compression/file publication, never the current pure generation body.
        _terrain.Dispose();
        _exiting = true;
    }

    private void Stop()
    {
        if (_flow is not null) _flow.Changed -= PhaseChanged;
        MapBuild[] maps;
        lock (_sync)
        {
            if (_stopped) return;
            _stopped = true;
            maps = _contexts.Values.ToArray();
            foreach (var map in maps) map.Active = false;
        }
        foreach (var map in maps)
        {
            _queue.ForgetMap(map.Context.Descriptor.Id);
            map.Lifetime.Cancel();
        }
        _queue.Stop();
        PersistInputIdentities(maps);
    }

    public void Dispose()
    {
        Stop();
        // Ordinary disposal/restart still drains owned workers; process exit has closed native access above.
        if (!_exiting)
        {
            _queue.Dispose();
            PersistInputIdentities(_contexts.Values.ToArray());
        }
    }

    public T GetModule<T>(IMapContext context, string moduleId) where T : class, IContentModule
    {
        ArgumentNullException.ThrowIfNull(context);
        lock (_sync)
        {
            if (!_contexts.TryGetValue(context.Descriptor.Id, out var map))
                throw new InvalidOperationException("The requested map has no prepared bake context.");
            // Reuse the same preparation/identity checks as worker context publication.
            return map.Content.GetModule<T>(context, moduleId);
        }
    }
    public Task<PreparedBakeTiles> GetPreparedModuleTilesAsync(IMapContext context)
    {
        MapBuild map;
        lock (_sync)
        {
            if (!_contexts.TryGetValue(context.Descriptor.Id, out map!))
                throw new InvalidOperationException("The requested map has no prepared bake context.");
            map.Directory.RequirePreparedTiles(context);
        }
        return map.Directory.GetPreparedModuleTilesAsync(context);
    }

    internal void Prebake(IMapContext context)
    {
        var map = Mount(context);
        if (!_options.MapOnly && !_options.Background) return;
        var mapKey = new BakeKey(context.Descriptor.Id, MapBaker.Module, MapBaker.Detail);
        if (map.Content.Producers.Any(producer => producer.Value.CanBuild(mapKey))) Schedule(map, mapKey, BakePriority.Normal);
        StartBackgroundCatalog(map);
    }

    private void StartBackgroundCatalog(MapBuild map)
    {
        Thread? enumeration;
        lock (_sync)
        {
            if (_stopped) return;
            enumeration = _catalog.Begin(map);
        }
        enumeration?.Start();
    }

    private void ReportCatalogFailure(MapBuild map, Exception error)
    {
        lock (_sync)
            if (map.Active && !_stopped)
                _queue.Ensure(new(map.Context.Descriptor.Id, "bake.catalog", "enumerate"), BakePriority.Background,
                    () => throw new InvalidDataException("Background catalog failed: " + error.Message, error));
    }

    internal Task BakeOffline(IMapContext context, BakePipelineCatalog? pipelines = null) =>
        new BakeOffline(Mount(context), pipelines, _catalog, _queue).Run();

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
            foreach (var map in maps) map.Inputs.PersistInputIdentities();
        }
    }

    private void PersistInputIdentities(IEnumerable<MapBuild> maps)
    {
        lock (_inputIdentitySync)
            foreach (var map in maps) map.Inputs.PersistInputIdentities();
    }

    internal sealed record Producer(string Id, IBakeProducer Value, string CodeHash);
    internal sealed record Interior(string ProviderId, bool IsStub, IInteriorProvider Value, string CodeHash, string Owner);
    internal sealed record CollectedDiagnostic(MapId Map, string Owner, string Module, string Stage, BuildDiagnostic Diagnostic);
    internal sealed record InteriorCoverage(string ProviderId, bool IsStub, int CatalogFeatures, int BoundFeatures,
        int CatalogFeatureStoreys, int ProducedFeatureStoreys, int AuthoredEntrances, int ProducedEntrances);
    internal sealed record LoadedMap(uint Seed, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Inputs,
        IReadOnlyList<InteriorProviderBinding> InteriorBindings, IReadOnlyDictionary<string, IInteriorProvider> InteriorProviders,
        BakeWindowPreparation.Result Windows, IReadOnlyList<InteriorLayout> Layouts, BakeResidentData? Resident = null, string ResidentStamp = "")
    {
        public IReadOnlyList<OutdoorAccessArea> OutdoorAccess { get; init; } = [];
    }

    internal sealed class MapBuild
    {
        internal IMapContext Context { get; }
        internal Lazy<LoadedMap> Data { get; }
        internal BakeContentSource Content { get; }
        internal BakeModuleTiles Directory { get; }
        internal BakeInputIdentity Inputs { get; }
        internal BakePipelineCatalog? PipelineStatistics;
        internal volatile bool Active = true;
        internal CancellationTokenSource Lifetime { get; } = new();
        internal ConcurrentQueue<CollectedDiagnostic> Diagnostics { get; } = new();

        internal MapBuild(IMapContext context, IReadOnlyDictionary<string, ContentModuleInput> modules,
            IReadOnlyDictionary<string, Interior> interiors, Producer[] producers, BakeGeometryBuilder geometry,
            Func<FeatureId, Aspects, string?> claimLookup, Lazy<LoadedMap> data,
            BuildInputSnapshot globalInputs, IReadOnlyList<IGroundMaskContributor> contributors,
            PartLodCatalog partLods, PaletteCatalog catalog, TerrainCacheService? terrain, BakeSourceIndexCache? sourceCache)
        {
            Context = context; Data = data; Inputs = new(sourceCache);
            Content = new(context, modules, interiors, producers, geometry, claimLookup, data,
                globalInputs, contributors, partLods, catalog, () => Active, terrain);
            Directory = new(context, Content, Lifetime.Token, () => Active);
        }

        internal void Collect(string module, string stage, IReadOnlyList<BuildDiagnostic> diagnostics, FeatureId? feature = null)
        {
            if (!Active || diagnostics.Count == 0) return;
            foreach (var diagnostic in diagnostics) diagnostic.Validate();
            var owner = Content.Modules.TryGetValue(module, out var content) ? content.Owner : Content.Interiors[module].Owner;
            foreach (var diagnostic in diagnostics)
                Diagnostics.Enqueue(new(Context.Descriptor.Id, owner, module, stage,
                    diagnostic.Feature is null && feature is not null ? diagnostic with { Feature = feature } : diagnostic));
        }
    }
}
