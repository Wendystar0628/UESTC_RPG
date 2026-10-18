using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

/// <summary>
/// 13c2c: vending groups, delivery lockers and shared-bicycle bays where people use them (WLD-LIFE): a few clusters at the
/// dormitory cluster gates, the canteens, the sports venues and the doors of teaching buildings, libraries and canteens, blank in between
/// (ART-AMBIENCE rule 4; "no piling up"). Every rule and number is in <see cref="FurnitureStyle"/>.
/// </summary>
public static partial class FurniturePlanner
{
    private static readonly VendingMachinePart VendingDef = new();
    private static readonly DeliveryLockerPart LockerDef = new();
    private static readonly BicycleBayPart BayDef = new();

    /// <summary>A place a service thing serves: its point, a label for keys and the diagnostics, and the outline it must stay outside, if any.</summary>
    private readonly record struct ServiceSite(string Label, Vector2 At, PolygonRings? Outside, Vector2? Front, bool Open = false);

    private static void Services(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, IReadOnlyList<RoadGroup> roads)
    {
        var world = site.World;
        var cache = new Dictionary<(string, object), (int Meshes, int Triangles)>();
        var axis = roads.Where(r => r.Tier == RoadTier.Avenue).SelectMany(r => r.Lines).ToList();
        var gates = Gates(world);
        // Vending groups: the gates first, then the canteens and the sports venues; one to a line of sight, none on the main axis.
        var vendingSites = gates.Concat(Buildings(site, S.VendingCategories, door: false)).ToList();
        var groups = new List<ServiceSite>();
        int axial = 0, crowded = 0, cramped = 0, heavy = 0, machines = 0, plain = 0;
        var lockerSpots = new List<ServiceSite>();
        foreach (var spot in vendingSites)
        {
            if (axis.Any(l => MathF.Sqrt(PlanLines.PolylineDistanceSquared(l.Line.Points, spot.At)) < S.MainAxisClear)) { axial++; continue; }
            if (groups.Any(g => Vector2.Distance(g.At, spot.At) < S.VendingSpacing)) { crowded++; continue; }
            var hash = StableHash.Hash32(0x13C2Cu, "vending", spot.Label);
            var count = S.VendingMin + (int)(hash % (uint)(S.VendingMax - S.VendingMin + 1));
            var width = count * S.Vending.Width + (count - 1) * S.VendingSideGap;
            if (ServiceFit(site, spot, width, S.Vending.Depth, S.ServiceFront, S.ServiceReach) is not { } fit) { cramped++; continue; }
            // The first closed gates' groups hold a locker beside them (the open gate keeps its room for a bay) and keep to one machine
            // look (a cell's draw calls); of the others every other group mixes in its one odd machine.
            var withLocker = spot.Outside is not null && !spot.Open && lockerSpots.Count < S.LockerSites;
            var odd = !withLocker && plain % 2 == 1 ? (int)((hash >> 8) % (uint)count) : -1;
            var tangent = new Vector2(fit.Facing.Y, -fit.Facing.X);
            var group = new List<FurnitureItem>();
            for (var k = 0; k < count; k++)
            {
                var p = S.Vending with { Variation = k == odd ? 1 : 0 };
                var centre = fit.Centre + tangent * ((k - (count - 1) * 0.5f) * (S.Vending.Width + S.VendingSideGap));
                var item = Item($"vending:{spot.Label}:{Num(k)}", FurnitureKind.VendingMachine, PartCatalog.VendingMachine, p, Frame(centre, fit.Facing, fit.Top), VendingDef.Collision(p), []);
                // The group's one bay rides on its first machine.
                group.Add(k == 0 && fit.Bay is { } bay ? item with { Pad = bay, Colliders = [.. item.Colliders, bay.Collider()] } : item);
            }
            if (!WithinBudget(items, group, cache)) { heavy++; continue; }
            items.AddRange(group);
            machines += count;
            if (!withLocker) plain++;
            site.Occupy(fit.Ends.A, fit.Ends.B, fit.Ends.R);
            groups.Add(spot);
            if (withLocker) lockerSpots.Add(spot);
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.vending.summary", $"vending: {machines} machines in {groups.Count} groups at {string.Join(", ", groups.Select(g => g.Label))} " +
            $"({axial} sites on the main axis, {crowded} within {Num(S.VendingSpacing)} m of a group, {cramped} with no room, {heavy} over a cell's budget, left out)"));

