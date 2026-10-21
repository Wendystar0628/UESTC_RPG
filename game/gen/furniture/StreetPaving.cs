using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using static Campus.Gen.Furniture.StreetNetwork;

namespace Campus.Gen.Furniture;

/// <summary>Queries stable walk and paving candidates from the immutable street index.</summary>
internal sealed class StreetPaving
{
    private readonly StreetIndex _index;

    public StreetPaving(StreetIndex index) => _index = index;

    /// <summary>A walk near a point: the line, the nearest point on it and that point's arc length along the line.</summary>
    public readonly record struct WalkPoint(NetworkLine Line, Vector2 Point, float Along, float Distance);

    /// <summary>Strollable walks within <paramref name="reach"/> (≤ 32 m) of <paramref name="p"/>, nearest first (ties by ID).</summary>
    public List<WalkPoint> Walks(Vector2 p, float reach)
    {
        var found = new List<WalkPoint>();
        foreach (var line in _index.Walks.AllAround(p))
        {
            var points = line.Line.Points;
            WalkPoint? best = null;
            var run = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                var a = points[i - 1]; var ab = points[i] - a;
                var len = ab.Length();
                if (len > 1e-4f)
                {
                    var t = Math.Clamp(Vector2.Dot(p - a, ab) / (len * len), 0f, 1f);
                    var q = a + ab * t;
                    var d = Vector2.Distance(p, q);
                    if (best is null || d < best.Value.Distance) best = new(line, q, run + t * len, d);
                }
                run += len;
            }
            if (best is { } b && b.Distance <= reach) found.Add(b);
        }
        return found.OrderBy(w => w.Distance).ThenBy(w => w.Line.Id.Value, StringComparer.Ordinal).ToList();
    }

    /// <summary>Distance from <paramref name="p"/> to the nearest strollable walk other than <paramref name="except"/> (a junction nearby).</summary>
    public float OtherWalk(Vector2 p, NetworkLine except)
    {
        var best = float.MaxValue;
        foreach (var line in _index.Walks.AllAround(p))
            if (!ReferenceEquals(line, except)) best = MathF.Min(best, MathF.Sqrt(PlanLines.PolylineDistanceSquared(line.Line.Points, p)));
        return best;
    }

    /// <summary>
    /// The nearest pedestrian paving to <paramref name="p"/> (walks, steps, paved road areas): the gap to it (0 on it) and the
    /// nearest point of its edge (<paramref name="p"/> itself on it). Null when none lies within about 32 m.
    /// </summary>
    public (float Gap, Vector2 Edge)? PavingEdge(Vector2 p) => PavingEdgeFacing(p) is { } hit ? (hit.Gap, hit.Edge) : null;

    /// <summary>
    /// <see cref="PavingEdge"/> with the paving edge's direction: <c>Inward</c> is the unit normal of the edge at <c>Edge</c> pointing
    /// into the paving, from the side <paramref name="p"/> is on (the zero vector when <paramref name="p"/> is on paving). A polygon's
    /// edge gives its segment's normal, a walk's the line's normal at the nearest point; <c>Road</c> is the network walk whose strip it is
    /// (null for a polygon or a paved road area).
    /// </summary>
    public (float Gap, Vector2 Edge, Vector2 Inward, NetworkLine? Road)? PavingEdgeFacing(Vector2 p)
    {
        (float, Vector2, Vector2, NetworkLine?)? best = null;
        foreach (var paving in _index.Surfaces.AllAround(p))
        {
            float gap; Vector2 edge, inward;
            if (paving.Line is { } line)
            {
                var q = PlanLines.Project(line, p).Point;
                var d = Vector2.Distance(p, q);
                if (d <= paving.Half) return (0f, p, Vector2.Zero, paving.Road);
                gap = d - paving.Half;
                edge = q + (p - q) / d * paving.Half;
                inward = (q - p) / d;
            }
            else
            {
                var rings = paving.Rings!;
                if (PlanRings.Contains(rings, p)) return (0f, p, Vector2.Zero, null);
                (Vector2 Point, Vector2 Tangent) hit = default;
                var bestD = float.MaxValue;
                foreach (var ring in rings.Holes.Prepend(rings.Outer))
                {
                    var near = PlanLines.Project(ring, p, closed: true);
                    var d = Vector2.DistanceSquared(p, near.Point);
                    if (d >= bestD) continue;
                    bestD = d;
                    hit = (near.Point, near.Tangent);
                }
                edge = hit.Point;
                gap = MathF.Sqrt(bestD);
                // The segment's normal on the far side from p: the way into the paving from where p stands.
                var normal = new Vector2(-hit.Tangent.Y, hit.Tangent.X);
                inward = Vector2.Dot(normal, edge - p) >= 0f ? normal : -normal;
            }
            if (best is null || gap < best.Value.Item1) best = (gap, edge, inward, paving.Road);
        }
        return best;
    }

    /// <summary>
    /// True when <paramref name="p"/> lies on plaza paving: a pedestrian polygon (a square, a paved road area), not a walk's strip, which
    /// belongs to walkers (and whose round end the strip test allows is lawn where game/gen/ground/ cuts the walk square).
    /// </summary>
    public bool OnPlaza(Vector2 p)
    {
        foreach (var paving in _index.Surfaces.AllAround(p))
            if (paving.Rings is { } rings && PlanRings.Contains(rings, p)) return true;
        return false;
    }

    /// <summary>
    /// True when <paramref name="p"/> lies on pedestrian paving that game/gen/ground/ paves for certain: a plaza polygon, or a walk's strip short of
    /// the line's ends (a strip's round end is lawn where the walk is cut square; a walk going on past a junction paves it itself).
    /// </summary>
    public bool OnPavedGround(Vector2 p)
    {
        foreach (var paving in _index.Surfaces.AllAround(p))
        {
            if (paving.Rings is { } rings) { if (PlanRings.Contains(rings, p)) return true; continue; }
            var line = paving.Line!;
            var q = PlanLines.Project(line, p).Point;
            if (Vector2.DistanceSquared(q, line[0]) < 1e-6f || Vector2.DistanceSquared(q, line[^1]) < 1e-6f) continue;
            if (Vector2.Distance(p, q) <= paving.Half) return true;
        }
        return false;
    }

    /// <summary>
    /// Spots on the edges of pedestrian paving within <paramref name="reach"/> of <paramref name="p"/>: the edge point and the unit normal
    /// into the paving, found by sampling the ground every <paramref name="step"/> and keeping the points just off the paving (no
    /// further than a step), nearest first (ties by position), one per 0.5 m of edge.
    /// </summary>
    public List<(Vector2 Edge, Vector2 Inward, NetworkLine? Road)> PavingEdgesNear(Vector2 p, float reach, float step)
    {
        var found = new SortedDictionary<(int, int), (float Distance, Vector2 Edge, Vector2 Inward, NetworkLine? Road)>();
        var n = (int)MathF.Floor(reach / step);
        for (var i = -n; i <= n; i++)
            for (var k = -n; k <= n; k++)
            {
                var q = p + new Vector2(i * step, k * step);
                if (PavingEdgeFacing(q) is not { Gap: > 0f } hit || hit.Gap > step) continue;
                var distance = Vector2.Distance(hit.Edge, p);
                if (distance > reach) continue;
                var key = ((int)MathF.Round(hit.Edge.X * 2f), (int)MathF.Round(hit.Edge.Y * 2f));
                if (!found.TryGetValue(key, out var old) || distance < old.Distance) found[key] = (distance, hit.Edge, hit.Inward, hit.Road);
            }
        return found.Values.OrderBy(f => f.Distance).ThenBy(f => f.Edge.X).ThenBy(f => f.Edge.Y).Select(f => (f.Edge, f.Inward, f.Road)).ToList();
    }
}
