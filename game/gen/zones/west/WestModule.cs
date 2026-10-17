using Campus.Gen.Geometry;
using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Zones.West.Framework;

namespace Campus.Gen.Zones.West;

/// <summary>
/// WP-19 west zone (MAP-QSH-WEST): the sites listed in <see cref="WestSites.All"/>, filed into cells. Generic buildings stay
/// with WP-14 (their storeys corrected by overlay/west.d/ HeightOps); this module claims only the landmarks it builds
/// itself (六号科研楼's three entities) and adds identity pieces (the motto stone) next to them. As the zone's one window
/// metadata source (contract-29) it declares a complete grid set for every Facade-claimed feature, gathered from its sites.
/// Pure .NET: reads the world model and terrain only.
/// </summary>
[ContentModule("zone.west", Owner = "WP-19")]
public sealed class WestModule : IContentModule, IWindowMetadataSource
{
    private static readonly ConditionalWeakTable<IWorldModel, Dictionary<(uint Seed, LodLevel Lod, string Numbering), IReadOnlyList<SiteItem>>> Cache = new();
    private static readonly object Sync = new();

    private readonly IReadOnlyList<WestSite> _sites;

    public WestModule() : this(WestSites.All) { }

    internal WestModule(IReadOnlyList<WestSite> sites)
    {
        _sites = sites;
        Claims = sites.SelectMany(s => s.Claims).ToArray();
        Probes = sites.SelectMany(s => s.Probes).ToArray();
    }

    public IReadOnlyList<Claim> Claims { get; }

    public IReadOnlyList<InspectionProbe> Probes { get; }

