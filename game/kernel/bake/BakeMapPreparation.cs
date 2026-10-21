using Campus.Gen.BakeInput;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using static Campus.Kernel.Bake.BakeService;
using System.Collections.ObjectModel;
using Campus.Contracts.Parts;

namespace Campus.Kernel.Bake;

internal sealed class BakeMapPreparation
{
    private readonly BakeOptions _options;
    private readonly BakeCacheStore _cache;
    private readonly TerrainCacheService _terrain;

    internal BakeMapPreparation(BakeOptions options, TerrainCacheService terrain)
    { _options = options; _terrain = terrain; _cache = terrain.Store; }

    internal BakeMapMetadata Prepare(MapBuild map)
    {
        var data = map.Data.Value;
        foreach (var layout in data.Layouts) map.Content.Layouts.Add(layout.Feature, layout);
        if (data.Resident is { } cached)
        {
            map.Content.Coverage = cached.Coverage;
            map.Directory.Prepare(cached.Metadata, cached.Directory);
            return cached.Metadata;
        }
        map.Content.Coverage = InspectInteriorCoverage(map);
        var generated = BakeMapMetadataBuilder.Build(map);
        RequireEntranceCoverage(map, generated);
        map.Directory.Prepare(generated);
        var terrainPoints = _terrain.SaveResidentPoints(map.Context.World, data.ResidentStamp);
        _terrain.WriteProduct(new(map.Context.Descriptor.Id, "resident", "meta"), data.ResidentStamp,
            new BakeResidentData(generated, data.Windows.Metadata, map.Content.Coverage.ToArray(), map.Content.Layouts.Values.ToArray(),
                map.Directory.PreparedTiles, terrainPoints) { OutdoorAccess = data.OutdoorAccess });
        return generated;
    }

    internal LoadedMap Load(IMapContext context, uint seed, PaletteCatalog catalog, IReadOnlyDictionary<string, Interior> interiors,
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
        var activeInteriors = new ReadOnlyDictionary<string, IInteriorProvider>(bindings.Where(binding => binding.InteriorFeatures.Count > 0).ToDictionary(binding => binding.ProviderId,
            binding => providers[binding.ProviderId], StringComparer.Ordinal));
        var residentStamp = sourceCache.ResolveResidentStamp(() =>
        {
            // Revalidate changed inputs before publishing a replacement resident version.
            BuildingOverlapValidator.Validate(context.World, claimLookup);
            return BakeInputIdentity.ResidentStamp(_terrain.CodeFingerprints, context, seed, catalog, interiors, modules, globalInputs, claimLookup, producers);
        });
        var resident = _terrain.ReadProduct<BakeResidentData>(new(context.Descriptor.Id, "resident", "meta"), residentStamp, candidate =>
        {
            BakeMapMetadataBuilder.ValidateResident(candidate, context.Descriptor.Id, modules, producers);
            if (!_terrain.RestoreResidentPoints(context.World, residentStamp, candidate.TerrainPoints))
                throw new InvalidDataException("Resident metadata has missing terrain points.");
        });
        if (resident is not null)
        {
            publication.PublishInteriors(InteriorAssembly.IndexEntries(resident.Layouts, bindings, activeInteriors, context.World, resident.OutdoorAccess));
            return new(seed, inputs, bindings, activeInteriors,
                new(resident.Metadata.Lighting.WindowNumbering
                    ?? throw new InvalidDataException("Resident metadata has no window numbering snapshot."), resident.Windows), resident.Layouts, resident, residentStamp) { OutdoorAccess = resident.OutdoorAccess };
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
        var outdoor = InteriorAssembly.CaptureOutdoorAccess(prepass, bindings,
            modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value.Value).OfType<IOutdoorAccessSource>());
        var layouts = bindings.SelectMany(binding => binding.InteriorFeatures.Select(feature =>
            InteriorAssembly.ValidatedLayout(activeInteriors[binding.ProviderId], binding, prepass, feature))).ToArray();
        publication.PublishInteriors(InteriorAssembly.IndexEntries(layouts, bindings, activeInteriors, context.World, outdoor));
        prepass = prepass.WithWorld(_terrain.AssetWorld(context.World));
        // This Lazy is completed before any catalog Tiles, geometry Build, or producer Collide can run.
        var windows = BakeWindowPreparation.Prepare(prepass, modules);
        return new(seed, inputs, bindings, activeInteriors, windows, layouts, null, residentStamp) { OutdoorAccess = outdoor };
    }

    private static IReadOnlyList<InteriorCoverage> InspectInteriorCoverage(MapBuild map)
    {
        var data = map.Data.Value;
        var coverage = new List<InteriorCoverage>();
        foreach (var binding in data.InteriorBindings.Where(binding => binding.InteriorFeatures.Count > 0).OrderBy(binding => binding.ProviderId, StringComparer.Ordinal))
        {
            var interior = map.Content.Interiors["interior." + binding.ProviderId];
            var floors = 0;
            var entrances = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in binding.InteriorFeatures.OrderBy(feature => feature.Value, StringComparer.Ordinal))
            {
                var layout = map.Content.Layouts[feature];
                floors += layout.OpenStoreys.Count;
                foreach (var entrance in layout.Entrances) entrances.Add(System.Text.Json.JsonSerializer.Serialize(entrance, ContractJson.Options));
            }
            coverage.Add(new(binding.ProviderId, interior.IsStub, binding.InteriorFeatures.Count, interior.Value.FeatureIds.Count,
                binding.InteriorFeatures.Sum(feature => binding.FloorsFor(feature).Count), floors, binding.AuthoredEntranceCount, entrances.Count));
        }
        return coverage.AsReadOnly();
    }

    private static void RequireEntranceCoverage(MapBuild map, BakeMapMetadata metadata)
    {
        foreach (var coverage in map.Content.Coverage)
            if (metadata.Entrances.Entrances.Count(item => item.Module == "interior." + coverage.ProviderId) < coverage.ProducedEntrances)
                throw new InvalidDataException($"Entrance inventory omits configured interior entries: {coverage.ProviderId}.");
    }
}
