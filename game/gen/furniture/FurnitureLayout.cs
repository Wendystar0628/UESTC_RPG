using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

public enum FurnitureKind : byte { StreetLamp, Bench, Bin, BusStop, AtlasKiosk, AtlasPost, EntranceMat, OpenSign, EntranceLamp, ParkedCar, KerbRamp, Signpost, DrinkingFountain, VendingMachine, DeliveryLocker, BicycleBay, ParkedBicycle }

/// <summary>
/// One placed piece of street furniture: the catalogue part with its world matrix, its lamp-inventory markers, its world-space
/// colliders (the part's own, plus its pad's) and the pad it brings, if any (a rest stop's bay, a kiosk's apron, a plinth).
/// </summary>
public sealed record FurnitureItem(string Key, FurnitureKind Kind, PlacedPart Part)
{
    public IReadOnlyList<LightEmitter> Emitters { get; init; } = [];
    public IReadOnlyList<Collider> Colliders { get; init; } = [];
    public Pad? Pad { get; init; }
    public Vector2 Anchor => new(Part.At.M41, Part.At.M43);
}

/// <summary>All street furniture of one map, in a stable order (anchors, entrances, stops, then roads in rule order).</summary>
public sealed record FurnitureLayout(IReadOnlyList<FurnitureItem> Items)
{
    /// <summary>
    /// One line per atlas anchor the layout left out (its point lands on a carriageway, in water or in a building) or left where
    /// the data put it (no pedestrian paving within reach), in anchor order: for the anchor data's review (WP-48c).
    /// </summary>
    public IReadOnlyList<string> Log { get; init; } = [];

    /// <summary>
    /// Deterministic build diagnostics in a stable order (ContentOutput.Diagnostics carries them out): a warning where the map's
    /// planting input could not give a road's tree rows and the fallback stood in.
    /// </summary>
    public IReadOnlyList<BuildDiagnostic> Diagnostics { get; init; } = [];

    public int Count(FurnitureKind kind) => Items.Count(i => i.Kind == kind);
}

/// <summary>
/// WP-13 placement (13b): pure and deterministic from the world model and the map's claims.
/// <list type="bullet">
/// <item>Atlas anchors (SYS-ATLAS §6): a kiosk or a post on every <c>anchor</c> feature of WP-48c's <c>atlas.overlay.json</c>,
/// at its footprint, facing its <c>yawDeg</c>; the kiosk on its pad (or the deck the overlay names), the post landed as a point.
/// A footprint more than <see cref="FurnitureStyle.AnchorPavingGap"/> off pedestrian paving moves the shortest way to its edge.</item>
/// <item>Open entrances (WLD-INTERIORS §4): mat, standing open sign and door lamp at every open entrance of the interior index,
/// on the entrance floor, facing out; skipped where another package claims the building's <see cref="Aspects.Props"/>.</item>
/// <item>Shuttle stops (WLD-LIFE §4.3): a stop sign at the kerb of the carriageway nearest each <c>bus</c> feature.</item>
/// <item>Roads (WLD-LIFE §4, MAP-QSH-ADDITIONS §8.1): lamps along the <see cref="FurnitureStyle.Roads"/> at the tier's spacing,
/// and rest stops midway between lamps: a bench lining the nearest walk on that side, facing the walk on a bay joined to it,
/// never facing a carriageway or in the strip between the kerb and the tree rows; at every other stop a four-stream bin on its
/// own bay 2–4 m along the same walk, towards a junction where there is one (Opus review 12:15).</item>
/// </list>
/// Everything lands on <see cref="IWorldModel.Terrain"/> (<see cref="Land"/>) and keeps clear of buildings, water, courts,
/// parking, other roads, bridges, trees and each other (<see cref="StreetSite"/>).
/// </summary>
public static partial class FurniturePlanner
{
    private static readonly StreetLampPart Lamp = new();
    private static readonly BenchPart BenchDef = new();
    private static readonly BinFourPart BinDef = new();
    private static readonly BusStopPart StopDef = new();
    private static readonly AtlasKioskPart KioskDef = new();
    private static readonly AtlasPostPart PostDef = new();
    private static readonly OpenSignPart SignDef = new();
    private static readonly ParkedCarPart CarDef = new();
    private static readonly KerbRampPart RampDef = new();
    private static readonly SignpostPart SignpostDef = new();
    private static readonly DrinkingFountainPart FountainDef = new();
    private static readonly WpId Self = new("WP-13");

    /// <summary>Pole footprint radius used for clearances and landing (the lamp's base flange).</summary>
    private const float PoleRadius = 0.25f;

    /// <summary>Half the stop board's thickness plus its clamp, for its clearance capsule.</summary>
    private const float StopHalfDepth = 0.15f;

    /// <param name="world">The world model.</param>
    /// <param name="claimedBy">The claims query (<see cref="BuildContext.ClaimedBy"/>); none means nothing is claimed.</param>
    /// <param name="inputs">The map's preloaded source inputs (<see cref="BuildContext.SourceInputs"/>): the tree rows come from its <see cref="S.PlantingKey"/>.</param>
    public static FurnitureLayout Build(IWorldModel world, Func<FeatureId, Aspects, WpId?>? claimedBy = null, IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs = null) =>
        Build(world, claimedBy, inputs, S.Roads);

    /// <summary><see cref="Build(IWorldModel, Func{FeatureId, Aspects, WpId?}?, IReadOnlyDictionary{string, ReadOnlyMemory{byte}}?)"/> for given road rules (the style's own for the module; tests give their own).</summary>
    internal static FurnitureLayout Build(IWorldModel world, Func<FeatureId, Aspects, WpId?>? claimedBy, IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs, IReadOnlyList<RoadRule> rules)
    {
        ArgumentNullException.ThrowIfNull(world);
        claimedBy ??= (_, _) => null;
        var (roads, diagnostics) = PlantingRows.Resolve(inputs, rules);
        var site = new StreetSite(world);
        foreach (var rule in roads) site.AddRows(world.Network.Roads.Where(l => StreetSite.RoadName(l) == rule.Name), rule.Rows);
        var items = new List<FurnitureItem>();
        var log = new List<string>();
        Anchors(site, items, log);
        Entrances(site, items, claimedBy);
        Stops(site, items);
        for (var r = 0; r < roads.Count; r++) Road(site, items, log, r, roads[r]);
        for (var r = 0; r < roads.Count; r++)
            if (roads[r].KerbRamps) KerbRamps(site, items, log, r, roads[r]);
        Cars(site, items);
        Fountains(site, items, log);
        Signposts(site, items, log);
        Services(site, items, log, roads);
        return new(items.AsReadOnly()) { Log = log.AsReadOnly(), Diagnostics = diagnostics };
    }

