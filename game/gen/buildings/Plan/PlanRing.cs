using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Buildings.Plan;

/// <summary>
/// One cleaned footprint ring with per-edge frames. Plan coordinates are (world X, world Z).
/// Every ring keeps the building material on the left of travel, so the outward (air side) normal of
/// edge i is (d.Y, -d.X): outer rings run counter-clockwise (positive signed area), courtyard holes clockwise.
/// Corner i sits at Points[i], between edge i-1 (incoming) and edge i (outgoing).
/// </summary>
public sealed class PlanRing
{
    private PlanRing(Vector2[] points, bool hole)
    {
        Points = points;
        IsHole = hole;
        var n = points.Length;
        Dir = new Vector2[n];
        Normal = new Vector2[n];
        Length = new float[n];
        Convex = new bool[n];
        TurnCos = new float[n];
        for (var i = 0; i < n; i++)
        {
            var e = points[(i + 1) % n] - points[i];
            var len = e.Length();
            Length[i] = len;
            Dir[i] = VectorMath.Unit(e);
            Normal[i] = new Vector2(Dir[i].Y, -Dir[i].X);
        }
        for (var i = 0; i < n; i++)
        {
            var prev = Dir[(i + n - 1) % n];
            var next = Dir[i];
            Convex[i] = PlanRings.CrossFloat(prev, next) > 0f;
            TurnCos[i] = Vector2.Dot(prev, next);
        }
    }

    public Vector2[] Points { get; }
    public bool IsHole { get; }
    public Vector2[] Dir { get; }
    public Vector2[] Normal { get; }
    public float[] Length { get; }
    /// <summary>True when corner i is an outside corner (left turn with the material on the left).</summary>
    public bool[] Convex { get; }
    /// <summary>Cosine between the incoming and outgoing edge directions at corner i (1 = straight).</summary>
    public float[] TurnCos { get; }
    public int Count => Points.Length;

    public float Perimeter
    {
        get
        {
            var sum = 0f;
            foreach (var l in Length) sum += l;
            return sum;
        }
    }

    /// <summary>Wraps already clean, correctly oriented points (for offset skins).</summary>
    public static PlanRing FromPoints(Vector2[] points, bool hole) => new(points, hole);

    /// <summary>Point inside the outer ring and outside every hole.</summary>
    public static bool InsideFootprint(PlanRing outer, IReadOnlyList<PlanRing> holes, Vector2 p)
    {
        if (!PlanRings.InsideByCrossings(outer.Points, p)) return false;
        foreach (var h in holes)
            if (PlanRings.InsideByCrossings(h.Points, p)) return false;
        return true;
    }

    /// <summary>Distance from p to the nearest edge of any ring.</summary>
    public static float DistanceToEdges(PlanRing outer, IReadOnlyList<PlanRing> holes, Vector2 p)
    {
        var best = PlanPolygons.DistanceToEdges(p, outer.Points, outer.Dir, outer.Length);
        foreach (var h in holes) best = MathF.Min(best, PlanPolygons.DistanceToEdges(p, h.Points, h.Dir, h.Length));
        return best;
    }

    /// <summary>
    /// Mitred offset of corner i along the outward normals by s (negative = into the material).
    /// Very sharp corners clamp the mitre so that the point never runs further than four times |s|.
    /// </summary>
    public Vector2 Mitre(int i, float s)
    {
        var n0 = Normal[(i + Count - 1) % Count];
        var n1 = Normal[i];
        var den = 1f + Vector2.Dot(n0, n1);
        var k = den > 0.125f ? 1f / den : 8f;
        return Points[i] + (n0 + n1) * (s * k);
    }

    /// <summary>
    /// Offsets the whole ring by s. Returns null when an edge would flip or the result self-intersects,
    /// so callers fall back to a simpler, still closed form.
    /// </summary>
    public Vector2[]? Offset(float s)
    {
        var result = new Vector2[Count];
        for (var i = 0; i < Count; i++) result[i] = Mitre(i, s);
        for (var i = 0; i < Count; i++)
        {
            var e = result[(i + 1) % Count] - result[i];
            if (Vector2.Dot(e, Dir[i]) <= 0.02f) return null;
        }
        return PlanRings.SelfIntersects(result) ? null : result;
    }

}
