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
    // Parked cars (WLD-LIFE): a few in each car park.
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
        // Each car look (shape and colour) is its own mesh set: a 64 m cell keeps to a few of them (draw calls, ART-MODELING).
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
}
