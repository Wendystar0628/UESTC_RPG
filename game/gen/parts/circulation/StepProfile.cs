using System.Numerics;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>The stepped block section shared by straight steps and steps swept along a path.</summary>
internal static class StepProfile
{
    /// <summary>
    /// Section in (s, y): n risers over rise × run, the first riser at s = 0, tread k at k·R spanning [(k−1)·T, k·T], the
    /// top tread flush with the upper level, closed 0.1 m below grade; L0 chamfers every nosing. The bottom is split under
    /// every riser so each step column is convex and fans from its top-right corner: the inner tread corners are
    /// collinear, which is exactly what makes a general ear clipper emit zero-area slivers.
    /// </summary>
    internal static (List<Vector2> Ring, List<bool> Chamfer, List<int> Triangles) Block(int risers, float rise, float run, Detail detail, float nosingBevel)
    {
        var ring = new List<Vector2>(); var flags = new List<bool>(); var triangles = new List<int>();
        int Add(float s, float y, bool bevelNext = false) { ring.Add(new(s, y)); flags.Add(bevelNext); return ring.Count - 1; }
        if (detail == Detail.Mass && risers > 1)
        {
            int a = Add(0, -S.Sink), b = Add(0, rise / risers), c = Add(run - run / risers, rise), d = Add(run, rise), e = Add(run, -S.Sink);
            triangles.AddRange([d, e, a, d, a, b, d, b, c]);
            return (ring, flags, triangles);
        }
        var n = detail == Detail.Coarse && risers >= 4 ? (risers + 1) / 2 : risers;
        float r = rise / n, t = run / n, bevel = detail == Detail.Full ? nosingBevel : 0;
        var ca = new int[n + 1]; var cb = new int[n + 1]; var top = new int[n + 1]; var ground = new int[n + 1];
        ground[0] = Add(0, -S.Sink);
        for (var k = 1; k <= n; k++)
        {
            var s = (k - 1) * t;
            if (bevel > 0) { ca[k] = Add(s, k * r - bevel, true); cb[k] = Add(s + bevel, k * r); }
            else ca[k] = cb[k] = Add(s, k * r);
            top[k] = k < n ? Add(k * t, k * r) : Add(run, rise);
        }
        ground[n] = Add(run, -S.Sink);
        for (var k = n - 1; k >= 1; k--) ground[k] = Add(k * t, -S.Sink);
        for (var k = 1; k <= n; k++)
        {
            var rim = new List<int> { cb[k] };
            if (ca[k] != cb[k]) rim.Add(ca[k]);
            if (k >= 2) rim.Add(top[k - 1]);
            rim.Add(ground[k - 1]); rim.Add(ground[k]);
            for (var i = 0; i + 1 < rim.Count; i++) { triangles.Add(top[k]); triangles.Add(rim[i]); triangles.Add(rim[i + 1]); }
        }
        return (ring, flags, triangles);
    }
}