    // ---------------------------------------------------------------------------------------------------------
    // Kerb ramps (13c2b): where a walk reaches the kerb of a ramped road, at its end or where it crosses the road.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A kerb ramp at every spot where a pedestrian line reaches the carriageway of <paramref name="rule"/>: a walk ending within
    /// <see cref="FurnitureStyle.RampKerbReach"/> of its kerb gets one on that kerb, a walk crossing it one on each kerb. The ramp lies on
    /// the carriageway against the kerb, rising to the kerb top and onto the paving behind it (sliding along the kerb by up to
    /// <see cref="FurnitureStyle.RampSlide"/> to find it, never onto a verge), <see cref="FurnitureStyle.RampGap"/> clear of the next; none inside a
    /// junction (on another road's surface), which WP-15 marks and paves. The count and the refusals go to the layout log.
    /// </summary>
    private static void KerbRamps(StreetSite site, List<FurnitureItem> items, List<string> log, int r, RoadRule rule)
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
                        if (Intersect(points[i - 1], points[i], line.Line.Points[j - 1], line.Line.Points[j]) is { } x) spots.Add((x, true));
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
                    if (site.World.Network.Roads.Any(o => !lines.Contains(o) && StreetSite.Carriageway(o) && DistanceTo(centre, o) < StreetSite.HalfWidth(o) + p.Width * 0.5f))
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
        log.Add($"road {rule.Name}: {Num(placed.Count)} kerb ramps where walks reach its kerb ({Num(junction)} spots inside junctions, {Num(verge)} with no walk behind the kerb, {Num(crowded)} next to a ramp, left out)");
    }

    /// <summary>The nearest point of any of <paramref name="lines"/> to <paramref name="p"/>, with the unit direction of its segment.</summary>
    private static (Vector2 Point, Vector2 Along, NetworkLine Line)? Nearest(IReadOnlyList<NetworkLine> lines, Vector2 p)
    {
        (Vector2, Vector2, NetworkLine)? best = null;
        var bestD = float.MaxValue;
        foreach (var line in lines)
            for (var i = 1; i < line.Line.Points.Count; i++)
            {
                var a = line.Line.Points[i - 1]; var ab = line.Line.Points[i] - a;
                var len2 = ab.LengthSquared();
                if (len2 < 1e-6f) continue;
                var q = a + ab * Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
                var d = Vector2.DistanceSquared(p, q);
                if (d >= bestD) continue;
                bestD = d;
                best = (q, ab / MathF.Sqrt(len2), line);
            }
        return best;
    }

    private static float DistanceTo(Vector2 p, NetworkLine line)
    {
        var best = float.MaxValue;
        for (var i = 1; i < line.Line.Points.Count; i++) best = MathF.Min(best, PlanGeometry.SegmentDistanceSquared(p, line.Line.Points[i - 1], line.Line.Points[i]));
        return MathF.Sqrt(best);
    }

    /// <summary>The crossing point of segments ab and cd, if they cross.</summary>
    private static Vector2? Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var r = b - a; var s = d - c;
        var denom = r.X * s.Y - r.Y * s.X;
        if (MathF.Abs(denom) < 1e-6f) return null;
        var t = ((c.X - a.X) * s.Y - (c.Y - a.Y) * s.X) / denom;
        var u = ((c.X - a.X) * r.Y - (c.Y - a.Y) * r.X) / denom;
        return t is >= 0f and <= 1f && u is >= 0f and <= 1f ? a + r * t : null;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Parked cars (WLD-LIFE §3.3): a few in each car park.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Up to <see cref="FurnitureStyle.CarsPerLot"/> cars in each car park: slots every <see cref="FurnitureStyle.CarSlot"/> along its longest
    /// edge, nose to that edge, a slot taken when its stable hash says so (<see cref="FurnitureStyle.CarShare"/>); a saloon or a hatchback
    /// in one of the low-saturation body colours. A car stands whole inside the lot, clear of trees and of each other, on ground that
    /// drops less than <see cref="FurnitureStyle.CarMaxDrop"/> under it.
    /// </summary>
    private static void Cars(StreetSite site, List<FurnitureItem> items)
    {
        var terrain = site.World.Terrain;
        // Each car look (shape and colour) is its own mesh set: a 64 m cell keeps to a few of them (draw calls, ART-MODELING §7.8).
        var looks = new Dictionary<(int, int), List<VehicleParams>>();
        for (var lot = 0; lot < site.ParkingLots.Count; lot++)
        {
            var rings = site.ParkingLots[lot];
            var ring = rings.Outer;
            var e = 0;
            for (var i = 1; i < ring.Count; i++)
                if (Vector2.DistanceSquared(ring[i], ring[(i + 1) % ring.Count]) > Vector2.DistanceSquared(ring[e], ring[(e + 1) % ring.Count])) e = i;
            var a = ring[e]; var b = ring[(e + 1) % ring.Count];
            var length = Vector2.Distance(a, b);
            if (length < S.CarSlot * 2f) continue;
            var along = (b - a) / length;
            var inward = new Vector2(-along.Y, along.X);
            var middle = (a + b) * 0.5f;
            if (!PlanGeometry.Contains(rings, middle + inward)) inward = -inward;
            if (!PlanGeometry.Contains(rings, middle + inward)) continue;
            var slots = (int)MathF.Floor((length - 1f) / S.CarSlot);
            var start = (length - slots * S.CarSlot) * 0.5f;
            var parked = 0;
            for (var k = 0; k < slots && parked < S.CarsPerLot; k++)
            {
                var hash = StableHash.Hash32(0x13C2Bu, "car", Num(lot), Num(k));
                if ((hash % 1000u) / 1000f >= S.CarShare) continue;
                var car = (hash >> 10) % 3u == 0u ? S.Hatch : S.Saloon;
                var colour = StreetStyle.CarBodies[(int)((hash >> 12) % (uint)StreetStyle.CarBodies.Length)];
                car = car with { Material = new(StreetStyle.Lit(colour), StreetStyle.Shade(colour)) };
                var centre = a + along * (start + (k + 0.5f) * S.CarSlot) + inward * (S.CarNoseIn + car.Depth * 0.5f);
                var cell = ((int)MathF.Floor(centre.X / 64f), (int)MathF.Floor(centre.Y / 64f));
                if (!looks.TryGetValue(cell, out var used)) looks.Add(cell, used = []);
                if (!used.Contains(car) && used.Count >= S.CarLooksPerCell) car = used[(int)((hash >> 16) % (uint)used.Count)];
                centre = a + along * (start + (k + 0.5f) * S.CarSlot) + inward * (S.CarNoseIn + car.Depth * 0.5f);
                var hw = car.Width * 0.5f; var hd = car.Depth * 0.5f;
                var corners = new[] { centre + along * hw + inward * hd, centre - along * hw + inward * hd, centre + along * hw - inward * hd, centre - along * hw - inward * hd };
                if (!corners.Append(centre).All(q => PlanGeometry.Contains(rings, q) && rings.Holes.Append(rings.Outer).Min(h => PlanGeometry.DistanceToRing(q, h)) >= S.CarLotMargin)) continue;
                var ends = (centre - inward * (hd - hw), centre + inward * (hd - hw));
                if (site.Clutter(ends.Item1, hw) is not null || site.Clutter(centre, hw) is not null || site.Clutter(ends.Item2, hw) is not null) continue;
                var heights = corners.Select(q => terrain.HeightAt(q.X, q.Y)).ToArray();
                if (heights.Max() - heights.Min() > S.CarMaxDrop) continue;
                // The nose (local −Z) towards the lot's edge.
                var at = Frame(centre, -inward, terrain.HeightAt(centre.X, centre.Y));
                items.Add(Item($"car:{Num(lot)}:{Num(k)}", FurnitureKind.ParkedCar, PartCatalog.ParkedCar, car, at, CarDef.Collision(car), []));
                if (!used.Contains(car)) used.Add(car);
                site.Occupy(ends.Item1, ends.Item2, hw);
                parked++;
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Drinking fountains and signposts (Opus review 13c2b 21:05): interim rules until WP-44's walk: points replace them.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// One drinking fountain for each sports venue and teaching building (<see cref="FurnitureStyle.FountainCategories"/>) with a main
    /// entrance: the first open entrance by ID of its interior-index entry. It stands beside the paving <see cref="FurnitureStyle.FountainNear"/>–
    /// <see cref="FurnitureStyle.FountainFar"/> in front of the door, the nearest spot that fits first, on a bay joined to the paving and
    /// facing it, under the walk furniture's rules (<see cref="PavingSlot"/>). The count and the buildings left without go to the log.
    /// </summary>
    private static void Fountains(StreetSite site, List<FurnitureItem> items, List<string> log)
    {
        var p = S.Fountain;
        var done = new HashSet<FeatureId>();
        var placed = 0;
        var left = new List<string>();
        foreach (var feature in site.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (!S.FountainCategories.Contains(Text(feature.Props, "category") ?? "")) continue;
            if (site.World.Interiors.Get(feature.Id) is not { } entry || !done.Add(entry.Feature)) continue;
            var door = entry.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault();
            if (door is null) continue;
            var plan = new Vector2(door.Center.X, door.Center.Z);
            var facing = Facing(door.YawDeg);
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
        log.Add($"drinking fountains: {placed} by the main entrances of sports and teaching buildings" +
            (left.Count == 0 ? "" : $"; no room {Num(S.FountainNear)} to {Num(S.FountainFar)} m out at {string.Join(", ", left)}"));
    }

    /// <summary>
    /// Signposts at the junctions of the effective network's pedestrian lines where <see cref="FurnitureStyle.SignpostBranches"/> or more
    /// branches meet and a landmark lies within <see cref="FurnitureStyle.SignpostLandmarkReach"/> (<see cref="Junctions"/>): the junctions
    /// with more branches first, then the nearer landmark, none within <see cref="FurnitureStyle.SignpostSpacing"/> of a placed one (one to a
    /// line of sight). Each stands beside the paving nearest its junction, within <see cref="FurnitureStyle.SignpostReach"/>, facing it, on a
    /// bay joined to the paving (<see cref="PavingSlot"/>). The counts go to the log.
    /// </summary>
    private static void Signposts(StreetSite site, List<FurnitureItem> items, List<string> log)
    {
        var p = S.Signpost;
        var landmarks = site.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Where(f => S.LandmarkSubcategories.Contains(Text(f.Props, "subcategory") ?? "") || S.LandmarkCategories.Contains(Text(f.Props, "category") ?? ""))
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
        log.Add($"signposts: {placed.Count} at walk junctions of {S.SignpostBranches}+ branches within {Num(S.SignpostLandmarkReach)} m of a landmark " +
            $"({junctions.Count(j => j.Branches >= S.SignpostBranches)} such junctions, {candidates.Count} near a landmark; {crowded} within {Num(S.SignpostSpacing)} m of another, {cramped} with no room beside the paving)");
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
                if (DistanceTo(at, lines[i]) <= S.JunctionSnap) { branches += 2; through++; }
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

    // ---------------------------------------------------------------------------------------------------------
    // Atlas anchors: kiosks and posts on WP-48c's overlay features.
    // ---------------------------------------------------------------------------------------------------------

    private static void Anchors(StreetSite site, List<FurnitureItem> items, List<string> log)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.Where(f => f.Kind.Value == "anchor").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PolygonGeometry { Rings.Outer: { Count: >= 3 } ring }) continue;
            var form = Text(feature.Props, "form");
            if (form is not ("kiosk" or "post")) continue;
            var yaw = Number(feature.Props, "yawDeg") ?? 0f;
            var facing = Facing(yaw);
            var right = new Vector2(-facing.Y, facing.X);
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            var width = Extent(ring, right);
            var depth = Extent(ring, facing);
            var key = "anchor:" + feature.Id.Value;
            // Fallback while anchor data is under review: nothing stands on a carriageway, in water or in a building (A01 and A05
            // wait for the gate plazas of 18c3 and WP-30).
            var landing = site.LandingAt(centre);
            if (landing != Landing.Open)
            {
                log.Add($"anchor {feature.Id.Value} ({form}) not placed: its point lands {Where(landing)}");
                continue;
            }
            switch (form)
            {
                case "kiosk":
                    {
                        var p = S.Kiosk with { Width = width >= 1.4f ? width : S.Kiosk.Width, Depth = depth >= 0.4f ? depth : S.Kiosk.Depth, Height = feature.Height is float h && h >= 2.2f ? h : S.Kiosk.Height };
                        // Large entity (WLD-TERRAIN §4.4): on its pad, with a paved apron; a deck the height field does not carry (A06)
                        // comes from the overlay's groundY. An apron off the paving moves to its edge (a deck is paving already).
                        var ground = terrain.HeightAt(centre.X, centre.Y);
                        var deck = Number(feature.Props, "groundY") is { } y && y > ground + S.DeckAbove ? y : (float?)null;
                        if (deck is null) (centre, facing) = KioskOnPaving(site, centre, facing, p.Width * 0.5f + S.ApronMargin, p.Depth * 0.5f + S.ApronMargin, log, feature.Id);
                        var level = deck ?? terrain.PadY(feature.Id);
                        var plan = Frame(centre, facing, 0f);
                        var apron = Land.Pad(terrain, plan, p.Width * 0.5f + S.ApronMargin, p.Depth * 0.5f + S.ApronMargin, level)!;
                        var at = Frame(centre, facing, apron.Top);
                        var kiosk = Item(key, FurnitureKind.AtlasKiosk, PartCatalog.AtlasKiosk, p, at, KioskDef.Collision(p), [AtlasKioskPart.Emitter(p, at)]);
                        items.Add(kiosk with { Pad = apron, Colliders = [.. kiosk.Colliders, apron.Collider()] });
                        site.Occupy(centre, MathF.Max(p.Width, p.Depth) * 0.5f + S.ApronMargin);
                        break;
                    }
                case "post":
                    {
                        var p = S.Post with { Width = width >= 0.16f ? width : S.Post.Width, Depth = depth >= 0.16f ? depth : S.Post.Depth, Height = feature.Height is float h && h >= 0.6f ? h : S.Post.Height };
                        var radius = MathF.Max(p.Width, p.Depth) * 0.5f;
                        centre = ToPaving(site, centre, facing, radius, radius, log, feature.Id, form);
                        if (Land.Point(terrain, centre, radius) is not { } y) continue;
                        var at = Frame(centre, facing, y);
                        items.Add(Item(key, FurnitureKind.AtlasPost, PartCatalog.AtlasPost, p, at, PostDef.Collision(p), [AtlasPostPart.Emitter(p, at)]));
                        site.Occupy(centre, radius);
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// A kiosk's apron (±<paramref name="halfWidth"/> across, ±<paramref name="halfDepth"/> along <paramref name="facing"/>) that is not whole
    /// on pedestrian paving yet (Opus review 14:20, 16:36, 17:28): it stands whole on the nearest paving (for one partly on it, the edge its
    /// outline crosses farthest), its back edge <see cref="FurnitureStyle.ApronPavingClear"/>
    /// inside the paving edge and parallel to it, its face towards the paving, where people stand: the data's yaw gives way. It slides
    /// along the edge, in steps up to <see cref="FurnitureStyle.ApronSlide"/>, until the whole apron is on paving; where none fits (or
    /// no paving lies within <see cref="FurnitureStyle.AnchorPavingReach"/>) it only moves to touch it, as <see cref="ToPaving"/> does, facing
    /// its data yaw. The new centre and yaw are logged for the anchor data (WP-48c), which takes them over; an apron whole on paving
    /// already stays as the data has it.
    /// </summary>
    private static (Vector2 Centre, Vector2 Facing) KioskOnPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth, List<string> log, FeatureId id)
    {
        // An apron whole on paving stays; one that only partly is (13c2a: A03, about half its outline off) turns onto the edge
        // its outline crosses, as one wholly off does onto the nearest edge (Opus review 17:28).
        if (OnPaving(site, centre, facing, halfWidth, halfDepth)) return (centre, facing);
        if ((site.PavingEdgeFacing(centre) is { Gap: > 0f } off ? off : FarthestOff(site, centre, facing, halfWidth, halfDepth)) is { } hit && hit.Gap <= S.AnchorPavingReach)
        {
            var inward = hit.Inward;
            var along = new Vector2(-inward.Y, inward.X);
            foreach (var slide in Shifts(S.ApronSlide))
            {
                var at = hit.Edge + along * slide + inward * (S.ApronPavingClear + halfDepth);
                if (Vector2.Distance(at, centre) > S.AnchorPavingReach || !OnPaving(site, at, inward, halfWidth, halfDepth)) continue;
                log.Add($"anchor {id.Value} (kiosk) turned onto paving: yawDeg {Fixed(YawDeg(inward))} (data {Fixed(YawDeg(facing))}), centre ({Fixed(at.X)}, {Fixed(at.Y)}) (data ({Fixed(centre.X)}, {Fixed(centre.Y)}))");
                return (at, inward);
            }
        }
        return (ToPaving(site, centre, facing, halfWidth, halfDepth, log, id, "kiosk"), facing);
    }

    /// <summary>
    /// For a rectangle partly on paving: the paving edge seen from the outline point farthest off it (every <see cref="FurnitureStyle.ApronSample"/>),
    /// with its gap measured from the centre's side, so the apron can turn its back onto that edge. Null when no outline point is off.
    /// </summary>
    private static (float Gap, Vector2 Edge, Vector2 Inward, NetworkLine? Road)? FarthestOff(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth)
    {
        var right = new Vector2(-facing.Y, facing.X);
        (float Gap, Vector2 Edge, Vector2 Inward, NetworkLine? Road)? far = null;
        for (var u = -halfWidth; u <= halfWidth + 1e-3f; u += S.ApronSample)
            for (var v = -halfDepth; v <= halfDepth + 1e-3f; v += S.ApronSample)
                if ((MathF.Abs(u) >= halfWidth - 1e-3f || MathF.Abs(v) >= halfDepth - 1e-3f) && site.PavingEdgeFacing(centre + right * u + facing * v) is { Gap: > 0f } hit
                    && (far is null || hit.Gap > far.Value.Gap))
                    far = hit;
        return far is { } f ? (Vector2.Distance(centre, f.Edge), f.Edge, f.Inward, f.Road) : null;
    }

    /// <summary>True when every point of a rectangle's outline (every <see cref="FurnitureStyle.ApronSample"/>) lies on pedestrian paving.</summary>
    private static bool OnPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth)
    {
        var right = new Vector2(-facing.Y, facing.X);
        for (var u = -halfWidth; u <= halfWidth + 1e-3f; u += S.ApronSample)
            for (var v = -halfDepth; v <= halfDepth + 1e-3f; v += S.ApronSample)
                if ((MathF.Abs(u) >= halfWidth - 1e-3f || MathF.Abs(v) >= halfDepth - 1e-3f) && site.PavingEdgeFacing(centre + right * u + facing * v) is not { Gap: <= 0f })
                    return false;
        return true;
    }

    /// <summary>
    /// The anchor's centre, moved the shortest way until its footprint (±<paramref name="halfWidth"/> across, ±<paramref name="halfDepth"/>
    /// along <paramref name="facing"/>) meets the nearest pedestrian paving, when it does not touch it yet; the facing stays. The footprint ends <see cref="FurnitureStyle.PadJoin"/> over the paving edge, so the two join. Paving
    /// further than <see cref="FurnitureStyle.AnchorPavingReach"/> is a data question (WP-48c), not a move: the anchor stays, logged.
    /// </summary>
    private static Vector2 ToPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth, List<string> log, FeatureId id, string form)
    {
        var right = new Vector2(-facing.Y, facing.X);
        var start = centre;
        for (var pass = 0; pass < 3; pass++)
        {
            // The footprint's rim point nearest to paving, and the paving edge it looks at.
            (float Gap, Vector2 From, Vector2 Edge)? near = null;
            for (var i = -1; i <= 1; i++)
                for (var k = -1; k <= 1; k++)
                {
                    var q = centre + right * (i * halfWidth) + facing * (k * halfDepth);
                    if (site.PavingEdge(q) is { } e && (near is null || e.Gap < near.Value.Gap)) near = (e.Gap, q, e.Edge);
                }
            if (near is { Gap: <= 0f }) break;
            if (near is { } n) centre += Vector2.Normalize(n.Edge - n.From) * (n.Gap + S.PadJoin);
            if (near is null || Vector2.Distance(start, centre) > S.AnchorPavingReach)
            {
                log.Add($"anchor {id.Value} ({form}) left in place: no pedestrian paving within {Num(S.AnchorPavingReach)} m");
                return start;
            }
        }
        return centre;
    }

    private static string Where(Landing landing) => landing switch
    {
        Landing.Carriageway => "on a carriageway",
        Landing.Water => "in water",
        Landing.Building => "in a building",
        _ => throw new ArgumentOutOfRangeException(nameof(landing), landing, "Not a fallback landing."),
    };

    // ---------------------------------------------------------------------------------------------------------
    // Open entrances: mat, standing open sign and door lamp on the entrance anchor.
    // ---------------------------------------------------------------------------------------------------------

    private static void Entrances(StreetSite site, List<FurnitureItem> items, Func<FeatureId, Aspects, WpId?> claimedBy)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in site.World.Features.Keys.OrderBy(f => f.Value, StringComparer.Ordinal))
        {
            if (site.World.Interiors.Get(feature) is not { } entry) continue;
            // A zone that builds the doorway and dresses it claims the building's props; its own kit stands there instead.
            if (claimedBy(entry.Feature, Aspects.Props) is { } owner && owner != Self) continue;
            foreach (var door in entry.Entrances.OrderBy(e => e.Id, StringComparer.Ordinal))
            {
                if (!door.Open || !seen.Add(door.Id)) continue;
                var facing = Facing(door.YawDeg);
                var plan = new Vector2(door.Center.X, door.Center.Z);
                // The entrance floor, never below the paving in front of the door (WLD-TERRAIN §4.3: mat and sign on the landing).
                var front = plan + facing * (S.MatDepth * 0.5f + S.MatGap);
                var floor = Frame(plan, facing, MathF.Max(door.Center.Y, site.World.Terrain.HeightAt(front.X, front.Y) + S.PavingLift));
                var key = "door:" + door.Id;
                var matWidth = Math.Clamp(door.ClearWidth + S.MatExtra, S.MatMinWidth, S.MatMaxWidth);
                var mat = new PanelParams(matWidth, S.MatThickness, S.MatDepth);
                items.Add(Item(key + ":mat", FurnitureKind.EntranceMat, PartCatalog.EntranceMat, mat,
                    Xf.Move(0, 0, -(S.MatDepth * 0.5f + S.MatGap)).Then(floor), [], []));
                var sign = S.OpenSign;
                var signAt = Xf.Move(matWidth * 0.5f + S.SignBeside + sign.Width * 0.5f, 0, -S.SignOut).Then(floor);
                items.Add(Item(key + ":sign", FurnitureKind.OpenSign, PartCatalog.OpenSign, sign, signAt, SignDef.Collision(sign), []));
                // The door lamp hangs above head height on the wall, whose collider stands in for it (visual only, like the part).
                var lamp = S.DoorLamp;
                var lampAt = Xf.Move(0, S.DoorLampY, -lamp.Depth * 0.5f).Then(floor);
                items.Add(Item(key + ":lamp", FurnitureKind.EntranceLamp, PartCatalog.EntranceLamp, lamp, lampAt, [],
                    [EntranceLampPart.Emitter(lamp, lampAt)]));
                site.Occupy(front, matWidth * 0.5f + 0.5f);
                site.Occupy(new(signAt.M41, signAt.M43), sign.Width * 0.5f);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Shuttle stops: a sign at the kerb of the carriageway nearest each bus feature (WLD-LIFE §3.2, §4.3).
    // ---------------------------------------------------------------------------------------------------------

    private static void Stops(StreetSite site, List<FurnitureItem> items)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (Text(feature.Props, "subcategory") != "bus") continue;
            var anchor = Anchor(feature);
            if (site.NearestCarriageway(anchor, 45f) is not { } near) continue;
            var normal = new Vector2(-near.Along.Y, near.Along.X);
            var side = Vector2.Dot(anchor - near.Point, normal) >= 0f ? 1f : -1f;
            var offset = StreetSite.HalfWidth(near.Line) + S.StopKerbGap;
            var p = S.Stop;
            foreach (var shift in Shifts(S.LampMaxShift))
            {
                var pos = near.Point + near.Along * shift + normal * (side * offset);
                // The board runs along the kerb: test it as a thin capsule, not a disc of its width.
                var reach = near.Along * (p.Width * 0.5f - StopHalfDepth);
                if (!site.Free(pos - reach, pos + reach, StopHalfDepth) || Land.Point(terrain, pos, PoleRadius) is not { } y) continue;
                var at = Frame(pos, -normal * side, y);
                items.Add(Item("stop:" + feature.Id.Value, FurnitureKind.BusStop, PartCatalog.BusStop, p, at, StopDef.Collision(p), []));
                site.Occupy(pos - reach, pos + reach, StopHalfDepth);
                break;
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Roads: lamps at the tier's spacing; rest stops midway between lamps.
    // ---------------------------------------------------------------------------------------------------------

    private static void Road(StreetSite site, List<FurnitureItem> items, List<string> log, int r, RoadRule rule)
    {
        var lines = site.World.Network.Roads.Where(l => StreetSite.RoadName(l) == rule.Name).ToList();
        if (lines.Count == 0) return;
        var half = lines.Max(StreetSite.HalfWidth);
        var main = rule.Tier == RoadTier.Main;
        var spacing = main ? S.MainLampSpacing : S.SecondaryLampSpacing;
        var lamp = main ? S.MainLamp : S.SecondaryLamp;
        var chains = StreetSite.Chains(lines);
        for (var c = 0; c < chains.Count; c++)
        {
            var run = new StreetSite.Run(chains[c]);
            var n = (int)MathF.Floor(run.Length / spacing);
            if (n < 1) continue;
            var s0 = (run.Length - (n - 1) * spacing) * 0.5f;
            // Grade one lights both sides, opposite each other; grade two one side, the side with more free slots.
            float[] lampSides = main ? [1f, -1f] : [BestSide(site, run, s0, spacing, n, half)];
            foreach (var side in lampSides)
                for (var k = 0; k < n; k++)
                    PlaceLamp(site, items, run, s0 + k * spacing, side, half, lamp, $"lamp:{r}:{c}:{Side(side)}:{Num(k)}");
            // Rest stops: every RestEvery lamp gaps on a side; the avenue's two sides alternate, a secondary road's sit opposite its lamps.
            // A road with rest nodes (Opus review 16:23) has none along its line: its stops are at the nodes.
            float[] restSides = rule.Nodes is null ? main ? [1f, -1f] : [-lampSides[0]] : [];
            foreach (var side in restSides)
            {
                var phase = main && side < 0f ? S.RestEvery / 2 : 0;
                int stops = 0, bins = 0;
                for (var k = 0; k + 1 < n; k++)
                {
                    if ((k + phase) % S.RestEvery != S.RestEvery / 2) continue;
                    var key = $"rest:{r}:{c}:{Side(side)}:{Num(k)}";
                    // A bin at every other stop; one that found no room passes to the next stop.
                    var (bench, bin) = PlaceRest(site, items, run, s0 + (k + 0.5f) * spacing, side, half, withBin: bins * 2 <= stops, key);
                    if (bench) stops++;
                    if (bin) bins++;
                }
            }
        }
        if (rule.Nodes is { } nodes) NodeStops(site, items, log, r, rule.Name, nodes);
    }

    /// <summary>
    /// A road's rest stops at its nodes (Opus review 16:23): at each node, up to <see cref="FurnitureStyle.NodeBenches"/> benches on the
    /// pedestrian paving within <see cref="FurnitureStyle.NodeReach"/> of it (a plaza's edge, a walk's), nearest first, each standing
    /// just off the paving edge and facing the paving under the 13c1 rules (<see cref="PavingSlot"/>), <see cref="FurnitureStyle.NodeBenchGap"/>
    /// apart. A node that takes none says why, in the layout log, and the rules stay as they are.
    /// </summary>
    private static void NodeStops(StreetSite site, List<FurnitureItem> items, List<string> log, int r, string road, IReadOnlyList<RestNode> nodes)
    {
        for (var n = 0; n < nodes.Count; n++)
        {
            var node = nodes[n];
            var placed = new List<Vector2>();
            var why = new SortedDictionary<string, int>(StringComparer.Ordinal);
            void Seat(WalkFit bench)
            {
                items.Add(Placed($"node:{r}:{Num(n)}:{Num(placed.Count)}:bench", FurnitureKind.Bench, PartCatalog.Bench, S.Bench, bench, BenchDef.Collision(S.Bench)));
                site.Occupy(bench.Ends.A, bench.Ends.B, bench.Ends.R);
                placed.Add(bench.Centre);
            }
            // On the plaza first, its back to the plaza's outer edge, facing in (Opus review 17:28, 13c2b rework): the plaza's edges
            // within NodePlazaReach, nearest first.
            var plaza = site.PavingEdgesNear(node.At, S.NodePlazaReach, S.NodeStep).Where(e => e.Road is null).ToList();
            foreach (var edge in plaza)
            {
                if (placed.Count >= S.NodeBenches) break;
                var (fit, reason) = PlazaSlot(site, edge.Edge, edge.Inward, S.Bench);
                if (fit is not { } bench)
                {
                    why["on the plaza: " + reason!] = why.GetValueOrDefault("on the plaza: " + reason!) + 1;
                    continue;
                }
                if (placed.Any(q => Vector2.Distance(q, bench.Centre) < S.NodeBenchGap)) continue;
                Seat(bench);
            }
            // Then at the edge of a walk 4 m or wider, its back to the edge (13c2b 21:05); else off the paving, facing it (13c1).
            var candidates = site.PavingEdgesNear(node.At, S.NodeReach, S.NodeStep);
            foreach (var edge in candidates)
            {
                if (placed.Count >= S.NodeBenches) break;
                var (onWalk, walkWhy) = WalkEdgeSlot(site, edge.Edge, edge.Inward, edge.Road, S.Bench);
                var (beside, besideWhy) = onWalk is null ? PavingSlot(site, edge.Edge, edge.Inward, edge.Road, S.Bench, facesWalk: true) : (null, null);
                if ((onWalk ?? beside) is not { } bench)
                {
                    why["on the walk: " + walkWhy!] = why.GetValueOrDefault("on the walk: " + walkWhy!) + 1;
                    why["beside it: " + besideWhy!] = why.GetValueOrDefault("beside it: " + besideWhy!) + 1;
                    continue;
                }
                if (placed.Any(q => Vector2.Distance(q, bench.Centre) < S.NodeBenchGap)) continue;
                Seat(bench);
            }
            if (placed.Count == 0)
                log.Add($"road {road} node {node.Label} has no bench: {plaza.Count} plaza edge spots within {Num(S.NodePlazaReach)} m and {candidates.Count} paving edge spots within {Num(S.NodeReach)} m, " +
                    (why.Count == 0 ? "none of them on pedestrian paving" : string.Join(", ", why.Select(w => $"{w.Value} {w.Key}"))));
        }
    }

    private static float BestSide(StreetSite site, StreetSite.Run run, float s0, float spacing, int n, float half)
    {
        int Count(float side)
        {
            var free = 0;
            for (var k = 0; k < n; k++)
                if (run.Normal(s0 + k * spacing) is { } normal && site.Free(run.At(s0 + k * spacing) + normal * (side * (half + S.LampKerbGap)), PoleRadius)) free++;
            return free;
        }
        return Count(-1f) > Count(1f) ? -1f : 1f;
    }

    private static void PlaceLamp(StreetSite site, List<FurnitureItem> items, StreetSite.Run run, float s, float side, float half, LampParams p, string key)
    {
        var terrain = site.World.Terrain;
        foreach (var shift in Shifts(S.LampMaxShift))
        {
            var at = s + shift;
            if (at < 0f || at > run.Length || run.Normal(at) is not { } normal) continue;
            var pos = run.At(at) + normal * (side * (half + S.LampKerbGap));
            if (!site.Free(pos, PoleRadius) || Land.Point(terrain, pos, PoleRadius) is not { } y) continue;
            // The front (−Z) faces the road, so a single-arm head reaches over the carriageway.
            var frame = Frame(pos, -normal * side, y);
            items.Add(Item(key, FurnitureKind.StreetLamp, PartCatalog.StreetLamp, p, frame, Lamp.Collision(p), [StreetLampPart.Emitter(p, frame)]));
            site.Occupy(pos, PoleRadius);
            return;
        }
    }

    /// <summary>
    /// The rest stop at arc length <paramref name="s"/> of the road, on <paramref name="side"/>: a bench lining the nearest walk on that
    /// side (within <see cref="FurnitureStyle.RestReach"/> of the centre line, sliding along it by up to <see cref="FurnitureStyle.RestSlide"/>),
    /// its front <see cref="FurnitureStyle.WalkGap"/> off the walk's edge and facing it, on a bay joined to the walk. The walk's road
    /// side is tried first (back to the trees and the road, looking away from it); the far side only takes a bench where no
    /// carriageway lies <see cref="FurnitureStyle.FaceClear"/> ahead. With <paramref name="withBin"/>, a bin on its own bay along the
    /// same edge, <see cref="FurnitureStyle.BinGaps"/> clear of the bench, on the side nearer another walk (a junction).
    /// </summary>
    private static (bool Bench, bool Bin) PlaceRest(StreetSite site, List<FurnitureItem> items, StreetSite.Run run, float s, float side, float half, bool withBin, string key)
    {
        if (run.Normal(s) is not { } normal) return (false, false);
        var road = run.At(s);
        var outward = normal * side;
        foreach (var walk in site.Walks(road, S.RestReach))
        {
            // The walk must lie on this side, beyond the carriageway, and stay wide enough to pass a sitter's feet.
            var whalf = StreetSite.HalfWidth(walk.Line);
            if (Vector2.Dot(walk.Point - road, outward) <= half || 2f * whalf - S.FeetReach < S.PassClear) continue;
            var path = new StreetSite.Run([.. walk.Line.Line.Points]);
            var ends = S.Bench.Width * 0.5f + S.ItemGap;
            foreach (var shift in Shifts(S.RestSlide))
            {
                var at = walk.Along + shift;
                if (at < ends || at > path.Length - ends || path.Normal(at) is not { } n) continue;
                var roadSide = Vector2.Dot(n, outward) < 0f ? 1f : -1f;
                foreach (var bside in new[] { roadSide, -roadSide })
                {
                    if (WalkSlot(site, path, at, bside, walk.Line, S.Bench, facesWalk: true) is not { } bench) continue;
                    items.Add(Placed(key + ":bench", FurnitureKind.Bench, PartCatalog.Bench, S.Bench, bench, BenchDef.Collision(S.Bench)));
                    site.Occupy(bench.Ends.A, bench.Ends.B, bench.Ends.R);
                    return (true, withBin && PlaceBin(site, items, path, at, bside, walk.Line, key + ":bin"));
                }
            }
        }
        return (false, false);
    }

    /// <summary>A bin along the same walk edge as the bench at <paramref name="benchAt"/>, 2–4 m clear of it, nearer side to a junction first.</summary>
    private static bool PlaceBin(StreetSite site, List<FurnitureItem> items, StreetSite.Run path, float benchAt, float bside, NetworkLine walk, string key)
    {
        var centre = S.Bench.Width * 0.5f + S.Bin.Width * 0.5f;
        float Junction(float sign) => site.OtherWalk(path.At(benchAt + sign * (centre + S.BinGaps[0])), walk);
        var first = Junction(-1f) < Junction(1f) ? -1f : 1f;
        foreach (var sign in new[] { first, -first })
            foreach (var gap in S.BinGaps)
            {
                var at = benchAt + sign * (centre + gap);
                if (at < S.Bin.Width * 0.5f || at > path.Length - S.Bin.Width * 0.5f) continue;
                if (WalkSlot(site, path, at, bside, walk, S.Bin, facesWalk: false) is not { } bin) continue;
                items.Add(Placed(key, FurnitureKind.Bin, PartCatalog.BinFour, S.Bin, bin, BinDef.Collision(S.Bin)));
                site.Occupy(bin.Ends.A, bin.Ends.B, bin.Ends.R);
                return true;
            }
        return false;
    }

    /// <summary>
    /// A small entity standing at a walk's edge, facing the walk, with the bay that joins it to the walk (or on a plaza, on the paving
    /// itself: no bay where the paving is level enough), and the height it stands at.
    /// </summary>
    private readonly record struct WalkFit(Vector2 Centre, Vector2 Facing, (Vector2 A, Vector2 B, float R) Ends, Pad? Bay, float Top);

    /// <summary>
    /// A small entity at arc length <paramref name="at"/> of a walk, on side <paramref name="bside"/> of it (along the walk's left
    /// normal), its front <see cref="FurnitureStyle.WalkGap"/> off the walk's edge and facing the walk, if it fits: free of
    /// outlines, roads (only off this walk's surface), trees, the planted rows and placed things; <see cref="FurnitureStyle.RoadSetback"/>
    /// off carriageways; a bench (<paramref name="facesWalk"/>) with no carriageway <see cref="FurnitureStyle.FaceClear"/> ahead.
    /// Its bay reaches <see cref="FurnitureStyle.PadJoin"/> over the walk's edge and <see cref="FurnitureStyle.BayBehind"/> behind it.
    /// </summary>
    private static WalkFit? WalkSlot(StreetSite site, StreetSite.Run path, float at, float bside, NetworkLine walk, FurnitureParams p, bool facesWalk)
    {
        if (path.Normal(at) is not { } n) return null;
        var whalf = StreetSite.HalfWidth(walk);
        var away = n * bside;
        var centre = path.At(at) + away * (whalf + S.WalkGap + p.Depth * 0.5f);
        return Slot(site, centre, -away, p, facesWalk, walk).Fit;
    }

    /// <summary>
    /// <see cref="WalkSlot"/> at a spot on the edge of any pedestrian paving (a plaza polygon, a footway's strip): the entity stands
    /// just off <paramref name="edge"/>, <paramref name="inward"/> being the unit normal into the paving, and faces it
    /// (<paramref name="walk"/>: the network walk the paving is, which the entity only has to stay off the surface of). A refusal
    /// names the rule that refused it.
    /// </summary>
    private static (WalkFit? Fit, string? Reason) PavingSlot(StreetSite site, Vector2 edge, Vector2 inward, NetworkLine? walk, BoxPartParams p, bool facesWalk) =>
        Slot(site, edge - inward * (S.WalkGap + p.Depth * 0.5f), inward, p, facesWalk, walk);

    /// <summary>
    /// The entity's slot at <paramref name="centre"/> facing <paramref name="facing"/> (towards the walk or paving), if it fits: free
    /// of outlines, roads (<paramref name="beside"/> only off its surface), trees, the planted rows and placed things;
    /// <see cref="FurnitureStyle.RoadSetback"/> off carriageways; a bench (<paramref name="facesWalk"/>) with no carriageway
    /// <see cref="FurnitureStyle.FaceClear"/> ahead. Its bay reaches <see cref="FurnitureStyle.PadJoin"/> over the walk's edge and
    /// <see cref="FurnitureStyle.BayBehind"/> behind it.
    /// </summary>
    private static (WalkFit? Fit, string? Reason) Slot(StreetSite site, Vector2 centre, Vector2 facing, BoxPartParams p, bool facesWalk, NetworkLine? beside)
    {
        var tangent = new Vector2(facing.Y, -facing.X);
        var reach = MathF.Max(0f, p.Width * 0.5f - p.Depth * 0.5f);
        var ends = (centre - tangent * reach, centre + tangent * reach, p.Depth * 0.5f + 0.05f);
        if (site.Blocker(ends.Item1, ends.Item2, ends.Item3, beside) is { } blocker) return (null, $"blocked by {blocker}");
        foreach (var q in new[] { ends.Item1, centre, ends.Item2 })
        {
            if (!site.OffRows(q, ends.Item3)) return (null, "on a planted tree row");
            if (!site.OffCarriageways(q, ends.Item3, S.RoadSetback)) return (null, $"closer than {Num(S.RoadSetback)} m to a carriageway");
        }
        if (facesWalk && site.CarriagewayAhead(centre + facing * (p.Depth * 0.5f), facing, S.FaceClear)) return (null, $"facing a carriageway within {Num(S.FaceClear)} m");
        // The bay, in the entity's frame (−Z towards the walk): from just over the walk's edge to behind the entity.
        var front = p.Depth * 0.5f + S.WalkGap + S.PadJoin;
        var back = p.Depth * 0.5f + S.BayBehind;
        var middle = centre + facing * ((front - back) * 0.5f);
        if (Land.Pad(site.World.Terrain, Frame(middle, facing, 0f), p.Width * 0.5f + S.BayEnds, (front + back) * 0.5f) is not { } bay) return (null, "no ground for its bay");
        return (new WalkFit(centre, facing, ends, bay, bay.Top), null);
    }

    /// <summary>
    /// A bench standing on a plaza's paving (Opus review 17:28), the way a kiosk does: its back <see cref="FurnitureStyle.PlazaBenchClear"/>
    /// inside the paving edge at <paramref name="edge"/> and parallel to it, facing into the paving (<paramref name="inward"/>). Its footprint
    /// and its sitters' feet are wholly on paving and off every walk's strip (benches leave the walk to walkers), and the walk benches'
    /// rules hold: free of outlines, trees, rows and placed things, <see cref="FurnitureStyle.RoadSetback"/> off carriageways, no carriageway
    /// <see cref="FurnitureStyle.FaceClear"/> ahead. It stands on the paving, a plinth only where the paving is steeper than 5%.
    /// </summary>
    private static (WalkFit? Fit, string? Reason) PlazaSlot(StreetSite site, Vector2 edge, Vector2 inward, FurnitureParams p)
    {
        var centre = edge + inward * (S.PlazaBenchClear + p.Depth * 0.5f);
        var tangent = new Vector2(inward.Y, -inward.X);
        var reach = MathF.Max(0f, p.Width * 0.5f - p.Depth * 0.5f);
        var ends = (centre - tangent * reach, centre + tangent * reach, p.Depth * 0.5f + 0.05f);
        // The footprint and the sitters' feet ahead of it.
        var feet = centre + inward * (S.FeetReach * 0.5f);
        if (!OnPaving(site, feet, inward, p.Width * 0.5f, (p.Depth + S.FeetReach) * 0.5f)) return (null, "not whole on the plaza's paving");
        // On a plaza's own paving, never on a walk's strip outside it (walks belong to walkers; a strip's round end is lawn where WP-15
        // cuts it square). A walk line the plaza carries does not count against the bench (Opus review 13c2b rework), but the bench
        // never sits across the line itself.
        for (var u = -1; u <= 1; u++)
            for (var v = -1; v <= 1; v++)
                if (!site.OnPlaza(feet + tangent * (u * p.Width * 0.5f) + inward * (v * (p.Depth + S.FeetReach) * 0.5f))) return (null, "on a walk, not a plaza");
        if (site.NearWalkLine([ends.Item1, centre, ends.Item2], ends.Item3 + S.PlazaWalkLineClear)) return (null, "across a walk line");
        if (site.Blocker(ends.Item1, ends.Item2, ends.Item3, outlineClear: S.PlazaWallClear, walksPass: true) is { } blocker) return (null, $"blocked by {blocker}");
        // At least PlazaPass of free paving ahead of the sitters' feet: people pass in front.
        for (var d = 0.5f; d <= S.PlazaPass + 1e-3f; d += 0.5f)
            for (var u = -1; u <= 1; u++)
            {
                var q = centre + inward * (p.Depth * 0.5f + S.FeetReach + d) + tangent * (u * p.Width * 0.5f);
                if (!site.OnPavedGround(q)) return (null, $"less than {Num(S.PlazaPass)} m of paving in front");
                if (site.Blocker(q, 0f, walksPass: true) is { } ahead) return (null, $"{ahead} within {Num(S.PlazaPass)} m in front");
            }
        foreach (var q in new[] { ends.Item1, centre, ends.Item2 })
        {
            if (!site.OffRows(q, ends.Item3)) return (null, "on a planted tree row");
            if (!site.OffCarriageways(q, ends.Item3, S.RoadSetback)) return (null, $"closer than {Num(S.RoadSetback)} m to a carriageway");
        }
        if (site.CarriagewayAhead(centre + inward * (p.Depth * 0.5f), inward, S.FaceClear)) return (null, $"facing a carriageway within {Num(S.FaceClear)} m");
        if (Land.Pad(site.World.Terrain, Frame(centre, inward, 0f), p.Width * 0.5f, p.Depth * 0.5f) is not { } ground) return (null, "no ground for its feet");
        // Not against a bank: the ground round it (the back included) keeps within PlazaBankRise of the paving it stands on.
        var terrain = site.World.Terrain;
        var paving = ground.Top - S.PavingLift - S.PadProud;
        for (var i = -1; i <= 1; i++)
            for (var k = -1; k <= 1; k++)
            {
                var q = centre + tangent * (i * (p.Width * 0.5f + S.PlazaBankReach)) + inward * (k * (p.Depth * 0.5f + S.PlazaBankReach));
                if (terrain.HeightAt(q.X, q.Y) > paving + S.PlazaBankRise) return (null, "against a bank");
            }
        return ground.Plinth ? (new WalkFit(centre, inward, ends, ground, ground.Top), null) : (new WalkFit(centre, inward, ends, null, ground.Top - S.PadProud), null);
    }

    /// <summary>
    /// A node bench standing on a walk at least <see cref="FurnitureStyle.NodeWalkMin"/> wide (Opus review 13c2b 21:05): its back
    /// <see cref="FurnitureStyle.WalkBenchClear"/> inside the walk's edge at <paramref name="edge"/> and parallel to it, facing across the
    /// walk (<paramref name="inward"/>), with at least <see cref="FurnitureStyle.NodeWalkPass"/> of the walk clear beyond its sitters' feet.
    /// Its footprint and the feet are on paved ground; the walk benches' rules hold (free of outlines, other roads, trees, rows and placed
    /// things, <see cref="FurnitureStyle.RoadSetback"/> off carriageways, none <see cref="FurnitureStyle.FaceClear"/> ahead). It stands on
    /// the paving, a plinth only where the paving is steeper than 5%.
    /// </summary>
    private static (WalkFit? Fit, string? Reason) WalkEdgeSlot(StreetSite site, Vector2 edge, Vector2 inward, NetworkLine? walk, FurnitureParams p)
    {
        if (walk is null) return (null, "not a walk's edge");
        var width = 2f * StreetSite.HalfWidth(walk);
        if (width < S.NodeWalkMin) return (null, $"on a walk {Fixed(width)} m wide, under {Num(S.NodeWalkMin)} m");
        if (width - (S.WalkBenchClear + p.Depth + S.FeetReach) < S.NodeWalkPass) return (null, $"less than {Num(S.NodeWalkPass)} m left to pass");
        var centre = edge + inward * (S.WalkBenchClear + p.Depth * 0.5f);
        var tangent = new Vector2(inward.Y, -inward.X);
        var reach = MathF.Max(0f, p.Width * 0.5f - p.Depth * 0.5f);
        var ends = (centre - tangent * reach, centre + tangent * reach, p.Depth * 0.5f + 0.05f);
        var feet = centre + inward * (S.FeetReach * 0.5f);
        for (var u = -1; u <= 1; u++)
            for (var v = -1; v <= 1; v++)
                if (!site.OnPavedGround(feet + tangent * (u * p.Width * 0.5f) + inward * (v * (p.Depth + S.FeetReach) * 0.5f))) return (null, "not whole on paved ground");
        if (site.Blocker(ends.Item1, ends.Item2, ends.Item3, walk, onBeside: true) is { } blocker) return (null, $"blocked by {blocker}");
        foreach (var q in new[] { ends.Item1, centre, ends.Item2 })
        {
            if (!site.OffRows(q, ends.Item3)) return (null, "on a planted tree row");
            if (!site.OffCarriageways(q, ends.Item3, S.RoadSetback)) return (null, $"closer than {Num(S.RoadSetback)} m to a carriageway");
        }
        if (site.CarriagewayAhead(centre + inward * (p.Depth * 0.5f), inward, S.FaceClear)) return (null, $"facing a carriageway within {Num(S.FaceClear)} m");
        if (Land.Pad(site.World.Terrain, Frame(centre, inward, 0f), p.Width * 0.5f, p.Depth * 0.5f) is not { } ground) return (null, "no ground for its feet");
        var terrain = site.World.Terrain;
        var paving = ground.Top - S.PavingLift - S.PadProud;
        for (var i = -1; i <= 1; i++)
            for (var k = -1; k <= 1; k++)
            {
                var q = centre + tangent * (i * (p.Width * 0.5f + S.PlazaBankReach)) + inward * (k * (p.Depth * 0.5f + S.PlazaBankReach));
                if (terrain.HeightAt(q.X, q.Y) > paving + S.PlazaBankRise) return (null, "against a bank");
            }
        return ground.Plinth ? (new WalkFit(centre, inward, ends, ground, ground.Top), null) : (new WalkFit(centre, inward, ends, null, ground.Top - S.PadProud), null);
    }

    private static FurnitureItem Placed(string key, FurnitureKind kind, string part, BoxPartParams p, WalkFit fit, IReadOnlyList<Collider> local)
    {
        var item = Item(key, kind, part, p, Frame(fit.Centre, fit.Facing, fit.Top), local, []);
        return fit.Bay is { } bay ? item with { Pad = bay, Colliders = [.. item.Colliders, bay.Collider()] } : item;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Common.
    // ---------------------------------------------------------------------------------------------------------

    private static FurnitureItem Item(string key, FurnitureKind kind, string part, BoxPartParams p, Matrix4x4 at, IReadOnlyList<Collider> local, IReadOnlyList<LightEmitter> emitters) =>
        new(key, kind, new PlacedPart(new(part), p, at)) { Emitters = emitters, Colliders = local.Select(c => Placed(c, at)).ToArray() };

    /// <summary>
    /// Rigid frame whose local −Z (every part's front) points along <paramref name="facing"/> and whose origin is (p.x, y, p.z).
    /// Rows are the images of local X, Y, Z (System.Numerics row vectors).
    /// </summary>
    public static Matrix4x4 Frame(Vector2 p, Vector2 facing, float y)
    {
        var f = Vector2.Normalize(facing);
        return new(-f.Y, 0, f.X, 0, 0, 1, 0, 0, -f.X, 0, -f.Y, 0, p.X, y, p.Y, 1);
    }

    /// <summary>Plan direction of a yaw in degrees: 0 = north (−Z), 90 = east (+X), the convention of anchors and entrances.</summary>
    public static Vector2 Facing(float yawDeg)
    {
        var a = yawDeg * (float.Pi / 180f);
        return new(DetMath.Sin(a), -DetMath.Cos(a));
    }

    /// <summary>Moves a part-local collider by a rigid placement matrix.</summary>
    public static Collider Placed(Collider collider, Matrix4x4 at) => collider switch
    {
        BoxCollider box => box with { At = box.At.Then(at) },
        PostCollider post => post with
        {
            C = new(post.C.X * at.M11 + post.C.Y * at.M31 + at.M41, post.C.X * at.M13 + post.C.Y * at.M33 + at.M43),
            Y0 = post.Y0 + at.M42,
            Y1 = post.Y1 + at.M42,
        },
        RampCollider ramp => ramp with { P0 = Xf.Point(at, ramp.P0), P1 = Xf.Point(at, ramp.P1), P2 = Xf.Point(at, ramp.P2), P3 = Xf.Point(at, ramp.P3) },
        _ => throw new ArgumentException("Unsupported collider kind for a placed street part.", nameof(collider)),
    };

    /// <summary>0, +step, −step, +2·step, … up to ±<paramref name="max"/>.</summary>
    private static IEnumerable<float> Shifts(float max)
    {
        yield return 0f;
        for (var d = S.ShiftStep; d <= max + 1e-3f; d += S.ShiftStep)
        {
            yield return d;
            yield return -d;
        }
    }

    private static float Extent(IReadOnlyList<Vector2> ring, Vector2 axis)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var p in ring)
        {
            var d = Vector2.Dot(p, axis);
            min = MathF.Min(min, d);
            max = MathF.Max(max, d);
        }
        return max - min;
    }

    private static Vector2 Anchor(WorldFeature feature)
    {
        if (feature.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("anchor", out var a) &&
            a.ValueKind == JsonValueKind.Array && a.GetArrayLength() >= 3)
            return new(a[0].GetSingle(), a[2].GetSingle());
        return new((feature.Bounds.Min.X + feature.Bounds.Max.X) * 0.5f, (feature.Bounds.Min.Z + feature.Bounds.Max.Z) * 0.5f);
    }

    private static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static float? Number(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : null;

    /// <summary>The yaw in degrees (0 = north, 90 = east, [0, 360)) of a plan direction: the inverse of <see cref="Facing"/>.</summary>
    private static float YawDeg(Vector2 facing)
    {
        var deg = DetMath.Atan2(facing.X, -facing.Y) * (180f / float.Pi);
        return deg < 0f ? deg + 360f : deg;
    }

    private static string Fixed(float v) => v.ToString("F2", CultureInfo.InvariantCulture);

    private static string Side(float side) => side < 0f ? "r" : "l";

    private static string Num(int k) => k.ToString(CultureInfo.InvariantCulture);

    private static string Num(float v) => v.ToString(CultureInfo.InvariantCulture);
}
