using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;

namespace Campus.Kernel.Collision;

/// <summary>Natural geometry comes from Terrain; swim areas are ordinary polygon overlay features.</summary>
internal sealed class WaterTraversalCatalog
{
    public TerrainWaterBody[] Waters { get; }
    public PolygonRings[] SwimAreas { get; }

    public WaterTraversalCatalog(IWorldModel world, ITerrainWaterSource source)
    {
        Waters = source.Waters.ToArray();
        var areas = new List<PolygonRings>();
        foreach (var feature in world.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Kind.Value != "swim-area") continue;
            areas.AddRange(Polygons(feature.Geometry));
        }
        SwimAreas = areas.ToArray();
    }

    public static bool Contains(IReadOnlyList<PolygonRings> polygons, Vector2 point)
    {
        for (int i = 0; i < polygons.Count; i++)
            if (PlanRings.Contains(polygons[i], point)) return true;
        return false;
    }

    private static PolygonRings[] Polygons(WorldGeometry geometry)
    {
        var values = WorldFeatureGeometry.PolygonsOf(geometry);
        if (values.Count == 0) throw new JsonException("A swim area needs a nonempty polygon.");
        var result = values.Select(polygon => new PolygonRings(PlanRings.OpenRing(polygon.Outer),
            polygon.Holes.Select(ring => PlanRings.OpenRing(ring, false)).ToArray())).ToArray();
        return result;
    }
}
