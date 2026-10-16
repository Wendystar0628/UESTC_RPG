using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.DesignBoundary.Framework;
using F = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Fence;
using L = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Look;

namespace Campus.Gen.Zones.DesignBoundary.Fence;

/// <summary>
/// One run of the boundary fence along a plan polyline (MAP-QSH-ADDITIONS §2.11): light grey stone pillars at every corner
/// and at most every <see cref="F.PillarPitch"/>, deep grey iron panels between them (bottom rail, top rail, 25 mm bars at
/// 0.14 m standing on a granite base course), and an optional clipped hedge on its own line inside. The free end of a run
/// gets a broader end pillar with a two-step cap (收头); an end that abuts a gate pier or a guard house has no pillar.
/// </summary>
public sealed record FenceRun(IReadOnlyList<Vector2> Line, bool StartPillar, bool EndPillar, IReadOnlyList<Vector2>? Hedge)
{
    /// <summary>Pillar centres in order: every vertex plus even subdivisions of each straight piece.</summary>
    public IReadOnlyList<(Vector2 At, Vector2 Along, bool Pillar, bool End)> Posts()
    {
        var posts = new List<(Vector2, Vector2, bool, bool)>();
        for (var i = 0; i + 1 < Line.Count; i++)
        {
            Vector2 p = Line[i], q = Line[i + 1];
            var length = Vector2.Distance(p, q);
            if (length < 0.05f) continue;
            var u = (q - p) / length;
            var panels = Math.Max(1, (int)MathF.Ceiling(length / F.PillarPitch - 1e-3f));
            for (var k = 0; k < panels; k++)
            {
                var first = i == 0 && k == 0;
                var at = p + u * (length * k / panels);
                var along = k == 0 && i > 0 ? Chord(Line[i - 1], q) : u;
                posts.Add((at, along, !first || StartPillar, false));
            }
        }
        var last = Line[^1];
        posts.Add((last, Unit(last - Line[^2]), EndPillar, EndPillar));
        return posts;
    }

    private static Vector2 Chord(Vector2 a, Vector2 b) => Unit(b - a);

    internal static Vector2 Unit(Vector2 v)
    {
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
        return length > 1e-6f ? v / length : Vector2.UnitX;
    }
}

public static class BoundaryFence
{
    public static void Build(SiteBuilder b, FenceRun run)
    {
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(run);
        var posts = run.Posts();
        var far = b.Lod is LodLevel.L3 or LodLevel.L4;
        for (var j = 0; j + 1 < posts.Count; j++)
        {
            var (p, _, pp, pe) = posts[j];
            var (q, _, qp, qe) = posts[j + 1];
            var length = Vector2.Distance(p, q);
            if (length < 0.05f) continue;
            var u = (q - p) / length;
            var y = MathF.Min(b.Ground(p), b.Ground(q));
            // Panels run between the pillar faces; an end without a pillar runs to the point itself.
            var a = p + u * (pp ? Half(pe) : 0f);
            var c = q - u * (qp ? Half(qe) : 0f);
            if (Vector2.Distance(a, c) < 0.05f) continue;
            b.Run(L.Granite, a, c, F.BaseWidth, y - 0.05f, y + F.BaseHeight);
            if (far) continue;
            b.Run(L.Steel, a, c, F.RailSize, y + F.BottomRail, y + F.BottomRail + F.RailSize);
            b.Run(L.Steel, a, c, F.RailSize, y + F.TopRail, y + F.TopRail + F.RailSize);
            if (b.Detail == SiteDetail.Mass) continue;
            var pitch = b.Detail == SiteDetail.Full ? F.BarPitch : F.BarPitch * 4;
            var span = Vector2.Distance(a, c);
            var count = (int)MathF.Floor(span / pitch);
            if (count < 1) continue;
            var start = (span - (count - 1) * pitch) * 0.5f;
            for (var k = 0; k < count; k++)
                b.Box(L.SteelBar, a + u * (start + k * pitch), u, F.BarSize, F.BarSize, y + F.BaseHeight, y + F.BarTop, caps: false, noShadow: true);
        }
        foreach (var (at, along, pillar, end) in posts)
            if (pillar) Pillar(b, at, along, end);
        if (run.Hedge is { } hedge) Hedge(b, hedge);
    }

    private static float Half(bool end) => (end ? F.EndPillarSize : F.PillarSize) * 0.5f;

    /// <summary>A stone pillar on a granite foot, with a cap; the end pillar is broader and taller with a second cap step.</summary>
    private static void Pillar(SiteBuilder b, Vector2 at, Vector2 along, bool end)
    {
        var y = b.Ground(at);
        var f = PlanFrame.Along(at, along);
        var size = end ? F.EndPillarSize : F.PillarSize;
        var height = end ? F.EndPillarHeight : F.PillarHeight;
        if (b.Detail == SiteDetail.Mass)
        {
            b.Box(L.Stone, at, along, size, size, y - 0.05f, y + height + F.CapHeight);
            return;
        }
        b.Add(PartGeometryKernel.BeveledBox(size + 0.06f, F.BaseHeight + 0.05f, size + 0.06f, 0.015f, L.Granite), f.At(0, y - 0.05f, 0));
        b.Add(PartGeometryKernel.BeveledBox(size, height - F.BaseHeight, size, 0.02f, L.Stone), f.At(0, y + F.BaseHeight, 0));
        var cap = size + (F.CapSize - F.PillarSize);
        b.Add(PartGeometryKernel.BeveledBox(cap, F.CapHeight, cap, 0.02f, L.StoneTrim), f.At(0, y + height, 0));
        if (b.Detail != SiteDetail.Full) return;
        b.Add(PartGeometryKernel.BeveledBox(cap - 0.16f, F.CapTop, cap - 0.16f, 0.012f, L.StoneTrim), f.At(0, y + height + F.CapHeight, 0));
        if (end) b.Add(PartGeometryKernel.BeveledBox(size + 0.02f, 0.05f, size + 0.02f, 0.01f, L.StoneTrim), f.At(0, y + height * 0.62f, 0));
    }

