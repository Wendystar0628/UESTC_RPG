using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Vegetation;
using Campus.Gen.Geometry;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>Blade form of a grass clump: the trimmed short lawn, or the taller, patchier grass of a transition green.</summary>
public enum GrassStyle : byte { Short = 0, Tall = 1 }

/// <summary>
/// One card-grass clump (WLD-VEGETATION): its foot on the lawn surface, yaw, uniform scale, a stable seed, and the lawn
/// tone (palette column) of the ground it stands on, so that its blades melt into the lawn's own colour field with distance.
/// </summary>
public sealed record GrassClump(Vector3 Position, float Yaw, float Scale, float Seed, ushort Tone, float WindPos, GrassStyle Style)
{
    /// <summary>INSTANCE_CUSTOM of veg_grass: (seed, lawn tone column, wind position, 0).</summary>
    public Vector4 Custom => new(Seed, Tone, WindPos, 0f);
}

/// <summary>
/// Near grass of the authored grass areas (overlay kind grass-area, <see cref="GrassInputs"/>). Pure and deterministic from the
/// world model, the tree layout and the world seed. Clumps fill each area on a jittered grid and keep off everything that is not
/// lawn: buildings, water, courts, parking, roads, pavement, tree pits and trunks, the exclude areas and every setback.
/// Edges follow §3.5: walks and paths under <see cref="SoftEdgeWidth"/> take a soft edge the blades reach over by up to
/// <see cref="SoftReach"/>; roads, plazas, wide walks and buildings keep a clean edge; steps keep <see cref="EntranceSetback"/>.
/// Beyond the near radius the lawn itself is the grass (veg_grass sinks the blades into it), so each clump takes the lawn tone
/// of the ground under it: the colour authority is the shared <see cref="LawnTone"/> and lawn noise.
/// </summary>
public static class GrassField
{
    /// <summary>Walks and paths narrower than this (m) take a soft grass edge (§3.5).</summary>
    public const float SoftEdgeWidth = 3f;

    /// <summary>How far blades may reach over a soft edge (m; §3.5 allows up to 0.15).</summary>
    public const float SoftReach = 0.12f;

    /// <summary>Blades stop this short of a clean edge (m): kerbs, plazas, wide walks, buildings, water.</summary>
    public const float CleanGap = 0.05f;

    /// <summary>Setback kept from steps (m; §3.5 doors, steps, benches, shore exits).</summary>
    public const float EntranceSetback = 1f;

    /// <summary>No blade this close to a trunk (m): tree feet are collared by shrubs and ornamental grass, not by flat lawn.</summary>
    public const float TrunkGap = 0.45f;

    /// <summary>Kerb width beside a carriageway, outside the road's own half width (m; WP-15's kerb).</summary>
    public const float KerbWidth = 0.15f;

    /// <summary>Grid spacing of the clumps (m) per style.</summary>
    public static float Spacing(GrassStyle style) => style == GrassStyle.Short ? 0.3f : 0.42f;

    /// <summary>Tall grass grows in patches: cell size of the patch noise (m) and the share of ground left open.</summary>
    public const float TallPatchCell = 7f, TallOpen = 0.4f;
    private const float MinimumScale = 0.86f, ScaleRange = 0.28f;
    public const float MaximumScale = MinimumScale + ScaleRange;

