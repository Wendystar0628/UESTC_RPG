using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.DesignPublic;

/// <summary>
/// WP-29 公共空间 (MAP-QSH-ADDITIONS §2.3, §2.7, §2.8, §7): only the unowned public spaces (D-185). 29a: the corner pocket
/// greens of the main junctions between the zones. The paving is a pedestrian polygon in overlay/public.d/ (WP-15 paves
/// it, WP-23 keeps trunks off it), the tree is a tree pit WP-23 plants; this module builds the curved bench and the pit
/// kerb and soil, their colliders and seat edges. Claims nothing: no OSM feature is replaced.
/// </summary>
[ContentModule("zone.design-public", Owner = "WP-29")]
public sealed class PublicModule : IContentModule
{
    public IReadOnlyList<Claim> Claims { get; } = [];

    /// <summary>ENG-BUDGET §8: WP-29 has 0.5 % (12,500 fine triangles) per view; one pocket green measures about 3k at L0.</summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: 12_000, TrianglesL1: 6_000, DrawCalls: 4);

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return Anchors(ctx.World).Select(a => Cell(a, LodLevel.L0)).Distinct().OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        // Pocket greens are a few metres across: L3 cells (512 m) and the whole-map L4 product carry nothing.
        if (lod is LodLevel.L3 or LodLevel.L4) return ContentOutput.Empty;
        var world = ctx.World;
        float Ground(Vector2 p) => world.Terrain.HeightAt(p.X, p.Y);
        var meshes = new List<GeometryBundle>();
        var seats = new List<SeatDecl>();
        foreach (var bench in PocketGreens.Benches(world))
        {
            if (Cell(bench.Centre, lod) != tile) continue;
            PocketGreens.Build(bench, Ground, lod, meshes);
            if (lod == LodLevel.L0) seats.AddRange(PocketGreens.Seats(bench, Ground));
        }
        foreach (var pit in PocketGreens.Pits(world))
            if (Cell(pit.Centre, lod) == tile) PocketGreens.Build(pit, Ground, lod, meshes);
        if (meshes.Count == 0) return ContentOutput.Empty;
        return new ContentOutput(GeometryBatch.Merge(meshes, GeometryMergePolicy.MaterialGroups), []) { Seats = seats };
    }

    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var world = ctx.World;
        float Ground(Vector2 p) => world.Terrain.HeightAt(p.X, p.Y);
        var colliders = new List<Collider>();
        foreach (var bench in PocketGreens.Benches(world))
            if (Cell(bench.Centre, LodLevel.L0) == tile) colliders.AddRange(PocketGreens.SeatBoxes(bench, Ground));
        foreach (var pit in PocketGreens.Pits(world))
            if (Cell(pit.Centre, LodLevel.L0) == tile) colliders.Add(PocketGreens.PitPost(pit, Ground));
        return colliders;
    }

    /// <summary>Every bench and pit is filed by its own centre, so a pocket green split by a cell line still builds whole pieces.</summary>
    private static IEnumerable<Vector2> Anchors(IWorldModel world) =>
        PocketGreens.Benches(world).Select(b => b.Centre).Concat(PocketGreens.Pits(world).Select(p => p.Centre));

    internal static TileKey Cell(Vector2 p, LodLevel lod)
    {
        var size = 64f * (1 << (int)lod);
        return new((int)MathF.Floor(p.X / size), (int)MathF.Floor(p.Y / size));
    }

}
