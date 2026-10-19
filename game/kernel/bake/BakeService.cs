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
[KernelService(typeof(BakeService), Owner = "WP-02a")]
[KernelService(typeof(IBakeService), Owner = "WP-02a")]
[KernelService(typeof(IBakeContextSource), Owner = "WP-02a")]
public sealed partial class BakeService : IBakeService, IBakeContextSource, IDisposable
{
    private readonly IMapService? _maps;
    private readonly IAppFlow? _flow;
    private readonly BakeOptions _options;
    private readonly BakeQueue _queue;
    private readonly BakeCacheStore _cache;
    private readonly Dictionary<MapId, MapBuild> _contexts = [];
    private readonly object _sync = new();
    private long _cacheGeneration;
    private Task? _clearing;
    private bool _stopped;
    private bool _exiting;

    public BakeService(IMapService maps, IAppFlow flow) : this(BakeOptions.FromApplication())
    {
        _maps = maps; _flow = flow;
        ApplyPhase(flow.State);
        flow.Changed += PhaseChanged;
    }

    internal BakeService(BakeOptions options)
    {
        _options = options;
        _queue = new(_options.MaximumWorkers);
        _cache = new(_options.CacheRoot);
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
            var entries = registry.ForMap(context.Descriptor.Id.Value);
            // Instantiate all generators on the main thread. Their Build methods receive only a captured pure context.
            var modules = entries.Where(e => e.Kind == "content").ToDictionary(e => e.Id,
                e => new ContentModuleInput((IContentModule)registry.Create(e.Type), CodeHash(e.Type), new WpId(e.Owner), e.Map is null), StringComparer.Ordinal);
            var producers = entries.Where(e => e.Kind == "bake").Select(e =>
                new Producer(e.Id, (IBakeProducer)registry.Create(e.Type), CodeHash(e.Type))).ToArray();
            var interiors = entries.Where(e => e.Kind == "interior").ToDictionary(e => "interior." + e.Id,
                e => new Interior(e.Id, e.IsStub, registry.CreateInteriorProvider(e.Type, context.Descriptor.Id), CodeHash(e.Type), new WpId(e.Owner)), StringComparer.Ordinal);
            // Capture one immutable source snapshot per MapBuild; never load extensions on a worker.
            var catalog = registry.Get<IPaletteCatalogSource>().Catalog;
            var globalInputs = Campus.Kernel.Registry.Kernel.GlobalBuildInputs;
            var groundContributors = GenerationInputCapture.CaptureGroundMaskContributors(registry, context.Descriptor.Id);
            var partLods = GenerationInputCapture.CapturePartLods(registry, context.Descriptor.Id);
            var geometry = new BakeGeometryBuilder(catalog, registry, context.Descriptor.Id);
            var seed = _options.SeedOverride ?? context.Descriptor.Seed;
            var claims = MapClaimCapture.Capture(registry, context.Descriptor.Id);
            var ledger = new Lazy<MapClaimLedger>(() => claims.Build(context.World), LazyThreadSafetyMode.ExecutionAndPublication);
            Func<FeatureId, Aspects, WpId?> claimLookup = (feature, aspects) => ledger.Value.ClaimedBy(feature, aspects);
            var map = new MapBuild(context, modules, interiors, producers, geometry,
                ledger, claimLookup, new(() =>
                {
                    _ = ledger.Value; // Empty maps also resolve their captured declaration before preparation publishes.
                    return Load(context, seed, catalog, interiors, modules, claimLookup, globalInputs, groundContributors, partLods);
                },
                    LazyThreadSafetyMode.ExecutionAndPublication), globalInputs, groundContributors, partLods, catalog);
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
    }

