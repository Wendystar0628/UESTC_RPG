using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Zones.DesignBoundary.Framework;

namespace Campus.Gen.Zones.DesignBoundary;

/// <summary>
/// WP-30 campus boundary and gates (WLD-OUTSKIRTS §7, MAP-QSH-ADDITIONS §2.11): the sites listed in
/// <see cref="BoundarySites.All"/>, filed into 64 m cells. 30a builds the north and west gates with a fence wing on each side
/// and the waterside plaza inside the north gate; the full boundary run follows in a later sub-ticket. Every closure collider
/// is tagged <see cref="SurfaceTag.Boundary"/>. Pure .NET: reads the world model and the terrain only.
/// </summary>
[ContentModule("zone.boundary", Owner = "WP-30")]
public sealed class BoundaryModule : IContentModule
{
    private static readonly ConditionalWeakTable<IWorldModel, Dictionary<(uint Seed, LodLevel Lod), IReadOnlyList<SiteItem>>> Cache = new();
    private static readonly ConditionalWeakTable<IWorldModel, IReadOnlyList<(Vector2 Anchor, Collider Collider)>> ColliderCache = new();
    private static readonly object Sync = new();

    private readonly IReadOnlyList<BoundarySite> _sites;

    public BoundaryModule() : this(BoundarySites.All) { }

    internal BoundaryModule(IReadOnlyList<BoundarySite> sites) => _sites = sites;

    public IReadOnlyList<Claim> Claims { get; } = [];

    public IReadOnlyList<InspectionProbe> Probes { get; } = [];

    /// <summary>Per 64 m cell; measured on the 30a sites (the heaviest L0 cell holds the north gate, its wings and forecourt).</summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: BoundaryTune.BudgetTrianglesL0, TrianglesL1: BoundaryTune.BudgetTrianglesL1, DrawCalls: BoundaryTune.BudgetDrawCalls);

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
            var shared = ReferenceEquals(_sites, BoundarySites.All);
            var byKey = Cache.GetValue(ctx.World, _ => []);
            if (shared && byKey.TryGetValue((ctx.WorldSeed, lod), out var found)) return found;
            var builder = new SiteBuilder(ctx, lod);
            foreach (var site in _sites) site.Build(builder);
            var items = builder.Items.ToArray();
            if (shared) byKey[(ctx.WorldSeed, lod)] = items;
            return items;
        }
    }

    /// <summary>All colliders, built once per world.</summary>
    internal IReadOnlyList<(Vector2 Anchor, Collider Collider)> CollidersFor(BuildContext ctx)
    {
        lock (Sync)
        {
            var shared = ReferenceEquals(_sites, BoundarySites.All);
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

/// <summary>The boundary package's sites in a fixed order; later sub-tickets append theirs after these.</summary>
public static class BoundarySites
{
    public static IReadOnlyList<BoundarySite> All { get; } = Array.AsReadOnly<BoundarySite>(
    [
        // 30a
        .. BoundaryTune.Gates.Select(g => (BoundarySite)new Gates.GateSite(g)),
        new Waterfront.NorthPlazaSite(),
        // 30b (the boundary run, the remaining gates): append here.
    ]);
}
