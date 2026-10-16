using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Walkable;

/// <summary>Bridge occupancy and its actual collision tops supplied by the offline composition root.</summary>
internal sealed class WalkBridgeSurfaces
{
    internal IReadOnlyList<Footprint2D> Footprints { get; }
    internal IReadOnlySet<FeatureId> ReplacedLines { get; }
    private readonly (Footprint2D Footprint, Vector2 Min, Vector2 Max)[] _areas;
    private readonly RampCollider[] _ramps;

    internal WalkBridgeSurfaces(IWorldModel world, IEnumerable<Footprint2D> occupancy, IEnumerable<RampCollider> supports)
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
        _ramps = supports.Where(ramp => ramp.Tag == SurfaceTag.Ground).ToArray();
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
        y = 0f;
        slopeSquared = 0f;
        if (!_areas.Any(area => point.X >= area.Min.X && point.X <= area.Max.X &&
            point.Y >= area.Min.Y && point.Y <= area.Max.Y && WalkGeometry.Contains(area.Footprint.Rings, point))) return false;
        var found = false;
        var supportY = 0f;
        var supportSlope = 0f;
        foreach (var ramp in _ramps)
        {
            Sample(ramp.P0, ramp.P1, ramp.P2);
            Sample(ramp.P0, ramp.P2, ramp.P3);
        }
        if (!found) throw new InvalidOperationException("Bridge occupancy has no matching collision-top support.");
        y = supportY;
        slopeSquared = supportSlope;
        return true;

        void Sample(Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = new Vector2(b.X - a.X, b.Z - a.Z);
            var ac = new Vector2(c.X - a.X, c.Z - a.Z);
            var ap = point - new Vector2(a.X, a.Z);
            var determinant = ab.X * ac.Y - ab.Y * ac.X;
            if (Math.Abs(determinant) < .000001f) return;
            var u = (ap.X * ac.Y - ap.Y * ac.X) / determinant;
            var v = (ab.X * ap.Y - ab.Y * ap.X) / determinant;
            if (u < -.00001f || v < -.00001f || u + v > 1.00001f) return;
            var height = a.Y + u * (b.Y - a.Y) + v * (c.Y - a.Y);
            if (!float.IsFinite(height)) throw new InvalidOperationException("Bridge collision-top support must be finite.");
            if (found && height <= supportY) return;
            supportY = height;
            var dx = ((b.Y - a.Y) * ac.Y - (c.Y - a.Y) * ab.Y) / determinant;
            var dz = (ab.X * (c.Y - a.Y) - ac.X * (b.Y - a.Y)) / determinant;
            supportSlope = dx * dx + dz * dz;
            found = true;
        }
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
