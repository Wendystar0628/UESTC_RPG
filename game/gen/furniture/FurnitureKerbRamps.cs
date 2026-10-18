using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

public static partial class FurniturePlanner
{
    // ---------------------------------------------------------------------------------------------------------
    // Kerb ramps (13c2b): where a walk reaches the kerb of a ramped road, at its end or where it crosses the road.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A kerb ramp at every spot where a pedestrian line reaches the carriageway of <paramref name="rule"/>: a walk ending within
    /// <see cref="FurnitureStyle.RampKerbReach"/> of its kerb gets one on that kerb, a walk crossing it one on each kerb. The ramp lies on
    /// the carriageway against the kerb, rising to the kerb top and onto the paving behind it (sliding along the kerb by up to
    /// <see cref="FurnitureStyle.RampSlide"/> to find it, never onto a verge), <see cref="FurnitureStyle.RampGap"/> clear of the next; none inside a
    /// junction (on another road's surface), which WP-15 marks and paves. The count and the refusals go to the layout diagnostics.
    /// </summary>
    private static void KerbRamps(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, int r, RoadRule rule)
    {
        var lines = site.World.Network.Roads.Where(l => StreetSite.RoadName(l) == rule.Name && StreetSite.Carriageway(l)).OrderBy(l => l.Id.Value, StringComparer.Ordinal).ToList();
        if (lines.Count == 0) return;
        var p = S.KerbRamp;
        var placed = new List<Vector2>();
        int junction = 0, crowded = 0, verge = 0;
        foreach (var walk in site.PedestrianLines())
        {
            var points = walk.Line.Points;
            var spots = new List<(Vector2 At, bool Crossing)> { (points[0], false), (points[^1], false) };
            foreach (var line in lines)
                for (var i = 1; i < points.Count; i++)
                    for (var j = 1; j < line.Line.Points.Count; j++)
                        if (PlanGeometry.SegmentIntersection(points[i - 1], points[i], line.Line.Points[j - 1], line.Line.Points[j], 1e-6f) is { } x) spots.Add((x, true));
            foreach (var (spot, crossing) in spots)
            {
                if (Nearest(lines, spot) is not { } near) continue;
                var half = StreetSite.HalfWidth(near.Line);
                var normal = new Vector2(-near.Along.Y, near.Along.X);
                var offset = Vector2.Dot(spot - near.Point, normal);
                float[] sides;
                if (crossing) sides = [1f, -1f];
                else if (MathF.Abs(offset) >= half - 0.5f && MathF.Abs(offset) <= half + S.RampKerbReach) sides = [MathF.Sign(offset)];
                else continue;
                foreach (var side in sides)
                {
                    // The ramp leads onto pedestrian paving behind the kerb, not onto a verge: it slides along the kerb (up to
                    // RampSlide) to where the walk meets it.
                    Vector2? found = null;
                    foreach (var shift in Shifts(S.RampSlide))
                    {
                        var kerb = near.Point + near.Along * shift + normal * (side * half);
                        if (Nearest(lines, kerb) is not { } on || Vector2.Distance(on.Point, near.Point + near.Along * shift) > 0.5f) continue;
                        if (site.PavingEdgeFacing(kerb + normal * (side * S.RampWalkBehind)) is not { Gap: <= 0f }) continue;
                        found = near.Point + near.Along * shift + normal * (side * (half - p.Depth * 0.5f));
                        break;
                    }
                    if (found is not { } centre) { verge++; continue; }
                    if (placed.Any(q => Vector2.Distance(q, centre) < S.RampGap)) { crowded++; continue; }
                    if (site.World.Network.Roads.Any(o => !lines.Contains(o) && StreetSite.Carriageway(o) && MathF.Sqrt(PlanGeometry.PolylineDistanceSquared(o.Line.Points, centre)) < StreetSite.HalfWidth(o) + p.Width * 0.5f))
                    {
                        junction++;
                        continue;
                    }
                    var at = Frame(centre, -normal * side, site.World.Terrain.HeightAt(centre.X, centre.Y));
                    items.Add(Item($"ramp:{r}:{Num(placed.Count)}", FurnitureKind.KerbRamp, StreetStyle.KerbRampId, p, at, RampDef.Collision(p), []));
                    placed.Add(centre);
                }
            }
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.kerb-ramps.summary", $"road {rule.Name}: {Num(placed.Count)} kerb ramps where walks reach its kerb ({Num(junction)} spots inside junctions, {Num(verge)} with no walk behind the kerb, {Num(crowded)} next to a ramp, left out)"));
    }

    /// <summary>The nearest point of any of <paramref name="lines"/> to <paramref name="p"/>, with the unit direction of its segment.</summary>
    private static (Vector2 Point, Vector2 Along, NetworkLine Line)? Nearest(IReadOnlyList<NetworkLine> lines, Vector2 p)
    {
        (Vector2, Vector2, NetworkLine)? best = null;
        var bestD = float.MaxValue;
        foreach (var line in lines)
        {
            var near = PlanGeometry.Project(line.Line.Points, p);
            var d = Vector2.DistanceSquared(p, near.Point);
            if (d >= bestD) continue;
            bestD = d;
            best = (near.Point, near.Tangent, line);
        }
        return best;
    }

}
