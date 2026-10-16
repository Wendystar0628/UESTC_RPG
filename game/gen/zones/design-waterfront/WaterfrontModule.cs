using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Zones.DesignWaterfront.Bridges;
using T = Campus.Gen.Zones.DesignWaterfront.WaterfrontTune;

namespace Campus.Gen.Zones.DesignWaterfront;

/// <summary>
/// WP-28 waterfront and bridges (WLD-WATER 分工, MAP-QSH-ADDITIONS §2.5, §2.12). 28a: the bridges registered in
/// waterfront.overlay.json (kind <see cref="T.BridgeKind"/>), each built from the network lines it names, plus the bridge-head
/// stones (kind <see cref="T.StoneKind"/>). The network itself is untouched: a bridge that merges several lines keeps them all
/// (路网拓扑照旧). Water surfaces stay WP-24's; the river banks between the bridges follow in the next slice.
/// Pure .NET: reads the world model and terrain only.
/// </summary>
[ContentModule("zone.waterfront", Owner = "WP-28")]
public sealed class WaterfrontModule : IContentModule
{
    private static readonly ConditionalWeakTable<IWorldModel, Site> Cache = new();
    private static readonly object Sync = new();

    public IReadOnlyList<Claim> Claims => [];

    public ModuleBudget Budget { get; } = new(T.BudgetTrianglesL0, T.BudgetTrianglesL1, T.BudgetDrawCalls);

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var site = SiteOf(ctx.World);
        var cells = new SortedSet<(int X, int Z)>();
        foreach (var plan in site.Bridges) cells.Add(Cell(plan.Centre, LodLevel.L0));
        foreach (var stone in site.Stones) cells.Add(Cell(stone.Position, LodLevel.L0));
        foreach (var (anchor, _) in site.Colliders) cells.Add(Cell(anchor, LodLevel.L0));
        return [.. cells.Select(c => new TileKey(c.X, c.Z))];
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var site = SiteOf(ctx.World);
        var meshes = new List<GeometryBundle>();
        var occupancy = new List<Footprint2D>();
        foreach (var plan in site.Bridges)
        {
            if (lod != LodLevel.L4 && Cell(plan.Centre, lod) != (tile.X, tile.Z)) continue;
            if (BridgeMesher.Build(plan, lod) is { } geometry) meshes.AddRange(geometry.Meshes);
            occupancy.Add(new(plan.Id, new PolygonRings(plan.Footprint), plan.RoadY - T.SlabDepth, plan.WalkY + T.HeadTop));
        }
        foreach (var stone in site.Stones)
        {
            if (lod != LodLevel.L4 && Cell(stone.Position, lod) != (tile.X, tile.Z)) continue;
            if (stone.Build(lod) is { } geometry) meshes.AddRange(geometry.Meshes);
        }
        if (meshes.Count == 0 && occupancy.Count == 0) return ContentOutput.Empty;
        return new ContentOutput(meshes, [], Occupancy: occupancy);
    }

    /// <summary>Colliders live in fixed 64 m cells, filed by each collider's own centre.</summary>
    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return [.. SiteOf(ctx.World).Colliders.Where(c => Cell(c.Anchor, LodLevel.L0) == (tile.X, tile.Z)).Select(c => c.Collider)];
    }

    internal static Site SiteOf(IWorldModel world)
    {
        lock (Sync) return Cache.GetValue(world, w => new Site(w));
    }

    private static (int X, int Z) Cell(Vector2 p, LodLevel lod)
    {
        var size = 64f * (1 << (int)lod);
        return ((int)MathF.Floor(p.X / size), (int)MathF.Floor(p.Y / size));
    }

    /// <summary>Everything this module builds for one world, resolved once.</summary>
    internal sealed class Site
    {
        public Site(IWorldModel world)
        {
            Bridges = BridgePlan.All(world);
            Stones = BridgeStone.All(world);
            Colliders = [.. Bridges.SelectMany(BridgeColliders.Of), .. Stones.Select(s => (s.Position, s.Collider))];
        }

        public IReadOnlyList<BridgePlan> Bridges { get; }
        public IReadOnlyList<BridgeStone> Stones { get; }
        public IReadOnlyList<(Vector2 Anchor, Collider Collider)> Colliders { get; }
    }
}
