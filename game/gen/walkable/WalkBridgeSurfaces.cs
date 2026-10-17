using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Walkable;

/// <summary>Bridge occupancy and its actual collision tops supplied by the offline composition root.</summary>
public sealed class WalkBridgeSurfaces
{
    internal IReadOnlyList<Footprint2D> Footprints { get; }
    internal IReadOnlySet<FeatureId> ReplacedLines { get; }
    private readonly (Footprint2D Footprint, Vector2 Min, Vector2 Max)[] _areas;
    private readonly WalkSupportSurfaces _supports;

    public WalkBridgeSurfaces(IWorldModel world, IEnumerable<Footprint2D> occupancy, IEnumerable<Collider> supports)
    {
        var bridges = world.Features.Values.Where(feature => feature.Kind.Value == "bridge-deck")
            .ToDictionary(feature => feature.Id);
        var footprints = new List<Footprint2D>();
        foreach (var footprint in occupancy.Where(item => bridges.ContainsKey(item.Feature))
            .OrderBy(item => item.Feature.Value, StringComparer.Ordinal))
        {
            if (footprints.Any(item => item.Feature == footprint.Feature && Same(item.Rings, footprint.Rings))) continue;
            footprints.Add(footprint);
        }
        // Coordinate order also fixes the order when a feature has several distinct footprints.
        footprints.Sort((a, b) =>
        {
            var feature = StringComparer.Ordinal.Compare(a.Feature.Value, b.Feature.Value);
            return feature != 0 ? feature : Compare(a.Rings, b.Rings);
        });
        Footprints = footprints;
        _areas = footprints.Select(item =>
        {
            var (min, max) = WalkGeometry.Bounds(item.Rings.Outer);
            return (item, min, max);
        }).ToArray();
        _supports = new WalkSupportSurfaces(supports);
        var lines = new HashSet<FeatureId>();
        foreach (var id in footprints.Select(item => item.Feature).Distinct())
        {
            if (bridges[id].Props is not { ValueKind: JsonValueKind.Object } props) continue;
            if (props.TryGetProperty("axis", out var axis) && axis.ValueKind == JsonValueKind.String)
                lines.Add(new FeatureId(axis.GetString()!));
            if (props.TryGetProperty("walks", out var walks) && walks.ValueKind == JsonValueKind.Array)
                foreach (var walk in walks.EnumerateArray())
                    if (walk.ValueKind == JsonValueKind.String) lines.Add(new FeatureId(walk.GetString()!));
        }
        ReplacedLines = lines;
    }

    internal bool TrySupport(Vector2 point, out float y, out float slopeSquared)
    {
        y = 0f; slopeSquared = 0f;
        var inside = false;
        foreach (var area in _areas)
            if (point.X >= area.Min.X && point.X <= area.Max.X && point.Y >= area.Min.Y && point.Y <= area.Max.Y
                && WalkGeometry.Contains(area.Footprint.Rings, point)) { inside = true; break; }
        if (!inside) return false;
        if (!_supports.TrySupport(point, float.PositiveInfinity, out y, out slopeSquared))
            throw new InvalidOperationException("Bridge occupancy has no matching collision-top support.");
        return true;
    }

    private static bool Same(PolygonRings a, PolygonRings b) => Compare(a, b) == 0;
    private static int Compare(PolygonRings a, PolygonRings b)
    {
        var outer = CompareRing(a.Outer, b.Outer);
        if (outer != 0) return outer;
        var holes = a.Holes.Count.CompareTo(b.Holes.Count);
        if (holes != 0) return holes;
        for (var i = 0; i < a.Holes.Count; i++)
        {
            var ring = CompareRing(a.Holes[i], b.Holes[i]);
            if (ring != 0) return ring;
        }
        return 0;
    }

    private static int CompareRing(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b)
    {
        var count = a.Count.CompareTo(b.Count);
        if (count != 0) return count;
        for (var i = 0; i < a.Count; i++)
        {
            var x = a[i].X.CompareTo(b[i].X);
            if (x != 0) return x;
            var z = a[i].Y.CompareTo(b[i].Y);
            if (z != 0) return z;
        }
        return 0;
    }
}
