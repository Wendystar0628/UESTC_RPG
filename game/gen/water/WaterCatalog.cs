using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

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
        var inside = Crossings(Rings.Outer, p);
        foreach (var hole in Rings.Holes) inside ^= Crossings(hole, p);
        return inside;
    }

    private static bool Crossings(IReadOnlyList<Vector2> ring, Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[i];
            var b = ring[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
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
        var centre = (min + max) * 0.5f;
        // One normal water level per body (D-180): bank top minus the bank-type drop. Mesh, reflection mirror plane and
        // everything else read this same h. ITerrain.WaterY (contract-15) replaces the rule when it lands.
        var bank = world.Spatial.GroundY(centre.X, centre.Y);
        var y = kind == WaterKind.Lake ? bank - p.LakeDrop + p.LakeLift : bank - p.ChannelDrop;
        return new(id, kind, rings, y, slot, min, max, area);
    }

    private static PolygonRings? Clean(PolygonRings rings)
    {
        var outer = CleanRing(rings.Outer);
        if (outer is null) return null;
        var holes = new List<IReadOnlyList<Vector2>>();
        foreach (var hole in rings.Holes)
            if (CleanRing(hole) is { } h) holes.Add(h);
        return new(outer, holes.AsReadOnly());
    }

    private static IReadOnlyList<Vector2>? CleanRing(IReadOnlyList<Vector2> ring)
    {
        var points = new List<Vector2>(ring.Count);
        foreach (var point in ring)
        {
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) return null;
            if (points.Count == 0 || Vector2.DistanceSquared(points[^1], point) > 1e-8f) points.Add(point);
        }
        if (points.Count > 1 && Vector2.DistanceSquared(points[0], points[^1]) <= 1e-8f) points.RemoveAt(points.Count - 1);
        return points.Count >= 3 ? points.AsReadOnly() : null;
    }

    public static float Area(PolygonRings rings)
    {
        var area = MathF.Abs(SignedArea(rings.Outer));
        foreach (var hole in rings.Holes) area -= MathF.Abs(SignedArea(hole));
        return area;
    }

    public static float SignedArea(IReadOnlyList<Vector2> ring)
    {
        double sum = 0d;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            sum += (double)ring[j].X * ring[i].Y - (double)ring[i].X * ring[j].Y;
        return (float)(sum * 0.5d);
    }

    private static float Perimeter(PolygonRings rings)
    {
        var length = RingLength(rings.Outer);
        foreach (var hole in rings.Holes) length += RingLength(hole);
        return length;
    }

    private static float RingLength(IReadOnlyList<Vector2> ring)
    {
        var length = 0f;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++) length += Vector2.Distance(ring[i], ring[j]);
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
