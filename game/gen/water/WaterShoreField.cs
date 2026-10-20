using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using static Campus.Gen.Water.WaterEdges;

namespace Campus.Gen.Water;

/// <summary>Derives distance to closed shores and the existing bank-aligned channel flow at each vertex.</summary>
internal static class WaterShoreField
{
    internal static (float[] Shore, float[] Flow) Build(WaterBody body, IReadOnlyList<Vector3> vertices,
        IReadOnlyList<WaterBody> all, WaterParams p)
    {
        var shores = ShoreSegments(body, all, p);
        var count = vertices.Count;
        var shore = new float[count];
        var flow = new float[count * 2];
        for (var i = 0; i < count; i++)
        {
            var v = new Vector2(vertices[i].X, vertices[i].Z);
            var best = float.MaxValue;
            float sxx = 0f, sxz = 0f, szz = 0f;
            foreach (var s in shores)
            {
                var d = PlanLines.SegmentDistanceSquared(v, s.A, s.B);
                if (d < best) best = d;
                if (body.Kind != WaterKind.Channel || d > FlowReach * FlowReach) continue;
                // Orientation tensor of the nearby banks: the long banks outweigh an end cap or a joint.
                var e = s.B - s.A;
                var len = e.Length();
                if (len <= 1e-6f) continue;
                var u = e / len;
                var w = len / (1f + d / (FlowFalloff * FlowFalloff));
                sxx += w * u.X * u.X;
                sxz += w * u.X * u.Y;
                szz += w * u.Y * u.Y;
            }
            shore[i] = shores.Count == 0 ? p.SdfClamp : MathF.Min(MathF.Sqrt(best), p.SdfClamp);
            var tangent = PrincipalAxis(sxx, sxz, szz);
            if (body.Kind == WaterKind.Channel && tangent.LengthSquared() > 1e-12f)
            {
                var t = Vector2.Normalize(tangent);
                // WLD-WATER: every channel flows north to south (+z), east-west pieces flow east (+x).
                if (MathF.Abs(t.Y) >= 0.25f ? t.Y < 0f : t.X < 0f) t = -t;
                flow[2 * i] = t.X * p.FlowSpeed;
                flow[2 * i + 1] = t.Y * p.FlowSpeed;
            }
        }
        return (shore, flow);
    }

    private readonly record struct Shore(Vector2 A, Vector2 B);

    private const float FlowReach = 15f;
    private const float FlowFalloff = 6f;

    /// <summary>Major eigenvector of the symmetric tensor [[a, b], [b, c]], unnormalised; zero when isotropic.</summary>
    private static Vector2 PrincipalAxis(float a, float b, float c)
    {
        var half = (a - c) * 0.5f;
        var lambda = (a + c) * 0.5f + MathF.Sqrt(half * half + b * b);
        var u = new Vector2(b, lambda - a);
        var w = new Vector2(lambda - c, b);
        return u.LengthSquared() >= w.LengthSquared() ? u : w;
    }

    private static List<Shore> ShoreSegments(WaterBody body, IReadOnlyList<WaterBody> all, WaterParams p)
    {
        var shores = new List<Shore>();
        foreach (var other in all)
        {
            if (other != body && !Near(body, other, p.NeighbourReach)) continue;
            foreach (var e in Edges(other))
            {
                var length = Vector2.Distance(e.A, e.B);
                var pieces = Math.Max(1, (int)MathF.Ceiling(length / 2f));
                for (var k = 0; k < pieces; k++)
                {
                    var a = Vector2.Lerp(e.A, e.B, (float)k / pieces);
                    var b = Vector2.Lerp(e.A, e.B, (float)(k + 1) / pieces);
                    if (IsShore(other, all, a, b, p.OpenEdgeProbe)) shores.Add(new(a, b));
                }
            }
        }
        return shores;
    }

    private static bool Near(WaterBody a, WaterBody b, float reach) =>
        a.Min.X - reach <= b.Max.X && a.Max.X + reach >= b.Min.X && a.Min.Y - reach <= b.Max.Y && a.Max.Y + reach >= b.Min.Y;

    private static bool IsShore(WaterBody owner, IReadOnlyList<WaterBody> all, Vector2 a, Vector2 b, float probe)
    {
        var mid = (a + b) * 0.5f;
        var t = b - a;
        var n = Vector2.Normalize(new Vector2(-t.Y, t.X));
        if (owner.Contains(mid + n * 0.05f)) n = -n;
        var outside = mid + n * probe;
        foreach (var other in all)
            if (other != owner && other.Contains(outside)) return false;
        return true;
    }


}
