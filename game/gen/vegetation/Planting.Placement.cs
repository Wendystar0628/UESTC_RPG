using Campus.Gen.Geometry;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>Planting, common: variant, scale, terrain, clearance and the pavement giving-way.</summary>
public static partial class Planting
{
    // ---------------------------------------------------------------------------------------------------------
    // Common: variant, scale, terrain, clearance.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>The variant, scale and seed a key gets, before the position is tested.</summary>
    private static PlantedTree Probe(Site site, SpeciesInfo info, string key)
    {
        var h = StableHash.Hash32(site.Seed, "veg", key);
        var seed = (h >> 8) * (1f / 16777216f);
        var h2 = StableHash.Hash32(h, "look");
        var variant = (int)(h2 % TreeForms.Variants);
        var scale = 0.92f + 0.16f * ((h2 >> 8) & 0xffff) / 65535f;
        var yaw = 6.2831853f * (StableHash.Hash32(h, "yaw") >> 8) * (1f / 16777216f);
        var height = TreeForms.HeightOf(info, variant) * scale;
        return new PlantedTree(key, info, variant, Vector3.Zero, yaw, scale, height, false, PlantSource.Grove, seed, 0f);
    }

    private static bool Try(Site site, SpeciesInfo info, Vector2 p, string key, bool wash, PlantSource source, List<PlantedTree> trees, bool crownClear)
    {
        if (!PlanGeometry.Contains(site.World.Boundary, p)) return false;
        var probe = Probe(site, info, key);
        var crown = probe.CrownRadius;
        // Trunks keep off building, water, court and parking outlines and road surfaces; grove crowns keep clear entirely.
        var keep = crownClear ? crown + 0.7f : TrunkClearance;
        if (site.Blocked(p, keep) || site.OnRoad(p, crownClear ? crown * 0.5f + TrunkClearance : TrunkClearance)) return false;
        if (crownClear && !site.Spaced(p, crown * StandOverlap)) return false;
        var terrain = site.World.Terrain;
        if (terrain.FactsAt(p.X, p.Y).Grade > MaxGrade) return false;
        var position = new Vector3(p.X, terrain.HeightAt(p.X, p.Y), p.Y);
        var tree = probe with { Position = position, Wash = wash, Source = source, WindPos = site.WindPos(p.X) };
        site.Occupy(p, crown * (crownClear ? StandOverlap : 1f));
        // Pavement is tested last and the tree still counts as planted (its place stays taken, a grove's count and keys go on),
        // so only the trees on pavement leave the layout and every other tree stays exactly as it was.
        if (site.OnPavement(p)) site.Avoided.Add(new(trees.Count + site.Avoided.Count, tree));
        else trees.Add(tree);
        return true;
    }

    private static float Hash01(uint seed, string purpose, int x, int z) =>
        (StableHash.Hash32(seed, purpose, x.ToString(CultureInfo.InvariantCulture), z.ToString(CultureInfo.InvariantCulture)) >> 8) * (1f / 16777216f);

    private static string Key(string kind, int rule, int chain, int side, int row, int index) => string.Join(':', kind,
        rule.ToString(CultureInfo.InvariantCulture), chain.ToString(CultureInfo.InvariantCulture),
        side < 0 ? "l" : "r", row.ToString(CultureInfo.InvariantCulture), index.ToString(CultureInfo.InvariantCulture));

    private static float PolylineDistanceSquared(Vector2 p, IReadOnlyList<Vector2> line)
    {
        var best = float.MaxValue;
        for (var i = 1; i < line.Count; i++) best = MathF.Min(best, PlanGeometry.SegmentDistanceSquared(p, line[i - 1], line[i]));
        return best;
    }
}
