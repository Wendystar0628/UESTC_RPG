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
    // Drinking fountains and signposts (Opus review 13c2b 21:05): interim rules until WP-44's walk: points replace them.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// One drinking fountain for each sports venue and teaching building (<see cref="FurnitureStyle.FountainCategories"/>) with a main
    /// entrance: the first open entrance by ID of its interior-index entry. It stands beside the paving <see cref="FurnitureStyle.FountainNear"/>–
    /// <see cref="FurnitureStyle.FountainFar"/> in front of the door, the nearest spot that fits first, on a bay joined to the paving and
    /// facing it, under the walk furniture's rules (<see cref="PavingSlot"/>). The count and the buildings left without go to the diagnostics.
    /// </summary>
    private static void Fountains(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, EquipmentParams p)
    {
        var done = new HashSet<FeatureId>();
        var placed = 0;
        var left = new List<string>();
        foreach (var feature in site.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (!S.FountainCategories.Contains(WorldProps.TryText(feature.Props, "category") ?? "")) continue;
            if (site.World.Interiors.Get(feature.Id) is not { } entry || !done.Add(entry.Feature)) continue;
            var door = entry.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault();
            if (door is null) continue;
            var plan = new Vector2(door.Center.X, door.Center.Z);
            var facing = Angles.Heading(door.YawDeg);
            WalkFit? fit = null;
            foreach (var edge in site.PavingEdgesNear(plan, S.FountainFar, S.NodeStep))
            {
                var away = edge.Edge - plan;
                if (away.Length() < S.FountainNear || Vector2.Dot(away, facing) <= 0f) continue;
                if (PavingSlot(site, edge.Edge, edge.Inward, edge.Road, p, facesWalk: false).Fit is not { } slot) continue;
                fit = slot;
                break;
            }
            if (fit is not { } at) { left.Add(door.Id); continue; }
            items.Add(Placed($"fountain:{door.Id}", FurnitureKind.DrinkingFountain, PartCatalog.DrinkingFountain, p, at, FountainDef.Collision(p)));
            site.Occupy(at.Ends.A, at.Ends.B, at.Ends.R);
            placed++;
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.fountains.summary", $"drinking fountains: {placed} by the main entrances of sports and teaching buildings" +
            (left.Count == 0 ? "" : $"; no room {Num(S.FountainNear)} to {Num(S.FountainFar)} m out at {string.Join(", ", left)}")));
    }

    /// <summary>
    /// Signposts at the junctions of the effective network's pedestrian lines where <see cref="FurnitureStyle.SignpostBranches"/> or more
    /// branches meet and a landmark lies within <see cref="FurnitureStyle.SignpostLandmarkReach"/> (<see cref="Junctions"/>): the junctions
    /// with more branches first, then the nearer landmark, none within <see cref="FurnitureStyle.SignpostSpacing"/> of a placed one (one to a
    /// line of sight). Each stands beside the paving nearest its junction, within <see cref="FurnitureStyle.SignpostReach"/>, facing it, on a
    /// bay joined to the paving (<see cref="PavingSlot"/>). The counts go to the diagnostics.
    /// </summary>
    private static void Signposts(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics)
    {
        var p = S.Signpost;
        var landmarks = site.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Where(f => S.LandmarkSubcategories.Contains(WorldProps.TryText(f.Props, "subcategory") ?? "") || S.LandmarkCategories.Contains(WorldProps.TryText(f.Props, "category") ?? ""))
            .ToList();
        var junctions = Junctions(site);
        var candidates = new List<(Vector2 At, int Branches, float Landmark)>();
        foreach (var (at, branches) in junctions)
        {
            if (branches < S.SignpostBranches) continue;
            var nearest = landmarks.Count == 0 ? float.MaxValue : landmarks.Min(f => DistanceToFeature(at, f));
            if (nearest <= S.SignpostLandmarkReach) candidates.Add((at, branches, nearest));
        }
        var placed = new List<Vector2>();
        int crowded = 0, cramped = 0;
        foreach (var (at, _, _) in candidates.OrderByDescending(c => c.Branches).ThenBy(c => c.Landmark).ThenBy(c => c.At.X).ThenBy(c => c.At.Y))
        {
            if (placed.Any(q => Vector2.Distance(q, at) < S.SignpostSpacing)) { crowded++; continue; }
            WalkFit? fit = null;
            foreach (var edge in site.PavingEdgesNear(at, S.SignpostReach, 0.5f))
                if (PavingSlot(site, edge.Edge, edge.Inward, edge.Road, p, facesWalk: false).Fit is { } slot) { fit = slot; break; }
            if (fit is not { } spot) { cramped++; continue; }
            items.Add(Placed($"signpost:{Num(placed.Count)}", FurnitureKind.Signpost, PartCatalog.Signpost, p, spot, SignpostDef.Collision(p)));
            site.Occupy(spot.Ends.A, spot.Ends.B, spot.Ends.R);
            placed.Add(at);
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.signposts.summary", $"signposts: {placed.Count} at walk junctions of {S.SignpostBranches}+ branches within {Num(S.SignpostLandmarkReach)} m of a landmark " +
            $"({junctions.Count(j => j.Branches >= S.SignpostBranches)} such junctions, {candidates.Count} near a landmark; {crowded} within {Num(S.SignpostSpacing)} m of another, {cramped} with no room beside the paving)"));
    }

    /// <summary>
    /// Junctions of the pedestrian lines (WP-08's effective network): line ends gathered within <see cref="FurnitureStyle.JunctionSnap"/> of
    /// the first end of their group (lines in ID order), each with its branch count, a line that runs through the point without ending
    /// there counting two. Only points where two or more lines meet are kept.
    /// </summary>
    internal static List<(Vector2 At, int Branches)> Junctions(StreetSite site)
    {
        var lines = site.PedestrianLines().ToList();
        var groups = new List<(Vector2 At, List<int> Lines)>();
        for (var i = 0; i < lines.Count; i++)
            foreach (var end in new[] { lines[i].Line.Points[0], lines[i].Line.Points[^1] })
            {
                var g = groups.FindIndex(x => Vector2.Distance(x.At, end) <= S.JunctionSnap);
                if (g < 0) groups.Add((end, [i]));
                else groups[g].Lines.Add(i);
            }
        var result = new List<(Vector2, int)>();
        foreach (var (at, ends) in groups)
        {
            var branches = ends.Count;
            var through = 0;
            for (var i = 0; i < lines.Count; i++)
            {
                if (ends.Contains(i)) continue;
                var points = lines[i].Line.Points;
                if (Vector2.Distance(points[0], at) <= S.JunctionSnap || Vector2.Distance(points[^1], at) <= S.JunctionSnap) continue;
                if (MathF.Sqrt(PlanLines.PolylineDistanceSquared(lines[i].Line.Points, at)) <= S.JunctionSnap) { branches += 2; through++; }
            }
            if (ends.Distinct().Count() + through >= 2) result.Add((at, branches));
        }
        return result;
    }

    /// <summary>Plan distance from <paramref name="p"/> to a feature: to its outline (0 inside) for an area, else to its anchor.</summary>
    private static float DistanceToFeature(Vector2 p, WorldFeature feature)
    {
        IReadOnlyList<PolygonRings> polygons = feature.Geometry switch
        {
            PolygonGeometry polygon => [polygon.Rings],
            MultiPolygonGeometry multi => multi.Polygons,
            _ => [],
        };
        if (polygons.Count == 0) return Vector2.Distance(p, Anchor(feature));
        var best = float.MaxValue;
        foreach (var rings in polygons)
            best = MathF.Min(best, PlanGeometry.Contains(rings, p) ? 0f : PlanGeometry.DistanceToRing(p, rings.Outer));
        return best;
    }
}