    private static void Hedge(SiteBuilder b, IReadOnlyList<Vector2> line)
    {
        var step = b.Lod is LodLevel.L3 or LodLevel.L4 ? 3 : 1;
        for (var i = 0; i + 1 < line.Count; i += step)
        {
            Vector2 p = line[i], q = line[Math.Min(i + step, line.Count - 1)];
            var length = Vector2.Distance(p, q);
            if (length < 0.05f) continue;
            var u = (q - p) / length;
            var y = MathF.Min(b.Ground(p), b.Ground(q));
            // Blocks overlap their neighbours a little at corners; the ends stop flush with the line's ends.
            var lead = i == 0 ? 0f : 0.15f;
            var tail = i + step >= line.Count - 1 ? 0f : 0.15f;
            var mid = (p + q) * 0.5f + u * ((tail - lead) * 0.5f);
            var span = length + lead + tail;
            if (b.Detail != SiteDetail.Full) { b.Box(L.Hedge, mid, u, span, F.HedgeWidth, y - 0.05f, y + F.HedgeHeight); continue; }
            b.Add(PartGeometryKernel.BeveledBox(span, F.HedgeHeight + 0.05f, F.HedgeWidth, F.HedgeBevel, L.Hedge), PlanFrame.Along(mid, u).At(0, y - 0.05f, 0));
        }
    }

    /// <summary>Boundary collision along the run (top 2.2 m, ≤ 0.27 m beyond the fence line) and along the hedge.</summary>
    public static void Collide(SiteColliders c, FenceRun run)
    {
        ArgumentNullException.ThrowIfNull(c);
        ArgumentNullException.ThrowIfNull(run);
        for (var i = 0; i + 1 < run.Line.Count; i++)
        {
            Vector2 p = run.Line[i], q = run.Line[i + 1];
            if (Vector2.Distance(p, q) < 0.05f) continue;
            var lo = MathF.Min(c.Ground(p), c.Ground(q));
            var hi = MathF.Max(c.Ground(p), c.Ground(q));
            c.Wall(p, q, F.ColliderHalf, lo - 0.5f, hi + F.ColliderTop, SurfaceTag.Boundary, FootMat.Metal);
        }
        if (run.Hedge is not { } hedge) return;
        for (var i = 0; i + 1 < hedge.Count; i++)
        {
            Vector2 p = hedge[i], q = hedge[i + 1];
            if (Vector2.Distance(p, q) < 0.05f) continue;
            var lo = MathF.Min(c.Ground(p), c.Ground(q));
            var hi = MathF.Max(c.Ground(p), c.Ground(q));
            c.Wall(p, q, F.HedgeWidth * 0.5f, lo - 0.5f, hi + BoundaryTune.Gate.LowTop, SurfaceTag.Boundary, FootMat.Hedge);
        }
    }

    /// <summary>
    /// A copy of a polyline offset <paramref name="distance"/> to the side where <paramref name="inside"/> holds on its last
    /// segment, mitred at the vertices, its start pulled back along the line by <paramref name="trimStart"/>.
    /// </summary>
    public static IReadOnlyList<Vector2> Offset(IReadOnlyList<Vector2> line, float distance, Func<Vector2, bool> inside, float trimStart)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(inside);
        // The side is read on the last segment, which follows the ring; a closure's first leg crosses campus on both sides.
        var un = FenceRun.Unit(line[^1] - line[^2]);
        var probe = (line[^1] + line[^2]) * 0.5f + new Vector2(-un.Y, un.X) * 0.5f;
        var sign = inside(probe) ? 1f : -1f;
        var result = new Vector2[line.Count];
        for (var k = 0; k < line.Count; k++)
        {
            var before = FenceRun.Unit(line[Math.Max(k, 1)] - line[Math.Max(k, 1) - 1]);
            var after = FenceRun.Unit(line[Math.Min(k + 1, line.Count - 1)] - line[Math.Min(k + 1, line.Count - 1) - 1]);
            var nb = new Vector2(-before.Y, before.X);
            var na = new Vector2(-after.Y, after.X);
            var n = FenceRun.Unit(nb + na);
            var cos = MathF.Max(Vector2.Dot(n, na), 0.5f);
            result[k] = line[k] + n * (sign * distance / cos);
        }
        if (trimStart > 0 && result.Length > 1) result[0] += FenceRun.Unit(result[1] - result[0]) * trimStart;
        return result;
    }
}
