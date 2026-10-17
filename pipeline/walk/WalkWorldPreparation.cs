using System.Collections.ObjectModel;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Ground;
using Campus.Gen.Collision;
using Campus.Gen.Walkable;
using Campus.Gen.World;
using Campus.Pipeline.Map;

namespace Campus.Pipeline.Walk;

/// <summary>The offline producer prepares all modules; playable maps restore the resulting support product.</summary>
public static class WalkWorldPreparation
{
    public static (BuildContext Context, IReadOnlyList<InteriorProviderBinding> InteriorBindings) CreateContext(WorldModelSnapshot world, MapDescriptor map, MapGenerationInputs inputs,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> sourceInputs)
    {
        var claims = new MapClaimLedger(world, inputs.Modules.Select(pair =>
            new MapClaimSource(pair.Key, pair.Value.Owner, pair.Value.Global, pair.Value.Claims)));
        var providers = inputs.InteriorProviders;
        var bindings = sourceInputs.TryGetValue("interiors.json", out var bytes)
            ? InteriorAssembly.ConfigureForWorld(bytes, providers, world) : [];
        return (new()
        {
            World = world,
            Map = map,
            WorldSeed = map.Seed,
            Catalog = inputs.Catalog,
            ClaimedBy = claims.ClaimedBy,
            SourceInputs = sourceInputs,
            InteriorProviders = new ReadOnlyDictionary<string, IInteriorProvider>(bindings.ToDictionary(
                binding => binding.ProviderId, binding => providers[binding.ProviderId], StringComparer.Ordinal)),
            GlobalInputs = inputs.GlobalInputs,
            PartLods = inputs.PartLods,
            GroundMaskContributors = inputs.GroundMaskContributors,
            DataHash = WalkBakeInputHash.ComputeForMap(sourceInputs)
        }, bindings);
    }

    public static (WorldModelSnapshot World, IReadOnlyList<Footprint2D> Occupancy, WalkSupportData Support) Prepare(
        BuildContext context, IReadOnlyDictionary<string, ContentModuleInput> modules,
        IReadOnlyList<InteriorProviderBinding> interiorBindings, IReadOnlyDictionary<string, string> interiorCodeHashes,
        Func<PlacedPart, IReadOnlyList<Collider>> partCollision)
    {
        if (context.World is not WorldModelSnapshot parsed)
            throw new ArgumentException("Walking preparation requires the parsed world snapshot.", nameof(context));
        var ground = GroundSupport.Prepare(parsed, context.Map);
        var groundColliders = ground.Export().Ground.SelectMany(tile => new Collider[] { tile.Base }.Concat(tile.Tops))
            .ToHashSet(ReferenceEqualityComparer.Instance);
        var prepass = BakeWindowPreparation.Context(context, context.DataHash, context.CodeHash, null, parsed.WithWalkSupport(ground));
        var windows = BakeWindowPreparation.Prepare(prepass, modules);
        var dataHash = BakeWindowPreparation.IncludeNumbering(prepass.DataHash, windows.Snapshot);
        var occupancy = new List<Footprint2D>();
        var supports = new List<Collider>();
        foreach (var (_, module) in modules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var build = BakeWindowPreparation.Context(prepass, dataHash, module.CodeHash, windows.Snapshot);
            var tiles = module.Value.Tiles(build).Distinct().OrderBy(tile => tile.Z).ThenBy(tile => tile.X).ToArray();
            foreach (var tile in tiles)
            {
                var output = module.Value.Build(build, tile, LodLevel.L0);
                occupancy.AddRange(output.Occupancy ?? []);
                AddSupports(module.Value.Collide(build, tile));
                AddPartSupports(output.Parts);
            }
        }
        foreach (var binding in interiorBindings.OrderBy(binding => binding.ProviderId, StringComparer.Ordinal))
        {
            var provider = context.InteriorProviders[binding.ProviderId];
            if (!provider.Walkable) continue;
            var build = BakeWindowPreparation.Context(prepass, dataHash, interiorCodeHashes[binding.ProviderId], windows.Snapshot);
            foreach (var feature in binding.FeatureIds.OrderBy(feature => feature.Value, StringComparer.Ordinal))
            {
                var layout = InteriorAssembly.ValidatedLayout(provider, binding, build, feature);
                foreach (var storey in layout.OpenStoreys.Order())
                {
                    AddSupports(provider.CollisionStorey(build, feature, storey));
                    AddPartSupports(provider.BuildStorey(build, feature, storey, InteriorDetail.Full).Parts);
                }
            }
        }
        var prepared = ground.WithOtherSupports(supports);
        return (parsed.WithWalkSupport(prepared), occupancy.AsReadOnly(), prepared.Export());

        void AddPartSupports(IEnumerable<PlacedPart> parts)
        {
            foreach (var part in parts)
                AddSupports(partCollision(part).Select(collider => CollisionGeometry.Transform(collider, part.At)));
        }

        void AddSupports(IEnumerable<Collider> colliders) => supports.AddRange(colliders.Where(collider =>
            !groundColliders.Contains(collider) && (collider.Tag is SurfaceTag.Ground or SurfaceTag.Stair)
            && (collider is RampCollider or SlabCollider or BoxCollider or HeightFieldCollider)));
    }

    public static WorldModelSnapshot Restore(WorldModelSnapshot world, WalkSupportData support) =>
        world.WithWalkSupport(GroundSupport.Restore(support));
}