    internal async Task<BakeMapMetadata> PrepareMapData(IMapContext context)
    {
        var map = Mount(context);
        var metadata = await Task.Run(() =>
        {
            map.InteriorCoverage = InspectInteriorCoverage(map);
            var generated = BakeMapMetadataBuilder.Build(map);
            RequireEntranceCoverage(map, generated);
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

    internal void Prebake(IMapContext context)
    {
        var map = Mount(context);
        if (!_options.MapOnly && !_options.Background) return;
        lock (_sync)
        {
            if (map.EnumerationStarted) return;
            map.EnumerationStarted = true;
        }
        _queue.BeginCatalog();
        var thread = new Thread(() =>
        {
            try
            {
                var data = map.Data.Value;
                var basic = map.CreateBuildContext("");
                if (_options.MapOnly)
                {
                    Schedule(map, new(context.Descriptor.Id, "map", "base"), BakePriority.Normal);
                    return;
                }
                var moduleTiles = map.GetPreparedModuleTilesAsync(context).GetAwaiter().GetResult();
                var keys = moduleTiles.SelectMany(pair =>
                {
                    var tiles = pair.Value;
                    return new[] { LodLevel.L4, LodLevel.L3, LodLevel.L2, LodLevel.L1, LodLevel.L0 }.SelectMany(level =>
                        (level == LodLevel.L4 ? new[] { new TileKey(0, 0) } : tiles.Select(t => BakeRequestKeys.Parent(t, level)).Distinct())
                        .Select(tile => BakeRequestKeys.Tile(context.Descriptor.Id, pair.Key, tile, level)));
                }).ToArray();
                // The initial visible rings and map base outrank the distant background catalog.
                foreach (var key in keys.Where(k => BakeRequestKeys.Parse(k).Level == LodLevel.L4 ||
                    BakeRequestKeys.Parse(k).Level is LodLevel.L2 or LodLevel.L3 && Distance(k, context) <= 800f * 800f)
                    .OrderBy(k => Distance(k, context))) Schedule(map, key, BakePriority.KnownDestination);
                var mapKey = new BakeKey(context.Descriptor.Id, "map", "base");
                if (map.Producers.Any(p => p.Value.CanBuild(mapKey))) Schedule(map, mapKey, BakePriority.Normal);
                foreach (var key in keys.OrderBy(k => LevelOrder(k)).ThenBy(k => Distance(k, context)).ThenBy(k => k.Module, StringComparer.Ordinal))
                    Schedule(map, key, BakePriority.Background);
                foreach (var binding in data.InteriorBindings.OrderBy(binding => binding.ProviderId, StringComparer.Ordinal))
                {
                    var name = "interior." + binding.ProviderId;
                    var interior = map.Interiors[name];
                    foreach (var feature in binding.FeatureIds.OrderBy(f => f.Value, StringComparer.Ordinal))
                        foreach (var storey in InteriorAssembly.ValidatedLayout(interior.Value, binding, basic, feature).OpenStoreys.Order())
                            foreach (var detail in new[] { "IF", "IS" })
                                Schedule(map, new(context.Descriptor.Id, name, FormattableString.Invariant($"{feature.Value}/{storey}/{detail}")), BakePriority.Background);
                }
            }
            catch (Exception) when (!map.Active) { }
            catch (Exception error)
            {
                lock (_sync)
                    if (map.Active && !_stopped)
                        _queue.Ensure(new(context.Descriptor.Id, "bake.catalog", "enumerate"), BakePriority.Background,
                            () => throw new InvalidDataException("Background catalog failed: " + error.Message, error));
            }
            finally { map.EnumerationComplete = true; _queue.EndCatalog(); }
        })
        { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "Campus bake catalog" };
        thread.Start();
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
        var codeHash = module?.CodeHash ?? interior?.CodeHash ?? matches[0].CodeHash;
        var context = module is not null ? map.GetPreparedContext(map.Context, key.Module) : map.CreateBuildContext(codeHash);
        var runtimeCode = RuntimeCodeHash();
        var dependencies = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (key.Detail.StartsWith("C/", StringComparison.Ordinal))
        {
            var area = BakeInputFingerprint.Area(context, key);
            var moduleTiles = map.GetPreparedModuleTilesAsync(map.Context).GetAwaiter().GetResult();
            foreach (var (id, tiles) in moduleTiles)
                if (tiles.Any(tile =>
                    (tile.X + 1) * TileGrid.BaseEdge >= area.Min.X && tile.X * TileGrid.BaseEdge <= area.Max.X &&
                    (tile.Z + 1) * TileGrid.BaseEdge >= area.Min.Y && tile.Z * TileGrid.BaseEdge <= area.Max.Y)) dependencies[id] = map.Modules[id].CodeHash;
            foreach (var (id, source) in map.Interiors)
                if (source.Value.FeatureIds.Any(feature => context.World.Features.TryGetValue(feature, out var value) &&
                    value.Bounds.Min.X <= area.Max.X && value.Bounds.Max.X >= area.Min.X &&
                    value.Bounds.Min.Z <= area.Max.Y && value.Bounds.Max.Z >= area.Min.Y)) dependencies[id] = source.CodeHash;
            dependencies.Add("collision", GenerationInputAssembly.CodeVersion(typeof(Campus.Gen.Collision.CollisionGeometry).Assembly));
        }
        var inputHash = BakeInputFingerprint.Compute(context, key, runtimeCode, map.SourceIndex(context), dependencies);
        var identity = BakeInputFingerprint.Identity(key, inputHash);
        if (!_options.Live)
        {
            var cached = _cache.TryRead(key,
                key.Module == "map" ? BakeCacheStore.MaximumProductBytes : BakeCacheStore.MaximumPayloadBytes, cancellation);
            if (cached is not null)
            {
                try
                {
                    var product = Decode(identity, key, cached);
                    cancellation.ThrowIfCancellationRequested();
                    RequireInteriorProduct(map, key, product);
                    return product;
                }
                catch (InvalidDataException) { _cache.Remove(key, cancellation); }
            }
            if (_options.Strict) throw new InvalidDataException($"Strict cache miss: {identity}");
        }
        var buildContext = module is not null ? context : map.CreateBuildContext(codeHash, inputHash);
        var built = module is not null ? map.Geometry!.Build(key, buildContext, module.Value, diagnostics => map.Collect(key.Module, "geometry", diagnostics)) :
            interior is not null ? BuildInterior(map, key, buildContext, interior.Value) : matches[0].Value.Build(key, buildContext);
        cancellation.ThrowIfCancellationRequested();
        var bytes = Encode(identity, built);
        cancellation.ThrowIfCancellationRequested();
        // Validate exactly the bytes about to be written before publishing Ready; malformed producer output is terminal.
        var validated = Decode(identity, key, bytes);
        cancellation.ThrowIfCancellationRequested();
        RequireInteriorProduct(map, key, validated);
        if (!_options.Live && generation == Interlocked.Read(ref _cacheGeneration))
        {
            _cache.Write(key, bytes, cancellation);
        }
        return validated;
    }

    private LoadedMap Load(IMapContext context, uint seed, PaletteCatalog catalog, IReadOnlyDictionary<string, Interior> interiors,
        IReadOnlyDictionary<string, ContentModuleInput> modules, Func<FeatureId, Aspects, WpId?> claimLookup, BuildInputSnapshot globalInputs,
        IReadOnlyList<IGroundMaskContributor> groundContributors, PartLodCatalog partLods)
    {
        // Validate the final post-overlay world once, before cache hits or geometry can bypass it.
        // Claims add diagnostics only; validation failures must remain terminal for this map load.
        BuildingOverlapValidator.Validate(context.World, claimLookup);
        if (!_options.Live)
        {
            _cache.Initialize();
        }
        var inputs = BakeInputFingerprint.ReadInputs(context);
        // These instances belong to this MapBuild, never the registry's global cache or an older map's jobs.
        // Configure once before the Lazy publishes them to any consumer of this map's generation context.
        var providers = interiors.Values.ToDictionary(interior => interior.ProviderId, interior => interior.Value, StringComparer.Ordinal);
        IReadOnlyList<InteriorProviderBinding> bindings = inputs.TryGetValue("interiors.json", out var interiorCatalogBytes)
            ? InteriorAssembly.ConfigureForWorld(interiorCatalogBytes, providers, context.World) : [];
        var activeInteriors = new ReadOnlyDictionary<string, IInteriorProvider>(bindings.ToDictionary(binding => binding.ProviderId,
            binding => providers[binding.ProviderId], StringComparer.Ordinal));
        var prepass = new BuildContext
        {
            World = context.World,
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
        // This Lazy is completed before any catalog Tiles, geometry Build, or producer Collide can run.
        var windows = BakeWindowPreparation.Prepare(prepass, modules);
        return new(seed, inputs, bindings, activeInteriors, windows);
    }

    private static byte[] Encode(string identity, object product) => BakeProductCodec.Encode(identity, product);
    private static object Decode(string identity, BakeKey key, byte[] bytes) => BakeProductCodec.Decode(identity, key, bytes);
    private static void RequireInteriorProduct(MapBuild map, BakeKey key, object product)
    {
        if (map.Interiors.ContainsKey(key.Module) && (product is not TileBundle tile || tile.Stats.Surfaces == 0 || tile.Stats.Triangles == 0))
            throw new InvalidDataException($"Open interior product is empty: {key.Map.Value}/{key.Module}/{key.Detail}.");
    }
    private static string RuntimeCodeHash() => typeof(BakeService).Module.ModuleVersionId.ToString("N") + ":" +
        typeof(GenerationInputAssembly).Module.ModuleVersionId.ToString("N");
    private static string CodeHash(Type type) => GenerationInputAssembly.CodeVersion(type);
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
            var context = map.CreateBuildContext(interior.CodeHash);
            var floors = 0;
            var entrances = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in binding.FeatureIds.OrderBy(feature => feature.Value, StringComparer.Ordinal))
            {
                var layout = InteriorAssembly.ValidatedLayout(interior.Value, binding, context, feature);
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
    private void ApplyPhase(AppState state) => _queue.SetLimit(state switch
    {
        AppState.Boot or AppState.Title => _options.MaximumWorkers,
        AppState.Entering or AppState.Playing or AppState.Leaving or AppState.Quitting => _options.PlayingWorkers,
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    });
    private static int LevelOrder(BakeKey key) => BakeRequestKeys.Parse(key).Level switch
    {
        LodLevel.L4 => 0,
        LodLevel.L3 => 1,
        LodLevel.L2 => 2,
        LodLevel.L1 => 3,
        LodLevel.L0 => 4,
        _ => throw new InvalidDataException("Unknown LOD.")
    };
    private static float Distance(BakeKey key, IMapContext map)
    {
        var request = BakeRequestKeys.Parse(key);
        var size = TileGrid.Edge(request.Level);
        var center = TileGrid.Origin(request.Tile, request.Level) + new System.Numerics.Vector2(size * 0.5f);
        var start = map.Descriptor.Title?.T0.Position;
        var origin = start is { } p ? new System.Numerics.Vector2(p.X, p.Z) : map.Descriptor.Center;
        return System.Numerics.Vector2.DistanceSquared(center, origin);
    }
    /// <summary>Called by the stage only after saving has allowed process exit. Remaining workers own CPU snapshots only.</summary>
    public void StopForExit()
    {
        Stop();
        // Drain only native compression/file publication, never the current pure generation body.
        _cache.Stop();
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
    }

    public void Dispose()
    {
        Stop();
        // Ordinary disposal/restart still drains owned workers; process exit has closed native access above.
        if (!_exiting) _queue.Dispose();
    }

    internal sealed record Producer(string Id, IBakeProducer Value, string CodeHash);
    internal sealed record Interior(string ProviderId, bool IsStub, IInteriorProvider Value, string CodeHash, WpId Owner);
    internal sealed record CollectedDiagnostic(MapId Map, WpId Owner, string Module, string Stage, BuildDiagnostic Diagnostic);
    internal sealed record InteriorCoverage(string ProviderId, bool IsStub, int CatalogFeatures, int BoundFeatures,
        int CatalogFeatureStoreys, int ProducedFeatureStoreys, int AuthoredEntrances, int ProducedEntrances);
    internal sealed record LoadedMap(uint Seed, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Inputs,
        IReadOnlyList<InteriorProviderBinding> InteriorBindings, IReadOnlyDictionary<string, IInteriorProvider> InteriorProviders,
        BakeWindowPreparation.Result Windows);
    internal sealed partial record MapBuild(IMapContext Context, IReadOnlyDictionary<string, ContentModuleInput> Modules, IReadOnlyDictionary<string, Interior> Interiors, Producer[] Producers,
        BakeGeometryBuilder Geometry, Lazy<MapClaimLedger> Claims, Func<FeatureId, Aspects, WpId?> ClaimLookup, Lazy<LoadedMap> Data,
        BuildInputSnapshot GlobalInputs, IReadOnlyList<IGroundMaskContributor> GroundMaskContributors, PartLodCatalog PartLods, PaletteCatalog Catalog)
    {
        private readonly object _sourceIndexLock = new();
        private BakeSourceIndex? _sourceIndex;
        internal BakeSourceIndex SourceIndex(BuildContext context)
        {
            lock (_sourceIndexLock) return _sourceIndex ??= new(context);
        }
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
        private IReadOnlyDictionary<string, BuildContext>? _preparedContexts;

        internal BuildContext CreateBuildContext(string codeHash, string dataHash = "")
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
                DataHash = dataHash,
                CodeHash = codeHash,
                InteriorProviders = data.InteriorProviders,
                WindowNumbering = data.Windows.Snapshot,
                GlobalInputs = GlobalInputs,
                GroundMaskContributors = GroundMaskContributors,
                PartLods = PartLods
            };
        }

        internal void CompletePreparation(BakeMapMetadata metadata)
        {
            if (!Active || !Data.IsValueCreated || !Claims.IsValueCreated)
                throw new InvalidOperationException("Map preparation has not completed.");
            var data = Data.Value;
            if (!ReferenceEquals(metadata.Lighting.WindowNumbering, data.Windows.Snapshot))
                throw new InvalidDataException("Lighting publication must retain the prepared window snapshot instance.");
            _preparedContexts = new ReadOnlyDictionary<string, BuildContext>(Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => CreateBuildContext(pair.Value.CodeHash), StringComparer.Ordinal));
            Metadata = metadata;
            PrepareModuleTiles();
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
