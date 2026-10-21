using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurniturePlanner;
using static Campus.Gen.Furniture.FurnitureParts;

namespace Campus.Gen.Furniture;

/// <summary>Places capacity-admitted stone rows at wide walk mouths.</summary>
internal static class FurnitureBarriers
{

    // ---------------------------------------------------------------------------------------------------------
    // Barriers (WLD-LIFE-STREET 路障): white stone spheres across the mouth of a wide walk at a carriageway.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>A carriageway with its box, grown by its half width and the kerb reach: what is farther from a point cannot reach it.</summary>
    private sealed record BarrierRoad(NetworkLine Line, Vector2 Min, Vector2 Max);

    /// <summary>
    /// At every main junction, a row of stone spheres across the walk (see <see cref="FurnitureStyle.BarrierSphere"/> for the rule and its numbers):
    /// found from the walk side, for each pedestrian walk at least <see cref="FurnitureStyle.BarrierWalkMin"/> wide: its two ends within
    /// <see cref="FurnitureStyle.RampKerbReach"/> of a carriageway's kerb, and both sides of every carriageway it crosses. A sphere that does not
    /// fit (an outline, a tree, a tree row, a road, steep ground, another item) is left out; a row over its cell's budget is left out whole.
    /// The south gate's forecourt spheres and bollards are its zone site's, not this planner's.
    /// </summary>
    internal static void Plan(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics)
    {
        var roads = site.World.Network.Roads
            .Where(l => l.Line.Points.Count >= 2 && StreetNetwork.Carriageway(l) && !StreetNetwork.Covered(l))
            .OrderBy(l => l.Id.Value, StringComparer.Ordinal)
            .Select(l => new BarrierRoad(l, l.Line.Points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - new Vector2(StreetNetwork.HalfWidth(l) + S.RampKerbReach),
                l.Line.Points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + new Vector2(StreetNetwork.HalfWidth(l) + S.RampKerbReach))).ToList();
        if (roads.Count == 0) return;
        var p = S.BarrierSphere;
        var radius = ((PostCollider)SphereDef.Collision(p)[0]).R;
        var capacity = new FurnitureCapacity();
        var rows = new List<Vector2>();
        int junctions = 0, spheres = 0, heavy = 0;
        foreach (var walk in site.PedestrianLines())
        {
            if (!StreetNetwork.Strollable(walk) || walk.Width < S.BarrierWalkMin || StreetNetwork.Covered(walk)) continue;
            var path = new StreetNetwork.Run([.. walk.Line.Points]);
            var half = StreetNetwork.HalfWidth(walk);
            var mouths = Mouths(walk, path, roads);
            for (var m = 0; m < mouths.Count; m++)
            {
                var (station, dir) = mouths[m];
                // From the mouth into the walk to the kerb, then Setback further in.
                var kerb = station;
                var steps = (int)MathF.Ceiling(S.BarrierSearch / S.BarrierStep);
                while (steps-- > 0 && kerb >= 0f && kerb <= path.Length && !site.OffCarriageways(path.At(kerb), 0f, 0f)) kerb += dir * S.BarrierStep;
                var at = kerb + dir * S.BarrierSetback;
                if (steps < 0 || at < 0f || at > path.Length || path.Normal(at) is not { } normal) continue;
                var centre = path.At(at);
                if (rows.Any(q => Vector2.Distance(q, centre) < S.BarrierRowGap)) continue;
                var extent = half - radius - S.BarrierEdge;
                var count = extent <= 0f ? 1 : (int)MathF.Floor(2f * extent / S.BarrierPitch) + 1;
                var row = new List<FurnitureItem>();
                for (var k = 0; k < count; k++)
                {
                    var pos = centre + normal * ((k - (count - 1) * 0.5f) * S.BarrierPitch);
                    if (Land.Point(site.World.Terrain, pos, radius) is not { } lawn || !site.OffRows(pos, radius)
                        || site.Blocker(pos, radius, walk, onBeside: true, walksPass: true) is not null) continue;
                    // On paving the sphere stands on the paving; on the end of a strip game/gen/ground/ may leave lawn, so it lands as a post.
                    var y = site.Paving.OnPavedGround(pos) ? site.World.Terrain.HeightAt(pos.X, pos.Y) + GroundLevels.Paving : lawn;
                    row.Add(Item($"barrier:{walk.Id.Value}:{Num(m)}:{Num(k)}", FurnitureKind.Barrier, PartCatalog.StoneSphere, p, Frame(pos, normal, y), SphereDef.Collision(p), []));
                }
                if (row.Count == 0) continue;
                if (!capacity.Allows(items, row)) { heavy++; continue; }
                items.AddRange(row);
                foreach (var sphere in row) site.Occupy(sphere.Anchor, radius);
                rows.Add(centre);
                junctions++;
                spheres += row.Count;
            }
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "furniture.barriers.summary", $"barriers: {Num(spheres)} stone spheres in {Num(junctions)} rows at walk mouths ({Num(heavy)} rows over a cell's budget, left out)"));
    }

    /// <summary>
    /// The places on <paramref name="walk"/> where it meets a carriageway, as (arc length, direction into the walk: +1 or −1): each end within
    /// <see cref="FurnitureStyle.RampKerbReach"/> of a carriageway's kerb looks into the walk, and a crossing looks both ways.
    /// </summary>
    private static List<(float Station, float Dir)> Mouths(NetworkLine walk, StreetNetwork.Run path, IReadOnlyList<BarrierRoad> roads)
    {
        var points = walk.Line.Points;
        var mouths = new List<(float, float)>();
        bool Reaches(Vector2 q) => roads.Any(r => q.X >= r.Min.X && q.X <= r.Max.X && q.Y >= r.Min.Y && q.Y <= r.Max.Y
            && MathF.Sqrt(PlanLines.PolylineDistanceSquared(r.Line.Line.Points, q)) <= StreetNetwork.HalfWidth(r.Line) + S.RampKerbReach);
        if (Reaches(points[0])) mouths.Add((0f, 1f));
        if (Reaches(points[^1])) mouths.Add((path.Length, -1f));
        var min = points.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
        var max = points.Aggregate(new Vector2(float.MinValue), Vector2.Max);
        foreach (var road in roads)
        {
            if (road.Max.X < min.X || road.Min.X > max.X || road.Max.Y < min.Y || road.Min.Y > max.Y) continue;
            var line = road.Line.Line.Points;
            for (var i = 1; i < points.Count; i++)
                for (var j = 1; j < line.Count; j++)
                    if (PlanLines.SegmentIntersection(points[i - 1], points[i], line[j - 1], line[j], 1e-6f) is { } x)
                    {
                        var station = (float)PlanLines.Project(points, x).Station;
                        mouths.Add((station, 1f));
                        mouths.Add((station, -1f));
                    }
        }
        return mouths;
    }
}
