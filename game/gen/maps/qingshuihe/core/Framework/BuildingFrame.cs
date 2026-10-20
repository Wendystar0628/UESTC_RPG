using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using FacadeRules = Campus.Gen.Buildings.Rules.FacadeRules;

namespace Campus.Gen.Maps.Qingshuihe.Core.Framework;

/// <summary>
/// Vertical datum and plan of one building the core zone builds itself (the library, its fourteen blocks, the
/// museum): every building piece starts from this frame so all of them sit on the terrain the same way.
/// <para>
/// WLD-TERRAIN: the floor is PadY + <see cref="FacadeRules.FloorAbovePad"/>, walls start at
/// PadY − <see cref="FacadeRules.WallBelowPad"/> to cover the apron's small fall, and the plinth has a level top line at
/// PadY + <see cref="FacadeRules.PlinthTop"/>. Storey heights are the building's own (MAP-QSH-CORE initial values),
/// so storey k's floor is Floor + sum of the storeys below it.
/// </para>
/// </summary>
public sealed record BuildingFrame(FeatureId Feature, IReadOnlyList<PolygonRings> Footprints, float PadY, IReadOnlyList<float> StoreyHeights)
{
    public float FloorY => PadY + FacadeRules.FloorAbovePad;
    public float WallBaseY => PadY - FacadeRules.WallBelowPad;
    public float PlinthTopY => PadY + FacadeRules.PlinthTop;
    public float EavesY => FloorY + StoreyHeights.Sum();

    /// <summary>Floor level of storey <paramref name="storey"/> (0 = ground floor).</summary>
    public float StoreyY(int storey)
    {
        if (storey < 0 || storey > StoreyHeights.Count) throw new ArgumentOutOfRangeException(nameof(storey));
        var y = FloorY;
        for (var i = 0; i < storey; i++) y += StoreyHeights[i];
        return y;
    }

    /// <summary>Area-weighted centroid of all footprints' outer rings (the anchor for the building's tile).</summary>
    public Vector2 Centroid => PlanPolygons.Centroid(Footprints.Select(f => f.Outer));

    /// <summary>
    /// Frame of a building feature from the world model. The footprints are the facade outlines of its source parts when it has any
    /// (a multi-wing entity keeps each wing; WP-08 keeps the physical shared-edge outline for terrain and pads), otherwise its own polygon geometry. PadY comes from the terrain (WP-08;
    /// the flat stub returns 0), never from a literal.
    /// </summary>
    public static BuildingFrame Of(IWorldModel world, FeatureId feature, IReadOnlyList<float> storeyHeights)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Features.TryGetValue(feature, out var f))
            throw new KeyNotFoundException($"Core building '{feature.Value}' is missing from the world model.");
        var rings = WorldFeatureGeometry.FacadePolygonsOf(f);
        if (rings.Count == 0) throw new InvalidOperationException($"Core building '{feature.Value}' has no footprint.");
        return new(feature, rings, world.Terrain.PadY(feature), storeyHeights);
    }
}
