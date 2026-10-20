using Campus.Pipeline.Map;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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











public sealed partial class BakeService : IBakeService, IBakeContextSource, IDisposable, IRegistryExitParticipant
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
    private long _cacheGeneration;
    private Task? _clearing;
    private bool _stopped;
    private bool _exiting;
    private AppState _phase;

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
            return map.GetPreparedContext(context, moduleId);
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
            Interlocked.Increment(ref _cacheGeneration);
            _clearing = Task.Run(_cache.Clear);
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
                SourceVersion(context, seed, globalInputs, modules, interiors, producers),
                IndexCode(modules, interiors, producers));
            var map = new MapBuild(context, modules, interiors, producers, geometry,
                ledger, claimLookup, new(() => Load(context, seed, catalog, interiors, modules, claimLookup, globalInputs,
                    groundContributors, partLods, sourceCache, producers), LazyThreadSafetyMode.ExecutionAndPublication),
                globalInputs, groundContributors, partLods, catalog) { TerrainCache = _terrain, SourceCache = sourceCache };
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
        var metadata = await Task.Run(() =>
        {
            var data = map.Data.Value;
            foreach (var layout in data.Layouts) map.InteriorLayouts.Add(layout.Feature, layout);
            if (data.Resident is { } cached)
            {
                map.InteriorCoverage = cached.Coverage;
                map.PrepareDirectory(cached.Metadata, cached.Directory);
                return cached.Metadata;
            }
            map.InteriorCoverage = InspectInteriorCoverage(map);
            var generated = BakeMapMetadataBuilder.Build(map);
            RequireEntranceCoverage(map, generated);
            map.PrepareDirectory(generated);
            PrepareResidentTerrain(map);
            var terrainPoints = _terrain.SaveResidentPoints(context.World, data.ResidentStamp);
            _terrain.WriteProduct(new(context.Descriptor.Id, "resident", "meta"), data.ResidentStamp,
                new BakeResidentData(generated, data.Windows.Metadata, map.InteriorCoverage.ToArray(), map.InteriorLayouts.Values.ToArray(),
                    map.PreparedTiles, terrainPoints));
            return generated;
        });
        lock (_sync)
        {
            if (!map.Active || !_contexts.TryGetValue(context.Descriptor.Id, out var current) || !ReferenceEquals(map, current))
                throw new OperationCanceledException("Map unloaded while preparing resident metadata.");
            map.CompletePreparation(metadata);
        }
        return metadata;
    }

    private IBakeHandle Schedule(MapBuild map, BakeKey key, BakePriority priority)
    {
        lock (_sync)
        {
            if (_stopped || !map.Active) throw new OperationCanceledException("Map unloaded before request scheduling.");
            return _queue.Ensure(key, priority, () => Build(map, key));
        }
    }

    internal object Build(MapBuild map, BakeKey key)
    {
        var cancellation = map.Lifetime.Token;
        cancellation.ThrowIfCancellationRequested();
        var generation = Interlocked.Read(ref _cacheGeneration);
        Task? clearing;
        lock (_sync) clearing = _clearing;
        clearing?.WaitAsync(cancellation).GetAwaiter().GetResult();
        cancellation.ThrowIfCancellationRequested();
        _ = map.Data.Value;
        cancellation.ThrowIfCancellationRequested();
        var module = map.Modules.GetValueOrDefault(key.Module);
        var interior = map.Interiors.GetValueOrDefault(key.Module);
        var matches = map.Producers.Where(p => p.Value.CanBuild(key)).ToArray();
        if (matches.Length > 1 || (module is not null || interior is not null) && matches.Length != 0)
            throw new InvalidDataException("More than one producer claims this bake key.");
        if (module is null && interior is null && matches.Length == 0) throw new InvalidDataException("No bake producer is registered for this key.");
        var payloadClass = module is not null || interior is not null ? BakePayloadClass.Tile : matches[0].Value.PayloadClass;
        var context = map.GetPreparedBuildContext(module is not null ? module.Value :
            interior is not null ? interior.Value : matches[0].Value);
        var sources = map.SourceIndex(context);
        var inputHash = sources.TryProduct(key, out var preparedHash) ? preparedHash : ProductInputHash(map, context, key);
        sources.RememberProduct(key, inputHash);
        var identity = BakeInputFingerprint.Identity(key, inputHash);
        PrepareProductTerrain(map, key);
        if (!_options.Live)
        {
            var cached = _cache.TryReadParts(key, payloadClass, payload =>
            {
                var product = Decode(payloadClass, identity, key, payload);
                RequireInteriorProduct(map, key, product);
                RequireAuxiliaryProduct(map, key, product);
                return product;
            }, cancellation);
            if (cached is not null)
            {
                map.PipelineStatistics?.Add(cached, map.Geometry);
                return cached;
            }
            if (_options.Strict) throw new InvalidDataException($"Strict cache miss: {identity}");
        }
        // Only a cache miss needs the evaluator; cached products keep the published read-only world.
        var buildContext = map.CreateAssetContext(context);
        var built = module is not null ? map.Geometry!.Build(key, buildContext, module.Value, diagnostics => map.Collect(key.Module, "geometry", diagnostics)) :
            interior is not null ? BuildInterior(map, key, buildContext, interior.Value) : matches[0].Value.Build(key, buildContext, inputHash);
        cancellation.ThrowIfCancellationRequested();
        var bytes = Encode(payloadClass, identity, built, cancellation);
        cancellation.ThrowIfCancellationRequested();
        // Validate exactly the bytes about to be written before publishing Ready; malformed producer output is terminal.
        var validated = Decode(payloadClass, identity, key, bytes);
        cancellation.ThrowIfCancellationRequested();
        RequireInteriorProduct(map, key, validated);
        RequireAuxiliaryProduct(map, key, validated);
        map.PipelineStatistics?.Add(validated, map.Geometry);
        if (!_options.Live && generation == Interlocked.Read(ref _cacheGeneration))
        {
            _cache.WriteParts(key, payloadClass, bytes, cancellation);
        }
        return validated;
    }

    private LoadedMap Load(IMapContext context, uint seed, PaletteCatalog catalog, IReadOnlyDictionary<string, Interior> interiors,
        IReadOnlyDictionary<string, ContentModuleInput> modules, Func<FeatureId, Aspects, string?> claimLookup, BuildInputSnapshot globalInputs,
        IReadOnlyList<IGroundMaskContributor> groundContributors, PartLodCatalog partLods, BakeSourceIndexCache sourceCache, Producer[] producers)
    {
        if (!_options.Live)
        {
            _cache.Initialize();
        }
        var inputs = BakeInputFingerprint.ReadInputs(context);
        var publication = context as IMapInteriorPreparation
            ?? throw new InvalidOperationException("The loading map does not support interior publication.");
        // These instances belong to this MapBuild, never the registry's global cache or an older map's jobs.
        // Configure once before the Lazy publishes them to any consumer of this map's generation context.
        var providers = interiors.Values.ToDictionary(interior => interior.ProviderId, interior => interior.Value, StringComparer.Ordinal);
        IReadOnlyList<InteriorProviderBinding> bindings = inputs.TryGetValue("interiors.json", out var interiorCatalogBytes)
            ? InteriorAssembly.ConfigureForWorld(interiorCatalogBytes, providers, context.World) : [];
        var activeInteriors = new ReadOnlyDictionary<string, IInteriorProvider>(bindings.ToDictionary(binding => binding.ProviderId,
            binding => providers[binding.ProviderId], StringComparer.Ordinal));
        var residentStamp = sourceCache.ResolveResidentStamp(() =>
        {
            // Revalidate changed inputs before publishing a replacement resident version.
            BuildingOverlapValidator.Validate(context.World, claimLookup);
            return ResidentStamp(context, seed, catalog, interiors, modules, globalInputs, claimLookup, producers);
        });
        var resident = _terrain.ReadProduct<BakeResidentData>(new(context.Descriptor.Id, "resident", "meta"), residentStamp, candidate =>
        {
            BakeMapMetadataBuilder.ValidateResident(candidate, context.Descriptor.Id, modules, producers);
            if (!_terrain.RestoreResidentPoints(context.World, residentStamp, candidate.TerrainPoints))
                throw new InvalidDataException("Resident metadata has missing terrain points.");
        });
        if (resident is not null)
        {
            publication.PublishInteriors(InteriorAssembly.IndexEntries(resident.Layouts, bindings, activeInteriors));
            return new(seed, inputs, bindings, activeInteriors,
                new(resident.Metadata.Lighting.WindowNumbering
                    ?? throw new InvalidDataException("Resident metadata has no window numbering snapshot."), resident.Windows), resident.Layouts, resident, residentStamp);
        }
        _terrain.RequireMiss(new(context.Descriptor.Id, "resident", "meta"));
        var prepass = new BuildContext
        {
            CodeHash = residentStamp, WindowNumbering = new((context.Descriptor).Id, new string('0', 64), []),
            World = _terrain.AssetWorld(context.World),
            Map = context.Descriptor,
            WorldSeed = seed,
            Catalog = catalog,
            ClaimedBy = claimLookup,
            SourceInputs = inputs,
            InteriorProviders = activeInteriors,
            GlobalInputs = globalInputs,
            GroundMaskContributors = groundContributors,
            PartLods = partLods
        };
        var layouts = bindings.SelectMany(binding => binding.FeatureIds.Select(feature =>
            InteriorAssembly.ValidatedLayout(activeInteriors[binding.ProviderId], binding, prepass, feature))).ToArray();
        publication.PublishInteriors(InteriorAssembly.IndexEntries(layouts, bindings, activeInteriors));
        prepass = prepass.WithWorld(_terrain.AssetWorld(context.World));
        // This Lazy is completed before any catalog Tiles, geometry Build, or producer Collide can run.
        var windows = BakeWindowPreparation.Prepare(prepass, modules);
        return new(seed, inputs, bindings, activeInteriors, windows, layouts, null, residentStamp);
    }

    private IReadOnlyList<byte[]> Encode(BakePayloadClass payloadClass, string identity, object product, CancellationToken cancellation) =>
        BakeProductCodec.EncodeParts(payloadClass, identity, product, bytes => _cache.Fits(payloadClass, bytes, cancellation));
    private static object Decode(BakePayloadClass payloadClass, string identity, BakeKey key, IReadOnlyList<byte[]> parts) => BakeProductCodec.DecodeParts(payloadClass, identity, key, parts);
    private static void RequireInteriorProduct(MapBuild map, BakeKey key, object product)
    {
        if (map.Interiors.ContainsKey(key.Module) && (product is not TileBundle tile || tile.Stats.Surfaces == 0 || tile.Stats.Triangles == 0))
            throw new InvalidDataException($"Open interior product is empty: {key.Map.Value}/{key.Module}/{key.Detail}.");
    }
    private static TileBundle BuildInterior(MapBuild map, BakeKey key, BuildContext context, IInteriorProvider provider)
    {
        var fields = key.Detail.Split('/');
        if (fields.Length != 3 || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var storey))
            throw new ArgumentException("Invalid interior bake detail.");
        var feature = new FeatureId(fields[0]);
        var binding = map.Data.Value.InteriorBindings.SingleOrDefault(binding => "interior." + binding.ProviderId == key.Module);
        if (binding is null || !binding.FeatureIds.Contains(feature) || !provider.FeatureIds.Contains(feature))
            throw new InvalidDataException("Interior feature is not bound to this map's authoritative catalog.");
        var detail = fields[2] switch { "IF" => InteriorDetail.Full, "IS" => InteriorDetail.Shell, _ => throw new InvalidDataException("Interior detail must be IF or IS.") };
        var layout = InteriorAssembly.ValidatedLayout(provider, binding, context, feature);
        if (!layout.OpenStoreys.Contains(storey)) throw new InvalidDataException("Interior storey is not open.");
        var center = layout.Bounds.Center;
        var tile = TileGrid.At(center);
        return map.Geometry!.BuildOutput(key, context, provider.BuildStorey(context, feature, storey, detail), tile, LodLevel.L0,
            diagnostics => map.Collect(key.Module, "interior-geometry", diagnostics, feature));
    }

    private static IReadOnlyList<InteriorCoverage> InspectInteriorCoverage(MapBuild map)
    {
        var data = map.Data.Value;
        var coverage = new List<InteriorCoverage>();
        foreach (var binding in data.InteriorBindings.OrderBy(binding => binding.ProviderId, StringComparer.Ordinal))
        {
            var interior = map.Interiors["interior." + binding.ProviderId];
            var floors = 0;
            var entrances = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in binding.FeatureIds.OrderBy(feature => feature.Value, StringComparer.Ordinal))
            {
                var layout = map.InteriorLayouts[feature];
                floors += layout.OpenStoreys.Count;
                foreach (var entrance in layout.Entrances) entrances.Add(System.Text.Json.JsonSerializer.Serialize(entrance, ContractJson.Options));
            }
            coverage.Add(new(binding.ProviderId, interior.IsStub, binding.FeatureIds.Count, interior.Value.FeatureIds.Count,
                checked(binding.FeatureIds.Count * binding.OpenFloors.Count), floors, binding.AuthoredEntranceCount, entrances.Count));
        }
        return coverage.AsReadOnly();
    }

    private static void RequireEntranceCoverage(MapBuild map, BakeMapMetadata metadata)
    {
        foreach (var coverage in map.InteriorCoverage)
            if (metadata.Entrances.Entrances.Count(item => item.Module == "interior." + coverage.ProviderId) < coverage.ProducedEntrances)
                throw new InvalidDataException($"Entrance inventory omits configured interior entries: {coverage.ProviderId}.");
    }
    private void PhaseChanged(AppState previous, AppState next) => ApplyPhase(next);
    private void ApplyPhase(AppState state)
    {
        _phase = state;
        _queue.SetLimit(state switch
        {
            AppState.Boot or AppState.Title => _options.MaximumWorkers,
            AppState.Entering or AppState.Playing or AppState.Leaving or AppState.Quitting => _options.PlayingWorkers,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        });
        if (state is not (AppState.Title or AppState.Playing)) return;
        MapBuild[] maps;
        lock (_sync) maps = _contexts.Values.Where(map => map.Active && map.Metadata is not null).ToArray();
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

    internal sealed record Producer(string Id, IBakeProducer Value, string CodeHash);
    internal sealed record Interior(string ProviderId, bool IsStub, IInteriorProvider Value, string CodeHash, string Owner);
    internal sealed record CollectedDiagnostic(MapId Map, string Owner, string Module, string Stage, BuildDiagnostic Diagnostic);
    internal sealed record InteriorCoverage(string ProviderId, bool IsStub, int CatalogFeatures, int BoundFeatures,
        int CatalogFeatureStoreys, int ProducedFeatureStoreys, int AuthoredEntrances, int ProducedEntrances);
    internal sealed record LoadedMap(uint Seed, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Inputs,
        IReadOnlyList<InteriorProviderBinding> InteriorBindings, IReadOnlyDictionary<string, IInteriorProvider> InteriorProviders,
        BakeWindowPreparation.Result Windows, IReadOnlyList<InteriorLayout> Layouts, BakeResidentData? Resident = null, string ResidentStamp = "");

    internal sealed partial record MapBuild(IMapContext Context, IReadOnlyDictionary<string, ContentModuleInput> Modules, IReadOnlyDictionary<string, Interior> Interiors, Producer[] Producers,
        BakeGeometryBuilder Geometry, Lazy<MapClaimLedger> Claims, Func<FeatureId, Aspects, string?> ClaimLookup, Lazy<LoadedMap> Data,
        BuildInputSnapshot GlobalInputs, IReadOnlyList<IGroundMaskContributor> GroundMaskContributors, PartLodCatalog PartLods, PaletteCatalog Catalog)
    {
        internal TerrainCacheService? TerrainCache { get; init; }
        internal BakePipelineCatalog? PipelineStatistics;
        internal BakeSourceIndexCache? SourceCache { get; init; }
        internal BuildContext CreateAssetContext(string codeHash) => CreateAssetContext(CreateBuildContext(codeHash));

        internal BuildContext CreateAssetContext(BuildContext context)
        {
            if (TerrainCache is null) return context;
            var world = TerrainCache.AssetWorld(context.World);
            return context.Memo(world, () => context.WithWorld(world));
        }
        private readonly object _sourceIndexLock = new();
        private BakeSourceIndex? _sourceIndex;
        internal BakeSourceIndex SourceIndex(BuildContext context)
        {
            lock (_sourceIndexLock)
                return _sourceIndex ??= new BakeSourceIndex(context, SourceCache);
        }
        internal void PersistInputIdentities() => Volatile.Read(ref _sourceIndex)?.Persist();
        internal ConcurrentQueue<CollectedDiagnostic> Diagnostics { get; } = new();

        internal void Collect(string module, string stage, IReadOnlyList<BuildDiagnostic> diagnostics, FeatureId? feature = null)
        {
            if (!Active || diagnostics.Count == 0) return;
            foreach (var diagnostic in diagnostics) diagnostic.Validate();
            var owner = Modules.TryGetValue(module, out var content) ? content.Owner : Interiors[module].Owner;
            foreach (var diagnostic in diagnostics)
                Diagnostics.Enqueue(new(Context.Descriptor.Id, owner, module, stage,
                    diagnostic.Feature is null && feature is not null ? diagnostic with { Feature = feature } : diagnostic));
        }

        public volatile bool EnumerationComplete;
        public bool EnumerationStarted;
        public volatile bool Active = true;
        internal CancellationTokenSource Lifetime { get; } = new();
        public BakeMapMetadata? Metadata { get; private set; }
        public IReadOnlyList<InteriorCoverage> InteriorCoverage { get; set; } = [];
        internal Dictionary<FeatureId, InteriorLayout> InteriorLayouts { get; } = [];
        private IReadOnlyDictionary<string, BuildContext>? _preparedContexts;
        private IReadOnlyDictionary<object, BuildContext>? _preparedBuildContexts;

        internal BuildContext CreateBuildContext(string codeHash)
        {
            var data = Data.Value;
            return new()
            {
                World = Context.World,
                Map = Context.Descriptor,
                WorldSeed = data.Seed,
                Catalog = Catalog,
                ClaimedBy = ClaimLookup,
                SourceInputs = data.Inputs,
                CodeHash = codeHash,
                InteriorProviders = data.InteriorProviders,
                WindowNumbering = data.Windows.Snapshot,
                InteriorLayouts = new ReadOnlyDictionary<FeatureId, InteriorLayout>(InteriorLayouts),
                GlobalInputs = GlobalInputs,
                GroundMaskContributors = GroundMaskContributors,
                PartLods = PartLods
            };
        }

        internal void CompletePreparation(BakeMapMetadata metadata)
        {
            if (!Active || !Data.IsValueCreated)
                throw new InvalidOperationException("Map preparation has not completed.");
            var data = Data.Value;
            if (!ReferenceEquals(metadata.Lighting.WindowNumbering, data.Windows.Snapshot))
                throw new InvalidDataException("Lighting publication must retain the prepared window snapshot instance.");
            Metadata = metadata;
            PublishDirectory();
        }

        private void FreezeInputs(BakeMapMetadata metadata)
        {
            if (!Active || !Data.IsValueCreated || !ReferenceEquals(metadata.Lighting.WindowNumbering, Data.Value.Windows.Snapshot))
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

        internal BuildContext GetPreparedBuildContext(object producer)
        {
            if (!Active || _preparedBuildContexts is null || !_preparedBuildContexts.TryGetValue(producer, out var context))
                throw new InvalidOperationException("The producer has no frozen build context.");
            return context;
        }

        internal BuildContext GetPreparedContext(IMapContext context, string moduleId)
        {
            if (!Active || context.Descriptor.Id != Context.Descriptor.Id || !ReferenceEquals(context, Context) ||
                Metadata is null || _preparedContexts is null)
                throw new InvalidOperationException("Map context identity is stale or its bake preparation is not ready.");
            return _preparedContexts.TryGetValue(moduleId, out var prepared) ? prepared :
                throw new InvalidOperationException($"No prepared content module '{moduleId}' in map {context.Descriptor.Id.Value}.");
        }
    }
}