        // Delivery lockers: beside the first gates that hold a vending group (their WLD-LIFE places wait for WP-30's gates).
        var lockers = new List<string>();
        foreach (var spot in lockerSpots)
        {
            if (ServiceFit(site, spot, S.Locker.Width, S.Locker.Depth, S.ServiceFront, S.ServiceReach) is not { } fit) continue;
            var locker = Placed($"locker:{spot.Label}", FurnitureKind.DeliveryLocker, PartCatalog.DeliveryLocker, S.Locker, fit, LockerDef.Collision(S.Locker));
            if (!WithinBudget(items, [locker], cache)) continue;
            items.Add(locker);
            site.Occupy(fit.Ends.A, fit.Ends.B, fit.Ends.R);
            lockers.Add(spot.Label);
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.lockers.summary", $"delivery lockers: {lockers.Count} at {string.Join(", ", lockers)}"));

        // Shared-bicycle bays: the open cluster gates, the doors of teaching buildings, libraries and canteens, then the places the map
        // marks for parking bicycles (OSM 非机动车 points).
        var parking = world.Features.Values.Where(f => S.BaySubcategories.Contains(WorldProps.TryText(f.Props, "subcategory") ?? "")).OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Select(f => new ServiceSite(f.Id.Value, Anchor(f), null, null));
        var baySites = gates.Where(g => g.Open).Concat(Buildings(site, S.BayCategories, door: true)).Concat(parking).ToList();
        var bays = new List<string>();
        var placedAt = new List<Vector2>();
        var bikes = 0;
        heavy = 0;
        foreach (var spot in baySites)
        {
            if (placedAt.Any(b => Vector2.Distance(b, spot.At) < S.BaySpacing)) continue;
            var p = S.BikeBay;
            if (ServiceFit(site, spot, p.Width, p.Depth, S.WalkGap, spot.Front is null ? S.ServiceReach : S.BayFar) is not { } fit) continue;
            var bay = new List<FurnitureItem> { Placed($"bay:{spot.Label}", FurnitureKind.BicycleBay, PartCatalog.BicycleShelter, p, fit, BayDef.Collision(p)) };
            // Stand-in stock until WP-55 mounts the rentable bikes: every other slot, front wheel to the back line.
            var hash = StableHash.Hash32(0x13C2Cu, "bay", spot.Label);
            var count = S.BayBikesMax <= 0 ? 0 : S.BayBikesMin + (int)(hash % (uint)(S.BayBikesMax - S.BayBikesMin + 1));
            var tangent = new Vector2(fit.Facing.Y, -fit.Facing.X);
            var slots = (int)MathF.Floor(p.Width / S.BayBikePitch);
            var first = (int)((hash >> 8) % (uint)Math.Max(1, slots - count + 1));
            for (var k = 0; k < Math.Min(count, slots); k++)
            {
                var centre = fit.Centre + tangent * ((first + k - (slots - 1) * 0.5f) * S.BayBikePitch);
                bay.Add(Item($"bay:{spot.Label}:bike:{Num(k)}", FurnitureKind.ParkedBicycle, PartCatalog.ParkedBicycle, S.Bike, Frame(centre, -fit.Facing, fit.Top), [], []));
            }
            // Over the cell's budget with its stand-in bikes, an open gate's bay stands empty for WP-55's stock; any other is left out.
            if (!WithinBudget(items, bay, cache))
            {
                if (!spot.Open) { heavy++; continue; }
                bay.RemoveRange(1, bay.Count - 1);
                if (!WithinBudget(items, bay, cache)) continue;
            }
            items.AddRange(bay);
            bikes += bay.Count - 1;
            site.Occupy(fit.Ends.A, fit.Ends.B, fit.Ends.R);
            placedAt.Add(spot.At);
            bays.Add(spot.Label);
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.bicycles.summary", $"bicycle bays: {bays.Count} with {bikes} stand-in bikes at {string.Join(", ", bays)} ({heavy} over a cell's budget, left out)"));
    }
    /// <summary>The dormitory cluster gates (WP-20's <c>clusterGate</c> features), the open ones first, then by ID; each with its cluster's outline.</summary>
    private static List<ServiceSite> Gates(IWorldModel world)
    {
        var gates = new List<(bool Open, ServiceSite Site)>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "clusterGate").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PointGeometry point) continue;
            var open = feature.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("open", out var o) && o.ValueKind == JsonValueKind.True;
            PolygonRings? outline = null;
            if (WorldProps.TryText(feature.Props, "cluster") is { } id && world.Features.TryGetValue(new(id), out var cluster))
                outline = cluster.Geometry switch { PolygonGeometry polygon => polygon.Rings, MultiPolygonGeometry { Polygons.Count: > 0 } multi => multi.Polygons[0], _ => null };
            if (outline is null) continue;
            gates.Add((open, new(feature.Id.Value, new(point.Position.X, point.Position.Z), outline, null, open)));
        }
        return gates.OrderBy(g => g.Open ? 0 : 1).Select(g => g.Site).ToList();
    }

    /// <summary>
    /// The buildings of <paramref name="categories"/> in ID order (one site a building): with <paramref name="door"/>, only those with an
    /// open entrance (the first by ID), the site in front of it; otherwise the first open entrance or, without one, the outline point nearest
    /// the network (the side the building is reached from).
    /// </summary>
    private static IEnumerable<ServiceSite> Buildings(StreetSite site, IReadOnlyList<string> categories, bool door)
    {
        var world = site.World;
        var done = new HashSet<FeatureId>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "building").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (!categories.Contains(WorldProps.TryText(feature.Props, "category") ?? "")) continue;
            var entry = world.Interiors.Get(feature.Id);
            if (entry is not null && !done.Add(entry.Feature)) continue;
            var open = entry?.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault();
            if (open is not null)
            {
                var plan = new Vector2(open.Center.X, open.Center.Z);
                var facing = Angles.Heading(open.YawDeg);
                yield return new(feature.Id.Value, plan + facing * (door ? (S.BayNear + S.BayFar) * 0.5f : S.BayNear), null, door ? plan : null);
                continue;
            }
            if (door || Frontage(site, feature) is not { } front) continue;
            yield return new(feature.Id.Value, front, null, null);
        }
    }

    /// <summary>The point of a building's outline (every 2 m) nearest a carriageway or a strollable walk, within 30 m; null when none is.</summary>
    private static Vector2? Frontage(StreetSite site, WorldFeature feature)
    {
        IReadOnlyList<PolygonRings> polygons = feature.Geometry switch { PolygonGeometry p => [p.Rings], MultiPolygonGeometry m => m.Polygons, _ => [] };
        if (polygons.Count == 0) return null;
        var min = new Vector2(feature.Bounds.Min.X, feature.Bounds.Min.Z) - new Vector2(30f);
        var max = new Vector2(feature.Bounds.Max.X, feature.Bounds.Max.Z) + new Vector2(30f);
        var lines = site.World.Network.Roads.Where(l => (StreetSite.Carriageway(l) || StreetSite.Strollable(l)) &&
            l.Line.Points.Any(q => q.X >= min.X && q.X <= max.X && q.Y >= min.Y && q.Y <= max.Y)).OrderBy(l => l.Id.Value, StringComparer.Ordinal).ToList();
        (float D, Vector2 P)? best = null;
        foreach (var rings in polygons)
        {
            var ring = rings.Outer;
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                var steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(a, b) / 2f));
                for (var k = 0; k < steps; k++)
                {
                    var q = Vector2.Lerp(a, b, (k + 0.5f) / steps);
                    foreach (var line in lines)
                    {
                        var d = MathF.Sqrt(PlanLines.PolylineDistanceSquared(line.Line.Points, q)) - StreetSite.HalfWidth(line);
                        if (d <= 30f && (best is null || d < best.Value.D)) best = (d, q);
                    }
                }
            }
        }
        return best?.P;
    }

    /// <summary>
    /// A row of things <paramref name="width"/> long and <paramref name="depth"/> deep serving <paramref name="spot"/>: off the edge of the
    /// pedestrian paving or carriageway nearest the site (within <paramref name="reach"/>), facing it with <paramref name="front"/> of its bay in
    /// front, its back away from it; free of outlines, other roads, trees, the planted rows and placed things, outside the site's outline
    /// (a cluster's) by <see cref="FurnitureStyle.GateOutside"/> and <see cref="FurnitureStyle.GateKeep"/> off its point; for a door
    /// (<see cref="ServiceSite.Front"/>), <see cref="FurnitureStyle.BayNear"/> or more out and in front of it.
    /// </summary>
    private static WalkFit? ServiceFit(StreetSite site, ServiceSite spot, float width, float depth, float front, float reach)
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
                        if (PlanGeometry.Contains(outline, q) || PlanGeometry.DistanceToRing(q, outline.Outer) < S.GateOutside) outside = false;
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
        foreach (var (edge, inward, road) in site.PavingEdgesNear(p, reach, 1f)) found.Add((Vector2.Distance(edge, p), edge, inward, road));
        foreach (var line in site.World.Network.Roads.Where(StreetSite.Carriageway).OrderBy(l => l.Id.Value, StringComparer.Ordinal))
        {
            var half = StreetSite.HalfWidth(line);
            if (MathF.Sqrt(PlanLines.PolylineDistanceSquared(line.Line.Points, p)) > reach + half) continue;
            var run = new StreetSite.Run([.. line.Line.Points]);
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

    // ---------------------------------------------------------------------------------------------------------
    // The module budget (ENG-BUDGET) per 64 m cell: a service cluster that would push its cell over is left out.
    // ---------------------------------------------------------------------------------------------------------

    private static readonly EntranceMatPart MatDef = new();
    private static readonly EntranceLampPart DoorLampDef = new();
    private static readonly ParkedBicyclePart BikeDef = new();

    /// <summary>
    /// True when <paramref name="extra"/> keeps every L0 cell it falls in within the module's triangle and draw budget, counted the way
    /// the module emits them: each distinct part look once per cell for its meshes, its triangles per instance, the pads as one mesh.
    /// </summary>
    private static bool WithinBudget(List<FurnitureItem> items, IReadOnlyList<FurnitureItem> extra, Dictionary<(string, object), (int Meshes, int Triangles)> cache)
    {
        foreach (var cell in extra.Select(i => TileGrid.At(i.Anchor)).Distinct())
        {
            var inCell = items.Where(i => TileGrid.At(i.Anchor) == cell).Concat(extra.Where(i => TileGrid.At(i.Anchor) == cell)).ToList();
            int draws = inCell.Any(i => i.Pad is not null) ? 1 : 0, triangles = 0;
            foreach (var look in inCell.Select(i => (i.Part.Part.Value, i.Part.Params)).Distinct()) draws += Measure(look.Value, look.Params, cache).Meshes;
            foreach (var item in inCell) triangles += Measure(item.Part.Part.Value, item.Part.Params, cache).Triangles;
            if (draws > S.BudgetDrawCalls || triangles > S.BudgetTrianglesL0) return false;
        }
        return true;
    }

    private static (int Meshes, int Triangles) Measure(string part, object p, Dictionary<(string, object), (int Meshes, int Triangles)> cache)
    {
        if (cache.TryGetValue((part, p), out var known)) return known;
        var geometry = part switch
        {
            PartCatalog.StreetLamp => Lamp.Lods[LodLevel.L0]((LampParams)p),
            PartCatalog.EntranceLamp => DoorLampDef.Lods[LodLevel.L0]((LampParams)p),
            PartCatalog.Bench => BenchDef.Lods[LodLevel.L0]((FurnitureParams)p),
            PartCatalog.BinFour => BinDef.Lods[LodLevel.L0]((FurnitureParams)p),
            PartCatalog.BusStop => StopDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.AtlasKiosk => KioskDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.AtlasPost => PostDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.OpenSign => SignDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.EntranceMat => MatDef.Lods[LodLevel.L0]((PanelParams)p),
            PartCatalog.ParkedCar => CarDef.Lods[LodLevel.L0]((VehicleParams)p),
            PartCatalog.Signpost => SignpostDef.Lods[LodLevel.L0]((SignParams)p),
            PartCatalog.DrinkingFountain => FountainDef.Lods[LodLevel.L0]((EquipmentParams)p),
            PartCatalog.VendingMachine => VendingDef.Lods[LodLevel.L0]((EquipmentParams)p),
            PartCatalog.DeliveryLocker => LockerDef.Lods[LodLevel.L0]((EquipmentParams)p),
            PartCatalog.BicycleShelter => BayDef.Lods[LodLevel.L0]((RoofParams)p),
            PartCatalog.ParkedBicycle => BikeDef.Lods[LodLevel.L0]((VehicleParams)p),
            StreetStyle.KerbRampId => RampDef.Lods[LodLevel.L0]((RampParams)p),
            _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown street part."),
        };
        var measured = (geometry.Meshes.Count, geometry.Meshes.Sum(m => m.Index.Length / 3));
        cache.Add((part, p), measured);
        return measured;
    }
}
