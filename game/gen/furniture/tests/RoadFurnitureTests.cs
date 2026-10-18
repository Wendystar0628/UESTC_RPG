using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

[TestSuite]
public sealed class RoadFurnitureTests
{
    [TestCase]
    public void CarsParkSparselyInTheLotsAndKerbRampsMeetTheWalks()
    {
        // WLD-LIFE: a few cars in each car park, whole inside it, noses to its longest edge; never more than the cap a lot.
        var world = World.Value;
        var layout = Build(world);
        var site = new StreetSite(world);
        var cars = layout.Items.Where(i => i.Kind == FurnitureKind.ParkedCar).ToList();
        Console.WriteLine($"cars {cars.Count} in {cars.Select(c => c.Key.Split(':')[1]).Distinct().Count()} of {site.ParkingLots.Count} lots; " +
            string.Join(", ", cars.Take(6).Select(c => $"{c.Key} ({c.Anchor.X:F1}, {c.Anchor.Y:F1})")));
        Check(cars.Count > 0, "no parked cars");
        foreach (var group in cars.GroupBy(c => c.Key.Split(':')[1]))
            Check(group.Count() <= FurnitureStyle.CarsPerLot, $"lot {group.Key} holds {group.Count()} cars");
        foreach (var car in cars)
        {
            var p = (VehicleParams)car.Part.Params;
            var lot = site.ParkingLots[int.Parse(car.Key.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture)];
            foreach (var (u, v) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var q = Xf.Point(car.Part.At, new(u * p.Width * 0.5f, 0, v * p.Depth * 0.5f));
                Check(PlanGeometry.Contains(lot, new(q.X, q.Z)), $"{car.Key} sticks out of its lot");
            }
            Check(car.Colliders.Count == 1 && car.Colliders[0].Foot == FootMat.Metal, $"{car.Key}: one Metal box (WLD-LIFE)");
            Check(StreetStyle.CarBodies.Any(c => p.Material.Lit.Value == c + ".lit"), $"{car.Key} is not in a car body colour");
        }
        // Kerb ramps on the osmanthus road where its walks reach the kerb: on the carriageway against the kerb, rising towards the walk.
        var index = FurnitureStyle.Roads.ToList().FindIndex(r => r.KerbRamps);
        var rule = FurnitureStyle.Roads[index];
        var ramps = layout.Items.Where(i => i.Kind == FurnitureKind.KerbRamp).ToList();
        var line = layout.Diagnostics.Single(d => d.Code == "wp13.kerb-ramps.summary" && d.Message.StartsWith($"road {rule.Name}: ", StringComparison.Ordinal)).Message;
        Console.WriteLine(line + "; " + string.Join(", ", ramps.Select(r => $"{r.Key} ({r.Anchor.X:F1}, {r.Anchor.Y:F1})")));
        Check(ramps.All(r => r.Key.StartsWith($"ramp:{index}:", StringComparison.Ordinal)), "a kerb ramp on another road");
        var road = world.Network.Roads.Where(l => StreetSite.RoadName(l) == rule.Name && StreetSite.Carriageway(l)).ToList();
        foreach (var ramp in ramps)
        {
            var p = (RampParams)ramp.Part.Params;
            var back = ramp.Anchor - Front(ramp.Part.At) * (p.Depth * 0.5f);
            var kerb = road.Min(l => Distance(back, l) - StreetSite.HalfWidth(l));
            Check(MathF.Abs(kerb) < 0.05f, $"{ramp.Key}: its top edge is {kerb:F2} m off the kerb");
            Check(road.Min(l => Distance(ramp.Anchor + Front(ramp.Part.At) * 2f, l)) < road.Min(l => Distance(ramp.Anchor, l)), $"{ramp.Key} does not face the road");
        }
        for (var i = 0; i < ramps.Count; i++)
            for (var k = i + 1; k < ramps.Count; k++)
                Check(Vector2.Distance(ramps[i].Anchor, ramps[k].Anchor) >= FurnitureStyle.RampGap - 1e-3f, $"{ramps[i].Key} and {ramps[k].Key} crowd each other");
    }

    [TestCase]
    public void AvenueLampsKeepTheirRhythm()
    {
        // MAP-QSH-ADDITIONS: the avenue's lamps keep a regular 25–30 m; only junctions may nudge one by a step or two.
        foreach (var side in new[] { ":l:", ":r:" })
        {
            var row = Build(World.Value).Items.Where(i => i.Kind == FurnitureKind.StreetLamp && i.Key.StartsWith("lamp:0:0" + side, StringComparison.Ordinal)).ToList();
            var gaps = row.Zip(row.Skip(1), (p, q) => Vector2.Distance(p.Anchor, q.Anchor)).ToList();
            Check(gaps.Count > 30, $"{gaps.Count} gaps on side {side}");
            var regular = gaps.Count(g => g is >= 24f and <= 31f);
            Check(regular >= gaps.Count * 0.75f, $"side {side}: only {regular} of {gaps.Count} gaps are 25–30 m");
        }
    }

    [TestCase]
    public void RoadFurnitureKeepsOffRoadsBuildingsAndWater()
    {
        var world = World.Value;
        var outlines = world.Features.Values.Where(f => f.Kind.Value is "building" or "water").SelectMany(Polygons).ToList();
        foreach (var item in Build(world).Items.Where(i => i.Kind is FurnitureKind.StreetLamp or FurnitureKind.Bench or FurnitureKind.Bin or FurnitureKind.BusStop))
        {
            var p = item.Anchor;
            Check(PlanGeometry.Contains(world.Boundary, p), $"{item.Key} outside the campus");
            Check(!outlines.Any(o => PlanGeometry.Contains(o, p)), $"{item.Key} inside a building or water");
            foreach (var road in world.Network.Roads)
            {
                var d = MathF.Sqrt(road.Line.Points.Zip(road.Line.Points.Skip(1), (u, v) => PlanGeometry.SegmentDistanceSquared(p, u, v)).DefaultIfEmpty(float.MaxValue).Min());
                Check(d > MathF.Max(road.Width, 2f) * 0.5f + 0.2f, $"{item.Key} stands on {road.Id.Value} ({d:F2} m from its centre line)");
            }
        }
    }
}
