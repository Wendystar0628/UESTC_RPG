using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Zones.North.Framework;

namespace Campus.Gen.Zones.North;

/// <summary>
/// WP-20 north living zone (MAP-QSH-NORTH): the sites listed in <see cref="NorthSites.All"/>, filed into cells.
/// Generic buildings (the dormitories among them) stay with WP-14; this module claims only the features it builds itself
/// and adds the zone's own parts next to the generic massing: cluster fences and gates, entrances, canteen and street
/// identity parts. Pure .NET: reads the world model, the interior index and the terrain only.
/// </summary>
[ContentModule("zone.north", Owner = "WP-20")]
public sealed class NorthModule : IContentModule
{
    private static readonly ConditionalWeakTable<IWorldModel, Dictionary<(uint Seed, LodLevel Lod), IReadOnlyList<SiteItem>>> Cache = new();
    private static readonly ConditionalWeakTable<IWorldModel, IReadOnlyList<(Vector2 Anchor, Collider Collider)>> ColliderCache = new();
    private static readonly object Sync = new();

    private readonly IReadOnlyList<NorthSite> _sites;

    public NorthModule() : this(NorthSites.All) { }

    internal NorthModule(IReadOnlyList<NorthSite> sites)
    {
        _sites = sites;
        Claims = sites.SelectMany(s => s.Claims).ToArray();
        Probes = sites.SelectMany(s => s.Probes).ToArray();
    }

    public IReadOnlyList<Claim> Claims { get; }

    public IReadOnlyList<InspectionProbe> Probes { get; }

    /// <summary>Per 64 m cell; measured on the 20a sites (the heaviest L0 cell holds a gate with its guard house and fences).</summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: NorthTune.BudgetTrianglesL0, TrianglesL1: NorthTune.BudgetTrianglesL1, DrawCalls: NorthTune.BudgetDrawCalls);

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var cells = new SortedSet<(int X, int Z)>();
        foreach (var item in ItemsFor(ctx, LodLevel.L0)) cells.Add(Cell(item.Anchor, LodLevel.L0));
        foreach (var (anchor, _) in CollidersFor(ctx)) cells.Add(Cell(anchor, LodLevel.L0));
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
        return CollidersFor(ctx).Where(i => Cell(i.Anchor, LodLevel.L0) == (tile.X, tile.Z)).Select(i => i.Collider).ToArray();
    }

    /// <summary>All items of one level, built once per world, seed and level; every Build call on any tile agrees.</summary>
    internal IReadOnlyList<SiteItem> ItemsFor(BuildContext ctx, LodLevel lod)
    {
        lock (Sync)
        {
            var shared = ReferenceEquals(_sites, NorthSites.All);
            var byKey = Cache.GetValue(ctx.World, _ => []);
            if (shared && byKey.TryGetValue((ctx.WorldSeed, lod), out var found)) return found;
            var builder = new SiteBuilder(ctx, lod);
            foreach (var site in _sites) site.Build(builder);
            var items = builder.Items.ToArray();
            if (shared) byKey[(ctx.WorldSeed, lod)] = items;
            return items;
        }
    }

    /// <summary>All colliders, built once per world: the fence runs are long, and every tile asks.</summary>
    internal IReadOnlyList<(Vector2 Anchor, Collider Collider)> CollidersFor(BuildContext ctx)
    {
        lock (Sync)
        {
            var shared = ReferenceEquals(_sites, NorthSites.All);
            if (shared && ColliderCache.TryGetValue(ctx.World, out var found)) return found;
            var colliders = new SiteColliders(ctx);
            foreach (var site in _sites) site.Collide(colliders);
            var items = colliders.Items.ToArray();
            if (shared) ColliderCache.AddOrUpdate(ctx.World, items);
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
/// The north zone's sites in a fixed order. 20a: the 学知苑 and international students' cluster enclosures and
/// 学知苑 1 栋's open front door. 20b appends 硕丰苑 and 博翰苑 (clusters into <see cref="NorthTune.Clusters"/> plus their
/// own sites, and the three blocks of WLD-INTERIORS §17); 20c appends the canteens, the shopping street, the student
/// centre and auditorium, the hospital, the covered walkway and the cultural landmarks, after these.
/// </summary>
public static class NorthSites
{
    public static IReadOnlyList<NorthSite> All { get; } = Array.AsReadOnly<NorthSite>(
    [
        // 20a
        .. NorthTune.Clusters.Select(c => (NorthSite)new Clusters.ClusterSite(c)),
        new Xuezhi.HallEntranceSite(),
        // 20b, 20c: append here.
    ]);
}
