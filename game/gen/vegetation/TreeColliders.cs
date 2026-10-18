using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Trunk colliders of the planted trees (WLD-VEGETATION, SYS-WALK): one vertical PostCollider per tree, the
/// breast-height trunk radius from its mother model, from the lowest ground under the trunk minus 0.1 m up to the first
/// scaffold branch, so one can walk under the crown. Barrier with the Bark contact material. Parameters are world
/// coordinates, matching visible geometry; the kernel localizes the complete C tile once. Shrubs and the other plantings do not collide.
/// </summary>
public static class TreeColliders
{
    public static IReadOnlyList<Collider> For(PlantingLayout layout, TileKey tile, ITerrain terrain)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(terrain);
        var origin = TileGrid.Origin(tile);
        var plans = new Dictionary<(string, int), (float Radius, float Branch)>();
        var colliders = new List<Collider>();
        foreach (var tree in layout.Trees)
        {
            var p = new Vector2(tree.Position.X, tree.Position.Z);
            if (p.X < origin.X || p.Y < origin.Y || p.X >= origin.X + TileGrid.BaseEdge || p.Y >= origin.Y + TileGrid.BaseEdge) continue;
            if (TreeForms.IsBall(tree.Species.Form)) continue;
            var key = (tree.Species.Id, tree.Variant);
            if (!plans.TryGetValue(key, out var trunk))
            {
                var plan = new TreePlan(tree.Species, tree.Variant);
                plans.Add(key, trunk = (plan.TrunkRadius, plan.BranchY));
            }
            var r = trunk.Radius * tree.Scale;
            // WLD-TERRAIN: Y0 is the lowest ground within the trunk footprint, minus 0.1 m.
            var low = tree.Position.Y;
            foreach (var d in new[] { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY })
                low = MathF.Min(low, terrain.HeightAt(p.X + d.X * r, p.Y + d.Y * r));
            colliders.Add(new PostCollider(p, r, low - 0.1f, tree.Position.Y + trunk.Branch * tree.Scale, SurfaceTag.Barrier)
            { Foot = FootMat.Bark });
        }
        return colliders;
    }
}
