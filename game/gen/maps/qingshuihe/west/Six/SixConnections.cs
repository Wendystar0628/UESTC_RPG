using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixLayout;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixOpenings;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的玻璃连廊、空中连桥及护栏；桥线供开口表与室内共用。</summary>
internal static class SixConnections
{
    internal static void LinkAB(SiteBuilder b, IReadOnlyList<Vector2> ring, float y)
    {
        var top = y + S.BaseHeight;
        var plan = new PolygonRings(ring);
        b.Add(PartSolids.Extrude(plan, top - S.LinkFascia - y + 0.1f, b.Lod is LodLevel.L3 or LodLevel.L4 ? L.DarkGrey : L.Glass, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartSolids.Extrude(plan, S.LinkFascia, L.Stone, top - S.LinkFascia), Matrix4x4.Identity);
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(PartSweeps.Sweep(PartSweeps.BeveledRectangleProfile(0.5f, 0.16f, 0.03f), PlanMath.OutsideRight(ring), L.Trim, closed: true, baseY: top), Matrix4x4.Identity);
        if (b.Detail != SiteDetail.Full) return;
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            var count = Math.Max(1, (int)MathF.Ceiling(e.Length / S.MullionPitch));
            for (var i = 1; i < count; i++)
                GridFacade.Box(b, e.Frame, e.Length * i / count - 0.035f, e.Length * i / count + 0.035f, y, top - S.LinkFascia, 0, 0.1f, L.Metal, 0);
            GridFacade.Box(b, e.Frame, 0, e.Length, y + S.BaseStorey - 0.2f, y + S.BaseStorey + 0.15f, 0, 0.1f, L.DarkGrey, 0);
        }
    }

    internal static void LinkBC(SiteBuilder b, IWorldModel world, float y)
    {
        var ring = Plan(world, S.EntityC, S.LinkBC).Outer;
        var plan = new PolygonRings(ring);
        float deck = y + S.LinkDeck, roof = y + S.BaseHeight;
        var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        b.Add(PartSolids.Extrude(plan, 0.7f, L.Trim, deck - 0.7f), Matrix4x4.Identity);
        b.Add(PartSolids.Extrude(plan, 0.55f, L.Trim, roof - 0.55f), Matrix4x4.Identity);
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            var f = e.Frame;
            var h = S.LinkPier * 0.5f;
            var openings = LinkOpenings(world, e);
            foreach (var x in new[] { h, e.Length * 0.5f })
            {
                if (openings.Any(o => x + h > o.From && x - h < o.To)) continue;
                GridFacade.Box(b, f, x - h, x + h, y - 0.1f, roof - 0.55f, -S.LinkPier, 0, L.Stone, bevel);
            }
            foreach (var (x0, x1) in GridFacade.Open(0, e.Length, openings))
                GridFacade.Box(b, f, x0, x1, deck - 0.75f, deck + 0.05f, -0.05f, 0.12f, L.Stone, bevel);
            GridFacade.Box(b, f, -0.15f, e.Length + 0.15f, roof - 0.6f, roof + 0.1f, -0.05f, 0.3f, L.Stone, bevel);
            if (b.Detail != SiteDetail.Mass)
                foreach (var (x0, x1) in GridFacade.Open(S.LinkPier + 0.2f, e.Length - S.LinkPier - 0.2f, openings))
                    Rail(b, f, x0, x1, deck);
        }
        foreach (var (from, to) in Bridges(world))
        {
            var f = PlanFrame.Toward(from, to);
            var length = Vector2.Distance(from, to);
            GridFacade.Box(b, f, 0, length, deck - 0.7f, deck, -BridgeHalfWidth, BridgeHalfWidth, L.Trim, bevel);
            GridFacade.Box(b, f, 0, length, roof - 0.5f, roof - 0.1f, -2.0f, 2.0f, L.Trim, bevel);
            if (b.Detail == SiteDetail.Mass) continue;
            foreach (var s in new[] { -1f, 1f })
            {
                var side = PlanFrame.Toward(f.P(0, s * 1.6f), f.P(length, s * 1.6f));
                Rail(b, side, 0, length, deck);
                GridFacade.Box(b, side, length * 0.5f - 0.2f, length * 0.5f + 0.2f, deck, roof - 0.5f, -0.2f, 0.2f, L.Metal, 0);
            }
        }
    }

    internal static IEnumerable<(Vector2 From, Vector2 To)> Bridges(IWorldModel world)
    {
        var centre = PlanPolygons.Centroid(Plan(world, S.EntityC, S.LinkBC).Outer);
        var direction = new Vector2(-S.Axis.Y, S.Axis.X);
        foreach (var (start, end) in WestIds.SixBridges)
        {
            var a = world.ContourSegment(start);
            var b = world.ContourSegment(end);
            var from = PlanLines.LineIntersection(centre, direction, a[0], a[^1] - a[0], 1e-6f)
                ?? throw new InvalidOperationException($"Bridge source {start} is parallel to its axis.");
            var to = PlanLines.LineIntersection(centre, direction, b[0], b[^1] - b[0], 1e-6f)
                ?? throw new InvalidOperationException($"Bridge target {end} is parallel to its axis.");
            var target = PlanFrame.Toward(b[^1], b[0]);
            var directionToBlock = Vector2.Normalize(to - from);
            // The stone line projects beyond the base glass; keep one continuous deck through that recess.
            to += directionToBlock * (-S.Recess / Vector2.Dot(directionToBlock, target.W));
            yield return (from, to);
        }
    }

    private static void Rail(SiteBuilder b, PlanFrame f, float x0, float x1, float deck)
    {
        if (x1 - x0 < 0.5f) return;
        GridFacade.Box(b, f, x0, x1, deck + 0.08f, deck + S.RailHeight - 0.08f, -0.02f, 0.02f, L.Glass, 0);
        GridFacade.Box(b, f, x0, x1, deck + S.RailHeight - 0.08f, deck + S.RailHeight, -0.04f, 0.04f, L.Metal, 0);
        if (b.Detail != SiteDetail.Full) return;
        var n = Math.Max(1, (int)MathF.Ceiling((x1 - x0) / 1.5f));
        for (var i = 0; i <= n; i++)
        {
            var x = x0 + (x1 - x0) * i / n;
            GridFacade.Box(b, f, x - 0.03f, x + 0.03f, deck, deck + S.RailHeight, -0.03f, 0.03f, L.Metal, 0);
        }
    }
}
