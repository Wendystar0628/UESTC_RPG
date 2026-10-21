using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurniturePlanner;
using static Campus.Gen.Furniture.FurnitureParts;

namespace Campus.Gen.Furniture;

/// <summary>Lays out vending groups, their lockers, then bicycle bays under one capacity cache.</summary>
internal static class FurnitureServices
{
    internal static void Plan(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, IReadOnlyList<RoadGroup> roads)
    {
        var capacity = new FurnitureCapacity();
        var axis = roads.Where(r => r.Tier == RoadTier.Avenue).SelectMany(r => r.Lines).ToList();
        var gates = FurnitureServiceSites.Gates(site.World);
        var lockerSpots = Vending(site, items, diagnostics, axis, gates, capacity);
        Lockers(site, items, diagnostics, lockerSpots, capacity);
        Bicycles(site, items, diagnostics, gates, capacity);
    }

    private static List<ServiceSite> Vending(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, IReadOnlyList<NetworkLine> axis, List<ServiceSite> gates, FurnitureCapacity capacity)
    {
        // Vending groups: the gates first, then the canteens and the sports venues; one to a line of sight, none on the main axis.
        var vendingSites = gates.Concat(FurnitureServiceSites.Buildings(site, S.VendingCategories, door: false)).ToList();
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
            if (FurnitureServiceSlots.ServiceFit(site, spot, width, S.Vending.Depth, S.ServiceFront, S.ServiceReach) is not { } fit) { cramped++; continue; }
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
            if (!capacity.Allows(items, group)) { heavy++; continue; }
            items.AddRange(group);
            machines += count;
            if (!withLocker) plain++;
            site.Occupy(fit.Ends.A, fit.Ends.B, fit.Ends.R);
            groups.Add(spot);
            if (withLocker) lockerSpots.Add(spot);
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.vending.summary", $"vending: {machines} machines in {groups.Count} groups at {string.Join(", ", groups.Select(g => g.Label))} " +
            $"({axial} sites on the main axis, {crowded} within {Num(S.VendingSpacing)} m of a group, {cramped} with no room, {heavy} over a cell's budget, left out)"));

        return lockerSpots;
    }

    private static void Lockers(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, List<ServiceSite> lockerSpots, FurnitureCapacity capacity)
    {
        // Delivery lockers: beside the first gates that hold a vending group (their WLD-LIFE places wait for game/gen/maps/qingshuihe/boundary/'s gates).
        var lockers = new List<string>();
        foreach (var spot in lockerSpots)
        {
            if (FurnitureServiceSlots.ServiceFit(site, spot, S.Locker.Width, S.Locker.Depth, S.ServiceFront, S.ServiceReach) is not { } fit) continue;
            var locker = Fitted($"locker:{spot.Label}", FurnitureKind.DeliveryLocker, PartCatalog.DeliveryLocker, S.Locker, fit, LockerDef.Collision(S.Locker));
            if (!capacity.Allows(items, [locker])) continue;
            items.Add(locker);
            site.Occupy(fit.Ends.A, fit.Ends.B, fit.Ends.R);
            lockers.Add(spot.Label);
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.lockers.summary", $"delivery lockers: {lockers.Count} at {string.Join(", ", lockers)}"));
    }

    private static void Bicycles(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, List<ServiceSite> gates, FurnitureCapacity capacity)
    {
        // Shared-bicycle bays: the open cluster gates, the doors of teaching buildings, libraries and canteens, then the places the map
        // marks for parking bicycles (OSM 非机动车 points).
        var parking = site.World.Features.Values.Where(f => S.BaySubcategories.Contains(WorldProps.TryText(f.Props, "subcategory") ?? "")).OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Select(f => new ServiceSite(f.Id.Value, Anchor(f), null, null));
        var baySites = gates.Where(g => g.Open).Concat(FurnitureServiceSites.Buildings(site, S.BayCategories, door: true)).Concat(parking).ToList();
        var bays = new List<string>();
        var placedAt = new List<Vector2>();
        var bikes = 0;
        var heavy = 0;
        foreach (var spot in baySites)
        {
            if (placedAt.Any(b => Vector2.Distance(b, spot.At) < S.BaySpacing)) continue;
            var p = S.BikeBay;
            if (FurnitureServiceSlots.ServiceFit(site, spot, p.Width, p.Depth, S.WalkGap, spot.Front is null ? S.ServiceReach : S.BayFar) is not { } fit) continue;
            var bay = new List<FurnitureItem> { Fitted($"bay:{spot.Label}", FurnitureKind.BicycleBay, PartCatalog.BicycleShelter, p, fit, BayDef.Collision(p)) };
            // Stand-in stock until game/camera/walk/ mounts the rentable bikes: every other slot, front wheel to the back line.
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
            // Over the cell's budget with its stand-in bikes, an open gate's bay stands empty for game/camera/walk/'s stock; any other is left out.
            if (!capacity.Allows(items, bay))
            {
                if (!spot.Open) { heavy++; continue; }
                bay.RemoveRange(1, bay.Count - 1);
                if (!capacity.Allows(items, bay)) continue;
            }
            items.AddRange(bay);
            bikes += bay.Count - 1;
            site.Occupy(fit.Ends.A, fit.Ends.B, fit.Ends.R);
            placedAt.Add(spot.At);
            bays.Add(spot.Label);
        }
        diagnostics.Add(new(DiagnosticLevel.Info, "wp13.bicycles.summary", $"bicycle bays: {bays.Count} with {bikes} stand-in bikes at {string.Join(", ", bays)} ({heavy} over a cell's budget, left out)"));
    }
}