    /// <summary>Measured on the 19a sites: the heaviest L0 cell (六号's C block with its sign and vestibule) stays under these bounds.</summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: WestTune.BudgetTrianglesL0, TrianglesL1: WestTune.BudgetTrianglesL1, DrawCalls: WestTune.BudgetDrawCalls);

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var cells = new SortedSet<(int X, int Z)>();
        foreach (var item in ItemsFor(ctx, LodLevel.L0)) cells.Add(Cell(item.Anchor, LodLevel.L0));
        var colliders = new SiteColliders(ctx);
        foreach (var site in _sites) site.Collide(colliders);
        foreach (var (anchor, _) in colliders.Items) cells.Add(Cell(anchor, LodLevel.L0));
        return cells.Select(c => new TileKey(c.X, c.Z)).ToArray();
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var meshes = new List<GeometryBundle>();
        var parts = new List<PlacedPart>();
        var lights = new List<LightEmitter>();
        foreach (var item in ItemsFor(ctx, lod))
        {
            if (lod != LodLevel.L4)
            {
                var cell = Cell(item.Anchor, lod);
                if (cell.X != tile.X || cell.Z != tile.Z) continue;
            }
            if (item.Mesh is { } mesh) meshes.Add(GeometryPublication.Publish(mesh, item.HasLodChannel));
            if (item.Part is { } part) parts.Add(part);
            if (item.Light is { } light) lights.Add(light);
        }
        if (meshes.Count == 0 && parts.Count == 0 && lights.Count == 0) return ContentOutput.Empty;
        return new ContentOutput(GeometryBatch.Merge(meshes, GeometryMergePolicy.Plain), parts, lights.Count == 0 ? null : lights);
    }

    /// <summary>Colliders live in fixed 64 m cells, filed by each collider's anchor.</summary>
    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var colliders = new SiteColliders(ctx);
        foreach (var site in _sites) site.Collide(colliders);
        return colliders.Items.Where(i => Cell(i.Anchor, LodLevel.L0) == (tile.X, tile.Z)).Select(i => i.Collider).ToArray();
    }

    public IReadOnlyList<ClaimedWindowLayout> DescribeWindowLayouts(IWorldModel world, uint worldSeed)
    {
        ArgumentNullException.ThrowIfNull(world);
        // Every Facade-claimed feature of this map gets a declaration, empty when it has no windows (the gate).
        var grids = new SortedDictionary<string, List<WindowGridSpec>>(StringComparer.Ordinal);
        foreach (var claim in Claims.OfType<FeatureClaim>())
            if ((claim.Aspects & Aspects.Facade) != 0 && world.Features.ContainsKey(claim.Feature) && !grids.TryAdd(claim.Feature.Value, []))
                throw new InvalidOperationException($"{claim.Feature.Value} is claimed twice by the west sites.");
        foreach (var site in _sites)
            foreach (var (feature, grid) in site.WindowGrids(world))
            {
                if (!grids.TryGetValue(feature.Value, out var list))
                    throw new InvalidOperationException($"{site.Id} declares windows for {feature.Value}, which the zone does not claim for Facade.");
                if (list.Any(g => g.Key == grid.Key)) throw new InvalidOperationException($"{feature.Value} / {grid.Key} is declared twice.");
                list.Add(grid);
            }
        return [.. grids.Select(p => new ClaimedWindowLayout(new(p.Key), [.. p.Value.OrderBy(g => g.Key, StringComparer.Ordinal)]))];
    }

    public WindowMetadata DescribeWindows(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.WindowNumbering is null)
            throw new InvalidOperationException("Window positions are described only after BuildContext.WindowNumbering is assigned.");
        var facades = new List<WindowFacadeDecl>();
        var windows = new List<WindowDecl>();
        foreach (var site in _sites) site.DescribeWindows(context, facades, windows);
        return new(facades, windows);
    }

    /// <summary>
    /// All items of one level, built once per world, seed, level and window numbering (its fingerprint, empty without one);
    /// every Build call on any tile agrees.
    /// </summary>
    internal IReadOnlyList<SiteItem> ItemsFor(BuildContext ctx, LodLevel lod)
    {
        lock (Sync)
        {
            var byKey = Cache.GetValue(ctx.World, _ => []);
            var key = (ctx.WorldSeed, lod, ctx.WindowNumbering?.Fingerprint ?? "");
            if (byKey.TryGetValue(key, out var found) && ReferenceEquals(_sites, WestSites.All)) return found;
            var builder = new SiteBuilder(ctx, lod, lod switch
            {
                LodLevel.L0 => SiteDetail.Full,
                LodLevel.L1 => SiteDetail.Plain,
                LodLevel.L2 or LodLevel.L3 or LodLevel.L4 => SiteDetail.Mass,
                _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown LOD level."),
            });
            foreach (var site in _sites) site.Build(builder);
            var items = Array.AsReadOnly(builder.Items.ToArray());
            if (ReferenceEquals(_sites, WestSites.All)) byKey[key] = items;
            return items;
        }
    }

    private static (int X, int Z) Cell(Vector2 p, LodLevel lod)
    {
        var size = 64f * (1 << (int)lod);
        return ((int)MathF.Floor(p.X / size), (int)MathF.Floor(p.Y / size));
    }
}

/// <summary>
/// The west zone's sites in a fixed order. 19a: 六号科研楼 and the motto stone (the A10 plaza is overlay paving drawn by
/// WP-15). 19b: 四号's glass entrance boxes and lawn seat wall, 创新中心's stair shafts and main portal; the other 19b
/// buildings (五号、研究院、核磁、经管楼与 7482、荟文楼、6151/6154) are WP-14's, styled in overlay/west.d/. 19c (托幼园、
/// 湿地与西门桥头、石刻) appends its own entries after these.
/// </summary>
public static class WestSites
{
    public static IReadOnlyList<WestSite> All { get; } = Array.AsReadOnly<WestSite>(
    [
        new Six.SixSite(),
        new Motto.MottoStoneSite(),
        new Four.FourSite(),
        new Innovation.InnovationSite(),
        new Wetland.BoardwalkSite(),
        new Wetland.LeveeStepsSite(),
        new Bridgehead.ShelterSite(),
        new Bridgehead.CarvingSite(),
    ]);
}
