using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlanner;

namespace Campus.Gen.Furniture;

/// <summary>Admits service rows outside gates and in front of doors.</summary>
internal static class FurnitureServiceSlots
{
    /// <summary>
    /// A row of things <paramref name="width"/> long and <paramref name="depth"/> deep serving <paramref name="spot"/>: off the edge of the
    /// pedestrian paving or carriageway nearest the site (within <paramref name="reach"/>), facing it with <paramref name="front"/> of its bay in
    /// front, its back away from it; free of outlines, other roads, trees, the planted rows and placed things, outside the site's outline
    /// (a cluster's) by <see cref="FurnitureStyle.GateOutside"/> and <see cref="FurnitureStyle.GateKeep"/> off its point; for a door
    /// (<see cref="ServiceSite.Front"/>), <see cref="FurnitureStyle.BayNear"/> or more out and in front of it.
    /// </summary>
    internal static WalkFit? ServiceFit(StreetSite site, ServiceSite spot, float width, float depth, float front, float reach)
    {
        foreach (var (edge, inward, line) in ServiceEdges(site, spot.Front ?? spot.At, reach))
        {
            var centre = edge - inward * (front + depth * 0.5f);
            var tangent = new Vector2(inward.Y, -inward.X);
            var reachAlong = MathF.Max(0f, width * 0.5f - depth * 0.5f);
            var ends = (A: centre - tangent * reachAlong, B: centre + tangent * reachAlong, R: depth * 0.5f + 0.05f);
            if (spot.Front is { } door)
            {
                var away = centre - door;
                if (away.Length() < S.BayNear || Vector2.Dot(away, spot.At - door) <= 0f) continue;
            }
            else if (Vector2.Distance(centre, spot.At) < S.GateKeep + width * 0.5f && spot.Outside is not null) continue;
            if (spot.Outside is { } outline)
            {
                var outside = true;
                for (var u = -1; u <= 1 && outside; u++)
                    for (var v = -1; v <= 1 && outside; v++)
                    {
                        var q = centre + tangent * (u * width * 0.5f) + inward * (v * (depth * 0.5f + front));
                        if (PlanRings.Contains(outline, q) || PlanRings.DistanceToRing(q, outline.Outer) < S.GateOutside) outside = false;
                    }
                if (!outside) continue;
            }
            if (site.Blocker(ends.A, ends.B, ends.R, line) is not null) continue;
            if (!new[] { ends.A, centre, ends.B }.All(q => site.OffRows(q, ends.R))) continue;
            // The bay: from just over the edge to behind the row, past its ends.
            var bayFront = depth * 0.5f + front + S.PadJoin;
            var bayBack = depth * 0.5f + S.BayBehind;
            var middle = centre + inward * ((bayFront - bayBack) * 0.5f);
            if (Land.Pad(site.World.Terrain, Frame(middle, inward, 0f), width * 0.5f + S.BayEnds, (bayFront + bayBack) * 0.5f) is not { } bay) continue;
            return new WalkFit(centre, inward, ends, bay, bay.Top);
        }
        return null;
    }

    /// <summary>
    /// Edge spots of the pedestrian paving and of the carriageways within <paramref name="reach"/> of <paramref name="p"/>, each with the unit
    /// normal into the paving or road and the network line it belongs to, nearest first (ties by position).
    /// </summary>
    private static List<(Vector2 Edge, Vector2 Inward, NetworkLine? Line)> ServiceEdges(StreetSite site, Vector2 p, float reach)
    {
        var found = new List<(float D, Vector2 Edge, Vector2 Inward, NetworkLine? Line)>();
        foreach (var (edge, inward, road) in site.Paving.PavingEdgesNear(p, reach, 1f)) found.Add((Vector2.Distance(edge, p), edge, inward, road));
        foreach (var line in site.World.Network.Roads.Where(StreetNetwork.Carriageway).OrderBy(l => l.Id.Value, StringComparer.Ordinal))
        {
            var half = StreetNetwork.HalfWidth(line);
            if (MathF.Sqrt(PlanLines.PolylineDistanceSquared(line.Line.Points, p)) > reach + half) continue;
            var run = new StreetNetwork.Run([.. line.Line.Points]);
            for (var s = 0f; s <= run.Length; s += 1f)
            {
                var q = run.At(s);
                if (Vector2.Distance(q, p) > reach + half || run.Normal(s) is not { } n) continue;
                foreach (var side in new[] { 1f, -1f })
                {
                    var edge = q + n * (side * half);
                    // Not inside a junction: just beyond the edge is not carriageway.
                    if (!site.OffCarriageways(edge + n * (side * 0.3f), 0f, 0f)) continue;
                    var d = Vector2.Distance(edge, p);
                    if (d <= reach) found.Add((d, edge, -n * side, line));
                }
            }
        }
        return found.OrderBy(f => f.D).ThenBy(f => f.Edge.X).ThenBy(f => f.Edge.Y).Select(f => (f.Edge, f.Inward, f.Line)).ToList();
    }
}
