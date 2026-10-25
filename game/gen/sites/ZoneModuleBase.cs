using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Sites;

/// <summary>Range-based routing and metadata assembly for site-based zones.</summary>
public abstract class ZoneModuleBase : IContentModule, IOutdoorAccessSource
{
    private readonly Func<IWorldModel, Func<Campus.Contracts.Maps.MapRect, bool>?, IReadOnlyList<ZoneSite>> _sites;
    private readonly Func<IWorldModel, IReadOnlyList<Campus.Contracts.Maps.MapRect>> _ranges;

    protected ZoneModuleBase(IReadOnlyList<ZoneSite> sites)
    {
        IReadOnlyList<Campus.Contracts.Maps.MapRect> SiteRanges(IWorldModel world, ZoneSite site) =>
            DerivedMemo.ForWorld(world, (typeof(ZoneModuleBase), site), () => site.Ranges(world));
        _sites = (world, includes) => includes is null ? sites : sites.Where(site => SiteRanges(world, site).Any(includes)).ToArray();
        _ranges = world => DerivedMemo.ForWorld<IReadOnlyList<Campus.Contracts.Maps.MapRect>>(world, this,
            () => sites.SelectMany(site => SiteRanges(world, site)).ToArray());
        Claims = sites.SelectMany(site => site.Claims).ToArray();
    }

    protected ZoneModuleBase(
        Func<IWorldModel, Func<Campus.Contracts.Maps.MapRect, bool>?, IReadOnlyList<ZoneSite>> sites,
        Func<IWorldModel, IReadOnlyList<Campus.Contracts.Maps.MapRect>> ranges,
        IReadOnlyList<Claim> claims)
    {
        _sites = sites;
        _ranges = world => DerivedMemo.ForWorld(world, this, () => ranges(world));
        Claims = claims;
    }

    public virtual BakeInputDependencies BakeInputs => BakeInputDependencies.World;
    public virtual BakeInputDependencies CollisionInputs => BakeInputDependencies.World;

    public virtual IReadOnlyList<Claim> Claims { get; }

    public IEnumerable<TileKey> Tiles(BuildContext context)
    {
        var cells = new SortedSet<(int X, int Z)>();
        foreach (var range in _ranges(context.World))
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
            new(GeometryBatch.Merge(meshes), parts, lights.Count == 0 ? null : lights);
    }

    public IReadOnlyList<Collider> Collide(BuildContext context, TileKey tile)
    {
        var colliders = new SiteColliders(context, tile);
        if (!_ranges(context.World).Any(colliders.Includes)) return [];
        foreach (var site in _sites(context.World, colliders.Includes)) site.Collide(colliders);
        return colliders.Items.Where(item => TileGrid.At(item.Anchor) == tile).Select(item => item.Collider).ToArray();
    }

    public IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel lod) =>
        _sites(context.World, null).SelectMany(site => site.TraceSources(context, lod));

    public IReadOnlyList<ClaimedWindowLayout> DescribeWindowLayouts(IWorldModel world, uint worldSeed)
    {
        var grids = new SortedDictionary<string, List<WindowGridSpec>>(StringComparer.Ordinal);
        foreach (var claim in Claims.OfType<FeatureClaim>())
            if ((claim.Aspects & Aspects.Facade) != 0 && world.Features.ContainsKey(claim.Feature) && !grids.TryAdd(claim.Feature.Value, []))
                throw new InvalidOperationException($"{claim.Feature.Value} is claimed twice by the zone sites.");
        foreach (var site in _sites(world, null))
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
        foreach (var site in _sites(context.World, null)) site.DescribeWindows(context, facades, windows);
        return new(facades, windows);
    }

    public IReadOnlyList<OutdoorAccessArea> DescribeOutdoorAccess(BuildContext context) =>
        _sites(context.World, null).SelectMany(site => site.DescribeOutdoorAccess(context)).ToArray();

    public IReadOnlyList<LightEmitter> DescribeLights(BuildContext context) =>
        _sites(context.World, null).OfType<ILightMetadataSource>().SelectMany(site => site.DescribeLights(context)).ToArray();

    internal IReadOnlyList<SiteItem> BuildItems(BuildContext context, TileKey tile, LodLevel lod)
    {
        var builder = new SiteBuilder(context, lod, lod switch
        {
            LodLevel.L0 => SiteDetail.Full,
            LodLevel.L1 => SiteDetail.Plain,
            LodLevel.L2 or LodLevel.L3 or LodLevel.L4 => SiteDetail.Mass,
            _ => throw new ArgumentOutOfRangeException(nameof(lod)),
        }, tile);
        if (!_ranges(context.World).Any(builder.Includes)) return [];
        foreach (var site in _sites(context.World, lod == LodLevel.L4 ? null : builder.Includes)) site.Build(builder);
        return builder.Items.Where(item => lod == LodLevel.L4 || TileGrid.At(item.Anchor, lod) == tile).ToArray();
    }
}
