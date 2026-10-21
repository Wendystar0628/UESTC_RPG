using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.Geometry;
using static Campus.Gen.Vegetation.Planting;
using static Campus.Gen.Vegetation.TreePlacement;

namespace Campus.Gen.Vegetation;

/// <summary>Plants empty tree pits and authored design trees.</summary>
internal static class AuthoredTreePlanting
{
    // ---------------------------------------------------------------------------------------------------------
    // Tree pits and design trees (max-011 23c2b): planted last, so every earlier tree keeps its key, look and place.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Each tree pit of the overlays that no planted tree stands in gets its perPit trees (props.species, else the rules' pit
    /// species: square pits and strips one, round beds the other), evenly along the pit's long side. A pit that already holds a
    /// tree (an OSM point or a rule tree kept by the pit) is left as it is. Returns the number of such pits.
    /// </summary>
    internal static int PlantPits(PlantingSite site, SpeciesTable species, PlantingRules rules, List<PlantedTree> trees)
    {
        var held = 0;
        foreach (var pit in site.Pits)
        {
            if (pit.PerPit <= 0) continue;
            if (trees.Any(t => pit.Holds(new(t.Position.X, t.Position.Z))))
            {
                held++;
                continue;
            }
            var id = pit.Species is { } given && species.TryGet(given, out _) ? given : pit.Rings is null ? rules.Pits.Round : rules.Pits.Square;
            var info = species.Get(id);
            for (var k = 0; k < pit.PerPit; k++)
            {
                var p = pit.Centre + pit.Axis * (pit.HalfLength * 2f * ((k + 0.5f) / pit.PerPit - 0.5f));
                var key = pit.PerPit == 1 ? pit.Key : pit.Key + ":" + k.ToString(CultureInfo.InvariantCulture);
                Place(site, info, p, key, PlantSource.Pit, trees, scale: null);
            }
        }
        return held;
    }

    /// <summary>
    /// Trees the overlays place and hand to game/gen/vegetation/ (kind tree, props.planter matching <see cref="VegetationModule.ModuleId"/>, props.species): planted where they stand,
    /// with the mother model whose height is nearest to the feature's height, scaled to it. A design tree on top of an already
    /// planted one (within <see cref="SameTree"/>) is that tree.
    /// </summary>
    internal static void PlantDesignTrees(PlantingSite site, SpeciesTable species, List<PlantedTree> trees)
    {
        foreach (var feature in site.World.Features.Values.Where(f => f.Kind.Value == DesignTreeKind).OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Props?.OptionalText("planter") != VegetationModule.ModuleId || feature.Geometry is not PointGeometry point) continue;
            if (feature.Props?.OptionalText("species") is not { } id || !species.TryGet(id, out var info)) continue;
            var p = new Vector2(point.Position.X, point.Position.Z);
            if (trees.Any(t => Vector2.Distance(new(t.Position.X, t.Position.Z), p) < SameTree)) continue;
            Place(site, info!, p, feature.Id.Value, PlantSource.Design, trees, feature.Height is > 0f and var h ? h : null);
        }
    }

    /// <summary>A deliberately placed tree (pit or design): no clearance or pavement test, only the boundary and the terrain.</summary>
    internal static void Place(PlantingSite site, SpeciesInfo info, Vector2 p, string key, PlantSource source, List<PlantedTree> trees, float? scale)
    {
        if (!PlanRings.Contains(site.World.Boundary, p)) return;
        var probe = Probe(site, info, key);
        if (scale is { } height)
        {
            // The mother model nearest the given height, scaled to it exactly.
            var variant = Enumerable.Range(0, TreeForms.Variants).MinBy(v => MathF.Abs(TreeForms.HeightOf(info, v) - height));
            probe = probe with { Variant = variant, Scale = height / TreeForms.HeightOf(info, variant), Height = height };
        }
        var terrain = site.World.Terrain;
        var tree = probe with { Position = new Vector3(p.X, terrain.HeightAt(p.X, p.Y), p.Y), Source = source, WindPos = site.WindPos(p.X) };
        site.Occupy(p, tree.CrownRadius);
        trees.Add(tree);
    }
}
