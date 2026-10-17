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
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Bake;

/// <summary>One path for live generation, local caching and reference baking. Ready always means validated CPU data.</summary>
public sealed class BakeService : IBakeService, IBakeContextSource, IDisposable
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
    public bool IsReference => _options.Reference;
    public bool IsBatch => _options.Reference || _options.Verify || _options.MapOnly;
    public bool IsVerification => _options.Verify;
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
        Interlocked.Increment(ref _cacheGeneration);
        lock (_sync) _clearing = Task.Run(_cache.Clear);
    }
    public void ClearFailures() => _queue.ClearFailures();

    internal MapBuild Mount(IMapContext context)
    {
        lock (_sync)
        {
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
                e => new Module((IContentModule)registry.Create(e.Type), CodeHash(e.Type), new WpId(e.Owner), e.Map is null), StringComparer.Ordinal);
            var producers = entries.Where(e => e.Kind == "bake").Select(e =>
                new Producer(e.Id, (IBakeProducer)registry.Create(e.Type), CodeHash(e.Type))).ToArray();
            var interiors = entries.Where(e => e.Kind == "interior").ToDictionary(e => "interior." + e.Id,
                e => new Interior(e.Id, e.IsStub, registry.CreateInteriorProvider(e.Type, context.Descriptor.Id), CodeHash(e.Type), new WpId(e.Owner)), StringComparer.Ordinal);
            // Capture one immutable source snapshot per MapBuild; never load extensions on a worker.
            var catalog = registry.Get<IPaletteCatalogSource>().Catalog;
            var globalInputs = Campus.Kernel.Registry.Kernel.GlobalBuildInputs;
            var groundContributors = BuildInputAssembly.CaptureGroundMaskContributors(registry, context.Descriptor.Id);
            var partLods = BuildInputAssembly.CapturePartLods(registry, context.Descriptor.Id);
            var geometry = new BakeGeometryBuilder(catalog, registry, context.Descriptor.Id);
            var seed = _options.SeedOverride ?? context.Descriptor.Seed;
            var claims = MapClaimLedger.Capture(registry, context.Descriptor.Id);
            var ledger = new Lazy<MapClaimLedger>(() => claims.Build(context.World), LazyThreadSafetyMode.ExecutionAndPublication);
            var map = new MapBuild(context, modules, interiors, producers, geometry,
                ledger, new(() => Load(context, seed, catalog, interiors, modules, ledger.Value, globalInputs, groundContributors, partLods),
                    LazyThreadSafetyMode.ExecutionAndPublication), globalInputs, groundContributors, partLods, catalog);
            _contexts.Add(context.Descriptor.Id, map);
            return map;
        }
    }

    internal void Unmount(MapId map)
    {
        lock (_sync)
        {
            if (_contexts.Remove(map, out var context)) context.Active = false;
        }
        _queue.ForgetMap(map);
    }

    internal async Task<BakeMapMetadata> PrepareMapData(IMapContext context, bool rebuild = false)
    {
        var map = Mount(context);
        var metadata = await Task.Run(() =>
        {
            map.InteriorCoverage = InspectInteriorCoverage(map);
            var hash = BakeMapMetadataCodec.SourceHash(map);
            var lightPath = context.BakeRoot.TrimEnd('/') + "/" + BakeMapMetadataCodec.LightingFile;
            var entrancePath = context.BakeRoot.TrimEnd('/') + "/" + BakeMapMetadataCodec.EntrancesFile;
            if (!rebuild && Godot.FileAccess.FileExists(lightPath) && Godot.FileAccess.FileExists(entrancePath))
            {
                try
                {
                    var loaded = BakeMapMetadataCodec.Read(Godot.FileAccess.GetFileAsBytes(lightPath), Godot.FileAccess.GetFileAsBytes(entrancePath),
                        context.Descriptor.Id, hash, map.Data.Value.Windows.Snapshot);
                    RequireEntranceCoverage(map, loaded);
                    return loaded;
                }
                catch (InvalidDataException) when (_options.AllowUnlisted) { }
            }
            if (!rebuild && !_options.AllowUnlisted) throw new InvalidDataException("Required resident lighting/entrance metadata is missing from the boot pack.");
            var generated = BakeMapMetadataCodec.Build(map);
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
        if (!IsBatch && !_options.Background) return;
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
                var basic = new BuildContext
                {
                    World = context.World,
                    Map = context.Descriptor,
                    WorldSeed = data.Seed,
                    SourceInputs = data.Inputs,
                    DataHash = data.Hash,
                    Catalog = map.Catalog,
                    ClaimedBy = map.Claims.Value.ClaimedBy,
                    InteriorProviders = data.InteriorProviders,
                    WindowNumbering = data.Windows.Snapshot,
                    GlobalInputs = map.GlobalInputs,
                    GroundMaskContributors = map.GroundMaskContributors,
                    PartLods = map.PartLods
                };
                if (_options.MapOnly)
                {
                    Schedule(map, new(context.Descriptor.Id, "map", "base"), BakePriority.Normal);
                    return;
                }
                var keys = map.Modules.OrderBy(p => p.Key, StringComparer.Ordinal).SelectMany(pair =>
                {
                    var tiles = pair.Value.Value.Tiles(map.GetPreparedContext(context, pair.Key))
                        .Distinct().OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();
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
                if (map.Producers.Any(p => p.Value.CanBuild(new(context.Descriptor.Id, "collision.world", "C/0/0"))))
                    for (var x = context.Descriptor.TileMin.X; x <= context.Descriptor.TileMax.X; x++)
                        for (var z = context.Descriptor.TileMin.Z; z <= context.Descriptor.TileMax.Z; z++)
                            Schedule(map, BakeRequestKeys.Collision(context.Descriptor.Id, "collision.world", new(x, z)), BakePriority.Background);
                foreach (var binding in data.InteriorBindings.OrderBy(binding => binding.ProviderId, StringComparer.Ordinal))
                {
                    var name = "interior." + binding.ProviderId;
                    var interior = map.Interiors[name];
                    foreach (var feature in binding.FeatureIds.OrderBy(f => f.Value, StringComparer.Ordinal))
                        foreach (var storey in ValidatedLayout(interior.Value, binding, basic, feature).OpenStoreys.Order())
                            foreach (var detail in new[] { "IF", "IS" })
                                Schedule(map, new(context.Descriptor.Id, name, FormattableString.Invariant($"{feature.Value}/{storey}/{detail}")), BakePriority.Background);
                }
            }
            catch (Exception) when (!map.Active) { }
            catch (Exception error)
            {
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
        if (!map.Active) throw new OperationCanceledException("Map unloaded before request scheduling.");
        return _queue.Ensure(key, priority, () => Build(map, key));
    }

    private object Build(MapBuild map, BakeKey key)
    {
        var generation = Interlocked.Read(ref _cacheGeneration);
        Task? clearing;
        lock (_sync) clearing = _clearing;
        clearing?.GetAwaiter().GetResult();
        var data = map.Data.Value;
        var module = map.Modules.GetValueOrDefault(key.Module);
        var interior = map.Interiors.GetValueOrDefault(key.Module);
        var matches = map.Producers.Where(p => p.Value.CanBuild(key)).ToArray();
        if (matches.Length > 1 || (module is not null || interior is not null) && matches.Length != 0)
            throw new InvalidDataException("More than one producer claims this bake key.");
        if (module is null && interior is null && matches.Length == 0) throw new InvalidDataException("No bake producer is registered for this key.");
        var codeHash = module?.CodeHash ?? interior?.CodeHash ?? matches[0].CodeHash;
        var dataHash = key.Module == "map" ? data.AtlasHash : data.Hash;
        var identity = BakeInputFingerprint.Identity(key, dataHash, codeHash);
        if (!_options.Live && !_options.Verify)
        {
            var boot = BakeBootStore.TryRead(map.Context.BakeRoot, identity);
            if (boot is not null)
            {
                data.Manifest.Verify(identity, boot, _options.AllowUnlisted);
                var product = Decode(identity, key, boot);
                RequireInteriorProduct(map, key, product);
                if (_options.Reference) lock (map.Reference)
                {
                    map.Reference.Add(identity, boot);
                    if (product is TileBundle { Level: LodLevel.L4 }) map.Products[identity] = product;
                    if (map.Interiors.ContainsKey(key.Module)) map.CompletedInteriorKeys.Add(key);
                }
                return product;
            }
            var cached = _cache.TryRead(key.Map.Value, identity, data.Manifest.Find(identity), _options.AllowUnlisted || _options.Reference,
                key.Module == "map" ? BakeCacheStore.MaximumProductBytes : BakeCacheStore.MaximumPayloadBytes);
            if (cached is not null)
            {
                try
                {
                    var product = Decode(identity, key, cached);
                    RequireInteriorProduct(map, key, product);
                    if (_options.Reference) lock (map.Reference)
                    {
                        map.Reference.Add(identity, cached);
                        if (product is TileBundle { Level: LodLevel.L4 }) map.Products[identity] = product;
                        if (map.Interiors.ContainsKey(key.Module)) map.CompletedInteriorKeys.Add(key);
                    }
                    return product;
                }
                catch (InvalidDataException) { _cache.Remove(key.Map.Value, identity); }
            }
            if (_options.Strict) throw new InvalidDataException($"Strict cache miss: {identity}");
        }
        var buildContext = new BuildContext
        {
            World = map.Context.World,
            Map = map.Context.Descriptor,
            WorldSeed = data.Seed,
            SourceInputs = data.Inputs,
            DataHash = dataHash,
            CodeHash = codeHash,
            Catalog = map.Catalog,
            ClaimedBy = map.Claims.Value.ClaimedBy,
            InteriorProviders = data.InteriorProviders,
            WindowNumbering = data.Windows.Snapshot,
            GlobalInputs = map.GlobalInputs,
            GroundMaskContributors = map.GroundMaskContributors,
            PartLods = map.PartLods
        };
        var built = module is not null ? map.Geometry!.Build(key, buildContext, module.Value, diagnostics => map.Collect(key.Module, "geometry", diagnostics)) :
            interior is not null ? BuildInterior(map, key, buildContext, interior.Value) : matches[0].Value.Build(key, buildContext);
        var bytes = Encode(identity, built);
        data.Manifest.Verify(identity, bytes, !_options.Verify && (_options.AllowUnlisted || _options.Reference));
        // Validate exactly the bytes about to be written before publishing Ready; malformed producer output is terminal.
        var validated = Decode(identity, key, bytes);
        RequireInteriorProduct(map, key, validated);
        if (!_options.Live && generation == Interlocked.Read(ref _cacheGeneration)) _cache.Write(key.Map.Value, identity, bytes, key.Module == "map");
        if (_options.Reference) lock (map.Reference)
        {
            map.Reference.Add(identity, bytes);
            if (validated is TileBundle { Level: LodLevel.L4 }) map.Products[identity] = validated;
            if (map.Interiors.ContainsKey(key.Module)) map.CompletedInteriorKeys.Add(key);
        }
        return validated;
    }

    private LoadedMap Load(IMapContext context, uint seed, PaletteCatalog catalog, IReadOnlyDictionary<string, Interior> interiors,
        IReadOnlyDictionary<string, Module> modules, MapClaimLedger claims, BuildInputSnapshot globalInputs,
        IReadOnlyList<IGroundMaskContributor> groundContributors, PartLodCatalog partLods)
    {
        // Validate the final post-overlay world once, before cache hits or geometry can bypass it.
        // Claims add diagnostics only; validation failures must remain terminal for this map load.
        BuildingOverlapValidator.Validate(context.World, claims.ClaimedBy);
        if (!_options.Live)
        {
            _cache.Initialize();
            _cache.RemoveOtherAbis(context.Descriptor.Id.Value);
        }
        var inputs = BakeInputFingerprint.ReadInputs(context);
        // These instances belong to this MapBuild, never the registry's global cache or an older map's jobs.
        // Configure once before the Lazy publishes them to any consumer of this map's generation context.
        var providers = interiors.Values.ToDictionary(interior => interior.ProviderId, interior => interior.Value, StringComparer.Ordinal);
        IReadOnlyList<D0InteriorBinding> bindings = inputs.TryGetValue("interiors.json", out var interiorCatalogBytes)
            ? D0InteriorSetup.Configure(interiorCatalogBytes, providers, context.World.Terrain) : [];
        foreach (var binding in bindings)
        {
            var actual = providers[binding.ProviderId].FeatureIds;
            if (actual.Count != binding.FeatureIds.Count || actual.Distinct().Count() != actual.Count || !actual.ToHashSet().SetEquals(binding.FeatureIds))
                throw new InvalidDataException($"Interior provider {binding.ProviderId} is not bound to its catalog: expected {binding.FeatureIds.Count} features, actual {actual.Count}.");
            foreach (var feature in binding.FeatureIds)
                if (!context.World.Features.ContainsKey(feature)) throw new InvalidDataException($"Interior catalog feature is absent from the loaded world: {feature.Value}");
        }
        var manifestPath = context.BakeRoot.TrimEnd('/') + "/bake.manifest.json";
        var manifest = new BakeManifest();
        if (Godot.FileAccess.FileExists(manifestPath))
        {
            try { manifest = BakeManifest.Parse(Godot.FileAccess.GetFileAsBytes(manifestPath)); }
            catch (InvalidDataException) when (!_options.Verify && (_options.Reference || _options.AllowUnlisted)) { }
        }
        var activeInteriors = new ReadOnlyDictionary<string, IInteriorProvider>(bindings.ToDictionary(binding => binding.ProviderId,
            binding => providers[binding.ProviderId], StringComparer.Ordinal));
        var inputHash = BakeInputFingerprint.Compute(context, seed, inputs, catalog, globalInputs);
        var prepass = new BuildContext
        {
            World = context.World,
            Map = context.Descriptor,
            WorldSeed = seed,
            Catalog = catalog,
            ClaimedBy = claims.ClaimedBy,
            SourceInputs = inputs,
            DataHash = inputHash,
            InteriorProviders = activeInteriors,
            GlobalInputs = globalInputs,
            GroundMaskContributors = groundContributors,
            PartLods = partLods
        };
        // This Lazy is completed before any catalog Tiles, geometry Build, or producer Collide can run.
        var windows = BakeWindowPreparation.Prepare(prepass, modules);
        return new(seed, inputs, BakeWindowPreparation.IncludeNumbering(inputHash, windows.Snapshot),
            BakeWindowPreparation.IncludeNumbering(BakeInputFingerprint.Compute(context, seed, inputs, catalog, globalInputs, includeAtlas: true), windows.Snapshot),
            manifest, bindings, activeInteriors, windows);
    }

    private static byte[] Encode(string identity, object product) => BakeProductCodec.Encode(identity, product);
    private static object Decode(string identity, BakeKey key, byte[] bytes) => BakeProductCodec.Decode(identity, key, bytes);
    private static void RequireInteriorProduct(MapBuild map, BakeKey key, object product)
    {
        if (map.Interiors.ContainsKey(key.Module) && (product is not TileBundle tile || tile.Stats.Surfaces == 0 || tile.Stats.Triangles == 0))
            throw new InvalidDataException($"Open interior product is empty: {key.Map.Value}/{key.Module}/{key.Detail}.");
    }
    private static string CodeHash(Type type) => BuildInfo.ModuleCodeHashes.TryGetValue(type.Assembly.GetName().Name!, out var hash)
        ? hash : throw new InvalidDataException($"BuildInfo has no code fingerprint for {type.Assembly.GetName().Name}.");
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
        var layout = ValidatedLayout(provider, binding, context, feature);
        if (!layout.OpenStoreys.Contains(storey)) throw new InvalidDataException("Interior storey is not open.");
        var center = layout.Bounds.Center;
        var tile = new TileKey((int)Math.Floor(center.X / 64f), (int)Math.Floor(center.Z / 64f));
        return map.Geometry!.BuildOutput(key, context, provider.BuildStorey(context, feature, storey, detail), tile, LodLevel.L0,
            diagnostics => map.Collect(key.Module, "interior-geometry", diagnostics, feature));
    }

    internal static InteriorLayout ValidatedLayout(IInteriorProvider provider, D0InteriorBinding binding, BuildContext context, FeatureId feature)
    {
        var layout = provider.Layout(context, feature);
        if (layout.Feature != feature || layout.OpenStoreys.Count == 0 || layout.OpenStoreys.Distinct().Count() != layout.OpenStoreys.Count ||
            !layout.OpenStoreys.ToHashSet().SetEquals(binding.OpenFloors) ||
            layout.OpenStoreys.Any(floor => !layout.Storeys.Any(storey => storey.Index == floor)))
            throw new InvalidDataException($"Interior layout coverage differs from its catalog: {binding.ProviderId}/{feature.Value}.");
        return layout;
    }

    private static IReadOnlyList<InteriorCoverage> InspectInteriorCoverage(MapBuild map)
    {
        var data = map.Data.Value;
        var coverage = new List<InteriorCoverage>();
        foreach (var binding in data.InteriorBindings.OrderBy(binding => binding.ProviderId, StringComparer.Ordinal))
        {
            var interior = map.Interiors["interior." + binding.ProviderId];
            var context = new BuildContext
            {
                World = map.Context.World,
                Map = map.Context.Descriptor,
                WorldSeed = data.Seed,
                Catalog = map.Catalog,
                ClaimedBy = map.Claims.Value.ClaimedBy,
                SourceInputs = data.Inputs,
                DataHash = data.Hash,
                CodeHash = interior.CodeHash,
                InteriorProviders = data.InteriorProviders,
                WindowNumbering = data.Windows.Snapshot,
                GlobalInputs = map.GlobalInputs,
                GroundMaskContributors = map.GroundMaskContributors,
                PartLods = map.PartLods
            };
            var floors = 0;
            var entrances = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in binding.FeatureIds.OrderBy(feature => feature.Value, StringComparer.Ordinal))
            {
                var layout = ValidatedLayout(interior.Value, binding, context, feature);
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
        var size = BakeRequestKeys.BaseTileMetres << (int)request.Level;
        var center = new System.Numerics.Vector2((request.Tile.X + 0.5f) * size, (request.Tile.Z + 0.5f) * size);
        var start = map.Descriptor.Title?.T0.Position;
        var origin = start is { } p ? new System.Numerics.Vector2(p.X, p.Z) : map.Descriptor.Center;
        return System.Numerics.Vector2.DistanceSquared(center, origin);
    }
    public void Dispose()
    {
        if (_flow is not null) _flow.Changed -= PhaseChanged;
        lock (_sync) foreach (var map in _contexts.Values) map.Active = false;
        _queue.Dispose();
    }

    internal sealed record Module(IContentModule Value, string CodeHash, WpId Owner, bool Global);
    internal sealed record Producer(string Id, IBakeProducer Value, string CodeHash);
    internal sealed record Interior(string ProviderId, bool IsStub, IInteriorProvider Value, string CodeHash, WpId Owner);
    internal sealed record CollectedDiagnostic(MapId Map, WpId Owner, string Module, string Stage, BuildDiagnostic Diagnostic);
    internal sealed record InteriorCoverage(string ProviderId, bool IsStub, int CatalogFeatures, int BoundFeatures,
        int CatalogFeatureStoreys, int ProducedFeatureStoreys, int AuthoredEntrances, int ProducedEntrances);
    internal sealed record LoadedMap(uint Seed, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Inputs, string Hash, string AtlasHash,
        BakeManifest Manifest, IReadOnlyList<D0InteriorBinding> InteriorBindings, IReadOnlyDictionary<string, IInteriorProvider> InteriorProviders,
        BakeWindowPreparation.Result Windows);
    internal sealed record MapBuild(IMapContext Context, IReadOnlyDictionary<string, Module> Modules, IReadOnlyDictionary<string, Interior> Interiors, Producer[] Producers,
        BakeGeometryBuilder Geometry, Lazy<MapClaimLedger> Claims, Lazy<LoadedMap> Data,
        BuildInputSnapshot GlobalInputs, IReadOnlyList<IGroundMaskContributor> GroundMaskContributors, PartLodCatalog PartLods, PaletteCatalog Catalog)
    {
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

        public BakeManifest Reference { get; } = new();
        public SortedDictionary<string, object> Products { get; } = new(StringComparer.Ordinal);
        public HashSet<BakeKey> CompletedInteriorKeys { get; } = [];
        public volatile bool EnumerationComplete;
        public bool EnumerationStarted;
        public volatile bool Active = true;
        public BakeMapMetadata? Metadata { get; private set; }
        public IReadOnlyList<InteriorCoverage> InteriorCoverage { get; set; } = [];
        private IReadOnlyDictionary<string, BuildContext>? _preparedContexts;

        internal void CompletePreparation(BakeMapMetadata metadata)
        {
            if (!Active || !Data.IsValueCreated || !Claims.IsValueCreated)
                throw new InvalidOperationException("Map preparation has not completed.");
            var data = Data.Value;
            if (!ReferenceEquals(metadata.Lighting.WindowNumbering, data.Windows.Snapshot))
                throw new InvalidDataException("Lighting publication must retain the prepared window snapshot instance.");
            _preparedContexts = new ReadOnlyDictionary<string, BuildContext>(Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => new BuildContext
                {
                    World = Context.World,
                    Map = Context.Descriptor,
                    WorldSeed = data.Seed,
                    Catalog = Catalog,
                    ClaimedBy = Claims.Value.ClaimedBy,
                    SourceInputs = data.Inputs,
                    DataHash = data.Hash,
                    CodeHash = pair.Value.CodeHash,
                    InteriorProviders = data.InteriorProviders,
                    WindowNumbering = data.Windows.Snapshot,
                    GlobalInputs = GlobalInputs,
                    GroundMaskContributors = GroundMaskContributors,
                    PartLods = PartLods
                }, StringComparer.Ordinal));
            Metadata = metadata;
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
