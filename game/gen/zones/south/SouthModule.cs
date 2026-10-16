using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Zones.South.Framework;

namespace Campus.Gen.Zones.South;

/// <summary>
/// WP-18 south zone (MAP-QSH-SOUTH): the landmark sites listed in <see cref="SouthSites.All"/>, filed into cells.
/// Generic buildings stay with WP-14; this module claims only the features it builds itself and adds identity parts
/// (portico, roof gallery, gate furniture) next to the generic massing. Pure .NET: reads the world model and terrain only.
/// </summary>
[ContentModule("zone.south", Owner = "WP-18")]
public sealed class SouthModule : IContentModule
{
    private static readonly ConditionalWeakTable<IWorldModel, Dictionary<(uint Seed, LodLevel Lod), IReadOnlyList<SiteItem>>> Cache = new();
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

    /// <summary>All items of one level, built once per world, seed and level; every Build call on any tile agrees.</summary>
    internal IReadOnlyList<SiteItem> ItemsFor(BuildContext ctx, LodLevel lod)
    {
        lock (Sync)
        {
            var byKey = Cache.GetValue(ctx.World, _ => []);
            if (byKey.TryGetValue((ctx.WorldSeed, lod), out var found) && ReferenceEquals(_sites, SouthSites.All)) return found;
            var builder = new SiteBuilder(ctx, lod);
            foreach (var site in _sites) site.Build(builder);
            var items = builder.Items.ToArray();
            if (ReferenceEquals(_sites, SouthSites.All)) byKey[(ctx.WorldSeed, lod)] = items;
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
/// The south zone's sites in a fixed order. 18a: gate and entrance axis, main building, forest park; 18b and 18c append
/// their own entries (east lake, Xuanbang, Liren; sports venues, innovation centre) after these.
/// </summary>
public static class SouthSites
{
    public static IReadOnlyList<SouthSite> All { get; } = Array.AsReadOnly<SouthSite>(
    [
        new Gate.SouthGateSite(),
        new MainBuilding.EntranceAxisSite(),
        new MainBuilding.MainBuildingSite(),
        new ForestPark.ForestParkSite(),
    ]);
}
