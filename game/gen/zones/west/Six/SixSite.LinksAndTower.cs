using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.West.WestTune.Look;
using S = Campus.Gen.Zones.West.WestTune.Six;
using W = Campus.Gen.Zones.West.WestTune.Windows;

namespace Campus.Gen.Zones.West.Six;

/// <summary>六号科研楼的连廊与塔楼：A-B 玻璃连廊、B-C 空中连桥、北侧石质楼梯塔。</summary>
public sealed partial class SixSite
{
    // ---- Links and tower ----

    /// <summary>The A–B link: two storeys of dark glass with slim mullions, a stone fascia and coping.</summary>
    private static void LinkAB(SiteBuilder b, IReadOnlyList<Vector2> ring, float y)
    {
        var top = y + S.BaseHeight;
        var plan = new PolygonRings(ring);
        b.Add(PartGeometryKernel.Extrude(plan, top - S.LinkFascia - y + 0.1f, b.Lod is LodLevel.L3 or LodLevel.L4 ? L.DarkGrey : L.Glass, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Extrude(plan, S.LinkFascia, L.Stone, top - S.LinkFascia), Matrix4x4.Identity);
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(0.5f, 0.16f, 0.03f), PlanMath.OutsideRight(ring), L.Trim, closed: true, baseY: top), Matrix4x4.Identity);
        if (b.Detail != SiteDetail.Full) return;
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            var count = Math.Max(1, (int)MathF.Ceiling(e.Length / S.MullionPitch));
            for (var i = 1; i < count; i++)
                GridFacade.Box(b, e.Frame, e.Length * i / count - 0.035f, e.Length * i / count + 0.035f, y, top - S.LinkFascia, 0, 0.1f, L.Metal, 0);
            GridFacade.Box(b, e.Frame, 0, e.Length, y + S.BaseStorey - 0.2f, y + S.BaseStorey + 0.15f, 0, 0.1f, L.DarkGrey, 0);
        }
    }

    /// <summary>
    /// The B–C link (§1.6): an open covered bridge on stone piers over the drive-through: a deck at the first-floor line with
    /// glass railings, a roof at the base top, and narrow covered decks across to B's and C's faces at the same level.
    /// </summary>
    private static void LinkBC(SiteBuilder b, IWorldModel world, float y)
    {
        var ring = Plan(world, S.EntityC, S.LinkBC).Outer;
        var plan = new PolygonRings(ring);
        float deck = y + S.LinkDeck, roof = y + S.BaseHeight;
        var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        b.Add(PartGeometryKernel.Extrude(plan, 0.7f, L.Trim, deck - 0.7f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Extrude(plan, 0.55f, L.Trim, roof - 0.55f), Matrix4x4.Identity);
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            var f = e.Frame;
            var h = S.LinkPier * 0.5f;
            foreach (var x in new[] { h, e.Length * 0.5f })
                GridFacade.Box(b, f, x - h, x + h, y - 0.1f, roof - 0.55f, -S.LinkPier, 0, L.Stone, bevel);
            GridFacade.Box(b, f, 0, e.Length, deck - 0.75f, deck + 0.05f, -0.05f, 0.12f, L.Stone, bevel);
            GridFacade.Box(b, f, -0.15f, e.Length + 0.15f, roof - 0.6f, roof + 0.1f, -0.05f, 0.3f, L.Stone, bevel);
            if (b.Detail != SiteDetail.Mass) Rail(b, f, S.LinkPier + 0.2f, e.Length - S.LinkPier - 0.2f, deck);
        }
        foreach (var (from, to) in Bridges(world))
        {
            var f = PlanFrame.Toward(from, to);
            var length = Vector2.Distance(from, to);
            GridFacade.Box(b, f, 0, length, deck - 0.7f, deck, -1.7f, 1.7f, L.Trim, bevel);
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

    /// <summary>The two narrow decks: from the link's centre line out to the facing faces of B and C.</summary>
    internal static IEnumerable<(Vector2 From, Vector2 To)> Bridges(IWorldModel world)
    {
        var centre = PlanGeometry.Centroid(Plan(world, S.EntityC, S.LinkBC).Outer);
        var direction = new Vector2(-S.Axis.Y, S.Axis.X);
        foreach (var (start, end) in WestIds.SixBridges)
        {
            var a = world.ContourSegment(start);
            var b = world.ContourSegment(end);
            var from = PlanLines.LineIntersection(centre, direction, a[0], a[^1] - a[0], 1e-6f)
                ?? throw new InvalidOperationException($"Bridge source {start} is parallel to its axis.");
            var to = PlanLines.LineIntersection(centre, direction, b[0], b[^1] - b[0], 1e-6f)
                ?? throw new InvalidOperationException($"Bridge target {end} is parallel to its axis.");
            yield return (from, to);
        }
    }

    /// <summary>Glass railing from local x0 to x1 on a frame's line: glass panes, posts, an aluminium handrail.</summary>
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

    /// <summary>The north tower: a stone stair core with a dark plinth, a tall glazed slot on each long face and a coping.</summary>
    private static void Tower(SiteBuilder b, IReadOnlyList<Vector2> ring, float y)
    {
        var plan = new PolygonRings(ring);
        b.Add(PartGeometryKernel.Extrude(plan, S.TowerHeight - 0.4f + 0.1f, L.Stone, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(0.7f, 0.4f, 0.04f), PlanMath.OutsideRight(ring), L.Trim, closed: true, baseY: y + S.TowerHeight - 0.4f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(0.3f, 0.7f, 0.03f), PlanMath.OutsideRight(ring), L.DarkGrey, closed: true, baseY: y - 0.1f), Matrix4x4.Identity);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            if (e.Length < 12f) continue;
            var x = e.Length * 0.5f;
            GridFacade.Box(b, e.Frame, x - 0.8f, x + 0.8f, y + 1.2f, y + S.TowerHeight - 2.0f, -0.05f, 0.04f, L.Glass, 0);
            GridFacade.Box(b, e.Frame, x - 1.0f, x + 1.0f, y + S.TowerHeight - 2.0f, y + S.TowerHeight - 1.8f, -0.05f, 0.12f, L.Trim, 0);
        }
    }
}
