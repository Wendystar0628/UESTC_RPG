using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.Core.Framework;

/// <summary>
/// Vertical datum and plan of one building the core zone builds itself (the library, its fourteen blocks, the
/// museum): every building piece starts from this frame so all of them sit on the terrain the same way.
/// <para>
/// WLD-TERRAIN §4.3: the floor is PadY + <see cref="CoreStyle.FloorAbovePad"/>, walls start at
/// PadY − <see cref="CoreStyle.WallBelowPad"/> to cover the apron's small fall, and the plinth has a level top line at
/// PadY + <see cref="CoreStyle.PlinthTop"/>. Storey heights are the building's own (MAP-QSH-CORE §10 initial values),
/// so storey k's floor is Floor + sum of the storeys below it.
/// </para>
/// </summary>
public sealed record BuildingFrame(FeatureId Feature, IReadOnlyList<PolygonRings> Footprints, float PadY, IReadOnlyList<float> StoreyHeights)
{
    public float FloorY => PadY + CoreStyle.FloorAbovePad;
    public float WallBaseY => PadY - CoreStyle.WallBelowPad;
    public float PlinthTopY => PadY + CoreStyle.PlinthTop;
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
    public Vector2 Centroid => Plan.Centroid(Footprints.Select(f => f.Outer));

    /// <summary>
    /// Frame of a building feature from the world model. The footprints are its source parts when it has any
    /// (a multi-wing entity keeps each wing), otherwise its own polygon geometry. PadY comes from the terrain (WP-08;
    /// the flat stub returns 0), never from a literal.
    /// </summary>
    public static BuildingFrame Of(IWorldModel world, FeatureId feature, IReadOnlyList<float> storeyHeights)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Features.TryGetValue(feature, out var f))
            throw new KeyNotFoundException($"Core building '{feature.Value}' is missing from the world model.");
        var rings = (f.Parts.Count > 0 ? f.Parts.Select(p => p.Geometry) : [f.Geometry]).SelectMany(Plan.Polygons).ToArray();
        if (rings.Length == 0) throw new InvalidOperationException($"Core building '{feature.Value}' has no footprint.");
        return new(feature, rings, world.Terrain.PadY(feature), storeyHeights);
    }
}

/// <summary>Plan helpers over contract geometry.</summary>
public static class Plan
{
    public static IEnumerable<PolygonRings> Polygons(OverlayGeometry geometry) => geometry switch
    {
        PolygonGeometry polygon => [polygon.Rings],
        MultiPolygonGeometry multi => multi.Polygons,
        PolylineGeometry or PointGeometry => [],
        _ => throw new ArgumentException("Unknown geometry.", nameof(geometry)),
    };

    /// <summary>Area-weighted centroid of simple rings; falls back to the vertex mean for degenerate input.</summary>
    public static Vector2 Centroid(IEnumerable<IReadOnlyList<Vector2>> rings)
    {
        double area = 0, x = 0, z = 0, mx = 0, mz = 0; var count = 0;
        foreach (var ring in rings)
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                var cross = (double)a.X * b.Y - (double)b.X * a.Y;
                area += cross; x += (a.X + b.X) * cross; z += (a.Y + b.Y) * cross;
                mx += a.X; mz += a.Y; count++;
            }
        if (count == 0) throw new ArgumentException("No points.", nameof(rings));
        return Math.Abs(area) > 1e-6 ? new((float)(x / (3 * area)), (float)(z / (3 * area))) : new((float)(mx / count), (float)(mz / count));
    }

    /// <summary>Outer ring of the feature's first polygon (its own geometry, or its first source part).</summary>
    public static IReadOnlyList<Vector2> OuterRing(WorldFeature feature)
    {
        var geometry = feature.Parts.Count > 0 ? feature.Parts[0].Geometry : feature.Geometry;
        return Polygons(geometry).FirstOrDefault()?.Outer
            ?? throw new InvalidOperationException($"Feature '{feature.Id.Value}' has no polygon.");
    }
}
