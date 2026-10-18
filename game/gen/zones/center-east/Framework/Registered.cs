using System.Numerics;
using System.Text.Json;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.CenterEast.Framework;

/// <summary>
/// Shape reads of the outlines and props this zone registered in the map data (D-239); the reads by ID are FeatureReads.
/// </summary>
internal static class Registered
{
    /// <summary>A two-number JSON array as a plan vector.</summary>
    public static Vector2 Pair(JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle());

    /// <summary>Axis-aligned bounds of a ring in a frame's local (x, z).</summary>
    public static (Vector2 Min, Vector2 Max) LocalBounds(PlanFrame frame, IEnumerable<Vector2> ring)
    {
        Vector2 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var p in ring)
        {
            var local = frame.Local(p);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        return (min, max);
    }

    /// <summary>The two points of a ring farthest apart (the chord of an arc-shaped outline), the western one first.</summary>
    public static (Vector2 From, Vector2 To) Chord(IReadOnlyList<Vector2> ring)
    {
        Vector2 a = ring[0], b = ring[1];
        var best = -1f;
        for (var i = 0; i < ring.Count; i++)
            for (var j = i + 1; j < ring.Count; j++)
            {
                var distance = Vector2.DistanceSquared(ring[i], ring[j]);
                if (distance <= best) continue;
                best = distance;
                a = ring[i];
                b = ring[j];
            }
        return a.X <= b.X ? (a, b) : (b, a);
    }

    /// <summary>A long four-sided strip as its centre line (end midpoints, the end nearer <paramref name="near"/> first) and width.</summary>
    public static (Vector2 From, Vector2 To, float Width) Strip(IReadOnlyList<Vector2> ring, Vector2 near)
    {
        var n = ring.Count;
        var i = Enumerable.Range(0, n).MaxBy(k => Vector2.Distance(ring[k], ring[(k + 1) % n]));
        Vector2 a = (ring[i] + ring[(i + n - 1) % n]) * 0.5f, b = (ring[(i + 1) % n] + ring[(i + 2) % n]) * 0.5f;
        var width = Vector2.Distance(ring[(i + 1) % n], ring[(i + 2) % n]);
        return Vector2.DistanceSquared(a, near) <= Vector2.DistanceSquared(b, near) ? (a, b, width) : (b, a, width);
    }
}