    public static IReadOnlyList<GrassClump> Build(GrassRuntimeData data, IWorldModel world, PlantingLayout trees, uint seed, TileKey tile)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(trees);
        if (data.Areas.Count == 0) return [];
        var origin = TileGrid.Origin(tile);
        var end = origin + new Vector2(TileGrid.BaseEdge);
        var site = new PlantingSite(world, seed);
        var trunks = new SpatialGrid<Vector2>(4f);
        foreach (var tree in trees.Trees)
        {
            if (TreeForms.IsBall(tree.Species.Form)) continue;
            var at = new Vector2(tree.Position.X, tree.Position.Z);
            trunks.AddPoint(at, at);
        }
        var excludes = data.Areas.Where(a => a.Category == GrassCategory.Exclude).ToArray();
        var lawn = LawnTone.Default;
        var toneSeed = LawnNoise.Seed(seed);
        var clumps = new List<GrassClump>();
        foreach (var area in data.Areas)
        {
            if (area.Category == GrassCategory.Exclude) continue;
            var style = area.Category == GrassCategory.Short ? GrassStyle.Short : GrassStyle.Tall;
            var reach = GrassClumps.Reach(style);
            var step = Spacing(style);
            var salt = StableHash.Hash32(seed, "grass", area.Id.Value);
            var patch = StableHash.Hash32(seed, "grass-patch", area.Id.Value);
            foreach (var polygon in area.Polygons)
            {
                var min = polygon.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                var max = polygon.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
                if (max.X < origin.X || min.X >= end.X || max.Y < origin.Y || min.Y >= end.Y) continue;
                var firstX = Math.Max((int)MathF.Floor(min.X / step), (int)MathF.Floor(origin.X / step) - 1);
                var firstZ = Math.Max((int)MathF.Floor(min.Y / step), (int)MathF.Floor(origin.Y / step) - 1);
                for (var j = firstZ; j * step <= max.Y && j * step < end.Y; j++)
                    for (var i = firstX; i * step <= max.X && i * step < end.X; i++)
                    {
                        var h = IntegerNoise.Hash2D(unchecked((uint)i), unchecked((uint)j), salt);
                        var u = IntegerNoise.High24(h);
                        var v = IntegerNoise.Low24(h);
                        var p = new Vector2((i + 0.5f + 0.8f * (u - 0.5f)) * step, (j + 0.5f + 0.8f * (v - 0.5f)) * step);
                        if (TileGrid.At(p) != tile || !PlanGeometry.Contains(polygon, p)) continue;
                        if (area.SetbackMeters is { } inset && Edge(polygon, p) < inset) continue;
                        if (style == GrassStyle.Tall && IntegerNoise.Value2D(p.X / TallPatchCell, p.Y / TallPatchCell, patch) < TallOpen) continue;
                        if (Excluded(excludes, p, reach) || !OnLawn(site, trunks, p, reach)) continue;
                        var k = IntegerNoise.Mix(h ^ 0x51ED27u);
                        var scale = MinimumScale + ScaleRange * IntegerNoise.High24(k);
                        var y = world.Terrain.HeightAt(p.X, p.Y) + GroundLevels.Lawn;
                        var band = lawn.EdgeBand;
                        var darkEdge = band > 0f && (site.Blocked(p, band) || site.OnRoad(p, band + KerbWidth, band, band, band) || site.IsPaved(p, band));
                        clumps.Add(new GrassClump(new Vector3(p.X, y, p.Y), 6.2831853f * IntegerNoise.Low24(k), scale,
                            IntegerNoise.High24(IntegerNoise.Mix(k)), LawnNoise.ColorAt(p.X, p.Y, toneSeed, lawn, darkEdge), site.WindPos(p.X), style));
                    }
            }
        }
        return clumps.AsReadOnly();
    }

    /// <summary>Conservative polygon directory; does not instantiate clumps or sample terrain.</summary>
    public static IReadOnlyList<TileKey> Tiles(GrassRuntimeData data)
    {
        var tiles = new HashSet<TileKey>();
        foreach (var area in data.Areas)
        {
            if (area.Category == GrassCategory.Exclude) continue;
            foreach (var polygon in area.Polygons)
            {
                var min = TileGrid.At(polygon.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min));
                var max = TileGrid.At(polygon.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max));
                for (var z = min.Z; z <= max.Z; z++)
                    for (var x = min.X; x <= max.X; x++) tiles.Add(new(x, z));
            }
        }
        return tiles.OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();
    }

    /// <summary>Lawn ground, clear of every non-lawn surface by the edge rule of each, of tree pits and of trunks.</summary>
    private static bool OnLawn(PlantingSite site, SpatialGrid<Vector2> trunks, Vector2 p, float reach) =>
        PlanGeometry.Contains(site.World.Boundary, p) && !site.InPit(p) &&
        !site.Blocked(p, reach + CleanGap) &&
        !site.OnRoad(p, reach + CleanGap + KerbWidth, reach + CleanGap, reach - SoftReach, reach + EntranceSetback) &&
        !site.IsPaved(p, reach + CleanGap, reach - SoftReach, reach + EntranceSetback) &&
        !trunks.AnyAround(p, q => Vector2.DistanceSquared(p, q) < (TrunkGap + reach) * (TrunkGap + reach));

    /// <summary>Inside an exclude area, or within its setback (outward) plus the clump's reach.</summary>
    private static bool Excluded(GrassAreaData[] excludes, Vector2 p, float reach)
    {
        foreach (var area in excludes)
        {
            var gap = (area.SetbackMeters ?? 0f) + reach;
            foreach (var polygon in area.Polygons)
                if (PlanGeometry.Contains(polygon, p) || Edge(polygon, p) < gap) return true;
        }
        return false;
    }

    private static float Edge(PolygonRings rings, Vector2 p) =>
        rings.Holes.Aggregate(PlanGeometry.DistanceToRing(p, rings.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h)));
}
