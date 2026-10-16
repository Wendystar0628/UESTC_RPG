using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Zones.South.Framework;

namespace Campus.Gen.Zones.South;

/// <summary>
/// WP-18 south zone (MAP-QSH-SOUTH): the landmark sites listed in <see cref="SouthSites.All"/>, filed into cells.
/// Generic buildings stay with WP-14; this module claims only the features it builds itself and adds identity parts
/// (portico, roof gallery, gate furniture) next to the generic massing. Pure .NET: reads the world model and terrain only.
/// As the zone's one window metadata source (contract-29) it declares a complete grid set for every Facade-claimed feature,
/// gathered from its sites; B2 and the other main-building wings, and every other WP-14 building, keep generic windows.
/// </summary>
[ContentModule("zone.south", Owner = "WP-18")]
public sealed class SouthModule : IContentModule, IWindowMetadataSource
{
    private static readonly ConditionalWeakTable<IWorldModel, Dictionary<(uint Seed, LodLevel Lod, string Numbering), IReadOnlyList<SiteItem>>> Cache = new();
    private static readonly object Sync = new();

    private readonly IReadOnlyList<SouthSite> _sites;

    public SouthModule() : this(SouthSites.All) { }

    internal SouthModule(IReadOnlyList<SouthSite> sites)
    {
        _sites = sites;
        Claims = sites.SelectMany(s => s.Claims).ToArray();
        Probes = sites.SelectMany(s => s.Probes).ToArray();
    }

    public IReadOnlyList<Claim> Claims { get; }

    public IReadOnlyList<InspectionProbe> Probes { get; }

    /// <summary>Measured on the 18a sites: the heaviest L0 cell (main building front) stays under these bounds.</summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: SouthTune.BudgetTrianglesL0, TrianglesL1: SouthTune.BudgetTrianglesL1, DrawCalls: SouthTune.BudgetDrawCalls);

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
        foreach (var item in ItemsFor(ctx, lod))
        {
            if (lod != LodLevel.L4)
            {
                var cell = Cell(item.Anchor, lod);
                if (cell.X != tile.X || cell.Z != tile.Z) continue;
            }
            if (item.Mesh is { } mesh) meshes.Add(mesh);
            if (item.Part is { } part) parts.Add(part);
        }
        if (meshes.Count == 0 && parts.Count == 0) return ContentOutput.Empty;
        return new ContentOutput(MeshMerge.Merge(meshes), parts);
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
                throw new InvalidOperationException($"{claim.Feature.Value} is claimed twice by the south sites.");
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
            if (byKey.TryGetValue(key, out var found) && ReferenceEquals(_sites, SouthSites.All)) return found;
            var builder = new SiteBuilder(ctx, lod);
            foreach (var site in _sites) site.Build(builder);
            var items = builder.Items.ToArray();
            if (ReferenceEquals(_sites, SouthSites.All)) byKey[key] = items;
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
/// The south zone's sites in a fixed order. 18a: gate and entrance axis, main building, forest park; 18b: Xuanbang, Liren,
/// east lake; 18c: the gym and natatorium, the west lawn by the innovation centre, and the ordinary buildings' probes (their
/// massing stays WP-14's).
/// </summary>
public static class SouthSites
{
    public static IReadOnlyList<SouthSite> All { get; } = Array.AsReadOnly<SouthSite>(
    [
        new Gate.SouthGateSite(),
        new MainBuilding.EntranceAxisSite(),
        new MainBuilding.MainBuildingSite(),
        new ForestPark.ForestParkSite(),
        new Xuanbang.XuanbangSite(),
        new Liren.LirenSite(),
        new EastLake.EastLakeSite(),
        new Sports.GymSite(),
        new Sports.NatatoriumSite(),
        new Innovation.WestLawnSite(),
        new Ordinary.OrdinaryBuildingsSite(),
    ]);
}
