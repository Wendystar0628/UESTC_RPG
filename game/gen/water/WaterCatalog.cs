using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.World;

namespace Campus.Gen.Water;

public enum WaterKind : byte { Lake, Channel }

/// <summary>One water surface: a lake, pond or a piece of channel, in plan coordinates (x, z).</summary>
public sealed record WaterBody(
    FeatureId Id,
    WaterKind Kind,
    PolygonRings Rings,
    float SurfaceY,
    int ReflectSlot,
    Vector2 Min,
    Vector2 Max,
    float Area)
{
    /// <summary>Even-odd containment over the outer ring and holes.</summary>
    public bool Contains(Vector2 p)
    {
        if (p.X < Min.X || p.X > Max.X || p.Y < Min.Y || p.Y > Max.Y) return false;
        var inside = PlanGeometry.InsideByCrossings(Rings.Outer, p);
        foreach (var hole in Rings.Holes) inside ^= PlanGeometry.InsideByCrossings(hole, p);
        return inside;
    }
}

/// <summary>
/// Reads the water bodies of one map from the world model. Pure and deterministic: the same world gives the same list.
/// Reflection candidates come from map.json env.reflectLakes; the list order is the lake number (WLD-WATER).
/// </summary>
public static class WaterCatalog
{
    public static readonly DesignKind WaterKindKey = new("water");

    public static IReadOnlyList<WaterBody> Build(IWorldModel world, MapDescriptor map, WaterParams? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(map);
        var p = parameters ?? WaterParams.Default;
        var slots = new Dictionary<FeatureId, int>();
        for (var i = 0; i < map.Env.ReflectLakes.Count; i++) slots.TryAdd(map.Env.ReflectLakes[i], i);

        var bodies = new List<WaterBody>();
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Kind != WaterKindKey) continue;
            var slot = slots.TryGetValue(feature.Id, out var s) ? s : -1;
            // A feature with parts is built part by part; its aggregate geometry is only the query envelope.
            var geometries = feature.Parts.Count > 0 ? feature.Parts.Select(part => part.Geometry) : [feature.Geometry];
            foreach (var geometry in geometries)
                foreach (var polygon in Polygons(geometry))
                    if (Body(world, feature.Id, polygon, slot, p) is { } body) bodies.Add(body);
        }
        return bodies.AsReadOnly();
    }

    private static IEnumerable<PolygonRings> Polygons(OverlayGeometry geometry) => geometry switch
    {
        PolygonGeometry polygon => [polygon.Rings],
        MultiPolygonGeometry multi => multi.Polygons,
        _ => [],
    };

    private static WaterBody? Body(IWorldModel world, FeatureId id, PolygonRings source, int slot, WaterParams p)
    {
        var rings = Clean(source);
        if (rings is null) return null;
        var area = Area(rings);
        if (area < 1f) return null;
        var (min, max) = Extent(rings.Outer);
        var perimeter = Perimeter(rings);
        var elongation = perimeter * perimeter / (4f * float.Pi * area);
        var kind = slot < 0 && elongation > p.ChannelElongation ? WaterKind.Channel : WaterKind.Lake;
        // The terrain owns the absolute level; its ground height here is the already carved bed, not the bank top.
        var y = world.Terrain.WaterY(id)
            ?? throw new WorldDataException("ITerrain.WaterY", id.Value, "Water feature has no terrain water level.");
        return new(id, kind, rings, y, slot, min, max, area);
    }

    private static PolygonRings? Clean(PolygonRings rings)
    {
        if (rings.Outer.Any(point => !VectorMath.Finite(point))) return null;
        var outer = PlanGeometry.DeduplicateRing(rings.Outer);
        if (outer.Length < 3) return null;
        var holes = new List<IReadOnlyList<Vector2>>();
        foreach (var hole in rings.Holes)
        {
            if (hole.Any(point => !VectorMath.Finite(point))) continue;
            var points = PlanGeometry.DeduplicateRing(hole);
            if (points.Length >= 3) holes.Add(points);
        }
        return new(outer, holes.AsReadOnly());
    }

    public static float Area(PolygonRings rings)
    {
        var area = MathF.Abs((float)PlanGeometry.SignedArea(rings.Outer));
        foreach (var hole in rings.Holes) area -= MathF.Abs((float)PlanGeometry.SignedArea(hole));
        return area;
    }

    private static float Perimeter(PolygonRings rings)
    {
        var length = (float)PlanLines.Length(rings.Outer, closed: true);
        foreach (var hole in rings.Holes) length += (float)PlanLines.Length(hole, closed: true);
        return length;
    }

    private static (Vector2 Min, Vector2 Max) Extent(IReadOnlyList<Vector2> ring)
    {
        var min = ring[0];
        var max = ring[0];
        foreach (var point in ring) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
        return (min, max);
    }
}
