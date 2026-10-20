using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Sites;

/// <summary>Range-based routing and metadata assembly for site-based zones.</summary>
public abstract class ZoneModuleBase : IContentModule
{
    private readonly IReadOnlyList<ZoneSite> _sites;
    private readonly GeometryMergePolicy _mergePolicy;

    protected ZoneModuleBase(IReadOnlyList<ZoneSite> sites, ModuleBudget budget,
        GeometryMergePolicy mergePolicy = GeometryMergePolicy.Plain)
    {
        _sites = sites;
        _mergePolicy = mergePolicy;
        Budget = budget;
        Claims = sites.SelectMany(site => site.Claims).ToArray();
        Probes = sites.SelectMany(site => site.Probes).ToArray();
    }

    public abstract BakeInputDependencies BakeInputs { get; }
    public abstract BakeInputDependencies CollisionInputs { get; }

    public virtual IReadOnlyList<Claim> Claims { get; }
    public virtual IReadOnlyList<InspectionProbe> Probes { get; }
    public ModuleBudget Budget { get; }

    public IEnumerable<TileKey> Tiles(BuildContext context)
    {
        var cells = new SortedSet<(int X, int Z)>();
        foreach (var site in _sites)
            foreach (var range in site.Ranges(context.World))
            {
                var min = TileGrid.At(range.Min);
                var max = TileGrid.At(range.Max);
                for (var x = min.X; x <= max.X; x++)
                    for (var z = min.Z; z <= max.Z; z++) cells.Add((x, z));
            }
        return cells.Select(cell => new TileKey(cell.X, cell.Z)).ToArray();
    }

    public virtual ContentOutput Build(BuildContext context, TileKey tile, LodLevel lod)
    {
        var meshes = new List<GeometryBundle>();
        var parts = new List<PlacedPart>();
        var lights = new List<LightEmitter>();
        foreach (var item in BuildItems(context, tile, lod))
        {
            if (item.Mesh is { } mesh) meshes.Add(GeometryPublication.Publish(mesh, item.HasLodChannel));
            if (item.Part is { } part) parts.Add(part);
            if (item.Light is { } light) lights.Add(light);
        }
        return meshes.Count == 0 && parts.Count == 0 && lights.Count == 0 ? ContentOutput.Empty :
            new(GeometryBatch.Merge(meshes, _mergePolicy), parts, lights.Count == 0 ? null : lights);
    }

    public IReadOnlyList<Collider> Collide(BuildContext context, TileKey tile)
    {
        var colliders = new SiteColliders(context, tile);
        foreach (var site in _sites)
            if (site.Ranges(context.World).Any(colliders.Includes)) site.Collide(colliders);
        return colliders.Items.Where(item => TileGrid.At(item.Anchor) == tile).Select(item => item.Collider).ToArray();
    }

    public IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel lod) =>
        _sites.SelectMany(site => site.TraceSources(context, lod));

    public IReadOnlyList<ClaimedWindowLayout> DescribeWindowLayouts(IWorldModel world, uint worldSeed)
    {
        var grids = new SortedDictionary<string, List<WindowGridSpec>>(StringComparer.Ordinal);
        foreach (var claim in Claims.OfType<FeatureClaim>())
            if ((claim.Aspects & Aspects.Facade) != 0 && world.Features.ContainsKey(claim.Feature) && !grids.TryAdd(claim.Feature.Value, []))
                throw new InvalidOperationException($"{claim.Feature.Value} is claimed twice by the zone sites.");
        foreach (var site in _sites)
            foreach (var (feature, grid) in site.WindowGrids(world))
            {
                if (!grids.TryGetValue(feature.Value, out var list))
                    throw new InvalidOperationException($"{site.Id} declares windows for unclaimed Facade {feature.Value}.");
                if (list.Any(item => item.Key == grid.Key)) throw new InvalidOperationException($"{feature.Value} / {grid.Key} is declared twice.");
                list.Add(grid);
            }
        return [.. grids.Select(pair => new ClaimedWindowLayout(new(pair.Key), [.. pair.Value.OrderBy(grid => grid.Key, StringComparer.Ordinal)]))];
    }

    public WindowMetadata DescribeWindows(BuildContext context)
    {
        if (context.WindowNumbering is null)
            throw new InvalidOperationException("Window positions require BuildContext.WindowNumbering.");
        var facades = new List<WindowFacadeDecl>();
        var windows = new List<WindowDecl>();
        foreach (var site in _sites) site.DescribeWindows(context, facades, windows);
        return new(facades, windows);
    }

    public IReadOnlyList<LightEmitter> DescribeLights(BuildContext context) =>
        _sites.OfType<ILightMetadataSource>().SelectMany(site => site.DescribeLights(context)).ToArray();

    internal IReadOnlyList<SiteItem> BuildItems(BuildContext context, TileKey tile, LodLevel lod)
    {
        var builder = new SiteBuilder(context, lod, lod switch
        {
            LodLevel.L0 => SiteDetail.Full,
            LodLevel.L1 => SiteDetail.Plain,
            LodLevel.L2 or LodLevel.L3 or LodLevel.L4 => SiteDetail.Mass,
            _ => throw new ArgumentOutOfRangeException(nameof(lod)),
        }, tile);
        foreach (var site in _sites)
            if (lod == LodLevel.L4 || site.Ranges(context.World).Any(builder.Includes)) site.Build(builder);
        return builder.Items.Where(item => lod == LodLevel.L4 || TileGrid.At(item.Anchor, lod) == tile).ToArray();
    }
}
